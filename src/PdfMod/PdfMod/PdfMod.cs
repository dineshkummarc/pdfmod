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
        private static readonly string CacheDir = System.IO.Path.Combine (System.Environment.GetFolderPath (System.Environment.SpecialFolder.ApplicationData), "pdfmod");

        public static void Main (string[] args)
        {
            ThreadAssist.InitializeMainThread ();
            ThreadAssist.ProxyToMainHandler = RunIdle;

            Hyena.Log.Debugging = true;
            Hyena.Log.DebugFormat ("Starting PdfMod");

            Application.Init ();

            try {
                System.IO.Directory.CreateDirectory (CacheDir);
            } catch (Exception e) {
                Log.Exception (String.Format ("Unable to create cache directory: {0}", CacheDir), e);
            }

            var app = new PdfMod ();
            RunIdle (app.LoadFiles);

            Application.Run ();
        }

        private MenuBar menu_bar;
        private Gtk.Toolbar header_toolbar;

        public ActionManager ActionManager { get; private set; }
        public GlobalActions GlobalActions { get; private set; }
        public Gtk.Statusbar StatusBar { get; private set; }
        public Gtk.Window Window { get; private set; }
        public PdfIconView IconView { get; private set; }
        public Document Document { get; private set; }

        public event EventHandler DocumentChanged;

        public PdfMod ()
        {
            app_count++;

            Window = new Gtk.Window (WindowType.Toplevel);
            Window.Title = Catalog.GetString ("PDF Mod");
            Window.SetSizeRequest (640, 480);
            Window.DeleteEvent += delegate(object o, DeleteEventArgs args) {
                Quit ();
                args.RetVal = true;
            };

            // PDF Icon View
            IconView = new PdfIconView (this);
            var IconView_sw = new Gtk.ScrolledWindow ();
            IconView_sw.Child = IconView;

            // Status bar
            StatusBar = new Gtk.Statusbar () { HasResizeGrip = true };

            // ActionManager
            ActionManager = new Hyena.Gui.ActionManager ();
            Window.AddAccelGroup (ActionManager.UIManager.AccelGroup);
            GlobalActions = new GlobalActions (this, ActionManager);

            // Menubar
            menu_bar = ActionManager.UIManager.GetWidget ("/MainMenu") as MenuBar;

            // Toolbar
            header_toolbar = ActionManager.UIManager.GetWidget ("/HeaderToolbar") as Gtk.Toolbar;
            header_toolbar.ShowArrow = false;
            header_toolbar.ToolbarStyle = ToolbarStyle.Icons;
            header_toolbar.Tooltips = true;

            var vbox = new VBox ();
            vbox.PackStart (menu_bar, false, false, 0);
            vbox.PackStart (header_toolbar, false, true, 0);
            vbox.PackStart (IconView_sw, true, true, 0);
            vbox.PackStart (StatusBar, false, true, 0);
            Window.Add (vbox);

            Window.ShowAll ();
        }

        public void Quit ()
        {
            if (Window == null) {
                return;
            }

            if (PromptIfUnsavedChanges ()) {
                return;
            }

            if (Document != null) {
                Document.Dispose ();
            }

            Window.Destroy ();
            Window = null;

            if (--app_count == 0) {
                Application.Quit ();
            }
        }

        private bool PromptIfUnsavedChanges ()
        {
            if (Document != null && Document.HasUnsavedChanged) {
                var message_dialog = new Hyena.Widgets.HigMessageDialog (
                    Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.None,
                    Catalog.GetString ("Save the changes made to this document?"),
                    String.Empty
                );
                message_dialog.AddButton (Catalog.GetString ("Close _Without Saving"), ResponseType.Close, false);
                message_dialog.AddButton (Stock.Cancel, ResponseType.Cancel, false);
                message_dialog.AddButton (Stock.SaveAs, ResponseType.Ok, true);

                var response = (ResponseType) message_dialog.Run ();
                message_dialog.Destroy ();
                switch (response) {
                    case ResponseType.Ok:
                        GlobalActions["SaveAsAction"].Activate ();
                        return PromptIfUnsavedChanges ();
                    case ResponseType.Close:
                        return false;
                    case ResponseType.Cancel:
                    case ResponseType.DeleteEvent:
                        return true;
                }
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
            LoadPath (uri);
        }

        // TODO support password protected docs
        public void LoadPath (string path)
        {
            LoadPath (path, null);
        }

        public void LoadPath (string path, string suggestedFilename)
        {
            var ctx_id = StatusBar.GetContextId ("loading");
            var msg_id = StatusBar.Push (1, String.Format (Catalog.GetString ("Loading {0}"), GLib.Markup.EscapeText (path)));

            try {
                Document = new Document (path, null, suggestedFilename != null);
                if (suggestedFilename != null) {
                    Document.SuggestedSavePath = suggestedFilename;
                }
                IconView.SetDocument (Document);

                var filename = System.IO.Path.GetFileName (Document.SuggestedSavePath);
                if (Document.Pdf.Info == null || String.IsNullOrEmpty (Document.Pdf.Info.Title)) {
                    Window.Title = filename;
                } else {
                    Window.Title = String.Format ("{0} ({1})", GLib.Markup.EscapeText (Document.Pdf.Info.Title), filename);
                }

                var handler = DocumentChanged;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            } catch (Exception e) {
                Document = null;
                Hyena.Log.Exception (e);
                Hyena.Log.Error (
                    Catalog.GetString ("Error Loading PDF"),
                    String.Format (Catalog.GetString ("There was an error loading {0}"), GLib.Markup.EscapeText (path ?? "")), true
                );
            } finally {
                StatusBar.Remove (ctx_id, msg_id);
            }
        }

        public static string GetTmpFilename ()
        {
            string filename = null;
            int i = 0;
            while (filename == null) {
                filename = System.IO.Path.Combine (CacheDir, "tmpfile-" + i++);
                if (System.IO.File.Exists (filename)) {
                    filename = null;
                }
            }
            return filename;
        }

        public static void RunIdle (InvokeHandler handler)
        {
            GLib.Idle.Add (delegate { handler (); return false; });
        }
    }
}