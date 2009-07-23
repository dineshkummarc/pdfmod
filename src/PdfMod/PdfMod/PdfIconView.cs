
using System;
using System.Collections.Generic;

using Gdk;
using Gtk;

using PdfSharp.Pdf;

namespace PdfMod
{
    public enum PageSelectionMode
    {
        None,
        Evens,
        Odds,
        Matching,
        All
    }

    public class PdfIconView : Gtk.IconView
    {
        private PdfMod app;
        private Document document;
        private PdfListStore store;
        private PageSelectionMode page_selection_mode = PageSelectionMode.None;
            
        public PdfListStore Store { get { return store; } }

        private int columns = 2;
        
        public PdfIconView (PdfMod app) : base ()
        {
            this.app = app;
            Model = store = new PdfListStore ();
            PixbufColumn = PdfListStore.PixbufColumn;
            MarkupColumn = PdfListStore.MarkupColumn;

            ColumnSpacing = RowSpacing = 12;
            UpdateItemWidth ();
            Reorderable = true;
            SelectionMode = SelectionMode.Multiple;
            
            /*SizeAllocated += (o, a) => {
                UpdateItemWidth ();
            };*/

            PopupMenu += HandlePopupMenu;
            ButtonPressEvent += HandleButtonPressEvent;

            SelectionChanged += delegate {
                if (!refreshing_selection) {
                    page_selection_mode = PageSelectionMode.None;
                }
            };
            //ButtonReleaseEvent += HandleButtonReleaseEvent;

            // Drag and Drop
            //EnableModelDragDest(TargetEntry[], Gdk.DragAction);
            //EnableModelDragSource(Gdk.ModifierType, TargetEntry[], Gdk.DragAction);

            //SelectedItems
            //SelectionChanged +=
            DragDataReceived += HandleDragDataReceived;
            //DragDataGet
            //DragMotion

            //GetDestItemAtPos(int, int, out TreePath, out IconViewDropPosition) : bool

            // Gtk.Drag.Highlight / Unhighlight
            
        }

