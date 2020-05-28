#include <stdio.h>
#include <string.h>
#include <sstream>
#include <fstream>

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"
#ifndef _WIN32
#include "esp_system.h"
#include "esp_spi_flash.h"
#include "esp_task_wdt.h"
#include "esp_heap_trace.h"
#include "nvs_flash.h"
#include "rom/uart.h"
#endif
#include "esp_log.h"
#include "driver/uart.h"

#include "../WroomEnv.h"
#include "SoftRobot/UdpCom.h"
#include "SoftRobot/AllBoards.h"
#include "SoftRobot/TouchSensing.h"
#include "SoftRobot/MotorDriver.h"
#include "monitor.h"
#include "SoftRobot/UartForBoards.h"
extern "C" {
    #include "duktapeEsp32/include/module_jslib.h"
    #include "duktapeEsp32/include/duktape_task.h"
    #include "duktapeEsp32/include/duktape_utils.h"
}
#include "duktapeEsp32/include/module_srcommand.h"

#include "websocketServer/ws_command.h"
#include "websocketServer/ws_task.h"
#include "websocketServer/ws_fs.h"
#include "espfs.h"
#include "espfsStream.h"
#include "esp32/ulp.h"

#include "../duktapeEsp32/include/logging.h"
LOG_TAG("Monitor");


Monitor Monitor::theMonitor;

const char* MonitorCommandBase::Tag(){
    return "Mon";
}

#ifndef _WIN32
int getchNoWait(){
    uint8_t ch;
    if (uart_read_bytes(UART_NUM_0, &ch, 1, 1) == 1){
        return ch;
    }
    return -1;
}
int getchWait(){
    uint8_t ch;
    while(uart_read_bytes(UART_NUM_0, &ch, 1, 10) != 1){
        allBoards.SaveMotorPos();
    }
    return ch;
}
#else
#include <conio.h>
int getchNoWait() {
	if (_kbhit()) return _getch();
	return -1;
}
int getchWait() {
	while (1) {
		if (_kbhit()) {
			return _getch();
		}
		vTaskDelay(20);
	}
}
#endif

static void getLine(char* buf, int len){
    int cur = 0;
    while(1){
        fflush(stdout);
        int ch = getchWait();
        switch (ch){
        case '\b':
            if (cur > 0) cur--;
            conPrintf("\b \b");
            break;
        case '\r':
            buf[cur] = '\0';
            conPrintf("\n");
            return;
            break;
        default:
            buf[cur] = ch;
            if (cur < len) cur ++;
            conPrintf("%c", (char)ch);
            break;
        }
    }
}

void Monitor::AddCommand(MonitorCommandBase* c){
    commands.push_back(c);
}
void Monitor::ShowList(){
	conPrintf("Top level commands:\n");
	for(int i=0; i<(int)commands.size(); ++i){
        MonitorCommandBase* mc = commands[i];
        conPrintf(" %-35s", mc->Desc());
        if (i%2 == 1 || i == (int)commands.size()-1) conPrintf("\n");
    }
}
void Monitor::Init(){
    uart_driver_install(UART_NUM_0, 1024, 1024, 10, NULL, 0);
}
void Monitor::Run(){
    conPrintf("Monitor starts. Hit [Enter] for help.\n");
    //ShowList();
    while(1){
        uint8_t ch = getchWait();
        int i=0;
        for(; i<(int)commands.size();++i){
            if (commands[i]->Desc()[0] == (char)ch){
                conPrintf("%s\n", commands[i]->Desc());
                commands[i]->Func();
                break;
            }
        }
        if (i==commands.size()){
            ShowList();
        }
    }
}
MonitorCommandBase::MonitorCommandBase(){
    Monitor::theMonitor.AddCommand(this);
}

