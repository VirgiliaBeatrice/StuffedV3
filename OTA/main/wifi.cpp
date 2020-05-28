#include "include/wifi.h"

#include <string.h>

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"

#include "esp_system.h"
#include "esp_wifi.h"
#include "esp_event_loop.h"
#include "esp_log.h"
#include "nvs_flash.h"

#include "CPPNVS.h"

#define WIFI_NVS_NAME "wifinvs"
#define WIFI_LAST_AP_KEY "lastAP"

/* FreeRTOS event group to signal when we are connected & ready to make a request */
static EventGroupHandle_t wifi_event_group;

/* The event group allows multiple bits for each event,
   but we only care about one event - are we connected
   to the AP with an IP? */
const int CONNECTED_BIT = BIT0;

static const char *TAG = "wifi";

static esp_err_t event_handler(void *ctx, system_event_t *event)
{
    switch (event->event_id) {
    case SYSTEM_EVENT_STA_START:
        esp_wifi_connect();
        break;
    case SYSTEM_EVENT_STA_GOT_IP:
        xEventGroupSetBits(wifi_event_group, CONNECTED_BIT);
        break;
    case SYSTEM_EVENT_STA_DISCONNECTED:
        /* This is a workaround as ESP32 WiFi libs don't currently
           auto-reassociate. */
        esp_wifi_connect();
        xEventGroupClearBits(wifi_event_group, CONNECTED_BIT);
        break;
    default:
        break;
    }
    return ESP_OK;
}

static wifi_config_t get_sta_info() {
    NVS wifi_nvs = NVS(WIFI_NVS_NAME);
    int32_t last_ap;
    wifi_nvs.get(WIFI_LAST_AP_KEY, last_ap);
    assert(last_ap >= 0 && last_ap < 10);
    char ssid_key[6], pass_key[6];
    sprintf(ssid_key, "ssid%1d", last_ap);
    sprintf(pass_key, "pass%1d", last_ap);
    std::string ssid, pass;
    wifi_nvs.get(ssid_key, ssid);
    wifi_nvs.get(pass_key, pass);

    wifi_config_t wifi_config;
    memset(&wifi_config, 0, sizeof(wifi_config));
	memcpy(wifi_config.sta.ssid, ssid.c_str(), ssid.size());
	memcpy(wifi_config.sta.password, pass.c_str(), pass.size());
	wifi_config.sta.bssid_set = 0;

    return wifi_config;
}

void initialise_wifi(void)
{
    tcpip_adapter_init();
    wifi_event_group = xEventGroupCreate();
    ESP_ERROR_CHECK( esp_event_loop_init(event_handler, NULL) );
    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    ESP_ERROR_CHECK( esp_wifi_init(&cfg) );
    ESP_ERROR_CHECK( esp_wifi_set_storage(WIFI_STORAGE_RAM) );
    wifi_config_t wifi_config = get_sta_info();
    ESP_LOGI(TAG, "Setting WiFi configuration SSID %s, password %s ...", wifi_config.sta.ssid, wifi_config.sta.password);
    ESP_ERROR_CHECK( esp_wifi_set_mode(WIFI_MODE_STA) );
    ESP_ERROR_CHECK( esp_wifi_set_config(ESP_IF_WIFI_STA, &wifi_config) );
    ESP_ERROR_CHECK( esp_wifi_start() );
}

void wait_wifi_connection() {
    /* Wait for the callback to set the CONNECTED_BIT in the
       event group.
    */
    xEventGroupWaitBits(wifi_event_group, CONNECTED_BIT,
                        false, true, portMAX_DELAY);
}
