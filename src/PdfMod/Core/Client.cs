using System;
using System.Collections.Generic;
using System.IO;

using Mono.Unix;

using Hyena;

using PdfMod.Pdf;

namespace PdfMod.Core
{
    public abstract class Client
    {
        private static readonly string old_cache_dir = Path.Combine (System.Environment.GetFolderPath (System.Environment.SpecialFolder.ApplicationData), "pdfmod");
        private static readonly string CacheDir = Path.Combine (XdgBaseDirectorySpec.GetUserDirectory ("XDG_CACHE_HOME", ".cache"), "pdfmod");

        public Document Document { get; protected set; }
        public static Configuration Configuration { get; private set; }

        public event EventHandler DocumentLoaded;

        static Client ()
        {
            Configuration = new Configuration ();
            InitCache ();
        }

        public Client ()
        {
        }

        protected void OnDocumentLoaded ()
        {
            var handler = DocumentLoaded;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        protected void LoadFiles ()
        {
            // This variable should probably be marked volatile
            Hyena.Log.Debugging = true;
            LoadFiles (ApplicationContext.CommandLine.Files);
        }

        protected abstract void LoadFiles (IList<string> files);

        public void LoadPath (string path)
        {
            LoadPath (path, null);
        }

        public abstract void LoadPath (string path, string suggestedFilename);

        private static void InitCache ()
        {
            // Remove the old "cache" dir that really ended up being ~/.config/
            if (Directory.Exists (old_cache_dir)) {
                try {
                    Directory.Delete (old_cache_dir);
                } catch {}
            }

            // Make sure the new one exists
            try {
                Directory.CreateDirectory (CacheDir);
                Log.DebugFormat ("Cache directory set to {0}", CacheDir);

                // Remove any tmp files that haven't been touched in three days
                var too_old = DateTime.Now;
                too_old.AddDays (-3);
                foreach (string file in Directory.GetFiles (CacheDir)) {
                    if (file.Contains ("tmpfile-")) {
                        if (File.GetLastAccessTime (file) < too_old) {
                            File.Delete (file);
                        }
                    }
                }
            } catch (Exception e) {
                Log.Exception (String.Format ("Unable to create cache directory: {0}", CacheDir), e);
            }
        }

        public static string GetTmpFilename ()
        {
            string filename = null;
            int i = 0;
            while (filename == null) {
                filename = Path.Combine (CacheDir, "tmpfile-" + i++);
                if (File.Exists (filename)) {
                    filename = null;
                }
            }
            return filename;
        }
    }
}