class MCTaskList: public MonitorCommandBase{
    const char* Desc(){ return "t Show task list"; }
    void Func(){
        char* buf = (char*)malloc(1024*2);
        vTaskList(buf);
        conPrintf("Task\t\tState\tPrio\tStack\tNum\n%s\n", buf);
        free(buf);
    }
} mcTaskList;

class MCEraseNvs: public MonitorCommandBase{
    const char* Desc(){ return "E Erase NVS flash"; }
    void Func(){
        conPrintf("This command erase all NVS flash. Are you sure ? (Y/N)\n");
        while(1){
            int ch = getchNoWait();
            if (ch == 'y' || ch == 'Y'){
#ifdef WROOM
				nvs_flash_erase();
#endif
				conPrintf("erased.\n");
                break;
            }else if(ch > 0){
                conPrintf("canceled.\n");
                break;
            }
        }
    }
} mcEraseNvs;

class MCReset: public MonitorCommandBase{
    const char* Desc(){ return "R Reset by software / Deep sleep / call ulp_run(0)"; }
    void Func(){
        conPrintf("This command deep sleep or reset this program. Are you sure ? (U/D/Y/N)\n");
        while(1){
            int ch = getchNoWait();
            if (ch == 'u' || ch == 'U'){
				conPrintf("call 'ulp_run(0)'.\n");
                ulp_run(0);
            }else if (ch == 'd' || ch == 'D'){
				conPrintf("deep sleep.\n");
                esp_deep_sleep(1000*1000);  //  sleep time in us.
                break;
            }else if (ch == 'y' || ch == 'Y'){
				conPrintf("reset.\n");
#ifdef WROOM
                esp_restart();
#endif
                break;
            }else if(ch > 0){
                conPrintf("canceled.\n");
                break;
            }
        }
    }
} mcReset;


inline void resumeControl(){
    motorDriver.bControl = true;
    resumeInterpolate();
}
inline void pauseControl(){
    pauseInterpolate();
    motorDriver.bControl = false;
}

class MCPwmTest: public MonitorCommandBase{
    const char* Desc(){ return "m Motor PWM Test"; }
    void Func(){
        const float delta = 0.1f;
        const char* up =   "qwertyu";
        const char* down = "asdfghj";
        float duty[MotorDriver::NMOTOR_DIRECT];
        pauseControl();
        for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
            duty[i] = 0.0f;
        }
        conPrintf("[SPACE]:show state, [%s]:forward, [%s]:backword, other:quit\n" ,up, down);
        while(1){
            vTaskDelay(1);
            int ch = getchNoWait();
            if (ch == -1) continue;
            const char* f = NULL;
            if ( (f = strchr(up, ch)) ){
                int channel = f-up;
                if (channel < MotorDriver::NMOTOR_DIRECT){
                    duty[channel] += delta;
                    if (duty[channel] > 1.0f) duty[channel] = 1.0f;
                }
            }else if ( (f = strchr(down, ch)) ) {
                int channel = f-down;
                if (channel < MotorDriver::NMOTOR_DIRECT){
                    duty[channel] -= delta;
                    if (duty[channel] < -1.0f) duty[channel] = -1.0f;
                }
            }else if (ch == ' '){
                conPrintf("PWM duty = ");
                for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
                    conPrintf(" %3.2f/%2.2f", SDEC2DBL(lastRatio[i]), duty[i]);
                }
                conPrintf("   Angle =");
                for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
                    conPrintf(" %4.2f", LDEC2DBL(motorState.pos[i]));
                }
                conPrintf(" heat =");
                for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
                    conPrintf(" %ld", motorHeat[i]);
                }
                conPrintf("\n");
            }else{
                break;
            }
            for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
                setPwmWithLimit(i, (SDEC)(duty[i] * SDEC_ONE));
                //motorDriver.Pwm(i, (SDEC)(duty[i] * SDEC_ONE));
            }
        }
        resumeControl();
    }
} mcPwmTest;

