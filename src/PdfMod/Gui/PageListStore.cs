// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Michael McKinley
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
using System.Linq;

using Gtk;

using PdfMod.Pdf;

namespace PdfMod.Gui
{
    public class PageListStore : ListStore
    {
        public const int SortColumn = 0;
        public const int TooltipColumn = 1;
        public const int PageColumn = 2;

        public PageListStore () : base (typeof (int), typeof (string), typeof (Page))
        {
            SetSortColumnId (SortColumn, SortType.Ascending);
        }

        public void SetDocument (Document document)
        {
            Clear ();

            foreach (var page in document.Pages) {
                AppendValues (GetValuesForPage (page));
            }
        }

        public TreeIter GetIterForPage (Page page)
        {
            return TreeIters.FirstOrDefault (iter => {
                return GetValue (iter, PageColumn) == page;
            });
        }

        public IEnumerable<TreeIter> TreeIters {
            get {
                TreeIter iter;
                if (GetIterFirst (out iter)) {
                    do {
                        yield return iter;
                    } while (IterNext (ref iter));
                }
            }
        }

        string GetPageTooltip (Page page)
        {
            return page.Name;
        }

        public void UpdateForPage (TreeIter iter, Page page)
        {
            SetValue (iter, SortColumn, page.Index);
            SetValue (iter, TooltipColumn, GetPageTooltip(page));
            SetValue (iter, PageColumn, page);
        }

        internal object [] GetValuesForPage (Page page)
        {
            return new object[] { page.Index, GetPageTooltip(page), page };
        }
    }
}
