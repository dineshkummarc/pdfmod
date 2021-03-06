// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace Poppler {

	using System;
	using System.Collections;
	using System.Runtime.InteropServices;

#region Autogenerated code
	public class PSFile : GLib.Object {

		[Obsolete]
		protected PSFile(GLib.GType gtype) : base(gtype) {}
		public PSFile(IntPtr raw) : base(raw) {}

		[DllImport("poppler-glib")]
		static extern IntPtr poppler_ps_file_new(IntPtr document, IntPtr filename, int first_page, int n_pages);

		public PSFile (Poppler.Document document, string filename, int first_page, int n_pages) : base (IntPtr.Zero)
		{
			if (GetType () != typeof (PSFile)) {
				throw new InvalidOperationException ("Can't override this constructor.");
			}
			IntPtr native_filename = GLib.Marshaller.StringToPtrGStrdup (filename);
			Raw = poppler_ps_file_new(document == null ? IntPtr.Zero : document.Handle, native_filename, first_page, n_pages);
			GLib.Marshaller.Free (native_filename);
		}

		[DllImport("poppler-glib")]
		static extern void poppler_ps_file_set_paper_size(IntPtr raw, double width, double height);

		public void SetPaperSize(double width, double height) {
			poppler_ps_file_set_paper_size(Handle, width, height);
		}

		[DllImport("poppler-glib")]
		static extern IntPtr poppler_ps_file_get_type();

		public static new GLib.GType GType { 
			get {
				IntPtr raw_ret = poppler_ps_file_get_type();
				GLib.GType ret = new GLib.GType(raw_ret);
				return ret;
			}
		}

		[DllImport("poppler-glib")]
		static extern void poppler_ps_file_free(IntPtr raw);

		public void Free() {
			poppler_ps_file_free(Handle);
		}

		[DllImport("poppler-glib")]
		static extern void poppler_ps_file_set_duplex(IntPtr raw, bool duplex);

		public bool Duplex { 
			set {
				poppler_ps_file_set_duplex(Handle, value);
			}
		}

#endregion
	}
}
