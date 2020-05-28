#include <iostream>
#include <cstring>
#include <unordered_map>
#include <string>

extern "C" {

#include <duktape.h>
#include "duktape_event.h"
#include "duktape_utils.h"
#include "duktape_task.h"
#include "logging.h"
}
#include "module_srcommand.h"
#include "module_srmovement.h"
#include "module_device.h"
#include "../websocketServer/ws_ws.h"
#include "../softRobot/UdpCom.h"
#include "../softRobot/AllBoards.h"
#include "../softRobot/Utils.h"

#define PRINT_DUKTAPE_PACKET 1

LOG_TAG("SRCmd");

static std::unordered_map<std::string, uint32_t> callback_stash_keys;

static struct MyBoardInfo {
    uint8_t nTarget;
    uint8_t nMotor;
    uint8_t nCurrent;
    uint8_t nForces;
    uint8_t nTouch;
} boardInfo = {0, 0, 0, 0, 0};

////////////////////////////////////////////////////////
//////////////////////// tool functions ////////////////
////////////////////////////////////////////////////////

// push prop (number) of object (index: -1) to payload, increment payload pointer
template <class T>
static void pushCtx2PayloadNum(duk_context* ctx, void* &payload, const char* prop) {
    duk_get_prop_string(ctx, -1, prop);
    T num = (T)duk_get_int(ctx, -1);
    pushPayload(payload, &num, sizeof(T));
    duk_pop(ctx);
}

// push prop (number array) of object (index: -1) to payload, increment payload pointer
template <class T>
static void pushCtx2PayloadNumArray(duk_context* ctx, void* &payload, const char* prop) {
    duk_get_prop_string(ctx, -1, prop);
    assert(duk_is_array(ctx, -1));
    size_t n = duk_get_length(ctx, -1);

    for(int i=0; i<n; i++){
        duk_get_prop_index(ctx, -1, i);
        T num = (T)duk_get_int(ctx, -1);
        pushPayload(payload, &num, sizeof(T));
        duk_pop(ctx);
    }

    duk_pop(ctx);
}

// pop payload (number) and put it as prop to object (index: -1), increment payload pointer
template <class T>
static void popPayload2CtxNum(duk_context* ctx, const void* &payload, const char* prop) {
    T num;
    popPayloadNum(payload, num);
    duk_push_number(ctx, (duk_double_t)num);
    duk_put_prop_string(ctx, -2, prop);
}

// pop payload (number array) and put it as prop to object (index: -1), increment payload pointer
template <class T>
static void popPayload2CtxNumArray(duk_context* ctx, const void* &payload, const char* prop, size_t len) {
    duk_push_array(ctx);

    for (int i=0; i<len; i++) {
        T num;
        popPayloadNum(payload, num);
        duk_push_number(ctx, (duk_double_t)num);
        duk_put_prop_index(ctx, -2, i);
    }

    duk_put_prop_string(ctx, -2, prop);
}



