#pragma once
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/queue.h"
#include <stdio.h>
#ifndef _WIN32
#include "driver/adc.h"
#include "esp_adc_cal.h"
#endif
#include "../../../PIC/boardType.h"
#include "commandWROOM.h"
extern "C"{
#include "../../../PIC/control.h"
}

class MotorDriver{
	xTaskHandle task;
    xQueueHandle queue;
    static void AdcReadTaskStatic(void*);
    void AdcReadTask();
    enum {
        ADC_DMA_LEN = 8
    };
    public:
    bool bControl;
#if defined BOARD3_SEPARATE || defined BOARD4
    enum {
        NTARGET_DIRECT = 12,
        NMOTOR_DIRECT = BoardInfo::NMOTOR,
        NFORCE_DIRECT = 0,
    };
    
#else
    enum {
        NTARGET_DIRECT = 0xFF,
        NMOTOR_DIRECT = 0,
        NFORCE_DIRECT = 0,
    };
#endif
    private:
#if defined BOARD3_SEPARATE 
    const uint8_t pwmPins[NMOTOR_DIRECT*2] = {4, 27, 21, 22, 0, 19};    //M0, M1, M2
#elif defined BOARD4
    const uint8_t pwmPins[NMOTOR_DIRECT*2] = {4, 27, 21, 22, 26, 19};    //M0, M1, M2
#else
    const uint8_t pwmPins[0] = {};
#endif
    const uint8_t adcChs[ADC_DMA_LEN] = {0, 3, 5, 4, 6, 7, 1, 2};   //M0, M1, M2, dummy
    uint8_t adcChsRev[8];   //  must be larger than ch numbers


public:
    int16_t adcRaws[NMOTOR_DIRECT*2];
    void Init();
    void Pwm(int ch, SDEC duty);
    int GetAdcRaw(int ch);
private:
};
extern MotorDriver motorDriver;
