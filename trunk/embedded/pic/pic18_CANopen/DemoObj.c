/*****************************************************************************
 *
 * Microchip CANopen Stack (Demonstration Object)
 *
 *****************************************************************************
 * FileName:        DemoObj.C
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
 * 
 * 
 *
 *
 * Author               Date        Comment
 *~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * Ross Fosler			11/13/03	...	
 * Giles Biddison		9/14/11     Application based on Microchip's CANOpen skeleton
 *****************************************************************************/


#include	"CO_MAIN.H"
#include	"eusart.h"


#define	RTR_DIS	bytes.B1.bits.b2
#define STD_DIS	bytes.B1.bits.b3
#define PDO_DIS	bytes.B1.bits.b4

// These are mapping constants for TPDO1 
// starting at 0x1A00 in the dictionary
rom unsigned long uTPDO1Map = 0x60000108;
rom unsigned long uRPDO1Map = 0x62000108;
rom unsigned long uPDO1Dummy = 0x00000008;

unsigned char uIOinFilter;					// 0x6003 filter
unsigned char uIOinPolarity;				// 0x6002 polarity
unsigned char uIOinIntChange;				// 0x6006 interrupt on change
unsigned char uIOinIntRise;					// 0x6007 interrupt on positive edge
unsigned char uIOinIntFall;					// 0x6008 interrupt on negative edge
unsigned char uIOinIntEnable;				// 0x6005 enable interrupts

unsigned char uIOinDigiInOld;				// 

// Static data refered to by the dictionary
rom unsigned char rMaxIndex1 = 1;
rom unsigned char rMaxIndex2 = 8;
rom unsigned char uDemoTPDO1Len = 8;

unsigned char uLocalXmtBuffer[8];			// Local buffer for TPDO1
unsigned char uLocalRcvBuffer[8];			// local buffer fot RPDO1

UNSIGNED8 uDemoState; 					// Bits used to control various states
unsigned char uDemoSyncCount;			// Counter for synchronous types
unsigned char uDemoSyncSet;				// Internal TPDO type control

#define false 0
#define true 1

#define RING_BUFFER_SIZE 64
unsigned char _tx_ring_buffer[RING_BUFFER_SIZE];
int _tx_ring_read;
int _tx_ring_write;

unsigned char _rx_ring_buffer[RING_BUFFER_SIZE];
int _rx_ring_read;
int _rx_ring_write;

/*********************************************************************
 * Function:        void DemoInit(void)
 *
 * PreCondition:    none
 *
 * Input:       	none
 *                  
 * Output:         	none  
 *
 * Side Effects:    none
 *
 * Overview:        This is the initialization to the demonstration
 *					object.
 *
 * Note:          	
 ********************************************************************/
void DemoInit(void)
{
	LATBbits.LATB0 = 1;					// LED 1 ON at boot

	InitializeEUSART(115200);

	_tx_ring_read = 0;
	_tx_ring_write = 0;
	_rx_ring_read = 0;
	_rx_ring_write = 0;

	uDemoSyncSet = 255;

	uIOinFilter = 0;
	uIOinPolarity = 0;
	uIOinIntChange = 1;
	uIOinIntRise = 0;
	uIOinIntFall = 0;
	uIOinIntEnable = 1;
	uIOinDigiInOld = 0;

	uLocalRcvBuffer[0] = uLocalXmtBuffer[0] = 0;
	uLocalRcvBuffer[1] = uLocalXmtBuffer[1] = 0;
	uLocalRcvBuffer[2] = uLocalXmtBuffer[2] = 0;
	uLocalRcvBuffer[3] = uLocalXmtBuffer[3] = 0;
	uLocalRcvBuffer[4] = uLocalXmtBuffer[4] = 0;
	uLocalRcvBuffer[5] = uLocalXmtBuffer[5] = 0;
	uLocalRcvBuffer[6] = uLocalXmtBuffer[6] = 0;
	uLocalRcvBuffer[7] = uLocalXmtBuffer[7] = 0;


	// Convert to MCHP
	mTOOLS_CO2MCHP(mCOMM_GetNodeID().byte + 0xC0000180L);
	
	// Store the COB
	mTPDOSetCOB(1, mTOOLS_GetCOBID());

	// Convert to MCHP
	mTOOLS_CO2MCHP(mCOMM_GetNodeID().byte + 0xC0000200L);
	
	// Store the COB
	mRPDOSetCOB(1, mTOOLS_GetCOBID());
	
	// Set the pointer to the buffers
	mTPDOSetTxPtr(1, (unsigned char *)(&uLocalXmtBuffer[0]));
	
	// Set the pointer to the buffers
	mRPDOSetRxPtr(1, (unsigned char *)(&uLocalRcvBuffer[0]));

	// Set the length
	mTPDOSetLen(1, uDemoTPDO1Len);

	mRPDOOpen(1);
	mTPDOOpen(1);
}




