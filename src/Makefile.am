
# Warning: This is an automatically generated file, do not edit!
#
EXTRA_DIST =

ASSEMBLY_COMPILER_COMMAND = $(MCS)
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug "-define:DEBUG"
ASSEMBLY = ../bin/PdfMod.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =
BUILD_DIR = ../bin

PDFMOD_EXE_MDB_SOURCE=../bin/PdfMod.exe.mdb

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = $(PDFMOD_EXE_MDB)

BINARIES = \
	$(PDFMOD)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES =  \
	PdfMod/Core/AssemblyInfo.cs \
	PdfMod/Core/Client.cs \
	PdfMod/Core/Configuration.cs \
	PdfMod/Core/Defines.cs \
	PdfMod/Gui/Actions.cs \
	PdfMod/Gui/BookmarkView.cs \
	PdfMod/Gui/CairoCell.cs \
	PdfMod/Gui/Client.cs \
	PdfMod/Gui/DocumentIconView.cs \
	PdfMod/Gui/MetadataEditorBox.cs \
	PdfMod/Gui/PageCell.cs \
	PdfMod/Gui/PageListStore.cs \
	PdfMod/Gui/SelectMatchingBox.cs \
	PdfMod/Main.cs \
	PdfMod/Pdf/Actions/BaseAction.cs \
	PdfMod/Pdf/Actions/BasePageAction.cs \
	PdfMod/Pdf/Actions/ExportImagesAction.cs \
	PdfMod/Pdf/Actions/MoveAction.cs \
	PdfMod/Pdf/Actions/RemoveAction.cs \
	PdfMod/Pdf/Actions/RotateAction.cs \
	PdfMod/Pdf/Document.cs \
	PdfMod/Pdf/Page.cs \
	PdfMod/Pdf/PageLabels.cs \
	PdfMod/Pdf/PageThumbnail.cs 

DATA_FILES = 

RESOURCES = Resources/UIManager.xml 

EXTRAS = \
	pdfmod.in 

REFERENCES =  \
	Mono.Cairo \
	Mono.Posix \
	-pkg:gconf-sharp-2.0 \
	-pkg:glib-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-pkg:hyena \
	-pkg:hyena.gui \
	System \
	System.Core

PROJECT_REFERENCES = ../bin/PdfSharp.dll

if USE_BUNDLED_POPPLER
PROJECT_REFERENCES += ../bin/poppler-sharp.dll
PROGRAMFILES += $(POPPLER_SHARP_DLL_CONFIG)

else
REFERENCES += -pkg:poppler-sharp
endif

DLL_REFERENCES = ../bin/PdfSharp.dll 

CLEANFILES = $(PROGRAMFILES) $(BINARIES)

include $(top_srcdir)/Makefile.include

PDFSHARP_DLL = $(BUILD_DIR)/PdfSharp.dll
PDFSHARP_DLL_MDB = $(BUILD_DIR)/PdfSharp.dll.mdb
POPPLER_SHARP_DLL = $(BUILD_DIR)/poppler-sharp.dll
POPPLER_SHARP_DLL_CONFIG = $(BUILD_DIR)/poppler-sharp.dll.config
PDFMOD_EXE_MDB = $(BUILD_DIR)/PdfMod.exe.mdb
PDFMOD = $(BUILD_DIR)/pdfmod

$(eval $(call emit-deploy-wrapper,PDFMOD,pdfmod,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	cp $(BUNDLE_ASSEMBLIES) $(BUILD_DIR)
	cp $(BUNDLE_FILES) $(BUILD_DIR)
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
