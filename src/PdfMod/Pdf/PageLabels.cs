// Copyright (C) 2009 Michael McKinley
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
using System.Linq;
using System.Text;
using System.Collections.Generic;

using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;

namespace PdfMod.Pdf
{
    struct PageLabelFormat
    {
        public string number_style;
        public string prefix;
        public int first_number;
    }

    public class PageLabels
    {
        const string name_labels = "/PageLabels";
        const string name_numtree = "/Nums";

        // Keys (PdfNames) for formatting attributes
        const string name_fmt = "/S";
        const string name_start_at = "/St";
        const string name_prefix = "/P";

        // Possible values for the numbering style
        const string alpha_upper = "/A";
        const string alpha_lower = "/a";
        const string roman_upper = "/R";
        const string roman_lower = "/r";
        const string arabic = "/D";

        SortedDictionary<int, PageLabelFormat> page_labels;
        PdfDictionary.DictionaryElements pdf_elements;
        PdfDocument pdf_document;
        bool edited;

        public string this[Page page] { get { return this[page.Index]; } }

        public string this[int index] {
            get {
                if (index < 0 || index > pdf_document.PageCount) {
                    throw new IndexOutOfRangeException ();
                }

                if (page_labels.Count == 0) {
                    return null;
                }

                int range_base = GetFormat (index);
                try {
                    PageLabelFormat cur_format = page_labels[range_base];
                    string label = cur_format.prefix;

                    // Restart numbering for each range of pages
                    int vindex = index + cur_format.first_number - range_base;

                    if (cur_format.number_style == roman_upper || cur_format.number_style == alpha_upper) {
                        label += RenderVal (vindex, cur_format.number_style).ToUpper ();
                    } else {
                        label += RenderVal (vindex, cur_format.number_style).ToLower ();
                    }
                    return label;
                } catch (KeyNotFoundException) {
                }

                return null;
            }
        }

        internal PageLabels (PdfDocument document)
        {
            page_labels = new SortedDictionary<int, PageLabelFormat> ();
            pdf_elements = document.Internals.Catalog.Elements;
            pdf_document = document;
            edited = false;

            // Ignore documents that don't have labelling stuff defined
            if (!pdf_elements.ContainsKey (name_labels)) {
                return;
            }

            // Ignore documents that don't have a properly-defined PageLabelFmt section
            PdfDictionary my_labels = pdf_elements.GetDictionary (name_labels);
            if (!my_labels.Elements.ContainsKey (name_numtree)) {
                return;
            }

            /* The number tree (not my term) is a PdfArray arranged as follows: [##, dict, ##, dict, ##, dict ...]
             * ## represents the starting index of the page (0-based) and the following dict is a PdfDictionary
             * containing formatting information regarding the range
             */

            PdfArray number_tree = my_labels.Elements.GetArray (name_numtree);

            for (int i = 0; i < number_tree.Elements.Count / 2; ++i) {
                Console.WriteLine ("Range # {0}", i);
                PageLabelFormat temp_label = new PageLabelFormat ();

                int range_start = number_tree.Elements.GetInteger (i * 2);
                PdfDictionary label_data = number_tree.Elements.GetDictionary (i * 2 + 1);

                // Set the prefix, default to ""
                if (label_data.Elements.ContainsKey (name_prefix)) {
                    temp_label.prefix = label_data.Elements.GetString (name_prefix);
                } else {
                    temp_label.prefix = "";
                }

                // Set the start number, default to 1
                if (label_data.Elements.ContainsKey (name_start_at)) {
                    temp_label.first_number = label_data.Elements.GetInteger (name_start_at);
                } else {
                    temp_label.first_number = 1;
                }

                // Set the format type, default to no numbering (only show the prefix)
                if (label_data.Elements.ContainsKey (name_fmt)) {
                    temp_label.number_style = label_data.Elements.GetString (name_fmt);
                } else {
                    temp_label.number_style = "";
                }

                page_labels.Add (range_start, temp_label);
            }
        }

