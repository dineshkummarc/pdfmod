
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
            
            Uri = new Uri (uri).AbsoluteUri;
            SuggestedSavePath = Path = new Uri (uri).AbsolutePath;

            this.password = password;

            pdf_document = PdfSharp.Pdf.IO.PdfReader.Open (Path, password, PdfDocumentOpenMode.Modify | PdfDocumentOpenMode.Import);
            for (int i = 0; i < pdf_document.PageCount; i++) {
                var page = new Page (pdf_document.Pages[i]) {
                    Document = this,
                    Index = i
                };
                pages.Add (page);
            }

            UpdateThumbnails (pages);
        }

        public bool HasUnsavedChanged {
            get { return tmp_uri != null; }
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
                        if (list != null) {
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
                pdf_document.Pages.RemoveAt (pages.IndexOf (page));
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

            OnChanged (rotate_pages);
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
                page.Document = this;
                pages.Insert (i, page);
                pdf_document.Pages.Insert (i, page.Pdf);
                i++;
            }

            Reindex ();
            SaveTemp ();
            UpdateThumbnails (add_pages);

            var handler = PagesAdded;
            if (handler != null) {
                handler (to_index, add_pages);
            }
        }

        private const int SIZE = 256;
        private void UpdateThumbnails (IEnumerable<Page> update_pages)
        {
            Console.WriteLine ("Trying to load thumbs for {0}", tmp_uri ?? Uri);
            using (var doc = Poppler.Document.NewFromFile (tmp_uri ?? Uri, password ?? "")) {
                foreach (var page in update_pages) {
                    using (var pop_page = doc.GetPage (IndexOf (page))) {
                        // TODO try to use/get the embedded thumbnail?
                        double w, h;
                        pop_page.GetSize (out w, out h);
                        double scale = SIZE / Math.Max (w, h);
        
                        int thumb_w = (int) Math.Ceiling (w * scale);
                        int thumb_h = (int) Math.Ceiling (h * scale);
                        page.Pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, false, 8, thumb_w, thumb_h);
                        pop_page.RenderToPixbuf (0, 0, (int)w, (int)h, scale, 0, page.Pixbuf);
                        Console.WriteLine ("Updated icon for {0}", page.Index);
                    }
                }
            }
        }

        private void Reindex ()
        {
            for (int i = 0; i < pages.Count; i++) {
                pages[i].Index = i;
            }
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
            }
        }

        private void OnChanged (Page [] changed_pages)
        {
            Reindex ();
            SaveTemp ();
            UpdateThumbnails (changed_pages);

            var handler = PagesChanged;
            if (handler != null) {
                handler (changed_pages);
            }
        }
    }
}
