#include <stdio.h>
#include <string.h>
#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/event_groups.h>
#ifndef _WIN32
#include <logging.h>
#include <esp_system.h>
#include <esp_spi_flash.h>
#include <esp_task_wdt.h>
#include <nvs_flash.h>
#include <rom/uart.h>
#endif

#include "UdpCom.h"
#include "AllBoards.h"
#include "TouchSensing.h"
#include "MotorDriver.h"
#ifndef USE_DUKTAPE
#include "../wifiMan/wifiMan.h"
#endif
LOG_TAG("sr_main");

extern "C" void softRobot_main()    //  called from app_main in main.cpp 
{        
    LOGD("Soft Robot Starts. Init local IO.");
    //  Init MCPWM and ADC
    motorDriver.Init();
    //for(int i=0; i<3; ++i){ LOGI("MotorDrive.Init() pdParam: k=%d b=%d", pdParam.k[i], pdParam.b[i]); }

#if 1   //  touchPads can not work with JTAG debugger
    touchPads.Init();
    LOGD("nPads %d", touchPads.NPad());
#endif
#if 1
    //  Search sub boards.
    LOGI("Soft Robot Starts. Search sub boards.");
    allBoards.Init();
    //for(int i=0; i<3; ++i){ LOGI("allBoards.Init() pdParam: k=%d b=%d", pdParam.k[i], pdParam.b[i]); }
    LOGI("%d motors, %d current sensors, %d force sensors and %d touch pads found.", 
        allBoards.GetNTotalMotor(), allBoards.GetNTotalCurrent(), allBoards.GetNTotalForce(), allBoards.GetNTotalTouch());
    udpCom.Init();    //  init command processing for udp.
    //for(int i=0; i<3; ++i){ LOGI("udpCom.Init() pdParam: k=%d b=%d", pdParam.k[i], pdParam.b[i]); }
#endif
}
