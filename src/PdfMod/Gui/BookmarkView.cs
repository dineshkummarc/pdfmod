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
using PdfMod.Pdf.Actions;
using System.Collections.Generic;
using Mono.Unix;
using PdfSharp.Pdf.Advanced;

namespace PdfMod.Gui
{
    public class BookmarkView : VBox
    {
        Client app;
        TreeView tree_view;
        TreeStore model;
        Document document;

        public BookmarkView (Client app)
        {
            this.app = app;
            BuildTreeView ();
            BuildButtonBar ();

            WidthRequest = 200;
            Spacing = 6;
            ShowAll ();

            app.Actions["AddBookmark"].Activated += OnAdd;
            app.Actions["RenameBookmark"].Activated += OnRename;
            app.Actions["ChangeBookmarkDest"].Activated += OnChangeDest;
            app.Actions["RemoveBookmarks"].Activated += OnRemove;
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
            UpdateActions ();

            Visible = Client.Configuration.ShowBookmarks;
        }

        // Bookmark action handlers

        void OnAdd (object o, EventArgs args)
        {
            // Figure out if there's a parent to put it under
            TreeIter parent_iter = TreeIter.Zero;
            if (tree_view.Selection.CountSelectedRows () > 0) {
                TreePath parent_path;
                TreeViewColumn col;
                tree_view.GetCursor (out parent_path, out col);
                model.GetIter (out parent_iter, parent_path);
            }

            // Add it to the PDF document
            var outline = new PdfOutline (Catalog.GetString ("New bookmark"), document.Pages.First ().Pdf, true);
            if (!TreeIter.Zero.Equals (parent_iter)) {
                var parent = GetOutline (parent_iter);
                SetDestIndex (outline, GetDestIndex (parent) + 1);
                parent.Outlines.Add (outline);
            } else {
                document.Pdf.Outlines.Add (outline);
            }

            // Add it to our TreeView
            var iter = AddOutline (parent_iter, outline);

            // Make sure it is visible
            tree_view.ExpandToPath (model.GetPath (iter));

            MarkModified ();

            // Begin editing its name
            tree_view.SetCursor (model.GetPath (iter), tree_view.Columns[0], true);
            Hyena.Log.Debug ("Added bookmark");

            // Create an IUndo action so it can be undone
            var action = CreateAddRemoveAction (true, iter);
            action.Description = Catalog.GetString ("Add Bookmark");
            app.Actions.UndoManager.AddUndoAction (action);
        }

        void OnRename (object o, EventArgs args)
        {
            tree_view.SetCursor (tree_view.Selection.GetSelectedRows ().First (), tree_view.Columns[0], true);
        }

        void OnChangeDest (object o, EventArgs args)
        {
            tree_view.SetCursor (tree_view.Selection.GetSelectedRows ().First (), tree_view.Columns[1], true);
        }

        void OnRemove (object o, EventArgs args)
        {
            TreeIter iter;
            var parent_iters = tree_view.Selection
                                 .GetSelectedRows ()
                                 .Select (p => { model.GetIter (out iter, p); return iter; });
            var iters = parent_iters.Concat (parent_iters.SelectMany (i => model.IterChildrenOf (i, true))).Distinct ().ToArray ();

            var action = CreateAddRemoveAction (false, iters);
            // Translators: {0} is available for you to use; contains the number of bookmarks
            action.Description = String.Format (Catalog.GetPluralString ("Remove Bookmark", "Remove Bookmarks", iters.Length), iters.Length);
            action.Do ();
            app.Actions.UndoManager.AddUndoAction (action);
        }

        class ActionContext {
            public TreeIter Iter;
            public PdfOutline Bookmark;
            public PdfOutline Parent;
            // TODO Save/restore the precise position this bookmark was at beneath its parent
            public int Position;
        }

        TreeIter IterForBookmark (PdfOutline bookmark)
        {
            return model.IterFor (bookmark);
        }

