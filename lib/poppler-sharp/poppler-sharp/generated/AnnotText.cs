// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace Poppler {

	using System;
	using System.Collections;
	using System.Runtime.InteropServices;

#region Autogenerated code
	public class AnnotText : GLib.Object {

		[Obsolete]
		protected AnnotText(GLib.GType gtype) : base(gtype) {}
		public AnnotText(IntPtr raw) : base(raw) {}

		protected AnnotText() : base(IntPtr.Zero)
		{
			CreateNativeObject (new string [0], new GLib.Value [0]);
		}

		[DllImport("poppler-glib")]
		static extern IntPtr poppler_annot_text_get_icon(IntPtr raw);

		public string Icon { 
			get {
				IntPtr raw_ret = poppler_annot_text_get_icon(Handle);
				string ret = GLib.Marshaller.PtrToStringGFree(raw_ret);
				return ret;
			}
		}

		[DllImport("poppler-glib")]
		static extern IntPtr poppler_annot_text_get_type();

		public static new GLib.GType GType { 
			get {
				IntPtr raw_ret = poppler_annot_text_get_type();
				GLib.GType ret = new GLib.GType(raw_ret);
				return ret;
			}
		}

		[DllImport("poppler-glib")]
		static extern int poppler_annot_text_get_state(IntPtr raw);

		public Poppler.AnnotTextState State { 
			get {
				int raw_ret = poppler_annot_text_get_state(Handle);
				Poppler.AnnotTextState ret = (Poppler.AnnotTextState) raw_ret;
				return ret;
			}
		}

		[DllImport("poppler-glib")]
		static extern bool poppler_annot_text_get_is_open(IntPtr raw);

		public bool IsOpen { 
			get {
				bool raw_ret = poppler_annot_text_get_is_open(Handle);
				bool ret = raw_ret;
				return ret;
			}
		}

#endregion
	}
}
