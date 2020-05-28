#include <stdlib.h>
#include <string.h>
#include <esp_log.h>
#include <esp_flash_encrypt.h>
#include <esp_ota_ops.h>
#include <esp_spiffs.h>
extern "C" {
    #include <esp_image_format.h>
}
#include "include/ota_flash.h"

#ifndef OTA_BUF_SIZE
#define OTA_BUF_SIZE 256
#endif

static const char *TAG = "ota_partition";

static void http_cleanup(esp_http_client_handle_t client)
{
    esp_http_client_close(client);
    esp_http_client_cleanup(client);
}

esp_err_t esp_https_ota_partition(const esp_http_client_config_t *config, const size_t dest_addr)
{
    if (!config) {
        ESP_LOGE(TAG, "esp_http_client config not found");
        return ESP_ERR_INVALID_ARG;
    }

    if (!config->cert_pem && !config->use_global_ca_store) {
        ESP_LOGE(TAG, "Server certificate not found, either through configuration or global CA store");
        return ESP_ERR_INVALID_ARG;
    }

    printf(">>> esp_http_client_init \n");
    esp_http_client_handle_t client = esp_http_client_init(config);
    printf("<<< esp_http_client_init \n");
    if (client == NULL) {
        ESP_LOGE(TAG, "Failed to initialise HTTP connection");
        return ESP_FAIL;
    }

    if (esp_http_client_get_transport_type(client) != HTTP_TRANSPORT_OVER_SSL) {
        ESP_LOGE(TAG, "Transport is not over HTTPS");
        return ESP_FAIL;
    }

    printf(">>> esp_http_client_open \n");
    esp_err_t err = esp_http_client_open(client, 0);
    printf("<<< esp_http_client_open \n");
    if (err != ESP_OK) {
        esp_http_client_cleanup(client);
        ESP_LOGE(TAG, "Failed to open HTTP connection: %s", esp_err_to_name(err));
        return err;
    }
    esp_http_client_fetch_headers(client);

    const esp_partition_t *update_partition = NULL;
    ESP_LOGI(TAG, "Starting OTA on addr: %#08x...", dest_addr);

    esp_err_t ota_write_err = ESP_OK;
    char *upgrade_data_buf = (char *)malloc(OTA_BUF_SIZE);
    if (!upgrade_data_buf) {
        ESP_LOGE(TAG, "Couldn't allocate memory to upgrade data buffer");
        return ESP_ERR_NO_MEM;
    }
    size_t binary_file_len = 0;
    while (1) {
        int data_read = esp_http_client_read(client, upgrade_data_buf, OTA_BUF_SIZE);
        if (data_read == 0) {
            ESP_LOGI(TAG, "Connection closed, all data received");
            break;
        }
        if (data_read < 0) {
            ESP_LOGE(TAG, "Error: SSL data read error");
            break;
        }
        if (data_read > 0) {
            if (esp_flash_encryption_enabled()) {
                ESP_LOGE(TAG, "Error: not able to handle encryped flash");
            }
            ota_write_err = spi_flash_write(dest_addr + binary_file_len, (const void*)upgrade_data_buf, data_read);
            if (ota_write_err != ESP_OK) {
                ESP_LOGE(TAG, "Error: spi_flash_write failed");
                break;
            }
            binary_file_len += data_read;
            ESP_LOGD(TAG, "Written image length %d", binary_file_len);
        }
    }
    free(upgrade_data_buf);
    http_cleanup(client);
    ESP_LOGI(TAG, "Total binary data length writen: %d", binary_file_len);

    return ESP_OK;
}
