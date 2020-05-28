/*
This is a simple read-only implementation of a file system. It uses a block of data coming from the
mkespfsimg tool, and can use that block to do abstracted operations on the files that are in there.
It's written for use with httpd, but doesn't need to be used as such.
*/

/*
 * ----------------------------------------------------------------------------
 * "THE BEER-WARE LICENSE" (Revision 42):
 * Jeroen Domburg <jeroen@spritesmods.com> wrote this file. As long as you retain 
 * this notice you can do whatever you want with this stuff. If we meet some day, 
 * and you think this stuff is worth it, you can buy me a beer in return. 
 * ----------------------------------------------------------------------------
 */


//These routines can also be tested by comping them in with the espfstest tool. This
//simplifies debugging, but needs some slightly different headers. The #ifdef takes
//care of that.

#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#include <esp_spi_flash.h>
#include <esp_log.h>
#include <esp_err.h>

#include "espfsformat.h"
#include "espfs.h"
#include "sdkconfig.h"

static char tag[] = "espfs";


static spi_flash_mmap_handle_t handle;
static void *espFlashPtr = NULL;
static size_t flashOffset;
static size_t flashSize;

struct EspFsFile {
	EspFsHeader *header;
	char decompressor;
	int32_t posDecomp;
	char *posStart;
	char *posComp;
	void *decompData;
};


EspFsInitResult espFsInit(void *flashAddress, size_t size) {

	spi_flash_init();
	if (size % (64*1024) != 0) {
		ESP_LOGE(tag, "Size is not divisible by 64K.  Supplied was %d", size);
		return ESPFS_INIT_RESULT_NO_IMAGE;
	}
	esp_err_t rc = spi_flash_mmap((uint32_t) flashAddress, size, SPI_FLASH_MMAP_DATA, (const void **)&espFlashPtr, &handle);
	if (rc != ESP_OK) {
		ESP_LOGE(tag, "rc from spi_flash_mmap: %d", rc);
	}
	flashOffset = (size_t)flashAddress;
	flashSize = size;

	// check if there is valid header at address
	EspFsHeader *testHeader = (EspFsHeader *)espFlashPtr;

	if (testHeader->magic != ESPFS_MAGIC) {
		ESP_LOGE(tag, "No valid header at flash address.  Expected to find %x and found %x", ESPFS_MAGIC, testHeader->magic);
		return ESPFS_INIT_RESULT_NO_IMAGE;
	}

	return ESPFS_INIT_RESULT_OK;
}

// Returns flags of opened file.
int espFsFlags(EspFsFile *fh) {
	if (fh == NULL) {
		ESP_LOGE(tag, "File handle not ready");
		return -1;
	}

	int8_t flags;
	memcpy((char*)&flags, (char*)&fh->header->flags, 1);
	return (int)flags;
}

static bool remap(){
	if (espFlashPtr == NULL) {
		if (flashOffset == 0){
			ESP_LOGE(tag, "Call espFsInit first!");
			return false;
		}else{
			spi_flash_munmap(handle);
			esp_err_t rc = spi_flash_mmap(flashOffset, flashSize, SPI_FLASH_MMAP_DATA, (const void **)&espFlashPtr, &handle);
			if (rc != ESP_OK) {
				ESP_LOGE(tag, "rc from spi_flash_mmap: %d", rc);
				return false;
			}
		}
	}
	return true;
}

//Open a file and return a pointer to the file desc struct.
EspFsFile *espFsOpen(const char *fileName) {
	ESP_LOGD(tag, ">> espFsOpen: %s", fileName);
	if (!remap()) return NULL;
	char *flashAddress = espFlashPtr;
	char *hpos;
	char *namebuf;
	EspFsHeader *header;
	EspFsFile *fileData;
	//Strip initial slashes
	while(fileName[0] == '/') {
		fileName++;
	}
	//Go find that file!
	while(1) {
		hpos=flashAddress;
		//Grab the next file header.
		header = (EspFsHeader *)flashAddress;

		if (header->magic != ESPFS_MAGIC) {
			ESP_LOGE(tag, "Magic mismatch. EspFS image broken at file '%s'.", flashAddress+sizeof(EspFsHeader));
			return NULL;
		}
		if (header->flags & FLAG_LASTFILE) {
			ESP_LOGD(tag, "End of image.  File not found.");
			return NULL;
		}
		//Grab the name of the file.
		flashAddress += sizeof(EspFsHeader);
		namebuf = (char *)flashAddress;
		if (strcmp(namebuf, fileName) == 0) {
			//Yay, this is the file we need!
			flashAddress += header->nameLen; //Skip to content.
			fileData = (EspFsFile *)malloc(sizeof(EspFsFile)); //Alloc file desc mem
			if (fileData==NULL) {
				return NULL;
			}
			fileData->header = (EspFsHeader *)hpos;
			fileData->decompressor = header->compression;
			fileData->posComp = flashAddress;
			fileData->posStart = flashAddress;
			fileData->posDecomp = 0;
			if (header->compression == COMPRESS_NONE) {
				fileData->decompData = NULL;
			} else {
				ESP_LOGE(tag, "Invalid compression: %d", header->compression);
				return NULL;
			}
			return fileData;
		}
		//We don't need this file. Skip name and file
		flashAddress += header->nameLen+header->fileLenComp + 1;	//	+1 is for appeded '\0'.
		if ((int)flashAddress&3) {
			flashAddress += 4-((int)flashAddress & 3); //align to next 32bit val
		}
	}
	ESP_LOGD(tag, "<< espFsOpen");
} // espFsOpen

