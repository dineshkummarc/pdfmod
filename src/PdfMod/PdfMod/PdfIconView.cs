
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
        const int MIN_WIDTH = 128;
        const int MAX_WIDTH = 2054;

        private PdfMod app;
        private Document document;
        private PdfListStore store;
        private PageSelectionMode page_selection_mode = PageSelectionMode.None;
        private int columns = 2;

        public PdfListStore Store { get { return store; } }
        public bool CanZoomIn { get; private set; }
        public bool CanZoomOut { get; private set; }
        
        public event System.Action ZoomChanged;

        public PdfIconView (PdfMod app) : base ()
        {
            this.app = app;
            Model = store = new PdfListStore ();
            TooltipColumn = PdfListStore.MarkupColumn;
            //MarkupColumn = PdfListStore.MarkupColumn;
            var ccell = new CellRendererPage ();
            PackStart (ccell, true);
            AddAttribute (ccell, "page", PdfListStore.PageColumn);
            CanZoomIn = CanZoomOut = true;
            Spacing = 0;

            ColumnSpacing = RowSpacing = Margin;
            //Reorderable = true;
            SelectionMode = SelectionMode.Multiple;
            
            SizeAllocated += (o, a) => {
                if (!zoom_manually_set) {
                    ZoomFit ();
                }
            };

            PopupMenu += HandlePopupMenu;
            ButtonPressEvent += HandleButtonPressEvent;
            ScrollEvent += HandleScrollEvent;

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

        void HandleScrollEvent(object o, ScrollEventArgs args)
        {
            if ((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                Zoom (args.Event.Direction == ScrollDirection.Down ? -20 : 20);
                args.RetVal = true;
            }
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
            page_selection_mode = PageSelectionMode.None;
            Refresh ();
        }

        private void Refresh ()
        {
            if (!zoom_manually_set) {
                ZoomFit ();
            }
            RefreshSelection ();
        }

        private void OnPagesAdded (int index, Page [] pages)
        {
            foreach (var page in pages) {
                var iter = store.InsertWithValues (index, store.GetValuesForPage (page));
                store.EmitRowInserted (store.GetPath (iter), iter);
            }

            Refresh ();
        }

        private void OnPagesChanged (Page [] pages)
        {
            foreach (var page in pages) {
                var iter = store.GetIterForPage (page);
                if (!TreeIter.Zero.Equals (iter)) {
                    store.EmitRowChanged (store.GetPath (iter), iter);
                }
            }
            
            Refresh ();
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

            Refresh ();
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

            Refresh ();
        }

        #endregion

        private bool zoom_manually_set;
        public void Zoom (int pixels)
        {
            CanZoomIn = CanZoomOut = true;
            zoom_manually_set = true;

            int new_width = ItemWidth + pixels;
            if (new_width <= MIN_WIDTH) {
                CanZoomOut = false;
                new_width = MIN_WIDTH;
            } else if (new_width >= MAX_WIDTH) {
                CanZoomIn = false;
                new_width = MAX_WIDTH;
            }

            if (ItemWidth == new_width) {
                return;
            }

            Console.WriteLine ("ItemWidth = {0}", new_width);
            ItemWidth = new_width;

            var handler = ZoomChanged;
            if (handler != null) {
                handler ();
            }
        }

        private int last_zoom, before_last_zoom;
        public void ZoomFit ()
        {
            if (document == null)
                return;

            zoom_manually_set = false;
            // Try to fit all pages into the view, with a minimum size
            var n = (double)document.Count;
            var width = (double)Allocation.Width - 2 * Margin - 2*BorderWidth - 4; // HACK this -4 is total hack
            var height = (double)Allocation.Height - 2 * Margin - 2*BorderWidth - 4; // same

            var n_across = (int)Math.Ceiling (Math.Sqrt (width * n / height));
            var best_width = (int) Math.Floor ((width - (n_across + 1) * ColumnSpacing - n_across*2*FocusLineWidth) / n_across);

            // restrict to min/max
            best_width = Math.Min (MAX_WIDTH, Math.Max (MIN_WIDTH, best_width));

            if (best_width == ItemWidth) {
                return;
            }

            // Total hack to avoid infinite SizeAllocate/ZoomFit loop
            if (best_width == before_last_zoom || best_width == last_zoom) {
                return;
            }

            before_last_zoom = last_zoom;
            last_zoom = ItemWidth;

            ItemWidth = best_width;
        }

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
    }
}
