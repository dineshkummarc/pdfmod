
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

using Mono.Unix;
using Gtk;

using PdfMod.Pdf;

namespace PdfMod.Gui
{
    public class PdfListStore : ListStore
    {
        public const int SortColumn = 0;
        public const int TooltipColumn = 1;
        public const int PageColumn = 2;

        public PdfListStore () : base (typeof (int), typeof (string), typeof (Page))
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
        
        private string GetPageTooltip (Page page)
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
