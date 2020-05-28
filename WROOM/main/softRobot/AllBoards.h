#pragma once
#include <vector>
#include "lwip/opt.h"
#include "lwip/tcpip.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "../WroomEnv.h"
#include "BoardBase.h"

class DeviceMap {
public:
	char uart;
	char board;
	char id;
	DeviceMap(char m): uart(0xFF), board(0xFF), id(m){}
	DeviceMap(char u, char b, char m): uart(u), board(b), id(m){}
} __attribute__((__packed__));

class UartForBoards;
class BoardDirect;
class UdpCmdPacket;
class UdpRetPacket;

//#define SAVE_ALL_MOTOR_PARAM_ON_WROOM	//	save on each board will be better and this option is obsolute.

//
class AllBoards{
public:
	static const char* Tag(){ return "AllB"; };
	int nBoard;
	int nTargetMin;
	static const int NUART = 2;
	std::vector<DeviceMap> motorMap;
	std::vector<DeviceMap> currentMap;
	std::vector<DeviceMap> forceMap;
	std::vector<DeviceMap> touchMap;
	volatile int* motorPos;
	short* motorOffset;
#ifdef SAVE_ALL_MOTOR_PARAM_ON_WROOM
	short* motorKba;
#endif

	UartForBoards* uart[NUART];
	BoardDirect* boardDirect;
	xTaskHandle taskExec;
	int GetNTotalMotor() { return (int)motorMap.size(); }
	int GetNTotalCurrent() { return (int)currentMap.size(); }
	int GetNTotalForce() { return (int)forceMap.size(); }
	int GetNTotalTouch() { return (int)touchMap.size(); }
	int GetNTarget() { return nTargetMin; }
	int GetSystemId() { return 0; }
	AllBoards();
	~AllBoards();
	void EnumerateBoard();	
	BoardBase& Board(char uid, char bid);
	void Init();
	bool HasRet(unsigned short id);
	///	Write contents of the UdpCmdPacket to all boards. 
	void WriteCmd(unsigned short commandId, BoardCmdBase& packet);	
	///	Read returns of all boards to  UdpRetPacket.   bNext: start to read next UART data (send notify to recvTask).
	void ReadRet(unsigned short commandId, BoardRetBase& packet);

	void ExecLoop();

	void LoadMotorPos();	//	load motor position afte enumerate boards
	void SaveMotorPos();	//	save motor position to nvs
#ifdef SAVE_ALL_MOTOR_PARAM_ON_WROOM
	void LoadMotorParam();	//	load pd and a parameter for motors on BoardDirect.
    void SaveMotorParam();
#endif
};
extern AllBoards allBoards;
