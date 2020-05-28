#include "WroomEnv.h"
#include <stdio.h>

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"
#include "esp_log.h"
#ifndef _WIN32
#include "esp_system.h"
#include "esp_spi_flash.h"
#include "esp_task_wdt.h"
#include "esp_log.h"
#include "nvs_flash.h"
#include "rom/uart.h"
#include "ws_task.h"
#include "espfs.h"
#endif
#include "monitor.h"
#include "ws_fs.h"
#include "ws_ws.h"
#include "softRobot/UdpCom.h"
#include "softRobot/Movement.h"
#include "duktapeEsp32/include/logging.h"

extern "C" {
    #include "websocketServer/CoreDumpReader.h"
}

LOG_TAG("main");

extern "C" void softRobot_main();
#if defined USE_DUKTAPE && !defined _WIN32
extern "C" void duktape_main();
extern "C" void ws_main();
#endif

#if CONFIG_HEAP_TRACING
#include "esp_heap_trace.h"
#define NUM_RECORDS 40
static heap_trace_record_t trace_record[NUM_RECORDS]; // This buffer must be in internal RAM
#endif

void setLogLevel(){
#ifndef _WIN32
    //  Set all log level to DEBUG
	esp_log_level_set("*", ESP_LOG_INFO);

	//  Components set to info
    esp_log_level_set("clk", ESP_LOG_INFO);
    esp_log_level_set("CPPNVS", ESP_LOG_INFO);
    esp_log_level_set("cpu_start", ESP_LOG_INFO);
    esp_log_level_set("esp_dbg_stubs", ESP_LOG_INFO);
    esp_log_level_set("espfs", ESP_LOG_VERBOSE);
    esp_log_level_set("event", ESP_LOG_INFO);
    esp_log_level_set("system_event", ESP_LOG_INFO);
    esp_log_level_set("heap_init", ESP_LOG_INFO);
    esp_log_level_set("nvs", ESP_LOG_INFO);
    esp_log_level_set("phy_init", ESP_LOG_INFO);
    esp_log_level_set("pthread", ESP_LOG_INFO);
    esp_log_level_set("efuse", ESP_LOG_INFO);
    esp_log_level_set("Task", ESP_LOG_INFO);
    esp_log_level_set("tcpip_adapter", ESP_LOG_INFO);
    esp_log_level_set("RTC_MODULE", ESP_LOG_INFO);

    // Components set to warn
    esp_log_level_set("phy", ESP_LOG_WARN);
    esp_log_level_set("system_api", ESP_LOG_WARN);
    esp_log_level_set("wifi", ESP_LOG_WARN);
    esp_log_level_set("gpio", ESP_LOG_WARN);
    esp_log_level_set("I2S", ESP_LOG_WARN);

    //  Application
    esp_log_level_set("main", ESP_LOG_INFO);
    //  SoftRobot
//    esp_log_level_set("sr_main", ESP_LOG_DEBUG);
//  esp_log_level_set("Uart", ESP_LOG_DEBUG);
//  esp_log_level_set("UdpCom", ESP_LOG_DEBUG);
//  esp_log_level_set("AllB", ESP_LOG_DEBUG);
//  esp_log_level_set("Board", ESP_LOG_DEBUG);
//    esp_log_level_set("MotorDriver", ESP_LOG_DEBUG);
//    esp_log_level_set("Movement", ESP_LOG_DEBUG);
    esp_log_level_set("FileTransferWebSocketHandler", ESP_LOG_DEBUG);
    esp_log_level_set("espfs", ESP_LOG_DEBUG);

    //  Web Server
    esp_log_level_set("Socket", ESP_LOG_INFO);
    esp_log_level_set("HttpServer", ESP_LOG_INFO);
    esp_log_level_set("HttpServerTask", ESP_LOG_INFO);
    esp_log_level_set("HttpRequest", ESP_LOG_INFO);
    esp_log_level_set("HttpParser", ESP_LOG_INFO);
    esp_log_level_set("HttpResponse", ESP_LOG_INFO);
    esp_log_level_set("PathHandler", ESP_LOG_INFO);
    esp_log_level_set("WiFi", ESP_LOG_INFO);
    esp_log_level_set("WiFiEventHandler", ESP_LOG_INFO);
    esp_log_level_set("ws_fs", ESP_LOG_INFO);
    //esp_log_level_set("ws_wifi", ESP_LOG_DEBUG);
    esp_log_level_set("ws_task", ESP_LOG_INFO);
    esp_log_level_set("ws_main", ESP_LOG_INFO);
    esp_log_level_set("ws_form", ESP_LOG_INFO);
    //esp_log_level_set("ws_ws", ESP_LOG_DEBUG);

    //  Duktape
    esp_log_level_set("duktape_task", ESP_LOG_INFO);
    esp_log_level_set("duktape_event", ESP_LOG_INFO);
    esp_log_level_set("duk_utils", ESP_LOG_INFO);
    esp_log_level_set("dukf_utils", ESP_LOG_INFO);
    esp_log_level_set("module_dukf", ESP_LOG_INFO);
    esp_log_level_set("modules", ESP_LOG_INFO);
    esp_log_level_set("module_os", ESP_LOG_INFO);
    esp_log_level_set("module_iot", ESP_LOG_DEBUG);
    esp_log_level_set("log", ESP_LOG_INFO);
#endif
}

