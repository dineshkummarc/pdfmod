using System;
using System.IO;

using Mono.Unix;
using Hyena;

namespace PdfMod
{
    public static class PdfMod
    {
        public static void Main (string[] args)
        {
            ApplicationContext.TrySetProcessName ("pdfmod");
            Gdk.Global.ProgramClass = "pdfmod";
            Log.Debugging = true;
            Log.DebugFormat ("Starting PdfMod");

            InitCatalog ("/usr/local/share/locale/", Core.Defines.PREFIX + "/share/locale/");

            new Gui.Client (true);
        }

        private static void InitCatalog (params string [] dirs)
        {
            foreach (var dir in dirs) {
                var test_file = Path.Combine (dir, "fr/LC_MESSAGES/pdfmod.mo");
                if (File.Exists (test_file)) {
                    Log.DebugFormat ("Initializing i18n catalog from {0}", dir);
                    Catalog.Init ("pdfmod", dir);
                    break;
                }
            }
        }
    }
}
