/**
 * API for controlling hardware
 */

#include "module_jslib.h"

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <stdio.h>
#include <esp_log.h>

#include "dukf_utils.h"
#include "duktape_utils.h"
#include "duktape_event.h"
#include "duktape_task.h"
#include "../softRobot/UdpCom.h"

static uint32_t stash_key_callback = 0;    // stash key for packet callback function
static size_t return_packet_buffer_size;   // the size of buffer we want to send

static duk_ret_t block_pause(duk_context* ctx){
    duk_int_t t = duk_get_int(ctx, -1);

    if(t<=0) return 0;
    JSThread* th=NULL;
    for(int i=0; i<NJSTHREADS; ++i){
        if (jsThreads[i].ctx == ctx){
            th = &jsThreads[i];
            break;
        }
    }
    assert(th != NULL);
    duk_suspend(th->ctx, &th->state);
    unlock_heap();
    vTaskDelay(pdMS_TO_TICKS(t));
    lock_heap();
    duk_resume(th->ctx, &th->state);
    return 0;
}

/*
* print heap size
* [0] - string
*/
static duk_ret_t printHeap(duk_context* ctx){
    const char* str = duk_get_string(ctx, -1);
    dukf_log_heap(str);
    duk_pop(ctx);
    return 0;
}
/*
* print remained native stack size.
* [0] - string
*/
static duk_ret_t printStackRemain(duk_context* ctx){
    const char* str = duk_get_string(ctx, -1);
    duktape_print_stack_remain(ctx, str);
    duk_pop(ctx);
    return 0;
}

static duk_ret_t print(duk_context* ctx){
    const char* str = duk_get_string(ctx, -1);
    ESP_LOGI("PRINT", "%s", str);
    duk_pop(ctx);

    return 0;
}

static duk_ret_t printNumber(duk_context* ctx) {
    ESP_LOGI("PRINT NUMBER", "%lf", (double)duk_get_number(ctx, -1));
    duk_pop(ctx);

    return 0;
}


/*
* register a callback with one parameter: arraybuffer
* the callback would be called when hardware want to send a packet
* [0] - callback function
*/
static duk_ret_t register_callback(duk_context* ctx){
    if(stash_key_callback!=0) esp32_duktape_stash_delete(ctx, stash_key_callback);             // delete last registered callback
    if(!duk_is_function(ctx, -1)) {
        printf("register_packet_callback: not a function\n");
        duk_pop(ctx);
        return 0;
    }

    stash_key_callback = esp32_duktape_stash_array(ctx, 1);

    if(stash_key_callback==0) printf("register_packet_callback: register callback failed.\n");
    else printf("register_packet_callback: register callback success with stash key - %d\n", stash_key_callback);

    return 0;
}

/**
 * Provide callback function with one parameters: arraybuffer
 */
static int return_packet_dataProvider(duk_context *ctx, void *context) {
    void* p = duk_push_buffer(ctx, return_packet_buffer_size, 0);
    memcpy(p, context, return_packet_buffer_size);
    duk_push_buffer_object(ctx, -1, 0, return_packet_buffer_size, DUK_BUFOBJ_ARRAYBUFFER);
    duk_replace(ctx, -2);

    return 1;
}

/*
* call registered callback
* [0] - context
* [1] - buffer of command packet
* [2] - size of the buffer
*/
void return_packet_to_jsfile(void* buffer, size_t buffer_size) {
    if(stash_key_callback==0) return;

    return_packet_buffer_size = buffer_size;

    event_newCallbackRequestedEvent(
      ESP32_DUKTAPE_CALLBACK_STATIC_TYPE_FUNCTION,
      stash_key_callback, // Stash key for stashed callback array
      return_packet_dataProvider, // Data provider parameter
      buffer // Context parameter
   );
}

/**
 * Send command to softrobot with onr parameter:
 * [0] - payload
 */
static duk_ret_t send_command(duk_context* ctx) {
    size_t length;

    void* data = duk_get_buffer_data(ctx, -2, &length);

    UdpCom_ReceiveCommand(data, length, 1);

    duk_pop_2(ctx);

    return 0;
}

static int nullDataProvider(duk_context* ctx, void* data) {
    return 0;
}
/**
 * Push a new event with no parameters into event queue
 * function pushEventQueue(func: () => void): void;
 */
static duk_ret_t pushEventQueue(duk_context* ctx) {
    uint32_t key = esp32_duktape_stash_array(ctx, 1);
    event_newCallbackRequestedEvent(
        ESP32_DUKTAPE_CALLBACK_TYPE_FUNCTION,
        key,
        nullDataProvider,
        NULL
    );

    return 0;
}

duk_ret_t ModuleJSLib(duk_context *ctx){
    ADD_FUNCTION("block_pause",         block_pause,        1);
    ADD_FUNCTION("register_callback",   register_callback,  1);
    ADD_FUNCTION("send_command",        send_command,       1);
    ADD_FUNCTION("print_heap",          printHeap,          1);
    ADD_FUNCTION("print_stack_remain",  printStackRemain,   1);
    ADD_FUNCTION("print",               print,              1);
    ADD_FUNCTION("printNumber",         printNumber,        1);
    ADD_FUNCTION("pushEventQueue",      pushEventQueue,     1);
    return 0;
}
