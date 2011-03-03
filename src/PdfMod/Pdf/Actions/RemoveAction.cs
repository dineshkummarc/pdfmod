
using System;
using System.Linq;
using System.Collections.Generic;

using Mono.Unix;

using Hyena;
using Hyena.Gui;

using PdfMod;

namespace PdfMod.Pdf.Actions
{
    public class RemoveAction : BasePageAction
    {
        //int [] old_indices;
        Page [] pages;

        public RemoveAction (Document document, IEnumerable<Page> pages) : base (document, pages)
        {
            this.pages = pages.ToArray ();
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
            Document.Remove (pages);
        }
    }
}
