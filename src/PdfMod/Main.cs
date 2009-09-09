// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Łukasz Jernaś
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

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

            // TODO could have a command line client here by checking CommandLine.Contains ("--headless") or something,
            // and implementing a subclass of Core.Client that does the rotate/remove/extract actions
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