        DelegateAction CreateAddRemoveAction (bool added, params TreeIter [] iters)
        {
            TreeIter iter;
            var items = iters.Select (i => {
                var parent = model.IterParent (out iter, i) ? GetOutline (iter) : null;
                var bookmark = GetOutline (i);
                return new ActionContext () {
                    Iter = i,
                    Bookmark = bookmark,
                    Parent = parent
                };
            }).ToList ();

            var add_action = new System.Action (() => {
                for (int i = 0; i < items.Count; i++) {
                    var item = items[i];
                    TreeIter parent_iter = TreeIter.Zero;
                    if (item.Parent != null) {
                        item.Parent.Outlines.Add (item.Bookmark);
                        parent_iter = IterForBookmark (item.Parent);
                    } else {
                        document.Pdf.Outlines.Add (item.Bookmark);
                    }

                    // Add it to our TreeView, and all its children
                    item.Iter = AddOutline (parent_iter, item.Bookmark);
                    tree_view.ExpandToPath (model.GetPath (item.Iter));
                    Hyena.Log.DebugFormat ("Added back bookmark '{0}'", item.Bookmark.Title);
                }
            });

            var remove_action = new System.Action (() => {
                items.Reverse ();
                foreach (var item in items) {
                    item.Bookmark.Remove ();
                    model.Remove (ref item.Iter);
                    Hyena.Log.DebugFormat ("Removed bookmark '{0}'", item.Bookmark.Title);
                }
                items.Reverse ();
            });

            return new DelegateAction (document) {
                UndoAction = delegate {
                    if (added) remove_action (); else add_action ();
                    RemoveModifiedMark ();
                },
                RedoAction = delegate {
                    if (added)  add_action (); else remove_action ();
                    MarkModified ();
                }
            };
        }

        // Document event handlers

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

            // Remove bookmarks that point to removed pages
            var to_remove = new List<TreeIter> ();
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

                Hyena.Log.DebugFormat ("Removed bookmark '{0}' since its page was removed", outline.Title);
            }

