#include  <stdio.h>
#include  <stdlib.h>

#include "globdata.h"
#include "comm.h"
#include "motor.h"
#include "keybrd.h"
#include "display.h"

//***********************************************************************
//* FILE        : comm1.c
//*
//* DESCRIPTION : Div. rutines regarding external commands (RS-232).
//*
//***********************************************************************

//--------------------------------------------------------------
// Functions in COMM1.C:
//--------------------------------------------------------------
void ClrAllFlashPrograms (void);
void Send_ParameterBlock (void);
int  Receive_ParameterBlock (void);
int  Upload_All_SubProgram_Blocks (char Comand);
int  Download_SubProgram_Block (void);
void Send_Device_Code (void);
void Send_Device_Code2 (void);
void Send_SerialNo (void);
void Send_FirmVer (void);
void Send_ProgName (void);
void Send_ProgDate (void);

//-----------------------------------------------------------------------
//----- EXTERNAL FUNCTIONS: ---------------------------------------------
//-----------------------------------------------------------------------
extern byte get_char (void);	        // Get RS-232 byte.
extern int put_char (byte tx_byte);	// Transmit RS-232 byte.
extern int put_word (word RS_word);	// Transmit RS-232 word.
extern int put_s (far char *str);	// Pointer to text-string.

extern int Flash_Reprogram (far const byte *flash_data_ptr, byte *new_data_ptr, word no_of_bytes);
extern void Load_New_Flash_Program (void);

extern void display (byte Display_Byte, byte Type);    //TESTING
extern void delay_ms(word time);
extern void stop_motor1 (void);		// Stop motor1.
extern void stop_motor2 (void);		// Stop motor1.
extern void lcd_puts (byte adr, const char *str);	// (display.c)

//-------------------------------------------------------------------

byte Flash_Data_Buf[256];	// Buffer for holding Flash Data.

//-------------------------------------------------------------------
extern int  rec_bufcnt;		// Counter for serial received data.
//-------------------------------------------------------------------



//*******************************************************************
//  FUNCTION  : ClrAllFlashPrograms
//*******************************************************************

void ClrAllFlashPrograms (void)
{
	int i;
	byte Data_Buf[256];	// Buffer for holding Flash Data.

	for (i=0; i<256; i++)
		Data_Buf[i] = 0;	// Clear buffer.

	for (i=0; i <MAX_SUBPROGRAMS; i++)	// Clear all flash subprograms.
	{

		if (Program[i].ProgName[0] != 0)	// Program to be reset?
		{
			if (Flash_Reprogram ((far const byte *)&Program[i].ProgNo, (byte *)&Data_Buf[0], FLASH_DATA_BUF_SIZE) != 0)
			{
				//	 set_STOP_LED;		//Flash the red STOP_LED if error
				//LATER call error function
			}
		}
	}

}



//*******************************************************************
//  FUNCTION  : Send_ParameterBlock ()
//-------------------------------------------------------------------
//              Send 256 bytes + checksum
//*******************************************************************

void Send_ParameterBlock (void)
{
	byte chk_sum = 0;
	byte *Param_Ptr;
	byte Param_Data;
	int i, t;

	//---------------------------------------------
	//  Upload Parameter Block (256 bytes):
	//---------------------------------------------

	Param_Ptr = (byte *)Param[0].SerialNoTxt;
	chk_sum = 0;
	for (i=0; i<256; i++)
	{
		Param_Data = *(Param_Ptr+i);
		chk_sum += Param_Data;
		put_char (Param_Data);		// Send out 256 bytes.
	}
	chk_sum = ~(chk_sum);
	chk_sum += 1;
	put_char (chk_sum);		// Send out check_sum.

}



//*******************************************************************
//  FUNCTION  : Receive_ParameterBlock ()
//-------------------------------------------------------------------
//              Get 256 bytes + checksum
//*******************************************************************

