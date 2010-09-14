// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Julien Rebetez
// Copyright (C) 2009 Łukasz Jernaś
// Copyright (C) 2009 Andreu Correa Casablanca
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
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Gui;

using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

using PdfMod.Pdf;
using PdfMod.Pdf.Actions;

namespace PdfMod.Gui
{
    public class Actions : HyenaActionGroup
    {
        Client app;
        UndoManager undo_manager;
        const string WIKI_URL = "http://live.gnome.org/PdfMod";
        const string DOCS_URL = "http://library.gnome.org/users/pdfmod/";

        static string [] require_doc_actions = new string[] {
            "Save", "SaveAs", "Properties", "Undo", "Redo", "ZoomFit", "OpenInViewer",
            "SelectAll", "SelectEvens", "SelectOdds", "SelectMatching", "SelectInverse", "InsertFrom", "ExportImages",
            "ViewBookmarks", "AddBookmark", "EditBookmarks"
        };

        static string [] require_page_actions = new string[] {
            "Remove", "Extract", "RotateRight", "RotateLeft"
        };

        public UndoManager UndoManager { get { return undo_manager; } }

        public Actions (Client app, ActionManager action_manager) : base (action_manager, "Global")
        {
            this.app = app;
            undo_manager = new UndoManager ();

            AddImportant (
                new ActionEntry ("FileMenu", null, Catalog.GetString ("_File"), null, null, null),
                new ActionEntry ("Open",   Gtk.Stock.Open,   null, "<control>O", Catalog.GetString ("Open a document"), OnOpen),
                new ActionEntry ("Save",   Gtk.Stock.Save,   null, "<control>S", Catalog.GetString ("Save changes to this document, overwriting the existing file"), OnSave),
                new ActionEntry ("SaveAs", Gtk.Stock.SaveAs, null, "<control><shift>S", Catalog.GetString ("Save this document to a new file"), OnSaveAs),
                new ActionEntry ("RecentMenu", null, Catalog.GetString ("Recent _Files"), null, null, null),
                new ActionEntry ("ExportImages", null, Catalog.GetString ("Export Images"), null, Catalog.GetString ("Save all images in this document to a new folder"), OnExportImages),
                new ActionEntry ("InsertFrom", Gtk.Stock.Add, Catalog.GetString("_Insert From..."), null, Catalog.GetString("Insert pages from another document"), OnInsertFrom),
                new ActionEntry ("Close", Gtk.Stock.Close, null, "<control>W", null, OnClose),

                new ActionEntry ("EditMenu", null, Catalog.GetString ("_Edit"), null, null, null),
                new ActionEntry ("Undo", Stock.Undo, null, "<control>z", null, OnUndo),
                new ActionEntry ("Redo", Stock.Redo, null, "<control>y", null, OnRedo),
                new ActionEntry ("Extract", Gtk.Stock.New, null, null, null, OnExtractPages),
                new ActionEntry ("Remove", Gtk.Stock.Remove, null, "Delete", null, OnRemove),
                new ActionEntry ("RotateLeft", null, Catalog.GetString ("Rotate Left"), "bracketleft", Catalog.GetString ("Rotate left"), OnRotateLeft),
                new ActionEntry ("RotateRight", null, Catalog.GetString ("Rotate Right"), "bracketright", Catalog.GetString ("Rotate right"), OnRotateRight),
                new ActionEntry ("SelectAll", Stock.SelectAll, null, "<control>A", null, OnSelectAll),
                new ActionEntry ("SelectOdds", null, Catalog.GetString ("Select Odd Pages"), null, null, OnSelectOdds),
                new ActionEntry ("SelectEvens", null, Catalog.GetString ("Select Even Pages"), null, null, OnSelectEvens),
                new ActionEntry ("SelectMatching", null, Catalog.GetString ("Select Matching..."), "<control>F", null, OnSelectMatching),
                new ActionEntry ("SelectInverse", null, Catalog.GetString ("_Invert Selection"), "<shift><control>I", null, OnSelectInverse),

                new ActionEntry ("ViewMenu", null, Catalog.GetString ("_View"), null, null, null),
                new ActionEntry ("ZoomIn", Stock.ZoomIn, null, "<control>plus", null, OnZoomIn),
                new ActionEntry ("ZoomOut", Stock.ZoomOut, null, "<control>minus", null, OnZoomOut),
                new ActionEntry ("OpenInViewer", null, Catalog.GetString ("Open in Viewer"), "F5", Catalog.GetString ("Open in viewer"), OnOpenInViewer),

                new ActionEntry ("BookmarksMenu", null, Catalog.GetString ("_Bookmarks"), null, null, null),
                new ActionEntry ("AddBookmark", null, Catalog.GetString ("_Add Bookmark"), "<control>d", null, null),
                new ActionEntry ("RenameBookmark", null, Catalog.GetString ("Re_name Bookmark"), "F2", null, null),
                new ActionEntry ("ChangeBookmarkDest", null, Catalog.GetString ("_Change Bookmark Destination"), null, null, null),
                new ActionEntry ("RemoveBookmarks", Stock.Remove, Catalog.GetString ("_Remove Bookmark"), null, null, null),
                new ActionEntry ("EditBookmarks", null, Catalog.GetString ("_Edit Bookmarks"), "<control>B", null, OnEditBookmarks),

                new ActionEntry ("HelpMenu", null, Catalog.GetString ("_Help"), null, null, null),
                new ActionEntry ("Help", Stock.Help, Catalog.GetString ("_Contents"), "F1", null, OnHelp),
                new ActionEntry ("About", Stock.About, null, null, null, OnAbout),

                new ActionEntry ("PageContextMenu", null, "", null, null, OnPageContextMenu),
                new ActionEntry ("BookmarkContextMenu", null, "", null, null, OnBookmarkContextMenu)
            );

            this["AddBookmark"].ShortLabel = Catalog.GetString ("_Add");
            this["RemoveBookmarks"].ShortLabel = Catalog.GetString ("_Remove");

            AddImportant (
                new ToggleActionEntry ("Properties", Stock.Properties, null, "<alt>Return", Catalog.GetString ("View and edit the title, keywords, and more for this document"), OnProperties, false),
                new ToggleActionEntry ("ZoomFit", Stock.ZoomFit, null, "<control>0", null, OnZoomFit, true),
                new ToggleActionEntry ("ViewToolbar", null, Catalog.GetString ("Toolbar"), null, null, OnViewToolbar, Client.Configuration.ShowToolbar),
                new ToggleActionEntry ("ViewBookmarks", null, Catalog.GetString ("Bookmarks"), "F9", null, OnViewBookmarks, Client.Configuration.ShowBookmarks),
                new ToggleActionEntry ("FullScreenView", null, Catalog.GetString ("Fullscreen"), "F11", null, OnFullScreenView, false)
            );

            this["RotateRight"].IconName = "object-rotate-right";
            this["RotateLeft"].IconName = "object-rotate-left";
            this["ExportImages"].IconName = "image-x-generic";
            this["ViewBookmarks"].IconName = "user-bookmarks";
            this["AddBookmark"].IconName = "bookmark-new";

            UpdateAction ("Help", true);

            Update ();
            app.IconView.SelectionChanged += OnChanged;
            app.IconView.ZoomChanged += delegate { Update (); };
            app.DocumentLoaded += (o, a) => {
                app.Document.Changed += () => Update ();
                Update ();
            };
            undo_manager.UndoChanged += OnChanged;

            AddUiFromFile ("UIManager.xml");
            Register ();

            // Add additional menu item keybindings
            AddAccel ("/MainMenu/ViewMenu/ZoomIn",  Gdk.ModifierType.ControlMask, Gdk.Key.KP_Add, Gdk.Key.equal);
            AddAccel ("/MainMenu/ViewMenu/ZoomOut", Gdk.ModifierType.ControlMask, Gdk.Key.KP_Subtract, Gdk.Key.underscore);
            AddAccel ("/MainMenu/FileMenu/Close",   Gdk.ModifierType.ControlMask, Gdk.Key.q);
            AddAccel ("/MainMenu/EditMenu/Redo",    Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask, Gdk.Key.z);

            // Set up recent documents menu
            MenuItem recent_item = ActionManager.UIManager.GetWidget ("/MainMenu/FileMenu/RecentMenu") as MenuItem;
            var recent_chooser_item = new RecentChooserMenu (RecentManager.Default) {
                Filter = new RecentFilter (),
                SortType = RecentSortType.Mru
            };
            recent_chooser_item.Filter.AddPattern ("*.pdf");
            recent_chooser_item.ItemActivated += delegate {
                Client.RunIdle (delegate { app.LoadPath (recent_chooser_item.CurrentUri); });
            };
            recent_item.Submenu = recent_chooser_item;
        }

