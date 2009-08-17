// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace Poppler {

	using System;
	using System.Runtime.InteropServices;

#region Autogenerated code
	public class Global {

		[DllImport("poppler-glib")]
		static extern IntPtr poppler_get_version();

		public static string Version { 
			get {
				IntPtr raw_ret = poppler_get_version();
				string ret = GLib.Marshaller.Utf8PtrToString (raw_ret);
				return ret;
			}
		}

		[DllImport("poppler-glib")]
		static extern int poppler_error_quark();

		public static int ErrorQuark() {
			int raw_ret = poppler_error_quark();
			int ret = raw_ret;
			return ret;
		}

		[DllImport("poppler-glib")]
		static extern int poppler_get_backend();

		public static Poppler.Backend Backend { 
			get {
				int raw_ret = poppler_get_backend();
				Poppler.Backend ret = (Poppler.Backend) raw_ret;
				return ret;
			}
		}

#endregion
	}
}
