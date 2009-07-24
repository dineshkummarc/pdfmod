
EXTRA_DIST =  expansions.m4 lib/Hyena.dll lib/Hyena.dll.mdb lib/Hyena.Gui.dll lib/Hyena.Gui.dll.mdb lib/Hyena.Gui.dll.config lib/poppler-sharp.dll lib/poppler-sharp.dll.config

#Warning: This is an automatically generated file, do not edit!
if ENABLE_DEBUG
 SUBDIRS =  src/Libraries/PdfSharp src/PdfMod po
endif
if ENABLE_RELEASE
 SUBDIRS =  src/Libraries/PdfSharp src/PdfMod po
endif

run:
	cd bin; mono --debug PdfMod.exe;