class MCShowADC: public MonitorCommandBase{
    const char* Desc(){ return "a Show ADC and motor angles"; }
    void Func(){
        while(1){
            for(int i=0; i<MotorDriver::NMOTOR_DIRECT*2; ++i){
                int raw = motorDriver.GetAdcRaw(i);
                conPrintf("%5d\t", raw);
            }
            for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
                conPrintf("\t%4.2f", LDEC2DBL(motorState.pos[i]));
            }
            conPrintf("\n");
            vTaskDelay(10);
            if (getchNoWait() >= 0) break;
        }
    }
} mcShowADC;

class MCMotorAngleTest: public MonitorCommandBase{
    const char* Desc(){ return "M Motor angle test"; }
    void Func(){
        int dir = 1;
        pauseControl();
        for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
            motorDriver.Pwm(i, 0);
        }

        conPrintf("Hit Enter key\n\n\n\n");
        if (getchWait() == '\r'){
            for(int d = 0; d<2; ++d){
                for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
                    motorDriver.Pwm(i, dir * (SDEC)(SDEC_ONE * 0.8) );
                }
                for(int t=0; t<150; ++t){
                    for(int i=0; i<MotorDriver::NMOTOR_DIRECT*2; ++i){
                        int raw = motorDriver.GetAdcRaw(i);
                        conPrintf("%5d\t", raw);
                    }
                    for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
                        conPrintf("\t%4.2f", LDEC2DBL(motorState.pos[i]));
                    }
                    conPrintf("\n");
                    vTaskDelay(2);
                    if (getchNoWait() >= 0) break;
                }
                dir *= -1;
            }
            for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
                motorDriver.Pwm(i, 0);
            }
            conPrintf("\n\n\n\nHit any key\n");
            getchWait();
            resumeControl();
        }
    }
} mcMotorAngleTest;

class MCShowMotorPos: public MonitorCommandBase{
    const char* Desc(){ return "P Show motor positions"; }
    void Func(){
        conPrintf("K:\t");
        for(int i=0; i<NMOTOR; ++i){ conPrintf("%d \t", pdParam.k[i]); } conPrintf("\n");
        conPrintf("B:\t");
        for(int i=0; i<NMOTOR; ++i){ conPrintf("%d \t", pdParam.b[i]); } conPrintf("\n");
        conPrintf("A:\t");
        for(int i=0; i<NMOTOR; ++i){ conPrintf("%d \t", pdParam.a[i]); } conPrintf("\n");
        conPrintf("Torque:\t");
        for(int i=0; i<NMOTOR; ++i){ conPrintf("%d~%d \t", torqueLimit.min[i], torqueLimit.max[i]); } conPrintf("\n");

        conPrintf("Off:\t");
        for(int i=0; i<allBoards.motorMap.size(); ++i){
            conPrintf("%d\t", (int) allBoards.motorOffset[i]);
        }
        conPrintf("\n");
        while(1){
            conPrintf("Pos:\t");
            for(int i=0; i<allBoards.motorMap.size(); ++i){
                conPrintf("%d\t", allBoards.motorPos[i]);
            }
            conPrintf("\n");
            vTaskDelay(20);
            if (getchNoWait() >= 0) break;
        }
    }
} mcShowMotorPos;


class MCShowTouch: public MonitorCommandBase{
    const char* Desc(){ return "T Show Touch sensors"; }
    void Func(){
        while(1){
            for(int i=0; i<touchPads.NPad(); ++i){
                conPrintf("%d\t", touchPads.Filtered(i));
            }
            conPrintf("\n");
            vTaskDelay(20);
            if (getchNoWait() >= 0) break;
        }
    }
} mcShowTouch;