            UpdateModel ();
        }

        void OnPagesMoved ()
        {
            UpdateModel ();
        }

        // Widget construction utility methods

        void BuildTreeView ()
        {
            // outline, expanded/opened, title, page # destination, tooltip
            model = new TreeStore (typeof(PdfSharp.Pdf.PdfOutline), typeof(bool), typeof(string), typeof(int), typeof(string));

            tree_view = new BookmarkTreeView () {
                App = app,
                Model = model,
                SearchColumn = (int)ModelColumns.Title,
                TooltipColumn = (int)ModelColumns.Tooltip,
                EnableSearch = true,
                EnableTreeLines = true,
                HeadersVisible = false,
                Reorderable = false,
                ShowExpanders = true
            };
            tree_view.Selection.Mode = SelectionMode.Multiple;

            var title = new CellRendererText () {
                Editable = true,
                Ellipsize = Pango.EllipsizeMode.End
            };
            title.Edited += delegate(object o, EditedArgs args) {
                TreeIter iter;
                if (model.GetIterFromString (out iter, args.Path)) {
                    if (!String.IsNullOrEmpty (args.NewText)) {
                        var bookmark = GetOutline (iter);
                        string new_name = args.NewText;
                        string old_name = bookmark.Title;
                        var action = new DelegateAction (document) {
                            Description = Catalog.GetString ("Rename Bookmark"),
                            UndoAction = delegate {
                                var i = IterForBookmark (bookmark);
                                bookmark.Title = old_name;
                                model.SetValue (i, (int)ModelColumns.Title, bookmark.Title);
                                RemoveModifiedMark ();
                            },
                            RedoAction = delegate {
                                var i = IterForBookmark (bookmark);
                                bookmark.Title = new_name;
                                model.SetValue (i, (int)ModelColumns.Title, bookmark.Title);
                                MarkModified ();
                            }
                        };
                        action.Do ();
                        app.Actions.UndoManager.AddUndoAction (action);
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
                        int old_dest = GetDestIndex (bookmark);
                        int new_dest = i - 1;

                        var action = new DelegateAction (document) {
                            Description = Catalog.GetString ("Rename Bookmark"),
                            UndoAction = delegate {
                                SetDestIndex (bookmark, old_dest);
                                model.SetValue (iter, (int)ModelColumns.PageNumber, old_dest + 1);
                                RemoveModifiedMark ();
                            },
                            RedoAction = delegate {
                                SetDestIndex (bookmark, new_dest);
                                model.SetValue (iter, (int)ModelColumns.PageNumber, new_dest + 1);
                                MarkModified ();
                            }
                        };
                        action.Do ();
                        app.Actions.UndoManager.AddUndoAction (action);
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
                VscrollbarPolicy = PolicyType.Automatic
            };
            sw.AddWithViewport (tree_view);
            PackStart (sw, true, true, 0);
        }

        void BuildButtonBar ()
        {
            var box = new HBox () { Spacing = 6 };
            var add_action = app.Actions["AddBookmark"];
            var add_button = add_action.CreateImageButton ();

            var remove_action = app.Actions["RemoveBookmarks"];
            var remove_button = remove_action.CreateImageButton ();
            tree_view.Selection.Changed += (o, a) => UpdateActions ();
            UpdateActions ();

            box.PackStart (add_button, false, false, 0);
            box.PackStart (remove_button, false, false, 0);

            PackStart (box, false, false, 0);
        }

        string [] selection_actions = new string [] { "RenameBookmark", "RemoveBookmarks", "ChangeBookmarkDest" };
        void UpdateActions ()
        {
            int count = tree_view.Selection.CountSelectedRows ();
            bool have_doc_and_selection = count > 0 && document != null;
            foreach (var action in selection_actions) {
                app.Actions.UpdateAction (action, true, have_doc_and_selection);
            }
            // Translators: {0} is available for you to use; contains the number of bookmarks
            app.Actions["RemoveBookmarks"].Label = String.Format (Catalog.GetPluralString ("_Remove Bookmark", "_Remove Bookmarks", count), count);
        }

        void UpdateModel ()
        {
            model.Foreach ((m, path, iter) => {
                model.SetValues (iter, GetValuesFor (GetOutline (iter)));
                return false;
            });
            UpdateActions ();
        }

        void RemoveModifiedMark ()
        {
            document.UnsavedChanges--;
        }

        void MarkModified ()
        {
            document.UnsavedChanges++;
        }

        TreeIter AddOutline (TreeIter parent, PdfOutline outline)
        {
            return TreeIter.Zero.Equals (parent)
                ? model.AppendValues (GetValuesFor (outline))
                : model.AppendValues (parent, GetValuesFor (outline));
        }

        object [] GetValuesFor (PdfOutline outline)
        {
            int dest_num = GetDestIndex (outline);

            return new object [] { outline, outline.Opened, outline.Title, dest_num + 1,
                String.Format (Catalog.GetString ("Bookmark links to page {0}"), dest_num + 1) };
        }

        int GetDestIndex (PdfOutline outline)
        {
            if (outline.DestinationPage == null)
                return -1;
            else
                return document.Pages.Select (p => p.Pdf).IndexOf (outline.DestinationPage);
        }

        void SetDestIndex (PdfOutline outline, int i)
        {
            if (i >= 0 && i < document.Count) {
                outline.DestinationPage = document.Pages.Skip (i).First ().Pdf;
            }
        }

        void AddOutlineCollection (Document document, PdfOutline.PdfOutlineCollection outlines, TreeIter parent)
        {
            if (outlines != null) {
                foreach (PdfOutline outline in outlines) {
                    var iter = AddOutline (parent, outline);
                    if (outline.Opened) {
                        tree_view.ExpandRow (model.GetPath (iter), false);
                    }

                    // Recursively add this item's children, if any
                    AddOutlineCollection (document, outline.Outlines, iter);
                }
            }
        }

        PdfOutline GetOutline (TreeIter iter)
        {
            return model.Get<PdfOutline> (iter);
        }

        enum ModelColumns : int {
            Bookmark,
            IsExpanded,
            Title,
            PageNumber,
            Tooltip
        };

        class BookmarkTreeView : TreeView
        {
            public Client App;

            protected override bool OnButtonPressEvent (Gdk.EventButton press)
            {
                TreePath path;
                if (!GetPathAtPos ((int)press.X, (int)press.Y, out path)) {
                    Selection.UnselectAll ();
                }

                bool call_parent = true;
                if (press.Button == 3 && path != null && Selection.PathIsSelected (path)) {
                    // Calling the parent in this case would unselect any other items than
                    // this path, which we don't want to do - they should stay selected and the
                    // context menu should pop up.
                    call_parent = false;
                }

                bool ret = false;
                if (call_parent) {
                    ret = base.OnButtonPressEvent (press);
                }

                if (press.Button == 3) {
                    ret = OnPopupMenu ();
                }

                return ret;
            }

            protected override bool OnPopupMenu ()
            {
                App.Actions["BookmarkContextMenu"].Activate ();
                return true;
            }
        }
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

        public static T Get<T> (this TreeStore model, TreeIter iter)
        {
            // NOTE: assumes object is stored in model column 0
            return (T) model.GetValue (iter, 0);
        }

        public static TreeIter IterFor<T> (this TreeStore model, T item)
        {
            var iter = TreeIter.Zero;
            model.Foreach ((m, path, i) => {
                if (model.Get<T> (i).Equals (item)) {
                    iter = i;
                    return true;
                }
                return false;
            });
            return iter;
        }

        public static IEnumerable<T> ObjectChildrenOf<T> (this TreeStore model, TreeIter iter, bool recursive)
        {
            foreach (var child in IterChildrenOf (model, iter, recursive)) {
                yield return model.Get<T> (child);
            }
        }

        public static IEnumerable<TreeIter> IterChildrenOf (this TreeStore model, TreeIter iter, bool recursive)
        {
            TreeIter child;
            if (model.IterChildren (out child, iter)) {
                do {
                    yield return child;

                    if (recursive) {
                        foreach (var subchild in model.IterChildrenOf (child, recursive)) {
                            yield return subchild;
                        }
                    }
                } while (model.IterNext (ref child));
            }
        }
    }
}
