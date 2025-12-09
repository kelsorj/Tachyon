#include "globdata.h"
#include "display.h"
#include "keybrd.h"

//***********************************************************************
//* FILE        : pressure.c
//*
//* DESCRIPTION : Pressure regulator
//*               Pressure sensor calibration
//*
//***********************************************************************

//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------

void init_pressure (void);
void regulate_pressure (int WantedPressure);
void calibrate_pressure (void);


//--------------------------------------------------------------
// LOCAL functions:
//--------------------------------------------------------------
int READ_ADC (void);

//--------------------------------------------------------------
// External functions:
//--------------------------------------------------------------
extern void delay_ms (word time);
extern void lcd_putc (byte adr, byte data);
extern void lcd_puts (byte adr, const char *str);	// (display.c)
extern void InterpretCommand();		// Check serial commands
extern byte get_key();

extern void start_motor0 (byte speed, long Position);
extern int put_word (word RS_word);	// Transmit RS-232 word.

//--------------------------------------------------------------

int  ValveWait;
int  PumpWait;
int  DiffPressure;


//***************************************************************
//   INIT_PRESSURE ()
//***************************************************************

void init_pressure (void)
{
	P_Fine_Reg = 0;	// Not High accuracy regulation!
	PressureOk = 0;	// Not Ok Pressure!
	PressureAlarm = 0;	// Not Alarm!

	ADC_Timer = 50;	// Min 50mS, Conts down i regulate_pressure().
	IdleTimer = 0;	// Counts up in Main(), 5 minutes, then low pressure!
	PressureTimeout = 0;	// Counts up in Timer1(), 120 sec timout (PressureAlarm).

	DiffPressure = 0;
	ValveWait = 0;
	PumpWait = 0;

	Pres_Pump_off;	// Stop Pressure-Pump!
	Pres_Valve_off;  	// Close drain valve!
}


//***************************************************************
//   REGULATE_PRESSURE ()
//***************************************************************

