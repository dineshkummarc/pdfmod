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

using Mono.Unix;
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
            var label = page.Document.Labels[page];
            string page_no = Catalog.GetString (String.Format ("Page {0}", page.Index + 1));
            return ((null == label) ? page_no : String.Format ("{0} ({1})", label, page_no));
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