////////////////////////////////////////////////////////
//////////////////////// send functions ////////////////
////////////////////////////////////////////////////////
static int getPropPos(duk_context* ctx, UdpCmdPacket* cmd) {        // return prop length, or error with -1
    duk_get_prop_string(ctx, -1, "pose");
    // ... obj pose
    if (!duk_is_array(ctx, -1)) {
        duk_pop(ctx);
        return -1;
    }
    size_t n = duk_get_length(ctx, -1);

    // set motor positions
    for(int i=0; i<n; i++){
        duk_get_prop_index(ctx, -1, i);
        cmd->SetMotorPos(duk_get_int(ctx, -1), i);
        duk_pop(ctx);
    }

    duk_pop(ctx);

    return n;
}
static int getPropVel(duk_context* ctx, UdpCmdPacket* cmd) {        // return prop length, or error with -1
    duk_get_prop_string(ctx, -1, "velocity");
    // ... obj velocity
    if (!duk_is_array(ctx, -1)) {
        duk_pop(ctx);
        return -1;
    }
    size_t n = duk_get_length(ctx, -1);

    // set motor positions
    for(int i=0; i<n; i++){
        duk_get_prop_index(ctx, -1, i);
        cmd->SetMotorVel(duk_get_int(ctx, -1), i);
        duk_pop(ctx);
    }

    duk_pop(ctx);

    return n;
}
static int getPropTorMin(duk_context* ctx, UdpCmdPacket* cmd) {        // return prop length, or error with -1
    duk_get_prop_string(ctx, -1, "params1");
    if (!duk_is_array(ctx, -1)) {
        duk_pop(ctx);
        return -1;
    }
    size_t n = duk_get_length(ctx, -1);

    // set motor positions
    for(int i=0; i<n; i++){
        duk_get_prop_index(ctx, -1, i);
        cmd->SetTorqueMin(duk_get_int(ctx, -1), i);
        duk_pop(ctx);
    }

    duk_pop(ctx);

    return n;
}
static int getPropTorMax(duk_context* ctx, UdpCmdPacket* cmd) {        // return prop length, or error with -1
    duk_get_prop_string(ctx, -1, "params2");
    if (!duk_is_array(ctx, -1)) {
        duk_pop(ctx);
        return -1;
    }
    size_t n = duk_get_length(ctx, -1);

    // set motor positions
    for(int i=0; i<n; i++){
        duk_get_prop_index(ctx, -1, i);
        cmd->SetTorqueMax(duk_get_int(ctx, -1), i);
        duk_pop(ctx);
    }

    duk_pop(ctx);

    return n;
}
static int getPropConK(duk_context* ctx, UdpCmdPacket* cmd) {        // return prop length, or error with -1
    duk_get_prop_string(ctx, -1, "params1");
    if (!duk_is_array(ctx, -1)) {
        duk_pop(ctx);
        return -1;
    }
    size_t n = duk_get_length(ctx, -1);

    // set motor positions
    for(int i=0; i<n; i++){
        duk_get_prop_index(ctx, -1, i);
        cmd->SetControlK(duk_get_int(ctx, -1), i);
        duk_pop(ctx);
    }

    duk_pop(ctx);

    return n;
}
static int getPropConB(duk_context* ctx, UdpCmdPacket* cmd) {        // return prop length, or error with -1
    duk_get_prop_string(ctx, -1, "params2");
    if (!duk_is_array(ctx, -1)) {
        duk_pop(ctx);
        return -1;
    }
    size_t n = duk_get_length(ctx, -1);

    // set motor positions
    for(int i=0; i<n; i++){
        duk_get_prop_index(ctx, -1, i);
        cmd->SetControlB(duk_get_int(ctx, -1), i);
        duk_pop(ctx);
    }

    duk_pop(ctx);

    return n;
}
static int getPropConA(duk_context* ctx, UdpCmdPacket* cmd) {        // return prop length, or error with -1
    duk_get_prop_string(ctx, -1, "params1");
    if (!duk_is_array(ctx, -1)) {
        duk_pop(ctx);
        return -1;
    }
    size_t n = duk_get_length(ctx, -1);

    // set motor positions
    for(int i=0; i<n; i++){
        duk_get_prop_index(ctx, -1, i);
        cmd->SetControlA(duk_get_int(ctx, -1), i);
        duk_pop(ctx);
    }

    duk_pop(ctx);

    return n;
}

// function requireBoardInfo();
static duk_ret_t requireBoardInfo(duk_context* ctx) {
    //  Prepare command
	UdpCmdPacket* cmd = udpCom.PrepareCommand(CI_BOARD_INFO, CS_DUKTAPE);
    if (!cmd) return DUK_RET_ERROR;

    #ifdef PRINT_DUKTAPE_PACKET
    // print packet
    printDTPacket(cmd->bytes+2, cmd->length);
    #endif

    //  send the packet
	udpCom.WriteCommand();

    return 0;
}

// function requireSensorInfo();
static duk_ret_t requireSensorInfo(duk_context* ctx) {
    //  Prepare command
	UdpCmdPacket* cmd = udpCom.PrepareCommand(CI_SENSOR, CS_DUKTAPE);
    if (!cmd) return DUK_RET_ERROR;

    #ifdef PRINT_DUKTAPE_PACKET
    // print packet
    printDTPacket(cmd->bytes+2, cmd->length);
    #endif

    //  send the packet
	udpCom.WriteCommand();

    return 0;
}

static void setMotorDirect() {
    //  Prepare command
	UdpCmdPacket* cmd = udpCom.PrepareCommand(CI_DIRECT, CS_DUKTAPE);
    if (!cmd) {
        ESP_LOGE(tag, "setMotorDirect: Failed to prepare command");
        return;
    };

    jsRobotState.write_lock();
    for (int i=0; i<allBoards.GetNTotalMotor(); i++) {
        cmd->SetMotorPos((int16_t)jsRobotState.motor[i].pose, i);
        cmd->SetMotorVel(jsRobotState.motor[i].velocity, i);
    }
    jsRobotState.write_unlock();

    #ifdef PRINT_DUKTAPE_PACKET
    // print packet
    printDTPacket(cmd->bytes+2, cmd->length);
    #endif

    //  send the packet
	udpCom.WriteCommand();
}

