#ifndef COMMAND_H
#define COMMAND_H
#include "commandCommon.h"
#include "boardType.h"
#include <stdbool.h>

void commandInit();
bool uartExecCommand();
typedef void ExecCommand();

#ifdef WROOM
void ExecCmd(void* cmd, int len);
void ExecRet(void* ret, int len);
#endif


#ifndef WROOM
extern CommandPacket command;
extern ReturnPacket retPacket;
extern int cmdCur;
extern int cmdLen;
extern int retCur;
extern int retLen;
extern ExecCommand* execCommand[CI_NCOMMAND];
extern ExecCommand* returnCommand[CI_NCOMMAND];
extern unsigned char cmdPacketLens[MAXBOARDID+1][CI_NCOMMAND];
extern unsigned char boardId;
#endif

#endif
