// Copyright (C) 2009 Novell, Inc.
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

using Gtk;
using Cairo;

using Hyena.Gui;
using Hyena.Gui.Theming;

namespace PdfMod.Gui
{
    public abstract class CairoCell : CellRenderer
    {
        protected Theme Theme { get; private set; }

        public CairoCell ()
        {
            Mode = CellRendererMode.Inert;
            Xpad = Ypad = 0;
        }

        protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState state)
        {
            if (Theme == null) {
                Theme = Hyena.Gui.Theming.ThemeEngine.CreateTheme (widget);
            }

            Context cr = Gdk.CairoHelper.Create (window);

            // Setup the drawing boundaries/positioning
            cr.Rectangle (cell_area.X, cell_area.Y, cell_area.Width, cell_area.Height);
            cr.Clip ();
            cr.Translate (cell_area.X, cell_area.Y);

            if (state == CellRendererState.Selected) {
                Theme.DrawRowSelection (cr, 0, 0, cell_area.Width, cell_area.Height, true);
            } else if (state == CellRendererState.Focused) {
                Theme.DrawRowSelection (cr, 0, 0, cell_area.Width, cell_area.Height, false);
            }

            var border = Theme.TotalBorderWidth;
            cr.Translate (border, border);
            var width = cell_area.Width - 2 * border;
            var height = cell_area.Height - 2 * border;

            Render (cr, width, height, state);

            Hyena.Gui.CairoExtensions.DisposeContext (cr);
        }

        protected abstract void Render (Cairo.Context context, double width, double height, CellRendererState state);
    }
}
