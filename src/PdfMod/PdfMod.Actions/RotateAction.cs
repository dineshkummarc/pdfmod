
using System;
using System.Collections.Generic;

namespace PdfMod.Actions
{
    public class RotateAction : BasePageAction
    {
        private int rotation;

        public RotateAction (Document document, IEnumerable<Page> pages, int rotation) : base (document, pages)
        {
            this.rotation = rotation;
        }

        public override void Undo ()
        {
            Document.Rotate (Pages.ToArray (), -rotation);
        }

        public override void Redo ()
        {
            Document.Rotate (Pages.ToArray (), rotation);
        }
    }
}
