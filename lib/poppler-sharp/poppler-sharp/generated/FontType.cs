// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace Poppler {

	using System;
	using System.Runtime.InteropServices;

#region Autogenerated code
	[GLib.GType (typeof (Poppler.FontTypeGType))]
	public enum FontType {

		Unknown,
		Type1,
		Type1c,
		Type1cot,
		Type3,
		Truetype,
		Truetypeot,
		CidType0,
		CidType0c,
		CidType0cot,
		CidType2,
		CidType2ot,
	}

	internal class FontTypeGType {
		[DllImport ("poppler-glib")]
		static extern IntPtr poppler_font_type_get_type ();

		public static GLib.GType GType {
			get {
				return new GLib.GType (poppler_font_type_get_type ());
			}
		}
	}
#endregion
}