void regulate_pressure (int WantedPressure)
{
	static int OldDiff;
	static int NewDiff;

	int CurPressure;	// Current pressure (ADC0-value).
	int WantedIntPres;	// Wanted Internal Pressure (reffered to ADC0-value).
	int P_Limit;		// Pressure Limit.
	int P_HiLim1;		// Pressure Limit.
	int P_HiLim2;		// Pressure Limit.

	//-----------------------------------------------

	ADC_Timer--;		// Decrement counter.

	if (PumpWait)
		PumpWait--;

	if (ValveWait)
		ValveWait--;

	//-----------------------------------------------

	if (DispFlag)	  	// If Disp-Valve is open:
		PressureOk = 0;		// Not accurate Pressure!

	if (DispPause)	  	// If pause after Disp-Valve was open:
		PressureOk = 0;		// Not accurate Pressure!


#define  Select_ADC9  Select_ADC6	// Internal Pressure Sensor (-)
#define  delay_ADC    delay_4uS		// delay (3k,1nF time-constant)


	//-----------------------------------------------
	//-- Read ADC-pressure:
	//-----------------------------------------------
	// DKM 092209 what is so special about the ADC selection?  Need to check
	//            the calling function to see what it does.  But then why
	//			  select ADC9, pause, then select ADC1 but do nothing in between???
	if (PressureOk)	  	// If stable pressure conditions:
	{
		switch(ADC_Timer)
		{
		case 18:	Select_ADC9;		// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC1;		// Multiplexer input 1.
			return;

		case 17:	Select_ADC9;		// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC2;		// Multiplexer input 2.
			return;

		case 16:	Select_ADC9;		// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC3;		// Multiplexer input 3.
			return;

		case 15:	Select_ADC9;		// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC4;		// Multiplexer input 4.
			return;

		case 14:	Select_ADC9;			// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC1;			// Multiplexer input.
			return;

		// DKM 112309 why didn't they write a mbar calculation function???  they calculate
		//		      mbar here slightly differently than in calibrate_pressure.
		case 12:	Press[1]  = READ_ADC ();	// Read pressure 1. (0,2mS rutine)
			Press[1] -= Param[0].PresOffset[1];
			mBar[1] = (long)((long)(Press[1]+2) * (long)1000L) / (long)Param[0].PresCal[1];
			Select_ADC9;			// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC2;			// Multiplexer input.
			return;

		case 10:	Press[2]  = READ_ADC ();	// Read pressure 2. (0,2mS rutine)
			Press[2] -= Param[0].PresOffset[2];
			mBar[2] = (long)((long)(Press[2]+2) * (long)1000L) / (long)Param[0].PresCal[2];
			//		Select_ADC6;			// Multiplexer input-NEG.
			Select_ADC9;			// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC3;			// Multiplexer input.
			return;

		case 8:	Press[3]  = READ_ADC ();	// Read pressure 3. (0,2mS rutine)
			Press[3] -= Param[0].PresOffset[3];
			mBar[3] = (long)((long)(Press[3]+2) * (long)1000L) / (long)Param[0].PresCal[3];
			Select_ADC9;			// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC4;			// Multiplexer input.
			return;

		case 6:	Press[4]  = READ_ADC ();	// Read pressure 4. (0,2mS rutine)
			Press[4] -= Param[0].PresOffset[4];
			mBar[4] = (long)((long)(Press[4]+2) * (long)1000L) / (long)Param[0].PresCal[4];
			Select_ADC9;			// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC0;			// Multiplexer input.
			return;

		// DKM 112309 this is a little corny but I guess they are trying to keep the
	    //			  time delays to a minimum.  It at least looks like they don't
		//			  interleave any other calls that could modify Press[0].  The only
		//			  other modifier is in the else block below.
		case 4:	Press[0]  = READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			return;

		case 1:	Press[0] += READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			Press[0]  = Press[0] >> 1;	// Mean value of 2 readings.
			Press[0] -= Param[0].PresOffset[0];
			mBar[0] = (long)((long)(Press[0]+2) * (long)1000L) / (long)Param[0].PresCal[0];
			mBar[6] = mBar[0];		// Ok-backup.
			return;

		case 0:	OldDiff = NewDiff;
			NewDiff = mBar[CurrentADC] - mBar[0];
			break;

		default:	return;
		}
	}
	else		// Not stable conditions:
	{
		switch(ADC_Timer)
		{
		case 5:	Select_ADC9;		// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC1;		// Multiplexer input.
			delay_ADC;		// +2 uS.

			Select_ADC9;		// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC2;		// Multiplexer input.
			delay_ADC;		// +2 uS.

			Select_ADC9;		// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC3;		// Multiplexer input.
			delay_ADC;		// +2 uS.

			Select_ADC9;		// Multiplexer input-NEG.
			delay_ADC;		// +2 uS.
			Select_ADC4;		// Multiplexer input.
			delay_ADC;		// +2 uS.

			Select_ADC9;		// Multiplexer input-NEG.
			return;

		case 4:	Select_ADC0;		// Multiplexer input.
			return;

		case 2:	Press[0]  = READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			return;

		case 0:	Press[0] += READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			Press[0]  = Press[0] >> 1;	// Mean value of 2 readings.
			Press[0] -= Param[0].PresOffset[0];
			mBar[0] = (long)((long)(Press[0]+2) * (long)1000L) / (long)Param[0].PresCal[0];
			break;

		default:	return;
		}
	}

	ADC_Timer = 20;		// Check pressure each 20mS!
	CurPressure = mBar[0];	// Mean value of last 2 readings!

	//--------------------------
	//-- IF TEST-BOARD: --------
	//--------------------------
#ifdef __TestBoard
	CurPressure = WantedPressure;
#endif
	//--------------------------


	//---------------------------------------------------------
	// Idle-state: (zero pressure) ?
	//---------------------------------------------------------

	if (WantedPressure == 0)	// Idle?
	{
		if (Pres_Pump)		// Is pump running?
			Pres_Pump_off;	// Stop Pressure-Pump!

		ADC_Timer = 100;	// Check pressure after 500mS!

		if (Pres_Valve)	// Is drain valve open?
		{
			if (CurPressure > 15)	// > 0.015 bar?
				PressureTimeout = 0;	// reset counter

			if (PressureTimeout > 20)	// 20sec extra?
			{
				Pres_Valve_off;	  	// Close drain valve!
				PressureTimeout = 0;	// reset
			}
		}
		else
		{
			PressureTimeout = 0;	// reset
			if (CurPressure > 15)		// > 0.015 bar?
				Pres_Valve_on;		// Open drain valve!
		}

		PressureOk = 0;		// Not accurate Pressure!
		IdleFlag = 1;			// Set IdleFlag.
		//	 mBar[6] = 0;			// Zero-backup.
		return;
	}


	//---------------------------------------------------------
	// Normal pressure regulation:
	//---------------------------------------------------------
#define PumpTime 		800	// 300mS
#define ValveTime 	800	// 400mS
#define NewAdcTime 	150	// 50mS
	//----------------------------------------
	if (WantedPressure >= 250)	// > 250mBar?
	{
		if      (P_Fine_Reg == 1) P_Limit =  15;	// 0.015 bar
		else if (P_Fine_Reg == 2) P_Limit =  20;	// 0.020 bar
		else if (P_Fine_Reg == 3) P_Limit =  60;	// 0.060 bar
		else 			  P_Limit = 100;	// 0.100 bar
		P_HiLim2 = 20;	// Valve On
		P_HiLim1 = 10;	// Valve Off
	}
	else if (WantedPressure >= 100)	// 100-250mBar?
	{
		if      (P_Fine_Reg == 1) P_Limit =  10;	// 0.010 bar
		else if (P_Fine_Reg == 2) P_Limit =  15;	// 0.015 bar
		else if (P_Fine_Reg == 3) P_Limit =  30;	// 0.030 bar
		else 			  P_Limit =  80;	// 0.080 bar
		P_HiLim2 = 10;	// Valve On
		P_HiLim1 = 1;	// Valve Off
	}
	else				// < 100mBar
	{
		if      (P_Fine_Reg == 1) P_Limit =  6;		// 0.006 bar
		else if (P_Fine_Reg == 2) P_Limit =  8;		// 0.008 bar
		else if (P_Fine_Reg == 3) P_Limit = 10;		// 0.010 bar
		else 			  P_Limit = 20;		// 0.020 bar
		P_HiLim2 = 10;	// Valve On
		P_HiLim1 = -1;	// Valve Off
	}

	//----------------------------------------
	if (P_Limit > WantedPressure/4)	// 25 prosent max
		P_Limit = WantedPressure/4;

	//----------------------------------------
	//- Include ExtPress:
	//----------------------------------------
	WantedIntPres = WantedPressure - DiffPressure;
	if (WantedIntPres < 10)	// < 10mBar?
		WantedIntPres = 10;
	//----------------------------------------


	//-----------------------------------------------
	//-- Regulate pressure:
	//-----------------------------------------------

	if (Pres_Pump)	// Is pump running?
	{
		PressureOk = 0;		// Not accurate Pressure!

		if (CurPressure >= WantedIntPres+4)	// High-limit?
		{
			Pres_Pump_off;		// Stop Pressure-Pump!
			ValveWait = ValveTime;	// Dont use valve for 600mS!
			ADC_Timer = NewAdcTime;	// Check pressure after 150mS!
		}
		else
			ADC_Timer = 6;	// Check pressure after another 10mS!
	}


	else if (Pres_Valve)		// Is drain valve open?
	{
		PressureOk = 0;		// Not accurate Pressure!

		if (CurPressure <= WantedIntPres+(P_HiLim1))	// Low-limit?
		{
			Pres_Valve_off;		// Close drain valve!
			PumpWait = PumpTime;	// Dont use pump for 600mS!
			ADC_Timer = NewAdcTime;	// Check pressure after 150mS!
		}
		else
			ADC_Timer = 6;	// Check pressure after another 10mS!
	}


	//---------------------------------------------------------
	// Is Pressure too High:
	//---------------------------------------------------------

	else if (CurPressure > (WantedIntPres+P_HiLim2))	// Pressure too high? (Limit + 0.005 bar)
	{
		PressureOk = 0;		// Not accurate Pressure!

		if (!ValveWait)
		{
			Pres_Valve_on;		// Open drain valve!
			ADC_Timer = 8;		// Check PeakValue after 10mS!
		}
	}

	//---------------------------------------------------------
	// Is Pressure too Low:
	//---------------------------------------------------------
	else if ((CurPressure+P_Limit) < WantedIntPres)	// Pressure too Low? (-Limit)
	{
		PressureOk = 0;		// Not accurate Pressure!

		if (!PumpWait)
		{
			Pres_Pump_on;		// Start Pressure-Pump!
			ADC_Timer = 8;		// Check pressure after 10mS!
		}
	}

	//---------------------------------------------------------
	// Pressure is within limits:
	//---------------------------------------------------------
	else
	{
		PressureTimeout=0;
		ADC_Timer = 20;	// Check pressure each 20mS!

		if (!PumpWait && !ValveWait && !DispPause)
		{
			if (NewDiff < -100)	// within +/- 100mBar?
			{
				DiffPressure = -100;
				SensorError = 1;
			}
			else if (NewDiff > 100)	// within +/- 100mBar?
			{
				DiffPressure = 100;
				SensorError = 2;
			}
			else if ((WantedPressure - NewDiff) < 5)	// Bottle too high? (internal pressure < 5mBar)?
			{
				DiffPressure = NewDiff;
				SensorError = 3;
			}
			else
			{
				DiffPressure = NewDiff;
				SensorError = 0;
				PressureError = 0;
			}


			IdleFlag = 0;		// Enable IdleTimer.
			PressureOk++;		// Pressure is accurate and stable!
			if (PressureOk > 250)
				PressureOk = 250;
		}
	}


	//---------------------------------------------------------

	if(IdleFlag)
		IdleTimer = 0;	// No IdleTimeOut when building pressure!

	if (PressureTimeout > PRESSURE_TIMEOUT)	// 120sec
	{
		PressureAlarm = 1;		// Set global Alarm!
		Pres_Pump_off;		// Stop Pressure-Pump!
		Pres_Valve_off;	  	// Close drain valve!
	}

}