static void setMotorParam(enum SetParamType type) {
    //  Prepare command
	UdpCmdPacket* cmd = udpCom.PrepareCommand(CI_SET_PARAM, CS_DUKTAPE);
    if (!cmd) {
        ESP_LOGE(tag, "setMotorParam: Failed to prepare command");
        return;
    };
    switch (type)
    {
        case PT_PD: {
            for (int i=0; i<allBoards.GetNTotalMotor(); i++) {
                jsRobotState.write_lock();
                cmd->SetControlK(jsRobotState.motor[i].controlK, i);
                cmd->SetControlB(jsRobotState.motor[i].controlB, i);
                jsRobotState.write_unlock();
            }
            break;
        }
        case PT_CURRENT: {
            for (int i=0; i<allBoards.GetNTotalMotor(); i++) {
                jsRobotState.write_lock();
                cmd->SetControlA(jsRobotState.motor[i].controlA, i);
                jsRobotState.write_unlock();
            }
            break;
        }
        case PT_TORQUE_LIMIT: {
            for (int i=0; i<allBoards.GetNTotalMotor(); i++) {
                jsRobotState.write_lock();
                cmd->SetTorqueMin(jsRobotState.motor[i].torqueMin, i);
                cmd->SetTorqueMax(jsRobotState.motor[i].torqueMax, i);
                jsRobotState.write_unlock();
            }
            break;
        }
        default:
            ESP_LOGE(tag, "setMotorParam: Unhandled type: %i", type);
            return;
    }

    #ifdef PRINT_DUKTAPE_PACKET
    // print packet
    printDTPacket(cmd->bytes+2, cmd->length);
    #endif

    //  send the packet
	udpCom.WriteCommand();
}

// function updateLocalMotorState(inst: IMotorInstruction): void
/* interface IMotorInstruction {
        [key: string]: number;
        motorId: number;
        pose?: number;
        velocity?: number;
        lengthMin?: number;
        lengthMax?: number;
        controlK?: number;
        controlB?: number;
        controlA?: number;
        torqueMin?: number;
        torqueMax?: number;
    } */
static duk_ret_t updateLocalMotorState(duk_context* ctx) {
    // .. inst

    duk_get_prop_string(ctx, -1, "motorId");
    uint16_t motorId = duk_get_int(ctx, -1);
    if (motorId >= allBoards.GetNTotalMotor()) return 0;
    duk_pop(ctx);

    jsRobotState.write_lock();

    // lengthMin
    duk_bool_t haveProp = duk_get_prop_string(ctx, -1, "lengthMin");
    if (haveProp) {
        int32_t lengthMin = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].lengthMin = lengthMin;
    }
    duk_pop(ctx);

    // lengthMax
    haveProp = duk_get_prop_string(ctx, -1, "lengthMax");
    if (haveProp) {
        int32_t lengthMax = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].lengthMax = lengthMax;
    }
    duk_pop(ctx);

    // pose
    haveProp = duk_get_prop_string(ctx, -1, "pose");
    if (haveProp) {
        int32_t pose = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].pose = saturateCast(pose, jsRobotState.motor[motorId].lengthMin, jsRobotState.motor[motorId].lengthMax);
    }
    duk_pop(ctx);

    // velocity
    haveProp = duk_get_prop_string(ctx, -1, "velocity");
    if (haveProp) {
        int16_t velocity = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].velocity = velocity;
    }
    duk_pop(ctx);

    // controlK
    haveProp = duk_get_prop_string(ctx, -1, "controlK");
    if (haveProp) {
        int16_t controlK = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].controlK = controlK;
    }
    duk_pop(ctx);

    // controlB
    haveProp = duk_get_prop_string(ctx, -1, "controlB");
    if (haveProp) {
        int16_t controlB = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].controlB = controlB;
    }
    duk_pop(ctx);

    // controlA
    haveProp = duk_get_prop_string(ctx, -1, "controlA");
    if (haveProp) {
        int16_t controlA = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].controlA = controlA;
    }
    duk_pop(ctx);

    // torqueMin
    haveProp = duk_get_prop_string(ctx, -1, "torqueMin");
    if (haveProp) {
        int16_t torqueMin = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].torqueMin = torqueMin;
    }
    duk_pop(ctx);

    // torqueMax
    haveProp = duk_get_prop_string(ctx, -1, "torqueMax");
    if (haveProp) {
        int16_t torqueMax = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].torqueMax = torqueMax;
    }
    duk_pop(ctx);

    jsRobotState.write_unlock();

    duk_pop(ctx);
    // ..

    return 0;
}
// function updateRemoteMotorState(inst: IMotorInstruction): void
static duk_ret_t updateRemoteMotorState(duk_context* ctx) {
    // .. inst

    duk_get_prop_string(ctx, -1, "motorId");
    uint16_t motorId = duk_get_int(ctx, -1);
    if (motorId >= allBoards.GetNTotalMotor()) return 0;
    duk_pop(ctx);

    jsRobotState.write_lock();

    // lengthMin
    duk_bool_t haveProp = duk_get_prop_string(ctx, -1, "lengthMin");
    if (haveProp) {
        int32_t lengthMin = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].lengthMin = lengthMin;
    }
    duk_pop(ctx);
    // lengthMax
    haveProp = duk_get_prop_string(ctx, -1, "lengthMax");
    if (haveProp) {
        int32_t lengthMax = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].lengthMax = lengthMax;
    }
    duk_pop(ctx);

    bool flag = false;
    // pose
    haveProp = duk_get_prop_string(ctx, -1, "pose");
    if (haveProp) {
        int32_t pose = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].pose = saturateCast(pose, jsRobotState.motor[motorId].lengthMin, jsRobotState.motor[motorId].lengthMax);
        flag = true;
    }
    duk_pop(ctx);
    // velocity
    haveProp = duk_get_prop_string(ctx, -1, "velocity");
    if (haveProp) {
        int16_t velocity = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].velocity = velocity;
        flag = true;
    }
    duk_pop(ctx);
    // send CI_DIRECT
    if (flag) setMotorDirect();

    flag = false;
    // controlK
    haveProp = duk_get_prop_string(ctx, -1, "controlK");
    if (haveProp) {
        int16_t controlK = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].controlK = controlK;
        flag = true;
    }
    duk_pop(ctx);
    // controlB
    haveProp = duk_get_prop_string(ctx, -1, "controlB");
    if (haveProp) {
        int16_t controlB = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].controlB = controlB;
        flag = true;
    }
    duk_pop(ctx);
    // Send CI_SET_PARAM
    if (flag) setMotorParam(PT_PD);

    flag = false;
    // controlA
    haveProp = duk_get_prop_string(ctx, -1, "controlA");
    if (haveProp) {
        int16_t controlA = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].controlA = controlA;
        flag = true;
    }
    duk_pop(ctx);
    // Send CI_SET_PARAM
    if(flag) setMotorParam(PT_CURRENT);

    flag = false;
    // torqueMin
    haveProp = duk_get_prop_string(ctx, -1, "torqueMin");
    if (haveProp) {
        int16_t torqueMin = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].torqueMin = torqueMin;
        flag = true;
    }
    duk_pop(ctx);
    // torqueMax
    haveProp = duk_get_prop_string(ctx, -1, "torqueMax");
    if (haveProp) {
        int16_t torqueMax = duk_get_int(ctx, -1);
        jsRobotState.motor[motorId].torqueMax = torqueMax;
        flag = true;
    }
    duk_pop(ctx);
    // Send CI_SET_PARAM
    if(flag) setMotorParam(PT_TORQUE_LIMIT);

    jsRobotState.write_unlock();

    duk_pop(ctx);
    // ..

    return 0;
}
// function updateRemoteDirect(): void
static duk_ret_t updateRemoteDirect(duk_context *ctx) {
    setMotorDirect();
    return 0;
}

