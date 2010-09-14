//
// ExportImagesAction.cs
//
// Based on the PDFSharp example, "Based on GDI+/ExportImages/Program.cs"
//
// Authors:
//   PDFsharp Team (mailto:PDFsharpSupport@pdfsharp.de)
//
// Modified by:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2005-2008 empira Software GmbH, Cologne (Germany)
// Copyright (C) 2009 Novell, Inc.
//
// http://www.pdfsharp.com
// http://sourceforge.net/projects/pdfsharp
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using Mono.Unix;

using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;

namespace PdfMod.Pdf.Actions
{
    public class ExportImagesAction
    {
        List<ImageInfo> image_objects;
        int max_page_index;

        public ExportImagesAction (Document document, IEnumerable<Page> pages)
        {
            var page_list = pages.ToList ();
            max_page_index = page_list.Last ().Index;
            image_objects = GetImageObjectsFrom (page_list).ToList ();
        }

        public int ExportableImageCount {
            get { return image_objects.Count; }
        }

        public void Do (string to_path)
        {
            foreach (var img_obj in image_objects) {
                Export (img_obj, to_path);
            }
        }

        IEnumerable<ImageInfo> GetImageObjectsFrom (IEnumerable<Page> pages)
        {
            foreach (var page in pages) {
                var resources = page.Pdf.Elements.GetDictionary ("/Resources");
                if (resources == null)
                    continue;

                var x_objects = resources.Elements.GetDictionary ("/XObject");
                if (x_objects == null)
                    continue;

                int i = 0;
                var items = x_objects.Elements.Values.ToList ();
                foreach (var item in items) {
                    var reference = item as PdfReference;
                    if (reference == null)
                        continue;

                    var x_object = reference.Value as PdfDictionary;
                    // Put this in a variable to pass to GetString so that it's not pulled out as a translation string
                    var subtype = "/Subtype";
                    if (x_object != null && x_object.Elements.GetString (subtype) == "/Image") {
                        var img = new ImageInfo () { Page = page, ImageObject = x_object, PageIndex = i++, PageCount = items.Count };
                        if (IsExportable (img)) {
                            yield return img;
                        } else {
                            i--;
                        }
                    }
                }
            }
        }

        bool IsExportable (ImageInfo image)
        {
            var filter = image.ImageObject.Elements.GetName("/Filter");
            return filter == "/DCTDecode" || filter == "/FlateDecode";
        }

        /// <summary>
        /// Currently extracts only JPEG images.
        /// </summary>
        void Export (ImageInfo image, string to_path)
        {
            string filter = image.ImageObject.Elements.GetName("/Filter");
            switch (filter) {
                case "/DCTDecode":
                    ExportJpegImage (image, GetFilename (image, to_path, "jpg"));
                    break;
                case "/FlateDecode":
                    ExportAsPngImage (image, GetFilename (image, to_path, "png"));
                    break;
            }
        }

        string GetFilename (ImageInfo image, string to_path, string ext)
        {
            string name = image.ImageObject.Elements.GetName ("/Name");
            if (name == "/X") { name = null; }
            var name_fragment = String.IsNullOrEmpty (name) ? null : String.Format (" ({0})", name);
            var path = Path.Combine (
                to_path,
                Hyena.StringUtil.EscapeFilename (String.Format (
                    "{0} - {1}{2}.{3}",
                    String.Format (Catalog.GetString ("Page {0}"),
                        SortableNumberString (image.Page.Index + 1, max_page_index + 1)),
                    SortableNumberString (image.PageIndex + 1, image.PageCount),
                    name_fragment, ext
                ))
            );
            return path;
        }

        static string SortableNumberString (int num, int count)
        {
            var fmt = new StringBuilder ("{0:");
            int places = count.ToString ().Length;
            for (int i = 0; i < places; i++) {
                fmt.Append ('0');
            }
            fmt.Append ('}');

            return String.Format (fmt.ToString (), num);
        }

        /// <summary>
        /// Exports a JPEG image.
        /// </summary>
        static void ExportJpegImage (ImageInfo image, string path)
        {
            // Fortunately JPEG has native support in PDF and exporting an image is just writing the stream to a file.
            var fs = new FileStream (path, FileMode.Create, FileAccess.Write);

            byte[] stream = image.ImageObject.Stream.Value;
            using (var bw = new BinaryWriter (fs)) {
                bw.Write (stream);
            }
        }

        /// <summary>
        /// Exports image in PNG format.
        /// </summary>
        static void ExportAsPngImage (ImageInfo image, string path)
        {
            int width = image.ImageObject.Elements.GetInteger (PdfImage.Keys.Width);
            int height = image.ImageObject.Elements.GetInteger (PdfImage.Keys.Height);

            try {
                byte [] data = image.ImageObject.Stream.UnfilteredValue;
                using (var pixbuf = new Gdk.Pixbuf (data, Gdk.Colorspace.Rgb, false, 8, width, height, width*3)) {
                    pixbuf.Save (path, "png");
                }
            } catch (Exception e) {
                Hyena.Log.Exception ("Unable to load PNG from embedded PDF object", e);
            }
        }

        class ImageInfo {
            public Page Page { get; set; }
            public PdfDictionary ImageObject { get; set; }
            public int PageIndex { get; set; }
            public int PageCount { get; set; }
        }
    }
}
