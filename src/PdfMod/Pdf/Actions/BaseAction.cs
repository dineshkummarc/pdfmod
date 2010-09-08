// Copyright (C) 2009-2010 Novell, Inc.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Collections.Generic;

using Hyena;

namespace PdfMod.Pdf.Actions
{
    public interface IDescribedUndoAction : IUndoAction
    {
        string Description { get; }
    }

    public abstract class BaseAction : IDescribedUndoAction
    {
        protected Document Document { get; private set; }
        public string Description { get; set; }

        public BaseAction (Document document) : this (document, null) {}

        public BaseAction (Document document, string description)
        {
            Document = document;
            Description = description;
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

    public class DelegateAction : BaseAction
    {
        public DelegateAction (Document document) : base (document) {}

        public System.Action UndoAction { get; set; }
        public System.Action RedoAction { get; set; }

        public override void Undo ()
        {
            UndoAction ();
        }

        public override void Redo ()
        {
            RedoAction ();
        }
    }
}