// function resetSensor(data: {resetSensorFlag: command.ResetSensorFlags});
static duk_ret_t resetSensor(duk_context* ctx) {
    // ... obj
    duk_require_object(ctx, -1);

    //  Prepare command
	UdpCmdPacket* cmd = udpCom.PrepareCommand(CI_RESET_SENSOR, CS_DUKTAPE);
    if (!cmd) return DUK_RET_ERROR;

    bool flag = duk_get_prop_string(ctx, -1, "resetSensorFlag");
    if(!flag) return DUK_RET_REFERENCE_ERROR;
    cmd->SetResetSensorFlags(duk_get_int(ctx, -1));
    duk_pop(ctx);

    #ifdef PRINT_DUKTAPE_PACKET
    // print packet
    printDTPacket(cmd->bytes+2, cmd->length);
    #endif

    //  send the packet
	udpCom.WriteCommand();

    duk_pop(ctx);

    return 0;
}


/* function movementAddKeyframe(data: {
    movementId: number,
    keyframeId: number,
    motorCount: number,
    motorId: number[],
    period: number,
    pose: number[],
    refId: number,
    refMotorId: number,
    timeOffset: number
}); */
static void movementAddKeyframe(duk_context* ctx, UdpCmdPacket* cmd) {
    // fill payload data
    void* payload = shiftPointer(cmd->data, 1);

    // little endian
    pushCtx2PayloadNum<uint8_t>(ctx, payload, "keyframeId");
    pushCtx2PayloadNum<uint8_t>(ctx, payload, "movementId");

    pushCtx2PayloadNum<uint8_t>(ctx, payload, "motorCount");
    pushCtx2PayloadNumArray<uint8_t>(ctx, payload, "motorId");
    pushCtx2PayloadNum<uint16_t>(ctx, payload, "period");
    pushCtx2PayloadNumArray<int32_t>(ctx, payload, "pose");

    // little endian
    pushCtx2PayloadNum<uint8_t>(ctx, payload, "refKeyframeId");
    pushCtx2PayloadNum<uint8_t>(ctx, payload, "refMovementId");

    pushCtx2PayloadNum<uint8_t>(ctx, payload, "refMotorId");
    pushCtx2PayloadNum<short>(ctx, payload, "timeOffset");

    uint8_t flags = 0;
    duk_get_prop_string(ctx, -1, "strictMode");
    bool strictMode = duk_get_boolean(ctx, -1);
    duk_pop(ctx);
    flags |= (strictMode ? 1 : 0) << 7;

    pushPayloadNum(payload, flags);

    #ifdef PRINT_MOVEMENT_ADD_KEYFRAME

        printf("movement commandId: %i \n", *(uint8_t*)shiftPointer(cmd->data, 0));

        // little endian
        printf("keyframeId: %i \n", *(uint8_t*)shiftPointer(cmd->data, 1));
        printf("movementId: %i \n", *(uint8_t*)shiftPointer(cmd->data, 2));

        printf("motorCount: %i \n", *(uint8_t*)shiftPointer(cmd->data, 3));
        printf("motorId: %i \n", *(uint8_t*)shiftPointer(cmd->data, 4));
        printf("period: %i \n", *(uint16_t*)shiftPointer(cmd->data, 5));
        printf("pose: %i \n", *(short*)shiftPointer(cmd->data, 7));

        // little endian
        printf("refKeyframeId: %i \n", *(uint8_t*)shiftPointer(cmd->data, 9));
        printf("refMovementId: %i \n", *(uint8_t*)shiftPointer(cmd->data, 10));

        printf("refMotorId: %i \n", *(uint8_t*)shiftPointer(cmd->data, 11));
        printf("timeOffset: %i \n", *(short*)shiftPointer(cmd->data, 12));

        printf("flags: %o (octal) \n", *(uint8_t*)shiftPointer(cmd->data, 14));
    #endif
}
/* function movementPauseMov(data: {
    movementId: number,
    motorCount: number,
    motorId: number[]
}) */
static void movementPauseMov(duk_context* ctx, UdpCmdPacket* cmd) {
    // fill payload data
    void* payload = shiftPointer(cmd->data, 1);
    pushCtx2PayloadNum<uint8_t>(ctx, payload, "movementId");
    pushCtx2PayloadNum<uint8_t>(ctx, payload, "motorCount");
    pushCtx2PayloadNumArray<uint8_t>(ctx, payload, "motorId");
}
/* function movementResumeMov(data: {
    movementId: number,
    motorCount: number
}) */
static void movementResumeMov(duk_context* ctx, UdpCmdPacket* cmd) {
    // fill payload data
    void* payload = shiftPointer(cmd->data, 1);
    pushCtx2PayloadNum<uint8_t>(ctx, payload, "movementId");
    pushCtx2PayloadNum<uint8_t>(ctx, payload, "motorCount");
}
/* function movementClearMov(data: {
    movementId: number,
    motorCount: number,
    motorId: number[]
}) */
static void movementClearMov(duk_context* ctx, UdpCmdPacket* cmd) {
    // fill payload data
    void* payload = shiftPointer(cmd->data, 1);
    pushCtx2PayloadNum<uint8_t>(ctx, payload, "movementId");
    pushCtx2PayloadNum<uint8_t>(ctx, payload, "motorCount");
    pushCtx2PayloadNumArray<uint8_t>(ctx, payload, "motorId");
}

