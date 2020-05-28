#include "fixed.h"
#include "command.h"
#include "control.h"
#include "boardType.h"
#include "uart.h"
#include "nvm.h"

#ifdef WROOM
#error This is a source file for PIC only. 
#endif

void commandUartInit(){
	UARTC_Initialize();
	/*	10 = Interrupt flag bit is asserted while receive buffer is 3/4 or more full (i.e., has 6 or more data characters)
		01 = Interrupt flag bit is asserted while receive buffer is 1/2 or more full (i.e., has 4 or more data characters)
		00 = Interrupt flag bit is asserted while receive buffer is not empty (i.e., has at least 1 data character)	*/
	UCSTAbits.URXISEL = 2;
	IEC_UCRXIE = 1;	// Enable UARTC receive interrupt.
	CLEAR_IFS_UCRXIF;

	//	timer to start TXC after receive command header
	//	2M baud/10bit = 200kBps= 5us/byte. Wait 5 bytes = 25us = 600/24MHz
    //  PR1 = (24MHz / baudrate) * 5 bytes 
    PR1 = (24*1000*1000 / PNVDATA->baudrate[0]) * 5;    	//PR1 = 600; //	for 2MBPS, 25us
}

uint32_t timeRetCmd, timeTx;

static volatile bool bRunExecCommand=false;
//	handler for TMR1 timer for TX
volatile bool bRunReturnCommand = false;
void __attribute__ ((vector(_TIMER_1_VECTOR), interrupt(IPL3AUTO))) TMR1_ISR()
{
	if (bRunReturnCommand){	//	call from recv
		bRunReturnCommand = false;
		PIC_LOGI("RC%d len%d ", retPacket.commandId, retLen);
        returnCommand[retPacket.commandId]();
		timeRetCmd = TMR1;
		IFS0bits.T1IF = false;
	}else{
		//	stop timer interrupt
		IEC0bits.T1IE = 0;
		//	start TX
		UCSTAbits.UTXEN = 1;	//	enable TX
		UCSTAbits.UTXISEL = 2;	//	10 = Interrupt is generated and asserted while the transmit buffer is empty
		IEC_UCTXIE = 1;	//	enable interrupt
		timeTx = TMR1 + PR1;
	}
	IFS0CLR= 1 << _IFS0_T1IF_POSITION;
}

//  Handler for TX interrupt
//	Note: "IPL2" below must fit to "IPC_UCTXIP = 2" in interrupt_manager.c;
void __attribute__ ((vector(_UARTC_TX_VECTOR), interrupt(IPL2AUTO))) _UARTC_TX_HANDLER(void){	
	//	Send
	while (retCur < retLen && !UCSTAbits.UTXBF){
		UCTXREG = retPacket.bytes[retCur];
		retCur ++;
	}
	//	StopTX
	if (retCur == retLen){
		UCSTAbits.UTXISEL = 1;		//	01 = Interrupt is generated and asserted when all characters have been transmitted
		if (UCSTAbits.TRMT){		//	TX completed
			IEC_UCTXIE = 0;	//	disable interrupt
			UCSTAbits.UTXEN = 0;	//	disable UCTX
		}
	}
	CLEAR_IFS_UCTXIF;
}
//	handler for rx interrupt
//	Note: "IPL4" below must fit to "IPC5bits.UCRXIP = 4" in interrupt_manager.c;
void __attribute__ ((vector(_UARTC_RX_VECTOR), interrupt(IPL4AUTO))) _UARTC_RX_HANDLER(void){
	int i;
	union CommandHeader head;
	static bool bRead;
	for(i=0; i<6; ++i){
        if (cmdCur == 0){
	        head.header = UCRXREG;
			//	Skip spacing (make time for return packet), invalid value (They may come after reset.) and invalid command
            if (head.header == 0 || head.header >= (CI_NCOMMAND<<BORADIDBITS)) continue;
			//	In case of valid command ID, get packet length
            cmdLen = cmdPacketLens[head.boardId][head.commandId];
            bRead = false;
			if (head.boardId == boardId){
				bRead = true;
				command.header = head.header;
                //printf("H%x\n", command.header);
				retLen = retPacketLen[command.commandId];
				if (retLen) {	//	start to return.
					//	Prepare to return
					retPacket.header = command.header;
					retCur = 0;
					//	Start TMR1 to enable TX after some delay.
					bRunReturnCommand = true;
					IEC0bits.T1IE = true;
					IFS0bits.T1IF = false;
					TMR1 = PR1-1;	//	call timer as soon as this task is ended.
				}
            }
			if (head.commandId == CI_SET_CMDLEN){
				bRead = true;
				command.header = head.header;
			}
        }else if (bRead){
			command.bytes[cmdCur] = UCRXREG;
		}else{
			head.header = UCRXREG;
		}
		if (cmdCur == cmdLen-1){
            if (bRead){
				bRunExecCommand = true;	//	exec command in main thread.
            }
            cmdCur = 0;
        } else {
			cmdCur ++;
		}
    }
	CLEAR_IFS_UCRXIF;
}

bool uartExecCommand(){
	if (bRunExecCommand){
		bRunExecCommand = false;
        execCommand[command.commandId]();
		PIC_LOGI("H%x Ex%d\r\n", (int)command.header, (int)command.commandId);
		return true;
	}else{
		return false;
	}
}