//Close the file.
void espFsClose(EspFsFile *fh) {
	if (fh == NULL) return;
	free(fh);
}

//	Add clean file and return the start address for data.
size_t espFsAddCleanArea(const char* fname, int len){
	if (!remap()) return 0;
	//Strip initial slashes
	while(fname[0] == '/') {
		fname++;
	}
	char *flashAddress = espFlashPtr;
	EspFsHeader *header;
	//Go find that file!
	while(1) {
		//Grab the next file header.
		header = (EspFsHeader *)flashAddress;
		if (header->magic != ESPFS_MAGIC) {
			ESP_LOGE(tag, "Magic mismatch. EspFS image broken at file '%s'.", flashAddress+sizeof(EspFsHeader));
			return 0;
		}
		if ((header->flags & FLAG_LASTFILE) || strcmp(flashAddress+sizeof(EspFsHeader), fname) == 0){
			bool bWriteHere = false;
			if (header->flags & FLAG_LASTFILE){
				ESP_LOGD(tag, "Find the end.");
				bWriteHere = true;
			}else{
				//	check if the next file is the last file or not.
				ESP_LOGD(tag, "Find the same name %s at 0x%x.", flashAddress+sizeof(EspFsHeader), (int)flashAddress);
				char* faNext = flashAddress;
				faNext += sizeof(EspFsHeader) + header->nameLen + header->fileLenComp + 1;	//	+1 is for appeded '\0'.
				if ((int)faNext&3) {
					faNext += 4-((int)faNext & 3); //align to next 32bit val
				}
				EspFsHeader* hNext = (EspFsHeader*)faNext; 
				if (hNext->magic != ESPFS_MAGIC) {
					ESP_LOGE(tag, "Magic mismatch. EspFS image broken at file '%s'.", faNext+sizeof(EspFsHeader));
				}
				if (hNext->flags & FLAG_LASTFILE){
					ESP_LOGD(tag, "Find a file with the same name '%s' at 0x%x as the last file.", fname, (int)flashAddress);
					bWriteHere = true;
				}else{
					ESP_LOGD(tag, "Find a file with the same name '%s' at 0x%x before '%s'. Rename it and skip.", fname, (int)flashAddress, faNext+sizeof(EspFsHeader));
					//	rename the filename of this file into "".
					const char* flashBase = (const char*)espFlashPtr - flashOffset;
					char* name = flashAddress + sizeof(EspFsFile);
					spi_flash_write(name-flashBase, "", 1);
					bWriteHere = false;
				}
			}
			if (bWriteHere){
				//	Write the new file
				const char* flashBase = (const char*)espFlashPtr - flashOffset;
				const char* flashErase = (const char*)((unsigned)flashAddress & 0xFFFFF000);	//	4kB = 0x1000byte align
				size_t keepLen = flashAddress - flashErase;
				//ESP_LOGD(tag, "base=0x%x, erase=0x%x, eraseInPh=0x%x,  keep=%d", (int)flashBase, (int)flashErase, flashErase-flashBase, keepLen);
				char* keep = (char*)malloc(keepLen);
				memcpy(keep, flashErase, keepLen);					//	keep contents of erasing area
				size_t nameLen = strlen(fname)+1;
				if (nameLen&3) nameLen+=4-(nameLen&3); 				//Round to next 32bit boundary

				size_t eraseLen = keepLen + sizeof(EspFsHeader) + nameLen + len + sizeof(EspFsHeader);
				eraseLen = (eraseLen + 0xFFF) & 0xFFFFF000;
				spi_flash_erase_range(flashErase - flashBase, eraseLen);
				spi_flash_write(flashErase - flashBase, keep, keepLen);			//	restore before new file
				free(keep);
				EspFsHeader newHeader;
				memset(&newHeader, 0, sizeof(newHeader)); 
				newHeader.fileLenDecomp = len;
				newHeader.fileLenComp = len;
				newHeader.nameLen = nameLen;
				newHeader.magic = ESPFS_MAGIC;
				spi_flash_write(flashAddress - flashBase, &newHeader, sizeof(newHeader));	//	write header
				flashAddress += sizeof(newHeader);
				spi_flash_write(flashAddress - flashBase, fname, newHeader.nameLen);	//	write header
				flashAddress += newHeader.nameLen;
	//			spi_flash_write(flashAddress - flashBase, data, len);
				size_t rv = flashAddress - flashBase;
				flashAddress += len;
				spi_flash_write(flashAddress - flashBase, "\0", 1);
				flashAddress += 1;
				if ((int)flashAddress&3) {
					flashAddress += 4-((int)flashAddress & 3); //align to next 32bit val
				}
				//	write terminator
				memset(&newHeader, 0, sizeof(newHeader)); 
				newHeader.flags = FLAG_LASTFILE;
				newHeader.magic = ESPFS_MAGIC;
				spi_flash_write(flashAddress - flashBase, &newHeader, sizeof(newHeader));
				espFlashPtr = NULL;	//	must flash unmap and map again.
				return rv;
			}
		}
		//Grab the name of the file.
		flashAddress += sizeof(EspFsHeader);
		//We don't need this file. Skip name and file
		flashAddress += header->nameLen+header->fileLenComp + 1;	//	+1 is for appeded '\0'.
		if ((int)flashAddress&3) {
			flashAddress += 4-((int)flashAddress & 3); //align to next 32bit val
		}
	} // While files to process
	return 0;
}
bool espFsAddFile(const char* fname, const char* data, int len){
	size_t flashAddress = espFsAddCleanArea(fname, len);
	if (flashAddress){
		spi_flash_write(flashAddress, &data, len);
		return true;
	}
	return false;
}

