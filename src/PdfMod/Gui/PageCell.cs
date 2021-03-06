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
using System.Linq;

using Gtk;
using Cairo;

using Hyena;
using Hyena.Collections;

using PdfMod.Pdf;

namespace PdfMod.Gui
{
    public class PageCell : CairoCell, IDisposable
    {
        const int scale_every = 400;

        ThumbnailLruCache surface_cache;

        public PageCell (IconView parent)
        {
            surface_cache = new ThumbnailLruCache ();
        }

        [GLib.Property ("page")]
        public Page Page { get; set; }

        public override void GetSize (Gtk.Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
        {
            x_offset = y_offset = 0;
            width = height = ItemSize;
        }

        public override void Dispose ()
        {
            var keys = surface_cache.Select (e => e.Key).ToList ();
            foreach (var key in keys) {
                surface_cache.Remove (key);
            }

            base.Dispose ();
        }

        int item_size = DocumentIconView.MIN_WIDTH;
        public int ItemSize {
            get { return item_size; }
            set {
                item_size = value;
                SetFixedSize (value, value);
            }
        }

        protected override void Render (Cairo.Context cr, double width, double height, CellRendererState state)
        {
            // Scale the border size w/ the cell size
            var border_width = width / 128.0;
            width -= 2 * border_width;
            height -= 2 * border_width;
            if (width < 0 || height < 0) {
                return;
            }

            //Console.WriteLine ("SurfaceCache has HitRatio = {0:0.00} ({1} hits, {2} misses, max_count = {3})", surface_cache.HitRatio, surface_cache.Hits, surface_cache.Misses, surface_cache.MaxCount);
            PageThumbnail cache_obj;
            ImageSurface surface = null;
            if (surface_cache.TryGetValue (Page, out cache_obj)) {
                // Don't use if not big enough, dirty, or corrupt
                surface = cache_obj.Surface;
                if (Page.SurfaceDirty || surface == null || surface.Handle == IntPtr.Zero || (surface.Width < width && surface.Height < height)) {
                    surface_cache.Remove (Page);
                    cache_obj = null;
                    surface = null;
                }
            }

            if (surface == null) {
                // Create a new thumbnail surface, but only on 200px boundaries, then we scale down if needed
                var w = width + (width % scale_every);
                var h = height + (height % scale_every);
                cache_obj = Page.Document.GetSurface (Page, (int)w, (int)h, DocumentIconView.MIN_WIDTH);
                if (cache_obj == null) {
                    return;
                }
                surface = cache_obj.Surface;

                // Put it in the cache
                surface_cache.Add (Page, cache_obj);
            }

            double scale = Math.Min (width / (double)surface.Width, height / (double)surface.Height);
            double doc_width = scale * surface.Width;
            double doc_height = scale * surface.Height;

            // Center the thumbnail if either dimension is smaller than the cell's
            if (doc_width < width || doc_height < height) {
                var x = ((width - doc_width) / 2.0) - border_width;
                var y = ((height - doc_height) / 2.0) - border_width;
                cr.Translate (x, y);
            }

            PaintDocumentBorder (cr, doc_width, doc_height, border_width);

            // Scale down the surface if it's not exactly the right size
            if (scale < 1) {
                cr.Scale (scale, scale);
            }

            // Paint the scaled/translated thumbnail
            cr.SetSource (surface);
            cr.Paint ();
        }

        void PaintDocumentBorder (Context cr, double doc_width, double doc_height, double border_width)
        {
            // Paint a nice document-like border around it
            var thin = 0.25 * border_width;
            var thick = 0.75 * border_width;

            cr.Rectangle (thick + thin/2, thick + thin/2, doc_width, doc_height);
            cr.Color = new Color (1, 1, 1);
            cr.FillPreserve ();
            cr.Color = new Color (0, 0, 0);
            cr.LineWidth = thin;
            cr.Stroke ();

            var offset = .02 * doc_width;
            cr.LineWidth = thick;
            cr.MoveTo (doc_width + 1.5*thick + thin, offset);
            cr.LineTo (doc_width + 1.5*thick + thin, doc_height + 1.5*thick + 2*thin);
            cr.Stroke ();

            cr.LineWidth = thick;
            cr.MoveTo (offset,                       doc_height + 1.5*thick + thin);
            cr.LineTo (doc_width + 1.5*thick + 2*thin, doc_height + 1.5*thick + thin);
            cr.Stroke ();

            cr.Translate (border_width, border_width);
        }

        class ThumbnailLruCache : LruCache<Page, PageThumbnail>
        {
            public ThumbnailLruCache () : base (60, 0.8)
            {
            }

            protected override void ExpireItem (PageThumbnail thumb)
            {
                thumb.Dispose ();
            }
        }
    }
}
