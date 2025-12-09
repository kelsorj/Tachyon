#include "globdata.h"
#include "display.h"
#include "keybrd.h"
#include "comm.h"

//***********************************************************************
//* FILE        : calib.c	(sist endret V1.9+)
//*
//* DESCRIPTION : Calibration Rutines (interface to AquaTools)
//*
//***********************************************************************

//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------
void calibrate_press (void);	// Calibration rutine in Calib.c!
void calibrate_disp_lift (void);	// Calibration rutine in Calib.c!
void calibrate_carriage (void);	// Calibration rutine in Calib.c!
void calibrate_liquids (void);	// Calibration rutine in Calib.c!

//--------------------------------------------------------------
// External functions:
//--------------------------------------------------------------
extern void regulate_pressure (int WantedPressure);
extern void delay_ms (word time);
extern void InterpretCommand();		// Check serial commands
extern byte get_key(void);
extern byte get_char (void);	        // Get RS-232 byte.
extern void lcd_puts (byte adr, const char *str);
extern void lcd_clrl (byte adr);

extern void start_motor0 (byte speed, long Position);
extern int  put_word (word RS_word);	// Transmit RS-232 word.
extern int  READ_ADC (void);
extern void check_heads(void);

extern void start_motor0 (byte speed, long Position);	// Carriage
extern void start_motor1 (byte speed, int Position);	// Asp-Lift
extern void start_motor2 (byte speed, int Position);	// Disp-Lift
extern void soft_stop_motor0 (void);	// Stop motor0
extern void stop_motor0 (void);		// Stop motor0.
extern void stop_motor1 (void);		// Stop motor1.
extern void stop_motor2 (void);		// Stop motor2.

extern void init_motor0_home_position (void);
extern void init_motor1_home_position (void);
extern void init_motor2_home_position (void);
//--------------------------------------------------------------
extern byte Head1;	// Type of Head mounted! (check_heads())
extern byte Head2;	// Type of Head mounted!
extern byte Head3;	// Type of Head mounted!
extern byte Head4;	// Type of Head mounted!
extern byte Head5;	// Type of Head mounted!
extern int rec_bufcnt;
//--------------------------------------------------------------


//----------------------------------------------------------------------------
//- calibrate_press ()    (AquaTools Calibration)
//----------------------------------------------------------------------------
void calibrate_press (void)		// Calibration rutine in Calib.c!
{
	int t = 0;
	int Value[6];

	Beep_Cnt = 100;

	//-----------------------------------------------------------------
	// 3. Relase Pressure:
	//    Wait for Zero-Calibration-Ok:
	//-----------------------------------------------------------------

	Pres_Pump_off;		// Stop Pressure-Pump!
	Pres_Valve_on;		// Open drain valve!
	ExtCommand = 0;		// Reset command.

	while(1)
	{
		do { idle();}
		while (!Start_Flag);		// Stay in IDLE mode, wait for startflag.
		Start_Flag = 0;		// Ok, clear startflag (every 1mS).

		InterpretCommand();  //Check for seial commands

		switch (ExtCommand)
		{
		case T0_Cmd:
			//-------------------------------------
			Select_ADC0;		// Multiplexer input.
			delay_ms(2);
			Value[0]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			//-------------------------------------
			Select_ADC0_Vac;	// Multiplexer input.
			delay_ms(2);
			Value[5]= READ_ADC ();	// Read Internal Vacuum (0,2mS rutine)
			//-------------------------------------
			Select_ADC1;		// Multiplexer input.
			delay_ms(2);
			Value[1]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			//-------------------------------------
			Select_ADC2;		// Multiplexer input.
			delay_ms(2);
			Value[2]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			//-------------------------------------
			Select_ADC3;		// Multiplexer input.
			delay_ms(2);
			Value[3]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
			//-------------------------------------
			Select_ADC4;		// Multiplexer input.
			delay_ms(2);
			Value[4]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)

			Select_ADC0;		// Multiplexer input.
			//-------------------------------------
			// Send 12 bytes:
			//-------------------------------------
			put_word (Value[0]);	// Transmit RS-232 word.
			put_word (Value[1]);	// Transmit RS-232 word.
			put_word (Value[2]);	// Transmit RS-232 word.
			put_word (Value[3]);	// Transmit RS-232 word.
			put_word (Value[4]);	// Transmit RS-232 word.
			put_word (Value[5]);	// Transmit RS-232 word.
			//-------------------------------------
			ExtCommand = 0;		// Reset command.
			break;

		case T1_Cmd : 	goto PressCal;
			ExtCommand = 0;		// Reset command.
			break;

		case T2_Cmd  : 	goto CAL_END1;
			ExtCommand = 0;		// Reset command.
			break;
		}
		if (get_key() == STOP_KEY)	// Get keyboard commands.
			goto CAL_END1;
	}

	//-----------------------------------------------------------------
	// 4. Set 600mB Pressure:
	//    Wait for 600mB-Calibration-Ok:
	//-----------------------------------------------------------------

