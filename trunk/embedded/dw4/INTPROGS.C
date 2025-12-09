#include "globdata.h"

#include "keybrd.h"
#include "display.h"
#include "motor.h"

//***********************************************************************
//* FILE        : prgm.c
//*
//* DESCRIPTION : State-machine, running INTERNAL SERVICE PROGRAMS
//*
//***********************************************************************

#define SHORT_BEEP  	 50		// Beep 100mS
#define LONG_BEEP  	200		// Beep 300mS

//--------------------------------------------------------------
// PUBLIC functions:		(PCB rev. 2.0)
//--------------------------------------------------------------
bool run_service_prgm(word *PrgmState);

void set_plate_type (byte type);
void get_HeadTypeTxt (void);

//---------------------------------------------------------
// external Global data:
//---------------------------------------------------------
extern char PrgNameTxt[];
extern char PlateNameTxt[];

extern byte Head1;	// Type of Head mounted! (check_heads())
extern byte Head2;	// Type of Head mounted!
extern byte Head3;	// Type of Head mounted!
extern byte Head4;	// Type of Head mounted!
extern byte Head5;	// Type of Head mounted!
extern byte Disp1;	// Commands in use in wanted program! (check_programs)
extern byte Disp2;	// Commands in use in wanted program!
extern byte Disp3;	// Commands in use in wanted program!
extern byte Disp4;	// Commands in use in wanted program!
extern byte Asp;	// Commands in use in wanted program!
extern byte RowCol;	// Plate-direction parameter.

//---------------------------------------------------------
// external functions:
//---------------------------------------------------------
//---------------------------------------------------------
extern void start_motor0 (byte speed, long Position);
extern void start_motor1 (byte speed, int Position);
extern void start_motor2 (byte speed, int Position);
extern void stop_motor0 (void);		// Stop motor0.
extern void stop_motor1 (void);		// Stop motor1.
extern void stop_motor2 (void);		// Stop motor2.
//---------------------------------------------
extern byte get_key();
extern void put_key(byte NewKey);	// Puts a key into the keyboard-buffer
extern void delay_ms (word time);
extern void regulate_pressure (int RegPressure);

extern void lcd_puts (byte adr, const char *str);
extern void lcd_putc (byte adr, byte data);
extern void lcd_clrl (byte adr);
extern void lcd_adr  (byte adr);
extern void get_PrgNameTxt(byte PrgmNo);
extern void calibrate_pressure (void);
extern void check_heads(void);
extern void InterpretCommand();		// Check serial commands
extern long adjust_disptime (long RefDispTime, byte ADC_No);
//---------------------------------------------

// static byte RowCol;		// Copy of Plate-direction parameter.
 static byte PlateType;	// Copy of Plate parameter.
 static word PlateTop;	// Copy of Plate parameter.
 static word PlateOffset;	// Copy of Plate parameter.
 static word PlateDepth;	// Copy of Plate parameter.
 static word AspOffset;	// Copy of Plate parameter.
 static long WellOffset;	// 2.25/9.00mm offset! (2.25mm x 4)
 static byte Temp;		// temporary values
 static byte Menu;		// temporary values
 static word HeadType;		// temporary values

 static char LcdTxt1[24];
 static char LcdTxt2[24];
 static char LcdTxt3[24];
 static char LcdTxt4[24];
 static char LcdTxt5[24];
//--------------------------------------------------------------

//*******************************************************************
//
//  run_service_prgm()
//
//*******************************************************************

bool run_service_prgm(word *PrgmState)
{
   bool Pgm_Finnished = FALSE;
   word LocalState;
   static word ReturnState;

   static byte Current_Cmd;
   static int  Current_Value;
//------------------------
   int t;
   static word AspTime;		// (time in mS)
   static long DispTime;	// (time in 100uS)
   static long DispTime1;	// (time in 100uS)
   static long DispTime2;	// (time in 100uS)
   static long DispTime3;	// (time in 100uS)
   static word LiqCal;		// (ul/s)

   static word Repeat;		// Wanted repetitions.
   static word RepCnt;		// Repetitions done.
   static word HeadNo;		// Which DispHead in use.
//------------------------
   static word Asp_Lo_Pos;	// Position for wanted Asp-depth in well.
   static byte Asp_Hi_Speed;	// AspLift HighSpeed (max 20mm/s = 100%).
   static byte Asp_Lo_Speed;	// AspLift LowSpeed, 8mm/s (40%).
   static byte DispSpeed;	// DispLift Speed (max 100%).
   static byte RunSpeed;	// Carriage Speed (max 100%).
//--------------------------------------------------------------
   static int  RowCnt;		// Counter for 32-step Dispense.
   static long DispPos;		// Dispense Position.
   static long AspPos;		// Aspirate Position.
   static long AspPos1[4];	// Aspirate Position.
//--------------------------------------------------------------
   static char LcdTxt[24];
   static int  LiftPos;		// Liftposition in mm.
//--------------------------------------------------------------



#define Cmd_Line  LCD2	// LCD-line
#define InfoLine  LCD3	// LCD-line
#define Err_Line  LCD4	// LCD-line

#define ASP_HI_SPEED	90	//   35mm/s speed ASP  (100%=40mm/s)
#define ASP_LO_SPEED	30	//   12mm/s speed ASP  (100%=40mm/s)
#define DISP_SPEED	90	//   35mm/s speed DISP (100%=40mm/s)
#define RUN_SPEED	80	//   205mm/s speed Carriage (100%=256mm/s)


   LocalState = *PrgmState;

   switch (LocalState)
   {

      //------------------------------------------------------------------------
      //  Initialize start of a new program:
      //------------------------------------------------------------------------

      case 0 :	Repeat = 0;
		RepCnt = 0;

	      //---------------------------------------------------------
	      // Set all Plate-parameters:
	      //---------------------------------------------------------

		set_plate_type (3);	// Default 1536

		AspPos1[3] = Param[0].AspPos;		// 0,000mm offset (1536)
		AspPos1[2] = Param[0].AspPos + 110;	// 1,095mm offset (384)
		AspPos1[1] = Param[0].AspPos - 110;	// 1,098mm offset (96)
		AspPos1[0] = Param[0].AspPos;		// Not used!

		Asp_Lo_Speed = ASP_LO_SPEED;	// AspLift -Asp-Speed, (modified speed from mm/sek -> %speed!)
		Asp_Hi_Speed = ASP_HI_SPEED;	// AspLift -HighSpeed (20 -100%).
		DispSpeed    = DISP_SPEED;	// DispLift-HighSpeed (20 -100%).
		RunSpeed     = RUN_SPEED;	// Carriage-HighSpeed (20 -100%).

		//---------------------------------------------------------

		CurrentADC = 0;
		P_Fine_Reg = 0;				// Not high accuracy pressure-regulation!

	      //---------------------------------------------------------
	      // Clear display and put ProgramName in Line1:
	      //---------------------------------------------------------

		get_PrgNameTxt(Wanted_Prgm);		// Get name of current program (in case of LINK).
		lcd_puts (LCD1, PrgNameTxt);
		lcd_clrl (LCD2);
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);

		LocalState++;
		break;

		//------------------------------------------------------------------------------

      case 1:   if (!Motor0_Busy)	// Wait for carriage-movement!
		   LocalState++;
		break;

	      //--------------------------------------------------------------
	      // Start selected Service-Program:
	      //--------------------------------------------------------------

      case 2 :
		switch (Wanted_Prgm)
		{
		  case SERVICE_PROG1:	Current_Cmd = PRIME1;
					LocalState = 50;		// PRIME-1
					break;

		  case SERVICE_PROG2:	Current_Cmd = PRIME2;
					LocalState = 50;		// PRIME-2
					break;

		  case SERVICE_PROG3:	Current_Cmd = PRIME3;
					LocalState = 50;		// PRIME-3
					break;

		  case SERVICE_PROG4:	Current_Cmd = PRIME4;
					LocalState = 50;		// PRIME-4
					break;

		  case SERVICE_PROG5:	LocalState = 500;		// DispClean
					break;

		  case SERVICE_PROG6:	LocalState = 600;		// AspClean
					break;

		  case SERVICE_PROG7:	LocalState = 900;		// Flush
					break;

		  case SERVICE_PROG8:	LocalState = 1000;		// MotorStep
					break;

		  case SERVICE_PROG9:	LocalState = 1100;		// MotorStep
					break;

		  default:		LocalState = 10;	// Not implemented!
					break;
		}

		break;	// End case 0.




     //**************************************************************
     //         TEST! (-not implemented)
     //**************************************************************

      case 10:	Timer_1mS = 1500;	// Pause = 1.5sec
		lcd_puts (InfoLine, "   -not implemented!");
		LocalState++;
		break;

      case 11:	if (!Timer_1mS)		// Wait...
		   LocalState++;
		break;

      case 12:	LocalState = 2000;	// END!
		break;




      //**************************************************************
      //
      //       Special Programs (S1/S2/S3/S4) PRIME 1/2/3/4:
      //       (Dispence without Plate)
      //
      //**************************************************************


      case 50:  if      (Current_Cmd == PRIME1) CurrentADC = 1;
		else if (Current_Cmd == PRIME2) CurrentADC = 2;
		else if (Current_Cmd == PRIME3) CurrentADC = 3;
		else			        CurrentADC = 4;

		RowCol = 0;	// Use Disp1TimeCorr (Row)

		if (ExtStart == 1)	// If Robot-start:
		{
		   if (ExtComVal2 == 2)	// PrimeR
		   {
		      RowCol = 0;	// Rows
		      LocalState = 100;	// Full Prime
		   }
		   else if (ExtComVal2 == 3)	// PrimeC
		   {
		      RowCol = 1;	// Columns
		      LocalState = 100;	// Full Prime
		   }
		   else
		       LocalState = 60;	// Mini-prime!
		}
		else
		   LocalState++;        // Menu-select!
		break;

      case 51:
		//----------------------------------------------------
		lcd_puts (LCD2, "Select Prime Type:  ");	// Program Name
		lcd_puts (LCD3, "-> 1.Maintenance  <-");
		lcd_puts (LCD4, "   2.Full Prime     ");
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------
		Menu = 1;
		LastKey = 0;
		LocalState++;
		break;

      //--------------------------------------------------------------
      //------- Select Maintenance/Full (ENTER): ---------------------
      //--------------------------------------------------------------

      case 52:	switch (LastKey)
		{
		   case UP_KEY    :  if(Menu == 2)
				     {
					Menu = 1;
					lcd_puts (LCD3, "-> 1.Maintenance  <-");
					lcd_puts (LCD4, "   2.Full Prime     ");
				     }
				     LastKey = 0;
				     break;

		   case DOWN_KEY  :  if(Menu == 1)
				     {
					Menu = 2;
					lcd_puts (LCD3, "   1.Maintenance    ");
					lcd_puts (LCD4, "-> 2.Full Prime   <-");
				     }
				     LastKey = 0;
				     break;

		   case START_KEY :
		   case ENTER_KEY :  LocalState++;	// Select Done!
				     break;

		   case CANCEL_KEY:
		   case STOP_KEY  :  LocalState = 2000;	// Program Finnished!
				     break;

		   default:	     LastKey = 0;
				     break;
		}
		break;

      case 53:
		//----------------------------------------------------
		lcd_clrl (LCD2);
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);
		//----------------------------------------------------
		if (Menu == 1)	// Menu-selection:
		   LocalState = 60;	// Mini Prime!
		else
		   LocalState = 70;	// Full Prime!
		break;


      //------------------------------------------------------------------
      //------------------------------------------------------------------
      //------- Mini Prime (1 dispence):             ---------------------
      //------------------------------------------------------------------
      //------------------------------------------------------------------

      case 60:  //----------------------------------------------------
		lcd_puts (LCD2, "    Maintenance     ");
		lcd_puts (LCD3, "    Prime...        ");
		lcd_clrl (LCD4);


		if (MiniPrimeVol == 0 || ExtStart == 0)		// No volume-value set? (Use 100mS time, 650mB)
		{
		     RegPressure = Param[0].PrimePress;		// Set Regulator-Pressure Value:
		     P_Fine_Reg = 2;				// High accuracy pressure-regulation!
		     PressureOk = 0;
		}
		else	// Dispence volume (MiniPrimeVol, 550mB):
		{
		     RegPressure = Param[0].DispPressure;	// Set Regulator-Pressure Value:
		     P_Fine_Reg = 1;				// High accuracy pressure-regulation!
		     PressureOk = 0;
		}
		//----------------------------------------------------
		LocalState++;
		break;

      case 61:  if (PressureOk > 2)	  	// wait here for correct pressure!
		{
		   Waste_Pump_on;	// Empty tray (start vacuum pump)
		   Timer_1mS = 100;	// Wait for vacuum pump!
		   LocalState++;
		}
		break;

      case 62:	if (!Timer_1mS)			// Wait after vacuum-pump start!
		   LocalState++;
		break;

      case 63:  if (PressureOk > 5)	  	// wait here for correct pressure!
		{
		   if (SensorError)
		   {
		      PressureError = 1;		// Set error to Main!
		      break;
		   }
		   P_Fine_Reg = 3;   		// -> Limited accuracy regulation!
		   LocalState++;
		}
		break;

      //------------------------------------------------------------------
      //------- DispValve ON-1, (controlled by timer1): ------------------
      //------------------------------------------------------------------


      case 64:  if (MiniPrimeVol == 0 || ExtStart == 0)		// No volume-value set? (Use 100mS time, 650mB)
		{
		    if      (Current_Cmd == PRIME1) set_Disp_Valve1; // Set port adress (Valve1)
		    else if (Current_Cmd == PRIME2) set_Disp_Valve2; // Set port adress (Valve2)
		    else if (Current_Cmd == PRIME3) set_Disp_Valve3; // Set port adress (Valve3)
		    else if (Current_Cmd == PRIME4) set_Disp_Valve4; // Set port adress (Valve4)

		    DispTime = (long)Param[0].PrimeTime4;	// MiniPrime-time!
		    Timer_100uS = DispTime+1L;		// Valve ON! (Timer1), Calculated microlitres.

		    sprintf(LcdTxt, "%ld.%ldmS  ", DispTime/10, DispTime%10);
		    lcd_puts (LCD4+4, LcdTxt);	// Write remaining time!
		    DispFlag = 1;			// Valve ON! (Timer1), Calculated microlitres.
		}
		else	// Dispence volume (MiniPrimeVol, 550mB):
		{
		    if (Current_Cmd == PRIME1)
		    {
			 LiqCal  = Param[0].Liq1Cal[Head1-1];
			 LiqCal += Param[0].Liq1Cal_b[Head1-1];
			 LiqCal = LiqCal / 2;	// Middle value (Row/Column).
			 DispTime = (long)((long)MiniPrimeVol * (long)10000L + (long)(LiqCal / 2)) / (long)LiqCal;	// 001 - 999ul
			 Timer_100uS = adjust_disptime (DispTime, 1);	// Adjust valve-time to current pressure!
			 set_Disp_Valve1; 	// Set port adress (Valve1)
		    }
		    else if (Current_Cmd == PRIME2)
		    {
			 LiqCal  = Param[0].Liq2Cal[Head2-1];
			 LiqCal += Param[0].Liq2Cal_b[Head2-1];
			 LiqCal = LiqCal / 2;	// Middle value (Row/Column).
			 DispTime = (long)((long)MiniPrimeVol * (long)10000L + (long)(LiqCal / 2)) / (long)LiqCal;	// 001 - 999ul
			 Timer_100uS = adjust_disptime (DispTime, 2);	// Adjust valve-time to current pressure!
			 set_Disp_Valve2; 	// Set port adress (Valve2)
		    }
		    else if (Current_Cmd == PRIME3)
		    {
			 LiqCal  = Param[0].Liq3Cal[Head3-1];
			 LiqCal += Param[0].Liq3Cal_b[Head3-1];
			 LiqCal = LiqCal / 2;	// Middle value (Row/Column).
			 DispTime = (long)((long)MiniPrimeVol * (long)10000L + (long)(LiqCal / 2)) / (long)LiqCal;	// 001 - 999ul
			 Timer_100uS = adjust_disptime (DispTime, 3);	// Adjust valve-time to current pressure!
			 set_Disp_Valve3; 	// Set port adress (Valve3)
		    }
		    else if (Current_Cmd == PRIME4)
		    {
			 LiqCal  = Param[0].Liq4Cal[Head4-1];
			 LiqCal += Param[0].Liq4Cal_b[Head4-1];
			 LiqCal = LiqCal / 2;	// Middle value (Row/Column).
			 DispTime = (long)((long)MiniPrimeVol * (long)10000L + (long)(LiqCal / 2)) / (long)LiqCal;	// 001 - 999ul
			 Timer_100uS = adjust_disptime (DispTime, 4);	// Adjust valve-time to current pressure!
			 set_Disp_Valve4; 	// Set port adress (Valve4)
		    }

		    sprintf(LcdTxt, "%uul   ", MiniPrimeVol);
		    lcd_puts (LCD4+4, LcdTxt);	// Write remaining time!
		    DispFlag = 2;			// Valve ON! (Timer1), INCLUDE PresRegulation!
		    P_Fine_Reg = 2;			// High accuracy pressure-regulation!
		}

