//#include "serial.h"

//***********************************************************************
//* FILE        : serial.c
//*
//* DESCRIPTION : RS-232 rutines
//*
//***********************************************************************


//#define TRANSMIT_BUF_SIZE 2048
//#define RECEIVE_BUF_SIZE  2048

#define TRANSMIT_BUF_SIZE 1024
#define RECEIVE_BUF_SIZE  1024

#define Xtal	   24000000	// 24.000MHz
#define Baud_rate  9600


//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------

void init_serial(void);
byte get_char (void);

int put_char (byte tx_byte);	// Byte
int put_word (word RS_word);	// Double byte
int put_s (far char *str);	// Pointer to text-string.


//--------------------------------------------------------------
// LOCAL functions:
//--------------------------------------------------------------

 void transmit(void);		/*  serial interrupt routine  */
 void receive(void);		/*  serial interrupt routine  */

#pragma interrupt(transmit=5)
#pragma interrupt(receive=6)


//--------------------------------------------------------------
// Data:
//--------------------------------------------------------------

static unsigned char sp_status_image;

/*   transmit buffer and it's indexes    */
static unsigned char trans_buff[TRANSMIT_BUF_SIZE];
static int begin_trans_buff, end_trans_buff;

/*   receive buffer and it's indexes    */
static unsigned char receive_buff[RECEIVE_BUF_SIZE];
static int end_rec_buff, begin_rec_buff;
int rec_bufcnt;


/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  init_serial()					*/
/*								*/
/*--------------------------------------------------------------*/

void init_serial()
{
//----------------------------------------
// Mode = ASYNCHRONOUS MODE1.
// Parity is Disabled
//----------------------------------------
 sp_con = SP_MODE1 | REC_ENABLE | NO_PARITY;

//----------------------------------------
// Sp_Baud = (Fosc / (Baud Rate x 16)) -1
// Sp_Baud = (24 000 / (9600 x 16)) -1 = 155.25 (0x9B)
//----------------------------------------

// sp_baud = 0x8000 + (Xtal / (Baud_Rate x 16)) -1;
 sp_baud = 0x809B;

 setbit(p2_reg,0);    /*  init txd pin output  */
 clrbit(p2_dir,0);     /*  make txd pin output  */
 setbit(p2_mode,0);   /*  enable txd mode on p2.0 */

 setbit(p2_reg,1);    /*  init rxd pin input  */
 setbit(p2_dir,1);     /*  make rxd pin input  */
 setbit(p2_mode,1);   /*  enable rxd mode on p2.1 */

 int_mask |= TXD_INTERRUPT | RXD_INTERRUPT;

 end_rec_buff=0;          /* initialize buffer pointers        */
 begin_rec_buff=0;
 rec_bufcnt = 0;	// No characters in buffer!

 end_trans_buff=0;
 begin_trans_buff=0;

 sp_status_image |= sp_status;	/*  image sp_status into sp_status_image  */
 sp_status_image = 0;
 setbit(sp_status_image, TI_BIT);	// Initialize put_char first time.
}



/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  get_char()					*/
/*								*/
/*--------------------------------------------------------------*/

byte get_char (void)
{
//----------------------------------------------------
//	Return zero if there is not a character avaliable:
//----------------------------------------------------

  if (rec_bufcnt == 0)
     return (0);

//----------------------------------------------------
//	Return the character in buffer, and
//	make buffer appear circular:
//----------------------------------------------------

  disable();
  rec_bufcnt--;		// One less character in buffer.
  enable();

  begin_rec_buff++;	// Point to next character in buffer.

  if(begin_rec_buff >= RECEIVE_BUF_SIZE)	/* Make buffer appear circular. */
     begin_rec_buff = 0;

  return (receive_buff[begin_rec_buff]);	/* Return the character in buffer. */
}


/*--------------------------------------------------------------*/
/*								*/
/*	NAME:   put_char()					*/
/*								*/
/*		Returnerer  0 hvis OK				*/
/*		Returnerer -1 hvis FEIL				*/
/*								*/
/*--------------------------------------------------------------*/

