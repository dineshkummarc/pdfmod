
using System;

using Gtk;

using PdfSharp.Pdf;

namespace PdfMod
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class PdfIconView : Gtk.IconView
    {
        private PdfListStore store;
        private int columns = 2;
        
        public PdfIconView () : base ()
        {
            Model = store = new PdfListStore ();
            PixbufColumn = PdfListStore.PixbufColumn;
            MarkupColumn = PdfListStore.MarkupColumn;

            ColumnSpacing = RowSpacing = 12;
            UpdateItemWidth ();
            Reorderable = true;
            SelectionMode = SelectionMode.Multiple;
            
            SizeAllocated += (o, a) => {
                UpdateItemWidth ();
            };

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

        private void HandleDragDataReceived (object o, DragDataReceivedArgs args)
        {
            TreePath path;
            IconViewDropPosition pos;
            GetDestItemAtPos (args.X, args.Y, out path, out pos);
            Console.WriteLine ("DragDataReceived: {0}", pos);
        }


        // CreateDragIcon(TreePath) : Gdk.Pixmap
        
        
        public int Columns {
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
        
        public void SetDocument (PdfDocument doc, string uri)
        {
            store.SetDocument (doc, uri);
        }
    }
}