/* function setMovement(data: {
    movementCommandId: number,
    ...
}) */
void setMovement(duk_context* ctx) {
    // ... obj
    duk_require_object(ctx, -1);

    //bool success =
    duk_get_prop_string(ctx, -1, "movementCommandId");
    uint8_t movementCommandId = duk_get_int(ctx, -1);
    duk_pop(ctx);
    if (movementCommandId > CI_M_COUNT || movementCommandId <= CI_M_NONE) {
        ESP_LOGE(tag, "setMovement: wrong movementCommandId - %i", movementCommandId);
        return;
    };

    //  Prepare command
	UdpCmdPacket* cmd = udpCom.PrepareMovementCommand(CIU_MOVEMENT, (CommandIdMovement)movementCommandId, CS_DUKTAPE);
    if (!cmd) {
        ESP_LOGE(tag, "setMovement: Failed to prepare command");
        return;
    };

    switch (movementCommandId) {
        case CI_M_ADD_KEYFRAME:
            movementAddKeyframe(ctx, cmd);
            printf("=== duktape add keyframe send === \n");
            break;
        case CI_M_PAUSE_MOV:
            movementPauseMov(ctx, cmd);
            break;
        case CI_M_RESUME_MOV:
            movementResumeMov(ctx, cmd);
            break;
        case CI_M_CLEAR_MOV:
            movementClearMov(ctx, cmd);
            break;
        case CI_M_PAUSE_INTERPOLATE:
        case CI_M_RESUME_INTERPOLATE:
        case CI_M_CLEAR_PAUSED:
        case CI_M_CLEAR_ALL:
        case CI_M_QUERY:
            break;

        default:
            ESP_LOGE(tag, "setMovement: wrong movementCommandId - %i", movementCommandId);
            return;
    }

    #ifdef PRINT_DUKTAPE_PACKET
    // print packet
    printDTPacket(cmd->bytes+2, cmd->length);
    #endif

    //  send the packet
	udpCom.WriteCommand();

    // ... obj
}

