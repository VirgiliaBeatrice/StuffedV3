#include "env.h"
#include "fixed.h"
#include "control.h"
#include "nvm.h"
#include <assert.h>


struct MotorState motorTarget, motorState;
SDEC currentTarget[NMOTOR]; //  Current target sent from host.
SDEC targetPwm[NMOTOR];  //  Previous target torque to fit the current to the target.
SDEC forceControlJK[NFORCE][NMOTOR];
struct PdParam pdParam;
struct TorqueLimit torqueLimit;
struct Targets targets;
LDEC lastTorques[NMOTOR];	//	last intended torque to limit sudden torque change.
SDEC lastRatio[NMOTOR];		//	last applied torque to limit heat


enum ControlMode controlMode, nextControlMode;
//	 angle
SDEC mcos[NAXIS], msin[NAXIS];
SDEC forceOffset[NFORCE];
SDEC currentSense[NMOTOR];

//	motor heat limit
#define USE_HEAT_LIMIT
#ifdef USE_HEAT_LIMIT
long motorHeat[NMOTOR];
SDEC motorHeatRelease[NMOTOR];
long motorHeatLimit[NMOTOR];
#define MOTOR_VEL_MAX		(5 * LDEC_ONE / 3000)			//	(300PRM/60s = 5rps) / 3000Hz
LDEC motorVelMax[NMOTOR];
struct TorqueLimit torqueLimitHeat;
#endif	//	USE_HEAT_LIMIT

uint32_t controlCount;
int underflowCount;

#ifdef WROOM
#define DISABLE_INTERRUPT	xSemaphoreTake(mutexForControl, portMAX_DELAY);
#define ENABLE_INTERRUPT	xSemaphoreGive(mutexForControl);
#endif
#ifdef PIC
#define DISABLE_INTERRUPT 	asm volatile("di"); // Disable all interrupts  
#define ENABLE_INTERRUPT	asm volatile("ei"); // Enable all interrupt	
#endif


#ifdef WROOM	//	Mutex for control
#include "freertos/FreeRTOS.h"
#include <freertos/task.h>
#include <freertos/semphr.h>
static xSemaphoreHandle mutexForControl;
#endif

//----------------------------------------------------------------------------
//	Control
//
void updateMotorState(){
    static u_short count;
	int i;
	readADC();
    for(i=0; i<NMOTOR; ++i){
        LDEC sense, prev, cur, diff;
		if (mcos[i] || msin[i]){
			sense = S2LDEC(atan2SDEC(msin[i], mcos[i]));
			cur = prev = motorState.pos[i];
			diff = sense - GetDecimalL(prev);
			cur += diff;
			if (diff < -LDEC_ONE/2){
				cur += LDEC_ONE;
			}else if (diff > LDEC_ONE/2){
				cur -= LDEC_ONE;
			}
#if 0	//	move motor pos for debug. (by hasevr) 
			cur = prev + ((i%2)*2 - 1) * (SDEC)(SDEC_ONE*0.01);
#endif
			motorState.pos[i] = cur;
			motorState.vel[i] = motorState.pos[i] - prev;
		}
#if 0
		if (i==0){
			logPrintf("p %f,  s %f, (%d,%d) %d\r\n", LDEC2DBL(motorState.pos[i]), LDEC2DBL(sense), msin[i], mcos[i], diff);
		}
#endif
    }
#ifdef USE_HEAT_LIMIT	//	motor heat
	count ++;
	if (count > 300){	//	10Hz
		count = 0;
		for(i=0; i<NMOTOR; ++i){
			LDEC vel = motorState.vel[i];
			if (vel < 0) vel = -vel;
			if (vel > motorVelMax[i]) motorVelMax[i] = vel;
			SDEC ratio = lastRatio[i];
			if (ratio < 0) ratio = -ratio;
			SDEC velRatio = L2SDEC((motorVelMax[i] - vel*3/2) * LDEC_ONE / motorVelMax[i]);
			if (velRatio < 0) velRatio = 0;
			if (velRatio > SDEC_ONE) velRatio = SDEC_ONE;
			SDEC deltaH = (long)velRatio * ratio / SDEC_ONE;
			assert(deltaH >= 0);
			motorHeat[i] += deltaH;                     //	heating by motor
			motorHeat[i] -= motorHeatRelease[i];        //	heat release to out air
			if (motorHeat[i] < 0) motorHeat[i] = 0;
			torqueLimitHeat.max[i] = torqueLimit.max[i];
			torqueLimitHeat.min[i] = torqueLimit.min[i];
			if (motorHeat[i] > (motorHeatLimit[i] >> 2)){
				SDEC ratio = (motorHeatLimit[i] - motorHeat[i]) / 3 * 4 / (motorHeatLimit[i] / SDEC_ONE);
				if (ratio < 0) ratio = 0;
				if (ratio > SDEC_ONE) ratio = SDEC_ONE; 
				SDEC limit =  (SDEC)((long)(SDEC_ONE - motorHeatRelease[i]) * ratio / SDEC_ONE) + motorHeatRelease[i];
				if (torqueLimitHeat.max[i] > limit) torqueLimitHeat.max[i] = limit;
				if (torqueLimitHeat.min[i] < -limit) torqueLimitHeat.min[i] = -limit; 
			}
		}
	}
#endif
}

