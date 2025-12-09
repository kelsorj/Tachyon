#include "flash.h"

//***********************************************************************
//* FILE        : new_prog.c
//*
//* DESCRIPTION : Receive user-progs from PC and store in Flash Prom.
//*
//***********************************************************************

//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------

void Load_New_Flash_Program (void);


//--------------------------------------------------------------
// LOCAL functions:
//--------------------------------------------------------------

void FlashSector_Program (far byte * flash_ptr, byte * buf_ptr);	// Func to be run in RAM
byte get_RS_byte (unsigned long time_out);


//--------------------------------------------------------------
// LOCAL Data:
//--------------------------------------------------------------

byte serial_error;
byte receive_flag;

byte Ram_Func_Space[RAMCODE_SIZE];	// Allocate room for code running in RAM!
#pragma locate (Ram_Func_Space = 0x08200)
byte Flash_Sector_Buffer[SECTOR_SIZE];	// Ram buffer for a sector in Atmel Flash



/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  get_RS_byte()					*/
/*								*/
/*--------------------------------------------------------------*/

byte get_RS_byte (register unsigned long time_out)
{
  register byte serial_status;

  serial_status = sp_status;	// Read Serial Status.

  //----------------------------------------------------
  //  Check for serial errors:
  //----------------------------------------------------

  if(checkbit(serial_status, FE_BIT))	// Framing error?
  {
     serial_error |= 0x04;
     return (0);
  }
  if(checkbit(serial_status, OE_BIT))	// Overrun error?
  {
     serial_error |= 0x02;
     return (0);
  }


  //----------------------------------------------------
  //  Wait for serial receive byte:
  //----------------------------------------------------

  while (!checkbit(serial_status, RI_BIT) && (--time_out > 0L))	// Wait for serial byte.
      serial_status = sp_status;		// Read RI-bit.


  //----------------------------------------------------
  //  Received byte, or timeout?
  //----------------------------------------------------

  if(!checkbit(serial_status, RI_BIT))	// Received byte?
  {
     serial_error |= 0x01;
     return (0);
  }

  return (sbuf_rx);	// Return serial byte.
}


/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  Load_New_Flash_Program ()			*/
/*								*/
/*--------------------------------------------------------------*/
/*								*/
/*	Receives data from PC and programs into Flash.		*/
/*								*/
/*	Data from PC is binary, sector by sector:     		*/
/*	-----------------------------------------		*/
/*	  word NumberOfBytes (  2 bytes)	     		*/
/*	  long FlashAdress   (  4 bytes)	     		*/
/*	  byte FlashData     (256 bytes)	     		*/
/*								*/
/*	Functions in this module will not be reprogrammed!	*/
/*								*/
/*--------------------------------------------------------------*/

