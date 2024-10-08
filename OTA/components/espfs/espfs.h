#ifndef ESPFS_H
#define ESPFS_H
#include <stdlib.h>
// This define is done in Makefile. If you do not use default Makefile, uncomment
// to be able to use Heatshrink-compressed espfs images.
//#define ESPFS_HEATSHRINK

#ifdef __cplusplus
extern "C" {
#endif

typedef enum {
	ESPFS_INIT_RESULT_OK,
	ESPFS_INIT_RESULT_NO_IMAGE,
	ESPFS_INIT_RESULT_BAD_ALIGN
} EspFsInitResult;
typedef struct EspFsFile EspFsFile;


EspFsInitResult espFsInit(void *flashAddress, size_t size);
EspFsFile *espFsOpen(const char *fileName);
int espFsFlags(EspFsFile *fh);
int espFsRead(EspFsFile *fh, char *buff, int len);
void espFsClose(EspFsFile *fh);
int espFsAccess(EspFsFile *fh, void **buf, size_t *len);
void espFsDumpFiles();
size_t espFsAddCleanArea(const char* fname, int len);
bool espFsAddFile(const char* fname, const char* data, int len);


#ifdef __cplusplus
}
#endif


#endif
