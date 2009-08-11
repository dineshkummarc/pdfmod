
using System;

namespace PdfMod.Pdf
{
    public class PageThumbnail : IDisposable
    {
        public Cairo.ImageSurface Surface { get; internal set; }
        internal Cairo.Context Context { get; set; }

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
