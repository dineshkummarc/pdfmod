
using System;

namespace PdfMod
{    
    public class Configuration
    {
        private GConf.Client client = new GConf.Client ();
        private string ns = "/apps/pdfmod/";

        public Configuration()
        {
        }

        private T Get<T> (string key, T fallback)
        {
            try {
                return (T) client.Get (ns + key);
            } catch {
                return fallback;
            }
        }

        private void Set<T> (string key, T val)
        {
            client.Set (ns + key, val);
        }

        public bool ShowToolbar {
            get { return Get<bool> ("show_toolbar", true); }
            set { Set<bool> ("show_toolbar", value); }
        }

        public string LastOpenFolder {
            get { return Get<string> ("last_folder", System.Environment.GetFolderPath (System.Environment.SpecialFolder.Desktop)); }
            set { Set<string> ("last_folder", value); }
        }
    }
}