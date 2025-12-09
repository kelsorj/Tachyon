/*****************************************************************************
 *
 * Microchip CANopen Stack (Main Entry)
 *
 *****************************************************************************
 * FileName:        main.C
 * Dependencies:    
 * Processor:       PIC18F with CAN
 * Compiler:       	C18 02.30.00 or higher
 * Linker:          MPLINK 03.70.00 or higher
 * Company:         Microchip Technology Incorporated
 *
 * Software License Agreement
 *
 * The software supplied herewith by Microchip Technology Incorporated
 * (the "Company") is intended and supplied to you, the Company's
 * customer, for use solely and exclusively with products manufactured
 * by the Company. 
 *
 * The software is owned by the Company and/or its supplier, and is 
 * protected under applicable copyright laws. All rights are reserved. 
 * Any use in violation of the foregoing restrictions may subject the 
 * user to criminal sanctions under applicable laws, as well as to 
 * civil liability for the breach of the terms and conditions of this 
 * license.
 *
 * THIS SOFTWARE IS PROVIDED IN AN "AS IS" CONDITION. NO WARRANTIES, 
 * WHETHER EXPRESS, IMPLIED OR STATUTORY, INCLUDING, BUT NOT LIMITED 
 * TO, IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
 * PARTICULAR PURPOSE APPLY TO THIS SOFTWARE. THE COMPANY SHALL NOT, 
 * IN ANY CIRCUMSTANCES, BE LIABLE FOR SPECIAL, INCIDENTAL OR 
 * CONSEQUENTIAL DAMAGES, FOR ANY REASON WHATSOEVER.
 *
 *
 * This is the main entry into the demonstration. In this file some startup
 * and running conditions are demonstrated.
 *
 *
 * Author               Date        Comment
 *~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * Ross Fosler			11/13/03	...	
 * Giles Biddison		9/14/11     Application based on Micrchip's CANOpen skeleton
 *****************************************************************************/

#include	"CO_MAIN.H"
#include	"Timer.H"
#include	"DemoObj.h"

// TIMING IS DEFINED IN Timer.C
// CAN baud rates in CO_DEFS.DEF
// UART baud rates in eusart.c

#pragma config XINST = OFF
#pragma config SOSCSEL = DIG   // digital IO on RC0/1
#pragma config FOSC = EC3	// EC3: Use external high power clock, don't output it on clock pins
//#pragma config FOSC = INTIO2	// INTIO2 - internal osc, no output	// EC3: Use external high power clock, don't output it on clock pins
#pragma config MCLRE = OFF		// Don't require 5v on MCLRE to avoid reset
#pragma config WDTEN = OFF		// Don't reset every 2 minutes
#pragma config PLLCFG = ON    // PLL CONFIG IS IGNORED when using internal osc

//#pragma config CANMX = PORTC

#pragma config CP0 = ON // code protect
#pragma config CP1 = ON // code protect
#pragma config CP2 = ON // code protect
#pragma config CP3 = ON // code protect
#pragma config CPB = ON // code protect
#pragma config CPD = ON // code protect

unsigned char GetNodeId(void);


/*********************************************************************
 * Function:        void main(void)
 *
 * PreCondition:    none
 *
 * Input:       	none
 *                  
 * Output:         	none  
 *
 * Side Effects:    none
 *
 * Overview:        Main entry into the application.
 *
 * Note:          	The following is simply a demonstration of 
 *					initialization and running the CANopen stack. 
 ********************************************************************/
void main(void)
{	
	unsigned char nodeid = 0x00;

	// Perform any application specific initialization
//	OSCCON = 0x70;	// Set Internal Osc @ 16 MHz -- not used in EC3 mode
	OSCTUNE = 0x40; // Turn on PLL, so FOSC is 4x16 = 64 MHz
	ANCON0 = 0x00;  // configure all analog or digital pins as digital
	ANCON1 = 0x00;  // configure all analog or digital pins as digital
	TRISA = 0xFF;   // Port A is all input, used for NODE ID0-3, ID5-6 (except RB7, which is clock in if we use external clock)
	//          -- 
	// --> NOTE -- don't set CAN TX direction here without SETTING CAN TX BIT HIGH (1) -- otherwise you risk pulling TX low until this chip boots the CAN driver, which kills the bus
	//          -- no need to set CAN TX / RX direction here, they will be set by the CAN driver - let it handle the pins
	//          -- 
	TRISB = 0xFE;   // Port B - RB0 is LED output,  RB1 is NODE ID4 (input), RB2 is CANTX (output), RB3 is CANRX (input), the rest are NC or PGC & PGD bits 
    TRISC = 0xBF;   // PORT X - RC0-2 is input for socket address.  RC6-7 is serial Tx (out) & Rx (in)
	LATBbits.LATB0 = 1;		// clear LED

	nodeid = GetNodeId();



	TimerInit();				// Init my timer

	mSYNC_SetCOBID(0x1000);		// Set the SYNC COB ID (MCHP format)
	mCO_SetNodeID(nodeid);		// Set the Node ID

//	mCO_SetBaud(0x00);			// Set the baudrate (0x00 = 500Kbps @ FOSC==25MHz
//	mCO_SetBaud(0x01);			// Set the baudrate (0x01 = 500Kbps @ FOSC==16MHz
//	mCO_SetBaud(0x02);			// Set the baudrate (0x02 = 500Kbps @ FOSC==8MHz
//	mCO_SetBaud(0x03);			// Set the baudrate (0x03 = 500Kbps @ FOSC==64MHz
	mCO_SetBaud(0x04);			// Set the baudrate (0x03 = 500Kbps @ FOSC==64MHz with better TQ allocation
	
	mNMTE_SetHeartBeat(0x00);	// Set the initial heartbeat
	mNMTE_SetGuardTime(0x01);	// Set the initial guard time (how long to wait after receiving a guard request before responding - flood protection?)
	mNMTE_SetLifeFactor(0x01);	// Set the initial life time (multiplied by guard time to set clock period for response to guard message)

	mCO_InitAll();				// Initialize CANopen to run, bootup will be sent

	DemoInit();					// Initialize my demo

	while(1)
	{
		// Process CANopen events
		mCO_ProcessAllEvents();		
		
		// Process application specific functions
		DemoProcessEvents();		
		
		// 10ms timer events
		if (TimerIsOverflowEvent()) 
		{
			// Process timer related events
			mCO_ProcessAllTimeEvents();	
			DemoProcessTimerOverflow();
		}
	}
}

/*
 * FUNCTION GetNodeId
 *    Reads hardware node address
 *
 * Configurable Address:
 *  b6   b5   b4   b3   b2   b1   b0
 *  RA6  RA5  RB1  RA3  RA2  RA1  RA0
 *
 * Add socket address with wrap-around:
 *
 *  b2   b1   b0
 *  RC2  RC1  RC0
 *
 */
unsigned char GetNodeId(void)
{
	unsigned char base;
	unsigned char socket;
	base = (PORTA & 0x6F) | (PORTBbits.RB1 << 4);
	socket = PORTC & 0x07;

	// base can not be > 0x78, since our last node is base + 0x7, and we don't want addresses > 0x7f
	if( base > 0x78)
		base = 0x78;

    return base + socket;
}