int Receive_ParameterBlock (void)
{
	byte *Param_Ptr;
	byte chk_sum = 0xAA;
	int Prog_No = 100;
	int Ok_Flag;
	int i;

	//  display (0xCC, BCD);	// Ok: Display (--).

	//--------------------------------------------------------
	//  Receive Parameter Block (256 bytes + 1 byte checksum):
	//--------------------------------------------------------

	RS_Timer_1mS = 0;
	while (rec_bufcnt < (256+1) && RS_Timer_1mS < 3000)      // if a full block in serial input buffer
	{}

	if (rec_bufcnt >= (256+1))      // if a full block in serial input buffer
	{
		chk_sum = 0;				// Clear checksum.
		for (i=0; i<256; i++)
		{
			Flash_Data_Buf[i] = get_char();
			chk_sum += Flash_Data_Buf[i];	// sum of 256 bytes in buffer
		}
		chk_sum += get_char();			// + checksum

		//------------------
		if (chk_sum == 0)	// OK?
		{
			Ok_Flag = 1;
			Param_Ptr = (byte *)Param[0].SerialNoTxt;

			for (i=0; i<256; i++)		// Copy data to Parameter-block!
				*(Param_Ptr+i) = Flash_Data_Buf[i];

			put_char(ACK);	 //send OK-ACK back to PC.
		}
		else
		{
			Ok_Flag = 0;
			//	   display (chk_sum, HEX);	// Error!
			while (RS_Timer_1mS < 2000)
			{}
		}
	}

	return (Ok_Flag);
}





//*******************************************************************
//  FUNCTION  : Upload_All_SubProgram_Blocks ()
//-------------------------------------------------------------------
//              Send Upload-Command and one block,
//              and wait for ACK before the next block.
//              End tramission with the Z-command.
//*******************************************************************

int Upload_All_SubProgram_Blocks (char Comand)
{
	byte chk_sum = 0;
	far byte *Flash_Ptr;
	byte Flash_Data;
	int i, t;
	int Ok_Flag = 1;

	//---------------------------------------------
	//  Upload Block 0 first (parameter block):
	//---------------------------------------------

	put_char (ESC);		// Send out command.
	put_char (SLASH);		// Send out command.
	put_char (Comand);		// Send out command.
	put_char (';');		// Send out command.

	Flash_Ptr = (far byte *)&ProgInfo[0].ProgNo;
	chk_sum = 0;
	for (i=0; i<256; i++)
	{
		Flash_Data = *(Flash_Ptr+i);
		chk_sum += Flash_Data;
		put_char (Flash_Data);		// Send out 256 bytes.
	}
	chk_sum = ~(chk_sum);
	chk_sum += 1;
	put_char (chk_sum);		// Send out check_sum.

	// Wait for ACK from PC:
	RS_Timer_1mS = 0;
	while (rec_bufcnt < 1 && RS_Timer_1mS < 3000)	// Wait for ACK (max 3 sec).
	{}
	if (rec_bufcnt)      // if ACK
	{
		Flash_Data = get_char();
		if (Flash_Data == ACK)
			Ok_Flag = 1;
		else
			Ok_Flag = 0;
	}
	else
		Ok_Flag = 0;


	//---------------------------------------------
	//  Upload SubProgram Blocks (1-99);
	//---------------------------------------------

	for (t=0; t <MAX_SUBPROGRAMS; t++)	// Search trough all flash subprograms.
	{

		if (Program[t].ProgName[0] != 0 && Ok_Flag)	// Program to be uploaded?
		{

			put_char (ESC);		// Send out command.
			put_char (SLASH);		// Send out command.
			put_char ('B');		// Send out command.
			put_char (';');		// Send out command.

			Flash_Ptr = (far byte *)&Program[t].ProgNo;
			chk_sum = 0;
			for (i=0; i<256; i++)
			{
				Flash_Data = *(Flash_Ptr+i);
				chk_sum += Flash_Data;
				put_char (Flash_Data);		// Send out 256 bytes.
			}
			chk_sum = ~(chk_sum);
			chk_sum += 1;
			put_char (chk_sum);		// Send out check_sum.

			// Wait for ACK from PC:
			RS_Timer_1mS = 0;
			while (rec_bufcnt < 1 && RS_Timer_1mS < 3000)	// Wait for ACK (max 3 sec).
			{}
			if (rec_bufcnt)      // if ACK
			{
				Flash_Data = get_char();
				if (Flash_Data == ACK)
					Ok_Flag = 1;
				else
					Ok_Flag = 0;
			}
			else
				Ok_Flag = 0;
		}
	}

	//---------------------------------------------
	//  All used Blocks are uploaded, END:
	//---------------------------------------------

	put_char (ESC);		// Send out command.
	put_char (SLASH);		// Send out command.
	put_char ('Z');		// Send out command.
	put_char (';');		// Send out command.


	return (Ok_Flag);
}



//*******************************************************************
//  FUNCTION  : Download_SubProgram_Block ()
//*******************************************************************