PressCal:
	Beep_Cnt = 100;
	//---------------------------------------------
	Pres_Pump_off;		// Stop Pressure-Pump!
	Pres_Valve_off;  	// Close drain valve!

	RegPressure = 600;	// 600 mBar
	CurrentADC = 0;		// Select internal pressure-sensor!
	P_Fine_Reg = 1;		// High accuracy regulation!
	ADC_Timer  = 16;	// Min 25mS, Conts down i regulate_pressure().
	IdleTimer  = 0;		// Counts up in Main(), 5 minutes, then low pressure!
	PressureTimeout = 0;	// Counts up in Timer1(), 120 sec timout (PressureAlarm).
	PressureOk = 0;

	ExtCommand = 0;		// Reset command.
	//---------------------------------------------

	while(1)
	{
		do { idle();}
		while (!Start_Flag);		// Stay in IDLE mode, wait for startflag.
		Start_Flag = 0;		// Ok, clear startflag (every 1mS).

		InterpretCommand();		// Check for seial commands
		regulate_pressure (RegPressure);	// Read pressure, set AlarmFlag, run Pump.

		if (PressureOk)
		{
			if (ADC_Timer == 16)
			{
				//-------------------------------------
				Value[0] = Press[0] + Param[0].PresOffset[0];	// Get back to ADC-Value
				Value[1] = Press[1] + Param[0].PresOffset[1];	// Get back to ADC-Value
				Value[2] = Press[2] + Param[0].PresOffset[2];	// Get back to ADC-Value
				Value[3] = Press[3] + Param[0].PresOffset[3];	// Get back to ADC-Value
				Value[4] = Press[4] + Param[0].PresOffset[4];	// Get back to ADC-Value
				Value[5] = PressureOk;
				//-------------------------------------
			}
		}
		else
		{
			if (ADC_Timer == 1)
			{
				//-------------------------------------
				Value[0] = Press[0];
				Value[1] = Press[0];
				Value[2] = Press[0];
				Value[3] = Press[0];
				Value[4] = Press[0];
				Value[5] = PressureOk;
				//-------------------------------------
			}
		}

		switch (ExtCommand)
		{
		case T0_Cmd:
			//-------------------------------------
			// Send 12 bytes:
			//-------------------------------------
			put_word (Value[0]);	// Transmit RS-232 word.
			put_word (Value[1]);	// Transmit RS-232 word.
			put_word (Value[2]);	// Transmit RS-232 word.
			put_word (Value[3]);	// Transmit RS-232 word.
			put_word (Value[4]);	// Transmit RS-232 word.
			put_word (Value[5]);	// Transmit RS-232 word (PressureOk).
			//-------------------------------------
			ExtCommand = 0;		// Reset command.
			break;

		case T1_Cmd:
		case T2_Cmd : 	goto CAL_END1;
			ExtCommand = 0;		// Reset command.
			break;
		}
		if (get_key() == STOP_KEY)	// Get keyboard commands.
			goto CAL_END1;
	}

CAL_END1:
	for (t=0; t<20; t++)
		InterpretCommand();		// Check for seial commands

	Beep_Cnt = 100;
	RegPressure = Param[0].DispPressure;
	P_Fine_Reg = 0;		// Not High accuracy regulation!
	ExtCommand = 0;			// Reset command.
}



