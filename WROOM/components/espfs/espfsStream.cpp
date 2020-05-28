#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#include <esp_spi_flash.h>
#include <esp_partition.h>
#include <esp_err.h>
#include <iostream>
#include "logging.h"
LOG_TAG("espfs");

extern "C" {
    #include "espfsformat.h"
    #include "espfs.h"
}

#include "espfsStream.h"
#include "sdkconfig.h"

class espFsStreambuf: public std::streambuf{
    protected:
    size_t flashAddress;
    int len;
    char buf[256];
    public:
    espFsStreambuf() = delete;
    espFsStreambuf(size_t fa, int l):flashAddress(fa), len(l){
        LOGD("stream buf with length %d bytes constructed.", len);
        setp(buf, buf+ sizeof(buf));
    }
    virtual ~espFsStreambuf(){
        sync();
        if (len){
            LOGE("Clean area remains %d bytes.", len);
        }
    }
    virtual int overflow( int p_iChar = EOF ){
        size_t writeLen = pptr()-pbase();
        if (writeLen > len){
            LOGE("Try to write a data (len:%d) longer than the file length remain %d.", writeLen, len);
            writeLen = len;
            return traits_type::eof();
        }
        len -= writeLen;
		spi_flash_write(flashAddress, pbase(), writeLen);	//	write whole buffer into flash.
        flashAddress += writeLen;
        setp(buf, buf + sizeof(buf));
        if (!traits_type::eq_int_type(p_iChar, traits_type::eof())){
            //  put overflowed character in the buffer.
            buf[0] = traits_type::to_char_type(p_iChar);
            pbump(1);
        }
        LOGD("Write %d, remain %d, char %d", writeLen, len, p_iChar);
        return writeLen; 
    }
    virtual int sync(){
        overflow();
        return 0;   // success
    }
};

class espOstream:public std::ostream{
public:
    espOstream(std::streambuf* b):std::ostream(b){
    }
    virtual ~espOstream(){
        delete rdbuf();
    }
};

std::ostream* espFsAddFileByStream(const char* fname, int len){
    size_t adr = espFsAddCleanArea(fname, len);
    if (adr){
        return new espOstream(new espFsStreambuf(adr, len));
    }
    return NULL;
}


std::ostream* replaceFsImage(FsType fs){
    uint32_t address;
    uint32_t size;
    switch (fs)
    {
    case FS_SPIFFS:{
        const esp_partition_t* pt = esp_partition_find_first(ESP_PARTITION_TYPE_DATA, ESP_PARTITION_SUBTYPE_DATA_SPIFFS, NULL);
        address = pt->address;
        size = pt->size;
        }break;
    case FS_ESPFS:
        address = espFsFlashAddress;
        size = espFsFlashSize;
        break;
    default:
        LOGE("Unknown FsType %d to replace image", fs);
        return NULL;
    }
    //  Erase the flash range. address and size must be align to 4kB
    spi_flash_erase_range(address, size);
    
    //  Create stream to write the image
    return new espOstream(new espFsStreambuf(espFsFlashAddress, espFsFlashSize));
}
