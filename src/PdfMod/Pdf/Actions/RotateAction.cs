
using System;
using System.Collections.Generic;

using Mono.Unix;

namespace PdfMod.Pdf.Actions
{
    public class RotateAction : BasePageAction
    {
        int rotation;

        public RotateAction (Document document, IEnumerable<Page> pages, int rotation) : base (document, pages)
        {
            this.rotation = rotation;
            Description = String.Format (Catalog.GetPluralString ("Rotate {1}", "Rotate {1}", Pages.Count),
                Pages.Count, Document.GetPageSummary (Pages, 5));
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
