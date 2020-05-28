#include <freertos/FreeRTOS.h>
//#include <freertos/heap_regions.h>
#include <freertos/task.h>
#include <esp_event.h>
#include <esp_event_loop.h>
#include <esp_log.h>
#include <esp_system.h>
#include <esp_wifi.h>
#include <nvs_flash.h>

#include "logging.h"
#include "WiFiEventHandler.h"

#include "ws_wifi.h"
#include "ws_fs.h"
#include "ws_http.h"

LOG_TAG("ws_main");

extern "C" void ws_main() {
    LOGD("Free heap at start: %d", esp_get_free_heap_size());
	// Boot the WiFi environment

    // Initialize NVS.
    esp_err_t err = nvs_flash_init();
    if (err == ESP_ERR_NVS_NO_FREE_PAGES || err == ESP_ERR_NVS_NEW_VERSION_FOUND) {
        // 1.OTA app partition table has a smaller NVS partition size than the non-OTA
        // partition table. This size mismatch may cause NVS initialization to fail.
        // 2.NVS partition contains data in new format and cannot be recognized by this version of code.
        // If this happens, we erase NVS partition and initialize NVS again.
        ESP_ERROR_CHECK(nvs_flash_erase());
        err = nvs_flash_init();
    }
    ESP_ERROR_CHECK( err );

	tcpip_adapter_init();
    LOGD("Free heap after tcp_ip: %d", esp_get_free_heap_size());
    SRWiFi::wifi.init();
    LOGD("Free heap after wifi: %d", esp_get_free_heap_size());

    // Mount the SPIFFS file system.
    #if defined(ESP_PLATFORM)
	    esp32_spiffs_mount();
    #endif /* ESP_PLATFORM */

    LOGD("Free heap after spiffs mount: %d", esp_get_free_heap_size());

    // start server
    createHttpServer();
    LOGD("Free heap after create server: %d", esp_get_free_heap_size());
}
