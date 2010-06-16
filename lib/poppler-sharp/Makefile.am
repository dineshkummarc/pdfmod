
EXTRA_DIST = 

if USE_BUNDLED_POPPLER
if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = $(MCS)
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -unsafe -debug "-define:DEBUG"
ASSEMBLY = ../../bin/poppler-sharp.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  
BUILD_DIR = ../../bin

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = $(MCS)
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -unsafe -debug -define:DEBUG
ASSEMBLY = ../../bin/poppler-sharp.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  
BUILD_DIR = ../../bin

endif

PROGRAMFILES = \
	$(POPPLER_SHARP_DLL_MDB)

BINARIES = 


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES)

endif

FILES =  \
	poppler-sharp/Document.cs \
	poppler-sharp/generated/Action.cs \
	poppler-sharp/generated/ActionAny.cs \
	poppler-sharp/generated/ActionGotoDest.cs \
	poppler-sharp/generated/ActionGotoRemote.cs \
	poppler-sharp/generated/ActionLaunch.cs \
	poppler-sharp/generated/ActionMovie.cs \
	poppler-sharp/generated/ActionNamed.cs \
	poppler-sharp/generated/ActionType.cs \
	poppler-sharp/generated/ActionUri.cs \
	poppler-sharp/generated/Annot.cs \
	poppler-sharp/generated/AnnotCalloutLine.cs \
	poppler-sharp/generated/AnnotExternalDataType.cs \
	poppler-sharp/generated/AnnotFlag.cs \
	poppler-sharp/generated/AnnotFreeText.cs \
	poppler-sharp/generated/AnnotFreeTextQuadding.cs \
	poppler-sharp/generated/AnnotMapping.cs \
	poppler-sharp/generated/AnnotMarkup.cs \
	poppler-sharp/generated/AnnotMarkupReplyType.cs \
	poppler-sharp/generated/AnnotText.cs \
	poppler-sharp/generated/AnnotTextState.cs \
	poppler-sharp/generated/AnnotType.cs \
	poppler-sharp/generated/Attachment.cs \
	poppler-sharp/generated/AttachmentSaveFunc.cs \
	poppler-sharp/generated/Backend.cs \
	poppler-sharp/generated/Color.cs \
	poppler-sharp/generated/Dest.cs \
	poppler-sharp/generated/DestType.cs \
	poppler-sharp/generated/Document.cs \
	poppler-sharp/generated/Error.cs \
	poppler-sharp/generated/FontInfo.cs \
	poppler-sharp/generated/FontType.cs \
	poppler-sharp/generated/FontsIter.cs \
	poppler-sharp/generated/FormButtonType.cs \
	poppler-sharp/generated/FormChoiceType.cs \
	poppler-sharp/generated/FormField.cs \
	poppler-sharp/generated/FormFieldMapping.cs \
	poppler-sharp/generated/FormFieldType.cs \
	poppler-sharp/generated/FormTextType.cs \
	poppler-sharp/generated/Global.cs \
	poppler-sharp/generated/ImageMapping.cs \
	poppler-sharp/generated/IndexIter.cs \
	poppler-sharp/generated/Layer.cs \
	poppler-sharp/generated/LayersIter.cs \
	poppler-sharp/generated/LinkMapping.cs \
	poppler-sharp/generated/Orientation.cs \
	poppler-sharp/generated/PSFile.cs \
	poppler-sharp/generated/Page.cs \
	poppler-sharp/generated/PageLayout.cs \
	poppler-sharp/generated/PageMode.cs \
	poppler-sharp/generated/PageTransition.cs \
	poppler-sharp/generated/PageTransitionAlignment.cs \
	poppler-sharp/generated/PageTransitionDirection.cs \
	poppler-sharp/generated/PageTransitionType.cs \
	poppler-sharp/generated/PopplerSharp.AttachmentSaveFuncNative.cs \
	poppler-sharp/generated/Rectangle.cs \
	poppler-sharp/generated/SelectionStyle.cs \
	poppler-sharp/generated/ViewerPreferences.cs

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

$(eval $(call emit-deploy-target,POPPLER_SHARP_DLL_CONFIG))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	cp $(srcdir)/poppler-sharp/poppler-sharp.dll.config $(BUILD_DIR)
