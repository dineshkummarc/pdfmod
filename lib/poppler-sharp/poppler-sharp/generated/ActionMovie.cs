// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace Poppler {

	using System;
	using System.Collections;
	using System.Runtime.InteropServices;

#region Autogenerated code
	[StructLayout(LayoutKind.Sequential)]
	public struct ActionMovie {

		public Poppler.ActionType Type;
		public string Title;

		public static Poppler.ActionMovie Zero = new Poppler.ActionMovie ();

		public static Poppler.ActionMovie New(IntPtr raw) {
			if (raw == IntPtr.Zero)
				return Poppler.ActionMovie.Zero;
			return (Poppler.ActionMovie) Marshal.PtrToStructure (raw, typeof (Poppler.ActionMovie));
		}

		private static GLib.GType GType {
			get { return GLib.GType.Pointer; }
		}
#endregion
	}
}
