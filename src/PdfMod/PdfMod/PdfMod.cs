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
        private static int app_count = 0;

        public static void Main (string[] args)
        {
            ThreadAssist.InitializeMainThread ();
            ThreadAssist.ProxyToMainHandler = RunIdle;

            Hyena.Log.Debugging = true;
            Hyena.Log.DebugFormat ("Starting PdfMod");

            Application.Init ();

            var app = new PdfMod ();
            RunIdle (app.LoadFiles);

            Application.Run ();
        }

        private ActionManager action_manager;
        private GlobalActions global_actions;
        private MenuBar menu_bar;
        private Gtk.Toolbar header_toolbar;
        
        public Gtk.Statusbar StatusBar { get; private set; }
        public Gtk.Window Window { get; private set; }
        public PdfIconView IconView { get; private set; }
        public UndoManager UndoManager { get; private set; }
        public PdfDocument Document { get; private set; }
        public string DocumentUri { get; private set; }

        public event EventHandler DocumentChanged;

        public PdfMod ()
        {
            app_count++;

            Window = new Gtk.Window (WindowType.Toplevel);
            Window.DeleteEvent += delegate(object o, DeleteEventArgs args) {
                args.RetVal = Quit ();
            };

            // PDF Icon View
            IconView = new PdfIconView ();
            var IconView_sw = new Gtk.ScrolledWindow ();
            IconView_sw.Child = IconView;

            // Status bar
            StatusBar = new Gtk.Statusbar () { HasResizeGrip = true };

            UndoManager = new Hyena.UndoManager ();

            // ActionManager
            action_manager = new Hyena.Gui.ActionManager ();
            Window.AddAccelGroup (action_manager.UIManager.AccelGroup);
            global_actions = new GlobalActions (this, action_manager);

            // Menubar
            menu_bar = action_manager.UIManager.GetWidget ("/MainMenu") as MenuBar;

            // Toolbar
            header_toolbar = action_manager.UIManager.GetWidget ("/HeaderToolbar") as Gtk.Toolbar;
            header_toolbar.ShowArrow = false;
            header_toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;

            var vbox = new VBox ();
            vbox.PackStart (menu_bar, false, false, 0);
            vbox.PackStart (header_toolbar, false, true, 0);
            vbox.PackStart (IconView_sw, true, true, 0);
            vbox.PackStart (StatusBar, false, true, 0);
            Window.Add (vbox);

            Window.ShowAll ();
        }

        public bool Quit ()
        {
            if (--app_count == 0) {
                Application.Quit ();
                return true;
            }

            return false;
        }

        private void LoadFiles ()
        {
            var files = ApplicationContext.CommandLine.Files;
            if (files.Count > 1) {
                // Make sure the user wants to open N windows

                // Even though always {0} will always be > 1, we use GetPluralString
                // because of languages with different pluralization patterns.
                /*String.Format (Catalog.GetPluralString (
                    "You are opening {0} document, which will open {0} window.  Continue?",
                    "You are opening {0} documents, which will open {0} windows.  Continue?", files.Count),
                     files.Count);*/
                //"Cancel" "Open First" "Open All"
            }

            /*foreach (string file in files) {
                new PdfMod ().LoadUri (file);
            }*/

            var uri = "/home/gabe/Projects/PdfMod/bin/Debug/test.pdf";
            //LoadUri (uri);
        }

        public void LoadUri (string uri)
        {
            var ctx_id = StatusBar.GetContextId ("loading");
            var msg_id = StatusBar.Push (1, String.Format (Catalog.GetString ("Loading {0}"), GLib.Markup.EscapeText (uri)));

            try {
                DocumentUri = uri;
                Document = PdfSharp.Pdf.IO.PdfReader.Open (uri, PdfDocumentOpenMode.Modify);
                IconView.SetDocument (Document, uri);

                var filename = System.IO.Path.GetFileNameWithoutExtension (uri);
                if (Document.Info == null || String.IsNullOrEmpty (Document.Info.Title)) {
                    Window.Title = filename;
                } else {
                    Window.Title = String.Format ("{0} ({1})", GLib.Markup.EscapeText (Document.Info.Title), filename);
                }

                var handler = DocumentChanged;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            } catch (Exception e) {
                DocumentUri = null;
                Document = null;
                Hyena.Log.Exception (e);
                Hyena.Log.Error (
                    Catalog.GetString ("Error Loading PDF"),
                    String.Format (Catalog.GetString ("There was an error loading {0}"), GLib.Markup.EscapeText (uri ?? "")), true
                );
            } finally {
                StatusBar.Remove (ctx_id, msg_id);
            }
        }

        public static void RunIdle (InvokeHandler handler)
        {
            GLib.Idle.Add (delegate { handler (); return false; });
        }
    }
}