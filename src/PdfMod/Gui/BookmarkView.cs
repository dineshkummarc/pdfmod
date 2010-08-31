// Copyright (C) 2010 Novell, Inc.
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
using System.Linq;

using Gtk;

using PdfSharp.Pdf;
using PdfMod.Pdf;
using System.Collections.Generic;
using Mono.Unix;
using PdfSharp.Pdf.Advanced;

namespace PdfMod.Gui
{
    public class BookmarkView : VBox
    {
        TreeView tree_view;
        TreeStore model;
        Document document;

        public bool IsModified { get; set; }

        public BookmarkView ()
        {
            BuildTreeView ();
            BuildButtonBar ();

            WidthRequest = 200;
            Spacing = 6;
            ShowAll ();
        }

        public void SetDocument (Document new_doc)
        {
            if (document != null) {
                document.PagesAdded   -= OnPagesAdded;
                document.PagesChanged -= OnPagesChanged;
                document.PagesRemoved -= OnPagesRemoved;
                document.PagesMoved   -= OnPagesMoved;
            }

            document = new_doc;
            document.PagesAdded   += OnPagesAdded;
            document.PagesChanged += OnPagesChanged;
            document.PagesRemoved += OnPagesRemoved;
            document.PagesMoved   += OnPagesMoved;

            model.Clear ();
            AddOutlineCollection (document, document.Pdf.Outlines, TreeIter.Zero);
        }

        void OnPagesAdded (int index, Page [] pages)
        {
            UpdateModel ();
        }

        void OnPagesChanged (Page [] pages)
        {
            UpdateModel ();
        }

        void OnPagesRemoved (Page [] pages)
        {
            var pdf_pages = pages.Select (p => p.Pdf).ToList ();

            var to_remove = new List<TreeIter> ();
            // Remove bookmarks that point to removed pages
            model.Foreach ((m, path, iter) => {
                var outline = GetOutline (iter);
                if (pdf_pages.Contains (outline.DestinationPage)) {
                    to_remove.Add (iter);
                }
                return false;
            });

            while (to_remove.Count > 0) {
                var iter = to_remove[0];
                to_remove.Remove (iter);

                var outline = GetOutline (iter);
                outline.Remove ();
                model.Remove (ref iter);

                Hyena.Log.DebugFormat ("Removing bookmark '{0}' since its page was removed", outline.Title);
            }

            UpdateModel ();
        }

        void OnPagesMoved ()
        {
            UpdateModel ();
        }

        public IEnumerable<PdfOutline> Outlines {
            get {
                TreeIter iter;
                if (model.GetIterFirst (out iter)) {
                    do {
                        yield return GetOutline (iter);
                    } while (model.IterNext (ref iter));
                }
            }
        }

        private void BuildTreeView ()
        {
            // outline, expanded/opened, title, page # destination, tooltip
            model = new TreeStore (typeof(PdfSharp.Pdf.PdfOutline), typeof(bool), typeof(string), typeof(int), typeof(string));

            tree_view = new TreeView () {
                Model = model,
                SearchColumn = (int)ModelColumns.Title,
                TooltipColumn = (int)ModelColumns.Tooltip,
                EnableSearch = true,
                EnableTreeLines = true,
                HeadersVisible = false,
                Reorderable = false,
                ShowExpanders = true
            };
            tree_view.Selection.Mode = SelectionMode.Browse;

            var title = new CellRendererText () {
                Editable = true,
                Ellipsize = Pango.EllipsizeMode.End
            };
            title.Edited += delegate(object o, EditedArgs args) {
                TreeIter iter;
                if (model.GetIterFromString (out iter, args.Path)) {
                    if (!String.IsNullOrEmpty (args.NewText)) {
                        var bookmark = GetOutline (iter);
                        bookmark.Title = args.NewText;
                        model.SetValue (iter, (int)ModelColumns.Title, bookmark.Title);
                        MarkModified ();
                    } else {
                        args.RetVal = false;
                    }
                }
            };
            var title_col = tree_view.AppendColumn ("", title, "text", ModelColumns.Title);
            title_col.Expand = true;

            var page = new CellRendererText () {
                Editable = true,
                Style = Pango.Style.Italic
            };
            page.Edited += delegate(object o, EditedArgs args) {
                TreeIter iter;
                if (model.GetIterFromString (out iter, args.Path)) {
                    var bookmark = GetOutline (iter);
                    int i = -1;
                    if (Int32.TryParse (args.NewText, out i) && i >= 1 && i <= document.Count && i != (GetDestIndex (bookmark) + 1)) {
                        SetDestIndex (bookmark, i - 1);
                        model.SetValue (iter, (int)ModelColumns.PageNumber, i);
                        MarkModified ();
                    } else {
                        args.RetVal = false;
                    }
                }
            };
            var num_col = tree_view.AppendColumn ("", page, "text", ModelColumns.PageNumber);
            num_col.Alignment = 1.0f;
            num_col.Expand = false;

            var label = new Label (Catalog.GetString ("_Bookmarks")) { Xalign = 0f, MnemonicWidget = tree_view };
            PackStart (label, false, false, 0);

            var sw = new Gtk.ScrolledWindow () {
                HscrollbarPolicy = PolicyType.Never,
                VscrollbarPolicy = PolicyType.Automatic,
                Child = tree_view
            };
            PackStart (sw, true, true, 0);
        }