//		if (DispTime < 10000)
//		   sprintf(LcdTxt, "%uul=%ld.%ldmS  ", MiniPrimeVol, DispTime/10, DispTime%10);
//		else
//		   sprintf(LcdTxt, "%uul=%ldmS  ", MiniPrimeVol, DispTime/10);
//		lcd_puts (LCD4+4, LcdTxt);	// Write remaining time!

		LocalState++;
		break;

      case 65:	if (!DispFlag)			// Wait while valve is open!
		   LocalState = 130;		// Goto Prime-END!
		break;




      //--------------------------------------------------------------
      //------- Full Prime: Select direction (ENTER): ----------------
      //--------------------------------------------------------------

      case 70:
		//----------------------------------------------------
		lcd_puts (LCD2, "Select Head Type:   ");	// Program Name
		lcd_puts (LCD3, "-> Rows           <-");
		lcd_puts (LCD4, "   Columns          ");
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------
		RowCol = 0;
		LastKey = 0;
		LocalState++;
		break;

     case 71: 	switch (LastKey)
		{
		   case UP_KEY    :  if(RowCol == 1)
				     {
					RowCol = 0;
					lcd_puts (LCD3, "-> Rows           <-");
					lcd_puts (LCD4, "   Columns          ");
				     }
				     LastKey = 0;
				     break;

		   case DOWN_KEY  :  if(RowCol == 0)
				     {
					RowCol = 1;
					lcd_puts (LCD3, "   Rows             ");
					lcd_puts (LCD4, "-> Columns        <-");
				     }
				     LastKey = 0;
				     break;

		   case START_KEY :
		   case ENTER_KEY :  LocalState++;	// Select Done!
				     break;

		   case CANCEL_KEY:
		   case STOP_KEY  :  LocalState = 2000;	// Program Finnished!
				     break;

		   default:	     LastKey = 0;
				     break;
		}
		break;

     case 72: 	LocalState = 100;	// Full Prime!

      //------------------------------------------------------------------
      //------- Full Prime (3 sequences):            ---------------------
      //------------------------------------------------------------------
      //------- 1. seequence: Fill head vith liquid: ---------------------
      //------------------------------------------------------------------

      case 100: //----------------------------------------------------
		lcd_puts (LCD2, "    Full Prime...   ");
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);

		if      (Current_Cmd == PRIME1) HeadType = Head1-1;
		else if (Current_Cmd == PRIME2) HeadType = Head2-1;
		else if (Current_Cmd == PRIME3) HeadType = Head3-1;
		else				HeadType = Head4-1;

		//---------------------------------------------------
		//--- Init Liquid Valve Times: ----------------------
		//---------------------------------------------------
		if(RowCol == 0)
		{
		   DispTime = (long)Param[0].PrimeTime1[HeadType];	// 96/384/1536-time! (Rows)
		   Repeat   = (word)Param[0].PrimeCount1;
		   RegPressure = Param[0].PrimePress1;	// Set Regulator-Pressure Value:
		}
		else
		{
		   DispTime = (long)Param[0].PrimeTime1_b[HeadType];	// 96/384/1536-time! (Columns)
		   Repeat   = (word)Param[0].PrimeCount1_b;
		   RegPressure = Param[0].PrimePress1_b;	// Set Regulator-Pressure Value:
		}
		P_Fine_Reg = 2;				// High accuracy pressure-regulation!
		PressureOk = 0;

		//---------------------------------------------------

		RepCnt = 1;
		Timer_1mS = 100;		// Wait for Pressure Regulation!
		LocalState++;
		break;

      //------- Pause, before opening the DispValve: ---------------------

      case 101:	if (!Timer_1mS)			// Loop for Regulation-time.
		   LocalState++;
		break;

      case 102: if (PressureOk > 2)	  	// wait here for correct pressure!
		{
		  lcd_puts (LCD2, "    1.Priming...    ");
		  Waste_Pump_on;		// Empty tray (start vacuum pump)
		  Timer_1mS = 100;		// Wait for vacuum pump!
		  LocalState++;
		}
		break;

      case 103:	if (!Timer_1mS)			// Wait after vacuum-pump start!
		   LocalState++;
		break;

      //------------------------------------------------------------------
      //------- DispValve ON-1, (controlled by timer1): ------------------
      //------------------------------------------------------------------

      case 104: sprintf(LcdTxt, "%d/%d repetitons  ", RepCnt, Repeat);
		lcd_puts (LCD4+4, LcdTxt);	// Write remaining time!
		LocalState++;
		break;

      case 105: if (PressureOk > 3)	  	// wait here for correct pressure!
		{
		  if (SensorError)
		  {
		     PressureError = 1;		// Set error to Main!
		     Waste_Pump_off;		// Empty tray (vacuum pump)
		     break;
		  }
		  else
		     Waste_Pump_on;		// Empty tray (start vacuum pump)

		  if      (Current_Cmd == PRIME1) set_Disp_Valve1; // Set port adress (Valve1)
		  else if (Current_Cmd == PRIME2) set_Disp_Valve2; // Set port adress (Valve2)
		  else if (Current_Cmd == PRIME3) set_Disp_Valve3; // Set port adress (Valve3)
		  else if (Current_Cmd == PRIME4) set_Disp_Valve4; // Set port adress (Valve4)

		  Timer_100uS = DispTime+1L;	// Valve ON! (Timer1), Calculated microlitres.
		  DispFlag = 1;			// Valve ON! (Timer1), Calculated microlitres.
		  LocalState++;
		}
		break;

      case 106:	if (!DispFlag)			// Wait while valve is open!
		{
		   Timer_1mS = 500;		// Pause, 0.5sec before next Disp.
		   LocalState++;
		}
		break;

      //------- Pause, 0.5s: ---------------------------------------------

      case 107:	if (!Timer_1mS)			// Loop for pause-time.
		   LocalState++;
		break;


      //------- More repetitions? ----------------------------------------

      case 108: RepCnt++;
		if (RepCnt <= Repeat)
		   LocalState = 104;	// Repeat prime!
		else
		   LocalState = 110;	// Next seequence!
		break;


      //------------------------------------------------------------------
      //------- 2. seequence: Vibrate liquid trough head: ----------------
      //------------------------------------------------------------------

      case 110: RepCnt = 1;
		lcd_puts (LCD2, "    2.Priming...    ");

		//---------------------------------------------------
		//--- Init Repeat & Liquid Valve Times: -------------
		//---------------------------------------------------

		if(RowCol == 0)
		{
		   DispTime = (long)Param[0].PrimeTime2[HeadType];	// 96/384/1536-time! (Rows)
		   Repeat   = (word)Param[0].PrimeCount2[HeadType];
		}
		else
		{
		   DispTime = (long)Param[0].PrimeTime2_b[HeadType];	// 96/384/1536-time! (Rows)
		   Repeat   = (word)Param[0].PrimeCount2_b[HeadType];
		}

		//---------------------------------------------------
		P_Fine_Reg = 2;		// High accuracy pressure-regulation!
		Timer_1mS = 100;	// Wait for Pressure Regulation!
		LocalState++;
		break;

      //------- Pause, before opening the DispValve: ---------------------

      case 111:	if (!Timer_1mS)			// Loop for Regulation-time.
		   LocalState++;
		break;

      case 112: if (PressureOk > 2)	  	// wait here for correct pressure!
		  LocalState++;
		break;

      //------------------------------------------------------------------
      //------- DispValve ON-2, (controlled by timer1): ------------------
      //------------------------------------------------------------------

      case 113: sprintf(LcdTxt, "%d/%d repetitons  ", RepCnt, Repeat);
		lcd_puts (LCD4+4, LcdTxt);	// Write remaining time!
		LocalState++;
		break;

      case 114:	if (!Timer_1mS)			// Loop for pause-time.
		   LocalState++;
		break;

      case 115: if      (Current_Cmd == PRIME1) set_Disp_Valve1; // Set port adress (Valve1)
		else if (Current_Cmd == PRIME2) set_Disp_Valve2; // Set port adress (Valve2)
		else if (Current_Cmd == PRIME3) set_Disp_Valve3; // Set port adress (Valve3)
		else if (Current_Cmd == PRIME4) set_Disp_Valve4; // Set port adress (Valve4)

		Timer_100uS = DispTime+1L;	// Valve ON! (Timer1), Calculated microlitres.
		DispFlag = 1;			// Valve ON! (Timer1), Calculated microlitres.
		LocalState++;
		break;

      case 116:	if (!DispFlag)			// Wait while valve is open!
		{
		   Timer_1mS = (long)DispTime/(long)5L;		// Pause, before next Disp.
		   DispPause = 1;			// PAUSE after a Dispense (300mS)!
		 //---------------------------------------------------
		   DispTime += (long)DispTime/(long)10L;	// Inc time by 10 percent each repetition!!!!!!!
		 //---------------------------------------------------
		   LocalState++;
		}
		break;

      //------- More repetitions? ----------------------------------------

      case 117: RepCnt++;
		if (RepCnt <= Repeat)
		   LocalState = 113;	// Repeat prime!
		else
		   LocalState = 120;	// Goto 3. seequence!
		break;


      //------------------------------------------------------------------
      //------- 3. seequence: Vibrate liquid trough head: ----------------
      //------------------------------------------------------------------

      case 120: RepCnt = 1;
		lcd_puts (LCD2, "    3.Priming...    ");

		//---------------------------------------------------
		//--- Init Liquid Valve Times: ----------------------
		//---------------------------------------------------

		if(RowCol == 0)
		{
		   DispTime = (long)Param[0].PrimeTime3[HeadType];	// 96/384/1536-time! (Rows)
		   Repeat   = (word)Param[0].PrimeCount3[HeadType];
		}
		else
		{
		   DispTime = (long)Param[0].PrimeTime3_b[HeadType];	// 96/384/1536-time! (Rows)
		   Repeat   = (word)Param[0].PrimeCount3_b[HeadType];
		}

		//---------------------------------------------------
		P_Fine_Reg = 2;			// High accuracy pressure-regulation!
		Timer_1mS = 100;		// Wait for Pressure Regulation!
		LocalState++;
		break;

      //------- Pause, before opening the DispValve: ---------------------

      case 121:	if (!Timer_1mS)			// Loop for Regulation-time.
		   LocalState++;
		break;

      case 122: if (PressureOk > 2)	  	// wait here for correct (stable) pressure!
		  LocalState++;
		break;

      //------------------------------------------------------------------
      //------- DispValve ON-3, (controlled by timer1): ------------------
      //------------------------------------------------------------------

      case 123: sprintf(LcdTxt, "%d/%d repetitons  ", RepCnt, Repeat);
		lcd_puts (LCD4+4, LcdTxt);	// Write remaining time!
		LocalState++;
		break;

      case 124:	if (!Timer_1mS)			// Loop for pause-time.
		   LocalState++;
		break;

      case 125: if      (Current_Cmd == PRIME1) set_Disp_Valve1; // Set port adress (Valve1)
		else if (Current_Cmd == PRIME2) set_Disp_Valve2; // Set port adress (Valve2)
		else if (Current_Cmd == PRIME3) set_Disp_Valve3; // Set port adress (Valve3)
		else if (Current_Cmd == PRIME4) set_Disp_Valve4; // Set port adress (Valve4)

		Timer_100uS = DispTime+1L;	// Valve ON! (Timer1), Calculated microlitres.
		DispFlag = 1;			// Valve ON! (Timer1), Calculated microlitres.
		LocalState++;
		break;

      case 126:	if (!DispFlag)			// Wait while valve is open!
		{
		   Timer_1mS = (long)DispTime/(long)5L;		// Pause, before next Disp.
		   DispPause = 1;			// PAUSE after a Dispense (300mS)!
		   LocalState++;
		}
		break;

      //------- More repetitions? ----------------------------------------

      case 127: RepCnt++;
		if (RepCnt <= Repeat)
		   LocalState = 123;	// Repeat prime!
		else
		{
		   LocalState = 130;	// Goto Prime-END!
		   lcd_clrl (LCD4);
		}
		break;


      //------------------------------------------------------------------
      //------- END, wait for WastePump:   -------------------------------
      //------------------------------------------------------------------

      case 130: RegPressure = Param[0].DispPressure;	// Set default pressure.