//Read len bytes from the given file into buff. Returns the actual amount of bytes read.
int espFsRead(EspFsFile *fh, char *buff, int len) {
	int flen;
	if (fh==NULL) {
		return 0;
	}
		
	memcpy((char*)&flen, (char*)&fh->header->fileLenComp, 4);

	if (fh->decompressor == COMPRESS_NONE) {
		int toRead;
		toRead = flen-(fh->posComp-fh->posStart);
		if (len > toRead) {
			len = toRead;
		}
		ESP_LOGD(tag, "copying %d bytes from 0x%x to 0x%x", len, (uint32_t)fh->posComp, (uint32_t)buff);
		memcpy(buff, fh->posComp, len);
		fh->posDecomp += len;
		fh->posComp += len;
		return len;
	}
	return 0;
}

int espFsAccess(EspFsFile *fh, void **buf, size_t *len) {
	*buf = fh->posStart;
	*len = fh->header->fileLenComp;
	return *len;
}

void espFsDumpFiles() {
	if (espFlashPtr == NULL) {
		ESP_LOGD(tag, "Call espFsInit first!");
		return;
	}
	ESP_LOGD(tag, ">> espFsDumpFiles");
	char *flashAddress = espFlashPtr;
	char *namebuf;
	uint32_t totalSize = 0;
	EspFsHeader *header;

	//Go find that file!
	while(1) {
		//Grab the next file header.
		header = (EspFsHeader *)flashAddress;

		if (header->magic != ESPFS_MAGIC) {
			ESP_LOGE(tag, "Magic mismatch. EspFS image broken at file '%s'.", flashAddress+sizeof(EspFsHeader));
			return;
		}
		if (header->flags & FLAG_LASTFILE) {
			ESP_LOGD(tag, "Files total size = %d", totalSize);
			return;
		}
		//Grab the name of the file.
		flashAddress += sizeof(EspFsHeader);
		namebuf = (char *)flashAddress;
		ESP_LOGD(tag, " - %s - %d", namebuf, header->fileLenComp);
		totalSize += header->fileLenComp;

		//We don't need this file. Skip name and file
		flashAddress += header->nameLen+header->fileLenComp + 1;	//	+1 is for appeded '\0'.
		if ((int)flashAddress&3) {
			flashAddress += 4-((int)flashAddress & 3); //align to next 32bit val
		}
	} // While files to process
} // espFsDumpFiles
