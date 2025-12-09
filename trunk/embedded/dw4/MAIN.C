
//***********************************************************************
//* FILE        : main.c
//*
//* DESCRIPTION : main module (AquaMax DW4) (PCB rev. 2.0)
//*
//***********************************************************************
//*  <hw_init.h>   Parameter/adress/macro declarations
//*  <globdata.c>  Global data declarations
//*  <globdata.h>  Extern declarations to global data
//***********************************************************************

#include "globdata.h"
#include "keybrd.h"
#include "display.h"
#include "comm.h"
#include "motor.h"

//--------------------------------------------------------------------
//----- EXTERNAL FUNCTIONS -------------------------------------------
//--------------------------------------------------------------------

extern void motor0_test (void);
//---------------------------------------------
extern void init_all (void);			// Init hardware and funtion parameters.
extern void calibrate_positions (void);		// Calibration of motor positions.
extern byte uppdate_alarms (void);	// Read and uppdate Alarms!
extern void copy_CParam_to_Param (void);
extern int  read_eeprom_param (void);
extern int  write_eeprom_param (void);
extern void delay_ms (word time);
//---------------------------------------------
extern void I2C_Timing_Test (void);
//---------------------------------------------
extern void lcd_init (void);				// (display.c)
extern void lcd_putc (byte adr, byte data);		// (display.c)
extern void lcd_puts (byte adr, const char *str);	// (display.c)
extern void lcd_clrl (byte adr);
extern void lcd_adr (byte adr);				// (display.c)
extern void lcd_write (byte regsel, byte LCD_byte);
//---------------------------------------------
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
//---------------------------------------------
extern byte get_key(void);
extern void put_key(byte NewKey);	// Puts a key into the keyboard buffer

extern void InterpretCommand();		// Check serial commands
extern bool run_prgm(word *PrgmState);
extern bool run_service_prgm(word *PrgmState);
extern void regulate_pressure (int RegPressure);
extern void calibrate_pressure (void);

//---------------------------------------------
extern void get_PrgNameTxt(byte PrgmNo);
extern int  check_program (byte PrgmNo);
extern void check_heads(void);
extern void display_prog_error(void);
extern void send_error_reply(void);
extern void ProgramEdit(void);

extern void AquaMaxInfo(void);
//---------------------------------------------

#define ProgLine   LCD2		// LCD-line
#define PlateLine  LCD3		// LCD-line
#define StatusLine LCD4		// LCD-line

#define Err1Line   LCD3		// LCD-line
#define Err2Line   LCD4		// LCD-line

//-----------------------------------------------------------------------
//----- EXTERNAL DATA: --------------------------------------------------
//-----------------------------------------------------------------------
extern char PrgNameTxt[24];
extern char PlateNameTxt[24];
extern char PrgStepsTxt[24];

extern byte HeadCode1;	// Type of Head mounted! (check_heads())
extern byte HeadCode2;	// Type of Head mounted!
extern byte HeadCode3;	// Type of Head mounted!
extern byte HeadCode4;	// Type of Head mounted!
extern byte HeadCode5;	// Type of Head mounted!

extern byte Disp1;		// Commands in use in wanted program! (check_programs)
extern byte Disp2;		// Commands in use in wanted program!
extern byte Disp3;		// Commands in use in wanted program!
extern byte Disp4;		// Commands in use in wanted program!
extern byte Asp;		// Commands in use in wanted program!

extern byte ProgError;		// ErrorFlag for wanted program!  (check_programs)
extern word PrgSteps;		// Total number of steps (inkluding REP and LINK)

//-----------------------------------------------------------------------
//----- GLOBAL DATA: --------------------------------------------------
//-----------------------------------------------------------------------

byte Reply_PZ;	// PlateTooHigh-RobotReplyFlag;
byte Reply_PL;	// NoPlate-RobotReplyFlag;
byte Reply_END;	// ProgramEnd-RobotReplyFlag;


//**************************************************************************
// Sub-rutine:
//**************************************************************************

void reply (far char *str)
{
	if (Reply_PZ)
	{
		put_s(PZ_ALARM_ACK);	// send PlateSize-Error-Ack back to PC
		Reply_PZ = 0;
	}
	else if (Reply_PL)
	{
		put_s(PL_ALARM_ACK);	// send NoPlate-Error-Ack back to PC
		Reply_PL = 0;
	}
	else if (Reply_END)
	{
		put_s(FINNISHED_ACK);	// Program END message.
		Reply_END = 0;
	}
	else
		put_s(str);	// Normal wanted message.
}


//**************************************************************************
// Sub-rutine:
//**************************************************************************
void check_robot_pl_alarm (void)
{
	InterpretCommand();  //Check for seial commands
	switch (ExtCommand)
	{
	case STATUS_Cmd:
	case START_Cmd :
	case STOP_Cmd  :
	case PROG_Cmd  : reply(BUSY_ACK);  	// Return BUSY-status to PC.
		ExtCommand = 0;		// Reset command.
		break;
	}
}


/**************************************************************************
*  FUNCTION    : Main
*  METHODE     : Initialize and then run main program
*  AUTHOR      : HJ 96-08
*  L. MODFIED  :
***************************************************************************/

