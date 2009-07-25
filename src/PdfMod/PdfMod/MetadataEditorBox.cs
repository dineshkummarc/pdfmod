
using System;

using Mono.Unix;
using Gtk;

namespace PdfMod
{
    public class MetadataEditorBox : EventBox
    {
        private PdfMod app;
        private VBox vbox;
        private Table table;
        private PdfSharp.Pdf.PdfDocumentInformation info;

        private TextProperty [] properties;
        private TextProperty title, author, keywords, subject;

        public MetadataEditorBox (PdfMod app)
        {
            this.app = app;
            app.DocumentLoaded += HandleDocumentLoaded;

            vbox = new VBox ();
            vbox.BorderWidth = 6;
            Child = vbox;

            table = new Table (2, 5, false) {
                RowSpacing = 6,
                ColumnSpacing = 6
            };
            vbox.PackStart (table, true, true, 0);

            BuildEditor ();
            BuildButtons ();

            KeyPressEvent += delegate (object o, KeyPressEventArgs args) {
                if (args.Event.Key == Gdk.Key.Escape) {
                    Hide ();
                }
            };

            UpdateSensitivity ();
            ShowAll ();
        }

        private void BuildEditor ()
        {
            title = new TextProperty (Catalog.GetString ("_Title:"), t => {
                info.Title = t;
                app.UpdateTitle ();
            });

            author   = new TextProperty (Catalog.GetString ("_Author:"),   t => info.Author = t);
            keywords = new TextProperty (Catalog.GetString ("_Keywords:"), t => info.Keywords = t);
            subject  = new TextProperty (Catalog.GetString ("_Subject:"),  t => info.Subject = t);

            properties = new TextProperty [] { title, author, keywords, subject };

            uint row = 0, column = 0;
            foreach (var property in properties) {
                table.Attach (property.Label, column++, column, row, row + 1, AttachOptions.Fill, 0, 0, 0);
                table.Attach (property.Entry, column++, column, row, row + 1, AttachOptions.Fill | AttachOptions.Expand, 0, 0, 0);

                if (column == 4) {
                    column = 0;
                    row++;
                }
            }
        }

        private void BuildButtons ()
        {
            var revert_button = new Hyena.Widgets.ImageButton (Catalog.GetString ("Revert Properties"), "revert");
            revert_button.TooltipText = Catalog.GetString ("Change the document's properties back to the original values");
            revert_button.Clicked += HandleRevert;

            var close_button = new Hyena.Widgets.ImageButton (Catalog.GetString ("Close"), Gtk.Stock.Close);
            close_button.TooltipText = Catalog.GetString ("Hide the document's properties");
            close_button.Clicked += delegate {
                Hide ();
            };

            table.Attach (revert_button, 4, 5, 0, 1, AttachOptions.Fill, 0, 0, 0);
            table.Attach (close_button, 4, 5, 1, 2, AttachOptions.Fill, 0, 0, 0);
        }

        private void UpdateSensitivity ()
        {
            Sensitive = app.Document != null;
        }

        #region Event handlers

        private void HandleDocumentLoaded(object sender, EventArgs e)
        {
            UpdateSensitivity ();
            var pdf = app.Document.Pdf;
            info = pdf.Info;
            
            Console.WriteLine ("Author           = {0}", info.Author);
            Console.WriteLine ("CreationDate     = {0}", info.CreationDate);
            Console.WriteLine ("Creator          = {0}", info.Creator);
            Console.WriteLine ("Keywords         = {0}", info.Keywords);
            Console.WriteLine ("ModificationDate = {0}", info.ModificationDate);
            Console.WriteLine ("Producer         = {0}", info.Producer);
            Console.WriteLine ("Subject          = {0}", info.Subject);
            Console.WriteLine ("Title            = {0}", info.Title);
            Console.WriteLine ("Page Layout      = {0}", pdf.PageLayout);
            Console.WriteLine ("Page Mode        = {0}", pdf.PageMode);
            Console.WriteLine ("SecurityLevel    = {0}", pdf.SecuritySettings.DocumentSecurityLevel);
            //Console.WriteLine ("OwnerPassword    = {0}", pdf.SecuritySettings.OwnerPassword);
            //Console.WriteLine ("UserPassword     = {0}", pdf.SecuritySettings.UserPassword);
            Console.WriteLine ("Settings.TrimMgns= {0}", pdf.Settings.TrimMargins);
            Console.WriteLine ("Version          = {0}", pdf.Version);
            Console.WriteLine ("# Outlines       = {0}", pdf.Outlines.Count);
            Console.WriteLine ("NoCompression    = {0}", pdf.Options.NoCompression);
            //Console.WriteLine ("CompressionMode  = {0}", pdf.CustomValues.CompressionMode);

            var prefs = pdf.ViewerPreferences;
            Console.WriteLine ("\nViewPreferences:");
            Console.WriteLine ("CenterWindow     = {0}", prefs.CenterWindow);
            Console.WriteLine ("Direction        = {0}", prefs.Direction);
            Console.WriteLine ("DisplayDocTitle  = {0}", prefs.DisplayDocTitle);
            Console.WriteLine ("FitWindow        = {0}", prefs.FitWindow);
            Console.WriteLine ("HideMenubar      = {0}", prefs.HideMenubar);
            Console.WriteLine ("HideToolbar      = {0}", prefs.HideToolbar);
            Console.WriteLine ("HideWindowUI     = {0}", prefs.HideWindowUI);

            title.SetDefault (info.Title);
            author.SetDefault (info.Author);
            keywords.SetDefault (info.Keywords);
            subject.SetDefault (info.Subject);
        }

        private void HandleRevert (object o, EventArgs args)
        {
            foreach (var prop in properties) {
                prop.Revert ();
            }
        }

        #endregion

        #region Gtk.Widget overrides

        public new void Hide ()
        {
            (app.GlobalActions["PropertiesAction"] as Gtk.ToggleAction).Active = false;
            base.Hide ();
            app.IconView.GrabFocus ();
        }

        #endregion

        private class TextProperty
        {
            public Entry Entry { get; private set; }
            public Label Label { get; private set; }
            private string initial_value;

            private Hyena.Gui.EditableUndoAdapter<Entry> undo_adapter;
            private Action<string> on_updated;

            public TextProperty (string label, Action<string> onUpdated)
            {
                this.on_updated = onUpdated;

                Entry = new Entry ();
                Entry.Changed += (o, a) => { on_updated (Entry.Text); };
                undo_adapter = new Hyena.Gui.EditableUndoAdapter<Entry> (Entry);
                undo_adapter.Connect ();
                Label = new Label (label) {
                    MnemonicWidget = Entry,
                    Xalign = 1.0f
                };
            }

            public void SetDefault (string default_value)
            {
                this.initial_value = default_value;
                Entry.Text = default_value;
                undo_adapter.UndoManager.Clear ();
            }

            public void Revert ()
            {
                Entry.Text = initial_value;
                undo_adapter.UndoManager.Clear ();
            }
        }
    }
}
