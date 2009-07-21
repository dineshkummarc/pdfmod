
using System;

using Mono.Posix;
using Gtk;

using Hyena;
using Hyena.Gui;

namespace PdfMod
{
    public class GlobalActions : HyenaActionGroup
    {
        private PdfMod app;

        private static string [] require_doc_actions = new string[] {
            "SaveAction", "SaveAsAction", "RemoveAction", "UndoAction", "RedoAction"
        };

        private static string [] require_page_actions = new string[] {
            "RemoveAction",
        };

        public GlobalActions (PdfMod app, ActionManager action_manager) : base (action_manager, "Global")
        {
            this.app = app;

            AddImportant (
                new ActionEntry ("OpenAction",   Gtk.Stock.Open,   null, "<control>O", null, OnOpen),
                new ActionEntry ("SaveAction",   Gtk.Stock.Save,   null, "<control>S", null, OnSave),
                new ActionEntry ("SaveAsAction", Gtk.Stock.SaveAs, null, "<control><shift>S", null, OnSaveAs),
                new ActionEntry ("RemoveAction", Gtk.Stock.Remove, null, "Delete", null, OnOpen)
            );

            Add (
                new ActionEntry ("FileMenuAction", null, Catalog.GetString ("_File"), null, null, null),
                new ActionEntry ("CloseAction", Gtk.Stock.Close, null, "<control>W", null, OnClose),

                new ActionEntry ("EditMenuAction", null, Catalog.GetString ("_Edit"), null, null, null),
                new ActionEntry ("UndoAction", Stock.Undo, null, "<control>z", null, OnUndo),
                new ActionEntry ("RedoAction", Stock.Redo, null, "<control>y", null, OnRedo),

                new ActionEntry ("ViewMenuAction", null, Catalog.GetString ("_View"), null, null, null)
            );

            Update ();
            app.IconView.SelectionChanged += OnChanged;
            app.DocumentChanged += OnChanged;
            app.UndoManager.UndoChanged += OnChanged;

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

            UpdateAction ("UndoAction", true, have_doc && app.UndoManager.CanUndo);
            UpdateAction ("RedoAction", true, have_doc && app.UndoManager.CanRedo);
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
            app.Document.Save (app.DocumentUri);
        }

        private void OnSaveAs (object o, EventArgs args)
        {
            var chooser = new Gtk.FileChooserDialog (Catalog.GetString ("Save as..."), app.Window, FileChooserAction.Save);
            chooser.SelectMultiple = false;
            chooser.AddButton (Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton (Stock.SaveAs, ResponseType.Ok);
            chooser.AddFilter (GtkUtilities.GetFileFilter ("PDF Documents", new string [] {"pdf"}));
            chooser.AddFilter (GtkUtilities.GetFileFilter (Catalog.GetString ("All Files"), new string [] {"*"}));
            chooser.SetCurrentFolder (System.IO.Path.GetDirectoryName (app.DocumentUri));
            chooser.CurrentName = System.IO.Path.GetFileName (app.DocumentUri);
            chooser.DefaultResponse = ResponseType.Ok;

            if (chooser.Run () == (int)ResponseType.Ok) {
                string filename = chooser.Filename;
                Log.DebugFormat ("Saving {0} to {1}", app.DocumentUri, filename);
                app.Document.Save (filename);
            }
            
            chooser.Destroy ();
        }

        private void OnUndo (object o, EventArgs args)
        {
            app.UndoManager.Undo ();
        }

        private void OnRedo (object o, EventArgs args)
        {
            app.UndoManager.Redo ();
        }

        private void OnClose (object o, EventArgs args)
        {
            app.Quit ();
        }

#endregion

    }
}