//----------------------------------------------------------------------------
//- calibrate_disp_lift ()
//----------------------------------------------------------------------------
void calibrate_disp_lift (void)		// Calibration rutine in Calib.c!
{
	byte Serial[8];
	int  Comand, Pos0;
	int  PreScale = 0;	// Reset Prescale
	int  HeadNo = 1;
	int  LiftPos;		// Liftposition in mm.
	char LcdTxt[24];


	Beep_Cnt = 100;
	Timer_1mS = 100;	// 1/10 sec
	//---------------------------------------------
	Pres_Pump_off;		// Stop Pressure-Pump!
	Pres_Valve_off;  	// Close drain valve!
	ExtCommand = 0;		// Reset command.
	//---------------------------------------------
	lcd_puts (LCD1, "     Alignment!     ");
	lcd_puts (LCD2, "--------------------");
	lcd_puts (LCD3, "  DispLift:20.00mm  ");

	while(1)
	{
		do { idle();}
		while (!Start_Flag);		// Stay in IDLE mode, wait for startflag.
		Start_Flag = 0;		// Ok, clear startflag (every 1mS).

		InterpretCommand();		// Check for seial commands

		//--------------------------------------------------------------
		switch (ExtCommand)
		{
		case T0_Cmd:
			check_heads();
			//-------------------------------------
			// Send 11 bytes:
			//-------------------------------------
			put_word (Motor0_TachoPos);	// Transmit RS-232 word.
			put_word (Motor1_TachoPos);	// Transmit RS-232 word.
			put_word (Motor2_TachoPos);	// Transmit RS-232 word.
			put_char (Head1);	// Transmit RS-232 byte.
			put_char (Head2);	// Transmit RS-232 byte.
			put_char (Head3);	// Transmit RS-232 byte.
			put_char (Head4);	// Transmit RS-232 byte.
			put_char (Head5);	// Transmit RS-232 byte.
			//-------------------------------------
			ExtCommand = 0;		// Reset command.
			break;


		case T1_Cmd:      delay_ms(10);
			if (rec_bufcnt == 4)
			{
				Beep_Cnt = 50;
				Serial[0] = get_char();
				Serial[1] = get_char();
				Serial[2] = get_char();
				Serial[3] = get_char();
				Comand = ((int)Serial[0]<<8 | (int)Serial[1]);
				Pos0   = ((int)Serial[2]<<8 | (int)Serial[3]);
				//-------------------------------------
				if (Comand == 0)
				{
					while(Motor0_Busy);
					start_motor0 (70, Pos0);	// Run Plate!
				}
				else if (Comand == 1)
				{
					stop_motor1();
					start_motor1 (70, Pos0);	// Run Asp-lift!
					while(Motor1_Busy);
				}
				else if (Comand == 2)
				{
					stop_motor2();
					start_motor2 (70, Pos0);	// Run Disp-lift!
					while(Motor2_Busy);
				}
			}
			//-------------------------------------
			ExtCommand = 0;		// Reset command.
			break;

		case T2_Cmd:      Beep_Cnt = 50;
			HeadNo = 1;		// Disp-Lift!
			lcd_puts (LCD3, "  DispLift:  .  mm  ");
			//-------------------------------------
			ExtCommand = 0;		// Reset command.
			break;

		case T3_Cmd:      Beep_Cnt = 50;
			HeadNo = 5;		// Asp-Lift!
			lcd_puts (LCD3, "  Asp-Lift:  .  mm  ");
			//-------------------------------------
			ExtCommand = 0;		// Reset command.
			break;

		case T5_Cmd : 	goto CAL_END2;
			ExtCommand = 0;		// Reset command.
			break;
		}	// end switch()
		//--------------------------------------------------------------

		if (get_key() == STOP_KEY)	// Get keyboard commands.
			goto CAL_END2;

		//--------------------------------------------------------------
		//------- Run LIFT up/down by arrow-keys: ----------------------
		//--------------------------------------------------------------

		if (!Timer_1mS)	// A new 0.1sec value?
		{
			// DKM 112309 100 should be equal to 1/10, not 80.
			Timer_1mS = 80;	// 1/10 sec

			//------------------------------------------------
			// Write position to display (20.00mm):
			//------------------------------------------------

			if (HeadNo == 5)	// ASP
				LiftPos = (long)((long)Motor1_TachoPos*(long)10000L)/(long)Param[0].M1_Tacho;	// AspLift-position.
			else
				LiftPos = (long)((long)Motor2_TachoPos*(long)10000L)/(long)Param[0].M2_Tacho;	// DispLift-position.

			sprintf(LcdTxt, "%d.%02dmm ", LiftPos/100, LiftPos%100);
			lcd_puts (LCD3+11, LcdTxt);	// Write position!

			//------------------------------------------------
			// Check KeyPad:
			//------------------------------------------------
			if (Keypad_In == NO_KEY)    	// If no key pressed
			{
				PreScale = -2;	// Prescale

				if(Motor1_Busy)
				{
					stop_motor1();
					Motor1_PWM = 1;	// PWM-speed (0-100)
					Motor2_PWM = 1;	// PWM-speed (0-100)
					PWM1_Out (Motor1_PWM);	// Set Low Speed!
					PWM2_Out (Motor2_PWM);	// Set Low Speed!
				}

				if(Motor2_Busy)
				{
					stop_motor2();
					Motor1_PWM = 1;	// PWM-speed (0-100)
					Motor2_PWM = 1;	// PWM-speed (0-100)
					PWM1_Out (Motor1_PWM);	// Set Low Speed!
					PWM2_Out (Motor2_PWM);	// Set Low Speed!
				}
			}
			//------------------------------------------------
			if (Keypad_In == UP_KEY)    	// If UP-key pressed
			{
				if (HeadNo == 5)
				{
					if(!Motor1_Busy)
					{
						start_motor1 (2, Param[0].M1_HomePos);	// Up to HOME
						Motor1_PWM = 1;		// PWM-speed (0-100)
						Motor2_PWM = 1;		// PWM-speed (0-100)
						PWM1_Out (Motor1_PWM);
					}
				}
				else
				{
					if(!Motor2_Busy)
					{
						start_motor2 (2, Param[0].M2_HomePos);	// Up to HOME
						Motor1_PWM = 1;		// PWM-speed (0-100)
						Motor2_PWM = 1;		// PWM-speed (0-100)
						PWM2_Out (Motor2_PWM);
					}
				}

				if (PreScale++ >= 5)
				{
					if (Motor1_PWM <= 3)
					{
						PreScale = 0;	// Reset Prescale
						PWM1_Out (Motor1_PWM++);	// IncSpeed!
						PWM2_Out (Motor2_PWM++);	// IncSpeed!
					}
					else if (Motor1_PWM < 17)
					{
						PreScale = 5;	// Reset Prescale
						PWM1_Out (Motor1_PWM++);	// IncSpeed!
						PWM2_Out (Motor2_PWM++);	// IncSpeed!
					}
					else
						PreScale = 0;	// Reset Prescale
				}
			}
			//------------------------------------------------
			if (Keypad_In == DOWN_KEY)   // If DOWN-key pressed
			{
				if (HeadNo == 5)
				{
					if(!Motor1_Busy)
					{
						start_motor1 (2, 100);	// Asp: Down to 1mm
						Motor1_PWM = 1;		// PWM-speed (0-100)
						Motor2_PWM = 1;		// PWM-speed (0-100)
						PWM1_Out (Motor1_PWM);
					}
				}
				else
				{
					if(!Motor2_Busy)
					{
						start_motor2 (2, 500);	// Disp: Down to 5mm
						Motor1_PWM = 1;		// PWM-speed (0-100)
						Motor2_PWM = 1;		// PWM-speed (0-100)
						PWM2_Out (Motor2_PWM);
					}
				}

				if (PreScale++ >= 5)
				{
					if (Motor1_PWM <= 3)
					{
						PreScale = 0;		// Reset Prescale
						PWM1_Out (Motor1_PWM++);	// IncSpeed!
						PWM2_Out (Motor2_PWM++);	// IncSpeed!
					}
					else if (Motor1_PWM <= 17)
					{
						PreScale = 5;		// Reset Prescale
						PWM1_Out (Motor1_PWM++);	// IncSpeed!
						PWM2_Out (Motor2_PWM++);	// IncSpeed!
					}
					else
						PreScale = 0;	// Reset Prescale
				}
			}
		}
		//--------------------------------------------------------------

	}	// End while(1)

CAL_END2:
	start_motor1 (90, Param[0].M1_HomePos);	// Goto HOME (Microswitch) position.
	start_motor2 (90, Param[0].M2_HomePos);	// Goto HOME (Microswitch) position.
	while (Motor1_Busy || Motor2_Busy) {}

	start_motor0 (90, Param[0].M0_HomePos);	// Goto HOME (Microswitch) position.

}