//***************************************************************
//   CALIBRATE_PRESSURE ()
//***************************************************************

void calibrate_pressure (void)
{
	int P_Bak[6];	// Copy of Current pressure
	int NewCal[6];	// Copy of Current pressure
	int Diff = 0;
	int t = 0;
	char Text[24];
	byte CalDone;
	byte key;
	//------------------

	//**********************************************************************
	//- 1: Calibrate Zero Offset: ------------------------------------------
	//**********************************************************************

	Pres_Pump_off;		// Stop Pressure-Pump!
	Pres_Valve_on;		// Open drain valve!
	CalDone = 0;		// No Calibration Done!

	lcd_puts (LCD1, "Zero Pressure Cal:  ");
	lcd_puts (LCD2, "                    ");
	lcd_puts (LCD3, "                    ");
	lcd_puts (LCD4, "                    ");

	delay_ms(100);
	key = get_key();

	delay_ms(100);
	key = get_key();


	while(1)
	{
		do { idle();}
		while (!Start_Flag);		// Stay in IDLE mode, wait for startflag.
		Start_Flag = 0;		// Ok, clear startflag (every 1mS).

		InterpretCommand();  //Check for seial commands

		if (++t == 100)
		{
			t = 0;
			//-------------------------------------
			Select_ADC0;		// Multiplexer input.
			delay_ms(3);
			Press[0]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			mBar[0] = Press[0] + 1 - Param[0].PresOffset[0];
			mBar[0] = (long)((long)mBar[0] * (long)1000L) / (long)Param[0].PresCal[0];
			//-------------------------------------
			Select_ADC0_Vac;		// Multiplexer input.
			delay_ms(3);
			Press[5]= READ_ADC ();	// Read Internal Vacuum (0,2mS rutine)
			mBar[5] = Press[5] + 1 - Param[0].PresOffset[5];
			mBar[5] = (long)((long)mBar[5] * (long)1000L) / (long)Param[0].PresCal[5];
			//-------------------------------------
			Select_ADC1;		// Multiplexer input.
			delay_ms(3);
			Press[1]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			mBar[1] = Press[1] + 1 - Param[0].PresOffset[1];
			mBar[1] = (long)((long)mBar[1] * (long)1000L) / (long)Param[0].PresCal[1];
			//-------------------------------------
			Select_ADC2;		// Multiplexer input.
			delay_ms(3);
			Press[2]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			mBar[2] = Press[2] + 1 - Param[0].PresOffset[2];
			mBar[2] = (long)((long)mBar[2] * (long)1000L) / (long)Param[0].PresCal[2];
			//-------------------------------------
			Select_ADC3;		// Multiplexer input.
			delay_ms(3);
			Press[3]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			mBar[3] = Press[3] + 1 - Param[0].PresOffset[3];
			mBar[3] = (long)((long)mBar[3] * (long)1000L) / (long)Param[0].PresCal[3];
			//-------------------------------------
			Select_ADC4;		// Multiplexer input.
			delay_ms(3);
			Press[4]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			mBar[4] = Press[4] + 1 - Param[0].PresOffset[4];
			mBar[4] = (long)((long)mBar[4] * (long)1000L) / (long)Param[0].PresCal[4];
			//-------------------------------------
			sprintf  (Text, " P0=%3d mBar ", mBar[0]);
			lcd_puts (LCD2, Text);
			sprintf  (Text, " P1=%3d P2=%3d ", mBar[1], mBar[2]);
			lcd_puts (LCD3, Text);
			sprintf  (Text, " P3=%3d P4=%3d ", mBar[3], mBar[4]);
			lcd_puts (LCD4, Text);
			//-------------------------------------
			key = get_key();
			switch (key)
			{
			case DOWN_KEY  : break;
			case UP_KEY    : break;
			case START_KEY : break;

			case STOP_KEY  : goto CAL_END; break;	// STOPP+Init.
			case ENTER_KEY : goto ZeroCal; break;	// STOPP+Init.
			case CANCEL_KEY: goto ZeroEnd; break;	// STOPP+Init.
			}
		}
	}

	//---------------------------------------------
	//- Calculate the new Offset Values:
	//---------------------------------------------
ZeroCal:
#define  MAX_OFS  PRESSURE_OFS*5/3	// +66%
#define  MIN_OFS  PRESSURE_OFS*1/3	// -66%

	if (Press[0] < MAX_OFS && Press[0] > MIN_OFS)
		Param[0].PresOffset[0] = Press[0];

	if (Press[1] < MAX_OFS && Press[1] > MIN_OFS)
		Param[0].PresOffset[1] = Press[1];

	if (Press[2] < MAX_OFS && Press[2] > MIN_OFS)
		Param[0].PresOffset[2] = Press[2];

	if (Press[3] < MAX_OFS && Press[3] > MIN_OFS)
		Param[0].PresOffset[3] = Press[3];

	if (Press[4] < MAX_OFS && Press[4] > MIN_OFS)
		Param[0].PresOffset[4] = Press[4];

	if (Press[5] < MAX_OFS && Press[5] > MIN_OFS)
		Param[0].PresOffset[5] = Press[5];

	CalDone = 1;	// 1 Calibration Done!

ZeroEnd:

	//**********************************************************************
	//- 2: Calibrate 600 mBar: ---------------------------------------------
	//**********************************************************************

	sprintf  (Text, "Pressure Cal 600mB: ");
	lcd_puts (LCD1, Text);
	lcd_puts (LCD2, "                    ");
	lcd_puts (LCD3, "                    ");
	lcd_puts (LCD4, "                    ");

	//---------------------------------------------

	// DKM 112309 It already was off!
	Pres_Pump_off;		// Stop Pressure-Pump!
	Pres_Valve_off;  	// Close drain valve!

	CurrentADC = 0;		// Select internal pressure-sensor!
	P_Fine_Reg = 1;		// High accuracy regulation!
	ADC_Timer  = 40;	// Min 25mS, Conts down i regulate_pressure().
	IdleTimer  = 0;		// Counts up in Main(), 5 minutes, then low pressure!
	PressureTimeout = 0;	// Counts up in Timer1(), 120 sec timout (PressureAlarm).
	RegPressure = 600;
	Diff = 0;
	t = 0;

	//---------------------------------------------

	while(1)
	{
		do { idle();}
		while (!Start_Flag);		// Stay in IDLE mode, wait for startflag.
		Start_Flag = 0;		// Ok, clear startflag (every 1mS).

		InterpretCommand();		// Check for seial commands
		regulate_pressure (RegPressure);	// Read pressure, set AlarmFlag, run Pump.

		if (PressureOk)
			if (ADC_Timer == 16)
				if (++t == 10)
				{
					t = 0;
					//	     sprintf  (Text, " P0=%3d mBar (%3d)  ", mBar[0], (mBar[0]+Diff));
					sprintf  (Text, " P0=%3d mBar        ", mBar[0]);
					lcd_puts (LCD2, Text);
					sprintf  (Text, " P1=%3d P2=%3d", mBar[1], mBar[2]);
					lcd_puts (LCD3, Text);
					sprintf  (Text, " P3=%3d P4=%3d", mBar[3], mBar[4]);
					lcd_puts (LCD4, Text);
				}

				if (PressureOk > 3)
					if (ADC_Timer == 16)
					{
						P_Bak[5] = mBar[0];	// Copy OK-pressure0 mBar!
						P_Bak[0] = Press[0];	// Copy OK-pressure0!
						P_Bak[1] = Press[1];	// Copy OK-pressure1!
						P_Bak[2] = Press[2];	// Copy OK-pressure2!
						P_Bak[3] = Press[3];	// Copy OK-pressure3!
						P_Bak[4] = Press[4];	// Copy OK-pressure4!
					}

					key = get_key();
					switch (key)
					{
					case UP_KEY   :  if (Diff < 50)
										 Diff++;	// Increase Pressure
						break;
					case DOWN_KEY :  if (Diff > -50)
										 Diff--;	// Decrease Pressure
						break;

					case STOP_KEY  : goto CAL_END; break;	// STOPP+Init.
					case ENTER_KEY : goto PressCal; break;	// STOPP+Init.
					case CANCEL_KEY: goto PressEnd; break;	// STOPP+Init.

					default: break;
					}
	}


	//---------------------------------------------
	//- Calculate the new Calibration Values:
	//---------------------------------------------
PressCal:
// DKM 112309 apparently only used for the internal pressure
//            and internal vacuum
#define  MAX_CAL_P0  PRESSURE_CAL*11/10	// +10%
#define  MIN_CAL_P0  PRESSURE_CAL*9/10	// -10%

#define  MAX_CAL  PRESSURE_CAL*13/10	// +30%
#define  MIN_CAL  PRESSURE_CAL*7/10	// -30%

	//	RegPressure = P_Bak[5] + Diff;	// New Correct pressure in mBar!
	RegPressure = P_Bak[5];		// New Correct pressure in mBar!

	NewCal[0] = (long)((long)P_Bak[0] * (long)1000L) / (long)RegPressure;	// Internal Pressure!
	NewCal[1] = (long)((long)P_Bak[1] * (long)1000L) / (long)RegPressure;
	NewCal[2] = (long)((long)P_Bak[2] * (long)1000L) / (long)RegPressure;
	NewCal[3] = (long)((long)P_Bak[3] * (long)1000L) / (long)RegPressure;
	NewCal[4] = (long)((long)P_Bak[4] * (long)1000L) / (long)RegPressure;

	if (NewCal[0] < MAX_CAL_P0 && NewCal[0] > MIN_CAL_P0)
	{
		Param[0].PresCal[0] = NewCal[0];	// Internal Pressure!
		Param[0].PresCal[5] = NewCal[0];	// Internal Vacuum!
	}
	if (NewCal[1] < MAX_CAL && NewCal[1] > MIN_CAL)
		Param[0].PresCal[1] = NewCal[1];	// Disp1 Pressure.

	if (NewCal[2] < MAX_CAL && NewCal[2] > MIN_CAL)
		Param[0].PresCal[2] = NewCal[2];	// Disp2 Pressure.

	if (NewCal[3] < MAX_CAL && NewCal[3] > MIN_CAL)
		Param[0].PresCal[3] = NewCal[3];	// Disp3 Pressure.

	if (NewCal[4] < MAX_CAL && NewCal[4] > MIN_CAL)
		Param[0].PresCal[4] = NewCal[4];	// Disp4 Pressure.

	CalDone = 1;	// 1 Calibration Done!


	//---------------------------------------------
	//- Store new parameters in EEPROM:
	//---------------------------------------------
PressEnd:
	if (CalDone > 0)	// Any Calibration Done?
		if (Param[0].PromCode == 0xAA55A55AL)	// EEPROM-Data ok?
		{
			write_eeprom_param ();
			lcd_puts (LCD1, "--------------------");
			lcd_puts (LCD2, "   New parameters   ");
			lcd_puts (LCD3, "   in EEPROM!       ");
			lcd_puts (LCD4, "--------------------");
			Beep_Cnt = 200;
			delay_ms(400);
			Beep_Cnt = 200;
			delay_ms(400);
			Beep_Cnt = 200;
			delay_ms(1200);
		}
		else
		{
			lcd_puts (LCD1, "--------------------");
			lcd_puts (LCD2, "   New parameters   ");
			lcd_puts (LCD3, "    not stored!     ");
			lcd_puts (LCD4, "--------------------");
			Beep_Cnt = 2000;
			delay_ms(2000);
		}
		//-------------------------------------
CAL_END:
		RegPressure = Param[0].DispPressure;
		P_Fine_Reg = 0;		// Not High accuracy regulation!

}



