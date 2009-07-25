
using System;

using Mono.Unix;
using Gtk;

namespace PdfMod
{
    public class MetadataEditorBox : EventBox
    {
        private PdfMod app;
        private VBox vbox;

        private Entry title_entry;

        public MetadataEditorBox (PdfMod app)
        {
            this.app = app;
            app.DocumentLoaded += HandleDocumentLoaded;

            vbox = new VBox ();
            vbox.BorderWidth = 6;
            Child = vbox;

            BuildEditor ();
            BuildButtons ();

            KeyPressEvent += delegate (object o, KeyPressEventArgs args) {
                if (args.Event.Key == Gdk.Key.Escape) {
                    Hide ();
                }
            };

            UpdateSensitivity ();
            ShowAll ();
        }

        private void BuildEditor ()
        {
            var editor_box = new HBox ();
            vbox.PackStart (editor_box, false, false, 0);

            title_entry = new Entry ();
            //Hyena.Gui.EditableUndoAdapter.
            editor_box.PackStart (title_entry, false, false, 0);
        }

        private void BuildButtons ()
        {
            var revert_button = new Hyena.Widgets.ImageButton (Catalog.GetString ("Revert Properties"), "revert");
            revert_button.Activated += HandleRevert;

            var close_button = new Hyena.Widgets.ImageButton (null, Gtk.Stock.Close);
            close_button.TooltipText = Catalog.GetString ("Hide the document's properties");
            close_button.Clicked += delegate {
                Hide ();
            };

            var button_box = new HBox ();
            vbox.PackStart (button_box, false, true, 0);

            button_box.PackEnd (close_button, false, false, 0);
            button_box.PackEnd (revert_button, false, false, 0);
        }

        private void UpdateSensitivity ()
        {
            Sensitive = app.Document != null;
        }

        #region Event handlers

        private void HandleDocumentLoaded(object sender, EventArgs e)
        {
            UpdateSensitivity ();
        }

        private void HandleRevert (object o, EventArgs args)
        {
        }

        #endregion

        #region Gtk.Widget overrides

        public new void Hide ()
        {
            (app.GlobalActions["PropertiesAction"] as Gtk.ToggleAction).Active = false;
            base.Hide ();
            app.IconView.GrabFocus ();
        }

        /*private bool changing_style;
        protected override void OnStyleSet (Style style)
        {
            if (!changing_style) {
                changing_style = true;
                ModifyBg (StateType.Normal, Style.Background (StateType.Selected));
                changing_style = false;
            }
        }

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            GdkWindow.DrawRectangle (Style.ForegroundGC (StateType.Normal), false, 0, 0, Allocation.Width - 1, Allocation.Height - 1);
            return base.OnExposeEvent (evnt);
        }*/

        #endregion
    }
}