/*********************************************************************
 * Function:        void CO_COMMSyncEvent(void)
 *
 * PreCondition:    none
 *
 * Input:       	none
 *                  
 * Output:         	none  
 *
 * Side Effects:    none
 *
 * Overview:        This is a simple demonstration of a SYNC event 
 *					handling function.
 *
 * Note:          	
 ********************************************************************/
void CO_COMMSyncEvent(void)
{
}

/*********************************************************************
 * Function:        void DemoProcessTimerOverflow(void)
 *
 * PreCondition:    none
 *
 * Input:       	none
 *                  
 * Output:         	none  
 *
 * Side Effects:    Toggles PORTA bits 1 & 2
 *
 * Overview:        infrequent actions happening in the timer overflow loop (1ms interval)
 *
 * Note:          	
 ********************************************************************/
void DemoProcessTimerOverflow(void)
{
	static int counter = 0;	
	int interval = 1000;
	const int leds_off = 100;

	if( !COMM_STATE_OPER)
	{
		interval = 50;		// flash bit 1 at 20/2 Hz
	}
	else if (COMM_STATE_STOP)
	{
		interval = 500;		// flash bit 1 at 2/2 Hz
	}
	else
	{
		interval = 1000;
	}
	if( !(++counter%interval))
	{	
		counter = 0;
		LATBbits.LATB0 = ~LATBbits.LATB0;
		// LATBbits.LATB1 = 0; //debugging LED, not used
	}

	/*if( !(counter%leds_off))
	{
		LATBbits.LATB2 = 0; //debugging LED, not used
		LATBbits.LATB3 = 0; //debugging LED, not used
	}*/
}

/*********************************************************************
 * Function:        void DemoProcessEvents(void)
 *
 * PreCondition:    none
 *
 * Input:       	none
 *                  
 * Output:         	none  
 *
 * Side Effects:    none
 *
 *
 * Note:          	
 ********************************************************************/
void DemoProcessEvents(void)
{
	// Initial concept:
	// User will treat serial bytes as I/O
	//
	// -- send an output pdo to transmit a serial byte:
	// 200 + CAN_ID length 1, byte to transmit.
	//	
	// -- device will start up in synchronous mode, and respond to a 180 + CAN_ID RTR msg with the RX byte
	// 180 + CAN_ID length 1, data byte 
	//
	// the RX transmission should be made toggle-able, so that asynch mode is possible, but this will
	// probably be in the future.

	//#define PACING 7 // 25MHz pacing -- empirical, 26 characters rcvd from serial xmt to windows over CAN
	#define PACING 10  // 64 MHz pacing -- empirically 8 is hairy edge, 9 is reasonable, 10 is safe, used 64/25 * 7 = 18, should be approx the same pace as 25mhz solution
	static int local_pacer = 0;
	static char got_eot = 0;
	char i;
	char data;

	// If any data has been received
	if (mRPDOIsGetRdy(1))
	{
		//  read up to 8 bytes
		for( i=0; i < mRPDOGetLen(1); ++i)
		{
			_rx_ring_buffer[_rx_ring_write] = uLocalRcvBuffer[i];
			_rx_ring_write = (++_rx_ring_write) % RING_BUFFER_SIZE;
		}
		mRPDORead(1); 					// PDO read, free the driver to accept more data
		// LATBbits.LATB3 = 1; //debugging LED, not used
	}

	if(!Busy1USART() && _rx_ring_write != _rx_ring_read) // wait for any previous write to complete 
	{
		Write1USART(_rx_ring_buffer[_rx_ring_read]);
		_rx_ring_read = (++_rx_ring_read) % RING_BUFFER_SIZE;
	}

	// If characters are available in USART, put them in ring buffer
	if (DataRdy1USART())
	{	
		do{
			data = Read1USART();
			_tx_ring_buffer[_tx_ring_write] = data;
			_tx_ring_write = (++_tx_ring_write) % RING_BUFFER_SIZE;
			if( data == '}')
				++got_eot;
		}while( DataRdy1USART()); // The USART holds 2 bytes, if we don't try reading them both, we will probably overrun
		// LATBbits.LATB2 = 1; //debugging LED, not used
	}
	else if(USART1_Status.FRAME_ERROR || USART1_Status.OVERRUN_ERROR)
	{
		// clear it by toggling CREN
		RCSTAbits.CREN = 0;
		RCSTAbits.CREN = 1;

		//LATBbits.LATB1 = 1; //debugging LED, not used
		USART1_Status.FRAME_ERROR = 0;
		USART1_Status.OVERRUN_ERROR = 0;
	}	

	++local_pacer;

	// make sure we don't go out of sync w/ got_eot vs. tx_ring e.g. due to buffer overrun
	if( got_eot < 0 || (got_eot > 0 && _tx_ring_write == _tx_ring_read))
		got_eot = 0;

	if( got_eot == 0) // accumulate serial data before turning into a CAN packet
		return;

	// don't put another message on the bus until the last message transmit is complete,
	// otherwise we can interfere with ourselves and send messages out of order
    if( TXB0CONbits.TXREQ || TXB1CONbits.TXREQ || TXB2CONbits.TXREQ)
		return;

	if( local_pacer < PACING) // basically, we need to insert some space between CAN packets or we start to choke.
		return;

	local_pacer = 0;

	// if the PDO is free and we've got characters, write them out: write up to 8 chars
	if( mTPDOIsPutRdy(1) && _tx_ring_write != _tx_ring_read)
	{
		// write up to 8 bytes
		for( i=0; i < 8 && _tx_ring_write != _tx_ring_read; ++i)
		{
			data = _tx_ring_buffer[_tx_ring_read];
			_tx_ring_read = (++_tx_ring_read) % RING_BUFFER_SIZE;
			uLocalXmtBuffer[i] = data;
			if( data == '}')
			{
				--got_eot;
				++i;
				break;
			}
		}

		mTPDOSetLen(1, i);
		mTPDOWritten(1);	// Tell the stack data is loaded for transmit
	}
}

