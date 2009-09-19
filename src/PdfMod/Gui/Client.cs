// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Robert Dyer
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Collections.Generic;
using System.IO;

using Gtk;
using Mono.Unix;

using Hyena;
using Hyena.Gui;

using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

using PdfMod.Pdf;

namespace PdfMod.Gui
{
    public class Client : Core.Client
    {
        static int app_count = 0;
        static string accel_map_file = Path.Combine (Path.Combine (
            XdgBaseDirectorySpec.GetUserDirectory ("XDG_CONFIG_HOME", ".config"), "pdfmod"), "gtk_accel_map");

        Gtk.MenuBar menu_bar;
        Gtk.Label status_label;
        QueryBox query_box;

        bool loading;
        string original_size_string = null;
        long original_size;

        public ActionManager ActionManager { get; private set; }
        public Gtk.Toolbar HeaderToolbar;
        public Actions Actions { get; private set; }
        public Gtk.Statusbar StatusBar { get; private set; }
        public Gtk.Window Window { get; private set; }
        public DocumentIconView IconView { get; private set; }
        public MetadataEditorBox EditorBox { get; private set; }

        static Client ()
        {
            Gtk.Application.Init ();
            ThreadAssist.InitializeMainThread ();
            ThreadAssist.ProxyToMainHandler = RunIdle;
            Hyena.Log.Notify += OnLogNotify;
            Gtk.Window.DefaultIconName = "pdfmod";

            try {
                if (System.IO.File.Exists (accel_map_file)) {
                    Gtk.AccelMap.Load (accel_map_file);
                    Hyena.Log.DebugFormat ("Loaded custom AccelMap from {0}", accel_map_file);
                }
            } catch (Exception e) {
                Hyena.Log.Exception ("Failed to load custom AccelMap", e);
            }
        }

        public Client () : this (false)
        {
        }

        internal Client (bool loadFiles)
        {
            app_count++;

            Window = new Gtk.Window (Gtk.WindowType.Toplevel) { Title = Catalog.GetString ("PDF Mod") };
            Window.SetSizeRequest (640, 480);
            Window.DeleteEvent += delegate (object o, DeleteEventArgs args) {
                Quit ();
                args.RetVal = true;
            };

            // PDF Icon View
            IconView = new DocumentIconView (this);
            var iconview_sw = new Gtk.ScrolledWindow () { Child = IconView };

            query_box = new QueryBox (this) { NoShowAll = true };
            query_box.Hide ();

            // Status bar
            StatusBar = new Gtk.Statusbar () { HasResizeGrip = true };
            status_label = new Label () { Xalign = 0.0f };
            StatusBar.PackStart (status_label, true, true, 6);
            StatusBar.ReorderChild (status_label, 0);

            // ActionManager
            ActionManager = new Hyena.Gui.ActionManager ();
            Window.AddAccelGroup (ActionManager.UIManager.AccelGroup);
            Actions = new Actions (this, ActionManager);

            EditorBox = new MetadataEditorBox (this) { NoShowAll = true };
            EditorBox.Hide ();

            // Menubar
            menu_bar = ActionManager.UIManager.GetWidget ("/MainMenu") as MenuBar;

            // Toolbar
            HeaderToolbar = ActionManager.UIManager.GetWidget ("/HeaderToolbar") as Gtk.Toolbar;
            HeaderToolbar.ShowArrow = false;
            HeaderToolbar.ToolbarStyle = ToolbarStyle.Icons;
            HeaderToolbar.Tooltips = true;
            HeaderToolbar.NoShowAll = true;
            HeaderToolbar.Visible = Configuration.ShowToolbar;

            var vbox = new VBox ();
            vbox.PackStart (menu_bar, false, false, 0);
            vbox.PackStart (HeaderToolbar, false, false, 0);
            vbox.PackStart (EditorBox, false, false, 0);
            vbox.PackStart (query_box, false, false, 0);
            vbox.PackStart (iconview_sw, true, true, 0);
            vbox.PackStart (StatusBar, false, true, 0);
            Window.Add (vbox);

            Window.ShowAll ();

            if (loadFiles) {
                RunIdle (LoadFiles);
                Application.Run ();
            }
        }

