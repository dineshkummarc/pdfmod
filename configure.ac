dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([pdfmod], [0.8.2])
AM_INIT_AUTOMAKE([foreign dist-bzip2 tar-ustar])
AM_MAINTAINER_MODE
AC_SUBST([ACLOCAL_AMFLAGS], ["-I . \${ACLOCAL_FLAGS}"])

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

GNOME_DOC_INIT

AC_DEFUN([SHAMROCK_EXPAND_LIBDIR],
[	
	expanded_libdir=`(
		case $prefix in 
			NONE) prefix=$ac_default_prefix ;; 
			*) ;; 
		esac
		case $exec_prefix in 
			NONE) exec_prefix=$prefix ;; 
			*) ;; 
		esac
		eval echo $libdir
	)`
	AC_SUBST(expanded_libdir)
])

AC_DEFUN([SHAMROCK_EXPAND_BINDIR],
[
	expanded_bindir=`(
		case $prefix in 
			NONE) prefix=$ac_default_prefix ;; 
			*) ;; 
		esac
		case $exec_prefix in 
			NONE) exec_prefix=$prefix ;; 
			*) ;; 
		esac
		eval echo $bindir
	)`
	AC_SUBST(expanded_bindir)
])

AC_DEFUN([SHAMROCK_EXPAND_DATADIR],
[
	case $prefix in
		NONE) prefix=$ac_default_prefix ;;
		*) ;;
	esac

	case $exec_prefix in
		NONE) exec_prefix=$prefix ;;
		*) ;;
	esac

	expanded_datadir=`(eval echo $datadir)`
	expanded_datadir=`(eval echo $expanded_datadir)`

	AC_SUBST(expanded_datadir)
])


SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

IT_PROG_INTLTOOL([0.35.0])
AC_PROG_INSTALL

AC_PATH_PROG(MCS, gmcs, no)
if test "x$MCS" = "xno"; then
        AC_MSG_ERROR([gmcs Not found])
fi


AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=NO]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test x$enable_debug = xyes)
if test "x$enable_debug" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=YES]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
if test "x$enable_release" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_RELEASE, true)
	enable_release=yes
fi

dnl localisation
GETTEXT_PACKAGE=pdfmod
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE,"$GETTEXT_PACKAGE",[Gettext package name])
AM_GLIB_GNU_GETTEXT

dnl package checks, common for all configs
PKG_CHECK_MODULES([GTK_SHARP_20], [gtk-sharp-2.0])
PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0])
PKG_CHECK_MODULES([GCONF_SHARP_20], [gconf-sharp-2.0])

dnl package checks, per config
PKG_CHECK_MODULES(HYENA, hyena >= 0.2)
HYENA_ASSEMBLIES="`$PKG_CONFIG --variable=Assemblies hyena`"
HYENA_FILES="`$PKG_CONFIG --variable=Files hyena`"
AC_SUBST(HYENA_ASSEMBLIES)
AC_SUBST(HYENA_FILES)

PKG_CHECK_MODULES(HYENA_GUI, hyena.gui >= 0.2)
HYENA_GUI_ASSEMBLIES="`$PKG_CONFIG --variable=Assemblies hyena.gui`"
HYENA_GUI_FILES="`$PKG_CONFIG --variable=Files hyena.gui`"
AC_SUBST(HYENA_GUI_ASSEMBLIES)
AC_SUBST(HYENA_GUI_FILES)

BUNDLE_ASSEMBLIES="$HYENA_ASSEMBLIES $HYENA_GUI_ASSEMBLIES"
BUNDLE_FILES="$HYENA_FILES $HYENA_GUI_FILES"
AC_SUBST(BUNDLE_ASSEMBLIES)
AC_SUBST(BUNDLE_FILES)

dnl How to configure for distcheck
DISTCHECK_CONFIGURE_FLAGS=""
AC_SUBST(DISTCHECK_CONFIGURE_FLAGS)

AC_CONFIG_FILES([
po/Makefile.in
docs/Makefile
data/Makefile
data/pdfmod.desktop.in
data/icons/Makefile
lib/PdfSharp/Makefile
lib/poppler-sharp/Makefile
src/PdfMod/Core/Defines.cs
src/pdfmod
src/Makefile
Makefile

])

AC_OUTPUT