#ifndef _WIN32
extern "C" void* duk_alloc_hybrid_udata;
extern "C"{
#include "../duktapeEsp32/duk_alloc_hybrid.h"
}
class MCShowHeap: public MonitorCommandBase{
    const char* Desc(){ return "h Show heap memory"; }
    void Func(){
		conPrintf("Heap free size: %d bytes", esp_get_free_heap_size());
        conPrintf(" a:dump all  c:check  m:max free block  t:trace heap  h:hybrid heap\n");
        switch(getchWait()){
            case 'a':
                heap_caps_dump_all();
                break;
            case 'c':
                heap_caps_check_integrity_all(true);
                conPrintf("Heap structure is checked.\n");
                break;
            case 'm':{
                size_t fs = heap_caps_get_largest_free_block(MALLOC_CAP_8BIT);
                conPrintf("Maximum free heap block size with MALLOC_CAP_8BIT is %d = 0x%x.\n", fs, fs);
                fs = heap_caps_get_largest_free_block(MALLOC_CAP_32BIT);
                conPrintf("Maximum free heap block size with MALLOC_CAP_32BIT is %d = 0x%x.\n", fs, fs);
                break;
            }
            case 't':
                heap_trace_dump();
                break;
            case 'h':
                duk_alloc_hybrid_dump(duk_alloc_hybrid_udata);
                break;

        }
	}
} mcShowHeap;
#endif

class MCWriteCmd: public MonitorCommandBase{
    const char* Desc(){ return "w Write command to UART"; }
    void Func(){
        conPrintf("Command ID in digit? ");
        char buf[20];
        getLine(buf, sizeof(buf));
        conPrintf("\n");
        int ci = atoi(buf);
        if (CI_NONE < ci && ci < CI_NCOMMAND){
            udpCom.recvs.Lock();
            UdpCmdPacket* recv = &udpCom.recvs.Poke();
            memset(recv->data, 0, sizeof(recv->data));
            recv->command = ci;
            if (recv->command == CI_RESET_SENSOR){
                conPrintf("CI_RESET_SENSOR\n");
                recv->SetResetSensorFlags(RSF_FORCE | RSF_MOTOR);
            }
            recv->length = recv->CommandLen();
            recv->count = udpCom.commandCount + 1;
            udpCom.recvs.Write();
            udpCom.recvs.Unlock();
        }else{
            conPrintf("Cmmand ID %d is invalid.\n", ci);
        }
    }
} mcWriteCmd;

#ifdef _WIN32
class MCShowTask : public MonitorCommandBase {
	const char* Desc() { return "f Show FreeRTOS tasks"; }
	void Func() {
		char buf[1024];
		vTaskList(buf);
		conPrintf("%s", buf);
	}
} mcShowTask;
#endif

extern "C"{
    extern int underflowCount;
}
class MCTargetUnderflow: public MonitorCommandBase{
    const char* Desc(){ return "u Show target underflow"; }
    void Func(){
        while(1){
            int uc = underflowCount;
            underflowCount = 0;
            conPrintf("control.c: target's underflowCount = %d\n", uc);
            if (getchNoWait() >= 0) break;
            vTaskDelay(100);
        }
    }
} mcShowTargetUnderflow;

static uint32_t test_callback = 0;
#if 0
static int test_callback_push_values(duk_context* ctx, void* data) {
    // event
    duk_push_string(ctx, "test");
    return 1;
}
#endif
extern "C" duk_ret_t registerTestCallback(duk_context* ctx) {
    test_callback = esp32_duktape_stash_array(ctx, 1);
    conPrintf("stashed Test callback with key: %i\n", test_callback);
    return 0;
}

class MCJSTest: public MonitorCommandBase{
public:
    const char* Desc(){ return "c JS call test"; }
    void Func(){
		conPrintf("Heap before put event: %d bytes\n", esp_get_free_heap_size());
#if 1
        const char* cmd = "jslib.print('"
            "Very long message aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n');";
        event_newCommandLineEvent((char*)cmd, strlen(cmd), 0);
#else
        event_newCallbackRequestedEvent(
            ESP32_DUKTAPE_CALLBACK_STATIC_TYPE_FUNCTION,
            test_callback,
            test_callback_push_values,
            NULL
        );
#endif
		conPrintf("Heap after put event: %d bytes\n", esp_get_free_heap_size());
    }
} mcJSTest;

