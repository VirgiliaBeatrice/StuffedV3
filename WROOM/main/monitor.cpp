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
#include "UdpCom.h"
#include "AllBoards.h"
#include "TouchSensing.h"
#include "MotorDriver.h"
#include "monitor.h"
#include "UartForBoards.h"
extern "C" {
#include "module_jslib.h"
#include "duktape_jsfile.h"
}
#include "../SoftRobot/UdpCom.h"
#include "module_srcommand.h"

#include "ws_command.h"
#include "ws_task.h"
#include "ws_fs.h"


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
    uart_read_bytes(UART_NUM_0, &ch, 1, portMAX_DELAY);
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

void Monitor::AddCommand(MonitorCommandBase* c){
    commands.push_back(c);
}
void Monitor::ShowList(){
	conPrintf("Top level commands:\n");
	for(int i=0; i<(int)commands.size(); ++i){
        MonitorCommandBase* mc = commands[i];
        conPrintf(" %s\n", mc->Desc());
    }
}
void Monitor::Init(){
    uart_driver_install(UART_NUM_0, 1024, 1024, 10, NULL, 0);
}
void Monitor::Run(){
    conPrintf("Monitor start.\n");
    ShowList();
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
        char buf[1024*2];
        vTaskList(buf);
        conPrintf("Task\t\tState\tPrio\tStack\tNum\n%s\n", buf);
    }
} mcTaskList;
 
class MCEraseNvs: public MonitorCommandBase{
    const char* Desc(){ return "E Erase NVS flash"; }
    void Func(){
        conPrintf("This command erase all NVS flash. Are you sure ? (Y/N)\n");
        while(1){
            int ch = getchNoWait();
            if (ch == 'y' || ch == 'Y'){
#ifndef _WIN32
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


class MCPwmTest: public MonitorCommandBase{
    const char* Desc(){ return "m Motor PWM Test"; }
    void Func(){
        const float delta = 0.1f;
        const char* up =   "qwertyu";
        const char* down = "asdfghj";
        float duty[MotorDriver::NMOTOR_DIRECT];
        motorDriver.bControl = false;
        for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
            duty[i] = 0.0f;
        }
        conPrintf("[ENTER/SPACE]:show state, [%s]:forward, [%s]:backword, other:quit\n" ,up, down);
        while(1){
            vTaskDelay(10);
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
            }else if (ch == '\r' || ch == ' '){
                conPrintf("PWM duty = ");
                for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
                    conPrintf("  %8.1f", duty[i]);
                }
                conPrintf("   Angle =");
                for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
                    conPrintf("  %8.2f", LDEC2DBL(motorState.pos[i]));
                }
                conPrintf("\n");
            }else{
                break;
            }
            for(int i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
                motorDriver.Pwm(i, duty[i]);
            }
        }
        motorDriver.bControl = true;
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
            vTaskDelay(100);
            if (getchNoWait() >= 0) break;
        }
    }
} mcShowADC;

class MCShowTouch: public MonitorCommandBase{
    const char* Desc(){ return "t Show Touch sensors"; }
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
        UdpCmdPacket* recv = &udpCom.recvs.Poke();
        recv->command = CI_DIRECT;
        recv->length = recv->CommandLen();
        recv->count = udpCom.commandCount + 1;
        udpCom.recvs.Write();
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
            conPrintf("Underflow count = %d\n", uc);
            vTaskDelay(100);
            if (getchNoWait() >= 0) break;
        }
    }
} mcShowTargetUnderflow;
class MCJSRestart: public MonitorCommandBase{
    const char* Desc(){ return "j Restart java script"; }
    void Func(){
        wsDeleteJsfileTask();
/*        combineMainFiles();
        std::ifstream m_ifstream("/spiffs/main/runtime.js");
        std::string str((std::istreambuf_iterator<char>(m_ifstream)),
        std::istreambuf_iterator<char>());
        ESP_LOGI(Tag(), "Start runtime file: %s", str.c_str());
        m_ifstream.close();
*/
        ESP_LOGI(Tag(), "before wsCreateJsfileTask heap size: %d", esp_get_free_heap_size());

        heap_trace_start(HEAP_TRACE_LEAKS);

        wsCreateJsfileTask();
        ESP_LOGI(Tag(), "Create JS Task and wait 10 sec.");
        vTaskDelay(1000);
        ESP_LOGI(Tag(), "Delete JS Task.");
        wsDeleteJsfileTask();
        ESP_LOGI(Tag(), "Deleted. dump heap:");
        heap_trace_dump();

        wsCreateJsfileTask();
    }
} mcJSRestart;

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
    struct Tag{
        const char* tag;
        const char* msg;
        int logLevel;
        Tag(const char* t, const char* m):tag(t), msg(m), logLevel(ESP_LOG_INFO){};
    };
    std::vector<Tag> tags;
	std::vector<char> keys;