void setPwmWithLimit(int ch, SDEC ratio){
#ifdef USE_HEAT_LIMIT
    if (ratio > torqueLimitHeat.max[ch]) ratio = torqueLimitHeat.max[ch];
    if (ratio < torqueLimitHeat.min[ch]) ratio = torqueLimitHeat.min[ch];
#else
    if (ratio > torqueLimit.max[ch]) ratio = torqueLimit.max[ch];
    if (ratio < torqueLimit.min[ch]) ratio = torqueLimit.min[ch];
#endif
	lastRatio[ch] = ratio;
	setPwm(ch, ratio);
}

//	set motor power for PD control
void pdControl(){
	int i;
	for(i=0; i<NMOTOR; ++i){
		LDEC torque;
		LDEC diff = motorTarget.pos[i] - motorState.pos[i];
		int sign = pdParam.k[i] > 0 ? 1 : -1;
		if (diff > LDEC_ONE){
			torque = sign * LDEC_ONE;
		}else if (diff < -LDEC_ONE){
			torque = -sign * LDEC_ONE;
		}else{	// diff is less than 16 bits
			torque = (pdParam.k[i] * diff + 
				pdParam.b[i] * (motorTarget.vel[i] - motorState.vel[i])*32 );	//	b must be large for stability.
			torque >>= (SDEC_BITS-4);   //  k and b are SDEC. But x16 is done here.
			if (torque < -LDEC_ONE) torque = -LDEC_ONE;
			else if (torque > LDEC_ONE) torque = LDEC_ONE;
		}
#if 1	//	This reduce impulsive current
		const LDEC diffLimit = (LDEC)(LDEC_ONE * 1);	//	smaller limit makes the control instable.
		LDEC torqueDiff = torque - lastTorques[i];
		if (torqueDiff > diffLimit){
			torque = lastTorques[i] + diffLimit;
		}
		if (torqueDiff < -diffLimit){
			torque = lastTorques[i] - diffLimit;
		}
		lastTorques[i] = torque;
#endif 
		setPwmWithLimit(i, L2SDEC(torque));
#if 0	//	to check pd control
		tcw ++;
		if (i==2 && tcw > 1000){
			tcw = 0;
			logPrintf("G:%d  C:%d  T:%d\r\n", motorTarget.pos[i], motorState.pos[i], torque);
		}
#endif
	}
}
inline int truncate(int min, int val, int max){
    if (val < min) val = min;
    if (val > max) val = max;
    return val;
}
void currentControl(){
    const SDEC DIFF_ZERO_LIMIT = SDEC_ONE / 16;
	int i;
    SDEC diff;
    int sign;
	for(i=0; i<NCURRENT && i<NMOTOR; ++i){
        if (currentTarget[i] > 0){
            diff = currentTarget[i] - currentSense[i];
            sign = 1;
        }else if (currentTarget[i] < 0){
            diff = -currentTarget[i] - currentSense[i];
            sign = -1;
        }else{
            sign = 0;
        }
        diff = ((int)diff * pdParam.a[i]) >> SDEC_BITS;
        if (currentSense[i] < 2 && diff > DIFF_ZERO_LIMIT){
            targetPwm[i] = 0;
            diff = DIFF_ZERO_LIMIT;
        }
        {
            //  to avoid vibration, give some gap
            short gap = (currentSense[i]>>4) + 1;
            if (diff >= gap) diff -= gap;
            else if (diff <= -gap) diff += gap;
            else diff = 0;
        }
        targetPwm[i] += sign*diff;
        if (sign > 0){
            if (targetPwm[i] < 0) targetPwm[i] = 0;
            else if (targetPwm[i] > SDEC_ONE) targetPwm[i] = SDEC_ONE;
        }else if (sign < 0){
            if (targetPwm[i] > 0) targetPwm[i] = 0;
            else if (targetPwm[i] < -SDEC_ONE) targetPwm[i] = -SDEC_ONE;
        }else{
            targetPwm[i] = 0;
        }
		setPwmWithLimit(i, targetPwm[i]);
    }
	for(; i < NMOTOR; ++i){
        targetPwm[i] = currentTarget[i];
		setPwmWithLimit(i, targetPwm[i]);        
    }
}

