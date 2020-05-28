#include "fixed.h"
#include "command.h"
#include "control.h"
#include "boardType.h"
#include "nvm.h"
#include "uart.h"
#include <string.h>
#ifdef WROOM
#include "../WROOM/main/SoftRobot/commandWROOM.h"
#endif

unsigned char boardId;
CommandPacket command;
int cmdCur;
int cmdLen;
int retLen;

//  implementation for getTouch() (read touch sensor value).
#ifdef WROOM
extern SDEC getTouch(int i);
#else
static inline SDEC getTouch(int i){
    PIC_LOGE("getTouch called");
}
#endif

//	command packet length for all boards
unsigned char cmdPacketLens[MAXBOARDID+1][CI_NCOMMAND];

void ecNop(){
}
void ecSetCmdLen(){
	int c;
	for(c=0; c<CI_NCOMMAND; ++c){
		cmdPacketLens[command.boardId][c] = command.cmdLen.len[c];
	}
}
void ecAll(){
	if (command.all.controlMode == CM_DIRECT) {
		int i;
		SDEC newPos, diff;
		controlSetMode(CM_DIRECT);
		for (i = 0; i < NMOTOR; ++i) {
			newPos = command.all.pos[i];
			diff = newPos - L2SDEC(motorTarget.pos[i]);
			motorTarget.pos[i] += S2LDEC(diff);
			motorTarget.vel[i] = S2LDEC(command.all.vel[i]);
		}
	}
	else if (command.all.controlMode == CM_CURRENT) {
		int i;
		controlSetMode(CM_CURRENT);
		for (i = 0; i<NMOTOR; ++i) {
			currentTarget[i] = command.all.current[i];
		}
	}else if (command.all.controlMode == CM_INTERPOLATE){
        controlSetMode(CM_INTERPOLATE);
        targetsAddOrUpdate(command.all.pos, command.all.period, command.all.targetCountWrite);
    }else if (command.all.controlMode == CM_FORCE_CONTROL){
        controlSetMode(CM_FORCE_CONTROL);
    	targetsForceControlAddOrUpdate(command.all.pos , command.all.jacob, command.all.period, command.all.targetCountWrite);
    }
}
void ecDirect(){
    int i;
    static SDEC newPos, diff;
    controlSetMode(CM_DIRECT);
    for(i=0; i<NMOTOR; ++i){
        newPos = command.direct.pos[i];
        diff = newPos - L2SDEC(motorTarget.pos[i]);
        motorTarget.pos[i] += S2LDEC(diff);
        motorTarget.vel[i] = S2LDEC(command.direct.vel[i]);
    }
}
void ecCurrent(){
    int i;
    controlSetMode(CM_CURRENT);
    for(i=0; i<NMOTOR; ++i){
        currentTarget[i] = command.current.current[i];
    }
}
void ecInterpolate(){
    controlSetMode(CM_INTERPOLATE);
	targetsAddOrUpdate(command.interpolate.pos, command.interpolate.period, command.interpolate.targetCountWrite);
}
void ecForceControl(){
    controlSetMode(CM_FORCE_CONTROL);
	targetsForceControlAddOrUpdate(command.forceControl.pos , command.forceControl.jacob, command.forceControl.period, command.forceControl.targetCountWrite);
}
void ecSetParam(){
    int i;
    bool bSaveParam = false;
    switch(command.param.type){
    case PT_PD:
        for(i=0; i<NMOTOR; ++i){
            pdParam.k[i] = command.param.pd.k[i];
            pdParam.b[i] = command.param.pd.b[i];
        }
        bSaveParam = true;
        break;
    case PT_CURRENT:
        for(i=0; i<NMOTOR; ++i){
            pdParam.a[i] = command.param.a[i];
        }
        bSaveParam = true;
        break;
    case PT_TORQUE_LIMIT:
        for(i=0; i<NMOTOR; ++i){
            torqueLimit.min[i] = command.param.torque.min[i];
            torqueLimit.max[i] = command.param.torque.max[i];
        }
        break;
    case PT_BOARD_ID:{
#ifdef PIC
        NvData nvData;
        NVMRead(&nvData);
        nvData.boardId = command.param.boardId;
        NVMWrite(&nvData);
        boardId = PNVDATA->boardId;
        if (boardId > 7) boardId = 7;
#endif
        } break;
    case PT_BAUDRATE:{
#ifdef PIC
        NvData nvData;
        NVMRead(&nvData);
        memcpy(nvData.baudrate, command.param.baudrate, sizeof(nvData.baudrate));
        setBaudrate(UCBRG, nvData.baudrate[0]);
        setBaudrate(UMBRG, nvData.baudrate[1]);
        NVMWrite(&nvData);
#endif
        } break;
    case PT_MOTOR_HEAT:{
        for(i=0; i<NMOTOR; ++i){
            motorHeatRelease[i] = command.param.heat.release[i];
            motorHeatLimit[i] = command.param.heat.limit[i] * command.param.heat.release[i];
        }
        bSaveParam = true;
        } break;
    default:
        PIC_LOGE("ecSetParam got wrong type %d", (int)command.param.type);
        break;
    }
    if (bSaveParam) saveMotorParam();
}
void ecResetSensor(){
    int i;
    if (command.resetSensor.flags & RSF_MOTOR){
        for(i=0; i<NMOTOR; ++i){
            motorState.pos[i] = motorState.pos[i] % LDEC_ONE;
            motorState.vel[i] = 0;
            motorTarget.pos[i] = motorTarget.pos[i] % LDEC_ONE;
        }
    }
    if (command.resetSensor.flags & RSF_FORCE){
        for(i=0; i<NFORCE; ++i){
            forceOffset[i] = getForceRaw(i);
        }
    }
}

