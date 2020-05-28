/**
 * Websocket server to communicate with browser
 */

#include "ws_ws.h"

#include <string>
#include <sstream>
#include <fstream>
#include <cstring>

#include "esp_heap_trace.h"
#include "esp_log.h"
#include "assert.h"
extern "C" {
#include "module_jslib.h"
#include "duktape_task.h"
#include "nvs_flash.h"
#include "nvs.h"
}
#include "../SoftRobot/UdpCom.h"
#include "module_srcommand.h"

#include "ws_command.h"
#include "ws_task.h"
#include "ws_fs.h"
#include "OTA.h"

static char LOG_TAG[] = "ws_ws";

static WebSocket* pWebSocket = NULL;
static SRWebSocketHandler webSocketHandler = SRWebSocketHandler();

bool offline_mode = false;   // offline: 1, synchronization || development: 0

void SRWebSocketHandler::onClose() {
    ESP_LOGV(LOG_TAG, "on close");
    pWebSocket = NULL;
}
void SRWebSocketHandler::onMessage(WebSocketInputStreambuf* pWebSocketInputStreambuf, WebSocket* pWebSocket){
    ESP_LOGV(LOG_TAG, "on message");
    wsOnMessageWs(pWebSocketInputStreambuf, pWebSocket);
}
void SRWebSocketHandler::onError(std::string error){
    ESP_LOGV(LOG_TAG, "on error");
    pWebSocket = NULL;
}

void wsOnConnected(WebSocket* pWS){
    ESP_LOGV(LOG_TAG, "Opened a Websocket connenction");
    pWebSocket = pWS;
    pWebSocket->setHandler(&webSocketHandler);
}

/*
static void saveToMainJsStream(const WebSocketInputStreambuf *content) {
    std::ofstream m_ofStream;

    std::string root_path = SPIFFS_MOUNTPOINT;
    m_ofStream.open(root_path + "/main/main.js", std::ofstream::out | std::ofstream::binary | std::ofstream::trunc);
    if (!m_ofStream.is_open()) {
        ESP_LOGV(LOG_TAG, "Failed to open file /spiffs/main/main.js for writing");
        return;
    }

    m_ofStream << content;

    m_ofStream.close();
}
*/

static void wsSend(void* data, size_t length) {
    if(!pWebSocket) {
        ESP_LOGV(LOG_TAG, "Unable to send packet, websocket is not connected");
        return;
    }
    pWebSocket->send((uint8_t*)data, length , WebSocket::SEND_TYPE_BINARY);
}

/**
 * inform pxt when delete / create js task finished
 */
void switchOfflineModeSuccess() {
    // return packet to pxt
    void* retBuffer = malloc(2 * sizeof(short));        // return packet for download success
    void* p = retBuffer;
    *(short*)p = PacketId::PI_SETTINGS;
    p = shiftPointer(p, 2);
    *(short*)p = PacketSettingsId::PSI_OFFLINE_MODE;
    p = shiftPointer(p, 2);

    wsSend((void*)retBuffer, 2 * sizeof(short));
    free(retBuffer);
    retBuffer = NULL;
}

/**
 * Handle message from websocket
 */