int put_char (byte tx_byte)
{

//----------------------------------------------------
//	Return error if the buffer is full:
//----------------------------------------------------

  if((end_trans_buff+1 == begin_trans_buff) || (end_trans_buff >= TRANSMIT_BUF_SIZE && begin_trans_buff == 0))
     return (-1);

//----------------------------------------------------
//	Put character in buffer, and
//	make buffer appear circular:
//----------------------------------------------------

  trans_buff[end_trans_buff] = tx_byte;

  end_trans_buff++;
  if(end_trans_buff >= TRANSMIT_BUF_SIZE)
     end_trans_buff = 0;

//----------------------------------------------------
//	If transmitt buffer was empty, then cause
//	an interrupt to start transmitting:
//----------------------------------------------------

  if(checkbit(sp_status_image, TI_BIT))
     int_pend |= TXD_INTERRUPT;

  return (0);
}

/*--------------------------------------------------------------*/
/*								*/
/*	NAME:   put_word ()					*/
/*								*/
/*		Returnerer  0 hvis OK				*/
/*		Returnerer -1 hvis FEIL				*/
/*								*/
/*--------------------------------------------------------------*/

int put_word (word RS_word)		// Pointer to text-string.
{

  if (put_char((byte)(RS_word >> 8)))	//send high byte
      return (-1);

  if (put_char((byte)RS_word))		//send low byte
      return (-1);

  return (0);		// Ok, data er skrevet.
}



/*--------------------------------------------------------------*/
/*								*/
/*	NAME:   put_s ()						*/
/*								*/
/*		Returnerer  0 hvis OK				*/
/*		Returnerer -1 hvis FEIL				*/
/*								*/
/*--------------------------------------------------------------*/

int put_s (far char *str)		// Pointer to text-string.
{
  register int i;

  for (i=0; *(str+i) != '\0'; i++)
  {
      if (put_char (*(str+i)))
	 return (-1);
  }
 return (0);		// Ok, data er skrevet.
}



/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  transmit()					*/
/*								*/
/*--------------------------------------------------------------*/

void transmit(void)		/*  serial interrupt routine  */
{
  sp_status_image |= sp_status;	/*  image sp_status into sp_status_image  */

//----------------------------------------------------
//   Transmitt a character if there is a character
//   in the buffer, else leave TI_BIT set in image
//   for put_char to enable interrupts:
//----------------------------------------------------
  if(begin_trans_buff != end_trans_buff)
  {
     sbuf_tx = trans_buff[begin_trans_buff];   /*  transmit character  */

//----------------------------------------------------
//   Make the buffer circular by starting over when
//   the index reaches the end of the buffer.
//----------------------------------------------------

     begin_trans_buff++;
     if(begin_trans_buff >= TRANSMIT_BUF_SIZE)
	begin_trans_buff = 0;

     clrbit(sp_status_image,TI_BIT);     /*  clear TI bit in status_image.   */
  }
}



/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  receive()					*/
/*								*/
/*--------------------------------------------------------------*/

void receive(void)              /*  serial interrupt routine  */
{
  sp_status_image |= sp_status;       /*  image sp_status into status_image  */

//----------------------------------------------------
//  If the input buffer is full, the last character
//  can be handled as desired.
//----------------------------------------------------

// if(end_rec_buff+1 == begin_rec_buff || (end_rec_buff >= RECEIVE_BUF_SIZE-1 && begin_rec_buff == 0))

 if (rec_bufcnt >= RECEIVE_BUF_SIZE)
 {
     ;  /*  input overrun code  */
 }


//----------------------------------------------------
//  Make the buffer circular by starting over when
//  the index reaches the end of the buffer.
//----------------------------------------------------

 else
 {
     rec_bufcnt++;	// One more character in buffer.
     end_rec_buff++;	// Point to next input character in buffer.

     if(end_rec_buff >= RECEIVE_BUF_SIZE)
	end_rec_buff = 0;

     receive_buff[end_rec_buff] = sbuf_rx;	/* place character in buffer */

//----------------------------------------------------
//  Check for serial errors:
//----------------------------------------------------

     if(checkbit(sp_status_image, FE_BIT))
     {
	  ;    /*  User code for framing error  */
	  clrbit(sp_status_image, FE_BIT);
     }
     if(checkbit(sp_status_image, OE_BIT))
     {
	  ;    /*  User code for overrun error  */
	  clrbit(sp_status_image, OE_BIT);
     }

 }

 clrbit(sp_status_image,RI_BIT);   /*  clear RI bit in status_image.  */
}