        private void AddAccel (string path, Gdk.ModifierType modifier, params Gdk.Key [] keys)
        {
            var item = ActionManager.UIManager.GetWidget (path);
            foreach (var key in keys) {
                item.AddAccelerator ("activate", ActionManager.UIManager.AccelGroup, (uint)key, modifier, Gtk.AccelFlags.Visible);
            }
        }

        void OnChanged (object o, EventArgs args)
        {
            Update ();
        }

        void Update ()
        {
            bool have_doc = app.Document != null;
            foreach (string action in require_doc_actions)
                UpdateAction (action, true, have_doc);

            bool have_page = have_doc && app.IconView.SelectedItems.Length > 0;
            foreach (string action in require_page_actions)
                UpdateAction (action, true, have_page);

            UpdateAction ("Undo", true, have_doc && undo_manager.CanUndo);
            UpdateAction ("Redo", true, have_doc && undo_manager.CanRedo);

            var undo = undo_manager.UndoAction as IDescribedUndoAction;
            this["Undo"].Label = undo == null
                ? Catalog.GetString ("_Undo")
                : String.Format (Catalog.GetString ("Undo {0}"), undo.Description);

            var redo = undo_manager.RedoAction as IDescribedUndoAction;
            this["Redo"].Label = redo == null
                ? Catalog.GetString ("_Redo")
                : String.Format (Catalog.GetString ("Redo {0}"), redo.Description);

            UpdateAction ("Save", true, have_doc && app.Document.HasUnsavedChanges);
            UpdateAction ("ZoomIn", true, have_doc && app.IconView.CanZoomIn);
            UpdateAction ("ZoomOut", true, have_doc && app.IconView.CanZoomOut);

            int selection_count = app.IconView.SelectedItems.Length;
            this["Remove"].Label = String.Format (Catalog.GetPluralString (
                "Remove Page", "Remove {0} Pages", selection_count),
                selection_count);
            this["Remove"].Tooltip = String.Format (Catalog.GetPluralString (
                "Remove the selected page", "Remove the {0} selected pages", selection_count),
                selection_count);
            this["Extract"].Label = String.Format (Catalog.GetPluralString (
                "Extract Page", "Extract {0} Pages", selection_count),
                selection_count);
            this["Extract"].Tooltip = String.Format (Catalog.GetPluralString (
                "Extract the selected page", "Extract the {0} selected pages", selection_count),
                selection_count);
        }

#region Action Handlers

