// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Igor Vatavuk
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

using Mono.Unix;
using Gtk;

namespace PdfMod.Gui
{
    public class QueryBox : EventBox
    {
        private Client app;
        private HBox hbox;
        public Entry Entry { get; private set; }

        public QueryBox (Client app)
        {
            this.app = app;
            AppPaintable = true;
            hbox = new HBox ();
            hbox.BorderWidth = 6;

            Entry = new Gtk.Entry ();
            Entry.WidthChars = 40;
            Entry.Activated += OnActivated;

            var query_button = new Hyena.Widgets.ImageButton (Catalog.GetString ("Select Matching"), Gtk.Stock.Find);
            query_button.Clicked += OnActivated;

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
