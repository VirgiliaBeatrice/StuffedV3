/* OTA example

   This example code is in the Public Domain (or CC0 licensed, at your option.)

   Unless required by applicable law or agreed to in writing, this
   software is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
   CONDITIONS OF ANY KIND, either express or implied.
*/
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"

#include "esp_system.h"
#include "esp_wifi.h"
#include "esp_event_loop.h"
#include "esp_log.h"
#include "esp_ota_ops.h"
#include "esp_http_client.h"
#include "esp_https_ota.h"

#include "nvs.h"
#include "nvs_flash.h"

#include "include/wifi.h"
#include "include/ota_flash.h"
#include <string.h>

static const char *TAG = "ota_updater";
extern const uint8_t server_cert_pem_start[] asm("_binary_ca_cert_pem_start");
extern const uint8_t server_cert_pem_end[] asm("_binary_ca_cert_pem_end");

esp_err_t _http_event_handler(esp_http_client_event_t *evt)
{
    switch(evt->event_id) {
        case HTTP_EVENT_ERROR:
            ESP_LOGD(TAG, "HTTP_EVENT_ERROR");
            break;
        case HTTP_EVENT_ON_CONNECTED:
            ESP_LOGD(TAG, "HTTP_EVENT_ON_CONNECTED");
            break;
        case HTTP_EVENT_HEADER_SENT:
            ESP_LOGD(TAG, "HTTP_EVENT_HEADER_SENT");
            break;
        case HTTP_EVENT_ON_HEADER:
            ESP_LOGD(TAG, "HTTP_EVENT_ON_HEADER, key=%s, value=%s", evt->header_key, evt->header_value);
            break;
        case HTTP_EVENT_ON_DATA:
            ESP_LOGD(TAG, "HTTP_EVENT_ON_DATA, len=%d", evt->data_len);
            break;
        case HTTP_EVENT_ON_FINISH:
            ESP_LOGD(TAG, "HTTP_EVENT_ON_FINISH");
            break;
        case HTTP_EVENT_DISCONNECTED:
            ESP_LOGD(TAG, "HTTP_EVENT_DISCONNECTED");
            break;
    }
    return ESP_OK;
}

void ota_task(void * pvParameter)
{
    ESP_LOGI(TAG, "Starting OTA example...");

    wait_wifi_connection();
    ESP_LOGI(TAG, "Connect to Wifi !");

    // 1. update firmware
    esp_http_client_config_t firmware_http_config;
    memset(&firmware_http_config, 0, sizeof(firmware_http_config));
    firmware_http_config.url = CONFIG_FIRMWARE_UPGRADE_URL;
    firmware_http_config.cert_pem = (char *)server_cert_pem_start;
    firmware_http_config.event_handler = _http_event_handler;

    ESP_LOGI(TAG, "Start to Connect to Server: %s ....", firmware_http_config.url);

    esp_err_t ret = esp_https_ota(&firmware_http_config);

    if (ret == ESP_OK) {
        ESP_LOGI(TAG, "Firmware Upgrades Succeed");
    } else {
        ESP_LOGE(TAG, "Firmware Upgrades Failed");
    }

    // 2. update spiffs
    esp_http_client_config_t spiffs_http_config;
    memset(&spiffs_http_config, 0, sizeof(spiffs_http_config));
    spiffs_http_config.url = CONFIG_SPIFFS_UPGRADE_URL;
    spiffs_http_config.cert_pem = (char *)server_cert_pem_start;
    spiffs_http_config.event_handler = _http_event_handler;

    ESP_LOGI(TAG, "Start to Connect to spiffs.img Server: %s ....", spiffs_http_config.url);

    ret = spi_flash_erase_range(0x200000, 0x80000);
    ret = esp_https_ota_partition(&spiffs_http_config, 0x200000);

    if (ret == ESP_OK) {
        ESP_LOGI(TAG, "spiffs.img Upgrades Succeed");
        esp_restart();
    } else {
        ESP_LOGE(TAG, "spiffs.img Upgrades Failed");
    }

    // 2. update spiffs
    esp_http_client_config_t espfs_http_config;
    memset(&espfs_http_config, 0, sizeof(spiffs_http_config));
    espfs_http_config.url = CONFIG_ESPFS_UPGRADE_URL;
    espfs_http_config.cert_pem = (char *)server_cert_pem_start;
    espfs_http_config.event_handler = _http_event_handler;

    ESP_LOGI(TAG, "Start to Connect to espfs.img Server: %s ....", espfs_http_config.url);

    ret = spi_flash_erase_range(0x3d0000, 0x20000);
    ret = ret || esp_https_ota_partition(&espfs_http_config, 0x3d0000);


    if (ret == ESP_OK) {
        ESP_LOGI(TAG, "espfs.img Upgrades Succeed");
        esp_restart();
    } else {
        ESP_LOGE(TAG, "espfs.img Upgrades Failed");
    }


    while (1) {
        vTaskDelay(1000 / portTICK_PERIOD_MS);
    }
}

extern "C" void app_main()
{
    printf("----------------- OTA start ------------------- \n");

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

    initialise_wifi();
    xTaskCreate(&ota_task, "ota_task", 8192, NULL, 5, NULL);
}