        // File menu actions

        void OnOpen (object o, EventArgs args)
        {
            var chooser = app.CreateChooser (Catalog.GetString ("Select PDF"), FileChooserAction.Open);
            chooser.SelectMultiple = true;
            chooser.AddButton (Stock.Open, ResponseType.Ok);

            if (app.Document != null) {
                chooser.SetCurrentFolder (System.IO.Path.GetDirectoryName (app.Document.SuggestedSavePath));
            } else {
                chooser.SetCurrentFolder (Client.Configuration.LastOpenFolder);
            }

            var response = chooser.Run ();
            var filenames = chooser.Filenames;
            chooser.Destroy ();

            if (response == (int)ResponseType.Ok) {
                Client.RunIdle (delegate {
                    foreach (var file in filenames) {
                        app.LoadPath (file);
                    }
                });
            }
        }

        void OnSave (object o, EventArgs args)
        {
            app.Document.Save (app.Document.SuggestedSavePath);
            undo_manager.Clear ();
        }

        void OnSaveAs (object o, EventArgs args)
        {
            var chooser = app.CreateChooser (Catalog.GetString ("Save as..."), FileChooserAction.Save);
            chooser.SelectMultiple = false;
            chooser.DoOverwriteConfirmation = true;
            chooser.CurrentName = System.IO.Path.GetFileName (app.Document.SuggestedSavePath);
            chooser.AddButton (Stock.SaveAs, ResponseType.Ok);
            chooser.SetCurrentFolder (System.IO.Path.GetDirectoryName (app.Document.SuggestedSavePath));

            var response = chooser.Run ();
            string filename = chooser.Filename;
            chooser.Destroy ();

            if (response == (int)ResponseType.Ok) {
                Log.DebugFormat ("Saving {0} to {1}", app.Document.Uri, filename);
                app.Document.Save (filename);
                undo_manager.Clear ();
            }
        }

