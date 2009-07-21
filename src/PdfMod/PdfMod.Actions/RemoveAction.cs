
using System;
using System.Collections.Generic;

using Mono.Posix;

using Hyena;
using Hyena.Gui;

using PdfSharp.Pdf;

namespace PdfMod.Actions
{
    public class RemoveAction : IUndoAction
    {
        private PdfDocument doc;
        private List<PdfPage> removed_pages = new List<PdfPage> ();
        private int [] to_remove;

        public RemoveAction (PdfDocument doc, int [] to_remove)
        {
            this.doc = doc;
            this.to_remove = to_remove;
        }

        #region IUndoAction implementation
        
        public void Undo ()
        {
            if (removed_pages.Count != to_remove.Length) {
                throw new InvalidOperationException (Catalog.GetString ("Error trying to unremove pages from document"));
            }

            foreach (var i in to_remove) {
                var page = removed_pages[0];
                removed_pages.RemoveAt (0);
                doc.Pages.Insert (i, page);
            }
        }
        
        public void Redo ()
        {
            if (removed_pages.Count != 0) {
                throw new InvalidOperationException (Catalog.GetString ("Error trying to remove pages from document"));
            }
            
            foreach (var i in to_remove) {
                removed_pages.Add (doc.Pages[i]);
                doc.Pages.RemoveAt (i);
            }
        }
        
        public void Merge (IUndoAction action)
        {
            throw new System.NotImplementedException();
        }
        
        public bool CanMerge (IUndoAction action)
        {
            return false;
        }
        
        #endregion
           
    }
}