//		RegPressure = CurrentPres;		// Set last used pressure.
		CurrentADC = 0;
		P_Fine_Reg = 0;			// High accuracy pressure-regulation!
		PressureOk = 0;
		LocalState++;
		break;

      //------- Pause, 2sec (after DispValve closed): ------------------


      case 131: Timer_1mS = 1500;		// Wait for WastePump!
		LocalState++;
		break;

      case 132:	if (!Timer_1mS)			// pause
		{
		   lcd_clrl (LCD2);
		   lcd_clrl (LCD3);
		   lcd_puts (LCD4, "    END!            ");
		   LocalState++;
		}
		break;

      case 133: Timer_1mS = 500;		// Wait for WastePump!
		LocalState++;
		break;

      case 134:	if (!Timer_1mS)			// pause
		{
		   Waste_Pump_off;
		   LocalState++;
		}
		break;

      case 135: LocalState = 2000;	// Program Finnished!
		break;





      //**************************************************************
      //
      //       Special Program (S5) Dispense-Clean:
      //
      //**************************************************************
      //
      //       1 - Prompt for Cleaning-Agent-Bottles:
      //       2 - Disp 8ml each head (15 repetitions)
      //
      //       3 - Prompt for Rinse-Agent-Bottles:
      //       4 - Disp 8ml each head (30 repetitions)
      //
      //       5 - Prompt for Air-Hose Only:
      //       6 - Disp all heads for 2 minutes (air-dry)
      //
      //**************************************************************

#define MIN_PRESS  	400		// 450mBar
#define CLEAN_VOLUME1  	8000/12		// 8000ul, 12 tubes
#define CLEAN_VOLUME2  	8000/48		// 8000ul, 48 tubes
#define CLEAN_VOLUME3  	8000/96		// 8000ul, 96 tubes


      //--------------------------------------------------------------
      //---- 1. Prompt for CLEANING AGENT: ---------------------------
      //--------------------------------------------------------------

      case 500:
		//----------------------------------------------------
		lcd_puts (LCD1, "S5:Dispense Clean   ");	// Program Name
		lcd_puts (LCD2, "1: Install:         ");
		lcd_puts (LCD3, "-> CLEANING AGENT <-");
		lcd_puts (LCD4, "   press ENTER key..");
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------
		CurrentADC = 0;
		RegPressure = Param[0].DispPressure;
		P_Fine_Reg = 1;			// High accuracy pressure-regulation!
		PressureOk = 0;
		WasteError = 0;

		DispTime1 = (long)((long)CLEAN_VOLUME1 * (long)10000L) / (long)Param[0].Liq1Cal[0];	// 8ml, 96
		DispTime2 = (long)((long)CLEAN_VOLUME2 * (long)10000L) / (long)Param[0].Liq1Cal[1];	// 8ml, 384
		DispTime3 = (long)((long)CLEAN_VOLUME3 * (long)10000L) / (long)Param[0].Liq1Cal[2];	// 8ml, 1536

		LastKey = 0;
		LocalState++;
		break;

      case 501:	switch (LastKey)
		{
		   case START_KEY :
		   case ENTER_KEY :	LocalState = 502;	// Start CLEAN!
					break;
		   case CANCEL_KEY:
		   case STOP_KEY  :	LocalState = 2000;	// Program Finnished!
					break;
		   default:	 	break;
		}
		break;

      //--------------------------------------------------------------
      //---- 2. Start Clean All Heads, 15 repetitions: ---------------
      //--------------------------------------------------------------

      case 502: if (PressureOk > 2)	  	// wait here for correct pressure!
		   LocalState++;
		break;

      case 503:	RepCnt = 0;
		if (Head1 && mBar[1] > MIN_PRESS) RepCnt = 1;
		if (Head2 && mBar[2] > MIN_PRESS) RepCnt = 1;
		if (Head3 && mBar[3] > MIN_PRESS) RepCnt = 1;
		if (Head4 && mBar[4] > MIN_PRESS) RepCnt = 1;
		if (RepCnt == 0)
		{
		   lcd_puts (LCD4, "Error: No Pressure! ");
		   LocalState = 941;	// 3 beeps! + Program End!
		   break;
		}
		LocalState++;
		break;

      case 504: Repeat = 15;
		RepCnt = 1;
		//----------------------------------------------------
		lcd_puts (LCD2, "1: Cleaning heads.. ");			// Program Name
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);
		//----------------------------------------------------
		Waste_Pump_on;		// Empty tray (start vacuum pump)
		LocalState++;		// Start CLEAN!
		break;

		//----------------------------------------------------
		// Run DISP_CLEAN-subrutine, 15 repetitions: ---------
		//----------------------------------------------------
      case 505:
		sprintf(LcdTxt, "%d/%d repetitons  ", RepCnt, Repeat);
		lcd_puts (LCD4+3, LcdTxt);	// Write remaining time!

		ReturnState = LocalState+1;	// Return after Sub-Rutine!
		LocalState  = 550;		// Run DispClean-Subrutine!
		break;

      case 506: RepCnt++;
		if (RepCnt <= Repeat)
		   LocalState = 505;	// Repeat CLEAN!
		else
		   LocalState = 510;	// Start RINSE!
		break;



      //--------------------------------------------------------------
      //---- 3. Prompt for RINSE AGENT: ------------------------------
      //--------------------------------------------------------------

      case 510:
		//----------------------------------------------------
		lcd_puts (LCD2, "2: Install:         ");			// Program Name
		lcd_puts (LCD3, "-> RINSE AGENT    <-");			// Program Name
		lcd_puts (LCD4, "   press ENTER key..");			// Program Name
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------

		Waste_Pump_off;		// Empty tray (vacuum pump)
		LastKey = 0;
		LocalState++;
		break;

      case 511:	switch (LastKey)
		{
		   case START_KEY :
		   case ENTER_KEY :	LocalState = 512;	// Start CLEAN!
					break;
		   case CANCEL_KEY:
		   case STOP_KEY  :	LocalState = 2000;	// Program Finnished!
					break;
		   default:	 	break;
		}
		break;

	      //--------------------------------------------------------------
	      //---- 4. Start RINSE All Heads, 30 repetitions: ---------------
	      //--------------------------------------------------------------

      case 512: Repeat = 30;
		RepCnt = 1;
		//----------------------------------------------------
		lcd_puts (LCD2, "2: Rinsing heads... ");			// Program Name
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);
		//----------------------------------------------------
		Waste_Pump_on;		// Empty tray (start vacuum pump)
		LocalState++;		// Start CLEAN!
		break;

		//----------------------------------------------------
		// Run DISP_CLEAN-subrutine, 30 repetitions: ---------
		//----------------------------------------------------
      case 513:
		sprintf(LcdTxt, "%d/%d repetitons  ", RepCnt, Repeat);
		lcd_puts (LCD4+3, LcdTxt);	// Write remaining time!

		ReturnState = LocalState+1;	// Return after Sub-Rutine!
		LocalState  = 550;		// Run DispClean-Subrutine!
		break;

      case 514: RepCnt++;
		if (RepCnt <= Repeat)
		   LocalState = 513;	// Repeat RINSE!
		else
		   LocalState = 520;	// Start HEAD-DRY!
		break;



      //--------------------------------------------------------------
      //---- 5. Prompt for AIR-HOSE: ---------------------------------
      //--------------------------------------------------------------

      case 520:
		//----------------------------------------------------
		lcd_puts (LCD2, "3: Install:         ");			// Program Name
		lcd_puts (LCD3, "-> AIR HOSES      <-");			// Program Name
		lcd_puts (LCD4, "   press ENTER key..");			// Program Name
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------

		Waste_Pump_off;		// Empty tray (vacuum pump)
		P_Fine_Reg = 2;		// Lower accuracy pressure-regulation!
		LastKey = 0;
		LocalState++;
		break;

      case 521:	switch (LastKey)
		{
		   case START_KEY :
		   case ENTER_KEY :	LocalState = 522;	// Start CLEAN!
					break;
		   case CANCEL_KEY:
		   case STOP_KEY  :	LocalState = 2000;	// Program Finnished!
					break;
		   default:	 	break;
		}
		break;

	      //--------------------------------------------------------------
	      //---- 6. Start DRY Heads ONE-BY-ONE, 4 repetitions (x2sec): ---
	      //----    1 head:  2s on          (4s pause)                 ---
	      //----    2 heads: 2s+2s on       (2s pause)                 ---
	      //----    3 heads: 2s+2s+2s on    (0s pause)                 ---
	      //----    4 heads: 2s+2s+2s+2s on (0s pause)                 ---
	      //--------------------------------------------------------------

      case 522: Repeat = 3;
		RepCnt = 1;
		//----------------------------------------------------
		lcd_puts (LCD2, "3: Drying heads...  ");			// Program Name
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);
		//----------------------------------------------------
		DispTime1 = 20000L;	// 2sec, 96
		DispTime2 = 20000L;	// 2sec, 384
		DispTime3 = 20000L;	// 2sec, 1536

		Waste_Pump_on;		// Empty tray (start vacuum pump)
		LocalState++;		// Start CLEAN!
		break;

		//----------------------------------------------------
		// Run DISP_CLEAN-subrutine, 3 repetitions: ---------
		//----------------------------------------------------
      case 523:
		sprintf(LcdTxt, "%d/%d repetitons  ", RepCnt, Repeat);
		lcd_puts (LCD4+3, LcdTxt);	// Write remaining time!

		ReturnState = LocalState+1;	// Return after Sub-Rutine!
		LocalState  = 550;		// Run DispClean-Subrutine!
		break;

      case 524: RepCnt++;
		if (RepCnt <= Repeat)
		   LocalState = 523;	// Repeat RINSE!
		else
		   LocalState = 530;	// Start HEAD-DRY, 2 minutes
		break;


	      //------------------------------------------------------
	      //------- Start Dry All Heads (1s on, 2s off), 2 minutes:
	      //------------------------------------------------------