        void OnExportImages (object o, EventArgs args)
        {
            var action = new ExportImagesAction (app.Document, app.Document.Pages);
            if (action.ExportableImageCount == 0) {
                Log.Information ("Found zero exportable images in the selected pages");
                return;
            }

            var export_path_base = Path.Combine (
                Path.GetDirectoryName (app.Document.SuggestedSavePath),
                Hyena.StringUtil.EscapeFilename (
                    System.IO.Path.GetFileNameWithoutExtension (app.Document.Filename))
            );

            var export_path = export_path_base;
            int i = 1;
            while (Directory.Exists (export_path) && i < 100) {
                export_path = String.Format ("{0} ({1})", export_path_base, i++);
            }

            try {
                Directory.CreateDirectory (export_path);
            } catch (Exception e) {
                Hyena.Log.Exception (e);
            }

            action.Do (export_path);
            System.Diagnostics.Process.Start (export_path);
        }

        void OnInsertFrom (object o, EventArgs args)
        {
            var chooser = app.CreateChooser (Catalog.GetString ("Select PDF"), FileChooserAction.Open);
            chooser.SelectMultiple = false;
            chooser.SetCurrentFolder (System.IO.Path.GetDirectoryName (app.Document.SuggestedSavePath));
            chooser.AddButton (Stock.Open, ResponseType.Ok);
            // TODO will uncomment this later; don't want to break string freeze now
            //chooser.AddButton (Catalog.GetString ("_Insert"), ResponseType.Ok);

            var response = chooser.Run ();
            string filename = chooser.Filename;
            chooser.Destroy();

            if (response == (int)ResponseType.Ok) {
                try {
                    app.Document.AddFromUri (new Uri (filename));
                } catch (Exception e) {
                    Hyena.Log.Exception (e);
                    Hyena.Log.Error (
                        Catalog.GetString ("Error Loading Document"),
                        String.Format (Catalog.GetString ("There was an error loading {0}"), GLib.Markup.EscapeText (filename ?? "")), true
                    );
                }
            }
        }

        void OnProperties (object o, EventArgs args)
        {
            app.EditorBox.Visible = (this["Properties"] as ToggleAction).Active;
            if (app.EditorBox.Visible) {
                app.EditorBox.GrabFocus ();
            }
        }

        void OnClose (object o, EventArgs args)
        {
            app.Quit ();
        }

        // Edit menu actions

        void OnUndo (object o, EventArgs args)
        {
            undo_manager.Undo ();
        }

        void OnRedo (object o, EventArgs args)
        {
            undo_manager.Redo ();
        }

        void OnExtractPages (object o, EventArgs args)
        {
            var to_doc = new PdfDocument ();
            var from_doc = PdfSharp.Pdf.IO.PdfReader.Open (new Uri (app.Document.CurrentStateUri).LocalPath, PdfDocumentOpenMode.Import, null);
            var pages = app.IconView.SelectedPages.ToList ();

            foreach (var index in pages.Select (p => p.Index)) {
                to_doc.AddPage (from_doc.Pages[index]);
            }

            var path = Client.GetTmpFilename ();
            to_doc.Save (path);
            to_doc.Dispose ();

            app.LoadPath (path, Path.Combine (
                Path.GetDirectoryName (app.Document.SuggestedSavePath),
                String.Format ("{0} [{1}].pdf",
                    Path.GetFileNameWithoutExtension (app.Document.SuggestedSavePath),
                    GLib.Markup.EscapeText (Document.GetPageSummary (pages, 10)))
            ));
        }

