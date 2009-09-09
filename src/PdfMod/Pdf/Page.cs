// Copyright (C) 2009 Novell, Inc.
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

using PdfSharp.Pdf;

namespace PdfMod.Pdf
{
    public class Page
    {
        internal PdfPage Pdf { get; set; }

        public Document Document { get; internal set; }
        public int Index { get; internal set; }
        public bool SurfaceDirty { get; internal set; }

        public Page (PdfPage pdf_page)
        {
            Pdf = pdf_page;
        }
    }
}