extern bool offline_mode;
class MCJSRestart: public MonitorCommandBase{
public:
    MCJSRestart() { bFirst = true; }
    const char* Desc(){ return "J Restart java script"; }
    bool bFirst;
    void Func(){
        offline_mode = false;
        wsDeleteJsfileTask();
        //heap_trace_dump();
        if (bFirst){
            //heap_trace_start(HEAP_TRACE_LEAKS);
            bFirst = false;
        }
        ESP_LOGI(Tag(), "before wsCreateJsfileTask heap size: %d", esp_get_free_heap_size());
        offline_mode = true;
        wsCreateJsfileTask();
        ESP_LOGI(Tag(), "After wsCreateJsfileTask heap size: %d", esp_get_free_heap_size());
    }
} mcJSRestart;
class MCJSDelete: public MonitorCommandBase{
public:
    MCJSDelete() { }
    const char* Desc(){ return "D delete java script"; }
    void Func(){
        offline_mode = false;
        wsDeleteJsfileTask();
    }
} mcJSDelete;

class MCJSStack: public MonitorCommandBase{
public:
    const char* Desc(){ return "j dump java script stack"; }
    void Func(){
        lock_heap();
        for(int i=0; i<NJSTHREADS; ++i){
            duk_context* ctx = jsThreads[i].ctx;
            duk_push_global_object(ctx);
            duk_push_context_dump(ctx);
            printf("%d: %s\n", i, duk_to_string(ctx, -1));
            duk_pop(ctx);
            duk_pop(ctx);
        }
        unlock_heap();
    }
} mcJSStack;

inline char toLower(char c){
    if ('A' <= c && c <= 'Z'){
        c += 'a'-'A';
    }
    return c;
}
inline char toUpper(char c){
    if ('a' <= c && c <= 'z'){
        c += 'A'-'a';
    }
    return c;
}

class MCLogLevel: public MonitorCommandBase{
public:
    const char* Desc(){ return "l log level set"; }
    void Func(){
        const char levels [] = "NEWIDV";
		conPrintf("Type 'tag level[ENTER]' loglevel='NEWIDV'\n");
		while(1){
            char buf[40];
            getLine(buf, sizeof(buf));
            char* tag = buf;
            while(*tag==' ') tag++;
            char* level = tag;
            while(*level != '\0' && *level != ' ') level++;
            if (*level){
                *level = '\0';
                level++;
            }
            while(*level == ' ') level++;
            for(int l=0; l<sizeof(levels); ++l){
                if (toUpper(level[0]) == levels[l]){
                    if (strlen(tag)){
                        esp_log_level_set(tag, (esp_log_level_t)l);
                        conPrintf("esp_log_level_set(%s, %d)", tag, l);
                        goto next;
                    }
                }
            }
            if (strlen(buf) == 0){
                break;
            }
            next:;
        }
		conPrintf("Back to 'main menu' from 'log level set'\n");
    }
} mcLogLevel;

