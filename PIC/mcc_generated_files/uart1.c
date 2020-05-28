/**
  UART1 Generated Driver File

  @Company
    Microchip Technology Inc.

  @File Name
    uart1.c

  @Summary
    This is the generated driver implementation file for the UART1 driver using Foundation Services Library

  @Description
    This header file provides implementations for driver APIs for UART1.
    Generation Information :
        Product Revision  :  Foundation Services Library - pic24-dspic-pic32mm : v1.26
        Device            :  PIC32MM0064GPL036
    The generated drivers are tested against the following:
        Compiler          :  XC32 1.42
        MPLAB             :  MPLAB X 3.45
*/

/*
    (c) 2016 Microchip Technology Inc. and its subsidiaries. You may use this
    software and any derivatives exclusively with Microchip products.

    THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS". NO WARRANTIES, WHETHER
    EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY IMPLIED
    WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS FOR A
    PARTICULAR PURPOSE, OR ITS INTERACTION WITH MICROCHIP PRODUCTS, COMBINATION
    WITH ANY OTHER PRODUCTS, OR USE IN ANY APPLICATION.

    IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE,
    INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND
    WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP HAS
    BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE. TO THE
    FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL CLAIMS IN
    ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT OF FEES, IF ANY,
    THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS SOFTWARE.

    MICROCHIP PROVIDES THIS SOFTWARE CONDITIONALLY UPON YOUR ACCEPTANCE OF THESE
    TERMS.
*/

/**
  Section: Included Files
*/
#include "../env.h"
#include "uart1.h"
#include "../uart.h"

/**
  Section: UART1 APIs
*/

void UART1_Initialize(void)
{
    // Set the UART1 module to the options selected in the user interface.

    // STSEL 1; PDSEL 8N; RTSMD disabled; OVFDIS disabled; ACTIVE disabled; RXINV disabled; WAKE disabled; BRGH enabled; IREN disabled; ON enabled; SLPEN disabled; SIDL disabled; ABAUD disabled; LPBACK disabled; UEN TX_RX; CLKSEL PBCLK; 
    U1MODE = (0x8008 & ~(1<<15)); // disabling UART ON bit  
    // UTXISEL TX_ONE_CHAR; UTXINV disabled; ADDR 0; MASK 0; URXEN disabled; OERR disabled; URXISEL RX_ONE_CHAR; UTXBRK disabled; UTXEN disabled; ADDEN disabled; 
    U1STA = 0x0;
#if defined BOARD1_MOTORDRIVER
    // BaudRate = 2000000; Frequency = 24000000 Hz; BRG 2; 
    setBaudrate(U1BRG, PNVDATA->baudrate[0]); //  for command
#elif defined BOARD2_COMBINATION || defined BOARD3_SEPARATE || defined BOARD4
    // BaudRate = 3000000; Frequency = 24000000 Hz; BRG 1; 
    setBaudrate(U1BRG, PNVDATA->baudrate[1]); //  for monitor
#if defined USE_MONITOR_RX && defined BOARD3_SEPARATE
    U1BRG = 0x1387; //      1200bps //  1uF requires 1ms.
//    U1BRG = 0x9C3;   //  2400bps
//    U1BRG = 0x4E1;  //  4800bps
//    U1BRG = 0x270;  //      9600bps
//    U1BRG = 0x138;  //      19200bps   
#endif

#else
#error
#endif
	
    //Make sure to set LAT bit corresponding to TxPin as high before UART initialization
#if defined BOARD1_MOTORDRIVER
	//U1STASET = _U1STA_UTXEN_MASK;	//	Do not on TX until called by master.
#elif defined BOARD2_COMBINATION || defined BOARD3_SEPARATE || defined BOARD4
	U1STASET = _U1STA_UTXEN_MASK;    //	Enable TX.
#else
#error
#endif
#if defined BOARD1_MOTORDRIVER || defined USE_MONITOR_RX   //  T1RX is used by ADC for current sense. for monitor define this in env.h
    U1STASET = _U1STA_URXEN_MASK;   //  Enable RX
#endif

    U1MODESET = _U1MODE_ON_MASK;	//  Enable UART ON bit
}


bool UART1_is_tx_ready(void)
{
    return (IFS0bits.U1TXIF && U1STAbits.UTXEN);
}

bool UART1_is_rx_ready(void)
{
    return IFS0bits.U1RXIF;
}

bool UART1_is_tx_done(void)
{
    return U1STAbits.TRMT;
}

uint8_t UART1_Read(void)
{
    while(!(U1STAbits.URXDA == 1))
    {
        
    }

    if ((U1STAbits.OERR == 1))
    {
        U1STACLR = _U1STA_OERR_MASK;
    }

    

    return U1RXREG;
}

void UART1_Write(uint8_t txData)
{
    while(U1STAbits.UTXBF == 1)
    {
        
    }

    U1TXREG = txData;    // Write the data byte to the USART.
}

UART1_STATUS UART1_StatusGet (void)
{
    return U1STA;
}


/**
  End of File
*/
