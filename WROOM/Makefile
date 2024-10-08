#
# This is a project Makefile. It is assumed the directory this Makefile resides in is a
# project subdirectory.
#

PROJECT_NAME := Nuibot

include $(IDF_PATH)/make/project.mk


#
# Build the files for shipping.
#
ship: images ship_build all
	echo "+---------------+"
	echo "| Building ship |"
	echo "+---------------+"
	mkdir -p build/ship
	cp build/Nuibot.bin build/ship
	cp build/partitions_singleapp.bin build/ship
	cp build/bootloader/bootloader.bin build/ship
	cp build/spiffs.img build/ship
	cp build/espfs.img build/ship
	cp data/install_binaries.sh build/ship
	cd build/ship; tar -cvzf ../../esp32-duktape-$(shell date "+%Y-%m-%d").tar.gz .
	echo "+---------+"
	echo "| Results |"
	echo "+---------+"
	echo "Created output: esp32-duktape-$(shell date "+%Y-%m-%d").tar.gz"


#
#  Force a recompile of duktape_task.c to always include the current date/time of
#  the build.
#
ship_build:
	touch main/duktape_task.c

#
#  Build the file system images.
#
images:
	echo "+--------------------+"
	echo "| Building espfs.img |"
	echo "+--------------------+"
	cd filesystem; find . -print | ../bin/mkespfsimage -c 0 > ../build/espfs.img
	echo "+---------------------+"
	echo "| Building spiffs.img |"
	echo "+---------------------+"
	./bin/mkspiffs -c filesystem -b 4096 -p 256 -s 0x80000 build/spiffs.img



#
#  Update duktape-esp32 from local source tree (from ../../duktape-esp32)
#
folders = main/duktapeEsp32 components/duktape components/espfs
duk32_update:
	echo "Update duktape-esp32 from local source tree (../../duktape-esp32)"
	rm -rf $(folders)
	mkdir $(folders)
	cp -R ../../duktape-esp32/main/* main/duktapeEsp32/
	cp -R ../../duktape-esp32/components/espfs components/
	cp -R ../../duktape-esp32/components/duktape/src components/duktape/
	cp -f ../../duktape-esp32/components/duktape/component.mk components/duktape/
	mkdir components/duktape/extras
	cp -R ../../duktape-esp32/components/duktape/extras/module-duktape components/duktape/extras/
	mkdir components/duktape/examples
	cp -R ../../duktape-esp32/components/duktape/examples/debug-trans-socket components/duktape/examples/
	cp -Rf ../../duktape-esp32/bin/* bin/
	cp -Rf ../../duktape-esp32/data/* data/
	cp -Rf ../../duktape-esp32/filesystem/* filesystem/
	cp -f ../../duktape-esp32/partitions.csv .

#  This is not needed. All files are added to git.
#  Perform a clean install of duktape.
#
duktape_install:
	echo "Installing duktape"
	rm -rf components/duktape
	cd ./components; git clone https://github.com/svaarala/duktape.git
	cp ./data/duktape/component.mk ./components/duktape/
	rm ./components/duktape/examples/debug-trans-socket/duk_trans_socket_windows.c

#  This is not needed. Configurated files are added to git.
#  Perform a configuration of duktape.
#  See the following Duktape documentation for details.
#  https://github.com/svaarala/duktape/blob/master/doc/duk-config.rst
#
#  The properties for the configuration are the "low_memory" profile with overrides
#  supplied in data/duktape/ESP32-Duktape.yaml.
#
# Remove the following:
#		--rom-support \
#		--rom-auto-lightfunc \
#
duktape_configure:
	python ./components/duktape/tools/configure.py \
		--rom-support \
		--rom-auto-lightfunc \
		--config-metadata components/duktape/config/ \
		--source-directory components/duktape/src-input \
		--option-file components/duktape/config/examples/low_memory.yaml \
		--option-file data/duktape/ESP32-Duktape.yaml \
		--fixup-file main/include/duktape_fixup.h \
		--output-directory components/duktape/src

#
#  Perform flashing of both ESPFS and SPIFFS to ESP32
#
flashdata: images
	echo "Flashing both ESPFS and SPIFFS to ESP32"
	$(ESPTOOLPY_WRITE_FLASH) --compress 0x3d0000 build/espfs.img 0x200000 build/spiffs.img
#
#  Build all, flash app & flash both ESPFS and SPIFFS to ESP32
#
flashall: flash flashdata

what:
	echo "duktape_configure - Configure Duktape."
	echo "duktape_install   - Install latest Duktape."
	echo "flash             - Flash the ESP32 application."
	echo "flashall          - Flash the ESP32 application and file systems data."
	echo "flashdata         - Flash the file systems data."


