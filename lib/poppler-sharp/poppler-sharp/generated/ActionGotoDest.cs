// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace Poppler {

	using System;
	using System.Collections;
	using System.Runtime.InteropServices;

#region Autogenerated code
	[StructLayout(LayoutKind.Sequential)]
	public struct ActionGotoDest {

		public Poppler.ActionType Type;
		public string Title;
		private IntPtr _dest;

		public Poppler.Dest dest {
			get { return Poppler.Dest.New (_dest); }
		}

		public static Poppler.ActionGotoDest Zero = new Poppler.ActionGotoDest ();

		public static Poppler.ActionGotoDest New(IntPtr raw) {
			if (raw == IntPtr.Zero)
				return Poppler.ActionGotoDest.Zero;
			return (Poppler.ActionGotoDest) Marshal.PtrToStructure (raw, typeof (Poppler.ActionGotoDest));
		}

		private static GLib.GType GType {
			get { return GLib.GType.Pointer; }
		}
#endregion
	}
}