        void OnRemove (object o, EventArgs args)
        {
            var action = new RemoveAction (app.Document, app.IconView.SelectedPages);
            action.Do ();
            // Undo isn't working yet
            //undo_manager.AddUndoAction (action);
        }

        void OnRotateLeft (object o, EventArgs args)
        {
            Rotate (-90);
        }

        void OnRotateRight (object o, EventArgs args)
        {
            Rotate (90);
        }

        void OnSelectAll (object o, EventArgs args)
        {
            app.IconView.SetPageSelectionMode (PageSelectionMode.All);
        }

        void OnSelectOdds (object o, EventArgs args)
        {
            app.IconView.SetPageSelectionMode (PageSelectionMode.Odds);
        }

        void OnSelectEvens (object o, EventArgs args)
        {
            app.IconView.SetPageSelectionMode (PageSelectionMode.Evens);
        }

        void OnSelectMatching (object o, EventArgs args)
        {
            app.ToggleMatchQuery ();
        }

        void OnSelectInverse (object o, EventArgs args)
        {
            app.IconView.SetPageSelectionMode (PageSelectionMode.Inverse);
        }

        // View menu actions

        void OnZoomIn (object o, EventArgs args)
        {
            app.IconView.Zoom (10);
        }

        void OnZoomOut (object o, EventArgs args)
        {
            app.IconView.Zoom (-10);
        }

        void OnZoomFit (object o, EventArgs args)
        {
            app.IconView.ZoomFit ();
        }

        void OnOpenInViewer (object o, EventArgs args)
        {
            System.Diagnostics.Process.Start (app.Document.CurrentStateUri);
        }

        void OnFullScreenView (object o, EventArgs args)
        {
            bool fullscreen = (this["FullScreenView"] as ToggleAction).Active;

            if (fullscreen) {
                app.Window.Fullscreen ();
            } else {
                app.Window.Unfullscreen ();
            }
        }

        void OnViewToolbar (object o, EventArgs args)
        {
            bool show = (this["ViewToolbar"] as ToggleAction).Active;
            Client.Configuration.ShowToolbar = app.HeaderToolbar.Visible = show;
        }

        void OnViewBookmarks (object o, EventArgs args)
        {
            bool show = (this["ViewBookmarks"] as ToggleAction).Active;
            Client.Configuration.ShowBookmarks = app.BookmarkView.Visible = show;
            if (app.BookmarkView.Visible) {
                app.BookmarkView.GrabFocus ();
            }
        }

        // Help menu actions

        void OnHelp (object o, EventArgs args)
        {
            bool shown = false;
            try {
                IntPtr lang_ptr = g_get_language_names ();
                var langs = GLib.Marshaller.NullTermPtrToStringArray (lang_ptr, false);

                string help_dir = null;
                foreach (var dir in new string [] { Core.Defines.PREFIX + "/share/gnome/help/", "/usr/local/share/gnome/help/", "docs/" }) {
                    help_dir = dir;
                    if (System.IO.Directory.Exists (dir + "pdfmod/")) {
                        break;
                    }
                }

                foreach (var lang in langs) {
                    var help_path = CombinePaths (help_dir, "pdfmod", lang, "pdfmod.xml");
                    if (System.IO.File.Exists (help_path)) {
                        System.Diagnostics.Process.Start (String.Format ("ghelp://{0}", help_path));
                        shown = true;
                        break;
                    }
                }
            } catch (Exception e) {
                Hyena.Log.Exception ("Error opening help", e);
            }

            if (!shown) {
                var message_dialog = new Hyena.Widgets.HigMessageDialog (
                    app.Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.None,
                    Catalog.GetString ("Error opening help"),
                    Catalog.GetString ("Would you like to open PDF Mod's online documentation?")
                );
                message_dialog.AddButton (Stock.No, ResponseType.No, false);
                message_dialog.AddButton (Stock.Yes, ResponseType.Yes, true);

                var response = (ResponseType) message_dialog.Run ();
                message_dialog.Destroy ();
                if (response == ResponseType.Yes) {
                    System.Diagnostics.Process.Start (DOCS_URL);
                }
            }
        }