#define DRY_TIME 90	// 90sec (1.5minutes)

      case 530:	//Waste_Pump_off;		// Empty tray (vacuum pump)
		lcd_puts (LCD3, "   Head1-4  ");
		lcd_clrl (LCD4);
		Timer_1sec = DRY_TIME;	// Start timer! (120s = 2min)
//		Timer_1mS  = 2000;	// Cycle-timer! (3sec)
		SecFlag = 1;		// Set Display-Flag.
		LocalState++;
		break;

      case 531: if (SecFlag)	// A new 1sec value?
		{
		  SecFlag = 0;			// Clear Display-Flag.
		  sprintf(LcdTxt, "%02d:%02d ", Timer_1sec/60, Timer_1sec%60);
		  lcd_puts (LCD3+13, LcdTxt);	// Write remaining time!

//		  if (Timer_1sec == DRY_TIME-10)	// 10sec?
//		      Waste_Pump_off;		// Empty tray (vacuum pump)
		}

		if (PressureOk > 2)  	// Pressure wait!
		   LocalState++;
		break;

      case 532: if (!Timer_1mS)
		{
		   if (Head1 && mBar[1] > MIN_PRESS)
		      set_Disp_Valve1;		// Valve to be activated!

		   if (Head2 && mBar[2] > MIN_PRESS)
		      set_Disp_Valve2;		// Valve to be activated!

		   if (Head3 && mBar[3] > MIN_PRESS)
		      set_Disp_Valve3;		// Valve to be activated!

		   if (Head4 && mBar[4] > MIN_PRESS)
		      set_Disp_Valve4;		// Valve to be activated!

		   Timer_1mS  = 3000;		// Cycle-timer! (3sec)
		   Timer_100uS = 10000L;	// Valve ON! (1sec)
		   DispFlag = 1;		// Valve ON! (Timer1), Calculated microlitres.
		}
		LocalState++;
		break;

      case 533: if (Timer_1sec)
		    LocalState = 531;	// Repeat!
		else
		    LocalState++;	// Finnished!
		break;

      case 534: if (!DispFlag)
		    LocalState = 590;	// Finnished!
		break;


      //--------------------------------------------------------------
      //------- Disp-Clean END: --------------------------------------
      //--------------------------------------------------------------

      case 590:
		RegPressure = IDLE_PRESSURE;	// Zero-Pressure!!
		CurrentADC = 0;
		P_Fine_Reg = 0;			// High accuracy pressure-regulation!
		ADC_Timer = 25;
		SensorError = 0;
		PressureError = 0;
		lcd_clrl (LCD2);
		lcd_clrl (LCD3);
		lcd_puts (LCD4, "   END!             ");
		Waste_Pump_off;			// Empty tray (vacuum pump)
		LocalState = 2000;		// Program Finnished!
		break;


      //-------------------------------------------------------------------
      //--  DISP_CLEAN SUBRUTINE:
      //-------------------------------------------------------------------
      //--  Disp 8 ml each head
      //-------------------------------------------------------------------

      case 550: if (WasteAlarm)  		// Waste Full?
		{
		   WasteError = 1;
		   Waste_Pump_off;		// Empty tray (vacuum pump)
		}
		if (WasteError == 0)	  	// Waste-Full-Stop?
		    LocalState++;
		break;

      case 551: if (PressureOk > 2)	  	// wait here for correct pressure!
		   LocalState++;
		break;

      case 552: Temp = 0;	// Count number of heads in use.
		if (Head1 && mBar[1] > MIN_PRESS)
		{
		   lcd_puts (LCD3, "   Head1 ");
		   set_Disp_Valve1;		// Valve to be activated!
		   if(Head1 == 1)
		      Timer_100uS = DispTime1;	// Valve ON! (96)
		   else if(Head1 == 2)
		      Timer_100uS = DispTime2;	// Valve ON! (384)
		   else
		      Timer_100uS = DispTime3;	// Valve ON! (1536)
		   DispFlag = 1;
		   while (DispFlag)  	// wait HERE while valve is open! (No pressure-regulation!)
		   {}
		   Temp++;
		}
		LocalState++;
		break;


      case 553:	if (PressureOk > 2)		// wait here for correct pressure!
		   LocalState++;
		break;

      case 554:	if (Head2 && mBar[2] > MIN_PRESS)
		{
		   lcd_puts (LCD3, "   Head2 ");
		   set_Disp_Valve2;		// Valve to be activated!
		   if(Head2 == 1)
		      Timer_100uS = DispTime1;	// Valve ON! (96)
		   else if(Head2 == 2)
		      Timer_100uS = DispTime2;	// Valve ON! (384)
		   else
		      Timer_100uS = DispTime3;	// Valve ON! (1536)
		   DispFlag = 1;
		   while (DispFlag)  	// wait HERE while valve is open! (No pressure-regulation!)
		   {}
		   Temp++;
		}
		LocalState++;
		break;


      case 555:	if (PressureOk > 2)	// wait here for correct pressure!
		   LocalState++;
		break;

      case 556:	if (Head3 && mBar[3] > MIN_PRESS)
		{
		   lcd_puts (LCD3, "   Head3 ");
		   set_Disp_Valve3;		// Valve to be activated!
		   if(Head3 == 1)
		      Timer_100uS = DispTime1;	// Valve ON! (96)
		   else if(Head3 == 2)
		      Timer_100uS = DispTime2;	// Valve ON! (384)
		   else
		      Timer_100uS = DispTime3;	// Valve ON! (1536)
		   DispFlag = 1;
		   while (DispFlag)  	// wait HERE while valve is open! (No pressure-regulation!)
		   {}
		   Temp++;
		}
		LocalState++;
		break;


      case 557:	if (PressureOk > 2)	// wait here for correct pressure!
		   LocalState++;
		break;

      case 558:	if (Head4 && mBar[4] > MIN_PRESS)
		{
		   lcd_puts (LCD3, "   Head4 ");
		   set_Disp_Valve4;		// Valve to be activated!
		   if(Head4 == 1)
		      Timer_100uS = DispTime1;	// Valve ON! (96)
		   else if(Head4 == 2)
		      Timer_100uS = DispTime2;	// Valve ON! (384)
		   else
		      Timer_100uS = DispTime3;	// Valve ON! (1536)
		   DispFlag = 1;
		   while (DispFlag)  	// wait HERE while valve is open! (No pressure-regulation!)
		   {}
		   Temp++;
		}
		LocalState++;
		break;


      //------- Check if pause is required (dutycucle 1/3): ---------------

      case 559:	if (Temp == 1)	// Only one head??
		{
		   if(Head4 == 1)
		      Timer_1mS = (long)DispTime1/(long)6L;	// Pause time! (2x 96)
		   else if(Head4 == 2)
		      Timer_1mS = (long)DispTime2/(long)6L;	// Pause time! (2x 384)
		   else
		      Timer_1mS = (long)DispTime3/(long)6L;	// Pause time! (2x 1536)
		}
		else if (Temp == 2)	// Two heads??
		{
		   if(Head4 == 1)
		      Timer_1mS = (long)DispTime1/(long)12L;	// Pause time! (1x 96)
		   else if(Head4 == 2)
		      Timer_1mS = (long)DispTime2/(long)12L;	// Pause time! (1x 384)
		   else
		      Timer_1mS = (long)DispTime3/(long)12L;	// Pause time! (1x 1536)
		}
		else
		   Timer_1mS  = 10;	// No pause! (10mS)
		LocalState++;
		break;

      case 560:	if (!Timer_1mS)		// Wait...
		   LocalState++;
		break;


      //------- Return from Sub-Rutine: ----------------------------------

      case 561:	LocalState = ReturnState;	// Return from SUBRUTINE!
		break;




      //**************************************************************
      //
      //       Special Program (S6) Aspirate-Clean:
      //
      //**************************************************************
      //
      //       1 - Prompt for Asp-Clean-Tray
      //       2 - Prompt for Cleaning-Agent
      //       3 - Disp 200ul in Tray (center)
      //       4 - Aspirate in center of Tray
      //
      //           5 repetitions
      //
      //**************************************************************

