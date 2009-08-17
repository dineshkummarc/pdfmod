

#Warning: This is an automatically generated file, do not edit!
if ENABLE_DEBUG
 SUBDIRS =  lib/PdfSharp lib/poppler-sharp src po docs data
endif
if ENABLE_RELEASE
 SUBDIRS =  lib/PdfSharp lib/poppler-sharp src po docs data
endif

INSTALL_DIR_RESOLVED = $(firstword $(subst , $(pkglibdir), $(INSTALL_DIR)))

run:
	cd bin; mono --debug PdfMod.exe;

install-data-hook:
	for ASM in $(BUNDLE_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(INSTALL_DIR_RESOLVED); \
	done; \
	for ASM in $(BUNDLE_FILES); do \
		$(INSTALL) -m 0644 $$ASM $(DESTDIR)$(INSTALL_DIR_RESOLVED); \
	done;

uninstall-hook:
	for ASM in $(BUNDLE_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(INSTALL_DIR_RESOLVED)/`basename $$ASM`; \
	done;
	for ASM in $(BUNDLE_FILES); do \
		rm -f $(DESTDIR)$(INSTALL_DIR_RESOLVED)/`basename $$ASM`; \
	done;

EXTRA_DIST = \
	lib/poppler-sharp/poppler-sharp/poppler-sharp.dll.config \
	gnome-doc-utils.make \
	icon-theme-installer \
	intltool-extract.in \
	intltool-merge.in \
	intltool-update.in \
	AUTHORS \
	HACKING

CLEANFILES = \
	bin/* \
	intltool-extract \
	intltool-update \
	intltool-merge

DISTCLEANFILES = \
	gnome-doc-utils.make \
	*.bak \
	*~ \
	*.pidb

MAINTAINERCLEANFILES = \
	compile \
	INSTALL \
	config.h.in \
	aclocal.m4 \
	ltmain.sh \
	Makefile.in \
	depcomp \
	missing \
	install-sh \
	configure \
	config.sub \
	config.guess \
	intltool-extract.in \
	intltool-merge.in   \
	intltool-update.in  \
	mkinstalldirs