////////////////////////////////////////////////////////
//////////////////////// register callbacks ////////////
////////////////////////////////////////////////////////
// function registerCallback(name: string, func: function);
static duk_ret_t registerCallback(duk_context* ctx) {
    // ... name func

    const char* name = duk_get_string(ctx, -2);
    std::string name_str(name);
    callback_stash_keys[name_str] = esp32_duktape_stash_array(ctx, 1);
    // ... name

    duk_pop(ctx);

    LOGI("register callback %s", name);

    return 0;
}

// void commandMessageEventHandler() {
//     event_newCallbackRequestedEvent(
//         ESP32_DUKTAPE_CALLBACK_STATIC_TYPE_FUNCTION,

//     )
// }

////////////////////////////////////////////////////////
//////////////////////// receive functions /////////////
////////////////////////////////////////////////////////

static void putPropPos(duk_context* ctx, UdpRetPacket& ret) {
    duk_push_array(ctx);
    for(size_t i=0; i<allBoards.GetNTotalMotor(); i++){
        duk_push_int(ctx, ret.GetMotorPos(i));
        duk_put_prop_index(ctx, -2, i);
    }
    duk_put_prop_string(ctx, -2, "pose");
}
static void putPropVel(duk_context* ctx, UdpRetPacket& ret) {
    duk_push_array(ctx);
    for(size_t i=0; i<allBoards.GetNTotalMotor(); i++){
        duk_push_int(ctx, ret.GetMotorVel(i));
        duk_put_prop_index(ctx, -2, i);
    }
    duk_put_prop_string(ctx, -2, "velocity");
}
static void putPropCur(duk_context* ctx, UdpRetPacket& ret) {
    duk_push_array(ctx);
    for(size_t i=0; i<allBoards.GetNTotalCurrent(); i++){
        duk_push_int(ctx, ret.GetCurrent(i));
        duk_put_prop_index(ctx, -2, i);
    }
    duk_put_prop_string(ctx, -2, "current");
}
static void putPropFor(duk_context* ctx, UdpRetPacket& ret) {
    duk_push_array(ctx);
    for(size_t i=0; i<allBoards.GetNTotalForce(); i++){
        duk_push_int(ctx, ret.GetForce(i));
        duk_put_prop_index(ctx, -2, i);
    }
    duk_put_prop_string(ctx, -2, "force");
}
static void putPropTou(duk_context* ctx, UdpRetPacket& ret) {
    duk_push_array(ctx);
    for(size_t i=0; i<allBoards.GetNTotalTouch(); i++){
        duk_push_int(ctx, ret.GetTouch(i));
        duk_put_prop_index(ctx, -2, i);
    }
    duk_put_prop_string(ctx, -2, "touch");
}
// function onReceiveCIBoardinfo(data: {systemId: number, nTarget: number, nMotor:number, nCurrent: number, nForces:number, nTouch: number, macAddress: ArrayBuffer});
int pushDataCIBoardinfo(duk_context* ctx, void* data) {
    UdpRetPacket& ret = *new UdpRetPacket;
    unsigned short* data_short = (unsigned short*)data;
    size_t len = data_short[1];
    memcpy(ret.bytes, data, 2 + len);

    duk_push_object(ctx);

    duk_push_number(ctx, ret.data[0]);
    duk_put_prop_string(ctx, -2, "systemId");

    duk_push_number(ctx, ret.data[1]);
    duk_put_prop_string(ctx, -2, "nTarget");
    boardInfo.nTarget = ret.data[1];

    duk_push_number(ctx, ret.data[2]);
    duk_put_prop_string(ctx, -2, "nMotor");
    boardInfo.nMotor = ret.data[2];

    duk_push_number(ctx, ret.data[3]);
    duk_put_prop_string(ctx, -2, "nCurrent");
    boardInfo.nCurrent = ret.data[3];

    duk_push_number(ctx, ret.data[4]);
    duk_put_prop_string(ctx, -2, "nForces");
    boardInfo.nForces = ret.data[4];

    duk_push_number(ctx, ret.data[5]);
    duk_put_prop_string(ctx, -2, "nTouch");
    boardInfo.nTouch = ret.data[5];

    void* p = duk_push_fixed_buffer(ctx, 6);
    std::memcpy(p, (void *)&ret.data[6], 6);
    delete &ret;
    duk_push_buffer_object(ctx, -1, 0, 6, DUK_BUFOBJ_ARRAYBUFFER);
    duk_put_prop_string(ctx, -3, "macAddress");
    duk_pop(ctx);

    free(data);

    return 1;
}
void onReceiveCISensor(UdpRetPacket& ret) {
    // trigger JS touch sensor callbacks
    vector<int16_t> newTouch;
    for (int i=0; i<allBoards.GetNTotalTouch(); i++) {
        newTouch.push_back(ret.GetTouch(i));
    }
    if(jsRobotState.touch.size() > 0 && jsRobotState.touch[0] >= 0) onRcvTouchMessage(jsRobotState.touch, newTouch);

    jsRobotState.write_lock();

    // get pos info
    for (int i=0; i<allBoards.GetNTotalMotor(); i++) jsRobotState.motor[i].pose = ret.GetMotorPos(i);
    // get current info
    for (int i=0; i<allBoards.GetNTotalCurrent(); i++) jsRobotState.current[i] = ret.GetCurrent(i);
    // get force info
    for (int i=0; i<allBoards.GetNTotalForce(); i++) jsRobotState.force[i] = ret.GetForce(i);
    // get touch sensor info
    for (int i=0; i<allBoards.GetNTotalTouch(); i++) jsRobotState.touch[i] = ret.GetTouch(i);

    jsRobotState.write_unlock();
}
void onReceiveCIDirect(UdpRetPacket& ret) {
    jsRobotState.write_lock();
    // get pos & velocity info
    for (int i=0; i<allBoards.GetNTotalMotor(); i++) {
        jsRobotState.motor[i].pose = ret.GetMotorPos(i);
        jsRobotState.motor[i].velocity = ret.GetMotorVel(i);
    }
    jsRobotState.write_unlock();
}
// function onReceiveCIInterpolate(data: {pose: number[], targetCountReadMin: number, targetCountReadMax: number, tickMin: number, tickMax: number});
int pushDataCIInterpolate(duk_context* ctx, void* data) {
    UdpRetPacket& ret = *new UdpRetPacket;
    unsigned short* data_short = (unsigned short*)data;
    size_t len = data_short[1];
    memcpy(ret.bytes, data, 2 + len);

    // get parameter
    duk_push_object(ctx);
    // ... obj

    putPropPos(ctx, ret);

    duk_push_int(ctx, ret.GetTargetCountReadMin());
    duk_put_prop_string(ctx, -2, "targetCountReadMin");
    duk_push_int(ctx, ret.GetTargetCountReadMax());
    duk_put_prop_string(ctx, -2, "targetCountReadMax");
    duk_push_int(ctx, ret.GetTickMin());
    duk_put_prop_string(ctx, -2, "tickMin");
    duk_push_int(ctx, ret.GetTickMax());
    duk_put_prop_string(ctx, -2, "tickMax");

    delete &ret;
    free(data);

    return 1;
}
// function onReceiveCISetparam();
int pushDataCISetparam(duk_context* ctx, void* data) {
    return 0;
}
// function onReceiveCIResetsensor();
int pushDataCIResetsensor(duk_context* ctx, void* data) {
    return 0;
}
// function pushDataCIUMovement();
int pushDataCIUMovement(duk_context* ctx, void* data) {
    const void* payload = data;

    // header
    uint16_t len;
    payload = shiftPointer(payload, 2);
    popPayloadNum(payload, len);
    payload = shiftPointer(payload, 2);

    // movement header
    uint8_t movementCommandId;
    popPayloadNum(payload, movementCommandId);

    duk_push_object(ctx);
    duk_push_number(ctx, movementCommandId);
    duk_put_prop_string(ctx, -2, "movementCommandId");

    switch (movementCommandId) {
        case CI_M_ADD_KEYFRAME: {
            // little endian
            popPayload2CtxNum<uint8_t>(ctx, payload, "keyframeId");
            popPayload2CtxNum<uint8_t>(ctx, payload, "movementId");
            popPayload2CtxNum<uint8_t>(ctx, payload, "success");
            popPayload2CtxNum<uint16_t>(ctx, payload, "startTime");
            popPayload2CtxNum<uint16_t>(ctx, payload, "endTime");

            printf("=== duktape add keyframe receive === \n");
        }
        case CI_M_QUERY:
            popPayload2CtxNumArray<uint8_t>(ctx, payload, "nOccupied", boardInfo.nMotor);
            // FIXME not full implemented
            break;
        case CI_M_PAUSE_MOV:
        case CI_M_RESUME_MOV:
        case CI_M_CLEAR_MOV:
        case CI_M_PAUSE_INTERPOLATE:
        case CI_M_RESUME_INTERPOLATE:
        case CI_M_CLEAR_PAUSED:
        case CI_M_CLEAR_ALL:
            break;
    }

    free(data);

    return 1;
}
void onReceiveCIUMovement(void* data) {
    const void* payload = data;

    payload = shiftPointer(payload, 6);
    const void* movementData = payload;

    // update device
    uint8_t movementCommandId;
    popPayloadNum(payload, movementCommandId);
    switch(movementCommandId) {
        case CI_M_ADD_KEYFRAME:
            payload = shiftPointer(payload, 3);
        default:
            break;
    }

    // inform movement sender
    onSrMovementReceiveCIUMovement(movementData);
}