void main(void)
{
	int t;
	byte Status;
	byte NewKey;
	byte StopFlag = 0;		// Stop-Flag
	byte NewAlarms = 0;		// Get alarm input port.
	byte OldAlarms = 0;		// change in alarm inputs.
	byte NewProgError = 0;		// Get program-errors.
	byte OldProgError = 0;		// change program-errors.

	Beep_Cnt = 0;
	clr_beep;

	Reply_PZ  = 0;	// Clear PlateTooHigh-Flag;
	Reply_PL  = 0;	// Clear NoPlate-Flag;
	Reply_END = 0;	// Clear ProgramEnd-Flag;

	//-----------------------------------------------
	// Init hardware and parameters:
	//-----------------------------------------------

	Status = 0;
	if (read_eeprom_param () != 0)		// Read EEPROM ok?
		Status = 0xC1;
	else if (Param[0].PromCode != 0xAA55A55AL)	// Data ok?
		Status = 0xC2;

	if (Status != 0)
	{
		copy_CParam_to_Param ();		// Copy from Flash to user Parameters!
		Param[0].PromCode = 0L;		// Set EEPROM-error! (Default values).
	}

	//-----------------------------------------------

	init_all();	// Init hardware and funktion parameters!
	lcd_init ();	// Init LCD-display

	//-----------------------------------------------

	if (Status != 0)
	{
		if (Status == 0xC1)
		{
			lcd_puts (LCD1, "--- AQUAMAX DW4 ----");
			lcd_clrl (LCD2);
			lcd_puts (LCD3, "EEPROM Read Error:  ");
			lcd_puts (LCD4, "->Default Parameters");
		}
		if (Status == 0xC2)
		{
			lcd_puts (LCD1, "--- AQUAMAX DW4 ----");
			lcd_clrl (LCD2);
			lcd_puts (LCD3, "EEPROM Data Error:  ");
			lcd_puts (LCD4, "->Default Parameters");
		}
		Beep_Cnt = 2000;
		delay_ms(2000);
	}
	else
	{
		lcd_puts (LCD1, "    AQUAMAX DW4     ");
		lcd_puts (LCD2, "    96/384/1536     ");
		lcd_puts (LCD3, "  Molecular Devices ");
		lcd_puts (LCD4, "  Copyright c 2003  ");
		delay_ms(1000);
	}

	MiniPrimeVol = 0;			// Robot-MiniPrime-Volume! (default = 0 ul)

	lcd_clrl (LCD1);
	lcd_clrl (LCD2);
	lcd_clrl (LCD3);
	lcd_clrl (LCD4);
	delay_ms (50);

	lcd_puts (LCD1, "     Firmware:      ");
	lcd_puts (LCD2, VerTxt);
	lcd_puts (LCD3, "     Programs:      ");
	lcd_puts (LCD4, ProgInfo[0].FileName);
	delay_ms (500);
	//-----------------------------------------------
#ifdef __TestBoard
	// No motor init!
#else
	init_motor1_home_position ();	// Asp-lift
	init_motor2_home_position ();	// Disp-lift
	init_motor0_home_position ();	// Carriage
#endif
	//-----------------------------------------------
	delay_ms (100);	// 100mS delay

	lcd_clrl (LCD1);	// Clear line 1
	lcd_clrl (LCD2);	// Clear line 2
	lcd_clrl (LCD3);	// Clear line 3
	lcd_clrl (LCD4);	// Clear line 4
	delay_ms (50);	// 50mS delay


	//-----------------------------------------------
	// Check for Pressure Calibration:
	//-----------------------------------------------

	Status = Keypad_In;	// Read keyboard-inputs.

	if (Status == (START_KEY | STOP_KEY))	 // If a both key's are pressed.
	{
		lcd_puts (LCD1, "--------------------");
		lcd_puts (LCD2, "- Pressure Sensor  -");
		lcd_puts (LCD3, "-   Calibration    -");
		lcd_puts (LCD4, "--------------------");

		while(Keypad_In)	// Wait for keys to be released!
		{
			Beep_Cnt = 150;		// Set a 150mS beep (timer, in background)
			delay_ms(300);		// Wait here for 300mS
		}

		lcd_puts (LCD1, "--------------------");
		lcd_puts (LCD2, "Pressure Calibration");
		lcd_puts (LCD3, " (press ENTER-key)  ");
		lcd_puts (LCD4, "--------------------");


		Status = get_key();	// Empty key buffer (dummy read)
		Status = get_key();	// Empty key buffer (dummy read)

		while(Status == 0)		// Wait for a new key press!
		{
			Status = get_key();
		}


		if (Status == ENTER_KEY)
			calibrate_pressure ();	// Calibration rutine in Pressure.c!
	}


	//-----------------------------------------------
	// Init program start:
	//-----------------------------------------------

	//Pressure regulator:
	CurrentPres  = Param[0].DispPressure;
	RegPressure = CurrentPres;
	CurrentADC = 0;		// Select internal pressure-sensor!
	ADC_Timer = 40;	// Min 25mS, Conts down i regulate_pressure().
	IdleTimer = 0;	// Counts up in Main(), 5 minutes, then low pressure!
	IdleFlag = 1;
	PressureTimeout = 0;	// Counts up in Timer1(), 120 sec timout (PressureAlarm).
	PlateAlarm = 0;


	//---------------------------
	// Programs:
	//---------------------------
	Wanted_Prgm = 1;	// Find first available program:
	while (Wanted_Prgm <= MAX_SUBPROGRAMS && Program[Wanted_Prgm-1].Command[0] == 0) // Any program here?
		Wanted_Prgm++;			// min. 1 special program must exist!
	//---------------------------
	PrgmState = 0;	// Init-state of program to be run.
	MainState = 0;	// Init main state.
	RunningState = 0;	// No action do be done.
	DispState = 0xff;	// Uppdate Display! (DispState != MainState).
	//---------------------------

	Beep_Cnt = 20;	// Start-up beep's
	delay_ms(100);
	Beep_Cnt = 20;
	delay_ms(100);
	Beep_Cnt = 20;




	//  motor0_test ();
	//  I2C_Timing_Test ();


	//-----------------------------------------------------------------------------
	// MAIN-LOOP:
	//-----------------------------------------------------------------------------

	while(1)
	{
		// DKM 112309 idle() must be in the Intel library because I can't seem to find it
		//            in this source code.
		// ah.  here it is, from http://www.milton.arachsys.com/nj71/pdf/Roland/dev96/taskingC196manual.pdf
		// idle() apparently sleeps the processor until an interrupt occurs.  So I guess the timer
		// wakes up the processor so it can check Start_Flag.
		do { idle();}
		while (!Start_Flag);	// Stay in IDLE mode, wait for startflag (set by timer every 1mS).
		Start_Flag = 0;		// Ok, clear startflag (1mS).


		//----------------------------------------------------
		//   Regulate pressure:
		//----------------------------------------------------

		if ((DispFlag == 1) || PressureAlarm || StopFlag || PlateAlarm)
		{
			Pres_Pump_off;	// Stop Pressure-Pump!
			Pres_Valve_off;	// Close Pressue-Valve!
		}
		else
			regulate_pressure (RegPressure);	// Read pressure, set AlarmFlags, run Pump.


		//----------------------------------------------------
		//   Check alarm inputs:
		//----------------------------------------------------

		if (PlateAlarm)		// PlateAlarm?
			MainState = 0;		// Uppdate leds, find new state.


		if (UppdateFlag || MainState == 0)	// (UppdateFlag every 1 Sec)
		{
			UppdateFlag = 0;	// Clear 1sec flag.

			//-------------------------------------------------
			OldAlarms = NewAlarms;
			NewAlarms = uppdate_alarms ();	// Get alarm-port

			if (OldAlarms != NewAlarms)	// Any change in alarms?
				MainState = 0;		// Uppdate leds, find new state.
			//-------------------------------------------------
			OldProgError = NewProgError;
			NewProgError = check_program (Wanted_Prgm);  	// Check selected program (errors)

			if (OldProgError != NewProgError)	// Any change in alarms?
				MainState = 0;		// Uppdate leds, find new state.
			//-------------------------------------------------
		}

		//----------------------------------------------------
		//   4 main states:
		//----------------------------------------------------
		//   0 - Uppdate state, check alarms, udddate states!
		//   1 - Idle state, PlateAlarm only!
		//   2 - Idle state, Alarms!
		//   3 - Idle state, No alarms
		//
		//  10 - Running state, No alarms
		//
		//  20 - FLASH-Reprogram Idle State, WAIT, dont start programs
		//  30 - STOP Idle State, WAIT, dont start programs
		//----------------------------------------------------

		switch(MainState)
		{
			//-------------------------------------------------------------------------------------
		case 0 : /* Init Main State */
			//-------------------------------------------------------------------------------------
			DispState = 0xffff;	// Uppdate display!

			if (RunningState == 0)	// Not running!
			{
				//---------------------------------------------------------
				//-- Check Idle Sleep timer: ------------------------------
				//---------------------------------------------------------

				CurrentADC = 0;		// Internal pressure-sensor!
				P_Fine_Reg = 0;		// Not High accuracy regulation!

				if (IdleTimer >= Param[0].MaxIdleTime)	// Idle (zero) pressure?
				{
					RegPressure = IDLE_PRESSURE;	// Yes, set regulated pressure to zero,
					IdleTimer = 0;			// and reset Idle-timer.
				}

				//---------------------------------------------------------
				//-- Check selected program: ------------------------------
				//---------------------------------------------------------

				get_PrgNameTxt(Wanted_Prgm);

				//---------------------------------------------------------
				//-- Check alarms, uppdate MainState: ---------------------
				//---------------------------------------------------------

				if (PlateAlarm)	// Microplate Alarm?
					PlateAlarm = 0;	// No PlateAlarm in Idle State!

				//---------------------------------------------------------

				if (NewProgError)	// Alarms?
					MainState = 1;	// -> Alarm-State, idle!
				else			// Not Alarms:
					MainState = 5;	// -> Ok-State, idle!

				if (RegPressure == IDLE_PRESSURE)
					MainState += 1;	// IdlePressure-state!
				//---------------------------------------------------------

				if (StopFlag == 1)	// STOP-mode?:
				{
					MainState = 30;	// Wait for next STOP-key!
					break;
				}
			}	// End Idle.

			else
			{	// Program is running:

				//---------------------------------------------------------
				//-- Check alarms, uppdate LEDs: --------------------------
				//---------------------------------------------------------

				MainState = 10;		// Program is running.

				if (WasteError)
					MainState = 12;	// Waste tank full!

				if (PlateAlarm)	// Plate Error?
				{
					Vac_Pump_off;		// Pump OFF!
					Asp_Pump_off;		// Pump OFF!
					Beep_Cnt = 1200;		// A loong beep.

					if (PlateAlarm == 1)	// No Plate?
					{
						soft_stop_motor0 ();	// Stop motor0 (de-accelerate).
						lcd_puts (LCD4, "Error: No Plate!    ");

						if (ExtStart == 1)	// Flag (robot-start)
							Reply_PL  = 1;	// Set NoPlate-ReplyFlag;
					}
					else		// Plate too High!
					{
						stop_motor0 ();		// Stop motor0 immedeately.
						lcd_puts (LCD4, "Error: Plate High!  ");
						if (ExtStart == 1)	// Flag (robot-start)
							Reply_PZ  = 1;	// Set PlateTooHigh-ReplyFlag;
					}

					while (Motor0_Busy || Motor1_Busy || Motor2_Busy) 	// Wait if motors are running!
						check_robot_pl_alarm ();

					start_motor1 (70, Param[0].M1_HomePos);	// Run Asp-lift up!
					start_motor2 (70, Param[0].M2_HomePos);	// Run Disp-lift up!
					while (Motor2_Busy) 	// Wait if motor is running!
						check_robot_pl_alarm ();

					start_motor0 (70, Param[0].M0_HomePos);	// Goto Home-position.
					while (Motor0_Busy) 	// Wait if motor is running!
						check_robot_pl_alarm ();

					PlateAlarm = 0;
					IdleTimer = 0;
					PrgmState = 0;	// Init state for running a program!
					RunningState = 0;	// STOPP program!
					MainState = 0;	// Uppdate display, init state.
					ExtStart = 0;	// Reset flag.
					break;		// End this state!
				}

				if (PressureAlarm)	// Pressure-timeout alarm?
				{
					RunningState = 100;	// STOPP program!
					break;			// End this state!
				}

			}	// End program is running.
			break;	// End case 0!


			//-------------------------------------------------------------------------------------
		case 1 :  /* Alarms: (not running) (not PressureIdle) Dont start programs */
			//-------------------------------------------------------------------------------------
			if (IdleTimer >= Param[0].MaxIdleTime) 	// 2 minutes idle?
				MainState = 0;			// Go into Idle-sleep State!

			if(LED_Off)	// Flash error-message:
			{
				LED_Off = 0;
				lcd_clrl (LCD4);
			}
			if(LED_On)	// Flash error-message:
			{
				LED_On = 0;
				display_prog_error();
			}

			NewKey = get_key();
			switch (NewKey)
			{
			case DOWN_KEY : RunningState = 10;  break;	// Select program
			case UP_KEY   : RunningState = 15;  break;	// Select program
			case START_KEY: RunningState = 1;   break;	// Reset IdleTimer (Nothing else to be done)!
			case STOP_KEY : RunningState = 110; break;	// STOPP+Init.

			case ENTER_KEY : ProgramEdit();	// Edit Program Parameters!
				MainState = 0;	// Uppdate Display.
				break;
			case CANCEL_KEY: AquaMaxInfo();	// Show Info-screen!
				MainState = 0;	// Uppdate Display.
				break;

			}
			switch (ExtCommand)
			{
			case START_Cmd :
			case STATUS_Cmd: send_error_reply();	// Send current Error-string!
				break;
			case PROG_Cmd  : RunningState = 5;		// Select program
				break;
			case STOP_Cmd  : RunningState = 210;	// STOPP+Init.
				break;
			}
			ExtCommand = 0;				// Reset command.
			break;

			//-------------------------------------------------------------------------------------
		case 2 :  /* Alarms + PressureIdle: (not running) Dont start programs */
			//-------------------------------------------------------------------------------------

			if(LED_Off)	// Flash error-message:
			{
				LED_Off = 0;
				lcd_clrl (LCD4);
			}
			if(LED_On)	// Flash error-message:
			{
				LED_On = 0;
				display_prog_error();
			}

			NewKey = get_key();
			switch (NewKey)
			{
			case START_KEY : RunningState = 1;   break;	// Reset IdleTimer (Nothing else to be done)!
			case DOWN_KEY  : RunningState = 10;  break;	// Select program
			case UP_KEY    : RunningState = 15;  break;	// Select program
			case STOP_KEY  : RunningState = 110; break;	// STOPP+Init.

			case ENTER_KEY : ProgramEdit();	// Edit Program Parameters!
				MainState = 0;	// Uppdate Display.
				break;
			case CANCEL_KEY: AquaMaxInfo();	// Show Info-screen!
				MainState = 0;	// Uppdate Display.
				break;
			}
			switch (ExtCommand)
			{
			case START_Cmd :
			case STATUS_Cmd: send_error_reply(); break;	// Send current Error-string!
			case PROG_Cmd  : RunningState = 5;	  break;	// Select program
			case STOP_Cmd  : RunningState = 210; break;	// STOPP+Init.
			}
			ExtCommand = 0;				// Reset command.
			break;

			//-------------------------------------------------------------------------------------
		case 5 : /* OkState: (not running, no alarms) Get new commands */
			//-------------------------------------------------------------------------------------
			if (IdleTimer >= Param[0].MaxIdleTime) 	// 2 minutes idle?
				MainState = 0;			// Go into Idle-sleep State!

			NewKey = get_key();	// Get keyboard commands.
			switch (NewKey)
			{
			case DOWN_KEY  : RunningState = 10;  break;	// Select program
			case UP_KEY    : RunningState = 15;  break;	// Select program
			case START_KEY : RunningState = 20;  break; // Start current program.
			case STOP_KEY  : RunningState = 110; break;	// STOPP+Init.

			case ENTER_KEY : ProgramEdit();	// Edit Program Parameters!
				MainState = 0;	// Uppdate Display.
				break;
			case CANCEL_KEY: AquaMaxInfo();	// View Info-screen!
				MainState = 0;	// Uppdate display, init state.
				break;
			}
			switch (ExtCommand)
			{
			case STATUS_Cmd: reply(READY_ACK);  break;	// Return status to PC.
			case PROG_Cmd  : RunningState = 5;  break;	// Select program
			case START_Cmd : RunningState = 20;	// Start Program
				ExtStart = 1;		// Flag (robot-start)
				reply(OK_ACK);		// send ACK back to PC
				break;
			case STOP_Cmd  : RunningState = 210; break;	// STOPP+Init.
			}
			ExtCommand = 0;				// Reset command.
			break;



			//-------------------------------------------------------------------------------------
		case 6 : /* OkState + PressureIdle: (not running, no alarms) Get new commands */
			//-------------------------------------------------------------------------------------
			if (IdleTimer >= Param[0].MaxIdleTime) 	// 20 minutes idle?
				MainState = 0;	// Go into Idle-sleep State!

			NewKey = get_key();
			switch (NewKey)
			{
			case DOWN_KEY  : RunningState = 10;  break;	// Select program
			case UP_KEY    : RunningState = 15;  break;	// Select program
			case START_KEY : RunningState = 20;  break; 	// Start current program.
			case STOP_KEY  : RunningState = 110; break;	// STOPP+Init.

			case ENTER_KEY : ProgramEdit();	// Edit Program Parameters!
				MainState = 0;	// Uppdate Display.
				break;
			case CANCEL_KEY: AquaMaxInfo();	// View Info-screen!
				MainState = 0;	// Uppdate display, init state.
				break;
			}
			switch (ExtCommand)
			{
			case STATUS_Cmd: reply(IDLE_ACK);	// Return IDLE-status to PC.
				break;
			case PROG_Cmd  : RunningState = 5;  break;	// Select program
			case START_Cmd : RunningState = 20;	// Start Program
				ExtStart = 1;		// Flag (robot-start)
				reply(OK_ACK);		// send ACK back to PC
				break;
			case STOP_Cmd  : RunningState = 210; break;	// STOPP+Init.
			}
			ExtCommand = 0;				// Reset command.
			break;



			//-------------------------------------------------------------------------------------
		case 10 : /* Running State, Programs, Get new commands */
			//-------------------------------------------------------------------------------------

			if (WasteError)
				MainState = 12;	// PresError!

			if (PlateErr_SW)	// Plate too high?
			{
				if (Motor0_Busy)
				{
					PWM0_Out (0x00);	// Speed = 0!
					PlateAlarm = 2;	// Set alarmflag!
					MainState = 0;	// Uppdate!
					break;
				}
			}

			if (PressureError)
				MainState = 11;	// PresError!

			IdleTimer = 0;
			switch (get_key())
			{
			case DOWN_KEY : break;			// Do not select program!
			case UP_KEY   : break;			// Do not select program!
			case START_KEY: break;			// Allready running!

			case STOP_KEY : if (Wanted_Prgm != SERVICE_PROG9)
								RunningState = 100;
				break;	// STOPP.
			}
			switch (ExtCommand)
			{
			case STATUS_Cmd:
			case PROG_Cmd  :
			case START_Cmd : reply(BUSY_ACK);  break;	// Return BUSY-status to PC.
			case STOP_Cmd  : RunningState = 200; break;	// STOPP.
			}
			ExtCommand = 0;				// Reset command.
			break;

			//-------------------------------------------------------------------------------------
		case 11 : /* Running State, and Pressure Sensor Error! */
			//-------------------------------------------------------------------------------------

			if(LED_Off)
			{
				LED_Off = 0;
				lcd_clrl (LCD4);
				Beep_Cnt = 20;
			}
			if(LED_On)
			{
				LED_On = 0;
				if      (SensorError == 1) lcd_puts (LCD4, "Err:Liq.Press(-100)!");
				else if (SensorError == 2) lcd_puts (LCD4, "Err:Liq.Press(+100)!");
				else if (SensorError == 3) lcd_puts (LCD4, "Err:Liq.Press(high)!");
				else 		      lcd_puts (LCD4, "Err:Liquid Pressure!");
				Beep_Cnt = 20;
			}

			if (PlateErr_SW)	// Plate too high?
			{
				if (Motor0_Busy)
				{
					PWM0_Out (0x00);	// Speed = 0!
					PlateAlarm = 2;	// Set alarmflag!
					MainState = 0;	// Uppdate!
					break;
				}
			}

			if (!PressureError)
			{
				MainState = 10;	// Not PresError!
				P_Fine_Reg = 2;	// Medium accuracy regulation!
				PressureOk = 0;	// Not ok yet!
				lcd_clrl (LCD4);
			}

			IdleTimer = 0;
			switch (get_key())
			{
			case DOWN_KEY : break;			// Do not select program!
			case UP_KEY   : break;			// Do not select program!
			case START_KEY: break;			// Allready running!
			case STOP_KEY : RunningState = 100; break;	// STOPP.
			}
			switch (ExtCommand)
			{
			case STATUS_Cmd:
			case PROG_Cmd  :
			case START_Cmd : reply(PS_ALARM_ACK);  break;	// Return BUSY-status to PC.
			case STOP_Cmd  : RunningState = 200; break;	// STOPP.
			}
			ExtCommand = 0;				// Reset command.
			break;

			//-------------------------------------------------------------------------------------
		case 12 : /* Running State, and WasteFull Error! */
			//-------------------------------------------------------------------------------------

			if(LED_Off)
			{
				LED_Off = 0;
				lcd_clrl (LCD3);
				lcd_clrl (LCD4);
				Beep_Cnt = 20;
			}
			if(LED_On)
			{
				LED_On = 0;
				if (WasteAlarm)
					lcd_puts (LCD3, "Error: Waste Alarm! ");
				lcd_puts (LCD4, " (ENTER to proceed) ");
				Beep_Cnt = 20;
			}

			IdleTimer = 0;
			switch (get_key())
			{
				//		     case START_KEY:
			case ENTER_KEY:
				if (!WasteAlarm)
				{
					lcd_clrl (LCD3);
					lcd_clrl (LCD4);
					MainState = 10;	// Not WasteAlarm!
					P_Fine_Reg = 2;	// Medium accuracy regulation!
					PressureOk = 0;	// Not ok yet!
					WasteError = 0;	// Not WasteAlarm!
					Waste_Pump_on;	// Empty tray (start vacuum pump)
				}
				break;			// Allready running!

			case STOP_KEY : lcd_clrl (LCD3);
				lcd_clrl (LCD4);
				WasteError = 0;	// Not WasteAlarm!
				RunningState = 100; break;	// STOPP.
			}
			switch (ExtCommand)
			{
			case STATUS_Cmd:
			case PROG_Cmd  :
			case START_Cmd :
			case STOP_Cmd  : reply(BUSY_ACK);  break;	// Return BUSY-status to PC.
			}
			ExtCommand = 0;				// Reset command.
			break;

			//-------------------------------------------------------------------------------------
		case 20:  /* Go into FLASH-Reprogram Idle State */
			//-------------------------------------------------------------------------------------
			MainState = 21;
			break;

		case 21:  /* FLASH-Reprogram Idle State, WAIT, dont start programs */
			break;

		case 22:  /* Release from reprogramming: */
			Beep_Cnt = 20;
			delay_ms(100);
			Beep_Cnt = 20;
			delay_ms(100);
			Beep_Cnt = 20;
			lcd_clrl (LCD2);
			lcd_clrl (LCD3);
			lcd_puts (LCD4, "--->>> RESET! <<<---");

			ADC_Timer = 40;
			PressureTimeout = 0;		// 120 sec timout (PressureAlarm)
			PressureAlarm = 0;
			P_Fine_Reg = 0;			// Not High accuracy regulation!
			IdleTimer = 0;
			PlateAlarm = 0;
			PrgmState = 0;	// Not running a program!

			init_motor1_home_position ();	// Asp-lift
			init_motor2_home_position ();	// Disp-lift
			init_motor0_home_position ();	// Carriage

			if (Program[Wanted_Prgm-1].Command[0] == 0) // Any program here?
		 {
			 Wanted_Prgm = 1;	// Find first available program:
			 while (Wanted_Prgm <= MAX_SUBPROGRAMS && Program[Wanted_Prgm-1].Command[0] == 0) // Any program here?
				 Wanted_Prgm++;			// min. 1 program must exist!
		 }
			MainState = 0;	// Init main state.
			break;

			//-------------------------------------------------------------------------------------
		case 30:  /* STOP Idle State, WAIT, dont start programs */
			//-------------------------------------------------------------------------------------
			if(LED_Off)
			{
				LED_Off = 0;
				lcd_puts (LCD4, "----            ----");
			}
			if(LED_On)
			{
				LED_On = 0;
				lcd_puts (LCD4, "---- STOP-MODE! ----");
			}

			switch (get_key())
		 {
			case DOWN_KEY : break;			// Do not select program!
			case UP_KEY   : break;			// Do not select program!
			case START_KEY: break;			// DO NOT start (STOP).
			case STOP_KEY : RunningState = 100; break;	// STOPP.
		 }
			switch (ExtCommand)
		 {
			case STATUS_Cmd:
			case PROG_Cmd  :
			case START_Cmd : reply(STOP_ACK);  break;		// STOPP-Idle
			case STOP_Cmd  : RunningState = 200; break;	// STOPP.
		 }
			ExtCommand = 0;				// Reset command.
			break;

		}		// End switch(MainState)

		//------------------------------------------------------------------------------

		//---------------------------------------------------------
		// Uppdate Display:
		//---------------------------------------------------------

		if (DispState != MainState)
		{
			DispState = MainState;

			if(MainState > 0 && MainState < 10)	// Idle-states!
			{
				lcd_puts (LCD1, "--- AQUAMAX DW4 ----");
				lcd_puts (LCD2, PrgNameTxt);			// Program Name
			}

			switch(MainState)
			{

			case 1 :  /* Alarm-State, (not running): */
				lcd_puts (LCD3, PlateNameTxt);	// Plate Name
				break;

			case 2 :  /* Alarms + IdlePressure State, (not running): */
				lcd_puts (LCD3, "--- Idle Pressure --");
				break;

			case 5 : /* Ok-Idle State, (not running): */
				lcd_puts (LCD3, PlateNameTxt);	// Plate Name
				lcd_puts (LCD4, PrgStepsTxt);		// Number of prog.steps.
				break;

			case 6 : /* Ok-Idle + IdlePressure State, (not running): */
				lcd_puts (LCD3, PlateNameTxt);	// Plate Name
				lcd_puts (LCD4, "--- Idle Pressure --");
				break;

			case 10 : /* Running State: */
				//		  lcd_clrl (LCD4);
				break;

			case 11 : /* Running State + PressureSensor Error: */
				lcd_puts (LCD4, "Err:Liquid Pressure!"); break;
				break;

			case 20:  /* FLASH-Reprogram Idle State, WAIT, dont start programs */
				break;

			case 30:  /* STOP Idle State, WAIT, dont start programs */
				break;
			}

		}    //-- Uppdate Display Finnished! ---------------------------


		//------------------------------------------------------------------------------

		InterpretCommand();  //Check for seial commands

		//------------------------------------------------------------------------------

		switch(RunningState)
		{
		case  0 : break; // Nothing to be done!

		case  1 :  // Reset IdleTimer (Nothing else to be done)!
			IdleTimer = 0;		// Reset IdleTimer!
			Pres_Valve_off;  	// Close drain valve!
			RegPressure = CurrentPres;	// Set normal pressure!
			MainState = 0;		// Uppdate display, init state.
			RunningState = 0;	// Done
			break;

		case  5 : /*  ExtCommand: SELECT PROGRAM */
			if (ExtComVal >= 1 && ExtComVal <= 99)
		 {
			 if (Program[ExtComVal-1].Command[0] != 0) // Any program here?
			 {
				 Wanted_Prgm = ExtComVal;
				 put_s(OK_ACK);		// send ACK back to PC
			 }
		 }
			else if (ExtComVal >= 100 && ExtComVal <= 103)	// Special programs
		 {
			 Wanted_Prgm = ExtComVal;
			 put_s(OK_ACK);		// send ACK back to PC
		 }

			if (Wanted_Prgm != ExtComVal)
				put_s(PROG_ERROR_ACK);		// send ACK back to PC

			//---------------------------
			RunningState = 1;	// Done, reset IdleTimer!
			break;

			//------------------------------------------------------------------------------

		case 10 : /* DOWN_KEY pressed */
			if (Wanted_Prgm > 1)
				Wanted_Prgm--;
			else
				Wanted_Prgm = MAX_PROGRAMS;

			//---------------------------
			if (Wanted_Prgm <= MAX_SUBPROGRAMS)	// Only check Programs (not Special programs)
		 {
			 while (Wanted_Prgm >= 1 && Program[Wanted_Prgm-1].Command[0] == 0) // Any program here?
				 Wanted_Prgm--;
		 }
			if (Wanted_Prgm == 0)
				Wanted_Prgm = MAX_PROGRAMS;	// min. 1 special program must exist!
			//---------------------------
			ExtComVal2 = 0;	// Not Robot-Prime!
			RunningState = 1;	// Done, reset IdleTimer!
			break;

		case 15 : /* UP_KEY pressed */
			if (Wanted_Prgm < MAX_PROGRAMS)
				Wanted_Prgm++;
			else
				Wanted_Prgm = MIN_SUBPROGRAMS;

			//---------------------------
			while (Wanted_Prgm <= MAX_SUBPROGRAMS &&
				Program[Wanted_Prgm-1].Command[0] == 0) // Any program here?
				Wanted_Prgm++;			// min. 1 special program must exist!
			//---------------------------

			ExtComVal2 = 0;	// Not Robot-Prime!
			RunningState = 1;	// Done, reset IdleTimer!
			break;

		case 20 : /* START_KEY pressed */
			IdleTimer = 0;		// Reset IdleTimer!
			Pres_Valve_off;  	// Close drain valve!
			RegPressure = CurrentPres;	// Set normal pressure!
			WasteError = 0;	// Not WasteAlarm!

			PrgmState = 0;		/* init state for running a program */
			MainState = 10;	/* Set main-state for running a program */

			if (Wanted_Prgm <= MAX_SUBPROGRAMS)	// Only check Programs (not Special programs)
				RunningState = 25;	/* Run this state until reset to 0 */
			else
				RunningState = 26;	/* Run this state until reset to 0 */
			break;

		case 25 : /* running a user-program: */
			if (run_prgm(&PrgmState) == FINNISHED)
		 {
			 if (ExtStart == 1)		// Flag (robot-start)
			 {
				 ExtStart = 0;		// Reset flag.
				 Reply_END = 1;		// Set ProgramEnd-ReplyFlag;
				 //			reply(FINNISHED_ACK);	// send ACK back to PC
			 }
			 IdleTimer = 0;
			 RunningState = 0;	// Done
			 MainState = 0;	// Uppdate display, init state.

			 //--------------------------------------------------------
			 //	 if (Wanted_Prgm == 10)
			 //	     RunningState = 20;	// TEST! Continious run!
			 //--------------------------------------------------------
		 }
			break;


		case 26 : /* running an internal service-program: */
			if (run_service_prgm(&PrgmState) == FINNISHED)
		 {
			 if (ExtStart == 1)		// Flag (robot-start)
			 {
				 ExtStart = 0;		// Reset flag.
				 Reply_END = 1;		// Set ProgramEnd-ReplyFlag;
				 //			reply(FINNISHED_ACK);	// send ACK back to PC
			 }
			 IdleTimer = 0;
			 RunningState = 0;	// Done
			 MainState = 0;	// Uppdate display, init state.
		 }
			break;


		case 100: // STOP_KEY pressed while running:
		case 200: // STOP_Cmd:
			if (StopFlag == 0)	// STOP:
		 {
			 StopFlag = 1;

			 Disp_Valve_off;	// Close DISP-valve.
			 Pres_Pump_off;	// Stop Pressure-Pump!
			 Asp_Pump_off;	// Stop Vacuum-Pump!
			 Vac_Pump_off;	// Stop Vacuum-Pump!
			 Waste_Pump_off;	// Stop Waste-Pump!
			 Pres_Valve_off;	// Close Pressue-Valve!
			 Vac_Valve_off;	// Close Vacuum-Valve!
			 stop_motor0 ();	// Stop motor0 (Carriage).
			 stop_motor1 ();	// Stop motor1 (Lift).
			 stop_motor2 ();	// Stop motor2 (Lift).

			 lcd_puts (LCD4, "---- STOP-MODE! ----");

			 if (RunningState == 200)
				 put_s(OK_STOP_ACK);

			 RunningState = 0;	// No program run!
			 MainState = 30;	// Wait for next STOP-key!
		 }
			else			// INIT motors:
		 {
			 StopFlag = 0;
			 lcd_puts (LCD4, "--->>> RESET! <<<---");

			 IdleTimer = 0;
			 ADC_Timer = 40;
			 PressureTimeout = 0;	// 120 sec timout (PressureAlarm)
			 PressureAlarm = 0;
			 P_Fine_Reg = 0;	// Not High accuracy regulation!

			 start_motor1 (90, Param[0].M1_HomePos);	// Goto HOME (Microswitch) position.
			 start_motor2 (90, Param[0].M2_HomePos);	// Goto HOME (Microswitch) position.
			 while (Motor1_Busy || Motor2_Busy) {}

			 init_motor0_home_position ();	// Carriage
			 init_motor1_home_position ();	// Asp-lift
			 init_motor2_home_position ();	// Disp-lift

			 if (RunningState == 200)
				 put_s(OK_ACK);

			 PlateAlarm = 0;
			 PrgmState = 0;	// Init state for running a program!
			 MainState = 0;	// Init main state.
			 RunningState = 0;	// Not running a program!
			 ExtStart = 0;	// Reset flag.
		 }
			break;


		case 110: /* STOP (RESET) Command: */
		case 210: /* STOP_Cmd (RESET) Command: */
			StopFlag = 0;

			Disp_Valve_off;	// Close DISP-valve.
			Pres_Pump_off;	// Stop Pressure-Pump!
			Vac_Pump_off;	// Stop Vacuum-Pump!
			Asp_Pump_off;	// Stop Vacuum-Pump!
			Waste_Pump_off;	// Stop Waste-Pump!
			Pres_Valve_off;	// Close Pressue-Valve!
			Vac_Valve_off;	// Close Vacuum-Valve!
			stop_motor0 ();	// Stop motor0 (Carriage).
			stop_motor1 ();	// Stop motor1 (Lift).
			stop_motor2 ();	// Stop motor2 (Lift).

			lcd_puts (LCD4, "--->>> RESET! <<<---");

			ADC_Timer = 40;
			PressureTimeout = 0;		// 120 sec timout (PressureAlarm)
			PressureAlarm = 0;
			P_Fine_Reg = 0;			// Not High accuracy regulation!
			IdleTimer = 0;
			RegPressure = IDLE_PRESSURE;	// Release pressure!

			init_motor1_home_position ();	// Asp-lift
			init_motor2_home_position ();	// Disp-lift
			init_motor0_home_position ();	// Carriage

			if (RunningState == 210)
				put_s(OK_ACK);

			PlateAlarm = 0;
			PrgmState = 0;	// Not running a program!
			MainState = 0;	// Init main state.
			RunningState = 0;	// Not running a program!
			break;

		} //end switch
	} /* end while */

}



