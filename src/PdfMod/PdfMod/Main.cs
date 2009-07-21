using System;

using Mono.Posix;
using Gtk;

using Hyena;
using Hyena.Gui;

using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfMod
{
    public class PdfMod
    {
        public static void Main (string[] args)
        {
            new PdfMod ();
        }

        private ActionManager action_manager;
        private GlobalActions global_actions;
        private Window win;
        private Gtk.Toolbar header_toolbar;
        private Gtk.Statusbar status_bar;
        private PdfIconView icon_view;

        private PdfMod ()
        {
            ThreadAssist.InitializeMainThread ();
            ThreadAssist.ProxyToMainHandler = RunIdle;

            Hyena.Log.Debugging = true;
            Hyena.Log.DebugFormat ("Starting PdfMod");

            Application.Init ();
            win = new Gtk.Window (WindowType.Toplevel);
            win.DeleteEvent += delegate(object o, DeleteEventArgs args) {
                Application.Quit ();
                args.RetVal = true;
            };

            // ActionManager
            action_manager = new Hyena.Gui.ActionManager ();
            global_actions = new GlobalActions (win, action_manager);

            // Toolbar
            header_toolbar = action_manager.UIManager.GetWidget ("/HeaderToolbar") as Gtk.Toolbar;
            header_toolbar.ShowArrow = false;
            header_toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;

            var uri = "/home/gabe/Projects/PdfMod/bin/Debug/test.pdf";
            
            /* document.Info.Title = "PDFsharp XGraphic Sample";
            document.Info.Author = "Stefan Lange";
            document.Info.Subject = "Created with code snippets that show the use of graphical functions";
            document.Info.Keywords = "PDFsharp, XGraphics"; */

            // PDF Icon View
            icon_view = new PdfIconView ();
            var icon_view_sw = new Gtk.ScrolledWindow ();
            icon_view_sw.Child = icon_view;

            // Status bar
            status_bar = new Gtk.Statusbar () { HasResizeGrip = true };
            
            //doc.Pages.RemoveAt (3);
            //doc.Save ("test2.pdf");
            //var g2 = PdfSharp.Drawing.XGraphics.FromPdfPage (doc.Pages[0]);

            var vbox = new VBox ();
            vbox.PackStart (header_toolbar, false, true, 0);
            vbox.PackStart (icon_view_sw, true, true, 0);
            vbox.PackStart (status_bar, false, true, 0);
            win.Add (vbox);

            RunIdle (delegate { LoadUri (uri); });

            win.ShowAll ();
            Application.Run ();
        }

        private void LoadUri (string uri)
        {
            var ctx_id = status_bar.GetContextId ("loading");
            var msg_id = status_bar.Push (1, String.Format (Catalog.GetString ("Loading {0}"), GLib.Markup.EscapeText (uri)));
            try {
                var doc = PdfSharp.Pdf.IO.PdfReader.Open (uri, PdfDocumentOpenMode.Modify);
                icon_view.SetDocument (doc, uri);
                win.Title = System.IO.Path.GetFileNameWithoutExtension (uri);
            } catch (Exception e) {
                Hyena.Log.Exception (e);
                Hyena.Log.Error (
                    Catalog.GetString ("Error Loading PDF"),
                    String.Format (Catalog.GetString ("There was an error loading {0}"), GLib.Markup.EscapeText (uri ?? ""))
                );
            } finally {
                status_bar.Remove (ctx_id, msg_id);
            }
        }

        private void RunIdle (InvokeHandler handler)
        {
            GLib.Idle.Add (delegate { handler (); return false; });
        }
    }
}