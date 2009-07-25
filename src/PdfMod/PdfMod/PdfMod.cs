using System;

using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Gui;

using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfMod
{
    public class PdfMod
    {
        private static int app_count = 0;
        private static readonly string CacheDir = System.IO.Path.Combine (System.Environment.GetFolderPath (System.Environment.SpecialFolder.ApplicationData), "pdfmod");

        public static void Main (string[] args)
        {
            ThreadAssist.InitializeMainThread ();
            ThreadAssist.ProxyToMainHandler = RunIdle;

            Hyena.Log.Debugging = true;
            Hyena.Log.DebugFormat ("Starting PdfMod");

            Application.Init ();

            try {
                System.IO.Directory.CreateDirectory (CacheDir);
            } catch (Exception e) {
                Log.Exception (String.Format ("Unable to create cache directory: {0}", CacheDir), e);
            }

            var app = new PdfMod ();
            RunIdle (app.LoadFiles);

            Application.Run ();
        }

        private MenuBar menu_bar;
        private Gtk.Toolbar header_toolbar;
        private Gtk.Label size_label;
        private QueryBox query_box;

        public ActionManager ActionManager { get; private set; }
        public GlobalActions GlobalActions { get; private set; }
        public Gtk.Statusbar StatusBar { get; private set; }
        public Gtk.Window Window { get; private set; }
        public PdfIconView IconView { get; private set; }
        public Document Document { get; private set; }
        public MetadataEditorBox EditorBox { get; private set; }

        public event EventHandler DocumentLoaded;

        public PdfMod ()
        {
            app_count++;

            Window = new Gtk.Window (WindowType.Toplevel);
            Window.Title = Catalog.GetString ("PDF Mod");
            Window.SetSizeRequest (640, 480);
            Window.DeleteEvent += delegate (object o, DeleteEventArgs args) {
                Quit ();
                args.RetVal = true;
            };

            // PDF Icon View
            IconView = new PdfIconView (this);
            var IconView_sw = new Gtk.ScrolledWindow ();
            IconView_sw.Child = IconView;

            query_box = new QueryBox (this) { NoShowAll = true };
            query_box.Hide ();

            // Status bar
            StatusBar = new Gtk.Statusbar () { HasResizeGrip = true };
            size_label = new Label ();
            size_label.Xalign = 0;
            StatusBar.PackStart (size_label, false, false, 0);

            // ActionManager
            ActionManager = new Hyena.Gui.ActionManager ();
            Window.AddAccelGroup (ActionManager.UIManager.AccelGroup);
            GlobalActions = new GlobalActions (this, ActionManager);

            EditorBox = new MetadataEditorBox (this) { NoShowAll = true };
            EditorBox.Hide ();

            // Menubar
            menu_bar = ActionManager.UIManager.GetWidget ("/MainMenu") as MenuBar;

            // Toolbar
            header_toolbar = ActionManager.UIManager.GetWidget ("/HeaderToolbar") as Gtk.Toolbar;
            header_toolbar.ShowArrow = false;
            header_toolbar.ToolbarStyle = ToolbarStyle.Icons;
            header_toolbar.Tooltips = true;

            var vbox = new VBox ();
            vbox.PackStart (menu_bar, false, false, 0);
            vbox.PackStart (header_toolbar, false, false, 0);
            vbox.PackStart (EditorBox, false, false, 0);
            vbox.PackStart (query_box, false, false, 0);
            vbox.PackStart (IconView_sw, true, true, 0);
            vbox.PackStart (StatusBar, false, true, 0);
            Window.Add (vbox);

            Window.ShowAll ();
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

            Window.Destroy ();
            Window = null;

            if (--app_count == 0) {
                Application.Quit ();
            }
        }

        private bool PromptIfUnsavedChanges ()
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
                        GlobalActions["SaveAsAction"].Activate ();
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

        private void LoadFiles ()
        {
            var files = ApplicationContext.CommandLine.Files;
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

        // TODO support password protected docs
        public void LoadPath (string path)
        {
            LoadPath (path, null);
        }

        public void LoadPath (string path, string suggestedFilename)
        {
            // One document per window
            if (Document != null) {
                new PdfMod ().LoadPath (path, suggestedFilename);
                return;
            }

            var ctx_id = StatusBar.GetContextId ("loading");
            var msg_id = StatusBar.Push (1, String.Format (Catalog.GetString ("Loading {0}"), GLib.Markup.EscapeText (path)));

            try {
                Document = new Document (path, null, suggestedFilename != null);
                if (suggestedFilename != null) {
                    Document.SuggestedSavePath = suggestedFilename;
                }

                IconView.SetDocument (Document);
                Document.Changed += UpdateForDocument;
                UpdateForDocument ();

                var handler = DocumentLoaded;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            } catch (Exception e) {
                Document = null;
                Hyena.Log.Exception (e);
                Hyena.Log.Error (
                    Catalog.GetString ("Error Loading Document"),
                    String.Format (Catalog.GetString ("There was an error loading {0}"), GLib.Markup.EscapeText (path ?? "")), true
                );
            } finally {
                StatusBar.Remove (ctx_id, msg_id);
            }
        }
        private string original_size_string = null;
        private long original_size;
        private void UpdateForDocument ()
        {
            var current_size = Document.FileSize;
            if (original_size_string == null) {
                size_label.Text = original_size_string = new Hyena.Query.FileSizeQueryValue (current_size).ToUserQuery ();
                original_size = current_size;
            } else if (current_size == original_size) {
                size_label.Text = original_size_string;
            } else {
                size_label.Text = String.Format (Catalog.GetString ("{0} (originally {1})"),
                    new Hyena.Query.FileSizeQueryValue (current_size).ToUserQuery (),
                    original_size_string
                );
            }

            var title = Document.Title;
            var filename = Document.Filename;
            Window.Title = title == null ? filename : String.Format ("{0} ({1})", title, filename);
        }

        public static string GetTmpFilename ()
        {
            string filename = null;
            int i = 0;
            while (filename == null) {
                filename = System.IO.Path.Combine (CacheDir, "tmpfile-" + i++);
                if (System.IO.File.Exists (filename)) {
                    filename = null;
                }
            }
            return filename;
        }

        public static void RunIdle (InvokeHandler handler)
        {
            GLib.Idle.Add (delegate { handler (); return false; });
        }
    }
}
