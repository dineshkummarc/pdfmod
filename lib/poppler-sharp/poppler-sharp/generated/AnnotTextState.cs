// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace Poppler {

	using System;
	using System.Runtime.InteropServices;

#region Autogenerated code
	[GLib.GType (typeof (Poppler.AnnotTextStateGType))]
	public enum AnnotTextState {

		Marked,
		Unmarked,
		Accepted,
		Rejected,
		Cancelled,
		Completed,
		None,
		Unknown,
	}

	internal class AnnotTextStateGType {
		[DllImport ("poppler-glib")]
		static extern IntPtr poppler_annot_text_state_get_type ();

		public static GLib.GType GType {
			get {
				return new GLib.GType (poppler_annot_text_state_get_type ());
			}
		}
	}
#endregion
}
