
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
    }
}
