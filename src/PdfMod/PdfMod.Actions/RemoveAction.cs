
using System;
using System.Collections.Generic;

using Mono.Posix;

using Hyena;
using Hyena.Gui;

using PdfMod;

namespace PdfMod.Actions
{
    public class RemoveAction : IUndoAction
    {
        private Document doc;
        private List<Page> removed_pages = new List<Page> ();
        private IEnumerable<Page> to_remove;
        private int [] old_indices;

        public RemoveAction (Document doc, IEnumerable<Page> to_remove)
        {
            this.doc = doc;
            this.to_remove = to_remove;
            Redo ();
        }

        #region IUndoAction implementation
        
        public void Undo ()
        {
            if (old_indices == null) {
                throw new InvalidOperationException (Catalog.GetString ("Error trying to unremove pages from document"));
            }

            for (int i = 0; i < old_indices.Length; i++) {
                doc.Add (old_indices[i], removed_pages[i]);
            }
            
            removed_pages.Clear ();
            old_indices = null;
        }
        
        public void Redo ()
        {
            if (removed_pages.Count != 0) {
                throw new InvalidOperationException (Catalog.GetString ("Error trying to remove pages from document"));
            }

            removed_pages.AddRange (to_remove);
            old_indices = new int[removed_pages.Count];

            for (int i = 0; i < removed_pages.Count; i++) {
                old_indices[i] = doc.IndexOf (removed_pages[i]);
                doc.Remove (removed_pages[i]);
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