ReturnPacket retPacket;
int retCur;
int retLen;
void rcNop(){
}
void rcBoardInfo(){
	retPacket.boardInfo.modelNumber = MODEL_NUMBER;
	retPacket.boardInfo.nTarget = NTARGET;
	retPacket.boardInfo.nMotor = NMOTOR;
	retPacket.boardInfo.nCurrent = NCURRENT;
	retPacket.boardInfo.nForce = NFORCE;
	retPacket.boardInfo.nTouch = NTOUCH;
}
void rcAll(){
    int i;
    retPacket.all.controlMode = controlMode;
    for(i=0; i<NMOTOR; ++i){
        retPacket.all.pos[i] = L2SDEC(motorState.pos[i]);
        retPacket.all.vel[i] = L2SDEC(motorState.vel[i]);
    }
    for(i=0; i<NCURRENT; ++i){
		retPacket.all.current[i] = currentSense[i];
    }
    for(i=0; i<NFORCE; ++i){
		retPacket.all.force[i] = getForce(i);
    }
    for(i=0; i<NTOUCH; ++i){
		retPacket.all.touch[i] = getTouch(i);
    }
    retPacket.all.targetCountRead = targets.targetCountRead;
	retPacket.all.tick = targets.tick;
}
void rcSensor(){
    int i;
    for(i=0; i<NMOTOR; ++i){
		retPacket.sensor.pos[i] = L2SDEC(motorState.pos[i]);
    }
    for(i=0; i<NCURRENT; ++i){
		retPacket.sensor.current[i] = currentSense[i];
    }
    for(i=0; i<NFORCE; ++i){
		retPacket.sensor.force[i] = getForce(i);
    }
    for(i=0; i<NTOUCH; ++i){
		retPacket.sensor.touch[i] = getTouch(i);
    }
}
void rcDirect(){
    int i;
	controlSetMode(CM_DIRECT);
	for(i=0; i<NMOTOR; ++i){
        retPacket.direct.pos[i] = L2SDEC(motorState.pos[i]);
        retPacket.direct.vel[i] = L2SDEC(motorState.vel[i]);
    }
}
void rcCurrent(){
    int i;
	controlSetMode(CM_CURRENT);
	for(i=0; i<NMOTOR; ++i){
        retPacket.current.pos[i] = L2SDEC(motorState.pos[i]);
		retPacket.current.vel[i] = L2SDEC(motorState.vel[i]);
		if (i < NCURRENT) {
			retPacket.current.current[i] = currentSense[i];
		}
		else {
			retPacket.current.current[i] = currentTarget[i];
		}
	}
}
inline void returnInterpolateParam(){
    int i;	
    for(i=0; i<NMOTOR; ++i){
        retPacket.interpolate.pos[i] = L2SDEC(motorState.pos[i]);
    }
    retPacket.interpolate.targetCountRead = targets.targetCountRead;
	retPacket.interpolate.tick = targets.tick;
}
void rcInterpolate(){
    controlSetMode(CM_INTERPOLATE);
    returnInterpolateParam();
}
void rcForceControl(){
    controlSetMode(CM_FORCE_CONTROL);
	returnInterpolateParam();
}
enum SetParamType getParamType = PT_PD;
void ecGetParam(){
    getParamType = command.param.type;
}
void rcGetParam(){
#ifdef WROOM
    PIC_LOGD("rcGetParam %d", getParamType);
#endif
    int i;
    retPacket.param.type = getParamType;
    switch(retPacket.param.type){
    case PT_PD:
        for(i=0; i<NMOTOR; ++i){
            retPacket.param.pd.k[i] = pdParam.k[i];
            retPacket.param.pd.b[i] = pdParam.b[i];
#ifdef WROOM
            PIC_LOGD("rcGetParam k=%d b=%d", pdParam.k[i], pdParam.b[i]);
#endif
        }
        break;
    case PT_CURRENT:
        for(i=0; i<NMOTOR; ++i){
            retPacket.param.a[i] = pdParam.a[i];
        }
        break;
    case PT_TORQUE_LIMIT:
        for(i=0; i<NMOTOR; ++i){
            retPacket.param.torque.min[i] = torqueLimit.min[i];
            retPacket.param.torque.max[i] = torqueLimit.max[i];
        }
        break;
    case PT_BOARD_ID:
#ifdef PIC
        retPacket.param.boardId = PNVDATA->boardId;
#else
        retPacket.param.boardId = -1;
#endif
        break;
    case PT_BAUDRATE:
#ifdef PIC
        getBaudrate(retPacket.param.baudrate[0], UCBRG);
        getBaudrate(retPacket.param.baudrate[1], UMBRG);
#else
        retPacket.param.baudrate[0] = 0;
        retPacket.param.baudrate[1] = 0;
#endif
        break;
    case PT_MOTOR_HEAT:
        for(i=0; i<NMOTOR; ++i){
            if (motorHeatRelease[i] < 1) motorHeatRelease[i] = 1;
            retPacket.param.heat.release[i] = motorHeatRelease[i];
            retPacket.param.heat.limit[i] = motorHeatLimit[i] / motorHeatRelease[i];
        }
        break;
    case PT_MAGNET:
        for(i=0; i<NMOTOR; ++i){
            retPacket.param.magnet.cos[i] = mcos[i];
            retPacket.param.magnet.sin[i] = msin[i];
        }
        break;
    default:
        PIC_LOGE("rcGetParam got wrong type %d, set = %d", (int)getParamType, (int)command.param.type);
        break;
    }
}

