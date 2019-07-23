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
}
#include "../SoftRobot/UdpCom.h"
#include "module_srcommand.h"

#include "ws_command.h"
#include "ws_task.h"
#include "ws_fs.h"

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
 * Handle message from websocket
 */
void wsOnMessageWs(WebSocketInputStreambuf* pWebSocketInputStreambuf, WebSocket* pWebSocket) {
    ESP_LOGD(LOG_TAG, "before wsOnMessageWs heap size: %d", esp_get_free_heap_size());

    int16_t packetId = pWebSocketInputStreambuf->sgetc();
    ESP_LOGD(LOG_TAG, "packet type: %i", packetId);


    size_t bufferSize = 4096;
    char* pBuffer = new char[bufferSize];
    std::streamsize ssize = pWebSocketInputStreambuf->sgetn(pBuffer, bufferSize);
    if(ssize>=bufferSize) {
        ESP_LOGI(LOG_TAG ,"WS command to long (longer than 4096)!!!!!!!!!");
        delete [] pBuffer;
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
            delete[] retBuffer;
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
            switch (id)
            {
                case PacketSettingsId::OFFLINE_MODE: {
                    bool new_offline_mode = pBufferI16[2];
                    if (new_offline_mode == offline_mode) break;
                    else offline_mode = new_offline_mode;
                    if(!offline_mode) { // exit offline mode
                        ESP_LOGD(LOG_TAG, "switch to development mode, stop running jsfile task");
                        wsDeleteJsfileTask();
                        heap_trace_dump();
                        heap_trace_start(HEAP_TRACE_LEAKS);                        
                        ESP_LOGI(LOG_TAG, "delete success");
                    }else if(!wsIsJsfileTaskRunning()){ // switch to offline mode
                        ESP_LOGI(LOG_TAG, "before wsCreateJsfileTask heap size: %d", esp_get_free_heap_size());
                        ESP_LOGD(LOG_TAG, "switch to offline mode, start running jsfile task");
                        wsCreateJsfileTask();
                    }
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
            delete[] retBuffer;
            retBuffer = NULL;
        }
    
        default:
            break;
    }

    if(pBuffer) delete[] pBuffer;
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
        case PacketSettingsId::OFFLINE_MODE:
            sprintf(buf, "%i ", pBufferI16[1]);
            break;
    
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