#define ASP_VOLUME1  	20000/12	// 20ml, 12 tubes (96)
#define ASP_VOLUME2  	20000/48	// 20ml, 48 tubes (384)
#define ASP_VOLUME3  	20000/96	// 20ml, 96 tubes (1536)

      //--------------------------------------------------------------
      //---- 1. Prompt for CLEAN-TRAY (plate): -----------------------
      //--------------------------------------------------------------

      case 600:
		//----------------------------------------------------
		lcd_puts (LCD1, "S6:Aspirate Clean   ");	// Program Name
		lcd_puts (LCD2, "1: Install:         ");
		lcd_puts (LCD3, "-> ASP CLEAN TRAY <-");
		lcd_puts (LCD4, "   press ENTER key..");
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------
		CurrentADC = 0;
		P_Fine_Reg = 2;		// High accuracy pressure-regulation!
		RegPressure = Param[0].DispPressure;
		PressureOk = 0;

		LastKey = 0;
		LocalState++;
		break;

      case 601:	switch (LastKey)
		{
		   case START_KEY :
		   case ENTER_KEY :	LocalState = 602;	// Start CLEAN!
					break;
		   case CANCEL_KEY:
		   case STOP_KEY  :	LocalState = 2000;	// Program Finnished!
					break;
		   default:	 	break;
		}
		break;

      //--------------------------------------------------------------
      //---- 1b. Select head:              ---------------------------
      //--------------------------------------------------------------

      case 602:	get_HeadTypeTxt ();	// Get LcdTxt for current heads!
		//----------------------------------------------------
		lcd_puts (LCD1, "2: Select DispHead: ");	// Program Name
		lcd_puts (LCD2, LcdTxt1);
		lcd_puts (LCD3, LcdTxt2);
		lcd_puts (LCD4, LcdTxt3);
		lcd_puts (LCD2, "->");
		HeadNo = 1;
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------

		LastKey = 0;
		LocalState++;
		break;

      //--------------------------------------------------------------
      //------- Select head (ENTER): ---------------------------------
      //--------------------------------------------------------------

      case 603:	switch (LastKey)
		{
		   case UP_KEY    :  if(HeadNo > 1)
				     {
					HeadNo--;
					goto LCD_UP_DATE1;	// Uppdate LCD.
				     }
				     LastKey = 0;
				     break;

		   case DOWN_KEY  :  if(HeadNo < 4)
				     {
					HeadNo++;

					LCD_UP_DATE1:	// Uppdate LCD.
					if (HeadNo <= 3)
					{
					   lcd_puts (LCD2, LcdTxt1);
					   lcd_puts (LCD3, LcdTxt2);
					   lcd_puts (LCD4, LcdTxt3);

					   if (HeadNo == 1) lcd_puts (LCD2, "->");
					   if (HeadNo == 2) lcd_puts (LCD3, "->");
					   if (HeadNo == 3) lcd_puts (LCD4, "->");
					}
					else
					{
					   lcd_puts (LCD2, LcdTxt2);
					   lcd_puts (LCD3, LcdTxt3);
					   lcd_puts (LCD4, LcdTxt4);
					   lcd_puts (LCD4, "->");
					}
				     }
				     LastKey = 0;
				     break;

		   case START_KEY :
		   case ENTER_KEY :  if      (HeadNo == 1 && Head1 != 0) LocalState++;	// Select Ok!
				     else if (HeadNo == 2 && Head2 != 0) LocalState++;	// Select Ok!
				     else if (HeadNo == 3 && Head3 != 0) LocalState++;	// Select Ok!
				     else if (HeadNo == 4 && Head4 != 0) LocalState++;	// Select Ok!
				     else Beep_Cnt = LONG_BEEP;	// Head missing!
				     LastKey = 0;
				     break;

		   case CANCEL_KEY:
		   case STOP_KEY  :  LocalState = 2000;	// Program Finnished!
				     break;

		   default:	     LastKey = 0;
				     break;
		}
		break;


      //--------------------------------------------------------------
      //---- 2. Prompt for CLEANING AGENT: ---------------------------
      //--------------------------------------------------------------

      case 604:
		CurrentADC = HeadNo;
		//----------------------------------------------------
		lcd_puts (LCD1, "S6:Aspirate Clean   ");	// Program Name
		lcd_puts (LCD2, "3: Install:         ");
		lcd_puts (LCD3, "-> CLEANING AGENT <-");
		lcd_puts (LCD4, "   press START key..");
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------
		LastKey = 0;
		LocalState++;
		break;

      case 605:	switch (LastKey)
		{
		   case START_KEY :
		   case ENTER_KEY :	LocalState++;	// Start CLEAN!
					break;
		   case CANCEL_KEY:
		   case STOP_KEY  :	LocalState = 2000;	// Program Finnished!
					break;
		   default:	 	break;
		}
		break;

      //--------------------------------------------------------------
      //---- 3. Set parameters for Disp and Asp: ---------------------
      //---- 4. Dispense 20ml (208/416/1666ul): ----------------------
      //--------------------------------------------------------------

      case 606:
		sprintf(LcdTxt3, "   Disp%d+Asp ", HeadNo);
		//----------------------------------------------------
		lcd_puts (LCD2, "1: Cleaning heads.. ");			// Program Name
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);
		//----------------------------------------------------
		lcd_puts (LCD3, LcdTxt3);	// Disp#+Asp


		//---------------------------------------------------------------
		// Calculate Disp-position for center-row:
		//---------------------------------------------------------------

		if (HeadNo == 1)
		{
		   DispPos = Param[0].DispPos1;	// DispPos1
		   PlateType = Head1;
		   lcd_puts (LCD3, "   Disp1+Asp ");
		}
		else if (HeadNo == 2)
		{
		   DispPos = Param[0].DispPos2;	// DispPos2
		   PlateType = Head2;
		   lcd_puts (LCD3, "   Disp2+Asp ");
		}
		else if (HeadNo == 3)
		{
		   DispPos = Param[0].DispPos3;	// DispPos3
		   PlateType = Head3;
		   lcd_puts (LCD3, "   Disp3+Asp ");
		}
		else if (HeadNo == 4)
		{
		   DispPos = Param[0].DispPos4;	// DispPos4
		   PlateType = Head4;
		   lcd_puts (LCD3, "   Disp4+Asp ");
		}

		if (PlateType == 1)
		{
		  DispPos +=  4270;	// Center of tray (42,7mm)
		  DispTime = (long)((long)ASP_VOLUME1 * (long)10000L) / (long)Param[0].Liq1Cal[0];	// 8ml, 96
		}
		else if (PlateType == 2)
		{
		  DispPos +=  4270-225;	// Center of tray (42,7mm-2,25mm)
		  DispTime = (long)((long)ASP_VOLUME2 * (long)10000L) / (long)Param[0].Liq1Cal[1];	// 8ml, 384
		}
		else if (PlateType == 3)
		{
		  DispPos +=  4270-112;	// Center of tray (42,7mm-1,125mm)
		  DispTime = (long)((long)ASP_VOLUME3 * (long)10000L) / (long)Param[0].Liq1Cal[2];	// 8ml, 1536
		}


		//---------------------------------------------------------------
		// Calculate Asp-position for center-row:
		//---------------------------------------------------------------
		AspTime    = 2000;	// 2sec
		PlateTop   = 1450;	// (14.5mm) Top of spesial TRAY (1/100 mm).
		PlateDepth = 1150;	// ( 3.0mm) Depth of TRAY from Top (1/100 mm).

		AspPos = AspPos1[Head5];		// Middle of first well.
		if      (Head5 == 1) AspPos +=  4270;		// Center of tray (42,7mm)         (1-row)
		else if (Head5 == 2) AspPos +=  4270-225;	// Center of tray (42,7mm-2,25mm)  (2-rows)
		else if (Head5 == 3) AspPos +=  4270-112;	// Center of tray (42,7mm-1,125mm) (2-rows)

		Asp_Lo_Pos = PlateTop - PlateDepth + 100;	// AspHeight (bottom + 1.00mm)

		//----------------------------------------------------

		start_motor2 (DispSpeed, PlateTop+DISP_AIR_GAP);	// Run Disp-lift down, high speed!
		start_motor1 (Asp_Hi_Speed, PlateTop+ASP_AIR_GAP);	// Run Asp-lift down, high speed!

		LocalState++;
		break;

      case 607:	if (!Motor2_Busy)
		   LocalState++;
		break;

      //---------------------------------------------------------------
      // Run DISP/ASP-subrutine, 5 repetitions:
      //---------------------------------------------------------------

      case 608: Repeat = 5;
		RepCnt = 1;
		LocalState++;
		break;

		//---------------------------------------------------------------

      case 609:	sprintf(LcdTxt, "%d/%d repetitions ", RepCnt, Repeat);
		lcd_puts (LCD4+3, LcdTxt);	// Write remaining time!

		ReturnState = LocalState+1;	// Return after Sub-Rutine!
		LocalState  = 650;		// Run DispAsp-Subrutine!
		break;

      case 610: RepCnt++;
		if (RepCnt <= Repeat)
		   LocalState = 609;	// Repeat DispAsp-Clean!
		else
		   LocalState++;	// Start Rinse
		break;


		//--------------------------------------------------------------
		// Run lifts and plate HOME:
		//--------------------------------------------------------------

      case 611:
		start_motor2 (DispSpeed, Param[0].M2_HomePos);		// Vert.Upper head position
		start_motor1 (Asp_Hi_Speed, Param[0].M1_HomePos);	// Vert.Upper head position
		LocalState++;
		break;

      case 612: if (!Motor1_Busy && !Motor2_Busy)	// wait until motor is finnished.
		   LocalState++;
		break;

      case 613: start_motor0 (RunSpeed, Param[0].M0_HomePos);	// Goto Home-position.
		   LocalState++;
		break;

      case 614: if (!Motor0_Busy)	// wait until motor is finnished.
		   LocalState++;
		break;

      //--------------------------------------------------------------
      //---- 3. Prompt for RINSE AGENT: ------------------------------
      //--------------------------------------------------------------

      case 615:
		//----------------------------------------------------
		lcd_puts (LCD2, "4: Install:          ");
		lcd_puts (LCD3, "-> RINSE AGENT    <-");
		lcd_puts (LCD4, "   press START key..");
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------
		LastKey = 0;
		LocalState++;
		break;

      case 616:	switch (LastKey)
		{
		   case START_KEY :
		   case ENTER_KEY :	LocalState = 620;	// Start CLEAN!
					break;
		   case CANCEL_KEY:
		   case STOP_KEY  :	LocalState = 2000;	// Program Finnished!
					break;
		   default:	 	break;
		}
		break;


		//---------------------------------------------------------------
		// Run DISP/ASP-subrutine, 5 repetitions:
		//---------------------------------------------------------------

      case 620:	Repeat = 5;
		RepCnt = 1;
		//----------------------------------------------------
		lcd_puts (LCD2, "2: Rinsing heads..  ");			// Program Name
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);
		lcd_puts (LCD3, LcdTxt3);	// Disp#+Asp
		//----------------------------------------------------
		start_motor2 (DispSpeed, PlateTop+DISP_AIR_GAP);	// Run Disp-lift down, high speed!
		start_motor1 (Asp_Hi_Speed, PlateTop+ASP_AIR_GAP);	// Run Asp-lift down, high speed!
		LocalState++;
		break;

      case 621:	if (!Motor2_Busy)
		   LocalState++;
		break;

		//---------------------------------------------------------------

      case 622:	sprintf(LcdTxt, "%d/%d repetitons  ", RepCnt, Repeat);
		lcd_puts (LCD4+3, LcdTxt);	// Write remaining time!

		ReturnState = LocalState+1;	// Return after Sub-Rutine!
		LocalState  = 650;		// Run DispAsp-Subrutine!
		break;

      case 623: RepCnt++;
		if (RepCnt <= Repeat)
		   LocalState = 622;	// Repeat DispAsp-Clean!
		else
		   LocalState++;	// End
		break;

      //--------------------------------------------------------------
      //------- Asp-Clean END: ---------------------------------------
      //--------------------------------------------------------------

      case 624:	lcd_clrl (LCD2);
		lcd_clrl (LCD3);

		RegPressure = Param[0].DispPressure;	// Set difault pressure.
