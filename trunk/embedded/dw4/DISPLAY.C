#include "globdata.h"

#include <stdarg.h>	// Variable argument list.


//***********************************************************************
//* FILE        : display.c
//*
//* DESCRIPTION : display (4 lines x 20 char LCD-display) rutines
//*
//***********************************************************************

void lcd_init (void);
void lcd_adr (byte adr);
void lcd_write (byte regsel, byte LCD_byte);
void lcd_puts (byte adr, const char *str);
void lcd_putc (byte adr, byte data);
void lcd_clrl (byte adr);

//**********************************************************/

extern void delay_ms (word time);

//-----------------------
// LCD-adresses:
//-----------------------
extern byte LCD_Inst_Out;		// Write LCD Intruction byte
extern byte LCD_Inst_In;		// Read LCD Intruction byte
extern byte LCD_Data_Out;		// Write LCD data byte
extern byte LCD_Data_In;		// Read LCD data byte
#define LCD_Busy  (LCD_Inst_In & 0x80)	// Read LCD-Busy-flag!
//-----------------------
#define LCD1 0x80	// Position = Line 1
#define LCD2 0xC0	// Position = Line 2
#define LCD3 0x94	// Position = Line 3
#define LCD4 0xD4	// Position = Line 4
//-----------------------

 static int curr_adr;



//**********************************************************
//  lcd_adr ()
//**********************************************************

void lcd_adr (byte adr)
{
   lcd_write (1, adr);
   curr_adr = adr;
}


//**********************************************************
//  lcd_write ()
//**********************************************************

void lcd_write (byte regsel, byte LCD_byte)
{
   register int wait = 2000;

   while (LCD_Busy && wait)	// Busy?
   {
      wait--;
   }

   if (!LCD_Busy)
   {
      if (regsel)
	 LCD_Inst_Out = LCD_byte;		// Write LCD Instruction.
      else
	 LCD_Data_Out = LCD_byte;		// Write LCD Data.
   }
}

//**********************************************************
//  lcd_puts ()
//**********************************************************
void lcd_puts (byte adr, const char *str)
{
   int i, endofline;

   if (adr)
   {
      lcd_write (1, adr);
      curr_adr = adr;
   }

   if      (curr_adr >= LCD4) 	endofline = LCD4+20;	// End of line 4!
   else if (curr_adr >= LCD2) 	endofline = LCD2+20;	// End of line 2!
   else if (curr_adr >= LCD3) 	endofline = LCD3+20;	// End of line 3!
   else				endofline = LCD1+20;	// End of line 1!


   for (i=0; *(str+i) != 0 && i<20; i++)
   {
      lcd_write (0, *(str+i));

      if (++curr_adr >= endofline)
	 break;
   }
}


//**********************************************************
//  lcd_putc ()
//**********************************************************
void lcd_putc (byte adr, byte data)
{
   if (adr)
   {
      lcd_write (1, adr);
      curr_adr = adr;
   }
   lcd_write (0, data);
   curr_adr++;
}

void lcd_clrl (byte adr)
{
  lcd_puts (adr, "                    ");
}




//**********************************************************
//  lcd_init ()
//**********************************************************
//  Routine that initiates the LCD display
//  Runs a start-up sequence
//**********************************************************

void lcd_init (void)
{
   delay_ms (16);		// min 15ms from PowerOn!

   LCD_Inst_Out = 0x30;		// Write LCD Instruction (Function Set)
   delay_ms (6);		// min 4.1ms.

   LCD_Inst_Out = 0x30;		// Write LCD Instruction.
   delay_ms (1);      		// min 100uS.

   LCD_Inst_Out = 0x30;		// Write LCD Instruction.
   delay_ms (1);      		// min 40uS.

   /* Now starts real programming of the display */
   lcd_write (1, 0x38);         // Funtion Set: (8 bits, 5x7 dots, 2 lines)
   lcd_write (1, 0x08);     	/* Set: Display off, Cursor off, Blink off */
   lcd_write (1, 0x01);     	/* Clear the display, cursor at home */
   lcd_write (1, 0x06);     	/* Entry Mode: Increment mode */
   lcd_write (1, 0x0C);     	/* Set Display on, Cursor off, Blink off */

//   load_cgram(); /* Load special Bit pattern characters into CGRAM */

}     /* END of routine lcd_init */




