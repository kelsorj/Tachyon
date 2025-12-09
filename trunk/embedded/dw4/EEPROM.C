#include  "globdata.h"
#include  "display.h"


//***********************************************************************
//* FILE        : eeprom.c
//*
//* DESCRIPTION : Read/write external EEPROM data/parameters (IIC serial bus).
//*
//***********************************************************************

#define EEPROM_WR  0xAE		// EEPROM Write adress.
#define EEPROM_RD  0xAF		// EEPROM Read  adress.

//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------

int read_eeprom_param (void);
int write_eeprom_param (void);

//--------------------------------------------------------------
// LOCAL functions:
//--------------------------------------------------------------
void I2C_start (void);
void I2C_stop (void);
void I2C_SendByte (byte);

//--------------------------------------------------------------

byte NoAck;		// Akcnowledge missing from controller.

//--------------------------------------------------------------
// Macros:
//--------------------------------------------------------------
// Timing:
// 		set_SDA   -> 3,2uS
// 		clr_SDA   -> 3,2uS
//		delay_1uS => asm{NOP;NOP;NOP;NOP;}
//--------------------------------------------------------------

#define   set_SDA	(p2_reg |=   (0x01 << 6))
#define   clr_SDA	(p2_reg &= (~(0x01 << 6)))
#define   set_SCL	(p2_reg |=   (0x01 << 5))
#define   clr_SCL	(p2_reg &= (~(0x01 << 5)))
#define   check_SDA	(p2_pin &    (0x01 << 6))

//Low-High transitition (R*C = 0.5uS):
#define   delay_high	asm{NOP;NOP;}	// 0.5uS!



//--------------------------------------------------------------
// write_eeprom_param ()
//--------------------------------------------------------------

int write_eeprom_param (void)
{
  byte *ptr;
  byte DataByte;
  int i, t;

   ptr = (byte*)&Param[0];

   for (i=0; i<256; i+=16)	// Send all 255 bytes to Param.
   {
//--------------------------------------------------------------
     t = 0;
     do {		// Poll (wait for) Aknowledge!
       delay_4uS; delay_4uS; delay_4uS; delay_4uS;
       NoAck = 0;			// Clear NoAck-flag.
       I2C_start ();			// Send start-command to EEPROM.
       I2C_SendByte (EEPROM_WR);	// Send write-command to EEPROM.
     } while (NoAck && t++ < 20000);	// Poll (wait for) Aknowledge!
//--------------------------------------------------------------

     if (NoAck) goto End_Write;
     I2C_SendByte ((byte)i);		// Send address of first byte.
     if (NoAck) goto End_Write;

     I2C_SendByte (*(ptr+i+ 0));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+ 1));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+ 2));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+ 3));	// Send data to EEPROM.

     I2C_SendByte (*(ptr+i+ 4));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+ 5));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+ 6));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+ 7));	// Send data to EEPROM.

     I2C_SendByte (*(ptr+i+ 8));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+ 9));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+10));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+11));	// Send data to EEPROM.

     I2C_SendByte (*(ptr+i+12));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+13));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+14));	// Send data to EEPROM.
     I2C_SendByte (*(ptr+i+15));	// Send data to EEPROM.

     I2C_stop();		// Stop-command to EEPROM.
   }

   return (NoAck);

End_Write:
   I2C_stop();			// Stop-command to EEPROM.
   return (NoAck);
}


//--------------------------------------------------------------
// read_eeprom_param ()
//--------------------------------------------------------------

int read_eeprom_param (void)
{
  byte *ptr;
  byte DataByte;
  int t,i;

//    ptr = (byte *)Param[0].SerialNoTxt;
    ptr = (byte *)&Param[0];
    NoAck = 0;			// Clear NoAck-flag.

    I2C_start ();		// Send start-command to EEPROM.
    I2C_SendByte (EEPROM_WR);	// Send write-command to EEPROM.
    I2C_SendByte (0x00);	// Send address of first byte.

    if (NoAck) goto End_Read;

    set_SDA;		// Set data high (input).
    clr_SCL;		// Set clock low.
    delay_4uS;		// +4 uS.

//-------

   for (i=0; i<256; i++)	// Read all 256 bytes to Param.
   {
     I2C_start ();		// Send start-command to EEPROM.
     I2C_SendByte (EEPROM_RD);	// Send read-command to EEPROM.

     DataByte = 0;		// Clear byte.

     for (t=8; t>0; t--)		// Send all 8 bits to SDA.
     {
	set_SCL;			// Set clock high (for min. 4,0uS)
	delay_high;			// Low-High delay (pullup+C)
	delay_4uS;			// +2 uS.

	DataByte = (DataByte << 1);	// Shift data bits up.
	if (check_SDA) DataByte |= 0x01;
	else           DataByte &= 0xFE;

	clr_SCL;			// Set clock low (for min. 4,7uS)
	delay_4uS;			// +4 uS.
     }

     *(ptr+i) = DataByte;	// Store byte.

     set_SDA;		// Set data high (Not Acknowledge).
     delay_2uS;		// +2 uS.
     set_SCL;		// Set clock high (for min. 4,0uS)
     delay_high;	// Low-High delay (pullup+C)
     delay_3uS;		// +3 uS.

     clr_SCL;		// Set clock low (for min. 4,7uS)
     delay_3uS;		// +3 uS.
     I2C_stop();	// Stop-command to EEPROM.
   }

End_Read:
    return (NoAck);
}



