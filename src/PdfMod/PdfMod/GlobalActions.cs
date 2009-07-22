
using System;
using System.Collections.Generic;

using Mono.Posix;
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

        private static string [] require_doc_actions = new string[] {
            "SaveAction", "SaveAsAction", "UndoAction", "RedoAction", "SelectAllAction", "SelectEvensAction", "SelectOddsAction"
        };

        private static string [] require_page_actions = new string[] {
            "RemoveAction", "ExtractAction"
        };

        public GlobalActions (PdfMod app, ActionManager action_manager) : base (action_manager, "Global")
        {
            this.app = app;
            undo_manager = new UndoManager ();

            AddImportant (
                new ActionEntry ("OpenAction",   Gtk.Stock.Open,   null, "<control>O", Catalog.GetString ("Open a document"), OnOpen),
                new ActionEntry ("SaveAction",   Gtk.Stock.Save,   null, "<control>S", Catalog.GetString ("Save changes to this document, overwriting the existing file"), OnSave),
                new ActionEntry ("SaveAsAction", Gtk.Stock.SaveAs, null, "<control><shift>S", Catalog.GetString ("Save this document to a new file"), OnSaveAs),

                new ActionEntry ("FileMenuAction", null, Catalog.GetString ("_File"), null, null, null),
                new ActionEntry ("CloseAction", Gtk.Stock.Close, null, "<control>W", null, OnClose),
                new ActionEntry ("RemoveAction", Gtk.Stock.Remove, null, "Delete", null, OnRemove),
                new ActionEntry ("ExtractAction", null, "Extract Pages", null, null, OnRemove),

                new ActionEntry ("EditMenuAction", null, Catalog.GetString ("_Edit"), null, null, null),
                new ActionEntry ("SelectAllAction", Stock.SelectAll, null, "<control>A", null, OnSelectAll),
                new ActionEntry ("SelectEvensAction", null, Catalog.GetString ("Select Even Pages"), null, null, OnSelectEvens),
                new ActionEntry ("SelectOddsAction", null, Catalog.GetString ("Select Odd Pages"), null, null, OnSelectOdds),
                new ActionEntry ("UndoAction", Stock.Undo, null, "<control>z", null, OnUndo),
                new ActionEntry ("RedoAction", Stock.Redo, null, "<control>y", null, OnRedo),

                new ActionEntry ("ViewMenuAction", null, Catalog.GetString ("_View"), null, null, null),

                new ActionEntry ("PageContextMenuAction", null, "", null, null, OnPageContextMenu)
            );

            Update ();
            app.IconView.SelectionChanged += OnChanged;
            app.DocumentChanged += OnChanged;
            undo_manager.UndoChanged += OnChanged;

            AddUiFromFile ("UIManager.xml");
            Register ();
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

            bool have_undo = have_doc && undo_manager.CanUndo;
            UpdateActions (true, have_undo, "UndoAction", "SaveAction", "SaveAsAction");
            UpdateAction ("RedoAction", true, have_doc && undo_manager.CanRedo);

            int selection_count = app.IconView.SelectedItems.Length;
            this["RemoveAction"].Label = String.Format (Catalog.GetPluralString (
                "Remove Page", "Remove {0} Pages", selection_count),
                selection_count);
            this["RemoveAction"].Tooltip = String.Format (Catalog.GetPluralString (
                "Remove the selected page", "Remove the {0} selected pages", selection_count),
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

            if (chooser.Run () == (int)ResponseType.Ok) {
                string filename = chooser.Filename;
                Log.DebugFormat ("Opening {0}", filename);
                var new_app = app.Document == null ? app : new PdfMod ();
                PdfMod.RunIdle (delegate { new_app.LoadUri (filename); });
            }
            
            chooser.Destroy ();
        }

        private void OnSave (object o, EventArgs args)
        {
            app.Document.Save (app.Document.Uri);
        }

        private void OnSaveAs (object o, EventArgs args)
        {
            var chooser = new Gtk.FileChooserDialog (Catalog.GetString ("Save as..."), app.Window, FileChooserAction.Save);
            chooser.SelectMultiple = false;
            chooser.AddButton (Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton (Stock.SaveAs, ResponseType.Ok);
            chooser.AddFilter (GtkUtilities.GetFileFilter ("PDF Documents", new string [] {"pdf"}));
            chooser.AddFilter (GtkUtilities.GetFileFilter (Catalog.GetString ("All Files"), new string [] {"*"}));
            chooser.SetCurrentFolder (System.IO.Path.GetDirectoryName (app.Document.Uri));
            chooser.CurrentName = System.IO.Path.GetFileName (app.Document.Uri);
            chooser.DefaultResponse = ResponseType.Ok;

            if (chooser.Run () == (int)ResponseType.Ok) {
                string filename = chooser.Filename;
                Log.DebugFormat ("Saving {0} to {1}", app.Document.Uri, filename);
                app.Document.Save (filename);
            }

            chooser.Destroy ();
        }

        private void OnRemove (object o, EventArgs args)
        {
            undo_manager.AddUndoAction (new RemoveAction (app.Document, app.IconView.SelectedPages));

            var paths = new List<TreePath> (app.IconView.SelectedItems);
            foreach (var path in paths) {
                TreeIter iter;
                app.IconView.Store.GetIter (out iter, path);
                app.IconView.Store.Remove (ref iter);
            }

            app.IconView.Store.Refresh ();
        }

        private void OnUndo (object o, EventArgs args)
        {
            undo_manager.Undo ();
        }

        private void OnRedo (object o, EventArgs args)
        {
            undo_manager.Redo ();
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

        private void OnClose (object o, EventArgs args)
        {
            app.Quit ();
        }

#endregion

    }
}
