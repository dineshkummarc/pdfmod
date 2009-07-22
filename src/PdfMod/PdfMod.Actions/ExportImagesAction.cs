//
// Authors:
//   PDFsharp Team (mailto:PDFsharpSupport@pdfsharp.de)
//
// Copyright (c) 2005-2008 empira Software GmbH, Cologne (Germany)
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

namespace PdfMod.Actions
{
    // From the PDFSharp example, samples/Samples C#/Based on GDI+/ExportImages/Program.cs
    public class ExportImagesAction
    {
        /*public ExportImagesAction()
        {
            int imageCount = 0;
            // Iterate pages
            foreach (PdfPage page in document.Pages) {
                // Get resources dictionary
                PdfDictionary resources = page.Elements.GetDictionary("/Resources");
                if (resources != null) {
                    // Get external objects dictionary
                    PdfDictionary xObjects = resources.Elements.GetDictionary("/XObject");
                    if (xObjects != null){
                        PdfItem[] items = xObjects.Elements.Values;
                        // Iterate references to external objects
                        foreach (PdfItem item in items) {
                            PdfReference reference = item as PdfReference;
                            if (reference != null) {
                                PdfDictionary xObject = reference.Value as PdfDictionary;
                                // Is external object an image?
                                if (xObject != null && xObject.Elements.GetString("/Subtype") == "/Image") {
                                ExportImage(xObject, ref imageCount);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Currently extracts only JPEG images.
        /// </summary>
        static void ExportImage(PdfDictionary image, ref int count)
        {
          string filter = image.Elements.GetName("/Filter");
          switch (filter)
          {
            case "/DCTDecode":
              ExportJpegImage(image, ref count);
              break;
    
            case "/FlateDecode":
              ExportAsPngImage(image, ref count);
              break;
          }
        }
    
        /// <summary>
        /// Exports a JPEG image.
        /// </summary>
        static void ExportJpegImage(PdfDictionary image, ref int count)
        {
          // Fortunately JPEG has native support in PDF and exporting an image is just writing the stream to a file.
          byte[] stream = image.Stream.Value;
          FileStream fs = new FileStream(String.Format("Image{0}.jpeg", count++), FileMode.Create, FileAccess.Write);
          BinaryWriter bw = new BinaryWriter(fs);
          bw.Write(stream);
          bw.Close();
        }*/
    }
}