//----------------------------------------------------------------------------
//	Targets and interpolation
//
void targetsWrite(){
	if (targets.write < NTARGET-1){
			targets.write++;
	}else{
		targets.write = 0;
	}
}
//	Update or add interpolate target
void targetsAddOrUpdate(SDEC* pos, short period, unsigned char tcw){
	targetsForceControlAddOrUpdate(pos, NULL, period, tcw);
}
//	Update or add interpolate target with force control
void targetsForceControlAddOrUpdate(SDEC* pos, SDEC JK[NFORCE][NMOTOR], short period, unsigned char tcw){
	char delta;					//	tcw - tcr	
	unsigned char avail, tcr, read;
	if (period == 0) return;	//	for vacancy check
	//	check targets delta
	DISABLE_INTERRUPT
	read = targets.read;
	avail = targetsReadAvail();
	tcr = targets.targetCountRead;
	ENABLE_INTERRUPT
	delta = tcw - tcr;
	PIC_LOGI("targetsAdd m0:%d pr:%d c:%d | tcr=%d read=%d delta=%d\r\n", (int)pos[0], (int)period, (int)tcw, 
		(int)tcr, (int)read, (int)delta);
	if (delta > avail){
		//	target count jumped. may be communication error.
		PIC_LOGE("CJ\r\n");
		targets.targetCountRead = tcw - (avail-1);
	}
	
	/*	buf[0],[1] is currently used for interpolation. buf[2] will be used in the next step.
		So, we can update from buf[3] to buf[read + avail-1]. Also we can add to buf[read + avail]	*/
	if (delta == avail || (3 <= delta && delta < avail)){	//	add or update
		int i, j;
		int w = (read + delta) % NTARGET;
		targets.buf[w].period = period;
		for(i=0; i<NMOTOR; ++i){
			targets.buf[w].pos[i] = pos[i];
		}
		if (JK){
			for(i=0; i<NMOTOR; ++i){
				for(j=0; j<NFORCE; ++j){
					targets.buf[w].Jacob[j][i] = JK[j][i];
				}
			}
		}
		PIC_LOGI("Write@%d ra:%d p:%d c:%d", w, avail, period, (int)tcw);
		if (w == targets.write){
			PIC_LOGI(" Add.\r\n");
			assert(delta == avail);
			if (targetsWriteAvail()){
				targetsWrite();
			}else{
				PIC_LOGE("Error: overflow of targets targetCountRead shifted\r\n");
				targets.targetCountRead = tcr + 1;
			}
		}else{
			PIC_LOGI(" Update.\r\n");		
		}
	}
}
void targetsInit(){
	/*
     * Initial state
     * buf[0] = buf[1] current position, period = 1.
     * interpolate between 0 and 1, ie. read=0, write=2
     * targetCountWrite start from 0. So targetCountRead start from -2.
     */
    int i, j;
	targets.tick = 1;
	targets.read = 0; 
	targets.write = 2;
	targets.targetCountRead = 0x100 - 2;
	updateMotorState();
	targets.buf[0].period = 1;
	targets.buf[1].period = 1;
	for(i=0; i<NMOTOR; ++i){
		targets.buf[0].pos[i] = L2SDEC(motorState.pos[i]);
		targets.buf[1].pos[i] = L2SDEC(motorState.pos[i]);
		for(j=0; j<NFORCE; ++j){
			targets.buf[0].Jacob[j][i] = 0;
			targets.buf[1].Jacob[j][i] = 0;
			forceControlJK[j][i] = 0;
		}
	}
}

