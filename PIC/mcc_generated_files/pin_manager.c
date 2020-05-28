/**
  System Interrupts Generated Driver File 

  @Company:
    Microchip Technology Inc.

  @File Name:
    pin_manager.c

  @Summary:
    This is the generated manager file for the MPLAB(c) Code Configurator device.  This manager
    configures the pins direction, initial state, analog setting.
    The peripheral pin select, PPS, configuration is also handled by this manager.

  @Description:
    This source file provides implementations for MPLAB(c) Code Configurator interrupts.
    Generation Information : 
        Product Revision  :  MPLAB(c) Code Configurator - 4.35
        Device            :  PIC32MM0064GPL036
    The generated drivers are tested against the following:
        Compiler          :  XC32 1.43
        MPLAB             :  MPLAB X 3.61

    Copyright (c) 2013 - 2015 released Microchip Technology Inc.  All rights reserved.

    Microchip licenses to you the right to use, modify, copy and distribute
    Software only when embedded on a Microchip microcontroller or digital signal
    controller that is integrated into your product or third party product
    (pursuant to the sublicense terms in the accompanying license agreement).

    You should refer to the license agreement accompanying this Software for
    additional information regarding your rights and obligations.

    SOFTWARE AND DOCUMENTATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,
    EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION, ANY WARRANTY OF
    MERCHANTABILITY, TITLE, NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR PURPOSE.
    IN NO EVENT SHALL MICROCHIP OR ITS LICENSORS BE LIABLE OR OBLIGATED UNDER
    CONTRACT, NEGLIGENCE, STRICT LIABILITY, CONTRIBUTION, BREACH OF WARRANTY, OR
    OTHER LEGAL EQUITABLE THEORY ANY DIRECT OR INDIRECT DAMAGES OR EXPENSES
    INCLUDING BUT NOT LIMITED TO ANY INCIDENTAL, SPECIAL, INDIRECT, PUNITIVE OR
    CONSEQUENTIAL DAMAGES, LOST PROFITS OR LOST DATA, COST OF PROCUREMENT OF
    SUBSTITUTE GOODS, TECHNOLOGY, SERVICES, OR ANY CLAIMS BY THIRD PARTIES
    (INCLUDING BUT NOT LIMITED TO ANY DEFENSE THEREOF), OR OTHER SIMILAR COSTS.

*/


/**
    Section: Includes
*/
#include <xc.h>
#include "pin_manager.h"
#include "mcc.h"
#include "../env.h"

/**
    void PIN_MANAGER_Initialize(void)
*/
void PIN_MANAGER_Initialize(void)
{
    /****************************************************************************
     * Setting the Output Latch SFR(s)
     ***************************************************************************/
    LATA = 0x0000;
#if defined BOARD1_MOTORDRIVER
    LATB = 0x4010;
    LATC = 0x0004;
#elif defined BOARD2_COMBINATION || defined BOARD3_SEPARATE || defined BOARD4
    LATB = 0x4800;
    LATC = 0x0000;
#endif
    /****************************************************************************
     * Setting the GPIO Direction SFR(s)
     ***************************************************************************/
#if defined BOARD1_MOTORDRIVER
    TRISA = 0x000B;
    TRISB = 0xF00F;
    TRISC = 0x0003;		//	
#elif defined BOARD2_COMBINATION || defined BOARD3_SEPARATE || defined BOARD4
    TRISA = 0x0007;		// RA 012=i, 34=o	9o
    TRISB = 0xBC6F;		// RB U2TX set to input 15i 14o 13i 12i  11i 10i 9o 8o 7o6i5i4o 3210i 
    TRISC = 0x010B;		// RC 9o8i           3i2o10i
#else
#error
#endif

    /****************************************************************************
     * Setting the Weak Pull Up and Weak Pull Down SFR(s)
     ***************************************************************************/
    CNPDA = 0x0008; //  RA3 = RP4 = SDO2 or LOW
    CNPDB = 0x0000;
    CNPDC = 0x0020; //  RC9 = RP19 = SDO2 or LOW
    CNPUA = 0x0000;
#if defined BOARD1_MOTORDRIVER
    CNPUB = 0x4000;	//	RB14
#elif defined BOARD2_COMBINATION || defined BOARD3_SEPARATE || defined BOARD4
    CNPUB = 0x0800;	//	RB11
#else
#error
#endif
    CNPUC = 0x0000;
    /****************************************************************************
     * Setting the Open Drain SFR(s)
     ***************************************************************************/
    ODCA = 0x0000;
    ODCB = 0x0000;
    ODCC = 0x0000;

    /****************************************************************************
     * Setting the Analog/Digital Configuration SFR(s)
     ***************************************************************************/
#if defined BOARD1_MOTORDRIVER
    ANSELA = 0x0007;
    ANSELB = 0x300C;
#elif defined BOARD2_COMBINATION || defined BOARD3_SEPARATE || defined BOARD4
    ANSELA = 0x000B;
#ifdef USE_MONITOR_RX
    ANSELB = 0x300F; //U1RX=AN10=RB15, U1TX=RB14, AN8=RB13, AN7=RB12, RB3210=AN11 AN4 AN3 AN2
#else
    ANSELB = 0xB00F;	//for AD
#endif
#endif
    ANSELC = 0x0003;

    /****************************************************************************
     * Set the PPS
     ***************************************************************************/
    SYSTEM_RegUnlock(); // unlock PPS
    RPCONbits.IOLOCK = 0;

#if defined BOARD1_MOTORDRIVER
    RPOR1bits.RP5R = 0x0001;   //RB4->UART2:U2TX;
    RPINR9bits.U2RXR = 4;	   //RP4=RA3->UART2:U2RX;
    RPOR1bits.RP6R = 0x0006;   //RA4->SCCP2:OCM2;
    RPOR1bits.RP8R = 0x0003;   //RB9->SPI2:SDO2;
    RPOR4bits.RP20R = 0x0007;  //RA9->SCCP3:OCM3;
#elif defined BOARD2_COMBINATION || defined BOARD3_SEPARATE || defined BOARD4
    RPINR9bits.U2RXR = 0x0011;  //RB10->UART2:U2RX;
    RPOR4bits.RP18R = 0x0001;   //RB11->UART2:U2TX;
    RPOR1bits.RP5R = 0x0007;    //RB4->SCCP3:OCM3;
    RPOR1bits.RP6R = 0x0006;    //RA4->SCCP2:OCM2;
    RPOR4bits.RP19R = 0x0003;   //RC9->SPI2:SDO2;
    RPOR4bits.RP20R = 0x0006;   //RA9->SCCP2:OCM2;
    RPOR2bits.RP11R = 0x0007;   //RB7->SCCP3:OCM3;
#else
#error
#endif

#if defined BOARD1_MOTORDRIVER
    RPCONbits.IOLOCK = 1; // lock   PPS
#elif defined BOARD2_COMBINATION || defined BOARD3_SEPARATE || defined BOARD4
	//	BOARD2 and BOARD3 need to change PPS to change PWM out pin.
#else
#error
#endif	
    SYSTEM_RegLock(); 
}
