
using System;
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
                Pdf.Info.Title = value;
                StartSaveTempTimeout ();
            }
        }

        public string Author {
            get { return Pdf.Info.Author; }
            set { Pdf.Info.Author = value; StartSaveTempTimeout (); }
        }

        public string Keywords {
            get { return Pdf.Info.Keywords; }
            set { Pdf.Info.Keywords = value; StartSaveTempTimeout (); }
        }

        public string Subject {
            get { return Pdf.Info.Subject; }
            set { Pdf.Info.Subject = value; StartSaveTempTimeout (); }
        }

        public string Filename {
            get { return System.IO.Path.GetFileName (SuggestedSavePath); }
        }

        public event System.Action Changed;
        public event Action<int, Page[]> PagesMoved;
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
                tmp_path = new Uri (uri).AbsolutePath;
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

        public void Move (int to_index, Page [] move_pages)
        {
            for (int i = 0, new_index = to_index; i < move_pages.Length; i++, new_index++) {
                var page = move_pages[i];
                int old_index = pages.IndexOf (page);

                // Move it in our list of Pages
                pages.Remove (page);
                pages.Insert (new_index, page);

                // Move it in the actual document
                pdf_document.Pages.MovePage (old_index, new_index);
            }

            Reindex ();
            SaveTemp ();

            var handler = PagesMoved;
            if (handler != null) {
                handler (to_index, move_pages);
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
            get { return poppler_doc ?? (poppler_doc = Poppler.Document.NewFromFile (tmp_uri ?? Uri, password ?? "")); }
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
            } catch (Exception e) {
                Log.Exception ("Failed to save tmp document", e);
            } finally {
                OnChanged ();
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
