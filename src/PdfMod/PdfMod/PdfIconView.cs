
using System;
using System.Collections.Generic;
using System.Linq;

using Gdk;
using Gtk;

using PdfSharp.Pdf;

using PdfMod.Actions;

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

    public class PdfIconView : Gtk.IconView, IDisposable
    {
        public const int MIN_WIDTH = 128;
        public const int MAX_WIDTH = 2054;

        private enum Target {
            UriSrc,
            UriDest,
            MoveInternal,
            MoveExternal
        }

        private static readonly TargetEntry uri_src_target = new TargetEntry ("text/uri-list", 0, (uint)Target.UriSrc);
        private static readonly TargetEntry uri_dest_target = new TargetEntry ("text/uri-list", TargetFlags.OtherApp, (uint)Target.UriDest);
        private static readonly TargetEntry move_internal_target = new TargetEntry ("pdfmod/page-list", TargetFlags.Widget, (uint)Target.MoveInternal);
        private static readonly TargetEntry move_external_target = new TargetEntry ("pdfmod/page-list-external", 0, (uint)Target.MoveExternal);

        private PdfMod app;
        private Document document;
        private PdfListStore store;
        private CellRendererPage page_renderer;
        private PageSelectionMode page_selection_mode = PageSelectionMode.None;
        private bool highlighted;

        public PdfListStore Store { get { return store; } }
        public bool CanZoomIn { get; private set; }
        public bool CanZoomOut { get; private set; }

        public IEnumerable<Page> SelectedPages {
            get {
                var pages = new List<Page> ();
                foreach (var path in SelectedItems) {
                    TreeIter iter;
                    store.GetIter (out iter, path);
                    pages.Add (store.GetValue (iter, PdfListStore.PageColumn) as Page);
                }
                pages.Sort ((a, b) => { return a.Index < b.Index ? -1 : 1; });
                return pages;
            }
        }

        public event System.Action ZoomChanged;

        public PdfIconView (PdfMod app) : base ()
        {
            this.app = app;

            TooltipColumn = PdfListStore.TooltipColumn;
            SelectionMode = SelectionMode.Multiple;
            ColumnSpacing = RowSpacing = Margin;
            Model = store = new PdfListStore ();
            CanZoomIn = CanZoomOut = true;
            Reorderable = false;
            Spacing = 0;

            page_renderer = new CellRendererPage (this);
            PackStart (page_renderer, true);
            AddAttribute (page_renderer, "page", PdfListStore.PageColumn);

            // TODO enable uri-list as drag source target for drag-out-of-pdfmod-to-extract feature
            EnableModelDragSource (Gdk.ModifierType.None, new TargetEntry [] { move_internal_target, move_external_target, uri_src_target }, Gdk.DragAction.Default | Gdk.DragAction.Move);
            EnableModelDragDest (new TargetEntry [] { move_internal_target, move_external_target, uri_dest_target }, Gdk.DragAction.Default | Gdk.DragAction.Move);

            SizeAllocated += HandleSizeAllocated;
            PopupMenu += HandlePopupMenu;
            ButtonPressEvent += HandleButtonPressEvent;
            SelectionChanged += HandleSelectionChanged;
            DragDataReceived += HandleDragDataReceived;
            DragDataGet += HandleDragDataGet;
            DragLeave += HandleDragLeave;
        }

        public override void Dispose ()
        {
            page_renderer.Dispose ();
            base.Dispose ();
        }

        #region Gtk.Widget event handlers/overrides

        protected override bool OnScrollEvent (Gdk.EventScroll evnt)
        {
            if ((evnt.State & Gdk.ModifierType.ControlMask) != 0) {
                Zoom (evnt.Direction == ScrollDirection.Down ? -20 : 20);
                return true;
            } else {
                return base.OnScrollEvent (evnt);
            }
        }

        private void HandleSizeAllocated (object o, EventArgs args)
        {
            if (!zoom_manually_set) {
                ZoomFit ();
            }
        }

        private void HandleSelectionChanged (object o, EventArgs args)
        {
            if (!refreshing_selection) {
                page_selection_mode = PageSelectionMode.None;
            }
        }

        private void HandleButtonPressEvent (object o, ButtonPressEventArgs args)
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

        private void HandlePopupMenu (object o, PopupMenuArgs args)
        {
            app.GlobalActions["PageContextMenuAction"].Activate ();
        }

        #endregion

        #region Drag and Drop event handling

        private void HandleDragLeave (object o, DragLeaveArgs args)
        {
            if (highlighted) {
                Gtk.Drag.Unhighlight (this);
                highlighted = false;
            }
            args.RetVal = true;
        }

        protected override bool OnDragMotion (Gdk.DragContext context, int x, int y, uint time_)
        {
            // Scroll if within 20 pixels of the top or bottom
            var parent = Parent as Gtk.ScrolledWindow;
            if (y < 20) {
                parent.Vadjustment.Value -= 30;
            } else if ((parent.Allocation.Height - y) < 20) {
                parent.Vadjustment.Value = Math.Min (parent.Vadjustment.Upper - parent.Allocation.Height, parent.Vadjustment.Value + 30);
            }

            var targets = context.Targets.Select (t => (string)t);

            if (targets.Contains (move_internal_target.Target) || targets.Contains (move_external_target.Target)) {
                bool ret = base.OnDragMotion (context, x, y, time_);
                SetDestInfo (x, y);
                return ret;
            } else if (targets.Contains (uri_dest_target.Target)) {
                // TODO could do this (from Gtk+ docs) to make sure the uris are all .pdfs (or mime-sniffed as pdfs):
                /* If the decision whether the drop will be accepted or rejected can't be made based solely on the
                   cursor position and the type of the data, the handler may inspect the dragged data by calling gtk_drag_get_data() and
                   defer the gdk_drag_status() call to the "drag-data-received" handler. Note that you cannot not pass GTK_DEST_DEFAULT_DROP,
                   GTK_DEST_DEFAULT_MOTION or GTK_DEST_DEFAULT_ALL to gtk_drag_dest_set() when using the drag-motion signal that way. */
                Gdk.Drag.Status (context, DragAction.Copy, time_);
                if (!highlighted) {
                    Gtk.Drag.Highlight (this);
                    highlighted = true;
                }

                SetDestInfo (x, y);

                return true;
            }

            Gdk.Drag.Abort (context, time_);
            return false;
        }

        private void SetDestInfo (int x, int y)
        {
            TreePath path;
            IconViewDropPosition pos;
            GetCorrectedPathAndPosition (x, y, out path, out pos);
            SetDragDestItem (path, pos);
        }

        private void HandleDragDataGet(object o, DragDataGetArgs args)
        {
            if (args.Info == move_internal_target.Info) {
                var pages = new Hyena.Gui.DragDropList<Page> ();
                pages.AddRange (SelectedPages);
                pages.AssignToSelection (args.SelectionData, Gdk.Atom.Intern (move_internal_target.Target, false));
                args.RetVal = true;
            } else if (args.Info == move_external_target.Info) {
                string doc_and_pages = String.Format ("{0}{1}{2}", document.CurrentStateUri, newline[0], String.Join (",", SelectedPages.Select (p => p.Index.ToString ()).ToArray ()));
                byte [] data = System.Text.Encoding.UTF8.GetBytes (doc_and_pages);
                args.SelectionData.Set (Gdk.Atom.Intern (move_external_target.Target, false), 8, data);
                args.RetVal = true;
            } else if (args.Info == uri_src_target.Info) {
                // TODO implement page extraction via DnD?
                Console.WriteLine ("HandleDragDataGet, wants a uri list...");
            }
        }

        private void GetCorrectedPathAndPosition (int x, int y, out TreePath path, out IconViewDropPosition pos)
        {
            GetDestItemAtPos (x, y, out path, out pos);

            // Convert drop above/below/into into DropLeft or DropRight based on the x coordinate
            if (path != null && (pos == IconViewDropPosition.DropAbove || pos == IconViewDropPosition.DropBelow || pos == IconViewDropPosition.DropInto)) {
                if (!path.Equals (GetPathAtPos (x + ItemWidth/2, y))) {
                    pos = IconViewDropPosition.DropRight;
                } else {
                    pos = IconViewDropPosition.DropLeft;
                }
            }
        }

        private int GetDropIndex (int x, int y)
        {
            TreePath path;
            TreeIter iter;
            IconViewDropPosition pos;
            GetCorrectedPathAndPosition (x, y, out path, out pos);
            if (path == null) {
                return -1;
            }

            store.GetIter (out iter, path);
            if (TreeIter.Zero.Equals (iter))
                return -1;

            var to_index = (store.GetValue (iter, PdfListStore.PageColumn) as Page).Index;
            if (pos == IconViewDropPosition.DropRight) {
                to_index++;
            }

            return to_index;
        }

        private static string [] newline = new string [] { "\r\n" };
        private void HandleDragDataReceived (object o, DragDataReceivedArgs args)
        {
            args.RetVal = false;
            string target = (string)args.SelectionData.Target;
            if (target == move_internal_target.Target) {
                // Move pages within the document
                int to_index = GetDropIndex (args.X, args.Y);
                if (to_index < 0)
                    return;

                var pages = args.SelectionData.Data as Hyena.Gui.DragDropList<Page>;
                to_index -= pages.Count (p => p.Index < to_index);
                var action = new MoveAction (document, pages, to_index);
                action.Do ();
                app.GlobalActions.UndoManager.AddUndoAction (action);
                args.RetVal = true;
            } else if (target == move_external_target.Target) {
                int to_index = GetDropIndex (args.X, args.Y);
                if (to_index < 0)
                    return;

                string doc_and_pages = System.Text.Encoding.UTF8.GetString (args.SelectionData.Data);
                var pieces = doc_and_pages.Split (newline, StringSplitOptions.RemoveEmptyEntries);
                string uri = pieces[0];
                int [] pages = pieces[1].Split (',').Select (p => Int32.Parse (p)).ToArray ();

                document.AddFromUri (new Uri (uri), to_index, pages);
                args.RetVal = true;
            } else if (target == uri_src_target.Target) {
                var uris = System.Text.Encoding.UTF8.GetString (args.SelectionData.Data).Split (newline, StringSplitOptions.RemoveEmptyEntries);
                if (uris.Length == 1 && app.Document == null) {
                    app.LoadPath (uris[0]);
                    args.RetVal = true;
                } else {
                    int to_index = GetDropIndex (args.X, args.Y);
                    if (to_index < 0)
                        return;
                    // TODO somehow ask user for which pages of the docs to insert?
                    // TODO pwd handling - keyring#?
                    // TODO make action/undoable
                    foreach (var uri in uris) {
                        document.AddFromUri (new Uri (uri), to_index);
                    }
                    args.RetVal = true;
                }
            }

            Gtk.Drag.Finish (args.Context, (bool)args.RetVal, false, args.Time);
        }

        #endregion

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
            GrabFocus ();
        }

        private void OnPagesAdded (int index, Page [] pages)
        {
            foreach (var page in pages) {
                store.InsertWithValues (index, store.GetValuesForPage (page));
            }

            UpdateAllPages ();
            Refresh ();
        }

        private void OnPagesChanged (Page [] pages)
        {
            /*foreach (var page in pages) {
                var iter = store.GetIterForPage (page);
                if (!TreeIter.Zero.Equals (iter)) {
                    store.EmitRowChanged (store.GetPath (iter), iter);
                }
            }*/

            Refresh ();
        }

        private void OnPagesRemoved (Page [] pages)
        {
            foreach (var page in pages) {
                var iter = store.GetIterForPage (page);
                if (!TreeIter.Zero.Equals (iter)) {
                    store.Remove (ref iter);
                }
            }

            UpdateAllPages ();
            Refresh ();
        }

        private void OnPagesMoved ()
        {
            UpdateAllPages ();
            Refresh ();
        }

        private void Refresh ()
        {
            if (!zoom_manually_set) {
                ZoomFit ();
            }
            RefreshSelection ();
        }

        private void UpdateAllPages ()
        {
            foreach (var page in document.Pages) {
                var iter = store.GetIterForPage (page);
                if (!TreeIter.Zero.Equals (iter)) {
                    store.UpdateForPage (iter, page);
                    store.EmitRowChanged (store.GetPath (iter), iter);
                }
            }
        }

        #endregion

        private bool zoom_manually_set;
        public void Zoom (int pixels)
        {
            CanZoomIn = CanZoomOut = true;

            if (!zoom_manually_set) {
                zoom_manually_set = true;
                (app.GlobalActions["ZoomFitAction"] as ToggleAction).Active = false;
            }

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

            ItemWidth  = best_width;
            CanZoomOut = ItemWidth > MIN_WIDTH;
            CanZoomIn  = ItemWidth < MAX_WIDTH;

            var handler = ZoomChanged;
            if (handler != null) {
                handler ();
            }
        }

        #region Selection

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

        #endregion
    }
}
