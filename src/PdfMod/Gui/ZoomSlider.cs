// Copyright (C) 2010 Novell, Inc.
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
using System.Linq;
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
    public class ZoomSlider : Alignment
    {
        public ZoomSlider (Client app) : base (1f, 0.5f, 0f, 0f)
        {
            RightPadding = 16;

            var box = new HBox () { Spacing = 2 };

            // Zoom in/out buttons
            var zoom_out = new Button (new Image (Stock.ZoomOut, IconSize.Button)) { Relief = ReliefStyle.None };
            app.Actions["ZoomOut"].ConnectProxy (zoom_out);

            var zoom_in  = new Button (new Image (Stock.ZoomIn, IconSize.Button)) { Relief = ReliefStyle.None };
            app.Actions["ZoomIn"].ConnectProxy (zoom_in);

            // Slider
            var slider = new HScale (DocumentIconView.MIN_WIDTH, DocumentIconView.MAX_WIDTH, 1) {
                WidthRequest = 100,
                DrawValue = false,
                Sensitive = false
            };

            bool setting_via_slider = false;
            slider.ValueChanged += (o, a) => {
                if (!setting_via_slider) {
                    setting_via_slider = true;
                    app.IconView.Zoom ((int)slider.Value, true);
                    setting_via_slider = false;
                }
            };

            app.IconView.ZoomChanged += () => {
                if (!setting_via_slider) {
                    setting_via_slider = true;
                    slider.Value = app.IconView.ItemSize;
                    setting_via_slider = false;
                }
            };

            app.DocumentLoaded += (o, a) => slider.Sensitive = app.Document != null;
            box.PackStart (zoom_out, false, false, 0);
            box.PackStart (slider,   false, false, 0);
            box.PackStart (zoom_in,  false, false, 0);
            Child = box;
        }
    }
}