void wsOnMessageWs(WebSocketInputStreambuf* pWebSocketInputStreambuf, WebSocket* pWebSocket) {
    ESP_LOGD(LOG_TAG, "before wsOnMessageWs heap size: %d", esp_get_free_heap_size());

    int16_t packetId = pWebSocketInputStreambuf->sgetc();
    ESP_LOGD(LOG_TAG, "packet type: %i", packetId);


    size_t bufferSize = 4096;
    char* pBuffer = (char*)malloc(bufferSize);
    std::streamsize ssize = pWebSocketInputStreambuf->sgetn(pBuffer, bufferSize);
    if(ssize>=bufferSize) {
        ESP_LOGI(LOG_TAG ,"WS command to long (longer than 4096)!!!!!!!!!");
        free(pBuffer);
        return;
    }

    pWebSocketInputStreambuf->discard();

    ESP_LOGD(LOG_TAG, "+ WS packet");
    printPacket((const void*)pBuffer, ssize);

    switch (packetId)
    {
        case PacketId::PI_JSFILE: {
            // stop current running js task
            wsDeleteJsfileTask();

            // return packet to pxt
            short* retBuffer = (short*)malloc(2 * sizeof(short));        // return packet for download success
            *retBuffer = PacketId::PI_JSFILE;
            *(retBuffer+1) = 1;
            wsSend((void*)retBuffer, 4);
            free(retBuffer);
            retBuffer = NULL;

            break;
        }

        case PacketId::PI_COMMAND: {
            UdpCom_ReceiveCommand((void*)(pBuffer+2), *(int16_t*)(&pBuffer[2]), CS_WEBSOCKET);
            break;
        }

        case PacketId::PI_SETTINGS: {
            uint16_t* pBufferI16 = (uint16_t*)pBuffer;
            uint16_t id = pBufferI16[1];
            printf("---- rcv PI_SETTINGS with id: %i\n", id);
            switch (id)
            {
                case PacketSettingsId::PSI_OFFLINE_MODE: {
                    bool new_offline_mode = pBufferI16[2];
                    if (new_offline_mode == offline_mode) break;
                    else offline_mode = new_offline_mode;
                    if(!offline_mode) { // exit offline mode
                        ESP_LOGD(LOG_TAG, "switch to development mode, stop running jsfile task");
                        ESP_LOGI(LOG_TAG, "quit offline mode, deleting jsfile task...");
                        wsDeleteJsfileTask();
                        //heap_trace_dump();
                        //heap_trace_start(HEAP_TRACE_LEAKS);
                    }else if(!wsIsJsfileTaskRunning()){ // switch to offline mode
                        ESP_LOGI(LOG_TAG, "before wsCreateJsfileTask heap size: %d", esp_get_free_heap_size());
                        ESP_LOGD(LOG_TAG, "switch to offline mode, start running jsfile task");
                        wsCreateJsfileTask();
                    }
                    break;
                }
                case PacketSettingsId::PSI_FIRMWARE_INFO: {     // get version of firmware
                    string message = "";
                    message = message + "{" + "\"version\": \"" + FIRMWARE_VERSION + "\"}";
                    size_t ret_size = 2 * sizeof(short) + message.size();

                    // return packet to pxt
                    void* retBuffer = malloc(ret_size);        // return packet for download success
                    void* p = retBuffer;
                    *(short*)p = PacketId::PI_SETTINGS;
                    p = shiftPointer(p, 2);
                    *(short*)p = PacketSettingsId::PSI_FIRMWARE_INFO;
                    p = shiftPointer(p, 2);
                    pushPayload(p, message.c_str(), message.size());

                    wsSend((void*)retBuffer, ret_size);
                    free(retBuffer);
                    retBuffer = NULL;

                    break;
                }
                case PacketSettingsId::PSI_WRITE_NVS: {
                    const void* payload = (void*)(pBufferI16+2);
                    uint8_t dataType;
                    popPayloadNum(payload, dataType);
                    nvs_handle nvsHandle;
                    nvs_open("motor", NVS_READWRITE, &nvsHandle);

                    const char* key;
                    size_t key_len = popPayloadStr(payload, key);
                    switch (dataType) {
                        case DT_U8: {
                            uint8_t number;
                            popPayloadNum(payload, number);
                            nvs_set_u8(nvsHandle, key, number);
                            ESP_LOGD(LOG_TAG, "set nvs u8, key: %s, val: %i \n", key, number);
                            break;
                        }
                        case DT_I8: {
                            int8_t number;
                            popPayloadNum(payload, number);
                            nvs_set_i8(nvsHandle, key, number);
                            ESP_LOGD(LOG_TAG, "set nvs i8, key: %s, val: %i \n", key, number);
                            break;
                        }
                        case DT_U16: {
                            uint16_t number;
                            popPayloadNum(payload, number);
                            nvs_set_u16(nvsHandle, key, number);
                            ESP_LOGD(LOG_TAG, "set nvs u16, key: %s, val: %i \n", key, number);
                            break;
                        }
                        case DT_I16: {
                            int16_t number;
                            popPayloadNum(payload, number);
                            nvs_set_i16(nvsHandle, key, number);
                            ESP_LOGD(LOG_TAG, "set nvs i16, key: %s, val: %i \n", key, number);
                            break;
                        }
                        case DT_U32: {
                            uint32_t number;
                            popPayloadNum(payload, number);
                            nvs_set_u32(nvsHandle, key, number);
                            ESP_LOGD(LOG_TAG, "set nvs u32, key: %s, val: %i \n", key, number);
                            break;
                        }
                        case DT_I32: {
                            int32_t number;
                            popPayloadNum(payload, number);
                            nvs_set_i32(nvsHandle, key, number);
                            ESP_LOGD(LOG_TAG, "set nvs i32, key: %s, val: %i \n", key, number);
                            break;
                        }
                        case DT_STR: {
                            const char* val;
                            //size_t val_len =
                            popPayloadStr(payload, val);
                            nvs_set_str(nvsHandle, key, val);
                            ESP_LOGD(LOG_TAG, "set nvs str, key: %s, val: %s \n", key, val);
                            break;
                        }
                        default:
                            break;
                    }
                    nvs_commit(nvsHandle);
                    nvs_close(nvsHandle);

                    void* retBuffer = malloc(5 + key_len);
                    void* p = retBuffer;
                    pushPayload(p, (void*)pBuffer, 5 + key_len);
                    wsSend(retBuffer, 5 + key_len);
                    free(retBuffer);

                    break;
                }
                case PacketSettingsId::PSI_READ_NVS: {
                    const void* payload = (void*)(pBufferI16+2);
                    uint8_t dataType;
                    popPayloadNum(payload, dataType);
                    nvs_handle nvsHandle;
                    nvs_open("motor", NVS_READWRITE, &nvsHandle);

                    const char* key;
                    size_t key_len = popPayloadStr(payload, key);

                    switch (dataType) {
                        case DT_U8: {
                            uint8_t number;
                            esp_err_t err = nvs_get_u8(nvsHandle, key, &number);
                            if (err != ESP_OK) break;

                            void* retBuffer = malloc(5 + key_len + 1);
                            void* p = retBuffer;
                            pushPayload(p, (void*)pBuffer, 5 + key_len);
                            pushPayload(p, &number, 1);
                            ESP_LOGD(LOG_TAG, "get nvs u8, key: %s, val: %i \n", key, number);
                            wsSend(retBuffer, 5 + key_len + 1);
                            free(retBuffer);
                            break;
                        }
                        case DT_I8: {
                            int8_t number;
                            esp_err_t err = nvs_get_i8(nvsHandle, key, &number);
                            if (err != ESP_OK) break;

                            void* retBuffer = malloc(5 + key_len + 1);
                            void* p = retBuffer;
                            pushPayload(p, (void*)pBuffer, 5 + key_len);
                            pushPayload(p, &number, 1);
                            ESP_LOGD(LOG_TAG, "get nvs i8, key: %s, val: %i \n", key, number);
                            wsSend(retBuffer, 5 + key_len + 1);
                            free(retBuffer);
                            break;
                        }
                        case DT_U16: {
                            uint16_t number;
                            esp_err_t err = nvs_get_u16(nvsHandle, key, &number);
                            if (err != ESP_OK) break;

                            void* retBuffer = malloc(5 + key_len + 2);
                            void* p = retBuffer;
                            pushPayload(p, (void*)pBuffer, 5 + key_len);
                            pushPayload(p, &number, 2);
                            ESP_LOGD(LOG_TAG, "get nvs u16, key: %s, val: %i \n", key, number);
                            wsSend(retBuffer, 5 + key_len + 2);
                            free(retBuffer);
                            break;
                        }
                        case DT_I16: {
                            int16_t number;
                            esp_err_t err = nvs_get_i16(nvsHandle, key, &number);
                            if (err != ESP_OK) break;

                            void* retBuffer = malloc(5 + key_len + 2);
                            void* p = retBuffer;
                            pushPayload(p, (void*)pBuffer, 5 + key_len);
                            pushPayload(p, &number, 2);
                            ESP_LOGD(LOG_TAG, "get nvs i16, key: %s, val: %i \n", key, number);
                            wsSend(retBuffer, 5 + key_len + 2);
                            free(retBuffer);
                            break;
                        }
                        case DT_U32: {
                            uint32_t number;
                            esp_err_t err = nvs_get_u32(nvsHandle, key, &number);
                            if (err != ESP_OK) break;

                            void* retBuffer = malloc(5 + key_len + 4);
                            void* p = retBuffer;
                            pushPayload(p, (void*)pBuffer, 5 + key_len);
                            pushPayload(p, &number, 4);
                            ESP_LOGD(LOG_TAG, "get nvs u32, key: %s, val: %i \n", key, number);
                            wsSend(retBuffer, 5 + key_len + 4);
                            free(retBuffer);
                            break;
                        }
                        case DT_I32: {
                            int32_t number;
                            esp_err_t err = nvs_get_i32(nvsHandle, key, &number);
                            if (err != ESP_OK) break;

                            void* retBuffer = malloc(5 + key_len + 4);
                            void* p = retBuffer;
                            pushPayload(p, (void*)pBuffer, 5 + key_len);
                            pushPayload(p, &number, 4);
                            ESP_LOGD(LOG_TAG, "get nvs i32, key: %s, val: %i \n", key, number);
                            wsSend(retBuffer, 5 + key_len + 4);
                            free(retBuffer);
                            break;
                        }
                        case DT_STR: {
                            size_t required_size;
                            esp_err_t err = nvs_get_str(nvsHandle, key, NULL, &required_size);
                            if (err != ESP_OK) break;
                            char* val = (char*)malloc(required_size);
                            nvs_get_str(nvsHandle, key, val, &required_size);

                            void* retBuffer = malloc(5 + key_len + required_size);
                            void* p = retBuffer;
                            pushPayload(p, (void*)pBuffer, 5 + key_len);
                            pushPayload(p, val, required_size);
                            ESP_LOGD(LOG_TAG, "get nvs str, key: %s, val: %s \n", key, val);
                            wsSend(retBuffer, 5 + key_len + required_size);
                            free(retBuffer);
                            free(val);
                            break;
                        }
                        default:
                            break;
                    }

                    nvs_close(nvsHandle);
                    break;
                }

                default:
                    ESP_LOGV(LOG_TAG, "Unknown packet settings id (%i)", pBufferI16[1]);
                    break;
            }
            break;
        }

        case PacketId::PI_PINGPONG: {
            short* retBuffer = (short*)malloc(1 * sizeof(short));
            *retBuffer = PacketId::PI_PINGPONG;
            wsSend((void*)retBuffer, 2);
            free(retBuffer);
            retBuffer = NULL;
            break;
        }

        case PacketId::PI_OTA: {
            short* retBuffer = (short*)malloc(1 * sizeof(short));
            *retBuffer = PacketId::PI_OTA;
            wsSend((void*)retBuffer, 2);
            free(retBuffer);
            retBuffer = NULL;

            updateFirmware();
            break;
        }

        default:
            break;
    }

    if(pBuffer) free(pBuffer);
}