//----------------------------------------------------------------------------
//- calibrate_carriage ()
//----------------------------------------------------------------------------
void calibrate_carriage (void)		// Calibration rutine in Calib.c!
{
	byte Serial[8];
	int  Comand, Pos0, Pos1, Pos2;
	int  PreScale = 0;	// Reset Prescale
	int  HeadNo = 1;
	int  PlatePos;		// Liftposition in mm.
	char LcdTxt[24];


	Beep_Cnt = 100;
	Timer_1mS = 100;	// 1/10 sec
	//---------------------------------------------
	Pres_Pump_off;		// Stop Pressure-Pump!
	Pres_Valve_off;  	// Close drain valve!
	ExtCommand = 0;		// Reset command.
	//---------------------------------------------
	lcd_puts (LCD1, "     Alignment!     ");
	lcd_puts (LCD2, "--------------------");
	lcd_puts (LCD3, "  PlatePos:20.00mm  ");

	while(1)
	{
		do { idle();}
		while (!Start_Flag);		// Stay in IDLE mode, wait for startflag.
		Start_Flag = 0;		// Ok, clear startflag (every 1mS).

		InterpretCommand();		// Check for seial commands

		//--------------------------------------------------------------
		switch (ExtCommand)
		{
		case T0_Cmd:
			check_heads();
			//-------------------------------------
			// Send 11 bytes:
			//-------------------------------------
			put_word (Motor0_TachoPos);	// Transmit RS-232 word.
			put_word (Motor1_TachoPos);	// Transmit RS-232 word.
			put_word (Motor2_TachoPos);	// Transmit RS-232 word.
			put_char (Head1);	// Transmit RS-232 byte.
			put_char (Head2);	// Transmit RS-232 byte.
			put_char (Head3);	// Transmit RS-232 byte.
			put_char (Head4);	// Transmit RS-232 byte.
			put_char (Head5);	// Transmit RS-232 byte.
			//-------------------------------------
			ExtCommand = 0;		// Reset command.
			break;


		case T1_Cmd:      delay_ms(10);
			if (rec_bufcnt == 4)
			{
				Beep_Cnt = 50;
				Serial[0] = get_char();
				Serial[1] = get_char();
				Serial[2] = get_char();
				Serial[3] = get_char();
				Comand = ((int)Serial[0]<<8 | (int)Serial[1]);	// Motor nr
				Pos0   = ((int)Serial[2]<<8 | (int)Serial[3]);	// Position
				//-------------------------------------
				if (Comand == 0)	// M0
				{
					while(Motor0_Busy);
					start_motor0 (70, Pos0);	// Run Plate!
					while(Motor0_Busy);
				}
				else if (Comand == 1)	// M1
				{
					while(Motor1_Busy);
					start_motor1 (70, Pos0);	// Run Asp-lift!
				}
				else if (Comand == 2)	// M2
				{
					while(Motor2_Busy);
					start_motor2 (70, Pos0);	// Run Disp-lift!
				}
			}
			//-------------------------------------
			ExtCommand = 0;		// Reset command.
			break;

		case T2_Cmd:      delay_ms(20);
			if (rec_bufcnt == 8)
			{
				Beep_Cnt = 50;
				Serial[0] = get_char();
				Serial[1] = get_char();
				Serial[2] = get_char();
				Serial[3] = get_char();
				Serial[4] = get_char();
				Serial[5] = get_char();
				Serial[6] = get_char();
				Serial[7] = get_char();
				Comand = ((int)Serial[0]<<8 | (int)Serial[1]);	// Motor nr
				Pos0   = ((int)Serial[2]<<8 | (int)Serial[3]);	// M0-Position
				Pos1   = ((int)Serial[4]<<8 | (int)Serial[5]);	// M1-Position
				Pos2   = ((int)Serial[6]<<8 | (int)Serial[7]);	// M2-Position
				//-------------------------------------
				if (Comand == 0)	// M0
				{
					while(Motor0_Busy);
					start_motor0 (70, Pos0);	// Run Plate!
					while(Motor0_Busy);
				}
				else if (Comand == 1)	// Auto Home
				{
					while(Motor0_Busy | Motor1_Busy | Motor2_Busy);
					start_motor1 (90, Param[0].M1_HomePos);	// Goto UP.
					start_motor2 (90, Param[0].M2_HomePos);	// Goto UP.

					while(Motor1_Busy | Motor2_Busy);
					start_motor0 (70, Pos0);	// Run Plate!
					while(Motor0_Busy);
				}
				else if (Comand == 2)	// Auto UP/DOWN
				{
					while(Motor0_Busy | Motor1_Busy | Motor2_Busy);
					start_motor1 (90, Pos1+300);	// Goto 3mm UP.
					start_motor2 (90, Pos2+300);	// Goto 3mm UP.

					while(Motor1_Busy | Motor2_Busy);
					start_motor0 (70, Pos0);	// Run Plate!

					while(Motor0_Busy);
					start_motor1 (70, Pos1);	// Goto Down Position.
					start_motor2 (70, Pos2);	// Goto Down Position.
				}
			}
			//-------------------------------------
			ExtCommand = 0;		// Reset command.
			break;

		case T5_Cmd : 	goto CAL_END3;
			ExtCommand = 0;		// Reset command.
			break;
		}	// end switch()
		//--------------------------------------------------------------

		if (get_key() == STOP_KEY)	// Get keyboard commands.
			goto CAL_END3;

		//--------------------------------------------------------------

		//--------------------------------------------------------------
		//------- Run CARRIAGE up/down by arrow-keys: ----------------------
		//--------------------------------------------------------------

		if (!Timer_1mS)	// A new 0.1sec value?
		{
			Timer_1mS = 80;	// 1/10 sec

			//------------------------------------------------
			// Write position to display (20.00mm):
			//------------------------------------------------

			PlatePos = (long)((long)Motor0_TachoPos*(long)10000L)/(long)Param[0].M0_Tacho;	// DispLift-position.

			sprintf(LcdTxt, "%d.%02dmm ", PlatePos/100, PlatePos%100);
			lcd_puts (LCD3+11, LcdTxt);	// Write position!

			//------------------------------------------------
			// Check KeyPad:
			//------------------------------------------------
			if (Keypad_In == NO_KEY)    	// If no key pressed
			{
				if(Motor0_Busy)
				{
					stop_motor0();
					Motor0_PWM = 0;	// PWM-speed (0-100)
					PWM0_Out (Motor0_PWM);	// Set Low Speed!
				}
			}
			//------------------------------------------------
			if (Keypad_In == UP_KEY)    	// If UP-key pressed
			{
				if(!Motor0_Busy)
				{
					start_motor0 (1, Pos0+500);	// Run Plate!
					Motor0_PWM = 1;		// PWM-speed (0-100)
					PWM0_Out (Motor0_PWM);
				}
			}
			//------------------------------------------------
			if (Keypad_In == DOWN_KEY)   // If DOWN-key pressed
			{
				if(!Motor0_Busy)
				{
					start_motor0 (1, Pos0-500);	// Run Plate!
					Motor0_PWM = 1;		// PWM-speed (0-100)
					PWM0_Out (Motor0_PWM);
				}
			}
		}
		//--------------------------------------------------------------

	}	// End while(1)

CAL_END3:
	start_motor1 (90, Param[0].M1_HomePos);	// Goto HOME (Microswitch) position.
	start_motor2 (90, Param[0].M2_HomePos);	// Goto HOME (Microswitch) position.
	while (Motor1_Busy || Motor2_Busy) {}

	start_motor0 (90, Param[0].M0_HomePos);	// Goto HOME (Microswitch) position.
}



//----------------------------------------------------------------------------
//-
//----------------------------------------------------------------------------
void calibrate_liquids (void)		// Calibration rutine in Calib.c!
{
	Beep_Cnt = 500;
	delay_ms(2000);
}



