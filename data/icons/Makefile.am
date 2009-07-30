theme = hicolor
themedir = $(datadir)/icons/$(theme)
theme_icons = \
	apps,pdfmod-16.png \
	apps,pdfmod-22.png \
	apps,pdfmod-24.png \
	apps,pdfmod-32.png \
	apps,pdfmod-256.png \
	apps,pdfmod.svg

install_icon_exec = $(top_srcdir)/icon-theme-installer \
	-t "$(theme)" \
	-s "$(srcdir)" \
	-d "x$(DESTDIR)" \
	-b "$(themedir)" \
	-m "$(mkinstalldirs)" \
	-x "$(INSTALL_DATA)"

install-data-local:
	@-$(install_icon_exec) -i $(theme_icons)

uninstall-hook: 
	@-$(install_icon_exec) -u $(theme_icons)
	
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(wildcard *.png *.svg)