        void HandleButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 3) {
                var path = GetPathAtPos ((int)args.Event.X, (int)args.Event.Y);
                if (path != null) {
                    if (!PathIsSelected (path)) {
                        bool ctrl = (args.Event.State & Gdk.ModifierType.ControlMask) != 0;
                        bool shift = (args.Event.State & Gdk.ModifierType.ShiftMask) != 0;
                        if (ctrl) {
                            SelectPath (path);
                        } else if (shift) {
                            TreePath cursor;
                            CellRenderer cell;
                            if (GetCursor (out cursor, out cell)) {
                                TreePath first = cursor.Compare (path) < 0 ? cursor : path;
                                do {
                                    SelectPath (first);
                                    first.Next ();
                                } while (first != path && first != cursor && first != null);
                            } else {
                                SelectPath (path);
                            }
                        } else {
                            UnselectAll ();
                            SelectPath (path);
                        }
                    }
                    HandlePopupMenu (null, null);
                    args.RetVal = true;
                }
            }
        }

        public void HandlePopupMenu (object o, PopupMenuArgs args)
        {
            app.GlobalActions["PageContextMenuAction"].Activate ();
        }


        public IEnumerable<Page> SelectedPages {
            get {
                foreach (var path in SelectedItems) {
                    TreeIter iter;
                    store.GetIter (out iter, path);
                    yield return store.GetValue (iter, PdfListStore.PageColumn) as Page;
                }
            }
        }

        public IEnumerable<int> SelectedIndices {
            get {
                foreach (var path in SelectedItems) {
                    TreeIter iter;
                    store.GetIter (out iter, path);
                    yield return (int)store.GetValue (iter, PdfListStore.PageColumn);
                }
            }
        }

        private void HandleDragDataReceived (object o, DragDataReceivedArgs args)
        {
            TreePath path;
            IconViewDropPosition pos;
            GetDestItemAtPos (args.X, args.Y, out path, out pos);
            Console.WriteLine ("DragDataReceived: {0}", pos);
        }

        #region Document event handling

        public void SetDocument (Document document)
        {
            if (this.document != null) {
                this.document.PagesAdded   -= OnPagesAdded;
                this.document.PagesChanged -= OnPagesChanged;
                this.document.PagesRemoved -= OnPagesRemoved;
                this.document.PagesMoved   -= OnPagesMoved;
            }

            this.document = document;
            this.document.PagesAdded   += OnPagesAdded;
            this.document.PagesChanged += OnPagesChanged;
            this.document.PagesRemoved += OnPagesRemoved;
            this.document.PagesMoved   += OnPagesMoved;

            store.SetDocument (document);
        }

        private void OnPagesAdded (int index, Page [] pages)
        {
            foreach (var page in pages) {
                var iter = store.InsertWithValues (index, store.GetValuesForPage (page));
                store.EmitRowInserted (store.GetPath (iter), iter);
            }

            RefreshSelection ();
        }

        private void OnPagesChanged (Page [] pages)
        {
            // Update preview pixbuf
            foreach (var page in pages) {
                Console.WriteLine ("IconView got page changed: {0}", page.Index);
                var iter = store.GetIterForPage (page);
                if (!TreeIter.Zero.Equals (iter)) {
                    var pixbuf = store.GetValue (iter, PdfListStore.PixbufColumn) as Pixbuf;
                    if (pixbuf != page.Pixbuf) {
                        store.SetValue (iter, PdfListStore.PixbufColumn, page.Pixbuf);
                        pixbuf.Dispose ();
                        store.EmitRowChanged (store.GetPath (iter), iter);
                    }
                }
            }
            
            RefreshSelection ();
        }

        private void OnPagesRemoved (Page [] pages)
        {
            foreach (var page in pages) {
                var iter = store.GetIterForPage (page);
                if (!TreeIter.Zero.Equals (iter)) {
                    store.Remove (ref iter);
                    store.EmitRowDeleted (store.GetPath (iter));
                }
            }

            RefreshSelection ();
        }

        private void OnPagesMoved (int index, Page [] pages)
        {
            // Update the sort values
            foreach (var page in pages) {
                var iter = store.GetIterForPage (page);
                if (!TreeIter.Zero.Equals (iter)) {
                    store.SetValue (iter, PdfListStore.SortColumn, index);
                    store.EmitRowChanged (store.GetPath (iter), iter);
                }
                index++;
            }

            RefreshSelection ();
        }

        #endregion

        private string selection_match_query;
        public void SetSelectionMatchQuery (string query)
        {
            selection_match_query = query;
            SetPageSelectionMode (PageSelectionMode.Matching);
        }

        public void SetPageSelectionMode (PageSelectionMode mode)
        {
            page_selection_mode = mode;
            RefreshSelection ();
        }

        private bool refreshing_selection;
        private void RefreshSelection ()
        {
            refreshing_selection = true;
            if (page_selection_mode == PageSelectionMode.None) {
            } else if (page_selection_mode == PageSelectionMode.All) {
                SelectAll ();
            } else {
                List<Page> matches = null;
                if (page_selection_mode == PageSelectionMode.Matching) {
                    matches = new List<Page> (app.Document.FindPagesMatching (selection_match_query));
                }
                int i = 1;
                foreach (var iter in store.TreeIters) {
                    var path = store.GetPath (iter);
                    bool select = false;

                    switch (page_selection_mode) {
                    case PageSelectionMode.Evens:
                        select = (i % 2) == 0;
                        break;
                    case PageSelectionMode.Odds:
                        select = (i % 2) == 1;
                        break;
                    case PageSelectionMode.Matching:
                        select = matches.Contains (store.GetValue (iter, PdfListStore.PageColumn) as Page);
                        break;
                    }

                    if (select) {
                        SelectPath (path);
                    } else {
                        UnselectPath (path);
                    }
                    i++;
                }
            }
            refreshing_selection = false;

            QueueDraw ();
        }

        // CreateDragIcon(TreePath) : Gdk.Pixmap
        
        
        public int PageColumns {
            get { return columns; }
            set {
                columns = value;
                UpdateItemWidth ();
            }
        }
        
        private void UpdateItemWidth ()
        {
            int last_item_width = ItemWidth;
            int new_item_width = Math.Max (48,
				(int) Math.Floor ((double)(Allocation.Width - 4*ColumnSpacing - 2*Margin) / Columns)
			);

            if (last_item_width != new_item_width) {
            	ItemWidth = new_item_width;
            }
            Console.WriteLine ("width = {0}, borderWidth = {1}, ColumnSpacing = {2}, Margin = {3}, itemWidth = {4}", Allocation.Width, this.BorderWidth, this.ColumnSpacing, this.Margin, this.ItemWidth);
        }
    }
}
