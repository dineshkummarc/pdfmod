
using System;

using Gtk;
using Cairo;

using Hyena;
using Hyena.Collections;

namespace PdfMod
{
    public class CellRendererPage : CellRendererCairo
    {
        const int scale_every = 200;

        private LruCache<Page, Page.Thumbnail> surface_cache = new LruCache<Page, Page.Thumbnail> (60, 0.8);

        public CellRendererPage ()
        {
        }

        [GLib.Property ("page")]
        public Page Page { get; set; }

        public override void GetSize (Gtk.Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
        {
            base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
            x_offset = y_offset = 0;
            width = cell_area.Width;
            height = cell_area.Width;
        }

        protected override void Render (Cairo.Context cr, double width, double height, CellRendererState state)
        {
            var border_width = 1.0 * (width / 128.0);
            width -= 2 * border_width;
            height -= 2 * border_width;

            //Console.WriteLine ("SurfaceCache has HitRatio = {0} ({1} hits, {2} misses)", surface_cache.HitRatio, surface_cache.Hits, surface_cache.Misses);
            Page.Thumbnail cache_obj;
            ImageSurface surface = null;
            if (surface_cache.TryGetValue (Page, out cache_obj)) {
                // Don't use if not big enough or if dirty
                surface = cache_obj.Surface;
                if (Page.SurfaceDirty || (surface.Width < width && surface.Height < height)) {
                    surface_cache.Remove (Page);
                    surface.Destroy ();
                    surface = null;
                    Hyena.Gui.CairoExtensions.DisposeContext (cache_obj.Context);
                }
            }

            if (surface == null) {
                // Create a new thumbnail surface, but only on 200px boundaries, then we scale down if needed
                var w = width + (width % scale_every);
                var h = height + (height % scale_every);
                cache_obj = Page.Document.GetSurface (Page, (int)w, (int)h);
                surface = cache_obj.Surface;
                
                // Put it in the cache
                surface_cache.Add (Page, cache_obj);
            }

            double scale = Math.Min (width / (double)surface.Width, height / (double)surface.Height);

            // Center the thumbnail if either dimension is smaller than the cell's
            if (surface.Width * scale < width || surface.Height * scale < height) {
                var x = ((width - surface.Width * scale) / 2.0) - border_width;
                var y = ((height - surface.Height * scale) / 2.0) - border_width;
                cr.Translate (x, y);
            }

            PaintDocumentBorder (cr, scale * surface.Width, scale * surface.Height, border_width);

            // Scale down the surface if it's not exactly the right size
            if (scale < 1) {
                cr.Scale (scale, scale);
            }

            // Paint the scaled/translated thumbnail
            cr.SetSource (surface);
            cr.Paint ();
        }

        private void PaintDocumentBorder (Context cr, double doc_width, double doc_height, double border_width)
        {
            // Paint a nice document-like border around it
            var thin = 0.25 * border_width;
            var thick = 0.75 * border_width;

            cr.Color = new Color (0, 0, 0);
            cr.LineWidth = thin;
            cr.Rectangle (thick + thin/2, thick + thin/2, doc_width, doc_height);
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
    }
}