/*----------------------------------------------------------------------*/
/*									*/
/*	NAVN:  Misc. functions for IIC-bus (EEPROM).			*/
/*									*/
/*----------------------------------------------------------------------*/
/*	       Note:  SDA (I2C Serial Data)  = P2 bit 6.		*/
/*	              SCL (I2C Serial Clock) = P2 bit 5.		*/
/*									*/
/*	              Timing is based on Xtal = 24 MHz			*/
/*----------------------------------------------------------------------*/

/*----------------------------------------------*/
/*	NAME: I2C_start ()			*/
/*----------------------------------------------*/
void I2C_start (void)
{
	set_SDA;		// Set data high.
	set_SCL;		// Set clock high. (for min. 4,7uS)
	delay_high;		// Low-High delay (pullup+C)
	delay_3uS;		// +3 uS.
	clr_SDA;		// Set data low    (for min. 4,0uS)
	delay_3uS;		// +3 uS
	clr_SCL;		// Set clock low.  (for min. 4,7uS)
	delay_2uS;		// +2 uS
}

/*----------------------------------------------*/
/*	NAME: I2C_stop ()			*/
/*----------------------------------------------*/

void I2C_stop (void)
{
	clr_SDA;		// Set data low (1 uS)
	set_SCL;		// Set clocke high (for min 4,0uS)
	delay_high;		// Low-High delay (pullup+C)
	delay_3uS;		// +3 uS
	set_SDA;		// Set data high.
}


/*----------------------------------------------*/
/*	NAME: I2C_SendByte ()			*/
/*----------------------------------------------*/

void I2C_SendByte (byte BCD_byte)
{
   register byte t;

   clr_SCL;			// Set clock low.

   for (t=8; t>0; t--)		// Send all 8 bits to SDA.
   {
	if (BCD_byte & 0x80) set_SDA;	// SDA = bit 7.
	else                 clr_SDA;
	set_SCL;			// Set clock high (for min. 4,0uS)
	delay_high;			// Low-High delay (pullup+C)

	BCD_byte = (BCD_byte << 1);	// Shift data bits up.
	delay_2uS;			// +2 uS.
	clr_SCL;			// Set clock low (for min. 4,7uS)
   }

   set_SDA;			// Set data high (input)
   set_SCL;			// Set clock high.
   delay_high;			// Low-High delay (pullup+C)
   delay_4uS;			// +4 uS.

   if (check_SDA) NoAck = 1;	// Read data and check if Acknowledge ok.
   clr_SCL;			// Set clock low.
}



void I2C_Timing_Test (void)
{
  int i, t;

   for (i=0; i<100; i++)	// ca. 16 sec tot.
   {
//--------------------------------------------------------------
     for (t=0; t<2000; t++)
     {
	set_SDA;		// Set data high (time = 3.2uS)
	clr_SDA;		// Set data low  (time = 3.2uS)
	delay_4uS;		// +4 uS. (time = 2uS)

	set_SDA;		// Set data high (input)
	delay_4uS;		// +2 uS.
	clr_SDA;		// Set data low (1 uS)

	set_SDA;		// Set data high (input)
	clr_SDA;		// Set data low (1 uS)
	delay_4uS;		// +4 uS.

	set_SDA;		// Set data high (input)
	delay_4uS;		// +2 uS.
	clr_SDA;		// Set data low (1 uS)

	set_SDA;		// Set data high (input)
	clr_SDA;		// Set data low (1 uS)
	delay_4uS;		// +4 uS.

	set_SDA;		// Set data high (input)
	delay_4uS;		// +2 uS.
	clr_SDA;		// Set data low (1 uS)

	set_SDA;		// Set data high (input)
	clr_SDA;		// Set data low (1 uS)
	delay_4uS;		// +4 uS.

	set_SDA;		// Set data high (input)
	delay_4uS;		// +2 uS.
	clr_SDA;		// Set data low (1 uS)

	set_SDA;		// Set data high (input)
	clr_SDA;		// Set data low (1 uS)
	delay_4uS;		// +4 uS.

	set_SDA;		// Set data high (input)
	delay_4uS;		// +2 uS.
	clr_SDA;		// Set data low (1 uS)

     }
//--------------------------------------------------------------
   }

	set_SDA;		// Set data high (input)
}