void commandMessageHandler(UdpRetPacket& ret) {
    //hase: now ctx is not needed here. It is assigned at dukEventHandleTask() in duktape_task.c
    //  duk_context* ctx= esp32_duk_context;
    //  duk_idx_t top = duk_get_top(ctx);
    switch (ret.command)
    {
        case CI_BOARD_INFO: {
            std::unordered_map<std::string, uint32_t>::const_iterator iter = callback_stash_keys.find("onReceiveCIBoardinfo");
            if (iter == callback_stash_keys.end()) {
                LOGE("Callback function onReceiveCIBoardinfo is not registered");
                break;
            }

            void* data = (void*)malloc(2 + ret.length);
            memcpy(data, ret.bytes, 2 + ret.length);

            event_newCallbackRequestedEvent(
                ESP32_DUKTAPE_CALLBACK_STATIC_TYPE_FUNCTION,
                iter->second,
                pushDataCIBoardinfo,
                data
            );

            break;
        }
        case CI_SENSOR:{
            onReceiveCISensor(ret);

            break;
        }
        case CI_DIRECT: {
            onReceiveCIDirect(ret);

            break;
        }
        case CI_INTERPOLATE: {
            break;  // NOTE not needed for JS

            std::unordered_map<std::string, uint32_t>::const_iterator iter = callback_stash_keys.find("onReceiveCIInterpolate");
            if (iter == callback_stash_keys.end()) {
                LOGE("Callback function onReceiveCIInterpolate is not registered");
                break;
            }

            void* data = (void*)malloc(2 + ret.length);
            memcpy(data, ret.bytes, 2 + ret.length);

            event_newCallbackRequestedEvent(
                ESP32_DUKTAPE_CALLBACK_STATIC_TYPE_FUNCTION,
                iter->second,
                pushDataCIInterpolate,
                data
            );

            break;
        }
        case CI_SET_PARAM: {
            break;  // NOTE not needed for JS

            std::unordered_map<std::string, uint32_t>::const_iterator iter = callback_stash_keys.find("onReceiveCISetparam");
            if (iter == callback_stash_keys.end()) {
                LOGE("Callback function onReceiveCISetparam is not registered");
                break;
            }

            event_newCallbackRequestedEvent(
                ESP32_DUKTAPE_CALLBACK_STATIC_TYPE_FUNCTION,
                iter->second,
                pushDataCISetparam,
                NULL
            );

            break;
        }
        case CI_RESET_SENSOR: {
            break;  // NOTE not needed for JS

            std::unordered_map<std::string, uint32_t>::const_iterator iter = callback_stash_keys.find("onReceiveCIResetsensor");
            if (iter == callback_stash_keys.end()) {
                LOGE("Callback function onReceiveCIResetsensor is not registered");
                break;
            }

            event_newCallbackRequestedEvent(
                ESP32_DUKTAPE_CALLBACK_STATIC_TYPE_FUNCTION,
                iter->second,
                pushDataCIResetsensor,
                NULL
            );

            break;
        }
        case CIU_MOVEMENT: {
            onReceiveCIUMovement(ret.bytes);

            break;
        }
        default:
            break;
    }

    //  ctx is assigned dukEventHandleTask() in duktape_task.c
    //  duk_pop_n(ctx, duk_get_top(ctx)-top);
}

////////////////////////////////////////////////////////
//////////////////////// register functions ////////////
////////////////////////////////////////////////////////
extern "C" duk_ret_t ModuleSRCommand(duk_context *ctx) {
    ADD_FUNCTION("requireBoardInfo", requireBoardInfo, 0);      // receive 1 parameter as input
    ADD_FUNCTION("requireSensorInfo", requireSensorInfo, 0);
    ADD_FUNCTION("resetSensor", resetSensor, 1);
    ADD_FUNCTION("updateLocalMotorState", updateLocalMotorState, 1);
    ADD_FUNCTION("updateRemoteMotorState", updateRemoteMotorState, 1);
    ADD_FUNCTION("updateRemoteDirect", updateRemoteDirect, 0);

    ADD_FUNCTION("registerCallback", registerCallback, 2);

    return 0;
}
