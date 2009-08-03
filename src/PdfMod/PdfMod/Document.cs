
using System;
using System.Linq;
using System.Collections.Generic;

using Hyena;

using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfMod
{
    public class Document : IDisposable
    {
        private PdfDocument pdf_document;
        private List<Page> pages = new List<Page> ();
        private string password;
        private string tmp_path;
        private string tmp_uri;
        internal string CurrentStateUri { get { return tmp_uri ?? Uri; } }

        public string SuggestedSavePath { get; set; }
        public string Uri { get; private set; }
        public string Path { get; private set; }
        public PdfDocument Pdf { get { return pdf_document; } }
        public int Count { get { return pages.Count; } }

        public IEnumerable<Page> Pages {
            get {
                foreach (var page in pages)
                    yield return page;
            }
        }

        public Page this [int index] {
            get { return pages[index]; }
        }

        public string Title {
            get {
                var title = Pdf.Info.Title;
                return String.IsNullOrEmpty (title) ? null : title;
            }
            set {
                if (value.Trim () == "")
                    value = null;

                if (value == Title)
                    return;

                Pdf.Info.Title = value;
                StartSaveTempTimeout ();
            }
        }

        public string Author {
            get { return Pdf.Info.Author; }
            set {
                if (value.Trim () == "")
                    value = null;

                if (value == Author)
                    return;

                Pdf.Info.Author = value;
                StartSaveTempTimeout ();
            }
        }

        public string Keywords {
            get { return Pdf.Info.Keywords; }
            set {
                if (value.Trim () == "")
                    value = null;

                if (value == Keywords)
                    return;

                Pdf.Info.Keywords = value;
                StartSaveTempTimeout ();
            }
        }

        public string Subject {
            get { return Pdf.Info.Subject; }
            set {
                if (value.Trim () == "")
                    value = null;

                if (value == Subject)
                    return;

                Pdf.Info.Subject = value;
                StartSaveTempTimeout ();
            }
        }

        public string Filename {
            get { return System.IO.Path.GetFileName (SuggestedSavePath); }
        }

        public event System.Action Changed;
        public event System.Action PagesMoved;
        public event Action<Page []> PagesRemoved;
        public event Action<int, Page []> PagesAdded;
        public event Action<Page []> PagesChanged;

        public Document (string uri, string password) : this (uri, password, false)
        {
        }

        public Document (string uri, string password, bool isAlreadyTmp)
        {
            if (isAlreadyTmp) {
                tmp_uri = new Uri (uri).AbsoluteUri;
                tmp_path = new Uri (uri).LocalPath;
            }

            var uri_obj = new Uri (uri);
            Uri = uri_obj.AbsoluteUri;
            SuggestedSavePath = Path = uri_obj.LocalPath;

            this.password = password;

            pdf_document = PdfSharp.Pdf.IO.PdfReader.Open (Path, password, PdfDocumentOpenMode.Modify | PdfDocumentOpenMode.Import);
            for (int i = 0; i < pdf_document.PageCount; i++) {
                var page = new Page (pdf_document.Pages[i]) {
                    Document = this,
                    Index = i
                };
                pages.Add (page);
            }

            ExpireThumbnails (pages);
            OnChanged ();
        }

        public bool HasUnsavedChanged {
            get { return tmp_uri != null || save_timeout_id != 0; }
        }

        public long FileSize {
            get { return new System.IO.FileInfo (tmp_path ?? Path).Length; }
        }

        public void Dispose ()
        {
            if (pdf_document != null) {
                pdf_document.Dispose ();
                pdf_document = null;
            }

            if (tmp_path != null) {
                System.IO.File.Delete (tmp_path);
                tmp_path = tmp_uri = null;
            }
        }

        public IEnumerable<Page> FindPagesMatching (string text)
        {
            using (var doc = Poppler.Document.NewFromFile (tmp_uri ?? Uri, password ?? "")) {
                for (int i = 0; i < doc.NPages; i++) {
                    using (var page = doc.GetPage (i)) {
                        var list = page.FindText (text);
                        if (list != null && list.Count > 0) {
                            yield return pages[i];
                            list.Dispose ();
                        }
                    }
                }
            }
        }

        public void Move (int to_index, Page [] move_pages, int [] new_indexes)
        {
            // Remove all the pages
            foreach (var page in move_pages) {
                pages.Remove (page);
            }

            if (new_indexes == null) {
                new_indexes = move_pages.Select (p => to_index++).ToArray ();
            }

            // Add back at the right index
            for (int i = 0; i < move_pages.Length; i++) {
                pages.Insert (new_indexes[i], move_pages[i]);
            }

            // Do the actual move in the document
            foreach (var page in move_pages) {
                pdf_document.Pages.MovePage (page.Index, IndexOf (page));
            }

            Reindex ();

            SaveTemp ();

            var handler = PagesMoved;
            if (handler != null) {
                handler ();
            }
        }

        public int IndexOf (Page page)
        {
            return pages.IndexOf (page);
        }

        public void Remove (params Page [] remove_pages)
        {
            foreach (var page in remove_pages) {
                pdf_document.Pages.Remove (page.Pdf);
                pages.Remove (page);
            }

            Reindex ();
            SaveTemp ();

            var handler = PagesRemoved;
            if (handler != null) {
                handler (remove_pages);
            }
        }

        public void Rotate (Page [] rotate_pages, int rotate_by)
        {
            foreach (var page in rotate_pages) {
                page.Pdf.Rotate += rotate_by;
            }

            OnPagesChanged (rotate_pages);
        }

        public void Save (string uri)
        {
            Pdf.Save (uri);
            Log.DebugFormat ("Saved to {0}", uri);
            Uri = uri;

            if (tmp_uri != null) {
                try {
                    System.IO.File.Delete (tmp_path);
                } catch (Exception e) {
                    Log.Exception ("Couldn't delete tmp file after saving", e);
                } finally {
                    tmp_uri = tmp_path = null;
                }
            }
        }

        public void AddFromUri (Uri uri)
        {
            AddFromUri (uri, 0);
        }

        public void AddFromUri (Uri uri, int to_index)
        {
            AddFromUri (uri, to_index, null);
        }

        public void AddFromUri (Uri uri, int to_index, int [] pages_to_import)
        {
            Log.DebugFormat ("Inserting pages from {0} to index {1}", uri, to_index);
            using (var doc = PdfSharp.Pdf.IO.PdfReader.Open (uri.LocalPath, null, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import)) {
                var pages = new List<Page> ();
                for (int i = 0; i < doc.PageCount; i++) {
                    if (pages_to_import == null || pages_to_import.Contains (i)) {
                        pages.Add (new Page (doc.Pages [i]));
                    }
                }
                Add (to_index, pages.ToArray ());
                to_index += pages.Count;
            }
        }

        public void Add (int to_index, params Page [] add_pages)
        {
            int i = to_index;
            foreach (var page in add_pages) {
                var pdf = pdf_document.Pages.Insert (i, page.Pdf);
                page.Pdf = pdf;
                page.Document = this;
                pages.Insert (i, page);
                i++;
            }

            Reindex ();
            SaveTemp ();
            ExpireThumbnails (add_pages);

            var handler = PagesAdded;
            if (handler != null) {
                handler (to_index, add_pages);
            }
        }

        private Poppler.Document poppler_doc;
        private Poppler.Document PopplerDoc {
            get {
                if (poppler_doc == null) {
                    poppler_doc = Poppler.Document.NewFromFile (tmp_uri ?? Uri, password ?? "");
                }
                return poppler_doc;
            }
        }

        private void ExpireThumbnails (IEnumerable<Page> update_pages)
        {
            if (poppler_doc != null) {
                poppler_doc.Dispose ();
                poppler_doc = null;
            }

            foreach (var page in update_pages) {
                page.SurfaceDirty = true;
            }
        }

        public PageThumbnail GetSurface (Page page, int w, int h)
        {
            if (w < PdfIconView.MIN_WIDTH || h < PdfIconView.MIN_WIDTH) {
                return null;
            }

            using (var ppage = PopplerDoc.GetPage (page.Index)) {
                double pw, ph;
                ppage.GetSize (out pw, out ph);
                double scale = Math.Min (w / pw, h / ph);

                var surface = new Cairo.ImageSurface (Cairo.Format.Argb32, (int)(scale * pw), (int)(scale * ph));
                var cr = new Cairo.Context (surface);
                cr.Scale (scale, scale);
                ppage.Render (cr);
                page.SurfaceDirty = false;
                return new PageThumbnail () { Surface = surface, Context = cr };
            }
        }

        private void Reindex ()
        {
            for (int i = 0; i < pages.Count; i++) {
                pages[i].Index = i;
            }
        }

        private uint save_timeout_id = 0;
        private void StartSaveTempTimeout ()
        {
            if (save_timeout_id != 0) {
                GLib.Source.Remove (save_timeout_id);
            }
            
            save_timeout_id = GLib.Timeout.Add (100, OnSaveTempTimeout);
        }

        private bool OnSaveTempTimeout ()
        {
            save_timeout_id = 0;
            SaveTemp ();
            return false;
        }

        private void SaveTemp ()
        {
            try {
                if (tmp_path == null) {
                    tmp_path = PdfMod.GetTmpFilename ();
                    if (System.IO.File.Exists (tmp_path)) {
                        System.IO.File.Delete (tmp_path);
                    }
                    tmp_uri = new Uri (tmp_path).AbsoluteUri;
                }

                pdf_document.Save (tmp_path);
                Log.DebugFormat ("Saved tmp file to {0}", tmp_path);
                OnChanged ();
            } catch (Exception e) {
                Log.Exception ("Failed to save tmp document", e);
                // TODO tell user, shutdown
            }
        }

        private void OnPagesChanged (Page [] changed_pages)
        {
            Reindex ();
            SaveTemp ();
            ExpireThumbnails (changed_pages);

            var handler = PagesChanged;
            if (handler != null) {
                handler (changed_pages);
            }
            OnChanged ();
        }

        private void OnChanged ()
        {
            var handler = Changed;
            if (handler != null) {
                handler ();
            }
        }
    }
}
