
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

            IntPtr error = IntPtr.Zero;
            IntPtr poppler_doc = poppler_document_new_from_file (new Uri (doc_uri).AbsoluteUri, null, ref error);
            int n_pages = poppler_document_get_n_pages (poppler_doc);

            if (n_pages != pdf_doc.PageCount) {
                Hyena.Log.Error (Catalog.GetString ("This PDF Not Supported"), Catalog.GetString ("There was an inconsistency detected when reading this file.  Editing it will probably not work."), true);
                return;
            }

            for (int i = 0; i < n_pages; i++) {
                IntPtr page = poppler_document_get_page (poppler_doc, i);

                Gdk.Pixbuf pixbuf = null;
                IntPtr thumb_ptr = poppler_page_get_thumbnail (page);
                if (thumb_ptr != IntPtr.Zero) {
                    // TODO should scale the thumbnail?
                    Console.WriteLine ("Got thumbnail!!");
                    var thumb = new ThumbnailSurface (thumb_ptr);
                    thumb.WriteToPng ("temp.png");
                    pixbuf = new Gdk.Pixbuf ("temp.png");
                    System.IO.File.Delete ("temp.png");
                    ((IDisposable)thumb).Dispose ();
                } else {
                    double w, h;
                    poppler_page_get_size (page, out w, out h);
                    double scale = SIZE / Math.Max (w, h);
                    
                    pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, false, 8, SIZE, SIZE);
                    poppler_page_render_to_pixbuf (page, 0, 0, (int)w, (int)h, scale, 0, pixbuf.Handle);
                }
                
                AppendValues (i,
                    String.Format ("<small>{0}</small>",
                        GLib.Markup.EscapeText (String.Format (Catalog.GetString ("Page {0}"), i + 1))),
                    page, pixbuf
                );
                //GLib.Marshaller.Free (page);
            }

            //GLib.Marshaller.Free (poppler_doc);
        }

        [DllImport ("libpoppler-glib.dll")]
        private static extern IntPtr poppler_document_new_from_file (string uri, string pwd, ref IntPtr error);

        [DllImport ("libpoppler-glib.dll")]
        private static extern IntPtr poppler_document_get_page (IntPtr doc, int index);

        [DllImport ("libpoppler-glib.dll")]
        private static extern void poppler_page_get_size (IntPtr page, out double width, out double height);

        [DllImport ("libpoppler-glib.dll")]
        private static extern void poppler_page_render (IntPtr page, out IntPtr context);

        [DllImport ("libpoppler-glib.dll")]
        private static extern void poppler_page_render_to_pixbuf (IntPtr page, int src_x, int src_y, int src_width, int src_height, double scale, int rotation, IntPtr pixbuf);

        [DllImport ("libpoppler-glib.dll")]
        private static extern int poppler_document_get_n_pages (IntPtr doc);

        [DllImport ("libpoppler-glib.dll")]
        private static extern IntPtr poppler_page_get_thumbnail (IntPtr page);

        [DllImport ("libpoppler-glib.dll")]
        private static extern bool poppler_page_get_thumbnail_size (IntPtr page, out int width, out int height);
    }
}