/**
 * Handle message from softrobot
 * send command to browser and jsfile task
 */
void wsOnMessageSr(UdpRetPacket& ret) {
    ESP_LOGD(LOG_TAG, "+ SR Packet");
    printPacketCommand(ret.bytes + 2, ret.length);

    if (ret.count == CS_WEBSOCKET) {
        // send packet to browser
        char* buf = (char*)malloc(ret.length+2);
        std::memcpy(buf+2, ret.bytes+2, ret.length);
        *(short*)buf = 2;
        wsSend(buf, ret.length+2);
        free(buf);
        ESP_LOGV(LOG_TAG, "Packet softrobot -> websocket");
    } else if (ret.count == CS_DUKTAPE) {
        // send packet to duktape task
        if(!wsIsJsfileTaskRunning()) return;
        commandMessageHandler(ret);
        ESP_LOGD(LOG_TAG, "Packet softrobot -> jsfile");
    } else {
        ESP_LOGV(LOG_TAG, "Cannot find destination: %i", ret.count);
    }
}

void printPacketJsfile(const void* pBuffer, size_t len) {
    ESP_LOGD(LOG_TAG, "|- PacketId: PI_JSFILE");

    /* deprecated: no content any more, use /ws_jsfile url to transfer file instead */
    // char buf[1024];
    // memcpy(buf, pBuffer, len);
    // buf[len] = '\0';
    // ESP_LOGD(LOG_TAG, "|- Content: %s", buf);
}