public:
    MCLogLevel(){
        tags.push_back(Tag(AllBoards::Tag(), "AllBoards"));
        tags.push_back(Tag(UartForBoards::Tag(), "UartForBoards"));
        tags.push_back(Tag(UdpCom::Tag(), "UdpCom"));
        tags.push_back(Tag(BoardBase::Tag(), "Board"));
        tags.push_back(Tag(BoardFactoryBase::Tag(), "BoardFactory"));
        tags.push_back(Tag("ws_ws", "Web socket"));
        tags.push_back(Tag("ws_task", "Web socket task"));
        tags.push_back(Tag("SRCmd", "module srCommand"));
        tags.push_back(Tag("duktape_jsfile", "duktape_jsfile.c"));
        tags.push_back(Tag("log", "duktape console log"));
        tags.push_back(Tag("modules", "duktape modules"));
        Init();
    }
    void Init(){
        for(int i=0; i<(int)tags.size(); ++i){
            char key = toLower(tags[i].tag[0]);
            for(int k=0; k<(int)keys.size(); ++k){
                if (key == keys[k]){
                    if (toUpper(key) != key){
                        key = toUpper(key);
                        k = 0;
                        continue;
                    }else if (key < '~'){
                        key ++;
                        k = 0;
                        continue;
                    }else{
                        key = '!';
                        k = 0;
                        continue;
                    }
                }
            }
            keys.push_back(key);
        }
        assert(tags.size() == keys.size());
    }
    const char* Desc(){ return "l change log level"; }
    void Func(){
        const char levels [] = "NEWIDV";
		conPrintf("Log level tags:\n");
		for (int i = 0; i<(int)tags.size(); ++i) {
			conPrintf(" %c %s\t= %c (%s)\n", (int)keys[i], tags[i].tag, (int)levels[tags[i].logLevel], tags[i].msg);
		}
		while(1){
            int ch = getchWait();
            int i;
            for(i=0; i<(int)tags.size(); ++i){
                if (keys[i] == ch){
					conPrintf("%c %s (%s) = ? choose from %s\n", (int)keys[i], tags[i].tag, tags[i].msg, levels);
                    ch = getchWait();
                    for(int l=0; l < sizeof(levels)/sizeof(levels[0]) - 1; ++l){
                        if (toUpper(ch) == levels[l]){
                            tags[i].logLevel = l;
#ifndef _WIN32
							if (l > CONFIG_LOG_DEFAULT_LEVEL){
                                conPrintf("Log level %d must be lower than CONFIG_LOG_DEFAULT_LEVEL %d.\n", l, CONFIG_LOG_DEFAULT_LEVEL);
                            }
                            esp_log_level_set(tags[i].tag, (esp_log_level_t)tags[i].logLevel);
#endif
							conPrintf("%c %s\t= %c (%s)\n", (int)keys[i], tags[i].tag, (int)levels[tags[i].logLevel], tags[i].msg);
						}
                    }
                    break;
                }
            }
			if (i == tags.size()) {
				conPrintf("Back to 'main menu' from 'log level'\n");
				break;
			}
        }
    }
} mcLogLevel;
