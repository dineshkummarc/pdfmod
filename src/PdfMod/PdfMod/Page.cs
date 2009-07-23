
using System;

using PdfSharp.Pdf;

namespace PdfMod
{
    public class Page
    {
        private PdfPage pdf_page;

        internal PdfPage Pdf { get { return pdf_page; } }

        public Document Document { get; internal set; }
        public int Index { get; internal set; }
        public bool SurfaceDirty { get; internal set; }
        
        public Page (PdfPage pdf_page)
        {
            this.pdf_page = pdf_page;
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

        public class Thumbnail
        {
            public Cairo.ImageSurface Surface { get; internal set; }
            public Cairo.Context Context { get; internal set; }
        }
    }
}