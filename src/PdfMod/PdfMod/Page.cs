
using System;

using PdfSharp.Pdf;

namespace PdfMod
{
    public class Page
    {
        internal PdfPage Pdf { get; set; }

        public Document Document { get; internal set; }
        public int Index { get; internal set; }
        public bool SurfaceDirty { get; internal set; }

        public Page (PdfPage pdf_page)
        {
            Pdf = pdf_page;
        }

        public Page Clone ()
        {
            return this;
            /*return new Page (pdf_page.Clone ()) {
                Document = this.Document,
                Index = this.Index,
                Pixbuf = this.Pixbuf
            };*/
        }

        public class Thumbnail : IDisposable
        {
            public Cairo.ImageSurface Surface { get; internal set; }
            public Cairo.Context Context { get; internal set; }

            public void Dispose ()
            {
                if (Surface != null && Surface.Handle != IntPtr.Zero) {
                    ((IDisposable)Surface).Dispose ();
                }
                Surface = null;

                if (Context != null) {
                    Hyena.Gui.CairoExtensions.DisposeContext (Context);
                    Context = null;
                }
            }
        }
    }
}