        private void BuildButtonBar ()
        {
            var box = new HBox () { Spacing = 6 };
            var add_button = new Button (Gtk.Stock.Add);
            add_button.Clicked += (o, a) => {
                TreeIter parent_iter;
                if (!tree_view.Selection.GetSelected (out parent_iter) && !model.GetIterFirst (out parent_iter)) {
                    parent_iter = TreeIter.Zero;
                }

                // Add it to the PDF document
                var outline = new PdfOutline (Catalog.GetString ("New bookmark"), document.Pages.First ().Pdf, true);
                if (!TreeIter.Zero.Equals (parent_iter)) {
                    var parent = GetOutline (parent_iter);
                    SetDestIndex (outline, GetDestIndex (parent) + 1);
                    parent.Outlines.Add (outline);

                    tree_view.ExpandToPath (model.GetPath (parent_iter));
                } else {
                    document.Pdf.Outlines.Add (outline);
                }

                // Add it to our TreeView
                var iter = AddOutline (parent_iter, outline);
                MarkModified ();

                // Begin editing its name
                tree_view.SetCursor (model.GetPath (iter), tree_view.Columns[0], true);
                Hyena.Log.Debug ("Added bookmark");
            };

            var remove_button = new Button (Gtk.Stock.Remove);
            remove_button.Clicked += (o, a) => {
                TreeIter iter;
                if (tree_view.Selection.GetSelected (out iter)) {
                    var outline = GetOutline (iter);
                    Hyena.Log.DebugFormat ("Removing bookmark '{0}'", outline.Title);
                    outline.Remove ();
                    model.Remove (ref iter);
                    MarkModified ();
                }
            };

            box.PackStart (add_button, false, false, 0);
            box.PackStart (remove_button, false, false, 0);

            PackStart (box, false, false, 0);
        }

        private void UpdateModel ()
        {
            model.Foreach ((m, path, iter) => {
                model.SetValues (iter, GetValuesFor (GetOutline (iter)));
                return false;
            });
        }

        private void MarkModified ()
        {
            IsModified = true;
            document.HasUnsavedChanges = true;
        }

        private TreeIter AddOutline (TreeIter parent, PdfOutline outline)
        {
            return TreeIter.Zero.Equals (parent)
                ? model.AppendValues (GetValuesFor (outline))
                : model.AppendValues (parent, GetValuesFor (outline));
        }

        private object [] GetValuesFor (PdfOutline outline)
        {
            int dest_num = GetDestIndex (outline);

            return new object [] { outline, outline.Opened, outline.Title, dest_num + 1,
                String.Format (Catalog.GetString ("Bookmark links to page {0}"), dest_num + 1) };
        }

        private int GetDestIndex (PdfOutline outline)
        {
            if (outline.DestinationPage == null)
                return -1;
            else
                return document.Pages.Select (p => p.Pdf).IndexOf (outline.DestinationPage);
        }

        private void SetDestIndex (PdfOutline outline, int i)
        {
            if (i >= 0 && i < document.Count) {
                outline.DestinationPage = document.Pages.Skip (i).First ().Pdf;
            }
        }

        private PdfOutline GetSelected ()
        {
            TreeIter iter;
            if (tree_view.Selection.GetSelected (out iter))
                return GetOutline (iter);
            return null;
        }

        private void AddOutlineCollection (Document document, PdfOutline.PdfOutlineCollection outlines, TreeIter parent)
        {
            if (outlines != null) {
                foreach (PdfOutline outline in outlines) {
                    var iter = AddOutline (parent, outline);

                    // Recursively add this item's children, if any
                    AddOutlineCollection (document, outline.Outlines, iter);
                }
            }
        }

        private PdfOutline GetOutline (TreeIter iter)
        {
            return (PdfOutline) model.GetValue (iter, (int)ModelColumns.Bookmark);
        }

        private enum ModelColumns : int {
            Bookmark,
            IsExpanded,
            Title,
            PageNumber,
            Tooltip
        };
    }

    internal static class Extensions
    {
        public static int IndexOf<T> (this IEnumerable<T> enumerable, T item)
        {
            int i = 0;
            foreach (var a in enumerable) {
                if (item.Equals (a)) {
                    return i;
                } else {
                    i++;
                }
            }

            return -1;
        }
    }
}
