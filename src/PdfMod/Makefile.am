
# Warning: This is an automatically generated file, do not edit!
#
EXTRA_DIST =

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug "-define:DEBUG"
ASSEMBLY = ../../bin/PdfMod.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES = ../../bin/PdfSharp.dll
BUILD_DIR = ../../bin

PDFSHARP_DLL_SOURCE=../../bin/PdfSharp.dll
PDFSHARP_DLL_MDB_SOURCE=../../bin/PdfSharp.dll.mdb
HYENA_DLL_SOURCE=../../lib/Hyena.dll
HYENA_DLL_MDB_SOURCE=../../lib/Hyena.dll.mdb
MONO_DATA_SQLITE_DLL_SOURCE=../../lib/Mono.Data.Sqlite.dll
MONO_DATA_SQLITE_DLL_MDB_SOURCE=../../lib/Mono.Data.Sqlite.dll.mdb
HYENA_GUI_DLL_SOURCE=../../lib/Hyena.Gui.dll
HYENA_GUI_DLL_CONFIG_SOURCE=../../lib/Hyena.Gui.dll.config
HYENA_GUI_DLL_MDB_SOURCE=../../lib/Hyena.Gui.dll.mdb
POPPLER_SHARP_DLL_SOURCE=../../lib/poppler-sharp.dll
POPPLER_SHARP_DLL_CONFIG_SOURCE=../../lib/poppler-sharp.dll.config
PDFMOD_EXE_MDB_SOURCE=../../bin/PdfMod.exe.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG
ASSEMBLY = ../../bin/PdfMod.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../bin/PdfSharp.dll
BUILD_DIR = ../../bin

PDFSHARP_DLL_SOURCE=../../bin/PdfSharp.dll
PDFSHARP_DLL_MDB_SOURCE=../../bin/PdfSharp.dll.mdb
HYENA_DLL_SOURCE=../../lib/Hyena.dll
HYENA_DLL_MDB_SOURCE=../../lib/Hyena.dll.mdb
MONO_DATA_SQLITE_DLL_SOURCE=../../lib/Mono.Data.Sqlite.dll
MONO_DATA_SQLITE_DLL_MDB_SOURCE=../../lib/Mono.Data.Sqlite.dll.mdb
HYENA_GUI_DLL_SOURCE=../../lib/Hyena.Gui.dll
HYENA_GUI_DLL_CONFIG_SOURCE=../../lib/Hyena.Gui.dll.config
HYENA_GUI_DLL_MDB_SOURCE=../../lib/Hyena.Gui.dll.mdb
POPPLER_SHARP_DLL_SOURCE=../../lib/poppler-sharp.dll
POPPLER_SHARP_DLL_CONFIG_SOURCE=../../lib/poppler-sharp.dll.config
PDFMOD_EXE_MDB_SOURCE=../../bin/PdfMod.exe.mdb

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(PDFSHARP_DLL) \
	$(PDFSHARP_DLL_MDB) \
	$(HYENA_DLL) \
	$(HYENA_DLL_MDB) \
	$(MONO_DATA_SQLITE_DLL) \
	$(MONO_DATA_SQLITE_DLL_MDB) \
	$(HYENA_GUI_DLL) \
	$(HYENA_GUI_DLL_CONFIG) \
	$(HYENA_GUI_DLL_MDB) \
	$(POPPLER_SHARP_DLL) \
	$(POPPLER_SHARP_DLL_CONFIG) \
	$(PDFMOD_EXE_MDB)  

BINARIES = \
	$(PDFMOD)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES =  \
	PdfMod.Actions/BasePageAction.cs \
	PdfMod.Actions/ExportImagesAction.cs \
	PdfMod.Actions/MoveAction.cs \
	PdfMod.Actions/RemoveAction.cs \
	PdfMod.Actions/RotateAction.cs \
	PdfMod/AssemblyInfo.cs \
	PdfMod/CellRendererCairo.cs \
	PdfMod/CellRendererPage.cs \
	PdfMod/Document.cs \
	PdfMod/GlobalActions.cs \
	PdfMod/MetadataEditorBox.cs \
	PdfMod/Page.cs \
	PdfMod/PageLabels.cs \
	PdfMod/PageThumbnail.cs \
	PdfMod/PdfIconView.cs \
	PdfMod/PdfListStore.cs \
	PdfMod/PdfMod.cs \
	PdfMod/QueryBox.cs 

DATA_FILES = 

RESOURCES = Resources/UIManager.xml 

EXTRAS = \
	pdfmod.in 

REFERENCES =  \
	Mono.Cairo \
	Mono.Posix \
	-pkg:glade-sharp-2.0 \
	-pkg:glib-sharp-2.0 \
	-pkg:gnome-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	System \
	System.Core \
	System.Drawing

DLL_REFERENCES =  \
	../../lib/Hyena.dll \
	../../lib/Hyena.Gui.dll \
	../../lib/Mono.Data.Sqlite.dll \
	../../lib/poppler-sharp.dll

CLEANFILES = $(PROGRAMFILES) $(BINARIES)

include $(top_srcdir)/Makefile.include

PDFSHARP_DLL = $(BUILD_DIR)/PdfSharp.dll
PDFSHARP_DLL_MDB = $(BUILD_DIR)/PdfSharp.dll.mdb
HYENA_DLL = $(BUILD_DIR)/Hyena.dll
HYENA_DLL_MDB = $(BUILD_DIR)/Hyena.dll.mdb
MONO_DATA_SQLITE_DLL = $(BUILD_DIR)/Mono.Data.Sqlite.dll
MONO_DATA_SQLITE_DLL_MDB = $(BUILD_DIR)/Mono.Data.Sqlite.dll.mdb
HYENA_GUI_DLL = $(BUILD_DIR)/Hyena.Gui.dll
HYENA_GUI_DLL_CONFIG = $(BUILD_DIR)/Hyena.Gui.dll.config
HYENA_GUI_DLL_MDB = $(BUILD_DIR)/Hyena.Gui.dll.mdb
POPPLER_SHARP_DLL = $(BUILD_DIR)/poppler-sharp.dll
POPPLER_SHARP_DLL_CONFIG = $(BUILD_DIR)/poppler-sharp.dll.config
PDFMOD_EXE_MDB = $(BUILD_DIR)/PdfMod.exe.mdb
PDFMOD = $(BUILD_DIR)/pdfmod

$(eval $(call emit-deploy-target,HYENA_DLL))
$(eval $(call emit-deploy-target,HYENA_DLL_MDB))
$(eval $(call emit-deploy-target,MONO_DATA_SQLITE_DLL))
$(eval $(call emit-deploy-target,MONO_DATA_SQLITE_DLL_MDB))
$(eval $(call emit-deploy-target,HYENA_GUI_DLL))
$(eval $(call emit-deploy-target,HYENA_GUI_DLL_CONFIG))
$(eval $(call emit-deploy-target,HYENA_GUI_DLL_MDB))
$(eval $(call emit-deploy-target,POPPLER_SHARP_DLL))
$(eval $(call emit-deploy-target,POPPLER_SHARP_DLL_CONFIG))
$(eval $(call emit-deploy-wrapper,PDFMOD,pdfmod,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
