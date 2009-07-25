
using System;
using System.Collections.Generic;

using Mono.Unix;

using Hyena;
using Hyena.Gui;

using PdfMod;

namespace PdfMod.Actions
{
    public class RemoveAction : BasePageAction
    {
        private int [] old_indices;

        public RemoveAction (Document document, IEnumerable<Page> pages) : base (document, pages)
        {
        }

        public override void Undo ()
        {
            Hyena.Log.Error ("Undo for the RemoveAction is not yet implemented");
            // Currently fails in PdfSharp
            /*if (old_indices == null) {
                throw new InvalidOperationException (Catalog.GetString ("Error trying to unremove pages from document"));
            }

            for (int i = 0; i < old_indices.Length; i++) {
                Console.WriteLine ("Trying to add back page {0} at index {1}", Pages[i], old_indices[i]);
                Document.Add (old_indices[i], Pages[i]);
            }

            old_indices = null;*/
        }

        public override void Redo ()
        {
            if (old_indices != null) {
                throw new InvalidOperationException (Catalog.GetString ("Error trying to remove pages from document"));
            }

            old_indices = new int[Pages.Count];
            for (int i = 0; i < Pages.Count; i++) {
                old_indices[i] = Document.IndexOf (Pages[i]);
                Console.WriteLine ("Old index of {0} was {1}", Pages[i], old_indices[i]);
                Document.Remove (Pages[i]);
            }
        }
    }
}