        // Determine which formatting rules apply to page index.  Returns the start of the formatting range
        int GetFormat (int index)
        {
            // TODO: find the correct range using a binary search

            int last = -1;
            foreach (int range_start in page_labels.Keys) {
                if (range_start > index) {
                    break;
                }
                last = range_start;
            }

            return last;
        }

        // Render the value index in the proper format (case-agnostic)
        string RenderVal (int index, string fmt)
        {
            if (arabic == fmt) {
                return index.ToString ();
            } else if (roman_upper == fmt || roman_lower == fmt) {
                return ToRoman (index);
            } else if (alpha_lower == fmt || alpha_upper == fmt) {
                return ToAlpha (index);
            } else {
                return "";
            }
        }

        // Convert val into Roman numerals
        string ToRoman (int val)
        {
            StringBuilder roman_val = new StringBuilder ();
            // TODO: see if there's a more elegant conversion

            if (val >= 1000) {
                roman_val.Append ('M', val / 1000);
                val -= (1000 * (val / 1000));
            }
            if (val >= 900) {
                roman_val.Append ("CM");
                val -= 900;
            }
            if (val >= 500) {
                roman_val.Append ('D', val / 500);
                val -= (500 * (val / 500));
            }
            if (val >= 400) {
                roman_val.Append ("CD");
                val -= 400;
            }
            if (val >= 100) {
                roman_val.Append ('C', val / 100);
                val -= (100 * (val / 100));
            }
            if (val >= 90) {
                roman_val.Append ("XC");
                val -= 90;
            }
            if (val >= 50) {
                roman_val.Append ('L', val / 50);
                val -= (50 * (val / 50));
            }
            if (val >= 40) {
                roman_val.Append ("XL");
                val -= 40;
            }
            if (val >= 10) {
                roman_val.Append ('X', val / 10);
                val -= (10 * (val / 10));
            }
            if (val >= 9) {
                roman_val.Append ("IX");
                val -= 9;
            }
            if (val >= 5) {
                roman_val.Append ('V', val / 5);
                val -= (5 * (val / 5));
            }
            if (val >= 4) {
                roman_val.Append ("IV");
                val -= 4;
            }
            roman_val.Append ('I', val);
            return roman_val.ToString ();
        }

        // Convert val into the alpha representation. 1 -> a, 2 -> b, ... 26 -> z, 27 -> aa, 28 -> bb, etc.
        string ToAlpha (int val)
        {
            char letter = (char)((val - 1) % 26 + 'a');
            int rep_count = (val - 1)/26 + 1;
            StringBuilder s = new StringBuilder (rep_count);
            s.Append (letter, rep_count);
            return s.ToString ();
        }

        // Write labels to the PDF
        internal void WriteLabels ()
        {
            if (!edited) {
                return;
            }

            // Grab the labels element, creating it if necessary
            PdfDictionary labels_dict;
            if (!pdf_elements.ContainsKey (name_labels)) {
                labels_dict = new PdfDictionary (pdf_document);
                pdf_elements.Add (name_labels, labels_dict);
            } else {
                labels_dict = pdf_elements.GetDictionary (name_labels);
            }
            labels_dict.Elements.Clear ();

            // Create the number tree
            PdfArray number_tree = new PdfArray (pdf_document);

            // Add the range-start, attrib-dict pairs
            foreach (int range_start in page_labels.Keys)
            {
                number_tree.Elements.Add (new PdfInteger (range_start));
                PageLabelFormat label_format = page_labels[range_start];
                PdfDictionary r_attribs = new PdfDictionary (pdf_document);

                if (label_format.number_style.Length > 0) {
                    r_attribs.Elements.Add (name_fmt, new PdfName (label_format.number_style));
                }
                if (label_format.first_number > 1) {
                    r_attribs.Elements.Add (name_start_at, new PdfInteger (label_format.first_number));
                }
                if (label_format.prefix.Length > 0) {
                    r_attribs.Elements.Add (name_prefix, new PdfString (label_format.prefix));
                }
                number_tree.Elements.Add (r_attribs);
            }
            labels_dict.Elements.Add (name_numtree, number_tree);
        }
    }
}
