#include "freertos/FreeRTOS.h"
#include "freertos/event_groups.h"
#include "esp_event_loop.h"
#include <logging.h>
#ifndef _WIN32
#include "CPPNVS.h"
#include "esp_adc_cal.h"
extern "C"{
#include "soc/syscon_struct.h"
}
#include "soc/syscon_reg.h"
#include <soc/mcpwm_struct.h>
#include <soc/mcpwm_reg.h>
#include <driver/mcpwm.h>
#include "driver/i2s.h"
#include "driver/adc.h"
#endif 

#include <string.h>
#include "MotorDriver.h"
#include "TouchSensing.h"
#include "AllBoards.h"
extern "C" {
#include "../../../PIC/control.h"
}

#ifdef _WIN32
#include <math.h>
float pwm[NMOTOR];
float theta[NMOTOR];
#endif
LOG_TAG("MotorDriver");

MotorDriver motorDriver;


void MotorDriver::AdcReadTaskStatic(void* arg){
    motorDriver.AdcReadTask();
}

#define ADCI2SNUM   I2S_NUM_0
void MotorDriver::AdcReadTask(){
#if defined BOARD4  
    const gpio_num_t GPIO_LED = GPIO_NUM_0; // for BOARD4
#else
    const gpio_num_t GPIO_LED = GPIO_NUM_26;  // for older BOARD
#endif
    const size_t bufLen = ADC_DMA_LEN * 2;
#ifndef _WIN32
    gpio_reset_pin(GPIO_LED);
    gpio_set_direction(GPIO_LED, GPIO_MODE_OUTPUT);
    static int count=0;
	while(1) {
        static uint16_t buf[ADC_DMA_LEN];
        static system_event_t evt;
        count ++;
        if (xQueueReceive(queue, &evt, portMAX_DELAY) == pdPASS) {
            if (evt.event_id==2) {
                //  read ADC
                static size_t readBytes = bufLen;
                i2s_read(ADCI2SNUM, buf, bufLen, &readBytes, portMAX_DELAY);
                for (int i=0; i<readBytes/2; ++i){
                    int ch = buf[i] >> 11;
                    int value = buf[i] &0x7FF;
                    int pos = adcChsRev[ch];
#define FILTER_TIME 16
                    if (pos < sizeof(adcRaws) / sizeof(adcRaws[0])){
                        adcRaws[pos] = adcRaws[pos]*(FILTER_TIME-1)/FILTER_TIME + value;
                    }
                }
                if (count % 4 == 0){    //  ADC read at 12kHz. control at 3kHz
                    if (bControl){
                        gpio_set_level(GPIO_LED, 1);
                        controlLoop();
                        gpio_set_level(GPIO_LED, 0);
                    }else{
                        updateMotorState();
                    }
                }
            }
        }
    }
#elif defined _WIN32
	while(1) {
		if (bControl) controlLoop();
		vTaskDelay(1);
    }
#else
#error
#endif
}