//----------------------------------------------------------------------------------
//	Time critical code (for motor control).
//	Do not use printf from here.
void targetsTickProceed(){
	targets.tick ++;
	if (targets.tick >= targets.buf[(targets.read+1)%NTARGET].period){
		if (targetsReadAvail() > 2){
			targets.tick = 0;
			targets.targetCountRead ++;
			if (targets.read < NTARGET-1){
				targets.read ++;
			}else{
				targets.read = 0;
			}
			//	Never use LOG here. This code is called from interrupt and can not use printf here.
			//PIC_LOGI("TickProceed: Read=%d tcr=%d\r\n", targets.read, targets.targetCountRead);
		}else{
			targets.tick = targets.buf[(targets.read+1)%NTARGET].period;
			//	Never use LOG here. This code is called from interrupt and can not use printf here.
			//PIC_LOGW("TickProceed: Underflow. Failed to increment read=%d tcr=%d\r\n", targets.read, targets.targetCountRead);
			underflowCount ++;
		}
	}
}
void targetsProceed(){
	int i, readPlus;
	short period;
	targetsTickProceed();
	readPlus = (targets.read+1)%NTARGET;
	period = targets.buf[readPlus].period;
	assert(period != 0);
	for(i=0; i<NMOTOR; ++i){
#if 0
		motorTarget.pos[i] = ((period - targets.tick)* S2LDEC(targets.buf[targets.read].pos[i])
			+ targets.tick*S2LDEC(targets.buf[readPlus].pos[i])) / period;
#else
        SDEC diff = targets.buf[readPlus].pos[i] - targets.buf[(int)targets.read].pos[i];
        motorTarget.pos[i] = S2LDEC(targets.buf[(int)targets.read].pos[i]) + S2LDEC((int)diff * (int)targets.tick / period); 
#endif
		motorTarget.vel[i] = S2LDEC(targets.buf[readPlus].pos[i] - targets.buf[(int)targets.read].pos[i]) / period;
#ifdef _WIN32
		filePrintf("%d,%d,", motorTarget.pos[i], motorTarget.vel[i]);
#endif
	}
#ifdef _WIN32
	filePrintf("\n");
#endif
}