void Load_New_Flash_Program (void)
{

//--------------------------------------

void (*FlashFunc_ptr)(far byte *, byte *);	// Declare pointer to function.
void (*RamFunc_ptr)  (far byte *, byte *);	// Declare pointer to function.

far       byte *flash_ptr;	// Pointer to FLASH
far const byte *rom_ptr;	// Pointer to far  ROM code
	  byte *ram_ptr;	// Pointer to near RAM code

//--------------------------------------

unsigned long flash_adr;	// Adress in Flash
word data_cnt = 0;		// Number of bytes
word t;				// Loop counter
byte RS_Byte1, RS_Byte2, RS_Byte3, RS_Byte4;
byte CheckSum;


//---------------------------------------------------
// Clear all interrupts!
//---------------------------------------------------

  int_mask  = 0;	// Clear all interrupts!
  int_mask1 = 0;	// Clear all interrupts!

//---------------------------------------------------
// Copy reprogramming function from flash to RAM:
//---------------------------------------------------

   FlashFunc_ptr = FlashSector_Program;			// Initialize pointer1 to function in FLASH.
   RamFunc_ptr = (void (*)(far byte *, byte *)) Ram_Func_Space;	// Initialize pointer2 to function in RAM.

   rom_ptr =  (far const byte *) FlashFunc_ptr;	// Byte-Pointer to function (in flash).
   ram_ptr =  Ram_Func_Space;			// Byte-Pointer to allocated area in RAM.

   for (t=0; t < RAMCODE_SIZE; t++)	// Unknown size of function, copy all RAMCODE_SIZE bytes!
   {
      *(ram_ptr+t) = *(rom_ptr+t);
   }


//---------------------------------------------------
// Send ACK to PC (ready to begin):
//---------------------------------------------------

  while (!(sp_status & 0x08)){}	// Transmit buffer empty?
  sbuf_tx = 0xA5;		// Send Acknowledge to PC.
  while (!(sp_status & 0x08)){}	// Transmit buffer empty?


//------------------------------------------------------
// REPROGRAM ALL THE SECTORS RECEIVED FROM PC (RS-232):
//------------------------------------------------------

  serial_error = 0;

  while (!serial_error)
  {

   //---------------------------------------------
   // Wait until 20 sec. for 1. serial byte from PC:
   // Get number of bytes to be received:
   //---------------------------------------------

     RS_Byte1 = get_RS_byte(2000000L);	// Wait max 20 sec.
     RS_Byte2 = get_RS_byte(50000L);		// Wait max 0.5 sec.
     data_cnt = ((word)RS_Byte1<<8) + (word)RS_Byte2;


   //---------------------------------------------
   // Get adress to Flash pointer:
   //---------------------------------------------

     RS_Byte1 = get_RS_byte(50000L);		// Wait max 0.5 sec.
     RS_Byte2 = get_RS_byte(50000L);		// Wait max 0.5 sec.
     RS_Byte3 = get_RS_byte(50000L);		// Wait max 0.5 sec.
     RS_Byte4 = get_RS_byte(50000L);		// Wait max 0.5 sec.
     flash_adr = ((unsigned long)RS_Byte1<<24) + ((unsigned long)RS_Byte2<<16) + ((unsigned long)RS_Byte3<<8) + (unsigned long)RS_Byte4;


   //---------------------------------------------
   // Check for errors:
   //---------------------------------------------

     if (data_cnt != SECTOR_SIZE)
	serial_error |= 0x10;

     if (flash_adr < 0x00F80000L)
	serial_error |= 0x20;


   //---------------------------------------------
   // Load sector-buffer with new flashdata:
   //---------------------------------------------

     CheckSum = 0;
     for (t=0; t < SECTOR_SIZE; t++)
     {
	if (!serial_error)
	{
	   Flash_Sector_Buffer[t] = get_RS_byte(50000L);	// Wait max 0.5 sec.
	   CheckSum += Flash_Sector_Buffer[t];
	}
     }

     if (CheckSum != get_RS_byte(50000L))	// Get checksum.
	serial_error |= 0x40;

     if (!serial_error)
     {

	//---------------------------------------------
	// Reprogram the current FLASH-SECTOR:
	//---------------------------------------------
	// The function in RAM is not known at compile-
	// time, and can not be called by name.
	//
	// The Function Call is here done by useing the
	// RamFunc_ptr (pointing to the RAM function):
	//
	// NOTE: All interrupts are turned off during
	//       sector reprogramming! (max 10 mS)
	//---------------------------------------------

	flash_ptr = (far byte *) flash_adr;

	if ((unsigned long)flash_ptr <= (PROTECTED_START-SECTOR_SIZE) ||
	    (unsigned long)flash_ptr >   PROTECTED_END)
	{
	   (*RamFunc_ptr) (flash_ptr, &Flash_Sector_Buffer[0]);	// Call the function pointed to by RamFunc_ptr.
	}
	sbuf_tx = 0xA5;			// Send Acknowledge to PC.
	while (!(sp_status & 0x08)){}	// Transmit buffer empty?
     }

  }	// End while (!serial_error)


  sbuf_tx = serial_error;	// Send errorcode to PC.
  while (!(sp_status & 0x08)){}	// Transmit buffer empty?

  t = 10000;		// 0.1 sec Timeout counter
  while (--t > 0L){}	// Delay

  asm rst;		// RESET PROSESSOR!
}




/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  FlashSector_Program ()				*/
/*								*/
/*--------------------------------------------------------------*/
/*								*/
/*	NOTE:							*/
/*								*/
/*	- This rutine must run in RAM, because the Flash	*/
/*	  is not accesable while being programmed! 		*/
/*	- No code (including interrupts) can be run from	*/
/*	  the flash area until program cycle is finished!	*/
/*								*/
/*--------------------------------------------------------------*/


void FlashSector_Program (far byte * flash_ptr, byte * buf_ptr)
{
 byte toggle0;		// Check toggle bit in flash
 byte toggle1;		// Check toggle bit in flash
 word t;		// counter for adress offset

 far byte * ptr;

//-------------------------------------------
// Only if Flash-adress is within leagal range:
//-------------------------------------------

  if ((unsigned long)flash_ptr <= (PROTECTED_START-SECTOR_SIZE) ||
      (unsigned long)flash_ptr >   PROTECTED_END)
  {

  //-------------------------------------------
  // Push INT-MASK and turn off all interrupts:
  //-------------------------------------------

     asm  PUSHA;		// Push and reset INT-MASK.

  //-------------------------------------------
  // Write new data to FLASH-SECTOR:
  //-------------------------------------------

      ptr = (far byte *) 0xF85555;
     *ptr = 0xAA;				// 3 bytes Software Protection.
      ptr = (far byte *) 0xF82AAA;
     *ptr = 0x55;				// 3 bytes Software Protection.
      ptr = (far byte *) 0xF85555;
     *ptr = 0xA0;				// 3 bytes Software Protection.

     for (t=0; t<SECTOR_SIZE; t++)
	*(flash_ptr++) = *(buf_ptr++);	// Copy buffer to flash sector.

  //-------------------------------------------
  // Wait for FLASH-write cycle to be finished:
  //-------------------------------------------

     do {
	toggle0 = *flash_ptr;
	toggle1 = *flash_ptr;
     }
     while (toggle0 != toggle1);


  //-------------------------------------------
  // Finished, Pop original INT-MASK back:
  //-------------------------------------------

     asm  POPA;		// Pop INT-MASK.
  }
}