        void OnAbout (object o, EventArgs args)
        {
            Gtk.AboutDialog.SetUrlHook ((dlg, link) => { System.Diagnostics.Process.Start (link); });

            var dialog = new Gtk.AboutDialog () {
                ProgramName = "PDF Mod",
                Version = Core.Defines.VERSION,
                Website = WIKI_URL,
                WebsiteLabel = Catalog.GetString ("Visit Website"),
                Authors = new string [] {
                    Catalog.GetString ("Primary Development:"),
                    "\tGabriel Burt",
                    "",
                    Catalog.GetString ("Contributors:"),
                    "\tSandy Armstrong",
                    "\tAaron Bockover",
                    "\tOlivier Le Thanh Duong",
                    "\tJulien Rebetez",
                    "\tIgor Vatavuk",
                    "\tBertrand Lorentz",
                    "\tMichael McKinley",
                    "\tŁukasz Jernaś",
                    "\tRomain Tartière",
                    "\tRobert Dyer",
                    "\tAndreu Correa Casablanca"
                },
                Documenters = new string [] { "Gabriel Burt" },
                Artists = new string [] { "Kalle Persson" },
                Copyright = String.Format (
                    // Translators: {0} and {1} are the years the copyright assertion covers; put into
                    // variables so you don't have to re-translate this every year
                    Catalog.GetString ("Copyright {0} Novell Inc.\nCopyright {1} Other PDF Mod Contributors"),
                    "2009-2010", "2009"
                ),
                TranslatorCredits = Catalog.GetString ("translator-credits")
            };

            try {
                dialog.Logo = Gtk.IconTheme.Default.LoadIcon ("pdfmod", 256, 0);
            } catch {}

            string [] license_paths = new string [] {
                Core.Defines.PREFIX + "/share/doc/packages/pdfmod/COPYING",
                "/usr/local/share/doc/packages/pdfmod/COPYING",
                "COPYING",
                "../COPYING"
            };

            foreach (var path in license_paths) {
                try {
                    dialog.License = System.IO.File.ReadAllText (path);
                    break;
                } catch {}
            }

            dialog.Run ();
            dialog.Destroy ();
        }

        void OnPageContextMenu (object o, EventArgs args)
        {
            ShowContextMenu ("/PageContextMenu");
        }

        // Bookmark actions

        void OnEditBookmarks (object o, EventArgs args)
        {
            if (!app.BookmarkView.Visible) {
                (this["ViewBookmarks"] as ToggleAction).Active = true;
            }
        }

        void OnBookmarkContextMenu (object o, EventArgs args)
        {
            ShowContextMenu ("/BookmarkContextMenu");
        }

#endregion

#region Utility methods

        void Rotate (int degrees)
        {
            if (!(app.Window.Focus is Gtk.Entry)) {
                var action = new RotateAction (app.Document, app.IconView.SelectedPages, degrees);
                action.Do ();
                undo_manager.AddUndoAction (action);
            }
        }

        string CombinePaths (params string [] parts)
        {
            string path = parts[0];
            for (int i = 1; i < parts.Length; i++) {
                path = System.IO.Path.Combine (path, parts[i]);
            }
            return path;
        }

        [DllImport ("glib-2.0.dll")]
        static extern IntPtr g_get_language_names ();

#endregion

    }

    public static class ActionExtensions
    {
        public static Hyena.Widgets.ImageButton CreateImageButton (this Gtk.Action action)
        {
            var button = new Hyena.Widgets.ImageButton (action.ShortLabel, action.IconName);
            action.ConnectProxy (button);
            return button;
        }
    }
}
