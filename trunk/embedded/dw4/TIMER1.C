#include "globdata.h"
#include "keybrd.h"

//***********************************************************************
//* FILE        : Timer1.c
//*
//* DESCRIPTION : Timer-1 interrupts accours at a 10kHz rate ( every 100uS ).
//*               Different timer-counters, flags and timeouts can be
//*               handled here.
//*
//* Interrupt execution times:
//* --------------------------
//*               Not measured yet.
//*               Be aware of the high repetition rate!!!
//*
//*
//***********************************************************************
//*  <c196init.h>  Common includes and macros
//*  <globdata.c>  Global data declarations
//***********************************************************************


//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------

void init_timer1 (void);		/* Local function */
void delay_ms (word time);	/* Local function */


//--------------------------------------------------------------
// LOCAL functions:
//--------------------------------------------------------------

void timer1_interrupt (void);		/* Local function */

//--------------------------------------------------------------
// EXTERNAL functions:
//--------------------------------------------------------------
extern void scan_key_board(void);


//--------------------------------------------------------------
#define TIMER1_CNT	600-102		// Timer reload value for 10kHz = 0.1mS (600 x 166.67nS)
//--------------------------------------------------------------


/*--------------------------------------------------------------*/
/*			                                        */
/*	NAME:  init_timer1()					*/
/*			                                        */
/*--------------------------------------------------------------*/

void init_timer1(void)
{

	//-----------------------------
	Timer1_Delay    = 0;
	ms1000_prescale = 1000;
	sec_prescale    = 1000;
	LED_prescale    = 1000;
	ms20_prescale   = 20;
	ms1_prescale	  = 10;	// = 1mS
	RS_Timer_1mS    = 0;
	Count_100uS     = 45;	// Ready for next disp (4,5mS)
	Timer_100uS     = 0;	// Timer counter in timer1_interrupt()
	Timer_1mS       = 0;	// Timer counter in timer1_interrupt()
	DispPause       = 0;	// Timer counter in timer1_interrupt()
	DispFlag        = 0;
	Timer_1sec      = 0;	// Timer counter in timer1_interrupt()
	IdleTimer       = 0;	// 1S counter, Idletime in main().
	PressureTimeout = 0;	// 1S counter, timout (PressureAlarm)
	SecFlag = 0;		// Flag a new 1sec value.

	//-----------------------------

	timer1   = TIMER1_CNT;	// 1kHz = 1mS (6000 x 166.67nS)

	t1control = COUNT_ENABLE |
		COUNT_DOWN |
		CLOCK_INTERNAL |
		DIVIDE_BY_1;

	int_mask |= 1;		/*  un-mask interrupt */

	//-----------------------------
}


//***************************************************************
//*
//*	NAME:  timer1_interrupt()
//*
//***************************************************************

#pragma interrupt(timer1_interrupt = 0)

void timer1_interrupt(void)
{

	//------------------------------------------
	//--------- Reload timer regs first: -------
	//------------------------------------------

	timer1 = TIMER1_CNT;	// 100uS (600 x 166.67nS)

	//------------------------------------------
	//--------- Check Disp-Valves: -------------
	//------------------------------------------

	if (DispFlag)		// DispenseValve aktivation!
	{
		if (!DispPause)		// Pause from last dispense?
		{
			if (Count_100uS)	// Counter for DispenseVoltage = 24v!
			{
				// DKM 112309 this is a crappy way to do this -- they call a macro
				//			  somewhere else that changes the port byte
				//			  this is here so that they use high current to first open
				//		      the valve, then low current to hold it open
				if      (Count_100uS >= 45) Disp_Valve_on;	// Open Valves (1/2/3/4) 24v ON! (4,5mS)
				else if (Count_100uS == 1)  Disp_Valve_low;	// Lower Current (100 ohm = 6v)!
				Count_100uS--;
			}

			Timer_100uS--;		// Total Dispense time! (32bit)

			if (Timer_100uS == 0L)
			{
				Disp_Valve_off;	// Disp-Valves OFF!
				DispFlag = 0;
				Count_100uS = 45;	// Ready for next disp (4,5mS)
				DispPause = Param[0].DispPause;	// PAUSE after a Dispense (300mS)!
			}
		}
	}

	//------------------------------------------
	//--------- Check 1mS counter: -------------
	//------------------------------------------

	--ms1_prescale;

	if (ms1_prescale != 0)
		return;
	// DKM 112309 apparently, this interrupt is signaled every 0.1ms, and we
	//			  only want it to process the following code every 1ms, so we
	//			  have to loop 10x
	ms1_prescale = 10;	// = 1mS

	//------------------------------------------
	//--------- 1mS timers: --------------------
	//------------------------------------------

	Start_Flag++;		// Set this flag each 1 mSek (for the main loop).

	++Timer1_Delay;		// inc each 1mS.
	++RS_Timer_1mS;

	if (Timer_1mS)		// Any count-down value ?
		--Timer_1mS;		// Decrement the 1mS counter.

	if (DispPause)		// Any count-down value ?
		--DispPause;		// Decrement the 1mS counter.
	//valve4_off;

	//------------------------------------------

	if (Beep_Cnt)
	{
		--Beep_Cnt;
		if (Beep_Cnt == 0)
			clr_beep;
		else
			set_beep;
	}


	//------------------------------------------------
	//--------- 20mS  timers:handling of keyboard ----
	//------------------------------------------------
	if (--ms20_prescale == 0)	// 20 mS ?
	{
		ms20_prescale = 20;	// Init the prescaler
		scan_key_board();
	}


	//------------------------------------------
	//--------- Display BLINK timer: -----------
	//------------------------------------------
	if (--LED_prescale == 0)	// 1000 mS ?
	{
		LED_prescale = 900;	// Init the prescaler
		LED_On = 1;		// Set flag!
		UppdateFlag = 1;		// Set flag!
	}
	else if (LED_prescale == 250)	// 200 mS ?
		LED_Off = 1;		// Set flag!

	//------------------------------------------
	//--------- 1 sek timers: ---------------
	//------------------------------------------

	if (--ms1000_prescale == 0)	// 1000 mS ?
	{
		ms1000_prescale = 1000;	// Init the prescaler
		++IdleTimer;		// Increment the 1S counter.
		++PressureTimeout;	// 120 sec timout (PressureAlarm)
	}

	if (Timer_1sec)		// SOAK-timer:
		if (--sec_prescale == 0)	// 1000 mS ?
		{
			sec_prescale = 1000;	// Init the prescaler
			Timer_1sec--;		// Decrement the 1S counter.
			SecFlag = 1;		// Flag the new value.
		}

}


/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  delay_ms (time)					*/
/*								*/
/*--------------------------------------------------------------*/
/*								*/
/*	Routine to delay (1mS * time) - (0 to 1mS)		*/
/*								*/
/*--------------------------------------------------------------*/

void  delay_ms (word time)
{
	Timer1_Delay = 0;

	while (Timer1_Delay < time)
	{}

}



