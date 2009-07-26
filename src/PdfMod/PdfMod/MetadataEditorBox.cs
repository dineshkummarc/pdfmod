
using System;

using Mono.Unix;
using Gtk;

namespace PdfMod
{
    public class MetadataEditorBox : EventBox
    {
        private PdfMod app;
        private Table table;
        private Button revert_button;
        private Document doc;

        private TextProperty [] properties;
        private TextProperty title, author, keywords, subject;

        public MetadataEditorBox (PdfMod app)
        {
            this.app = app;
            app.DocumentLoaded += HandleDocumentLoaded;

            table = new Table (2, 5, false) {
                RowSpacing = 6,
                ColumnSpacing = 6,
                BorderWidth = 6
            };
            Child = table;

            BuildEditor ();
            BuildButtons ();

            KeyPressEvent += (o, args) => {
                if (args.Event.Key == Gdk.Key.Escape) {
                    Hide ();
                }
            };

            UpdateSensitivity ();
            ShowAll ();
        }

        private void BuildEditor ()
        {
            title    = new TextProperty (Catalog.GetString ("_Title:"),    t => doc.Title = t);
            author   = new TextProperty (Catalog.GetString ("_Author:"),   t => doc.Author = t);
            keywords = new TextProperty (Catalog.GetString ("_Keywords:"), t => doc.Keywords = t);
            subject  = new TextProperty (Catalog.GetString ("_Subject:"),  t => doc.Subject = t);

            properties = new TextProperty [] { title, author, keywords, subject };

            uint row = 0, column = 0;
            foreach (var property in properties) {
                table.Attach (property.Label, column++, column, row, row + 1, AttachOptions.Fill, 0, 0, 0);
                table.Attach (property.Entry, column++, column, row, row + 1, AttachOptions.Fill | AttachOptions.Expand, 0, 0, 0);

                if (column == 4) {
                    column = 0;
                    row++;
                }

                property.Entry.Changed += delegate { UpdateSensitivity (); };
            }
        }

        private void BuildButtons ()
        {
            revert_button = new Hyena.Widgets.ImageButton (Catalog.GetString ("_Revert Properties"), "revert") {
                TooltipText = Catalog.GetString ("Change the document's properties back to the original values")
            };
            revert_button.Clicked += HandleRevert;


            var close_button = new Hyena.Widgets.ImageButton (Catalog.GetString ("_Close"), Gtk.Stock.Close);
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

            bool have_changes = false;
            foreach (var prop in properties) {
                have_changes |= prop.HasChanges;
            }
            revert_button.Sensitive = have_changes;
        }

        #region Event handlers

        private void HandleDocumentLoaded (object o, EventArgs e)
        {
            doc = app.Document;
            var pdf = app.Document.Pdf;
            var info = pdf.Info;
            
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

            title.SetDefault (doc.Title);
            author.SetDefault (doc.Author);
            keywords.SetDefault (doc.Keywords);
            subject.SetDefault (doc.Subject);
            UpdateSensitivity ();
        }

        private void HandleRevert (object o, EventArgs args)
        {
            foreach (var prop in properties) {
                prop.Revert ();
            }
            revert_button.Sensitive = false;
        }

        #endregion

        #region Gtk.Widget overrides

        public new void Hide ()
        {
            (app.GlobalActions["PropertiesAction"] as Gtk.ToggleAction).Active = false;
            base.Hide ();
            app.IconView.GrabFocus ();
        }

        public new void GrabFocus ()
        {
            title.Entry.GrabFocus ();
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
                Entry.Changed += (o, a) => {
                    on_updated (Entry.Text);
                };
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
                Entry.Text = initial_value ?? "";
                undo_adapter.UndoManager.Clear ();
            }

            public bool HasChanges { get { return undo_adapter.UndoManager.CanUndo; } }
        }
    }
}
