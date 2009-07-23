
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

using Mono.Posix;
using Gtk;

namespace PdfMod
{
    public class PdfListStore : ListStore
    {
        public const int SortColumn = 0;
        public const int TooltipColumn = 1;
        public const int PageColumn = 2;

        private Document document;

        public PdfListStore () : base (typeof (int), typeof (string), typeof (Page))
        {
            SetSortColumnId (SortColumn, SortType.Ascending);
        }

        public void SetDocument (Document document)
        {
            Clear ();
            this.document = document;

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

        public void UpdateForPage (TreeIter iter, Page page)
        {
            SetValue (iter, SortColumn, page.Index);
            SetValue (iter, TooltipColumn, String.Format (Catalog.GetString ("Page {0}"), page.Index + 1));
            SetValue (iter, PageColumn, page);
        }

        internal object [] GetValuesForPage (Page page)
        {
            return new object[] {
                page.Index,
                String.Format (Catalog.GetString ("Page {0}"), page.Index + 1),
                page
            };
        }
    }
}