class MCEspFs: public MonitorCommandBase{
public:
    const char* Desc(){ return "e test Espfs"; }
    void Func(){
        conPrintf("Espfs test w:write, r:read\n");
        switch (getchWait()){
            case 'w':
            case 'W':{
                std::ostream* m_ostream;
                const std::string content = "hello world !!. 0ABCDEFGHIJKLMNOPQRSTUVWXYZ"
                "1ABCDEFGHIJKLMNOPQRSTUVWXYZ" "2ABCDEFGHIJKLMNOPQRSTUVWXYZ"
                "3ABCDEFGHIJKLMNOPQRSTUVWXYZ" "4ABCDEFGHIJKLMNOPQRSTUVWXYZ"
                "5ABCDEFGHIJKLMNOPQRSTUVWXYZ" "6ABCDEFGHIJKLMNOPQRSTUVWXYZ"
                "7ABCDEFGHIJKLMNOPQRSTUVWXYZ" "8ABCDEFGHIJKLMNOPQRSTUVWXYZ"
                "9ABCDEFGHIJKLMNOPQRSTUVWXYZ" "AABCDEFGHIJKLMNOPQRSTUVWXYZ"
                "BABCDEFGHIJKLMNOPQRSTUVWXYZ" "CABCDEFGHIJKLMNOPQRSTUVWXYZ";
                m_ostream = espFsAddFileByStream("/main/main.js", content.length());
                m_ostream->write(content.c_str(), content.length());
                m_ostream->flush();
                delete m_ostream;
                LOGI("finish writing file");
            }
            break;
            case 'r':
            case 'R':{
                EspFsFile* fh = espFsOpen("/main/main.js");
                if (fh){
                    const char* buf=NULL;
                    size_t len=0;
                    espFsAccess(fh, (void **)&buf, &len);
                    LOGI("file, len=%d, at 0x%x buf=%s", len, (int)buf, buf);
                }
                break;
            }
        }
    }
} mcEspFs;

#include "../../PIC/control.h"
#include "./softRobot/Movement.h"
#include "./softRobot/CommandWROOM.h"

struct MonitorMovementKeyframe {
	uint16_t id;				// 8-bit movement id + 8-bit keyframe id
	uint8_t motorCount;			// count of motors used in the movement
	uint8_t motorId[1];	// the motorIds used
	uint16_t period;			// note that the sum of period in list could not larger than UINT16_MAX (or the sorting might fail)
	int32_t pose[1];			// the poses correspond with motorIds

	uint16_t refId;             // 0 if no ref (movement id should start from 1)
	uint8_t refMotorId;
	short timeOffset;
}__attribute__((packed));

