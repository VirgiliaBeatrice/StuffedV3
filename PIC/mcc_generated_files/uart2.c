/**
  UART2 Generated Driver File

  @Company
    Microchip Technology Inc.

  @File Name
    uart2.c

  @Summary
    This is the generated driver implementation file for the UART2 driver using Foundation Services Library

  @Description
    This header file provides implementations for driver APIs for UART2.
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
#include "uart2.h"
#include "../uart.h"

/**
  Section: UART2 APIs
*/

void UART2_Initialize(void)
{
    // Set the UART2 module to the options selected in the user interface.

    // STSEL 1; PDSEL 8N; RTSMD disabled; OVFDIS disabled; ACTIVE disabled; RXINV disabled; WAKE disabled; BRGH enabled; IREN disabled; ON enabled; SLPEN disabled; SIDL disabled; ABAUD disabled; LPBACK disabled; UEN TX_RX; CLKSEL PBCLK; 
    U2MODE = (0x8008 & ~(1<<15)); // disabling UART ON bit  
    // UTXISEL TX_ONE_CHAR; UTXINV disabled; ADDR 0; MASK 0; URXEN disabled; OERR disabled; URXISEL RX_ONE_CHAR; UTXBRK disabled; UTXEN disabled; ADDEN disabled; 
    U2STA = 0x0;
#if defined BOARD1_MOTORDRIVER
    // BaudRate = 3000000; Frequency = 24000000 Hz; BRG 1; 
    setBaudrate(U2BRG, PNVDATA->baudrate[1]); //  for monitor
#elif defined BOARD2_COMBINATION || defined BOARD3_SEPARATE || defined BOARD4
    // BaudRate = 2000000; Frequency = 24000000 Hz; BRG 2; 
    setBaudrate(U2BRG, PNVDATA->baudrate[0]); //  for command
#else
#error
#endif
     
    //Make sure to set LAT bit corresponding to TxPin as high before UART initialization
#if defined BOARD1_MOTORDRIVER
    U2STASET = _U2STA_UTXEN_MASK;
#elif defined BOARD2_COMBINATION || defined BOARD3_SEPARATE || defined BOARD4
	//U2STASET = _U2STA_UTXEN_MASK;	//	Do not on TX until called by master.
#else
#error
#endif
    U2MODESET = _U2MODE_ON_MASK;  // enabling UART ON bit
    U2STASET = _U2STA_URXEN_MASK;  
}


bool UART2_is_tx_ready(void)
{
    return (IFS1bits.U2TXIF && U2STAbits.UTXEN);
}

bool UART2_is_rx_ready(void)
{
    return IFS1bits.U2RXIF;
}

bool UART2_is_tx_done(void)
{
    return U2STAbits.TRMT;
}

uint8_t UART2_Read(void)
{
    while(!(U2STAbits.URXDA == 1))
    {
        
    }

    if ((U2STAbits.OERR == 1))
    {
        U2STACLR = _U2STA_OERR_MASK;
    }

    

    return U2RXREG;
}

void UART2_Write(uint8_t txData)
{
    while(U2STAbits.UTXBF == 1)
    {
        
    }

    U2TXREG = txData;    // Write the data byte to the USART.
}

UART2_STATUS UART2_StatusGet (void)
{
    return U2STA;
}

#ifndef MODULETEST
static char mon_buffer[512];
static int wp=0;
static int rp=0;
bool monOut(){
    if (rp != wp && !UMSTAbits.UTXBF){
		UMTXREG = mon_buffer[rp];
		if (rp < sizeof(mon_buffer)-1) rp++;
		else rp = 0;
	}
    return rp != wp;    //  if remain return true
}
int monWaiting(){
    int rv = rp - wp;
    if (rv < 0) rv += sizeof(mon_buffer); 
    if (rv >= sizeof(mon_buffer)) rv -= sizeof(mon_buffer); 
    return rv;
}
void _mon_putc(char c) {
	mon_buffer[wp] = c;
	if (wp < sizeof(mon_buffer)-1) wp++;
	else wp = 0;
	monOut();
}
#else
void _mon_putc(char c) {
    while(UMSTAbits.TRMT == 0);  
    UMTXREG = c;        
}
#endif

/**
  End of File
*/