        public void ToggleMatchQuery ()
        {
            if (query_box.Entry.HasFocus) {
                query_box.Hide ();
            } else {
                query_box.Show ();
                query_box.Entry.GrabFocus ();
            }
        }

        public void Quit ()
        {
            if (Window == null) {
                return;
            }

            if (PromptIfUnsavedChanges ()) {
                return;
            }

            if (Document != null) {
                Document.Dispose ();
            }

            if (IconView != null) {
                IconView.Dispose ();
                IconView = null;
            }

            Window.Destroy ();
            Window = null;

            if (--app_count == 0) {
                try {
                    Directory.CreateDirectory (Path.GetDirectoryName (accel_map_file));
                    Gtk.AccelMap.Save (accel_map_file);
                } catch (Exception e) {
                    Hyena.Log.Exception ("Failed to save custom AccelMap", e);
                }

                Application.Quit ();
            }
        }

        bool PromptIfUnsavedChanges ()
        {
            if (Document != null && Document.HasUnsavedChanged) {
                var message_dialog = new Hyena.Widgets.HigMessageDialog (
                    Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.None,
                    Catalog.GetString ("Save the changes made to this document?"),
                    String.Empty
                );
                message_dialog.AddButton (Catalog.GetString ("Close _Without Saving"), ResponseType.Close, false);
                message_dialog.AddButton (Stock.Cancel, ResponseType.Cancel, false);
                message_dialog.AddButton (Stock.SaveAs, ResponseType.Ok, true);

                var response = (ResponseType) message_dialog.Run ();
                message_dialog.Destroy ();
                switch (response) {
                    case ResponseType.Ok:
                        Actions["SaveAs"].Activate ();
                        return PromptIfUnsavedChanges ();
                    case ResponseType.Close:
                        return false;
                    case ResponseType.Cancel:
                    case ResponseType.DeleteEvent:
                        return true;
                }
            }
            return false;
        }

        public override void LoadFiles (IList<string> files)
        {
            if (files.Count == 1) {
                LoadPath (files[0]);
            } else if (files.Count > 1) {
                // Make sure the user wants to open N windows
                var message_dialog = new Hyena.Widgets.HigMessageDialog (
                    Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None,
                    String.Format (Catalog.GetPluralString (
                        "Continue, opening {0} document in separate windows?", "Continue, opening all {0} documents in separate windows?", files.Count),
                        files.Count),
                    String.Empty);
                message_dialog.AddButton (Stock.Cancel, ResponseType.Cancel, false);
                message_dialog.AddButton (Catalog.GetString ("Open _First"), ResponseType.Accept, false);
                message_dialog.AddButton (Catalog.GetString ("Open _All"), ResponseType.Ok, true);
                var response = message_dialog.Run ();
                message_dialog.Destroy ();

                if ((Gtk.ResponseType)response == Gtk.ResponseType.Ok) {
                    foreach (string file in files) {
                        LoadPath (file);
                    }
                } else if ((Gtk.ResponseType)response == Gtk.ResponseType.Accept) {
                    LoadPath (files[0]);
                }
            }
        }

        public override void LoadPath (string path, string suggestedFilename)
        {
            lock (this) {
                // One document per window
                if (loading || Document != null) {
                    new Client ().LoadPath (path, suggestedFilename);
                    return;
                }

                loading = true;
            }

            Configuration.LastOpenFolder = System.IO.Path.GetDirectoryName (suggestedFilename ?? path);
            status_label.Text = Catalog.GetString ("Loading document...");

            ThreadAssist.SpawnFromMain (delegate {
                try {
                    Document = new Document ();
                    Document.Load (path, PasswordProvider, suggestedFilename != null);
                    if (suggestedFilename != null) {
                        Document.SuggestedSavePath = suggestedFilename;
                    }

                    ThreadAssist.ProxyToMain (delegate {
                        IconView.SetDocument (Document);
                        RecentManager.Default.AddItem (Document.Uri);
                        Document.Changed += UpdateForDocument;
                        UpdateForDocument ();
                        OnDocumentLoaded ();
                    });
                } catch (Exception e) {
                    Document = null;
                    ThreadAssist.ProxyToMain (delegate {
                        status_label.Text = "";
                        if (e is System.IO.FileNotFoundException) {
                            try {
                                RecentManager.Default.RemoveItem (new Uri(path).AbsoluteUri);
                            } catch {}
                        }
                    });

                    Hyena.Log.Exception (e);
                    Hyena.Log.Error (
                        Catalog.GetString ("Error Loading Document"),
                        String.Format (Catalog.GetString ("There was an error loading {0}"), GLib.Markup.EscapeText (path ?? "")), true
                    );
                } finally {
                    lock (this) {
                        loading = false;
                    }
                }
            });
        }

