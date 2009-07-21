
using System;

using Mono.Posix;
using Gtk;

using Hyena;
using Hyena.Gui;

namespace PdfMod
{
    public class GlobalActions : HyenaActionGroup
    {
        private Gtk.Window window;

        public GlobalActions (Gtk.Window window, ActionManager action_manager) : base (action_manager, "Global")
        {
            this.window = window;

            AddImportant (
                new ActionEntry ("OpenAction", Gtk.Stock.Open, OnOpen),
                new ActionEntry ("SaveAction", Gtk.Stock.Save, OnOpen),
                new ActionEntry ("SaveAsAction", Gtk.Stock.SaveAs, OnOpen)
            );

            AddUiFromFile ("UIManager.xml");
            Register ();
        }

        private void OnOpen (object o, EventArgs args)
        {
            var chooser = new Gtk.FileChooserDialog (Catalog.GetString ("Select PDF"), window, FileChooserAction.Open);
            chooser.AddFilter (GtkUtilities.GetFileFilter ("PDFs", new string [] {"pdf"}));
            chooser.AddFilter (GtkUtilities.GetFileFilter (Catalog.GetString ("All Formats"), new string [] {"*"}));
            chooser.SelectMultiple = false;
            chooser.AddButton (Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton (Stock.Open, ResponseType.Ok);
            chooser.DefaultResponse = ResponseType.Ok;

            if (chooser.Run () == (int)ResponseType.Ok) {
                Log.DebugFormat ("Opening {0}", chooser.Uri);
                //System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo ("pdfmod", dialog.Uri));
            }
            
            chooser.Destroy ();
        }
    }
}
