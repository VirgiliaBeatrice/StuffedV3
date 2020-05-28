#pragma once
#include "boardType.h"
#include "nvm.h"

//	uart choice
#if defined BOARD1_MOTORDRIVER

#define UMSTA	U2STA
#define UMSTAbits	U2STAbits
#define UMTXREG	U2TXREG
#define UMRXREG	U2RXREG
#define UMBRG U2BRG

#define UCSTA	U1STA
#define UCSTAbits	U1STAbits
#define UCTXREG	U1TXREG
#define UCRXREG	U1RXREG
#define UCBRG U1BRG
#define IEC_UCRXIE	IEC0bits.U1RXIE
#define IEC_UCTXIE	IEC0bits.U1TXIE
#define IPC_UCTXIP	IPC6bits.U1TXIP
#define IPC_UCTXIS	IPC6bits.U1TXIS
#define IPC_UCRXIP	IPC5bits.U1RXIP
#define IPC_UCRXIS	IPC5bits.U1RXIS
#define CLEAR_IFS_UCTXIF		IFS0CLR = 1 << _IFS0_U1TXIF_POSITION
#define CLEAR_IFS_UCRXIF		IFS0CLR = 1 << _IFS0_U1RXIF_POSITION
#define _UARTC_TX_VECTOR	_UART1_TX_VECTOR
#define _UARTC_RX_VECTOR	_UART1_RX_VECTOR
#define UARTC_Initialize	UART1_Initialize


#elif defined BOARD2_COMBINATION || defined BOARD3_SEPARATE || defined BOARD4

#define UMSTA	U1STA
#define UMSTAbits	U1STAbits
#define UMTXREG	U1TXREG
#define UMRXREG	U1RXREG
#define UMBRG U1BRG

#define UCSTA	U2STA
#define UCSTAbits	U2STAbits
#define UCTXREG	U2TXREG
#define UCRXREG	U2RXREG
#define UCBRG U2BRG
#define IEC_UCRXIE	IEC1bits.U2RXIE
#define IEC_UCTXIE	IEC1bits.U2TXIE
#define IPC_UCTXIP	IPC10bits.U2TXIP
#define IPC_UCTXIS	IPC10bits.U2TXIS
#define IPC_UCRXIP	IPC10bits.U2RXIP
#define IPC_UCRXIS	IPC10bits.U2RXIS
#define CLEAR_IFS_UCTXIF		IFS1CLR = 1 << _IFS1_U2TXIF_POSITION
#define CLEAR_IFS_UCRXIF		IFS1CLR = 1 << _IFS1_U2RXIF_POSITION
#define _UARTC_TX_VECTOR	_UART2_TX_VECTOR
#define _UARTC_RX_VECTOR	_UART2_RX_VECTOR
#define UARTC_Initialize	UART2_Initialize
#else
#error
#endif

#define setBaudrate(r, b)   ((r) = ((6000000-1) / (b) ))
#define getBaudrate(b, r)   ((b) = (6000000 / ((r)+1)))
