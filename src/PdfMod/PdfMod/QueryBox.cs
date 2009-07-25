
using System;

using Mono.Unix;
using Gtk;

namespace PdfMod
{
    public class QueryBox : EventBox
    {
        private PdfMod app;
        private HBox hbox;
        public Entry Entry { get; private set; }

        public QueryBox (PdfMod app)
        {
            this.app = app;
            AppPaintable = true;
            hbox = new HBox ();
            hbox.BorderWidth = 6;

            Entry = new Gtk.Entry ();
            Entry.WidthChars = 40;
            Entry.Activated += OnActivated;

            var query_button = new Hyena.Widgets.ImageButton (Catalog.GetString ("Select Matching"), Gtk.Stock.Find);
            query_button.Activated += OnActivated;

            var close_button = new Hyena.Widgets.ImageButton (null, Gtk.Stock.Close);
            close_button.Clicked += delegate {
                Hide ();
            };

            hbox.PackStart (Entry, true, true, 0);
            hbox.PackStart (query_button, false, false, 0);
            hbox.PackStart (close_button, false, false, 0);
            Child = hbox;

            KeyPressEvent += delegate (object o, KeyPressEventArgs args) {
                if (args.Event.Key == Gdk.Key.Escape) {
                    Hide ();
                }
            };

            ShowAll ();
        }

        public new void Hide ()
        {
            base.Hide ();
            app.IconView.GrabFocus ();
        }

        private void OnActivated (object o, EventArgs args)
        {
            Hide ();
            if (!String.IsNullOrEmpty (Entry.Text.Trim ())) {
                app.IconView.SetSelectionMatchQuery (Entry.Text);
            }
        }

        private bool changing_style;
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
        }
    }
}
