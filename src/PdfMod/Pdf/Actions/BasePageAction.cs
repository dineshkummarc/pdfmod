
using System;
using System.Collections.Generic;

using Hyena;

namespace PdfMod.Pdf.Actions
{
    public interface IDescribedUndoAction : IUndoAction
    {
        string Description { get; }
    }

    public abstract class BasePageAction : IDescribedUndoAction
    {
        protected Document Document { get; private set; }
        protected List<Page> Pages { get; private set; }
        public string Description { get; protected set; }

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