/*********************************************************************
 * Function:        void CO_COMM_RPDO1_COBIDAccessEvent(void)
 *
 * PreCondition:    none
 *
 * Input:       	none
 *                  
 * Output:         	none  
 *
 * Side Effects:    none
 *
 * Overview:        This is a simple demonstration of a RPDO COB access
 *					handling function.
 *
 * Note:          	This function is called from the dictionary.
 ********************************************************************/
void CO_COMM_RPDO1_COBIDAccessEvent(void)
{
	switch (mCO_DictGetCmd())
	{
		case DICT_OBJ_READ: 	// Read the object
			// Translate MCHP COB to CANopen COB
			mTOOLS_MCHP2CO(mRPDOGetCOB(1));
			// Return the COBID
			*(unsigned long *)(uDict.obj->pReqBuf) = mTOOLS_GetCOBID();
			break;

		case DICT_OBJ_WRITE: 	// Write the object
			// Translate the COB to MCHP format
			mTOOLS_CO2MCHP(*(unsigned long *)(uDict.obj->pReqBuf));
			
			// If the request is to stop the PDO
			if ((*(UNSIGNED32 *)(&mTOOLS_GetCOBID())).PDO_DIS)
			{
				// And if the COB received matches the stored COB and type then close
				if (!((mTOOLS_GetCOBID() ^ mRPDOGetCOB(1)) & 0xFFFFEFFFL))
				{
					// but only close if the PDO endpoint was open
					if (mRPDOIsOpen(1)) {mRPDOClose(1);}
		
					// Indicate to the local object that this PDO is disabled
					(*(UNSIGNED32 *)(&mRPDOGetCOB(1))).PDO_DIS = 1;
				}
				else {mCO_DictSetRet(E_PARAM_RANGE);} //error
			}

			// Else if the TPDO is not open then start the TPDO
			else
			{
				// And if the COB received matches the stored COB and type then open
				if (!((mTOOLS_GetCOBID() ^ mRPDOGetCOB(1)) & 0xFFFFEFFFL))
				{
					// but only open if the PDO endpoint was closed
					if (!mRPDOIsOpen(1)) {mRPDOOpen(1);}
						
					// Indicate to the local object that this PDO is enabled
					(*(UNSIGNED32 *)(&mRPDOGetCOB(1))).PDO_DIS = 0;
				}
				else {mCO_DictSetRet(E_PARAM_RANGE);} //error
			}
			break;
	}	
}

