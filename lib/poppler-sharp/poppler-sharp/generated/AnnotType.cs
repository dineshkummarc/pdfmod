// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace Poppler {

	using System;
	using System.Runtime.InteropServices;

#region Autogenerated code
	[GLib.GType (typeof (Poppler.AnnotTypeGType))]
	public enum AnnotType {

		Unknown,
		Text,
		Link,
		FreeText,
		Line,
		Square,
		Circle,
		Polygon,
		PolyLine,
		Highlight,
		Underline,
		Squiggly,
		StrikeOut,
		Stamp,
		Caret,
		Ink,
		Popup,
		FileAttachment,
		Sound,
		Movie,
		Widget,
		Screen,
		PrinterMark,
		TrapNet,
		Watermark,
		ThreeD,
	}

	internal class AnnotTypeGType {
		[DllImport ("poppler-glib")]
		static extern IntPtr poppler_annot_type_get_type ();

		public static GLib.GType GType {
			get {
				return new GLib.GType (poppler_annot_type_get_type ());
			}
		}
	}
#endregion
}
