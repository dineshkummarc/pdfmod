
using System;
using System.Collections.Generic;

using Mono.Unix;

using Hyena;
using Hyena.Gui;

using PdfMod;

namespace PdfMod.Actions
{
    public class MoveAction : BasePageAction
    {
        private int [] old_indices;
        private int to_index;

        public MoveAction (Document document, IEnumerable<Page> pages, int to_index) : base (document, pages)
        {
            this.to_index = to_index;
        }

        public override void Undo ()
        {
            if (old_indices == null) {
                throw new InvalidOperationException (Catalog.GetString ("Error trying to unmove pages"));
            }

            for (int i = 0; i < old_indices.Length; i++) {
                Console.WriteLine ("Trying to move page {0} back to index {1}", Pages[i], old_indices[i]);
                Document.Move (old_indices[i], new Page [] { Pages[i] });
            }

            old_indices = null;
        }

        public override void Redo ()
        {
            if (old_indices != null) {
                throw new InvalidOperationException (Catalog.GetString ("Error trying to move pages"));
            }

            old_indices = new int[Pages.Count];
            for (int i = 0; i < Pages.Count; i++) {
                old_indices[i] = Document.IndexOf (Pages[i]);
            }

            Document.Move (to_index, Pages.ToArray ());
        }
    }
}