int Download_SubProgram_Block (void)
{
	int i;
	byte chk_sum = 0xAA;
	byte Prog_No = 100;
	int Ok_Flag = 0;
	char LcdTxt[40];

	//-- Wait for 256 bytes binary Sub program block + 1 byte checksum:

	RS_Timer_1mS = 0;
	while (rec_bufcnt < (256+1) && RS_Timer_1mS < 3000)      // if a full block in serial input buffer
	{}

	if (rec_bufcnt >= (256+1))      // if a full block in serial input buffer
	{
		chk_sum = 0;				// Clear checksum.
		for (i=0; i<256; i++)
		{
			Flash_Data_Buf[i] = get_char();
			chk_sum += Flash_Data_Buf[i];	// sum of 256 bytes in buffer
		}
		chk_sum += get_char();			// + checksum

		//-----------------------------------
		if (chk_sum != 0)	// Error?
		{
			//	    display (chk_sum, HEX);
			RS_Timer_1mS = 0;
			while (RS_Timer_1mS < 2000)
			{}
		}
		//	else
		//	    display (0xCC, BCD);	// Ok: Display (--).

		//-----------------------------------

		if (chk_sum == 0)	// OK?
		{
			Prog_No = Flash_Data_Buf[0];

			if (Prog_No == 0)	// Parameter block!
			{
				ClrAllFlashPrograms();

				if (Flash_Reprogram ((far const byte *)&ProgInfo[0].ProgNo, (byte *)&Flash_Data_Buf, FLASH_DATA_BUF_SIZE) == 0)
				{
					Ok_Flag = 1;
					put_char(ACK);	 //send OK-ACK back to PC.
				}
				else
				{
					//		set_STOP_LED;		//Flash the red STOP_LED if error
					//LATER call error function
				}
			}
			else if (Prog_No >= 1 && Prog_No <= 99)	// Subprogram blocks!
			{

				sprintf (LcdTxt, "- REPROGRAMMING:%2d -",  Prog_No);
				lcd_puts (LCD2, "--------------------");
				lcd_puts (LCD3, LcdTxt);
				lcd_puts (LCD4, "--------------------");

				if (Flash_Reprogram ((far const byte *)&Program[Prog_No-1].ProgNo, (byte *)&Flash_Data_Buf, FLASH_DATA_BUF_SIZE) == 0)
				{
					Ok_Flag = 1;
					put_char(ACK);	 //send OK-ACK back to PC.
				}
				else
				{
					//		set_STOP_LED;		//Flash the red STOP_LED if error
					//LATER call error function
				}
			}
		}
	}
	return (Ok_Flag);
}



//*******************************************************************
//  FUNCTION  : Send_Device_Code ()
//*******************************************************************

void Send_Device_Code (void)
{
	put_char (DevCode1);		// 10=ODIN, 20=Embla1, 21=Embla2
	put_char (DevCode2);		// 10=Washer, 20=Dispencer, 21=Robot
	put_char (VerCode);		// 18=Firmware V1.8
}

void Send_Device_Code2 (void)	// => Params located in 0x0FC0000, UserProgs located in 0xFD0000!
{
	put_char (0xA5);	// A5
	put_char (0xAA);	// AA
	put_char (0xA5);	// A5
}

void Send_SerialNo (void)
{
	put_s ((far char *)Param[0].SerialNoTxt);	// Text-string
	put_char ('\0');	// Null termination
}

void Send_FirmVer (void)
{
	put_s ((far char *)FirmwareVerTxt);	// Text-string
	put_char ('\0');	// Null termination
}


void Send_ProgName (void)
{
	put_s ((far char *)ProgInfo[0].FileName);	// Text-string
	put_char ('\0');	// Null termination
}


void Send_ProgDate (void)
{
	put_char (ProgInfo[0].FileDate[0]);	// (8 bytes)
	put_char (ProgInfo[0].FileDate[1]);	// (8 bytes)
	put_char (ProgInfo[0].FileDate[2]);	// (8 bytes)
	put_char (ProgInfo[0].FileDate[3]);	// (8 bytes)
	put_char (ProgInfo[0].FileDate[4]);	// (8 bytes)
	put_char (ProgInfo[0].FileDate[5]);	// (8 bytes)
	put_char (ProgInfo[0].FileDate[6]);	// (8 bytes)
	put_char (ProgInfo[0].FileDate[7]);	// (8 bytes)
}