        void UpdateForDocument ()
        {
            var current_size = Document.FileSize;
            string size_str = null;
            if (original_size_string == null) {
                size_str = original_size_string = new Hyena.Query.FileSizeQueryValue (current_size).ToUserQuery ();
                original_size = current_size;
            } else if (current_size == original_size) {
                size_str = original_size_string;
            } else {
                string current_size_string = new Hyena.Query.FileSizeQueryValue (current_size).ToUserQuery ();
                if (current_size_string == original_size_string) {
                    size_str = original_size_string;
                } else {
                    // Translators: this string is used to show current/original file size, eg "2 MB (originally 1 MB)"
                    size_str = String.Format (Catalog.GetString ("{0} (originally {1})"), current_size_string, original_size_string);
                }
            }

            status_label.Text = String.Format ("{0} \u2013 {1}",
                String.Format (Catalog.GetPluralString ("{0} page", "{0} pages", Document.Count), Document.Count),
                size_str
            );

            var title = Document.Title;
            var filename = Document.Filename;
            if (Document.HasUnsavedChanged) {
                filename = "*" + filename;
            }
            Window.Title = title == null ? filename : String.Format ("{0} - {1}", filename, title);
        }

        public void PasswordProvider (PdfPasswordProviderArgs args)
        {
            // This method is called from some random thread, but we need
            // to do the dialog on the GUI thread; use the reset_event
            // to block this thread until the user is done with the dialog.
            var reset_event = new System.Threading.ManualResetEvent (false);

            ThreadAssist.ProxyToMain (delegate {
                Log.Debug ("Password requested to open document");
                var dialog = new Hyena.Widgets.HigMessageDialog (
                    Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None,
                    Catalog.GetString ("Document is Encrypted"),
                    Catalog.GetString ("Enter the document's password to open it:")
                );
                dialog.Image = Gtk.IconTheme.Default.LoadIcon ("dialog-password", 48, 0);

                var password_entry = new Entry () { Visibility = false };
                password_entry.Show ();
                dialog.LabelVBox.PackStart (password_entry, false, false, 12);

                dialog.AddButton (Stock.Cancel, ResponseType.Cancel, false);
                dialog.AddButton (Stock.Ok, ResponseType.Ok, true);

                var response = (ResponseType)dialog.Run ();
                string password = password_entry.Text;
                dialog.Destroy ();

                if (response == ResponseType.Ok) {
                    args.Password = Document.Password = password;
                } else {
                    Log.Information ("Password dialog cancelled");
                    args.Abort = true;
                }
                reset_event.Set ();
            });

            reset_event.WaitOne ();
        }

        static void OnLogNotify (LogNotifyArgs args)
        {
            ThreadAssist.ProxyToMain (delegate {
                Gtk.MessageType mtype;
                var entry = args.Entry;

                switch (entry.Type) {
                    case LogEntryType.Warning:
                        mtype = Gtk.MessageType.Warning;
                        break;
                    case LogEntryType.Information:
                        mtype = Gtk.MessageType.Info;
                        break;
                    case LogEntryType.Error:
                    default:
                        mtype = Gtk.MessageType.Error;
                        break;
                }

                Hyena.Widgets.HigMessageDialog dialog = new Hyena.Widgets.HigMessageDialog (
                    null, Gtk.DialogFlags.Modal, mtype, Gtk.ButtonsType.Close, entry.Message, entry.Details);

                dialog.Title = String.Empty;
                dialog.Run ();
                dialog.Destroy ();
            });
        }

        public static void RunIdle (InvokeHandler handler)
        {
            GLib.Idle.Add (delegate { handler (); return false; });
        }
    }
}