//		RegPressure = CurrentPres;		// Set last used pressure.
		CurrentADC = 0;
		P_Fine_Reg = 0;			// High accuracy pressure-regulation!
		lcd_puts (LCD4, "   END!             ");
		LocalState = 2000;		// Program Finnished!
		break;




      //-------------------------------------------------------------------
      //--  DISP+ASP CLEAN SUBRUTINE:
      //-------------------------------------------------------------------

		//---------------------------------------------------------------
		// DISP-subrutine:
		//---------------------------------------------------------------
      case 650:	start_motor0 (60, DispPos);	// Run (slowly) to Disp-position.
		LocalState++;
		break;

      case 651:	if (!Motor0_Busy)
		   LocalState++;
		break;

      case 652: if (PressureOk > 3)	  	// wait here for correct pressure!
		{
		  if (SensorError)
		  {
		     PressureError = 1;		// Set error to Main!
		     break;
		  }

		  if      (HeadNo == 1) set_Disp_Valve1; // Set port adress (Valve1)
		  else if (HeadNo == 2) set_Disp_Valve2; // Set port adress (Valve2)
		  else if (HeadNo == 3) set_Disp_Valve3; // Set port adress (Valve3)
		  else if (HeadNo == 4) set_Disp_Valve4; // Set port adress (Valve4)

		  Timer_100uS = DispTime+1L;	// Valve ON! (Timer1), Calculated microlitres.
		  DispFlag = 1;
		  while (DispFlag)  	// wait HERE while valve is open! (No pressure-regulation!)
		  {}
		  Timer_1mS = 25;		// Pause = 25mS
		  LocalState++;
		}
		break;

      case 653:	if (!Timer_1mS)			// Wait...
		   LocalState++;
		break;


		//---------------------------------------------------------------
		// ASP-subrutine:
		//---------------------------------------------------------------

      case 654:	start_motor0 (40, AspPos);	// Run slowly to Asp-position (special-tray).
		LocalState++;
		break;

      case 655:	if (!Motor0_Busy)
		   LocalState++;
		break;

		//------- Run AspHead-lift down: -----------------------------------

      case 656:	start_motor1 (Asp_Lo_Speed, Asp_Lo_Pos);	// Run AspHead-lift down, low speed!
		Asp_Pump_on;			// Mega-Pump ON!
		LocalState++;
		break;

      case 657: if (!Motor1_Busy)	// Wait for lift-movement!
		   LocalState++;
		break;

		//------- Asp-Pause, at buttom of well: ----------------------------

      case 658: Timer_1mS = AspTime;		// Pause = AspTime!
		LocalState++;
		break;

      case 659:	if (!Timer_1mS)			// Wait...
		   LocalState++;
		break;

		//------- Run AspHead-lift up: ------------------------------------

      case 660:	start_motor1 (Asp_Hi_Speed, PlateTop+ASP_AIR_GAP);	// Run lift UP!
		LocalState++;
		break;

      case 661: if (!Motor1_Busy)	// Wait for lift-movement!
		   LocalState++;
		break;

      case 662: Asp_Pump_off;		// MegaPump OFF!
		LocalState++;
		break;

      //------- Return from Sub-Rutine: ----------------------------------

      case 663:	LocalState = ReturnState;	// Return from SUBRUTINE!
		break;




      //**************************************************************
      //
      //       Special Program (S7) Flush:
      //
      //**************************************************************
      //
      //       1 - Prompt for Select Head1-4
      //       2 - Disp as long as key pressed, max 10sec
      //
      //**************************************************************

      //--------------------------------------------------------------
      //---- 1. Prompt for HEAD-SELECT: ------------------------------
      //--------------------------------------------------------------

      case 900:	CurrentADC = 0;
		RegPressure = Param[0].DispPressure;
		P_Fine_Reg = 1;			// High accuracy pressure-regulation!
		PressureOk = 0;
		//----------------------------------------------------
		lcd_puts (LCD1, "S7: Flush DispHead  ");	// Program Name
		lcd_clrl (LCD2);
		lcd_puts (LCD3, " (wait for pressure)");
		lcd_clrl (LCD4);
		//----------------------------------------------------
		LocalState++;
		break;

      case 901:	if (PressureOk > 2)  	// wait here for correct pressure!
		   LocalState++;
		break;


      case 902:	get_HeadTypeTxt ();	// Get LcdTxt for current heads!
		//----------------------------------------------------
		lcd_puts (LCD1, "Select head:        ");	// Program Name
		lcd_puts (LCD2, LcdTxt1);
		lcd_puts (LCD3, LcdTxt2);
		lcd_puts (LCD4, LcdTxt3);
		lcd_puts (LCD2, "->");
		HeadNo = 1;
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------

		LastKey = 0;
		LocalState++;
		break;

      //--------------------------------------------------------------
      //------- Select head (ENTER): ---------------------------------
      //--------------------------------------------------------------

      case 903:	switch (LastKey)
		{
		   case UP_KEY    :  if(HeadNo > 1)
				     {
					HeadNo--;
					goto LCD_UPPDATE1;	// Uppdate LCD.
				     }
				     LastKey = 0;
				     break;

		   case DOWN_KEY  :  if(HeadNo < 4)
				     {
					HeadNo++;

					LCD_UPPDATE1:	// Uppdate LCD.
					if (HeadNo <= 3)
					{
					   lcd_puts (LCD2, LcdTxt1);
					   lcd_puts (LCD3, LcdTxt2);
					   lcd_puts (LCD4, LcdTxt3);

					   if (HeadNo == 1) lcd_puts (LCD2, "->");
					   if (HeadNo == 2) lcd_puts (LCD3, "->");
					   if (HeadNo == 3) lcd_puts (LCD4, "->");
					}
					else
					{
					   lcd_puts (LCD2, LcdTxt2);
					   lcd_puts (LCD3, LcdTxt3);
					   lcd_puts (LCD4, LcdTxt4);
					   lcd_puts (LCD4, "->");
					}
				     }
				     LastKey = 0;
				     break;

		   case START_KEY :
		   case ENTER_KEY :  if      (HeadNo == 1 && Head1 != 0) LocalState++;	// Select Ok!
				     else if (HeadNo == 2 && Head2 != 0) LocalState++;	// Select Ok!
				     else if (HeadNo == 3 && Head3 != 0) LocalState++;	// Select Ok!
				     else if (HeadNo == 4 && Head4 != 0) LocalState++;	// Select Ok!
				     else Beep_Cnt = LONG_BEEP;	// Head missing!
				     LastKey = 0;
				     break;

		   case CANCEL_KEY:
		   case STOP_KEY  :  LocalState = 2000;	// Program Finnished!
				     break;

		   default:	     LastKey = 0;
				     break;
		}
		break;


      //--------------------------------------------------------------
      //------- Head selected, check for ERRORS: ---------------------
      //--------------------------------------------------------------

      case 904:	if (HeadNo == 1)
		{
		   lcd_puts (LCD1, "S7:Flush DispHead1  ");	// Program Name

		   if (Head1 == 0)
		     LocalState = 930;		// Error!
//		   else if (mBar[1] < MIN_PRESS)
//		     LocalState = 940;		// Error!
		   else
		     LocalState++;	  	// OK:
		}
		else if (HeadNo == 2)
		{
		   lcd_puts (LCD1, "S7:Flush DispHead2  ");	// Program Name

		   if (Head2 == 0)
		     LocalState = 930;		// Error!
//		   else if (mBar[2] < MIN_PRESS)
//		     LocalState = 940;		// Error!
		   else
		     LocalState++;	  	// OK:
		}
		else if (HeadNo == 3)
		{
		   lcd_puts (LCD1, "S7:Flush DispHead3  ");	// Program Name

		   if (Head3 == 0)
		     LocalState = 930;		// Error!
//		   else if (mBar[3] < MIN_PRESS)
//		     LocalState = 940;		// Error!
		   else
		     LocalState++;	  	// OK:
		}
		else if (HeadNo == 4)
		{
		   lcd_puts (LCD1, "S7:Flush DispHead4  ");	// Program Name

		   if (Head4 == 0)
		     LocalState = 930;		// Error!
//		   else if (mBar[4] < MIN_PRESS)
//		     LocalState = 940;		// Error!
		   else
		     LocalState++;	  	// OK:
		}
		break;

      //--------------------------------------------------------------
      //------- Prompt for START: ------------------------------------
      //--------------------------------------------------------------

      case 905:
		//----------------------------------------------------
		lcd_puts (LCD2, "   Press and hold   ");
		lcd_puts (LCD3, "      START-key     ");
		lcd_puts (LCD4, " to flush (max 10s) ");
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------
		LastKey = 0;
		LocalState++;
		break;

      case 906:	switch (LastKey)
		{
		   case START_KEY : 	LocalState++;	// Start FLUSH!
					break;

		   case CANCEL_KEY:
		   case STOP_KEY  :	LocalState = 2000;	// Program Finnished!
					break;
		   default:	 	break;
		}
		break;

      //--------------------------------------------------------------
      //------- Start FLUSH (open disp-valve): -----------------------
      //--------------------------------------------------------------

      case 907:	if (PressureOk > 2)	  	// wait here for correct pressure!
		   LocalState++;
		break;

      case 908:	if      (HeadNo == 1) set_Disp_Valve1;		// Valve to be activated!
		else if (HeadNo == 2) set_Disp_Valve2;		// Valve to be activated!
		else if (HeadNo == 3) set_Disp_Valve3;		// Valve to be activated!
		else if (HeadNo == 4) set_Disp_Valve4;		// Valve to be activated!

		Waste_Pump_on;		// Empty tray (vacuum pump)

		lcd_puts (LCD2, "   Flushing:        ");
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);
		sprintf(LcdTxt, "0.0s ");
		lcd_puts (LCD2+13, LcdTxt);	// Write time!

		Timer_1sec = 10;	// Start timer! (max 10sec)
		SecFlag = 1;		// Set Display-Flag.
		Timer_1mS = 10;		// Valvetime high current!
		Current_Value = 0;

		Disp_Valve_on;		// Valves ON!

		LocalState++;
		break;

      case 909: if (!Timer_1mS)	  	// wait for Valvetime high current
		{
		   Disp_Valve_low;
		   Timer_1mS = 100;	// 1/10 sec
		   LocalState++;
		}
		break;

      case 910: if (!Timer_1mS)	// A new 0.1sec value?
		{
		   Timer_1mS = 100;	// 1/10 sec
		   Current_Value++;

		   sprintf(LcdTxt, "%d.%ds ", Current_Value/10, Current_Value%10);
		   lcd_puts (LCD2+13, LcdTxt);	// Write time!
		   SecFlag = 0;			// Clear Display-Flag.

		   if (Timer_1sec == 0)
		      LocalState++;

		   if (Keypad_In == NO_KEY)    // If no key pressed
		      LocalState++;
		}
		break;

      case 911: Disp_Valve_off;
		lcd_puts (LCD4, "   Flush End!       ");
		LocalState++;
		break;


      case 912: if (Keypad_In != NO_KEY)    	// If key not released:
		   Beep_Cnt = LONG_BEEP;	// A long BEEP.
		LocalState = 950;		// End
		break;


      //--------------------------------------------------------------
      //------- Error: Head absent! ----------------------------------
      //--------------------------------------------------------------

      case 930: lcd_clrl (LCD2);
		lcd_clrl (LCD3);
		lcd_puts (LCD4, "Error: Head absent! ");
		LocalState = 941;	// 3 beeps!
		break;

      //--------------------------------------------------------------
      //------- Error: No pressure! ----------------------------------
      //--------------------------------------------------------------

      case 940: lcd_clrl (LCD2);
		lcd_clrl (LCD3);
		lcd_puts (LCD4, "Error:No pressure!  ");
		LocalState++;
		break;

      //--------------------------------------------------------------

      case 941: Beep_Cnt = 300;		// A long BEEP.
		Timer_1mS = 500;	// Pause
		LocalState++;
		break;

      case 942:	if (!Timer_1mS)		// Wait...
		   LocalState++;
		break;

      case 943: Beep_Cnt = 300;		// A long BEEP.
		Timer_1mS = 500;	// Pause
		LocalState++;
		break;

      case 944:	if (!Timer_1mS)		// Wait...
		   LocalState++;
		break;

      case 945: Beep_Cnt = 300;		// A long BEEP.
		Timer_1mS = 500;	// Pause
		LocalState = 951;	// Program End!
		break;

      //--------------------------------------------------------------
      //------- Flush END: -------------------------------------------
      //--------------------------------------------------------------

      case 950: Timer_1mS = Current_Value*100;	// Waste-pause: 0-10sec!
		if (Timer_1mS < 4000)
		   Timer_1mS = 4000;		// min 4sec
		LocalState++;
		break;

      case 951:	if (!Timer_1mS)		// Wait...
		   LocalState++;
		break;

      case 952: Waste_Pump_off;		// Empty tray (vacuum pump)
		LocalState = 2000;	// Program Finnished!
		break;



      //**************************************************************
      //
      //       Special Program (S8) Motor-Step:
      //
      //**************************************************************
      //
      //       1 - Prompt for Select DispHead1-4 or AspHead
      //       2 - Run plate to selected head
      //       3 - Use Arrow-keys to run lift up and down
      //           STOP-key to end!
      //
      //**************************************************************

      //--------------------------------------------------------------
      //---- 1. Prompt for HEAD-SELECT: ------------------------------
      //--------------------------------------------------------------

     case 1000:	P_Fine_Reg = 0;		// Not High accuracy regulation!
		RegPressure = IDLE_PRESSURE;	// Zero-Pressure!!
		ADC_Timer = 25;
		SensorError = 0;
		PressureError = 0;
		//----------------------------------------------------
		lcd_puts (LCD1, "S8:Lift Alignment   ");	// Program Name
		lcd_clrl (LCD2);
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);
		//----------------------------------------------------
		Timer_1mS = 700;	// Pause = 0.5sec!
		LocalState++;
		break;

     case 1001:	if (!Timer_1mS)  	// wait
		   LocalState++;
		break;

     case 1002:	get_HeadTypeTxt ();	// Get LcdTxt for current heads!
		//----------------------------------------------------
		lcd_puts (LCD1, "Select position:   ");	// Program Name
		lcd_puts (LCD2, LcdTxt1);
		lcd_puts (LCD3, LcdTxt2);
		lcd_puts (LCD4, LcdTxt3);
		lcd_puts (LCD2, "->");
		HeadNo = 1;
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------

		LastKey = 0;
		LocalState++;
		break;

      //--------------------------------------------------------------
      //------- Select head (ENTER): ---------------------------------
      //--------------------------------------------------------------

     case 1003:	switch (LastKey)
		{
		   case UP_KEY    :  if(HeadNo > 1)
				     {
					HeadNo--;
					goto LCD_UPPDATE2;	// Uppdate LCD.
				     }
				     LastKey = 0;
				     break;

		   case DOWN_KEY  :  if(HeadNo < 5)
				     {
					HeadNo++;

					LCD_UPPDATE2:		// Uppdate LCD.
					if (HeadNo <= 3)
					{
					   lcd_puts (LCD2, LcdTxt1);
					   lcd_puts (LCD3, LcdTxt2);
					   lcd_puts (LCD4, LcdTxt3);

					   if (HeadNo == 1) lcd_puts (LCD2, "->");
					   if (HeadNo == 2) lcd_puts (LCD3, "->");
					   if (HeadNo == 3) lcd_puts (LCD4, "->");
					}
					else if (HeadNo == 4)
					{
					   lcd_puts (LCD2, LcdTxt2);
					   lcd_puts (LCD3, LcdTxt3);
					   lcd_puts (LCD4, LcdTxt4);
					   lcd_puts (LCD4, "->");
					}
					else
					{
					   lcd_puts (LCD2, LcdTxt3);
					   lcd_puts (LCD3, LcdTxt4);
					   lcd_puts (LCD4, LcdTxt5);
					   lcd_puts (LCD4, "->");
					}
				     }
				     LastKey = 0;
				     break;

		   case START_KEY :
		   case ENTER_KEY :  if      (HeadNo == 1 && Head1 != 0) LocalState++;	// Select Ok!
				     else if (HeadNo == 2 && Head2 != 0) LocalState++;	// Select Ok!
				     else if (HeadNo == 3 && Head3 != 0) LocalState++;	// Select Ok!
				     else if (HeadNo == 4 && Head4 != 0) LocalState++;	// Select Ok!
				     else if (HeadNo == 5 && Head5 != 0) LocalState++;	// Select Ok!
				     else Beep_Cnt = LONG_BEEP;	// Head missing!
				     LastKey = 0;
				     break;


		   case CANCEL_KEY:
		   case STOP_KEY  :  LocalState = 2000;	// Program Finnished!
				     break;

		   default:	     LastKey = 0;
				     break;
		}
		break;


      //--------------------------------------------------------------
      //------- Select direction (ENTER): ----------------------------
      //--------------------------------------------------------------

      case 1004:
		//----------------------------------------------------
		lcd_puts (LCD1, "Select direction:   ");	// Program Name
		lcd_puts (LCD2, "-> Rows           <-");
		lcd_puts (LCD3, "   Columns          ");
		lcd_clrl (LCD4);
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------
		RowCol = 0;
		LastKey = 0;
		LocalState++;
		break;

     case 1005: switch (LastKey)
		{
		   case UP_KEY    :  if(RowCol == 1)
				     {
					RowCol = 0;
					lcd_puts (LCD2, "-> Rows           <-");
					lcd_puts (LCD3, "   Columns          ");
				     }
				     LastKey = 0;
				     break;

		   case DOWN_KEY  :  if(RowCol == 0)
				     {
					RowCol = 1;
					lcd_puts (LCD2, "   Rows             ");
					lcd_puts (LCD3, "-> Columns        <-");
				     }
				     LastKey = 0;
				     break;

		   case START_KEY :
		   case ENTER_KEY :  LocalState++;	// Select Done!
				     break;

		   case CANCEL_KEY:
		   case STOP_KEY  :  LocalState = 2000;	// Program Finnished!
				     break;

		   default:	     LastKey = 0;
				     break;
		}
		break;

      //--------------------------------------------------------------
      //------- Head selected, check for ERRORS: ---------------------
      //--------------------------------------------------------------

     case 1006:	if (HeadNo == 1)
		{
		   lcd_puts (LCD1, "S8:DispLift (Head1) ");	// Program Name
		   set_plate_type (Head1);
		}
		else if (HeadNo == 2)
		{
		   lcd_puts (LCD1, "S8:DispLift (Head2) ");	// Program Name
		   set_plate_type (Head2);
		}
		else if (HeadNo == 3)
		{
		   lcd_puts (LCD1, "S8:DispLift (Head3) ");	// Program Name
		   set_plate_type (Head3);
		}
		else if (HeadNo == 4)
		{
		   lcd_puts (LCD1, "S8:DispLift (Head4) ");	// Program Name
		   set_plate_type (Head4);
		}
		else if (HeadNo == 5)
		{
		   lcd_puts (LCD1, "S8:AspLift (Head5)  ");	// Program Name
		   set_plate_type (Head5);
		}

		lcd_clrl (LCD2);
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);

		if (PlateType == 0)
		{
		   lcd_puts (LCD4, "Error: Head absent!  ");
		   LocalState = 941;		// 3 x Beep + Program End!
		}
		else
		   LocalState++;		// Ok

		break;

      //--------------------------------------------------------------
      //------- Prompt for START: ------------------------------------
      //--------------------------------------------------------------

     case 1007:	if      (PlateType == 1) lcd_puts (LCD2, "   96-Alignment     ");
		else if (PlateType == 2) lcd_puts (LCD2, "   384-Alignment    ");
		else if (PlateType == 3) lcd_puts (LCD2, "   1536-Alignment   ");

		if (RowCol == 0)
		lcd_puts (LCD3, "-> TESTPLATE (Row)  ");
		else
		lcd_puts (LCD3, "-> TESTPLATE (Col)  ");
		//----------------------------------------------------
		lcd_puts (LCD4, "   press START-key..");
		//----------------------------------------------------
		Beep_Cnt = LONG_BEEP;
		//----------------------------------------------------
		LastKey = 0;
		LocalState++;
		break;

     case 1008:	switch (LastKey)
		{
		   case START_KEY : 	LocalState++;	// Start Alinment!
					break;
		   case CANCEL_KEY:
		   case STOP_KEY  :	LocalState = 2000;	// Program Finnished!
					break;
		   default:	 	break;
		}
		break;

      //--------------------------------------------------------------
      //------- START: Run plate to head-position: -------------------
      //--------------------------------------------------------------

     case 1009:	if (RowCol == 0)	// Rows:
		{
		   AspPos1[3] = Param[0].AspPos;		// 0,000mm offset (1536)
		   AspPos1[2] = Param[0].AspPos + 110;	// 1,095mm offset (384)
		   AspPos1[1] = Param[0].AspPos - 110;	// 1,098mm offset (96)
		   AspPos1[0] = Param[0].AspPos;		// Not used!

		   if      (HeadNo == 1) DispPos = Param[0].DispPos1;	// Plate-edge
		   else if (HeadNo == 2) DispPos = Param[0].DispPos2;	// Plate-edge
		   else if (HeadNo == 3) DispPos = Param[0].DispPos3;	// Plate-edge
		   else if (HeadNo == 4) DispPos = Param[0].DispPos4;	// Plate-edge
		   else if (HeadNo == 5) DispPos = AspPos1[PlateType];	// Plate-edge
		}
		else
		{
		   AspPos1[3] = Param[0].AspPos_b;	// 0,000mm offset (1536)
		   AspPos1[2] = Param[0].AspPos_b + 110;	// 1,095mm offset (384)
		   AspPos1[1] = Param[0].AspPos_b - 110;	// 1,098mm offset (96)
		   AspPos1[0] = Param[0].AspPos_b;	// Not used!

		   if      (HeadNo == 1) DispPos = Param[0].DispPos1_b;	// Plate-edge
		   else if (HeadNo == 2) DispPos = Param[0].DispPos2_b;	// Plate-edge
		   else if (HeadNo == 3) DispPos = Param[0].DispPos3_b;	// Plate-edge
		   else if (HeadNo == 4) DispPos = Param[0].DispPos4_b;	// Plate-edge
		   else if (HeadNo == 5) DispPos = AspPos1[PlateType];// Plate-edge
		}


		if (HeadNo < 5) // Disp WellPos:
		{
		   if      (PlateType == 1) PlateOffset = 3250;	//  96-Row = 32.50mm
		   else if (PlateType == 2) PlateOffset = 1450;	// 384-Row = 14.50mm
		   else if (PlateType == 3) PlateOffset =  150;	//1536-Row =  1.50mm
		   if (RowCol)
		   {
		     if (PlateType == 1) PlateOffset += 30;	//  96-ColDispHead = +0.30mm
		     if (PlateType == 2) PlateOffset += 10;	// 384-ColDispHead = +0.10mm
		   }
		}
		else		 // Asp WellPos:
		{
		   if      (PlateType == 1) PlateOffset = 3250;	//  96-Row = 32.50mm
		   else if (PlateType == 2) PlateOffset = 2350;	// 384-Row = 23.50mm
		   else if (PlateType == 3) PlateOffset =  675;	//1536-Row =  6.75mm
		}

		DispPos += PlateOffset;
		start_motor0 (RunSpeed, DispPos);	// Run to head-position.
		LocalState++;
		break;

     case 1010:	if (!Motor0_Busy)
		   LocalState++;
		break;

      //--------------------------------------------------------------
      //------- Run LIFT up/down by arrow-keys: ----------------------
      //--------------------------------------------------------------

     case 1011:	// if (HeadNo == 5)
		//   start_motor1 (50, 1100);	// Run Asp-lift down to 11mm!
		// else
		//  start_motor2 (50, 1100);	// Run Disp-lift down to 11mm!
		   LocalState++;
		break;

     case 1012:	if (!Motor1_Busy && !Motor2_Busy)
		   LocalState++;
		break;

     case 1013:	Timer_1mS = 100;	// 1/10 sec
		lcd_puts (LCD2, "- Run Lift Up/Down -");
		lcd_clrl (LCD3);
		lcd_puts (LCD4, "  Position=15.00mm  ");	// Write position!

		LocalState++;
		break;

     case 1014: if (Keypad_In == CANCEL_KEY)   	// If STOP-key pressed
		    LocalState = 1030;		// Program Finnished!

		if (!Timer_1mS)	// A new 0.1sec value?
		{
		   Timer_1mS = 80;	// 1/10 sec

		   //------------------------------------------------
		   // Write position to display (20.00mm):
		   //------------------------------------------------

		   if (HeadNo == 5)	// ASP
		      LiftPos = (long)((long)Motor1_TachoPos*(long)10000L)/(long)Param[0].M1_Tacho;	// AspLift-position.
		   else
		      LiftPos = (long)((long)Motor2_TachoPos*(long)10000L)/(long)Param[0].M2_Tacho;	// DispLift-position.

		   sprintf(LcdTxt, "%d.%02dmm ", LiftPos/100, LiftPos%100);
		   lcd_puts (LCD4+11, LcdTxt);	// Write position!

		   //------------------------------------------------
		   // Check KeyPad:
		   //------------------------------------------------
		   if (Keypad_In == NO_KEY)    	// If no key pressed
		   {
		      Current_Value = -1;	// Prescale

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

		      if (Current_Value++ >= 5)
		      {
			 if (Motor1_PWM <= 3)
			 {
			   Current_Value = 0;	// Reset Prescale
			   PWM1_Out (Motor1_PWM++);	// IncSpeed!
			   PWM2_Out (Motor2_PWM++);	// IncSpeed!
			 }
			 else if (Motor1_PWM <= 18)
			 {
			   Current_Value = 5;	// Reset Prescale
			   PWM1_Out (Motor1_PWM++);	// IncSpeed!
			   PWM2_Out (Motor2_PWM++);	// IncSpeed!
			 }
			 else
			   Current_Value = 0;	// Reset Prescale
		      }
		   }
		   //------------------------------------------------
		   if (Keypad_In == DOWN_KEY)   // If DOWN-key pressed
		   {
		      if (HeadNo == 5)
		      {
			 if(!Motor1_Busy && Current_Value < 0)	// Key must have been released!)
			 {
			    if (Motor1_TachoPos > 925)	// Over 12 mm? (in tacho steps: 12*7720/100).
			      start_motor1 (2, 1100);	// Disp: Down to 11mm
			    else
			      start_motor1 (2, 500);	// Disp: Down to 5mm
			    start_motor1 (2, 100);	// Asp: Down to 1mm
			    Motor1_PWM = 1;		// PWM-speed (0-100)
			    Motor2_PWM = 1;		// PWM-speed (0-100)
			    PWM1_Out (Motor1_PWM);
			 }
		      }
		      else
		      {
			 if(!Motor2_Busy && Current_Value < 0)	// Key must have been released!)
			 {
			    if (Motor2_TachoPos > 925)	// Over 12 mm? (in tacho steps: 12*7720/100).
			      start_motor2 (2, 1100);	// Disp: Down to 11mm
			    else
			      start_motor2 (2, 500);	// Disp: Down to 5mm
			    Motor1_PWM = 1;		// PWM-speed (0-100)
			    Motor2_PWM = 1;		// PWM-speed (0-100)
			    PWM2_Out (Motor2_PWM);
			 }
		      }

		      if (Current_Value++ >= 5)
		      {
			 if (Motor1_PWM <= 3)
			 {
			   Current_Value = 0;		// Reset Prescale
			   PWM1_Out (Motor1_PWM++);	// IncSpeed!
			   PWM2_Out (Motor2_PWM++);	// IncSpeed!
			 }
			 else if (Motor1_PWM <= 17)
			 {
			   Current_Value = 5;		// Reset Prescale
			   PWM1_Out (Motor1_PWM++);	// IncSpeed!
			   PWM2_Out (Motor2_PWM++);	// IncSpeed!
			 }
			 else
			   Current_Value = 0;	// Reset Prescale
		      }

		      if (HeadNo == 5)
		      {
			 if (Motor1_TachoPos < 850)	// Under 11 mm (in tacho steps: 11*7720/100).
			 {
			    if (Motor1_PWM > 3)
			    {
			       Motor1_PWM = 3;		// Slow speed!
			       Motor2_PWM = 3;		// PWM-speed (0-100)
			    }
			 }
		      }
		      else
		      {
			 if (Motor2_TachoPos < 850)	// Under 11 mm (in tacho steps: 11*7720/100).
			 {
			    if (Motor2_PWM > 3)
			    {
			       Motor1_PWM = 3;		// Slow speed!
			       Motor2_PWM = 3;		// PWM-speed (0-100)
			    }
			 }
		      }
		   }
		   //------------------------------------------------
		   if (Keypad_In == STOP_KEY)   // If STOP-key pressed
		   {
		      LocalState = 1030;	// Program Finnished!
		   }
		   //------------------------------------------------
		}
		break;

      //--------------------------------------------------------------
      //------- Motor-Step END: --------------------------------------
      //--------------------------------------------------------------

     case 1030:
		LocalState = 2000;	// Program Finnished!
		break;



      //**************************************************************
      //
      //       Special Program (S9) Aspiraton Pump On (Back-Flush Disp-heads):
      //
      //**************************************************************
      //
      //       1 - Start = start pump
      //       2 - Stop  = stop  pump
      //
      //**************************************************************

     case 1100:	P_Fine_Reg = 0;		// Not High accuracy regulation!
		RegPressure = IDLE_PRESSURE;	// Zero-Pressure!!
		ADC_Timer = 25;
		SensorError = 0;
		PressureError = 0;
		//----------------------------------------------------
		lcd_puts (LCD1, "S9: Reverse Flush   ");	// Program Name
		lcd_clrl (LCD2);
		lcd_puts (LCD3, "    Vacuum On!      ");
		lcd_clrl (LCD4);
		//----------------------------------------------------
		Timer_1mS = 200;	// Pause = 0.2sec!
		LocalState++;
		break;

     case 1101:	if (!Timer_1mS)  	// wait
		   LocalState++;
		break;

     case 1102:	Asp_Pump_on;		// Mega-Pump ON!
		Timer_1mS = 400;	// Pause = 0.4sec!
		LocalState++;
		break;

     case 1103:	if (!Timer_1mS)  	// wait
		   LocalState++;
		break;

     case 1104:	Asp_Pump_on;			// Mega-Pump ON!
		//----------------------------------------------------
		lcd_clrl (LCD2);
		lcd_puts (LCD3, "  Press any key to  ");
		lcd_puts (LCD4, "  stop Vacuum Pump! ");
		//----------------------------------------------------
		LastKey = 0;
		LocalState++;
		break;

     case 1105:	switch (LastKey)
		{
		    case UP_KEY    :
		    case DOWN_KEY  :
		    case START_KEY :
		    case STOP_KEY  :
		    case ENTER_KEY :
		    case CANCEL_KEY:	LocalState++; break;	// Program Finnished!
		    default:	 	break;
		}
		break;

     case 1106: Asp_Pump_off;		// MegaPump OFF!
		//----------------------------------------------------
		lcd_puts (LCD3, "    Vacuum Off!     ");
		lcd_clrl (LCD4);
		//----------------------------------------------------
		Timer_1mS = 500;	// Pause = 0.2sec!
		LocalState++;
		break;

     case 1107:	if (!Timer_1mS)  	// wait
		   LocalState++;
		break;

      //--------------------------------------------------------------
      //------- AspPump END: --------------------------------------
      //--------------------------------------------------------------

     case 1108: lcd_clrl (LCD3);
		LocalState = 2000;	// Program Finnished!
		break;




      //**************************************************************
      //
      //       Program Finnished!
      //
      //**************************************************************

     case 2000: /***** Program Finnished! *******/

		CurrentADC = 0;
		P_Fine_Reg = 0;			// Not High accuracy regulation!
		start_motor2 (DispSpeed, Param[0].M2_HomePos);		// Vert.Upper head position
		start_motor1 (Asp_Hi_Speed, Param[0].M1_HomePos);	// Vert.Upper head position
		LocalState++;
		break;

     case 2001: if (!Motor1_Busy && !Motor2_Busy)	// wait until motor is finnished.
		   LocalState++;
		break;

     case 2002: start_motor0 (RunSpeed, Param[0].M0_HomePos);	// Goto Home-position.
		   LocalState++;
		break;

     case 2003: if (!Motor0_Busy)	// wait until motor is finnished.
		   LocalState++;
		break;

     case 2004: Pgm_Finnished = TRUE; break;  /* command finnished */

   }	/* End switch */

   *PrgmState = LocalState;
   return Pgm_Finnished;
}





