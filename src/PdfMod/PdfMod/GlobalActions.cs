
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

using PdfMod.Actions;

namespace PdfMod
{
    public class GlobalActions : HyenaActionGroup
    {
        private PdfMod app;
        private UndoManager undo_manager;
        private const string WIKI_URL = "http://live.gnome.org/PdfMod";

        private static string [] require_doc_actions = new string[] {
            "SaveAction", "SaveAsAction", "PropertiesAction", "UndoAction", "RedoAction", "ZoomFitAction",
            "SelectAllAction", "SelectEvensAction", "SelectOddsAction", "SelectMatchingAction", "InsertFromAction"
        };

        private static string [] require_page_actions = new string[] {
            "RemoveAction", "ExtractAction", "RotateRightAction", "RotateLeftAction"
            //, "ExportImagesAction"
        };

        public UndoManager UndoManager {
            get { return undo_manager; }
        }

        public GlobalActions (PdfMod app, ActionManager action_manager) : base (action_manager, "Global")
        {
            this.app = app;
            undo_manager = new UndoManager ();

            AddImportant (
                new ActionEntry ("OpenAction",   Gtk.Stock.Open,   null, "<control>O", Catalog.GetString ("Open a document"), OnOpen),
                new ActionEntry ("InsertFromAction", Gtk.Stock.Add, Catalog.GetString("_Insert From..."), null, Catalog.GetString("Insert pages from another document"), OnInsertFrom),
                new ActionEntry ("SaveAction",   Gtk.Stock.Save,   null, "<control>S", Catalog.GetString ("Save changes to this document, overwriting the existing file"), OnSave),
                new ActionEntry ("SaveAsAction", Gtk.Stock.SaveAs, null, "<control><shift>S", Catalog.GetString ("Save this document to a new file"), OnSaveAs),

                new ActionEntry ("FileMenuAction", null, Catalog.GetString ("_File"), null, null, null),
                new ActionEntry ("RecentMenuAction", null, Catalog.GetString ("Recent _Files"), null, null, null),
                new ActionEntry ("CloseAction", Gtk.Stock.Close, null, "<control>W", null, OnClose),
                new ActionEntry ("RemoveAction", Gtk.Stock.Remove, null, "Delete", null, OnRemove),
                new ActionEntry ("ExtractAction", Gtk.Stock.New, null, null, null, OnExtractPages),
                new ActionEntry ("RotateRightAction", null, Catalog.GetString ("Rotate Right"), "bracketright", Catalog.GetString ("Rotate right"), OnRotateRight),
                new ActionEntry ("RotateLeftAction", null, Catalog.GetString ("Rotate Left"), "bracketleft", Catalog.GetString ("Rotate left"), OnRotateLeft),
                new ActionEntry ("ExportImagesAction", null, Catalog.GetString ("Export Images..."), null, null, OnExportImages),

                new ActionEntry ("EditMenuAction", null, Catalog.GetString ("_Edit"), null, null, null),
                new ActionEntry ("SelectAllAction", Stock.SelectAll, null, "<control>A", null, OnSelectAll),
                new ActionEntry ("SelectEvensAction", null, Catalog.GetString ("Select Even Pages"), null, null, OnSelectEvens),
                new ActionEntry ("SelectOddsAction", null, Catalog.GetString ("Select Odd Pages"), null, null, OnSelectOdds),
                new ActionEntry ("SelectMatchingAction", null, Catalog.GetString ("Select Matching..."), "<control>F", null, OnSelectMatching),
                new ActionEntry ("UndoAction", Stock.Undo, null, "<control>z", null, OnUndo),
                new ActionEntry ("RedoAction", Stock.Redo, null, "<control>y", null, OnRedo),

                new ActionEntry ("ViewMenuAction", null, Catalog.GetString ("_View"), null, null, null),
                new ActionEntry ("ZoomInAction", Stock.ZoomIn, null, "<control>plus", null, OnZoomIn),
                new ActionEntry ("ZoomOutAction", Stock.ZoomOut, null, "<control>minus", null, OnZoomOut),

                new ActionEntry ("HelpMenuAction", null, Catalog.GetString ("_Help"), null, null, null),
                new ActionEntry ("HelpAction", Stock.Help, Catalog.GetString ("_Contents"), "F1", null, OnHelp),
                new ActionEntry ("AboutAction", Stock.About, null, null, null, OnAbout),

                new ActionEntry ("PageContextMenuAction", null, "", null, null, OnPageContextMenu)
            );

            AddImportant (
                new ToggleActionEntry ("PropertiesAction", Stock.Properties, null, "<alt>Return", Catalog.GetString ("View and edit the title, keywords, and more for this document"), OnProperties, false),
                new ToggleActionEntry ("ZoomFitAction", Stock.ZoomFit, null, "<control>0", null, OnZoomFit, true),
                new ToggleActionEntry ("ViewToolbar", null, Catalog.GetString ("Toolbar"), null, null, OnViewToolbar, true)
            );

            // Not ready/finished yet
            UpdateAction ("ExportImagesAction", false);

            this["RotateRightAction"].IconName = "object-rotate-right";
            this["RotateLeftAction"].IconName = "object-rotate-left";

            // Don't show HelpAction unless we can at least access some of the GNOME api
            UpdateAction ("HelpAction", false);
            try {
                Gnome.Program.Get ();
                UpdateAction ("HelpAction", true);
            } catch {}

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
            var item = ActionManager.UIManager.GetWidget ("/MainMenu/ViewMenu/ZoomInAction");
            item.AddAccelerator ("activate", ActionManager.UIManager.AccelGroup, (uint) Gdk.Key.KP_Add, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);
            item.AddAccelerator ("activate", ActionManager.UIManager.AccelGroup, (uint) Gdk.Key.equal, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);

            item = ActionManager.UIManager.GetWidget ("/MainMenu/ViewMenu/ZoomOutAction");
            item.AddAccelerator ("activate", ActionManager.UIManager.AccelGroup, (uint) Gdk.Key.KP_Subtract, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);
            item.AddAccelerator ("activate", ActionManager.UIManager.AccelGroup, (uint) Gdk.Key.underscore, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);

            item = ActionManager.UIManager.GetWidget ("/MainMenu/FileMenu/CloseAction");
            item.AddAccelerator ("activate", ActionManager.UIManager.AccelGroup, (uint) Gdk.Key.q, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);

            // Set up recent documents menu
            MenuItem recent_item = ActionManager.UIManager.GetWidget ("/MainMenu/FileMenu/RecentMenuAction") as MenuItem;
            var recent_chooser_item = new RecentChooserMenu (RecentManager.Default) {
                Filter = new RecentFilter (),
                SortType = RecentSortType.Mru
            };
            recent_chooser_item.Filter.AddPattern ("*.pdf");
            recent_chooser_item.ItemActivated += delegate {
                PdfMod.RunIdle (delegate { app.LoadPath (recent_chooser_item.CurrentUri); });
            };
            recent_item.Submenu = recent_chooser_item;
        }

