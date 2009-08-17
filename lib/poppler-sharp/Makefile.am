
EXTRA_DIST =

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = $(MCS)
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -unsafe -debug "-define:DEBUG"
ASSEMBLY = ../bin/poppler-sharp.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  
BUILD_DIR = ../bin

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = $(MCS)
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -unsafe -debug -define:DEBUG
ASSEMBLY = ../bin/poppler-sharp.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  
BUILD_DIR = ../bin

endif

PROGRAMFILES = \
	$(POPPLER_SHARP_DLL_MDB)

BINARIES = 


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES =  \
	poppler-sharp/generated/*.cs \
	poppler-sharp/Document.cs

DATA_FILES = 

RESOURCES = 

EXTRAS = 

REFERENCES =  \
	Mono.Cairo \
	-pkg:gtk-sharp-2.0 \
	System 

DLL_REFERENCES =  

CLEANFILES = $(PROGRAMFILES) $(BINARIES)

include $(top_srcdir)/Makefile.include

POPPLER_SHARP_DLL = $(BUILD_DIR)/poppler-sharp.dll
POPPLER_SHARP_DLL_CONFIG = $(BUILD_DIR)/poppler-sharp.dll.config
POPPLER_SHARP_DLL_MDB = $(BUILD_DIR)/poppler-sharp.dll.mdb

$(eval $(call emit-deploy-target,POPPLER_SHARP_DLL))
$(eval $(call emit-deploy-target,POPPLER_SHARP_DLL_CONFIG))
$(eval $(call emit-deploy-wrapper,PDFMOD,pdfmod,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	cp poppler-sharp/poppler-sharp.dll.config $(BUILD_DIR)