//--------------------------------------------------------------
//- Sub rutine:
//--------------------------------------------------------------

void set_plate_type (byte type)
{
   //---------------------------------------------------------
   // Set all Plate-parameters to "standard" 96/384/1536:
   //---------------------------------------------------------

   PlateType = type;	// 0=absent, 1=96, 2=384, 3=1536
   AspOffset = 0;

   if (type <= 1)	// 96-plate (or no plate)
   {
	PlateTop    = 1450;	// Top of plate (1/100 mm).
	PlateDepth  = 1140;	// Depth of Well from PlateTop (1/100 mm).
	PlateOffset = 1140;	// Distance from PlateEdge to center of first Well (1/100 mm)..
	WellOffset  = 9000L;	// Distance between Wells (1/1000mm),(2250=2.250mm, 4500=4.500mm, 9000=9.000mm)
   }
   else if (type == 2)	// 384-plate
   {
	PlateTop    = 1440;	// Top of plate
	PlateDepth  = 1160;	// Depth of Well from PlateTop (1/10 mm).
	PlateOffset = 900;	// Distance from PlateEdge to center of first Well (1/10 mm)..
	WellOffset  = 4500L;	// Distance between Wells (1/1000mm),(2250=2.250mm, 4500=4.500mm, 9000=9.000mm)
   }
   else if (type == 3)	// 1536-plate
   {
	PlateTop    = 740;	// Top of plate
	PlateDepth  = 500;	// Depth of Well from PlateTop (1/10 mm).
	PlateOffset = 780;	// Distance from PlateEdge to center of first Well (1/10 mm)..
	WellOffset  = 2250L;	// Distance between Wells (1/1000mm),(2250=2.250mm, 4500=4.500mm, 9000=9.000mm)
   }
}


