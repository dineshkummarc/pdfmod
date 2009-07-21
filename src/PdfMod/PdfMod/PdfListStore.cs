
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;

using Mono.Posix;
using Gtk;

using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace PdfMod
{
    public class PdfListStore : ListStore
    {
        public const int SortColumn = 0;
        public const int MarkupColumn = 1;
        public const int PixbufColumn = 3;

        private const int SIZE = 256;

        private string doc_uri;
        private PdfDocument pdf_doc;

        public PdfListStore () : base (typeof (int), typeof (string), typeof (PdfPage), typeof(Gdk.Pixbuf))
        {
            SetSortColumnId (SortColumn, SortType.Ascending);
        }

        public void SetDocument (PdfDocument pdf_doc, string doc_uri)
        {
            this.pdf_doc = pdf_doc;
            this.doc_uri = doc_uri;
            Refresh ();
        }

        private class ThumbnailSurface : Cairo.Surface
        {
            public ThumbnailSurface (IntPtr ptr) : base (ptr, true)
            {
            }
        }

        public void Refresh ()
        {
            Clear ();

            using (var doc = Poppler.Document.NewFromFile (new Uri (doc_uri).AbsoluteUri, "")) {
                int n_pages = doc.NPages;
    
                if (n_pages != pdf_doc.PageCount) {
                    Hyena.Log.Error (Catalog.GetString ("Unsupported PDF"), Catalog.GetString ("There was an inconsistency detected when reading this file.  Editing it will probably not work."), true);
                    return;
                }
    
                for (int i = 0; i < n_pages; i++) {
                    using (var page = doc.GetPage (i)) {
                        double w, h;
                        page.GetSize (out w, out h);
                        double scale = SIZE / Math.Max (w, h);
        
                        int thumb_w = (int) Math.Ceiling (w * scale);
                        int thumb_h = (int) Math.Ceiling (h * scale);
                        var pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, false, 8, thumb_w, thumb_h);
                        page.RenderToPixbuf (0, 0, (int)w, (int)h, scale, 0, pixbuf);
                        
                        AppendValues (
                            i,
                            String.Format ("<small>{0}</small>",
                                GLib.Markup.EscapeText (String.Format (Catalog.GetString ("Page {0}"), i + 1))),
                            pdf_doc.Pages[i],
                            pixbuf
                        );
                    }
                }
            }
        }
    }
}
