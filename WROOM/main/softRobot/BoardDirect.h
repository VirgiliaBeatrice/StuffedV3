#pragma once

#include "Board.h"
#include "esp_log.h"
#include "MotorDriver.h"

class BoardDirect: public Board<CommandPacketBD0, ReturnPacketBD0>{
public:
	typedef Board<CommandPacketBD0, ReturnPacketBD0> base;
	BoardDirect();
	void WriteCmd(unsigned short commandId, BoardCmdBase& packet);
	void ReadRet(unsigned short commandId, BoardRetBase& packet);
};