//--------------------------------------------------------------
//- Sub rutine:
//--------------------------------------------------------------

void get_HeadTypeTxt (void)
{
	if      (Head1 == 3) sprintf (LcdTxt1, "   DispHead1 1536   ");
	else if (Head1 == 2) sprintf (LcdTxt1, "   DispHead1 384    ");
	else if (Head1 == 1) sprintf (LcdTxt1, "   DispHead1 96     ");
	else                 sprintf (LcdTxt1, "   DispHead1 ---    ");

	if      (Head2 == 3) sprintf (LcdTxt2, "   DispHead2 1536   ");
	else if (Head2 == 2) sprintf (LcdTxt2, "   DispHead2 384    ");
	else if (Head2 == 1) sprintf (LcdTxt2, "   DispHead2 96     ");
	else                 sprintf (LcdTxt2, "   DispHead2 ---    ");

	if      (Head3 == 3) sprintf (LcdTxt3, "   DispHead3 1536   ");
	else if (Head3 == 2) sprintf (LcdTxt3, "   DispHead3 384    ");
	else if (Head3 == 1) sprintf (LcdTxt3, "   DispHead3 96     ");
	else                 sprintf (LcdTxt3, "   DispHead3 ---    ");

	if      (Head4 == 3) sprintf (LcdTxt4, "   DispHead4 1536   ");
	else if (Head4 == 2) sprintf (LcdTxt4, "   DispHead4 384    ");
	else if (Head4 == 1) sprintf (LcdTxt4, "   DispHead4 96     ");
	else                 sprintf (LcdTxt4, "   DispHead4 ---    ");

	if      (Head5 == 3) sprintf (LcdTxt5, "   AspHead   1536   ");
	else if (Head5 == 2) sprintf (LcdTxt5, "   AspHead   384    ");
	else if (Head5 == 1) sprintf (LcdTxt5, "   AspHead   96     ");
	else                 sprintf (LcdTxt5, "   AspHead   ---    ");
}

