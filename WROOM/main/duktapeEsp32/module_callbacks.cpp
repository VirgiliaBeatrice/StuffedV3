#include "module_callbacks.h"

//////////////////////// struct for store callbacks //////////////////////////////////
struct CallbackFunc {
    uint8_t touchSensorId;   // 1bit exceeds + 7bit touchSensorId
    int16_t threshold;
    uint32_t callbackId;

    struct CallbackFunc* next;
};

static struct CallbackFunc* head;
static struct CallbackFunc* tail;

static void addNode(struct CallbackFunc* node) {
    tail->next = node;
    tail = node;
}

static uint8_t getTouchSensorId(struct CallbackFunc* node) {
    return node->touchSensorId & 0b01111111;
}
static duk_bool_t getExceeds(struct CallbackFunc* node) {
    return (node->touchSensorId) >> 7;
}

///////////////////////// API for JS //////////////////////////////////////

// function bindTouchCallback(touchSensorId: number, threshold: number, exceeds: boolean, callback: () => void): void
static duk_ret_t bindTouchCallback(duk_context* ctx){
    struct CallbackFunc* newNode = (struct CallbackFunc*)malloc(sizeof(struct CallbackFunc));
    newNode->touchSensorId = duk_get_int(ctx, -4);
    newNode->threshold = duk_get_number(ctx, -3);
    duk_bool_t exceeds = duk_get_boolean(ctx, -2);
    newNode->touchSensorId = newNode->touchSensorId | (exceeds << 7);

    newNode->callbackId = esp32_duktape_stash_array(ctx, 1);
    duk_pop_3(ctx);

    newNode->next = NULL;

    addNode(newNode);

    return 0;
}

static int nullDataProvider(duk_context* ctx, void* data) {
    return 0;
}

void onRcvTouchMessage(const vector<int16_t> &oldTouch, const vector<int16_t> &newTouch) {
    struct CallbackFunc* node = head->next;
    while (node) {
        uint8_t touchSensorId = getTouchSensorId(node);
        struct CallbackFunc* tmp = node;
        node = node->next;

        if (touchSensorId >= oldTouch.size() || touchSensorId >= newTouch.size()) continue;
        int16_t oldV = oldTouch[touchSensorId], newV = newTouch[touchSensorId];
        int16_t lowV = oldV < newV ? oldV : newV, highV = oldV > newV ? oldV : newV;
        if (lowV >= tmp->threshold || highV < tmp->threshold) continue;
        if ((newV > oldV) != getExceeds(tmp)) continue;

        // call callback
        event_newCallbackRequestedEvent(
            ESP32_DUKTAPE_CALLBACK_STATIC_TYPE_FUNCTION,
            tmp->callbackId,
            nullDataProvider,
            NULL
        );
    }
}

extern "C" void callbacksBeforeStopJSTask() {
    if (!head) return;
    // clear registered functions
    struct CallbackFunc* p = head;
    while (p->next) {
        struct CallbackFunc* tmp = p->next;
        p->next = tmp->next;

        free(tmp);
    }
}

extern "C" duk_ret_t ModuleCallbacks(duk_context *ctx) {
    head = (struct CallbackFunc*)malloc(sizeof(struct CallbackFunc));
    head->next = NULL;
    tail = head;

    ADD_FUNCTION("bindTouchCallback", bindTouchCallback, 4);

    return 0;
}