        private void OnChanged (object o, EventArgs args)
        {
            Update ();
        }

        private void Update ()
        {
            bool have_doc = app.Document != null;
            foreach (string action in require_doc_actions)
                UpdateAction (action, true, have_doc);

            bool have_page = have_doc && app.IconView.SelectedItems.Length > 0;
            foreach (string action in require_page_actions)
                UpdateAction (action, true, have_page);

            UpdateAction ("UndoAction", true, have_doc && undo_manager.CanUndo);
            UpdateAction ("RedoAction", true, have_doc && undo_manager.CanRedo);
            UpdateActions (true, have_doc && app.Document.HasUnsavedChanged, "SaveAction", "SaveAsAction");
            UpdateAction ("ZoomInAction", true, have_doc && app.IconView.CanZoomIn);
            UpdateAction ("ZoomOutAction", true, have_doc && app.IconView.CanZoomOut);

            int selection_count = app.IconView.SelectedItems.Length;
            this["RemoveAction"].Label = String.Format (Catalog.GetPluralString (
                "Remove Page", "Remove {0} Pages", selection_count),
                selection_count);
            this["RemoveAction"].Tooltip = String.Format (Catalog.GetPluralString (
                "Remove the selected page", "Remove the {0} selected pages", selection_count),
                selection_count);
            this["ExtractAction"].Label = String.Format (Catalog.GetPluralString (
                "Extract Page", "Extract {0} Pages", selection_count),
                selection_count);
            this["ExtractAction"].Tooltip = String.Format (Catalog.GetPluralString (
                "Extract the selected page", "Extract the {0} selected pages", selection_count),
                selection_count);
        }

#region Action Handlers

