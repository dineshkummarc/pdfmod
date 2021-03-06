// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace Poppler {

	using System;
	using System.Collections;
	using System.Runtime.InteropServices;

#region Autogenerated code
	[StructLayout(LayoutKind.Sequential)]
	public struct ActionGotoRemote {

		public Poppler.ActionType Type;
		public string Title;
		public string FileName;
		private IntPtr _dest;

		public Poppler.Dest dest {
			get { return Poppler.Dest.New (_dest); }
		}

		public static Poppler.ActionGotoRemote Zero = new Poppler.ActionGotoRemote ();

		public static Poppler.ActionGotoRemote New(IntPtr raw) {
			if (raw == IntPtr.Zero)
				return Poppler.ActionGotoRemote.Zero;
			return (Poppler.ActionGotoRemote) Marshal.PtrToStructure (raw, typeof (Poppler.ActionGotoRemote));
		}

		private static GLib.GType GType {
			get { return GLib.GType.Pointer; }
		}
#endregion
	}
}