LDEC deltaPosForceControl[NMOTOR];
void targetsForceControlProceed(){
	int i, j, readPlus;
	short period;
	targetsTickProceed();
	readPlus = (targets.read+1)%NTARGET;
	period = targets.buf[readPlus].period;
	for(i=0; i<NMOTOR; ++i){
        deltaPosForceControl[i] = (deltaPosForceControl[i] * ( 0x1000 - 1 )) >> 12; //  multiply (4096-1)/4096 
		for(j=0; j<NFORCE; ++j){
			forceControlJK[j][i] = ((period - targets.tick)* S2LDEC(targets.buf[(int)targets.read].Jacob[j][i])
			+ targets.tick*S2LDEC(targets.buf[readPlus].Jacob[j][i])) / period;
			deltaPosForceControl[i] += forceControlJK[j][i] * getForce(j);
		}
	}
	for(i=0; i<NMOTOR; ++i){
        SDEC diff = targets.buf[readPlus].pos[i] - targets.buf[(int)targets.read].pos[i];
        motorTarget.pos[i] = S2LDEC(targets.buf[(int)targets.read].pos[i]) + S2LDEC((int)diff * (int)targets.tick / period) + deltaPosForceControl[i];
		motorTarget.vel[i] = S2LDEC(targets.buf[readPlus].pos[i] - targets.buf[(int)targets.read].pos[i]) / period;
	}
}
void controlLoop(){
	controlCount ++;
	controlMode = nextControlMode;
	
	#ifdef WROOM
	xSemaphoreTake(mutexForControl, portMAX_DELAY);
	#endif
    if (controlMode == CM_INTERPOLATE){
        targetsProceed();
    }else if (controlMode == CM_FORCE_CONTROL){
        targetsForceControlProceed();
	}
	#ifdef WROOM
	xSemaphoreGive(mutexForControl);
	#endif

    updateMotorState();
	if (controlMode == CM_CURRENT){
        currentControl();
    }else{
        pdControl();
    }
}
void controlInit(){
	int i;
	controlMode = nextControlMode = CM_CURRENT;
	for(i=0; i<NMOTOR; ++i){
#ifdef USE_HEAT_LIMIT
		motorVelMax[i] = MOTOR_VEL_MAX;
#endif
		torqueLimit.max[i] = SDEC_ONE;
		torqueLimit.min[i] = -SDEC_ONE;
        currentTarget[i] = 0;
        targetPwm[i] = 0;
	}
	loadMotorParam();
#ifdef PIC
	controlInitPic();
#endif
#ifdef WROOM
	mutexForControl = xSemaphoreCreateMutex();
#endif
}
void controlSetMode(enum ControlMode m){
	if (controlMode != m){
		if ((m == CM_INTERPOLATE || m == CM_FORCE_CONTROL) 
			&& (controlMode != CM_INTERPOLATE && controlMode != CM_FORCE_CONTROL)){
			//	current mode is not interpolate or force control.
			//	So targets and targetCounts can be initialized without locking.
			targetsInit();
		}
		nextControlMode = m;
	}
}

#ifdef MODULETEST
void printMotorState(){
#if 1
    int i;
	for(i=0; i<NMOTOR; ++i){
        logPrintf("%f\t", LDEC2DBL(motorState.pos[i]) );
    }
	for(i=0; i<NMOTOR; ++i){
        logPrintf("%f\t", LDEC2DBL(motorState.vel[i]) );
    }
    logPrintf("\n");
#endif
}
void printMotorTarget(){
    int i;
	for(i=0; i<NMOTOR; ++i){
        logPrintf("%f\t", LDEC2DBL(motorTarget.pos[i]) );
    }
	for(i=0; i<NMOTOR; ++i){
        logPrintf("%f\t", LDEC2DBL(motorTarget.vel[i]) );
    }
    logPrintf("\n");
}
void printMotorControl(){
    int i;
	for(i=0; i<1; ++i){
        logPrintf("%f\t", LDEC2DBL(motorTarget.pos[i]));
        logPrintf("%f\t", LDEC2DBL(motorTarget.vel[i]));
        logPrintf("%f\t", LDEC2DBL(motorState.pos[i]));
        logPrintf("%f\t", LDEC2DBL(motorState.vel[i]));
        logPrintf("%f\t", LDEC2DBL(motorTorques[i]) );
    }
    logPrintf("\n");
}
#endif

