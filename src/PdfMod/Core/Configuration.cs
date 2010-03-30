// Copyright (C) 2009-2010 Novell, Inc.
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

namespace PdfMod.Core
{    
    public class Configuration
    {
        GConf.Client client = new GConf.Client ();
        string ns = "/apps/pdfmod/";

        public Configuration()
        {
        }

        T Get<T> (string key, T fallback)
        {
            try {
                return (T) client.Get (ns + key);
            } catch {
                return fallback;
            }
        }

        void Set<T> (string key, T val)
        {
            client.Set (ns + key, val);
        }

        public bool ShowToolbar {
            get { return Get<bool> ("show_toolbar", true); }
            set { Set<bool> ("show_toolbar", value); }
        }

        public string LastOpenFolder {
            get { return Get<string> ("last_folder", System.Environment.GetFolderPath (System.Environment.SpecialFolder.Desktop)); }
            set {
                if (value != null && value.StartsWith ("file:/") && !value.StartsWith ("file://")) {
                    value = "file://" + value.Substring (6);
                }

                Set<string> ("last_folder", value);
            }
        }
    }
}