/*********************************************************************
 * Function:        void CO_COMM_TPDO1_COBIDAccessEvent(void)
 *
 * PreCondition:    none
 *
 * Input:       	none
 *                  
 * Output:         	none  
 *
 * Side Effects:    none
 *
 * Overview:        This is a simple demonstration of a TPDO COB access
 *					handling function.
 *
 * Note:          	This function is called from the dictionary.
 ********************************************************************/
void CO_COMM_TPDO1_COBIDAccessEvent(void)
{
	switch (mCO_DictGetCmd())
	{
		case DICT_OBJ_READ: 	// Read the object
			// Translate MCHP COB to CANopen COB
			mTOOLS_MCHP2CO(mTPDOGetCOB(1));
			
			// Return the COBID
			*(unsigned long *)(uDict.obj->pReqBuf) = mTOOLS_GetCOBID();
			break;

		case DICT_OBJ_WRITE: 	// Write the object
			// Translate the COB to MCHP format
			mTOOLS_CO2MCHP(*(unsigned long *)(uDict.obj->pReqBuf));
			
			// If the request is to stop the PDO
			if ((*(UNSIGNED32 *)(&mTOOLS_GetCOBID())).PDO_DIS)
			{
				// And if the COB received matches the stored COB and type then close
				if (!((mTOOLS_GetCOBID() ^ mTPDOGetCOB(1)) & 0xFFFFEFFFL))
				{
					// but only close if the PDO endpoint was open
					if (mTPDOIsOpen(1)) { mTPDOClose(1); }
		
					// Indicate to the local object that this PDO is disabled
					(*(UNSIGNED32 *)(&mTPDOGetCOB(1))).PDO_DIS = 1;
				}
				else {mCO_DictSetRet(E_PARAM_RANGE);} //error
			}

			// Else if the TPDO is not open then start the TPDO
			else
			{
				// And if the COB received matches the stored COB and type then open
				if (!((mTOOLS_GetCOBID() ^ mTPDOGetCOB(1)) & 0xFFFFEFFFL))
				{
					// but only open if the PDO endpoint was closed
					if (!mTPDOIsOpen(1)) {mTPDOOpen(1);}
						
					// Indicate to the local object that this PDO is enabled
					(*(UNSIGNED32 *)(&mTPDOGetCOB(1))).PDO_DIS = 0;
				}
				else {mCO_DictSetRet(E_PARAM_RANGE);} //error
			}
			break;
	}	
}

/*********************************************************************
 * Function:        void CO_COMM_TPDO1_TypeAccessEvent(void)
 *
 * PreCondition:    none
 *
 * Input:       	none
 *                  
 * Output:         	none  
 *
 * Side Effects:    none
 *
 * Overview:        This is a simple demonstration of a TPDO type access
 *					handling function.
 *
 * Note:          	This function is called from the dictionary.
 ********************************************************************/
void CO_COMM_TPDO1_TypeAccessEvent(void)
{
	unsigned char tempType;
	
	switch (mCO_DictGetCmd())
	{
		//case DICT_OBJ_INFO:		// Get information about the object
			// The application should use this to load the 
			// structure with legth, access, and mapping.
		//	break;

		case DICT_OBJ_READ: 	// Read the object
			// Write the Type to the buffer
			*(uDict.obj->pReqBuf) = uDemoSyncSet;
			break;

		case DICT_OBJ_WRITE: 	// Write the object
			tempType = *(uDict.obj->pReqBuf);
			if ((tempType >= 0) && (tempType <= 240))
			{
				// Set the new type and resync
				uDemoSyncCount = uDemoSyncSet = tempType;
			}
			else 
			if ((tempType == 254) || (tempType == 255))
			{
				uDemoSyncSet = tempType;
			}
			else {mCO_DictSetRet(E_PARAM_RANGE);} //error
			
			break;
	}	
}

/*********************************************************************
 * Function:        void CO_PDO1LSTimerEvent(void)
 *
 * PreCondition:    none
 *
 * Input:       	none
 *                  
 * Output:         	none  
 *
 * Side Effects:    none
 *
 * Overview:        none
 *
 * Note:          	none
 ********************************************************************/
void CO_PDO1LSTimerEvent(void)
{
	
}

/*********************************************************************
 * Function:        void CO_PDO1TXFinEvent(void)
 *
 * PreCondition:    none
 *
 * Input:       	none
 *                  
 * Output:         	none  
 *
 * Side Effects:    none
 *
 * Overview:        none
 *
 * Note:          	none
 ********************************************************************/
void CO_PDO1TXFinEvent(void)
{
	
}