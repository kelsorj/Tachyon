#include "flash.h"

//***********************************************************************
//* FILE        : flashprg.c
//*
//* DESCRIPTION : Rutines for reprogramming of the internal Flash-EPROM
//*
//***********************************************************************


//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------

int Flash_Reprogram (far const byte * flash_data_ptr, byte * new_data_ptr, word no_of_bytes);


//--------------------------------------------------------------
// LOCAL functions:
//--------------------------------------------------------------

void FlashSector_Reprogram (far byte * flash_ptr, byte * buf_ptr);	// Func to be run in RAM


//--------------------------------------------------------------
// LOCAL Data:
//--------------------------------------------------------------

byte Ram_Func_Area[RAMCODE_SIZE];	// Allocate room for code running in RAM!
#pragma locate (Ram_Func_Area = 0x08000)

byte Flash_Sector_Buf[SECTOR_SIZE];	// Ram buffer for a sector in Atmel Flash



/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  Flash_Reprogram ()				*/
/*								*/
/*--------------------------------------------------------------*/
/*								*/
/*    flash_data_ptr:	Pointer to adress in FLASH 		*/
/*			(to be reprogrammed)			*/
/*			(FLASH_DATA_START - FLASH_DATA_END)	*/
/*								*/
/*    new_data_ptr:	Pointer to adress in RAM 		*/
/*			(buffer of new data to be programmed)	*/
/*								*/
/*    no_of_bytes:	Number of bytes to be programmed	*/
/*			(1 - 64k)				*/
/*								*/
/*--------------------------------------------------------------*/
/*    Return value:	0 if programming OK.			*/
/*			1 if programming failed.		*/
/*								*/
/*--------------------------------------------------------------*/

int Flash_Reprogram (far const byte * flash_data_ptr, byte * new_data_ptr, word no_of_bytes)
{

	unsigned long flash_sector_start_adr;	// Adress calculations
	far byte *flash_ptr;			// Pointer to FLASH

	word flash_data_offset;	// Offset from sector-start to new-code-start.
	word flash_cnt = 0;	// Counts flash adresses
	word data_cnt = 0;	// Counts new data
	word t;			// Counts bytes in a sector
	int error;		// Return value, program error.

	//--------------------------------------

	far const byte * rom_ptr;	// Pointer to far  ROM code
	byte * ram_ptr;	// Pointer to near RAM code

	void (*FlashFunc_ptr)(far byte *, byte *);	// Declare pointer to function.
	void (*RamFunc_ptr)  (far byte *, byte *);	// Declare pointer to function.


	//---------------------------------------------------
	// Copy reprogramming function from flash to RAM:
	//---------------------------------------------------

	FlashFunc_ptr = FlashSector_Reprogram;			// Initialize pointer1 to function in FLASH.
	RamFunc_ptr = (void (*)(far byte *, byte *)) Ram_Func_Area;	// Initialize pointer2 to function in RAM.

	rom_ptr =  (far const byte *) FlashFunc_ptr;	// Byte-Pointer to function (in flash).
	ram_ptr =  Ram_Func_Area;			// Byte-Pointer to allocated area in RAM.

	for (t=0; t < RAMCODE_SIZE; t++)	// Unknown size of function, copy all RAMCODE_SIZE bytes!
	{
		*(ram_ptr+t) = *(rom_ptr+t);
	}

	//---------------------------------------------------


	//--------------------------------------
	// Calculate flash adresses:
	//--------------------------------------

	flash_sector_start_adr = (unsigned long)flash_data_ptr - ((unsigned long)flash_data_ptr % SECTOR_SIZE);		// First FLASH position
	flash_ptr = (far byte *) flash_sector_start_adr;	// Pointer to flash sector.

	flash_data_offset = (word)((unsigned long)flash_data_ptr - (unsigned long)flash_sector_start_adr);


	//----------------------------------------------------
	// REPROGRAM ALL THE SECTORS INVOLVED:
	//----------------------------------------------------

	while (data_cnt < no_of_bytes)
	{

		//---------------------------------------------
		// Load sector-buffer with new/old flashdata:
		//---------------------------------------------

		for (t=0; t<SECTOR_SIZE; t++)
		{
			if ((flash_cnt >= flash_data_offset) && (data_cnt < no_of_bytes))	// New data to load?
			{
				Flash_Sector_Buf[t] = *(new_data_ptr + data_cnt);	// New data to buffer.
				data_cnt++;
			}
			else
			{
				Flash_Sector_Buf[t] = *(flash_ptr + t);		// Old data to buffer.
			}
			flash_cnt++;
		}

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
		//
		// NOTE: Only sectors in the adresses-range:
		//       FLASH_DATA_START - FLASH_DATA_END
		//       will programmed!
		//---------------------------------------------

		if ((unsigned long)flash_ptr >= FLASH_DATA_START)
			if ((unsigned long)flash_ptr <= (FLASH_DATA_END+1L - SECTOR_SIZE))
			{
				(*RamFunc_ptr) (flash_ptr, &Flash_Sector_Buf[0]);	// Call the function pointed to by RamFunc_ptr.
			}

			//---------------------------------------------
			// Point to the next sector.
			//---------------------------------------------

			flash_ptr += SECTOR_SIZE;	// Point to next sector.

	}	// End while (data_cnt < no_of_bytes)


	//---------------------------------------------
	// Check the new data in FLASH:
	//---------------------------------------------

	error = 0;
	for (t=0; t < no_of_bytes; t++)
	{
		if (*(flash_data_ptr + t) != *(new_data_ptr + t))
		{
			t = no_of_bytes;       // Break
			error = 1;		// Set error flag
		}
	}
	return (error);	// Return ok=0 or error=1.
}




/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  FlashSector_Reprogram ()				*/
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


void FlashSector_Reprogram (far byte * flash_ptr, byte * buf_ptr)
{
	byte toggle0;		// Check toggle bit in flash
	byte toggle1;		// Check toggle bit in flash
	word t;		// counter for adress offset

	far byte * ptr;

	//-------------------------------------------
	// Only if Flash-adress is within leagal range:
	//-------------------------------------------

	if ((unsigned long)flash_ptr >= FLASH_DATA_START)
		if ((unsigned long)flash_ptr <= (FLASH_DATA_END+1L - SECTOR_SIZE))
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
