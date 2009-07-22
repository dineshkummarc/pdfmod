
using System;
using System.Collections.Generic;

using Hyena;

namespace PdfMod.Actions
{
    public abstract class BasePageAction : IUndoAction
    {
        protected Document Document { get; private set; }
        protected List<Page> Pages { get; private set; }

        public BasePageAction (Document document, IEnumerable<Page> to_remove)
        {
            Document = document;
            Pages = new List<Page> (to_remove);
        }

        public void Do ()
        {
            Redo ();
        }

        #region IUndoAction implementation

        public abstract void Undo ();

        public abstract void Redo ();

        public virtual void Merge (IUndoAction action)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool CanMerge (IUndoAction action)
        {
            return false;
        }

        #endregion
    }
}