void printPacketCommand(const void* pBuffer, size_t len) {
    int16_t* pBufferI16 = (int16_t*)pBuffer;
    int8_t* pBufferI8 = (int8_t*)pBuffer;
    ESP_LOGD(LOG_TAG, "|- PacketId: PI_COMMAND");

    uint16_t length = pBufferI16[0];
    ESP_LOGD(LOG_TAG, "|- Content:");
    ESP_LOGD(LOG_TAG, "   |- Length: %i", length);
    ESP_LOGD(LOG_TAG, "   |- CommandId: %i", pBufferI16[1]);

    char bufI16[1024]={'\0'};
    for(size_t i = 2; i < len/2; i++){
        sprintf(bufI16+strlen(bufI16), "%i, ", pBufferI16[i]);
    }
    ESP_LOGD(LOG_TAG, "   |- Command I16: %s", bufI16);

    char bufI8[1024]={'\0'};
    for(size_t i = 4; i < len; i++){
        sprintf(bufI8+strlen(bufI8), "%i, ", pBufferI8[i]);
    }
    ESP_LOGD(LOG_TAG, "   |- Command I8: %s", bufI8);

    // test
    if (length != len) {
        printf("!!!! length: %d, len: %d \n", length, len);
    }
    assert(length == len);
}

void printPacketSettings(const void* pBuffer, size_t len) {
    int16_t* pBufferI16 = (int16_t*)pBuffer;
    ESP_LOGD(LOG_TAG, "|- PacketId: PI_SETTINGS");
    uint16_t id = pBufferI16[0];
    ESP_LOGD(LOG_TAG, "   |- Setting type: %s", getPacketSettingsIdStr(id).c_str());
    char buf[1024]={'\0'};
    switch (id)
    {
        case PacketSettingsId::PSI_OFFLINE_MODE:
            sprintf(buf, "%i ", pBufferI16[1]);
            break;
        case PacketSettingsId::PSI_FIRMWARE_INFO:
        default:
            break;
    }
    ESP_LOGD(LOG_TAG, "   |- Value: %s", buf);
}

void printPacketPingPong() {
    ESP_LOGD(LOG_TAG, "|- PacketId: PI_PINGPONG");
}

void printPacket(const void* pBuffer, size_t len) {
    const int16_t* pBufferI16 = (const int16_t*)pBuffer;
    switch (*pBufferI16)
    {
        case PacketId::PI_JSFILE: {
            printPacketJsfile((char*)pBuffer+2, len-2);
            break;
        }

        case PacketId::PI_COMMAND: {
            printPacketCommand((char*)pBuffer+2, len-2);
            break;
        }

        case PacketId::PI_SETTINGS: {
            printPacketSettings((char*)pBuffer+2, len-2);
            break;
        }

        case PacketId::PI_PINGPONG: {
            printPacketPingPong();
            break;
        }

        default: {
            ESP_LOGD(LOG_TAG, "- PacketId: UNRECOGNIZED (%i)", pBufferI16[0]);
            break;
        }
    }
}

void printDTPacket(const void* pBuffer, size_t len) {
    ESP_LOGD(LOG_TAG, "+ DT packet");
    printPacketCommand(pBuffer, len);
}
