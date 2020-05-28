#ifndef ESPFSSTREAM_H
#define ESPFSSTREAM_H

#ifdef __cplusplus
#include <iostream>

/// Create or overwrite a file to espFs. 
//  When finished to write, returned stream must be deleted.
std::ostream* espFsAddFileByStream(const char* fname, int len);

//  File system type to replace
enum FsType{
    FS_NONE,
    FS_SPIFFS,
    FS_ESPFS
};

/// Replace whole file system image. 
//  Write the content of the *.img file by using returned stream.
//  When finished to write, returned stream must be deleted.
std::ostream* replaceFsImage(enum FsType fs);

#endif

#endif
