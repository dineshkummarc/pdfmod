
using System;
using System.Linq;
using System.Collections.Generic;

using Mono.Unix;

using Hyena;
using Hyena.Gui;

using PdfMod;

namespace PdfMod.Pdf.Actions
{
    public class MoveAction : BasePageAction
    {
        private int [] old_indices;
        private int to_index;

        public MoveAction (Document document, IEnumerable<Page> pages, int to_index) : base (document, pages)
        {
            this.to_index = to_index;
            // Translators: {0} is the # of pages, {1} is a translated string summarizing the pages, eg "page 1"
            Description = String.Format (Catalog.GetPluralString ("Move {1}", "Move {1}", Pages.Count),
                Pages.Count, Document.GetPageSummary (Pages, 5));
        }

        public override void Undo ()
        {
            if (old_indices == null) {
                throw new InvalidOperationException (Catalog.GetString ("Error trying to unmove pages"));
            }

            Document.Move (to_index, Pages.ToArray (), old_indices);
            old_indices = null;
        }

        public override void Redo ()
        {
            if (old_indices != null) {
                throw new InvalidOperationException (Catalog.GetString ("Error trying to move pages"));
            }

            old_indices = Pages.Select<Page, int> (Document.IndexOf).ToArray ();
            Document.Move (to_index, Pages.ToArray (), null);
        }
    }
}