#define struct MonitorMovementKeyframe MonitorMovementKeyframe;
static void print_timer_counter(uint64_t counter_value)
{
    printf("Counter: 0x%08x%08x\n", (uint32_t) (counter_value >> 32), (uint32_t) (counter_value));
    printf("Time   : %.8f ms\n", (double) counter_value / MOVEMENT_MANAGER_TIMER_SCALE);
}
static int32_t currentPos[3] = {0, 0, 0};
static void changeCurrentPos(uint8_t myMotorId, short offset, bool newMovement = false, bool playAfter = false) {
    size_t len = 4 + 1 + sizeof(MonitorMovementKeyframe);
    unsigned char motorId[1] = {myMotorId};
    currentPos[myMotorId] += offset;
    int32_t pose[1] = {currentPos[myMotorId]};

    void* payload = (void*)malloc(len);
    *((unsigned short*)payload) = len;
    *((unsigned short*)payload+1) = CIU_MOVEMENT;
    *((unsigned char*)payload+4) = CI_M_ADD_KEYFRAME;
    MonitorMovementKeyframe* keyframe = (MonitorMovementKeyframe*)((unsigned char*)payload+5);
    static uint8_t keyframeId = 0;
    keyframe->id = 0x0100 + (newMovement ? 0x0100 : 0) + keyframeId;
    keyframeId += 1;
    keyframeId = keyframeId % 20;
    keyframe->motorCount = 1;
    memcpy(keyframe->motorId, motorId, keyframe->motorCount);
    keyframe->period = 2000;
    memcpy(keyframe->pose, pose, keyframe->motorCount*4);
    keyframe->refId = 0x0000 + (playAfter ? 0x0001 : 0);
    keyframe->refMotorId = 0;
    keyframe->timeOffset = 0;

    UdpCom_ReceiveCommand(payload, len, CS_DUKTAPE);
}
class MCMovement: public MonitorCommandBase{
public:
    const char* Desc(){ return "p test movement"; }
    void Func(){
        conPrintf("MCMovement: \n");
        conPrintf(" p: get motor pose           m: print all motor keyframes        i: print interpolate parameters \n");
        conPrintf(" z: pause all movements      x: resume all movements             c: clear interpolate buffer \n");
        conPrintf(" v: pause movement 1         b: resume movement 1                n: clear movement 1 \n");
        conPrintf(" o: print movement info      t: print timer \n");
        switch (getchWait()){
            // print current pos
            case 'p': {
                printf("target pose: %ld, %ld, %ld \n", motorTarget.pos[0] >> 6, motorTarget.pos[1] >> 6, motorTarget.pos[2] >> 6);
                break;
            }
            // print keyframes
            case 'm': {
                printAllMotorKeyframes();
                break;
            }
            // print interpolate params
            case 'i': {
                printInterpolateParams();
                break;
            }
            // z pause all movements
            case 'z': {
                pauseInterpolate();
                break;
            }
            // x resume all movements
            case 'x': {
                resumeInterpolate();
                break;
            }
            // c clear all buffer
            case 'c': {
                clearInterpolateBuffer();
                break;
            }
            // v pause movement 1
            case 'v': {
                vector<uint8_t> motorId;
                motorId.push_back(0);
                pauseMovement(1, 1, motorId);
                break;
            }
            // b resume movement 1
            case 'b': {
                vector<uint8_t> motorId;
                motorId.push_back(0);
                resumeMovement(1, 1);
                break;
            }
            // n clear movement 1
            case 'n': {
                vector<uint8_t> motorId;
                motorId.push_back(0);
                clearMovement(1, 1, motorId);
                break;
            }
            // REVIEW add after
            case '+': {
                changeCurrentPos(1, 1000, true, true);
                break;
            }
            case '-': {
                changeCurrentPos(1, -1000, true, true);
                break;
            }
            // add motor 0
            case '3': {
                changeCurrentPos(0, 2000);

                break;
            }
            // minus motor 0
            case '1': {
                changeCurrentPos(0, -2000);

                break;
            }
            // add motor 1
            case '6': {
                changeCurrentPos(1, 2000);

                break;
            }
            // minus motor 1
            case '4': {
                changeCurrentPos(1, -2000);

                break;
            }
            // add motor 2
            case '9': {
                changeCurrentPos(2, 2000);

                break;
            }
            // minus motor 2
            case '7': {
                changeCurrentPos(2, -2000);

                break;
            }
            // print movement info
            case 'o': {
                printMovementInfo();
                break;
            }
            // print timer
            case 't': {
                uint64_t value;
                timer_get_counter_value(TIMER_GROUP_0, TIMER_0, &value);
                print_timer_counter(value);
                break;
            }
            // query interpolate state
            case 'q': {
                movementQueryInterpolateState();
                break;
            }
        }
    }
} mcMovement;

extern "C" {
    #include "websocketServer/CoreDumpReader.h"
}
#include "websocketServer/OTA.h"
class MCTest: public MonitorCommandBase {
    public:
    const char * Desc(){ return "H error handle";}
    void Func() {
        conPrintf(" a: produce error            s: read error                       d: mark error\n");
        conPrintf(" o: start ota                c: connect wifi \n");
        switch (getchWait()) {
            case 'a':
                assert(1 == 0);
                break;
            case 's':
                if (haveUnmarkedCoreDump()) {
                    printf("Have unmarked core dump \n");
                }
                else {
                    printf("Have no unmarked core dump \n");
                }
                break;
            case 'd':
                markCoreDump();
                break;
            case 'o':
                updateFirmware();
                break;
            case 'c':
                // SRWiFi::wifi.connectAP("haselab_pi_G", "2human2human2");
                break;
            default:
                printf("Unsupported command \n");
                break;
        }
    }
} mcTest;