void MotorDriver::Init(){
#ifndef _WIN32
    //  Set ADC input as GPIO output to charge cap
    const gpio_num_t GPIO_ADC[] = {GPIO_NUM_32, GPIO_NUM_33}; 
    for(int i=0; i < sizeof(GPIO_ADC) / sizeof(GPIO_ADC[0]); ++i){
        gpio_reset_pin(GPIO_ADC[i]);
        gpio_set_direction(GPIO_ADC[i], GPIO_MODE_OUTPUT);
        gpio_set_level(GPIO_ADC[i], 1);
    }
	//  PWM Init
    mcpwm_gpio_init(MCPWM_UNIT_0, MCPWM0A, pwmPins[0]);
    mcpwm_gpio_init(MCPWM_UNIT_0, MCPWM0B, pwmPins[1]);
    mcpwm_gpio_init(MCPWM_UNIT_0, MCPWM1A, pwmPins[2]);
    mcpwm_gpio_init(MCPWM_UNIT_0, MCPWM1B, pwmPins[3]);
    mcpwm_gpio_init(MCPWM_UNIT_0, MCPWM2A, pwmPins[4]);
    mcpwm_gpio_init(MCPWM_UNIT_0, MCPWM2B, pwmPins[5]);

    mcpwm_config_t pwm_config;
    pwm_config.frequency = 20000;       //frequency in Hz,
    pwm_config.cmpr_a = 0;              //duty cycle of PWMxA = 0
    pwm_config.cmpr_b = 0;              //duty cycle of PWMxb = 0
    pwm_config.counter_mode = MCPWM_UP_COUNTER;
    pwm_config.duty_mode = MCPWM_DUTY_MODE_0;
    mcpwm_init(MCPWM_UNIT_0, MCPWM_TIMER_0, &pwm_config);    //Configure PWM0A & PWM0B with above settings
    mcpwm_init(MCPWM_UNIT_0, MCPWM_TIMER_1, &pwm_config);    //Configure PWM0A & PWM0B with above settings
    mcpwm_init(MCPWM_UNIT_0, MCPWM_TIMER_2, &pwm_config);    //Configure PWM0A & PWM0B with above settings
#endif
    //  Init control/command and set initial values to motors. 
    controlInit();  //  initialize, call loadMotorParam()
    commandInit();
    bControl = true;
    //  ADC with DMA and I2S mode init. This also start periodic interrupt for control.
    //  i2s settings
    for(int i=0; i < sizeof(adcChsRev) / sizeof(adcChsRev[0]);++i){
        adcChsRev[adcChs[i]] = i;
    }
#ifndef _WIN32
	i2s_config_t i2s_config;
    memset(&i2s_config, 0, sizeof(i2s_config));
    i2s_config.mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_RX | I2S_MODE_ADC_BUILT_IN);
    i2s_config.sample_rate = ADC_DMA_LEN * 3000 * 4;   //  3kHz * 4
    i2s_config.bits_per_sample = i2s_bits_per_sample_t(16);
    i2s_config.channel_format = I2S_CHANNEL_FMT_ONLY_LEFT;
    i2s_config.communication_format = I2S_COMM_FORMAT_I2S_LSB;
    i2s_config.intr_alloc_flags = ESP_INTR_FLAG_LEVEL1;
    i2s_config.dma_buf_count = 2;
    i2s_config.dma_buf_len = ADC_DMA_LEN;
    //  Install and start I2S driver
    i2s_driver_install(ADCI2SNUM, &i2s_config, 1, &queue);
    ////    vTaskDelay(1);  //  not needed
    //  Start ADC task
	xTaskCreate(AdcReadTaskStatic, "ADC", 512+256, this, configMAX_PRIORITIES-1, &task);
    vTaskDelay(1);  //  This delay is needed (hasevr).
    //  Start ADC
    ESP_ERROR_CHECK(i2s_set_adc_mode(ADC_UNIT_1, ADC1_CHANNEL_0));
    ////    vTaskDelay(1);  //  not needed
    ESP_ERROR_CHECK(i2s_adc_enable(ADCI2SNUM));
    ////    vTaskDelay(1);  //  not needed
#if defined BOARD3_SEPARATE || defined BOARD4
    SYSCON.saradc_ctrl.sar1_patt_len = NMOTOR_DIRECT*2-1;   // table length - 1
#else
    SYSCON.saradc_ctrl.sar1_patt_len = 0;
#endif
    SYSCON.saradc_ctrl.data_sar_sel = true;
    LOGD("Init ADC");
    //  Making ADC scanning table pattern (WIDTH=11, DB=11) = 7
    //  ADC_WIDTH_BIT_9 = 0, ADC_WIDTH_BIT_12 = 3;
    //  ADC_ATTEN_DB_0 = 0, ADC_ATTEN_DB_2_5=1, ADC_ATTEN_DB_6=2, ADC_ATTEN_DB_11=3
    uint32_t patterns[ADC_DMA_LEN];
    for(int i=0; i<ADC_DMA_LEN; ++i){
        patterns[i] = (adcChs[i]<<4) | 7;
    }
    union{
        uint32_t tab[2];
        unsigned char pat[ADC_DMA_LEN];
    } patTab;
    for(int i=0; i<8; ++i){
        patTab.pat[i] = patterns[(i-i%4) + (3-i%4)];
    }
    LOGD("%08x ", patTab.tab[0]);
    LOGD("%08x ", patTab.tab[1]);
    SYSCON.saradc_sar1_patt_tab[0] = patTab.tab[0];
    SYSCON.saradc_sar1_patt_tab[1] = patTab.tab[1];
    SYSCON.saradc_ctrl2.sar1_inv = 1;

    for(int i=0; i < sizeof(GPIO_ADC) / sizeof(GPIO_ADC[0]); ++i){
        gpio_set_direction(GPIO_ADC[i], GPIO_MODE_INPUT);
        gpio_set_pull_mode(GPIO_ADC[i], GPIO_FLOATING);
    }
