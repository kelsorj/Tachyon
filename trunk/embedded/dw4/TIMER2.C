
//***********************************************************************
//* FILE        : Timer2.c
//***********************************************************************
//*
//* DESCRIPTION : The timer2 is used for the EPA channels. (High speed I/O)
//*
//*               EPA0 (motor-0 tacho) captures timer2 count for
//*               speed calculations, and the EPA0-event interrupt
//*               rutine controles the motor0 speed and position.
//*
//*               EPA1 (motor-1 tacho) captures timer2 count for
//*               speed calculations, and the EPA1-event interrupt
//*               rutine controles the motor1 speed and position.
//*
//*               EPA2 (motor-2 tacho) captures timer2 count for
//*               speed calculations, and an EPA2-event interrupt
//*               rutine controles the motor2 speed and position.
//*
//***********************************************************************
//*  <c196init.h>  Common includes and macros
//*  <globdata.c>  Global data declarations
//***********************************************************************


//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------

void init_timer2 (void);

//--------------------------------------------------------------
// LOCAL functions:
//--------------------------------------------------------------

void timer2_interrupt (void);


/*--------------------------------------------------------------*/
/*								*/
/*	NAME:   init_timer2()					*/
/*								*/
/*--------------------------------------------------------------*/
/*								*/
/*	The timer2 runs at the lowest speed (presclae = 64).	*/
/*								*/
/*	Timer2 count speed:	93.750 kHz  (24MHz /4 /64)	*/
/*	Timer2 overun rate:	1.43Hz (0.699 sec).		*/
/*								*/
/*--------------------------------------------------------------*/
void init_timer2(void)
{

    timer2 = 0;

 t2control = COUNT_ENABLE |
	     COUNT_UP |
	     CLOCK_INTERNAL |
	     DIVIDE_BY_64;

 int_mask |= 2;		/*  un-mask interrupt */
}


/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  timer2_interrupt()				*/
/*								*/
/*--------------------------------------------------------------*/

#pragma interrupt(timer2_interrupt = 1)

void timer2_interrupt(void)
{

}