//--------------------------------------------------------------
//	Note:  ADC_Data (ADC Serial Data)  = P2 bit 3.
//	       ADC_Clk  (ADC Serial Clock) = P2 bit 2. (Max 100kHz)
//	       ADC_Cs   (ADC Chip Select)  = P2 bit 4.
//
//	       Timing is based on Xtal = 24 MHz
//--------------------------------------------------------------

#define   set_ADC_Clk	 (p2_reg |= (0x01 << 2))
#define   clr_ADC_Clk	 (p2_reg &= (~(0x01 << 2)))
#define   set_ADC_Cs	 (p2_reg |= (0x01 << 4))
#define   clr_ADC_Cs	 (p2_reg &= (~(0x01 << 4)))
#define   check_ADC_Data (p2_pin & (0x01 << 3))


//***************************************************************
//								*
//     NAVN:  READ_ADC	(Burr Brown ADS1286P)	(Tot. 180uS)	*
//								*
//***************************************************************
//	FullScale = 5,0v (AmpOut max = 4,1v)			*
//	12-bit (4096) = 1.22 mV/bit				*
//---------------------------------------------------------------
//	Sensor: 1000mBar = 90mV (@12v) => 37.5mV (@5v)		*
//	        1000mBar = 37.5mV x 100 =3.75v => 3072 (ADC)	*
//***************************************************************

int READ_ADC (void)
{
	register int ADC_Word = 0;
	register byte t;

	//----------- Init ADC (Start Conversion): ----------------------

	clr_ADC_Clk;		// Clk low.
	clr_ADC_Cs;		// CS/ low (Start Conversion).
	delay_3uS;		// +3us
	set_ADC_Clk;		// Clk high (read data-bit) DUMMY!.
	delay_2uS;		// +2us

	//----------- Read data bits from ADC: --------------------------

	t=14;			// Read all 12 (+2 dummy msb) bits from ADC_SDA.
	do {

		clr_ADC_Clk;		// Clk low (clock new data-bit).
		delay_2uS;			// +2us
		ADC_Word = (ADC_Word <<1);	// Rotate bits up.
		set_ADC_Clk;		// Clk high (read data-bit) (Bit 11-0).

		if (check_ADC_Data)	// Read bit
			ADC_Word |= 0x0001;	// Set lsb-bit in word.

	} while (--t);

	//----------- End ADC: ------------------------------------------

	set_ADC_Cs;		// CS/ high (PowerDown).

	ADC_Word &= 0x0FFF;	// Mask 12 lsb-bits.
	return (ADC_Word);
}