#endif

    for(int ch=0; ch<NMOTOR_DIRECT; ++ch){
        Pwm(ch, 0);
    }
    LOGD("nPads %d", touchPads.NPad());

    //  Check ADC
    vTaskDelay(1);
    for (int ch = 0; ch < NMOTOR_DIRECT*2; ++ch) {
        if(adcRaws[ch] != 0) goto ADC_OK;
    }
    LOGE("Failed to initialize ADC. Move to deep sleep and restart.");
    esp_deep_sleep(1000*1000);  //  sleep time in us.
    ADC_OK: ;
}

void MotorDriver::Pwm(int ch, SDEC duty){  //   SDEC -1 to 1 (-1024 - 1024)
#ifndef _WIN32
	if (duty > 0){
        uint32_t set_duty = (MCPWM0.timer[(mcpwm_timer_t)ch].period.period) * duty / SDEC_ONE;
        mcpwm_set_signal_low(MCPWM_UNIT_0, (mcpwm_timer_t)ch, MCPWM_OPR_B);
        mcpwm_set_duty_in_us(MCPWM_UNIT_0, (mcpwm_timer_t)ch, MCPWM_OPR_A, set_duty);
        mcpwm_set_duty_type(MCPWM_UNIT_0, (mcpwm_timer_t)ch, MCPWM_OPR_A, MCPWM_DUTY_MODE_0); //call this each time, if operator was previously in low/high state
    }else{
        uint32_t set_duty = (MCPWM0.timer[(mcpwm_timer_t)ch].period.period) * -duty / SDEC_ONE;
        mcpwm_set_signal_low(MCPWM_UNIT_0, (mcpwm_timer_t)ch, MCPWM_OPR_A);
        mcpwm_set_duty_in_us(MCPWM_UNIT_0, (mcpwm_timer_t)ch, MCPWM_OPR_B, set_duty);
        mcpwm_set_duty_type(MCPWM_UNIT_0, (mcpwm_timer_t)ch, MCPWM_OPR_B, MCPWM_DUTY_MODE_0); //call this each time, if operator was previously in low/high state
    }
#else
	pwm[ch] = duty;
	float dt = 0.1f;
	theta[ch] = theta[ch] + pwm[ch] * dt;
	motorDriver.adcRaws[ch * 2] = (int) ((1+sin(theta[ch])) * msinOffset[ch]);
	motorDriver.adcRaws[ch * 2 + 1] = (int) ((1 + cos(theta[ch])) * mcosOffset[ch]);
#endif
}
int MotorDriver::GetAdcRaw(int ch){
    return adcRaws[ch];
}

#define ADC_CENTER  (16353/2)
const SDEC mcosOffset[NAXIS] ={
    ADC_CENTER, ADC_CENTER, ADC_CENTER
};
const SDEC msinOffset[NAXIS] ={
    ADC_CENTER, ADC_CENTER, ADC_CENTER
};