extern "C" void readAutoStart() {
    nvs_handle nvsHandle;
    nvs_open("motor", NVS_READWRITE, &nvsHandle);
    uint8_t auto_start;
    esp_err_t err = nvs_get_u8(nvsHandle, "auto_start", &auto_start);
    if (err != ESP_OK) {
        auto_start = false;     // not auto start on default
        nvs_set_u8(nvsHandle, "auto_start", auto_start);
        nvs_commit(nvsHandle);
    }

    if (haveUnmarkedCoreDump()) {
        auto_start = false;
        markCoreDump();
    }

    offline_mode = auto_start;
    nvs_close(nvsHandle);
}

extern "C" void app_main(){
    #ifndef _WIN32
    {
    	esp_log_level_set("*", ESP_LOG_INFO);
        esp_chip_info_t chip_info;
        esp_chip_info(&chip_info);
        printf("\n\n\n\n\n\nThis is ESP32 chip with %d CPU cores, WiFi%s%s, ",
            chip_info.cores,
            (chip_info.features & CHIP_FEATURE_BT) ? "/BT" : "",
            (chip_info.features & CHIP_FEATURE_BLE) ? "/BLE" : "");
        printf("silicon revision %d, ", chip_info.revision);
        printf("%dMB %s flash\n", spi_flash_get_chip_size() / (1024 * 1024),
                (chip_info.features & CHIP_FEATURE_EMB_FLASH) ? "embedded" : "external");
        LOGI("Initial heap size: %d", esp_get_free_heap_size());
    }
    #if CONFIG_HEAP_TRACING
    ESP_ERROR_CHECK( heap_trace_init_standalone(trace_record, NUM_RECORDS) );
    #endif
    #endif

    //  set log level
    setLogLevel();

    //  Start soft robot controller
    LOGI("Initialize soft robot...");
    softRobot_main();

#if 1
	//  Start file system (espFs)
    int flashSize = 128 * 1024;
	espFsInit((void *)0x3d0000, flashSize);
    //LOGI("after espFsInit heap size: %d", esp_get_free_heap_size());

    //  Start web server with web socket
    ws_main();

    //LOGI("after ws_main heap size: %d", esp_get_free_heap_size());

    //  start soft robot's udp command server.
    udpCom.Start();   //  start UDP server.

    // init movement manager
    initMovementDS();

    //  start DukTape, javascript engine and run /main/main*.js
    readAutoStart();

	if(offline_mode && !wsIsJsfileTaskRunning()) {
        wsCreateJsfileTask();
        LOGI("Start running default jsfile task");
    } else {
        LOGI("JS is NOT started.");
    }

#endif
    //  start monitor
    Monitor::theMonitor.Init();
    Monitor::theMonitor.Run();  //  monitor start. never return;
}