        private void OnOpen (object o, EventArgs args)
        {
            var chooser = new Gtk.FileChooserDialog (Catalog.GetString ("Select PDF"), app.Window, FileChooserAction.Open);
            chooser.AddFilter (GtkUtilities.GetFileFilter ("PDF Documents", new string [] {"pdf"}));
            chooser.AddFilter (GtkUtilities.GetFileFilter (Catalog.GetString ("All Files"), new string [] {"*"}));
            chooser.SelectMultiple = false;
            chooser.AddButton (Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton (Stock.Open, ResponseType.Ok);
            chooser.DefaultResponse = ResponseType.Ok;

            var response = chooser.Run ();
            string filename = chooser.Filename;
            chooser.Destroy ();

            if (response == (int)ResponseType.Ok) {
                PdfMod.RunIdle (delegate { app.LoadPath (filename); });
            }
        }

        private void OnInsertFrom (object o, EventArgs args)
        {
            var chooser = new Gtk.FileChooserDialog (Catalog.GetString ("Select PDF"), app.Window, FileChooserAction.Open);
            chooser.AddFilter (GtkUtilities.GetFileFilter ("PDF Documents", new string [] {"pdf"}));
            chooser.AddFilter (GtkUtilities.GetFileFilter (Catalog.GetString ("All Files"), new string [] {"*"}));
            chooser.SelectMultiple = false;
            chooser.AddButton (Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton (Stock.Open, ResponseType.Ok);
            chooser.DefaultResponse = ResponseType.Ok;

            var response = chooser.Run ();
            string filename = chooser.Filename;
            chooser.Destroy();

            if (response == (int)ResponseType.Ok) {
                app.Document.AddFromUri (new Uri (filename));
            }
        }

        private void OnSave (object o, EventArgs args)
        {
            app.Document.Save (app.Document.SuggestedSavePath);
            undo_manager.Clear ();
        }

        private void OnSaveAs (object o, EventArgs args)
        {
            var chooser = new Gtk.FileChooserDialog (Catalog.GetString ("Save as..."), app.Window, FileChooserAction.Save);
            chooser.SelectMultiple = false;
            chooser.DoOverwriteConfirmation = true;
            chooser.AddButton (Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton (Stock.SaveAs, ResponseType.Ok);
            chooser.AddFilter (GtkUtilities.GetFileFilter ("PDF Documents", new string [] {"pdf"}));
            chooser.AddFilter (GtkUtilities.GetFileFilter (Catalog.GetString ("All Files"), new string [] {"*"}));
            chooser.SetCurrentFolder (System.IO.Path.GetDirectoryName (app.Document.SuggestedSavePath));
            chooser.CurrentName = System.IO.Path.GetFileName (app.Document.SuggestedSavePath);
            chooser.DefaultResponse = ResponseType.Ok;

            var response = chooser.Run ();
            string filename = chooser.Filename;
            chooser.Destroy ();

            if (response == (int)ResponseType.Ok) {
                Log.DebugFormat ("Saving {0} to {1}", app.Document.Uri, filename);
                app.Document.Save (filename);
                undo_manager.Clear ();
            }
        }

        private void OnProperties (object o, EventArgs args)
        {
            app.EditorBox.Visible = (this["PropertiesAction"] as ToggleAction).Active;
            if (app.EditorBox.Visible) {
                app.EditorBox.GrabFocus ();
            }
        }

        private void OnRemove (object o, EventArgs args)
        {
            var action = new RemoveAction (app.Document, app.IconView.SelectedPages);
            action.Do ();
            // Undo isn't working yet
            //undo_manager.AddUndoAction (action);
        }

        private void OnExtractPages (object o, EventArgs args)
        {
            var doc = new PdfDocument ();
            var pages = new List<Page> (app.IconView.SelectedPages);
            foreach (var page in pages) {
                doc.AddPage (page.Pdf);
            }

            var path = PdfMod.GetTmpFilename ();
            doc.Save (path);
            doc.Dispose ();

            app.LoadPath (path, Path.Combine (
                Path.GetDirectoryName (app.Document.SuggestedSavePath),
                String.Format ("[{0}] {1}",
                    GLib.Markup.EscapeText (GetPageSummary (pages, 10)),
                    Path.GetFileName (app.Document.SuggestedSavePath))
            ));
        }

        // Return a simple, nice string describing the selected pages
        //   e.g.  Page 1, or Page 3 - 6, or Page 2, 4, 6
        public static string GetPageSummary (List<Page> pages, int maxListed)
        {
            string pages_summary = null;
            if (pages.Count == 1) {
                // Translators: {0} is the number of pages (always 1), and {1} is the page number, eg Page 1, or Page 5
                pages_summary = String.Format (Catalog.GetPluralString ("Page {1}", "Page {1}", pages.Count), pages.Count, pages[0].Index + 1);
            } else if (pages[0].Index + pages.Count - 1 == pages[pages.Count - 1].Index) {
                // Translators: {0} is the number of pages, and {1} is the first page, {2} is the last page,
                // eg Pages 3 - 7
                pages_summary = String.Format (Catalog.GetPluralString ("Pages {1} - {2}", "Pages {1} - {2}", pages.Count),
                    pages.Count, pages[0].Index + 1, pages[pages.Count - 1].Index + 1);
            } else if (pages.Count < maxListed) {
                string page_nums = String.Join (", ", pages.Select (p => (p.Index + 1).ToString ()).ToArray ());
                // Translators: {0} is the number of pages, {1} is a comma separated list of page numbers, eg Pages 1, 4, 9
                pages_summary = String.Format (Catalog.GetPluralString ("Pages {1}", "Pages {1}", pages.Count), pages.Count, page_nums);
            } else {
                // Translators: {0} is the number of pages, eg 12 Pages
                pages_summary = String.Format (Catalog.GetPluralString ("{0} Page", "{0} Pages", pages.Count), pages.Count);
            }
            return pages_summary;
        }

        private void OnExportImages (object o, EventArgs args)
        {
            var pages = app.IconView.SelectedPages.ToList ();
            var action = new ExportImagesAction (app.Document, pages);
            if (action.ExportableImageCount == 0) {
                Log.Information ("Found zero exportable images in the selected pages");
                return;
            }

            var export_path_base = Path.Combine (
                Path.GetDirectoryName (app.Document.SuggestedSavePath),
                // Translators: This is used for creating a folder name, be careful!
                String.Format (Catalog.GetString ("{0} - Images for {1}"), app.Document.Title ?? app.Document.Filename, GetPageSummary (pages, 10))
            );

            var export_path = export_path_base;
            int i = 1;
            while (Directory.Exists (export_path)) {
                export_path = String.Format ("{0} ({1})", export_path_base, i++);
            }

            Directory.CreateDirectory (export_path);

            action.Do (export_path);
            System.Diagnostics.Process.Start (export_path);
        }

        private void OnUndo (object o, EventArgs args)
        {
            undo_manager.Undo ();
        }

        private void OnRedo (object o, EventArgs args)
        {
            undo_manager.Redo ();
        }

        [DllImport ("glib-2.0.dll")]
        private static extern IntPtr g_get_language_names ();

        private string CombinePaths (params string [] parts)
        {
            string path = parts[0];
            for (int i = 1; i < parts.Length; i++) {
                path = System.IO.Path.Combine (path, parts[i]);
            }
            return path;
        }

        private void OnHelp (object o, EventArgs args)
        {
            bool shown = false;
            try {
                IntPtr lang_ptr = g_get_language_names ();
                var langs = GLib.Marshaller.NullTermPtrToStringArray (lang_ptr, false);

                string help_dir = null;
                foreach (var dir in new string [] { "/usr/share/gnome/help/", "/usr/local/share/gnome/help/", "docs/" }) {
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
                    System.Diagnostics.Process.Start (WIKI_URL);
                }
            }
        }

        private void OnAbout (object o, EventArgs args)
        {
            Gtk.AboutDialog.SetUrlHook ((dlg, link) => { System.Diagnostics.Process.Start (link); });

            var dialog = new Gtk.AboutDialog () {
                ProgramName = "PDF Mod",
                Version = "0.3",
                Website = WIKI_URL,
                WebsiteLabel = Catalog.GetString ("Visit Website"),
                Authors = new string [] {
                    "Gabriel Burt", "",
                    "Contributions from:",
                    " • Sandy Armstrong",
                    " • Aaron Bockover",
                    " • Olivier Le Thanh Duong",
                    " • Julien Rebetez"
                },
                Documenters = new string [] { "Gabriel Burt" },
                Artists = new string [] { "Kalle Persson" },
                Copyright = "Copyright 2009 Novell Inc.",
                TranslatorCredits = Catalog.GetString ("translator-credits")
            };

            try {
                dialog.Logo = Gtk.IconTheme.Default.LoadIcon ("pdfmod", 256, 0);
            } catch {}

            string [] license_paths = new string [] {
                "/usr/share/doc/packages/pdfmod/COPYING",
                "/usr/local/share/doc/packages/pdfmod/COPYING",
                "COPYING"
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

        private void OnPageContextMenu (object o, EventArgs args)
        {
            ShowContextMenu ("/PageContextMenu");
        }

        private void OnSelectAll (object o, EventArgs args)
        {
            app.IconView.SetPageSelectionMode (PageSelectionMode.All);
        }

        private void OnSelectEvens (object o, EventArgs args)
        {
            app.IconView.SetPageSelectionMode (PageSelectionMode.Evens);
        }

        private void OnSelectOdds (object o, EventArgs args)
        {
            app.IconView.SetPageSelectionMode (PageSelectionMode.Odds);
        }

        private void OnSelectMatching (object o, EventArgs args)
        {
            app.ToggleMatchQuery ();
        }

        private void OnZoomIn (object o, EventArgs args)
        {
            app.IconView.Zoom (10);
        }

        private void OnZoomOut (object o, EventArgs args)
        {
            app.IconView.Zoom (-10);
        }

        private void OnZoomFit (object o, EventArgs args)
        {
            app.IconView.ZoomFit ();
        }

        private void OnViewToolbar (object o, EventArgs args)
        {
            app.HeaderToolbar.Visible = (this["ViewToolbar"] as ToggleAction).Active;
        }

        private void OnRotateRight (object o, EventArgs args)
        {
            Rotate (90);
        }

        private void OnRotateLeft (object o, EventArgs args)
        {
            Rotate (-90);
        }

        private void Rotate (int degrees)
        {
            if (!(app.Window.Focus is Gtk.Entry)) {
                var action = new RotateAction (app.Document, app.IconView.SelectedPages, degrees);
                action.Do ();
                undo_manager.AddUndoAction (action);
            }
        }

        private void OnClose (object o, EventArgs args)
        {
            app.Quit ();
        }

#endregion

    }
}
