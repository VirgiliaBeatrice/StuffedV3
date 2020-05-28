#include "BoardDirect.h"
extern "C"{
#include "../../../PIC/control.h"
#include "../../../PIC/command.h"
}

BoardDirect::BoardDirect():Board(-1, cmdPacketLenBD0, retPacketLenBD0){
}
void BoardDirect::WriteCmd(unsigned short commandId, BoardCmdBase& packet){
    base::WriteCmd(commandId, packet);
    //  send command packet to board
	assert(sizeof(command) == sizeof(cmd));
	memcpy(&command, &cmd, sizeof(cmd));
    
    //  exec ret first. On actual PIC board ret runs before command. 
    ExecRet();
    ExecCmd();  //  exec command

#ifdef SAVE_ALL_MOTOR_PARAM_ON_WROOM
    //  To save all motor paramters in WROOM. 
    if (cmd.commandId == CI_SET_PARAM && (cmd.param.type == PT_PD || cmd.param.type == PT_CURRENT)){
        allBoards.SaveMotorParam();
    }
#endif
}
void BoardDirect::ReadRet(unsigned short commandId, BoardRetBase& packet){
    //  receive retPacket from board.
    assert(sizeof(retPacket) == sizeof(ret));
	memcpy((void*)&ret, &retPacket, sizeof(ret));
    //  copy read from board packet to udp packet.
    base::ReadRet(commandId, packet);
}