extern "C"{
    void readADC(){
        int i;
        for(i=0; i<MotorDriver::NMOTOR_DIRECT; ++i){
            msin[i] = motorDriver.adcRaws[i*2] - msinOffset[i];
            mcos[i] = motorDriver.adcRaws[i*2 + 1] - mcosOffset[i];
        }
    }
    void setPwm(int ch, SDEC ratio){
        motorDriver.Pwm(ch, ratio);
    }
}
#ifndef SAVE_ALL_MOTOR_PARAM_ON_WROOM
extern "C" void saveMotorParam(){
    NVS nvs("motor");
    for(int i=0; i<NMOTOR; ++i){
        //  heatLimit
        char keyLimit[11]   = "heatLimit0";
        char keyRelease[13] = "heatRelease0";
        keyLimit[9] = '0' + i;
        keyRelease[11] = '0' + i;
        nvs.set(keyLimit, (int)motorHeatLimit[i]);
        nvs.set(keyRelease, (int)motorHeatRelease[i]);

        char keyControlK[10]   = "controlK0";
        char keyControlB[10]   = "controlB0";
        char keyControlA[10]   = "controlA0";
        keyControlK[8] = '0' + i;
        keyControlB[8] = '0' + i;
        keyControlA[8] = '0' + i;
        nvs.set(keyControlK, pdParam.k[i]);
        nvs.set(keyControlB, pdParam.b[i]);
        nvs.set(keyControlA, pdParam.a[i]);

        char keyTorqueMin[11]   = "torqueMin0";
        char keyTorqueMax[11]   = "torqueMax0";
        keyTorqueMin[9] = '0' + i;
        keyTorqueMax[9] = '0' + i;
        nvs.set(keyTorqueMin, torqueLimit.min[i]);
        nvs.set(keyTorqueMax, torqueLimit.max[i]);
    }
    nvs.commit();
}
extern "C" void loadMotorParam(){
    NVS nvs("motor");
    int v;
    for(int i=0; i<NMOTOR; ++i){
        //  kba
        char keyControlK[10]   = "controlK0";
        char keyControlB[10]   = "controlB0";
        char keyControlA[10]   = "controlA0";
        keyControlK[8] = '0' + i;
        keyControlB[8] = '0' + i;
        keyControlA[8] = '0' + i;
        pdParam.k[i] = PDPARAM_K;
        pdParam.b[i] = PDPARAM_B;
        pdParam.a[i] = PDPARAM_A;
        if (nvs.get(keyControlK, v) == ESP_OK) pdParam.k[i] = v;
        else nvs.set(keyControlK, pdParam.k[i]);
        if (nvs.get(keyControlB, v) == ESP_OK) pdParam.b[i] = v;
        else nvs.set(keyControlB, pdParam.b[i]);
        if (nvs.get(keyControlA, v) == ESP_OK) pdParam.a[i] = v;
        else nvs.set(keyControlA, pdParam.a[i]);
        //LOGI("pdParam: %s:%d %s:%d %s:%d", keyControlK, pdParam.k[i], keyControlB, pdParam.b[i], keyControlA, pdParam.a[i]);

        //  heatLimit
        char keyLimit[11]   = "heatLimit0";
        char keyRelease[13] = "heatRelease0";
        keyLimit[9] = '0' + i;
        keyRelease[11] = '0' + i;
        motorHeatLimit[i] = MOTOR_HEAT_LIMIT;
        motorHeatRelease[i] = MOTOR_HEAT_RELEASE;
        int v;
        if (nvs.get(keyLimit, v) == ESP_OK) motorHeatLimit[i] = v;
        else nvs.set(keyLimit, (int)motorHeatLimit[i]);
        if (nvs.get(keyRelease, v) == ESP_OK) motorHeatRelease[i] = v;
        else nvs.set(keyRelease, (int)motorHeatRelease[i]);
        //LOGI("heat: %s:%ld %s:%d", keyLimit, motorHeatLimit[i], keyRelease, motorHeatRelease[i]);

        //  torqueLimit
        if (esp_reset_reason() == ESP_RST_BROWNOUT){    //  in case brownout, motors must be turned off. 
            torqueLimit.min[i] = 0;
            torqueLimit.max[i] = 0;
        }else{
            char keyTorqueMin[11]   = "torqueMin0";
            char keyTorqueMax[11]   = "torqueMax0";
            keyTorqueMin[9] = '0' + i;
            keyTorqueMax[9] = '0' + i;
            torqueLimit.min[i] = -SDEC_ONE;
            torqueLimit.max[i] = SDEC_ONE;
            if (nvs.get(keyTorqueMin, v) == ESP_OK) torqueLimit.min[i] = v;
            else nvs.set(keyTorqueMin, torqueLimit.min[i]);
            if (nvs.get(keyTorqueMax, v) == ESP_OK) torqueLimit.max[i] = v;
            else nvs.set(keyTorqueMax, torqueLimit.max[i]);
        }
        //LOGI("torque: %s:%d %s:%d", keyTorqueMin, torqueLimit.min[i], keyTorqueMax, torqueLimit.max[i]);
    }
    nvs.commit();
}
#else
extern "C" void loadMotorParam(){}
extern "C" void saveMotorParam(){}
#endif