ExecCommand* execCommand[CI_NCOMMAND] = {
	ecNop,
	ecNop,          //	board info
	ecSetCmdLen,
	ecAll,
    ecNop,          //  sensor
	ecDirect,
	ecCurrent,
    ecInterpolate,
	ecForceControl,
    ecSetParam,
    ecResetSensor,
    ecGetParam,
};
ExecCommand* returnCommand[CI_NCOMMAND] = {
    rcNop,
	rcBoardInfo,
    rcNop,          //  set cmdlen
    rcAll,
	rcSensor,
    rcDirect,
    rcCurrent,
    rcInterpolate,
    rcForceControl,
	rcNop,          //	set param
    rcNop,          //	reset sensor
    rcGetParam, 
};

#ifdef WROOM
void ExecCmd(){
	execCommand[command.commandId]();
    //logPrintf("MCID:%d\r\n", command.commandId);
}
void ExecRet(void* ret, int len){
	retPacket.header = command.header;
	returnCommand[retPacket.commandId]();
    //logPrintf("MRID:%d\r\n", command.commandId);
}
#endif

void commandInit(){
	int i, c;
	for(i=0; i<MAXBOARDID+1; ++i){
		for(c=0; c<CI_NCOMMAND; ++c){
			cmdPacketLens[i][c] = cmdPacketLen[c];
		}
	}
}
