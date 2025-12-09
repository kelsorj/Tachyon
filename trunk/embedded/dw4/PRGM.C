#include "globdata.h"

#include "keybrd.h"
#include "display.h"
#include "motor.h"

//***********************************************************************
//* FILE        : prgm.c
//*
//* DESCRIPTION : State-machine, running USER PROGRAMS
//*
//***********************************************************************


//--------------------------------------------------------------
// PUBLIC functions:		(PCB rev. 2.0)
//--------------------------------------------------------------
bool run_prgm(word *PrgmState);


//-----------------------------------------------
//Globals in prgm module
//-----------------------------------------------
word CmdNo;		// Step in running program.
byte RowCol;		// Copy of Plate-direction parameter.


//-------------------------------------------------------------------
extern void start_motor0 (byte speed, long Position);
extern void start_motor1 (byte speed, int Position);
extern void start_motor2 (byte speed, int Position);
extern byte get_key();
extern void delay_ms (word time);
extern void regulate_pressure (int RegPressure);

extern void lcd_puts (byte adr, const char *str);
extern void lcd_putc (byte adr, byte data);
extern void lcd_clrl (byte adr);
extern void lcd_adr  (byte adr);
extern void get_PrgNameTxt(byte PrgmNo);
extern void calibrate_pressure (void);

//-------------------------------------------------------------------

extern char PrgNameTxt[];
extern char PlateNameTxt[];

extern byte Head1;	// Type of Head mounted! (check_heads(), prgm2.c)
extern byte Head2;	// Type of Head mounted!
extern byte Head3;	// Type of Head mounted!
extern byte Head4;	// Type of Head mounted!
extern byte Head5;	// Type of Head mounted!

//*******************************************************************
//  adjust_disptime
//-------------------------------------------------------------------
//  NewTime = time *100/K2 * (550mBar + K1) / (CurrentPress + K1)
//-------------------------------------------------------------------
//  NewTime = time *(550+K1)*(K2-550+CurrentPress)/(K2*(CurrentPress+K1))
//*******************************************************************

long adjust_disptime (long RefDispTime, byte ADC_No)
{
	int  Pressure;
	long NewTime;
	long K1, K2;
	long Temp1, Temp2;
	int  TimeCorr;
	byte HeadType;
	char LcdTxt[32];

	if (ADC_No < 1) ADC_No = 0;
	if (ADC_No > 4) ADC_No = 0;

	//------------------------------------------------
	//- Read current pressure (4 readings):
	//------------------------------------------------

	if      (ADC_No == 1) {Select_ADC1}	// Multiplexer input.
	else if (ADC_No == 2) {Select_ADC2}
	else if (ADC_No == 3) {Select_ADC3}
	else if (ADC_No == 4) {Select_ADC4}
	else 		 {Select_ADC0}	// Error: select internal sensor!
	delay_ms (2);	// 2mS delay

	Pressure  = READ_ADC ();	// Read pressure 1. (0,2mS rutine)
	delay_ms (2);

	Pressure += READ_ADC ();	// Read pressure 2. (0,2mS rutine)
	delay_ms (2);

	Pressure += READ_ADC ();	// Read pressure 3. (0,2mS rutine)
	delay_ms (2);

	Pressure += READ_ADC ();	// Read pressure 4. (0,2mS rutine)
	Pressure  = Pressure >> 2;	// Mean value of 4 readings.
	Pressure -= Param[0].PresOffset[ADC_No];
	Pressure = (long)((long)(Pressure+2) * (long)1000L) / (long)Param[0].PresCal[ADC_No];

	ADC_Timer = 20;	// Init pressure-regulator.

	//------------------------------------------------
	//- Find headtype and timecorrection:
	//------------------------------------------------

	if (ADC_No <= 1)
	{
		HeadType = Head1;
		if(RowCol)
			TimeCorr = Param[0].Disp1TimeCorr_b[Head1-1];	// Correct for unlinearity!
		else
			TimeCorr = Param[0].Disp1TimeCorr[Head1-1];	// Correct for unlinearity!
	}
	else if (ADC_No == 2)
	{
		HeadType = Head2;
		if(RowCol)
			TimeCorr = Param[0].Disp2TimeCorr_b[Head2-1];	// Correct for unlinearity!
		else
			TimeCorr = Param[0].Disp2TimeCorr[Head2-1];	// Correct for unlinearity!
	}
	else if (ADC_No == 3)
	{
		HeadType = Head3;
		if(RowCol)
			TimeCorr = Param[0].Disp3TimeCorr_b[Head3-1];	// Correct for unlinearity!
		else
			TimeCorr = Param[0].Disp3TimeCorr[Head3-1];	// Correct for unlinearity!
	}
	else
	{
		HeadType = Head4;
		if(RowCol)
			TimeCorr = Param[0].Disp4TimeCorr_b[Head4-1];	// Correct for unlinearity!
		else
			TimeCorr = Param[0].Disp4TimeCorr[Head4-1];	// Correct for unlinearity!
	}


	//------------------------------------------------
	//- Set konstants according to pressure and headtype:
	//------------------------------------------------

	if (HeadType == 1)	// 96
	{
		K1 = 0;
		K2 = 2650;
	}
	else if (HeadType == 2)	// 384
	{
		K1 = 8;
		K2 = 2625;
	}
	else			// 1536
	{
		K1 = 11;
		K2 = 2300;
	}


	//-------------------------------------------------------------------
	//- Calculate New DispTime:
	//-------------------------------------------------------------------
	//  NewTime = time *(550+K1)*(K2-550+CurrentPress)/(K2*(CurrentPress+K1))
	//-------------------------------------------------------------------

	//   Temp1  = (long) RefDispTime * (long) (550 + K1);
	//   Temp1 *= (long) (K2 - 550 + Pressure);

	//   Temp2  = (long) K2 * (long) (Pressure + K1);

	//   NewTime  = (long) Temp1 / (long) Temp2;
	//   NewTime += (long) TimeCorr;		// Correct for valve unlinearity!

	//-------------------------------------------------------------------

	Temp1  = (long) (550 + K1) * (long) (K2 - 550 + Pressure);
	Temp1  *= (long) 200L;			// (resolution: 0.5 prosent)
	Temp2  = (long) K2 * (long) (Pressure + K1);
	Temp1  = (long) Temp1 / (long) Temp2;	// Korr (1.00 = 200)

	NewTime  = (long) RefDispTime * (long) Temp1;
	NewTime /= (long) 200L;
	NewTime += (long) TimeCorr;		// Correct for valve unlinearity!

	//------------------------------------------------
	//- Test; vis trykk og tid i displayet:
	//------------------------------------------------
	//   Temp1  = (long) Temp1 / (long) 2L;	// Korr (1.00 = 100)
	//   if (NewTime >= 1000L)
	//   sprintf(LcdTxt, "(%3dmB=%ld.%02ld=%ldmS)  ", Pressure, (long)Temp1/100L, (long)Temp1%100, (long)NewTime/10L);
	//   else
	//   sprintf(LcdTxt, "(%3dmB=%ld.%02ld=%2ld.%ldmS)  ", Pressure, (long)Temp1/100L, (long)Temp1%100, (long)NewTime/10L, (long)NewTime%10);
	//   LcdTxt[20] = 0;
	//   lcd_puts (LCD4, LcdTxt);
	//------------------------------------------------

	return (NewTime);
}



//*******************************************************************
//
//  run_prgm()
//
//*******************************************************************

bool run_prgm(word *PrgmState)
{
	bool Pgm_Finnished = FALSE;
	word LocalState;
	static word ReturnState;

	static byte PrgmNo;		// Current running program
	static byte StepNo;		// Current running program
	static byte Current_Cmd;
	static byte AspSweep;
	static int  Current_Value;
	static byte Current_Value_Msb;
	static byte Current_Value_Lsb;

	//------------------------
	int t;
	static word AspTime;		// (time in mS)
	static word SoakTime;	// (time in seconds)
	static long DispTime;	// (time in 100uS)

	static word Repeat;		// Wanted repetitions.
	static word RepCnt;		// Repetitions done.
	static word HeadNo;		// Which DispHead in use.
	//--------------------------------------------------------------
	static word Asp_Lo_Pos;	// Position for wanted Asp-depth in well.

	static byte Asp_Hi_Speed;	// AspLift HighSpeed (max 20mm/s = 100%).
	static byte Asp_Lo_Speed;	// AspLift LowSpeed, 8mm/s (40%).
	static byte DispSpeed;	// DispLift Speed (max 100%).
	static byte RunSpeed;	// Carriage Speed (max 100%).

	static word PrimeCount;	// Number of Primes

	word wTemp;
	//------------------------
	static byte PlateType;	// Copy of Plate parameter.
	static byte LowBasePlate;	// Flag Extended Rim plate.
	static int  PlateTop;	// Copy of Plate parameter.
	static int  PlateOffset;	// Copy of Plate parameter.
	static int  PlateDepth;	// Copy of Plate parameter.
	static word PlateRows0;	// Copy of Plate parameter.
	static word PlateRows1;	// Copy of Plate parameter.
	static int  AspOffset;	// Copy of Plate parameter.
	//--------------------------------------------------------------
	static int  RowCnt;		// Counter for Dispense positions.
	static int  FlgCnt;		// Counter for Dispense flags.
	static int  MaxFlgs;		// Number of RowCol steps!
	static long WellOffset;	// 2.25/9.00mm offset! (2.25mm x 4)
	static long WellPos1;	// First Dispense Position.
	static long AspPos1[4];	// Different position (head type).
	static long SoakPos;		// Just before First Dispense Position.
	static int  DispOffset[4];	// Different position (head type).
	static char LcdTxt[24];
	char TmpTxt[24];
	static long NextPos;		// Next Dispense Position.
	long SweepPos;		// Next AspSweep Position.
	byte RowFlag;
	//--------------------------------------------------------------

#define Cmd_Line  LCD2	// LCD-line
#define InfoLine  LCD3	// LCD-line
#define Err_Line  LCD4	// LCD-line

#define ASP_HI_SPEED	90	//   35mm/s speed ASP  (100%=40mm/s)
#define ASP_LO_SPEED	50	//   20mm/s speed ASP  (100%=40mm/s)
#define DISP_SPEED	90	//   35mm/s speed DISP (100%=40mm/s)
#define RUN_SPEED	80	//   205mm/s speed Carriage (100%=256mm/s)


	LocalState = *PrgmState;

	switch (LocalState)
	{

		//------------------------------------------------------------------------
		//  Initialize start of a new program (first program):
		//------------------------------------------------------------------------

	case 0 :
		PrgmNo = Wanted_Prgm-1;		// Current running program block number.
		CmdNo  = 0;			// Current command number.
		StepNo = 0;			// Number of commands done.
		Repeat = 0;
		RepCnt = 0;

		Asp_Lo_Speed = ASP_LO_SPEED;	// AspLift -Asp-Speed, (modified speed from mm/sek -> %speed!)
		Asp_Hi_Speed = ASP_HI_SPEED;	// AspLift -HighSpeed (20 -100%).
		DispSpeed    = DISP_SPEED;	// DispLift-HighSpeed (20 -100%).
		RunSpeed     = RUN_SPEED;	// Carriage-HighSpeed (20 -100%).
		AspSweep = 0;

		//---------------------------------------------------------
		// Get all Plate-parameters from this program:
		//---------------------------------------------------------

		if (PrgmNo < MAX_SUBPROGRAMS)	// Normal program:
		{
			RowCol      = Program[PrgmNo].RowColumn;		// Plate Direction.
			PlateType   = Program[PrgmNo].PlateType;		// 1=96, 2=384, 3=1536.
			PlateTop    = Program[PrgmNo].PlateHeight;		// Top of plate (1/10 mm).
			PlateDepth  = Program[PrgmNo].PlateDepth;		// Depth of Well from PlateTop (1/10 mm).
			PlateOffset = Program[PrgmNo].PlateOffset;		// Distance from PlateEdge to center of first Well (1/10 mm)..
			WellOffset  = (long)Program[PrgmNo].PlateDbwc;	// Distance between Wells (1/1000mm),(2250=2.250mm, 4500=4.500mm, 9000=9.000mm)
			AspOffset   = Program[PrgmNo].AspOffset * 10;		// Out of center, Asp, (1/10 -> 1/100mm).

			PlateRows0 = Program[PrgmNo].PlateRows0;
			PlateRows1 = Program[PrgmNo].PlateRows1;

			if (PlateType >= 10)	// LowBasePlate? (Extended Rim?)
			{
				PlateType -= 10;
				LowBasePlate = 1;
			}
			else
				LowBasePlate = 0;

			if (!LowBasePlate)
				start_motor2 (DispSpeed, PlateTop+DISP_AIR_GAP);	// Run DispHead-lift down, high speed!
		}


		if (RowCol == 0)	// 32 Rows
		{
			AspPos1[3] = Param[0].AspPos;		// 0,000mm offset (1536)
			AspPos1[2] = Param[0].AspPos + 110;	// 1,095mm offset (384)
			AspPos1[1] = Param[0].AspPos - 110;	// 1,098mm offset (96)
			AspPos1[0] = Param[0].AspPos;		// Not used!
			DispOffset[0] = 0;
			DispOffset[1] = 0;
			DispOffset[2] = 0;
			DispOffset[3] = 0;
			SoakPos = Param[0].DispPos1;			// Plate-edge.
		}
		else			// 48 Colomns
		{
			AspPos1[3] = Param[0].AspPos_b;	// 0,000mm offset (1536)
			AspPos1[2] = Param[0].AspPos_b + 110;	// 1,095mm offset (384)
			AspPos1[1] = Param[0].AspPos_b - 110;	// 1,098mm offset (96)
			AspPos1[0] = Param[0].AspPos_b;	// Not used!
			DispOffset[0] = 0;
			DispOffset[1] = 30;	//  96: +0.30mm
			DispOffset[2] = 10;	// 384: +0.10mm
			DispOffset[3] = 0;
			SoakPos = Param[0].DispPos1_b;		// Plate-edge.
		}

		if (PlateType > 1)	// Head = 2 rows
			SoakPos -= (long)WellOffset / 10L;       // Outside plate-edge.
		SoakPos -= 500;       // 5mm outside edge.

		//------------------------------------------------------------------------
		//  Initialize start of new program:
		//------------------------------------------------------------------------
	case 1 :
		//---------------------------------------------------------
		// Clear display and put ProgramName in Line1:
		//---------------------------------------------------------

		get_PrgNameTxt(PrgmNo+1);	// Get name of current program (in case of LINK).
		lcd_puts (LCD1, PrgNameTxt);
		lcd_clrl (LCD2);
		lcd_clrl (LCD3);
		lcd_clrl (LCD4);
		LocalState++;
		break;

		//------------------------------------------------------------------------------

	case 2:   if (!Motor0_Busy)	// Wait for carriage-movement!
				  LocalState = 21;	// Initialize next command.
		break;

		//------------------------------------------------------------------------
		//   Advance to next command (in current program):
		//------------------------------------------------------------------------

	case 20 :	CmdNo++;		// Next command number (in current program)
		StepNo++;		// Number of commands done.
		LocalState = 21;	// Initialize next command.
		break;


		//------------------------------------------------------------------------
		//   Start a new command (in current program):
		//------------------------------------------------------------------------
	case 21 :	// start a new command:

		//---------------------------------------------------------
		// Clear display and put Command in Line2:
		//---------------------------------------------------------
		//		lcd_puts (ProgLine, "P01:Test-prog 1     ");
		//		lcd_puts (Cmd_Line, "s01:01-Dispense Liq1");
		//		lcd_puts (InfoLine, "    AA+BB - 100.0ul ");
		//		lcd_puts (Err_Line, "    1/4 repetitons  ");

		sprintf(LcdTxt, "s%02d:%02d-             ", StepNo+1, CmdNo+1);
		lcd_puts (Cmd_Line, LcdTxt);
		lcd_clrl (InfoLine);

		//---------------------------------------------------------
		if(Repeat)
		{
			sprintf(LcdTxt, "    %d/%d repetitons  ", RepCnt, Repeat);
			lcd_puts (Err_Line, LcdTxt);	// Info on error-line!
		}
		else
			lcd_clrl (Err_Line);

		//---------------------------------------------------------
		lcd_adr (Cmd_Line+7);	// Set current adress to Command!


		//--------------------------------------------------------------
		// Find next Disp-command (get Pressure and ADC-Sensor number):
		//--------------------------------------------------------------

		if (PrgmNo < MAX_SUBPROGRAMS)	// Normal program:
		{
			for (t=CmdNo; t<50; t++)	// Check all commands
			{
				Current_Cmd = Program[PrgmNo].Command[t];
				//		     CurrentPres = Param[0].DispPressure;	// Default Regulator-Pressure Value:

				switch(Current_Cmd)
				{
				case DISP1:
					CurrentADC = 1;
					P_Fine_Reg = 1;	// High accuracy pressure-regulation!
					CurrentPres = Param[0].DispPressure;	// Default Regulator-Pressure Value:
					t = 50;	// Stop loop!
					break;

				case DISP2:
					CurrentADC = 2;
					P_Fine_Reg = 1;	// High accuracy pressure-regulation!
					CurrentPres = Param[0].DispPressure;	// Default Regulator-Pressure Value:
					t = 50;	// Stop loop!
					break;

				case DISP3:
					CurrentADC = 3;
					P_Fine_Reg = 1;	// High accuracy pressure-regulation!
					CurrentPres = Param[0].DispPressure;	// Default Regulator-Pressure Value:
					t = 50;	// Stop loop!
					break;

				case DISP4:
					CurrentADC = 4;
					P_Fine_Reg = 1;	// High accuracy pressure-regulation!
					CurrentPres = Param[0].DispPressure;	// Default Regulator-Pressure Value:
					t = 50;	// Stop loop!
					break;
					//---------------------------------------------

				case DISPL1:
					CurrentADC = 1;
					P_Fine_Reg = 1;	// High accuracy pressure-regulation!
					CurrentPres = Program[PrgmNo].DispLowPr1;	// Low-Pressure Value:
					t = 50;	// Stop loop!
					break;

				case DISPL2:
					CurrentADC = 2;
					P_Fine_Reg = 1;	// High accuracy pressure-regulation!
					CurrentPres = Program[PrgmNo].DispLowPr2;	// Low-Pressure Value:
					t = 50;	// Stop loop!
					break;

				case DISPL3:
					CurrentADC = 3;
					P_Fine_Reg = 1;	// High accuracy pressure-regulation!
					CurrentPres = Program[PrgmNo].DispLowPr3;	// Low-Pressure Value:
					t = 50;	// Stop loop!
					break;

				case DISPL4:
					CurrentADC = 4;
					P_Fine_Reg = 1;	// High accuracy pressure-regulation!
					CurrentPres = Program[PrgmNo].DispLowPr4;	// Low-Pressure Value:
					t = 50;	// Stop loop!
					break;

				case REP1 :
				case REP2 :
				case REP3 :
				case REP4 :
				case REP5 :
				case REP6 :
				case REP7 :
				case REP8 :
				case REP9 :		// A Jump! Keep current values (dont examine now).
				case REP10: 
					t = 50;	// Stop loop!
					break;

				case END  :
					CurrentADC = 0;
					P_Fine_Reg = 0;	// Not high accuracy pressure-regulation!
					t = 50;	// Stop loop!
					break;

				default   : break;
				}
			}		// All 50 Commands checed!
		}		// End if(normal program)

		else 	// Special Internal Programs:
		{
			CurrentADC = 0;
			P_Fine_Reg = 0;				// Not high accuracy pressure-regulation!
		}

		// Set Regulator-Pressure Value:
		RegPressure = CurrentPres;


		//--------------------------------------------------------------
		// Get next Command (ordinary/service-programs):
		//--------------------------------------------------------------

		if (PrgmNo < MAX_SUBPROGRAMS && CmdNo < MAX_COMMANDS)	// ordinary subprograms
		{
			Current_Cmd   = Program[PrgmNo].Command[CmdNo];
			Current_Value = Program[PrgmNo].CmdValue[CmdNo];
		}
		else		// undefined program or step-number?
		{
			Current_Cmd   = 0;	// END (Error!)
			Current_Value = 0;	// 0
		}



		//--------------------------------------------------------------
		// Chech ASP-Sweep command:
		//--------------------------------------------------------------

		if (Current_Cmd == ASP1sweep)
		{
			Current_Cmd =ASP1;
			AspSweep = 1;
		}
		else if (Current_Cmd == ASP2sweep)
		{
			Current_Cmd =ASP2;
			AspSweep = 1;
		}
		else if (Current_Cmd == ASP3sweep)
		{
			Current_Cmd =ASP3;
			AspSweep = 1;
		}
		else
			AspSweep = 0;


		//--------------------------------------------------------------
		// Initialize and Run the new Command:
		//--------------------------------------------------------------
		switch(Current_Cmd)
		{
			//-----------------------------------------------
		case ASP1  :
		case ASP2  :
		case ASP3  :
			if      (Current_Cmd == ASP1) wTemp = Param[0].AspSpeed1;	// Speed (x10)mm/sek
			else if (Current_Cmd == ASP2) wTemp = Param[0].AspSpeed2;	// Speed (x10)mm/sek
			else if (Current_Cmd == ASP3) wTemp = Param[0].AspSpeed3;	// Speed (x10)mm/sek

			//--------------------------------------
			// Calculate speed in % (100% = 39mm/s)
			//--------------------------------------
			wTemp = wTemp * 10/39;	// Speed from (x10)mm/sek -> % speed!
			if (wTemp > 100)
				wTemp = 100;	// Max 100%

			Asp_Lo_Speed = wTemp;	// Speed: 0-100%

			Current_Value_Msb = (word)Current_Value >> 8;		// ASP: Time (1/10 sec)
			Current_Value_Lsb = (word)Current_Value & 0x00ff;	// ASP: Height (1/10 mm)

			AspTime = Current_Value_Msb * 100;	// x mS pause
			Asp_Lo_Pos = PlateTop - PlateDepth + ((word)Current_Value_Lsb * 10);	// AspHeight

			//--------------------------------------
			// Write this Command to display:
			//--------------------------------------
			//				lcd_puts (0, "P01:Test-prog 1     ");
			//				lcd_puts (0, "s01:01-Aspirate     ");
			//				lcd_puts (0, "    AA+BB - 1.1sec  ");
			//				lcd_puts (0, "    1/4 repetitons  ");

			if (AspSweep)
				sprintf(LcdTxt, "Asp-Sweep    ");
			else
				sprintf(LcdTxt, "Aspirate     ");
			lcd_puts (0, LcdTxt);	// Add text to current line!

			//-------------------------------------------------
			// Make a Asp-Time textstring (to use with rows):
			//-------------------------------------------------
			sprintf(LcdTxt, "%d.%dsec ", AspTime/1000, (AspTime%1000)/100);

			P_Fine_Reg = 0;			// Not High accuracy regulation!
			LocalState = 200;		// Asp/Disp
			Asp_Pump_on;			// Mega-Pump ON!
			break;


			//-----------------------------------------------
		case DISP1 :     		// Disp Liq1 (Head1)
		case DISP2 :     		// Disp Liq2 (Head2)
		case DISP3 :     		// Disp Liq3 (Head3)
		case DISP4 :     		// Disp Liq4 (Head4)
			HeadNo = Current_Cmd - DISP1 + 1;
			//-------------------------------------------------
			// Write this Command to display:
			//-------------------------------------------------
			//				lcd_puts (0, "P01:Test-prog 1     ");
			//				lcd_puts (0, "s01:01-Dispense Liq1");
			//				lcd_puts (0, "    AA+BB - 100.0ul ");
			//				lcd_puts (0, "    1/4 repetitons  ");

			sprintf(LcdTxt, "Dispense Liq%d", HeadNo);
			lcd_puts (0, LcdTxt);	// Add text to current line!

			//-------------------------------------------------
			// Make a Disp-Volume textstring (to use with rows):
			//-------------------------------------------------
			if (Current_Value >= 1000)	// > 100ul ?
				sprintf(LcdTxt, "%dul  ", Current_Value/10);
			else				// < 99.9ul
				sprintf(LcdTxt, "%d.%dul  ", Current_Value/10, Current_Value%10);

			LocalState = 200;	// Asp/Disp
			break;

			//-----------------------------------------------
		case DISPL1:     		// LowPressurDisp1 (Head1)
		case DISPL2:     		// LowPressurDisp2 (Head2)
		case DISPL3:     		// LowPressurDisp3 (Head3)
		case DISPL4:     		// LowPressurDisp4 (Head4)
			HeadNo = Current_Cmd - DISPL1 + 1;
			//-------------------------------------------------
			// Write this Command to display:
			//-------------------------------------------------
			//				lcd_puts (0, "P01:Test-prog 1     ");
			//				lcd_puts (0, "s01:01-Disp1 (200mB)");
			//				lcd_puts (0, "    AA+BB - 100.0ul ");
			//				lcd_puts (0, "    1/4 repetitons  ");

			if (HeadNo==1) sprintf(LcdTxt, "Disp1 (%dmB)  ", Program[PrgmNo].DispLowPr1);
			if (HeadNo==2) sprintf(LcdTxt, "Disp2 (%dmB)  ", Program[PrgmNo].DispLowPr2);
			if (HeadNo==3) sprintf(LcdTxt, "Disp3 (%dmB)  ", Program[PrgmNo].DispLowPr3);
			if (HeadNo==4) sprintf(LcdTxt, "Disp4 (%dmB)  ", Program[PrgmNo].DispLowPr4);
			LcdTxt[13] = 0;
			lcd_puts (0, LcdTxt);	// Add text to current line!

			//-------------------------------------------------
			// Make a Disp-Volume textstring (to use with rows):
			//-------------------------------------------------
			if (Current_Value >= 1000)	// > 100ul ?
				sprintf(LcdTxt, "%dul  ", Current_Value/10);
			else				// < 99.9ul
				sprintf(LcdTxt, "%d.%dul  ", Current_Value/10, Current_Value%10);

			LocalState = 200;	// Asp/Disp
			break;

			//-----------------------------------------------
		case SOAK  :     		// Soak (Pause, CmdValue=seconds)
			SoakTime = Current_Value;
			//				lcd_puts (0, "P01:Test-prog 1     ");
			//				lcd_puts (0, "s01:01-Soak 0:00:10 ");
			//				lcd_puts (0, "            0:00:07 ");
			//				lcd_puts (0, "    1/4 repetitons  ");

			sprintf(LcdTxt, "Soak %d:%02d:%02d ", SoakTime/3600, (SoakTime%3600)/60, SoakTime%60);
			lcd_puts (0, LcdTxt);	// Add text to current line!

			LocalState = 300;	// Soak
			break;

			//-----------------------------------------------
		case ROW0 :
			PlateRows0 = Current_Value;	// Change Row0-flags:
			LocalState = 20;		// Command finished, next command.
			break;

		case ROW1 :
			PlateRows1 = Current_Value;	// Change Row1-flags:
			LocalState = 20;		// Command finished, next command.
			break;

			//-----------------------------------------------
		case ASP_OFS:
			AspOffset = Current_Value * 10;	// Change Asp-Offset,(1/10 -> 1/100mm):
			LocalState = 20;		// Command finished, next command.
			break;

			//-----------------------------------------------
		case REP1  :
		case REP2  :
		case REP3  :
		case REP4  :
		case REP5  :
		case REP6  :
		case REP7  :
		case REP8  :
		case REP9  :
		case REP10 :
			Repeat = Current_Cmd - REP0;	// Number of repetitons.
			if (RepCnt < Repeat)		// If all repetitions not done:
			{
				RepCnt++;
				CmdNo = Current_Value-1;	// Jump to Repeat From Command!
				LocalState = 21;		// Jump to Repeat From Command!
			}
			else
			{
				Repeat = 0;
				RepCnt = 0;
				CmdNo += 1;		// Finnished; Jump to next Command!
				LocalState = 21;	// Jump to next Command!
			}
			break;

			//-----------------------------------------------
		case END   :	lcd_puts (0, "END ");	  // end of program sequence.
		default    :	LocalState = 1000; break; // Program Finnished!
		}

		break;	// End case 20.





		//**************************************************************
		//
		//        DISPENCE 8, 16 or 32 times (DispValve use 32bits Timer_100uS)
		//        ASPIRATE 8, 16 or 32 times (AspTime use Timer_1ms)
		//
		//**************************************************************
		//        Motor0: Carriage
		//        Motor1: Asp-lift
		//        Motor2: Disp-lift
		//        MaxFlgs   =   8,  16,  32;	// Number of wells in actual Plate!
		//        WellOffset = 9000, 4500, 2250; 	// Distance between wells! (2250=2.250mm)
		//**************************************************************

	case 200:
		//		if (!LowBasePlate)
		//		   start_motor2 (DispSpeed, PlateTop+DISP_AIR_GAP);	// Run DispHead-lift down, high speed!

		if (RowCol == 0)	// 32 Rows
		{
			//---------------------------------------------------
			//--- Init Liquid Valve Times: ----------------------
			//---------------------------------------------------

			if (Current_Cmd == DISP1 || Current_Cmd == DISPL1)	// Head1
			{
				DispTime = (long)((long)Current_Value * (long)1000L + (long)(Param[0].Liq1Cal[Head1-1] / 2)) / (long)Param[0].Liq1Cal[Head1-1];	// 0.5-10ul
				DispTime = (long)((long)DispTime * (long)Program[PrgmNo].Liq1Factor) / (long)100L;	// 50-250 procent
				WellPos1 = Param[0].DispPos1+PlateOffset;	// Middle of first well.
			}
			else if (Current_Cmd == DISP2 || Current_Cmd == DISPL2)	// Head2
			{
				DispTime = (long)((long)Current_Value * (long)1000L + (long)(Param[0].Liq2Cal[Head2-1] / 2)) / (long)Param[0].Liq2Cal[Head2-1];	// 0.5-10ul
				DispTime = (long)((long)DispTime * (long)Program[PrgmNo].Liq2Factor) / (long)100L;	// 50-250 procent
				WellPos1 = Param[0].DispPos2+PlateOffset;	// Middle of first well.
			}
			else if (Current_Cmd == DISP3 || Current_Cmd == DISPL3)	// Head3
			{
				DispTime = (long)((long)Current_Value * (long)1000L + (long)(Param[0].Liq3Cal[Head3-1] / 2)) / (long)Param[0].Liq3Cal[Head3-1];	// 0.5-10ul
				DispTime = (long)((long)DispTime * (long)Program[PrgmNo].Liq3Factor) / (long)100L;	// 50-250 procent
				WellPos1 = Param[0].DispPos3+PlateOffset;	// Middle of first well.
			}
			else if (Current_Cmd == DISP4 || Current_Cmd == DISPL4)	// Head4
			{
				DispTime = (long)((long)Current_Value * (long)1000L + (long)(Param[0].Liq4Cal[Head4-1] / 2)) / (long)Param[0].Liq4Cal[Head4-1];	// 0.5-10ul
				DispTime = (long)((long)DispTime * (long)Program[PrgmNo].Liq4Factor) / (long)100L;	// 50-250 procent
				WellPos1 = Param[0].DispPos4+PlateOffset;	// Middle of first well.
			}
			else	// Aspirate, Head 5!
			{
				WellPos1 = AspPos1[PlateType] + PlateOffset + AspOffset;	// Middle of first well.
				//		      if (!LowBasePlate)
				//			 start_motor1 (Asp_Hi_Speed, PlateTop+ASP_AIR_GAP);	// Run lift down!
			}

			//---------------------------------------------------
			//--- Init number of Disp-positions: ----------------
			//---------------------------------------------------
			if      (PlateType == 3) MaxFlgs = 16;	// (1536-Head, 2 rows): 32 rows = 16 steps max! (2.25mm x 2)
			else if (PlateType == 2) MaxFlgs =  8;	// ( 384-Head, 2 rows): 16 rows =  8 steps max! (4.50mm x 2)
			else if (PlateType == 1) MaxFlgs =  8;	// (  96-Head, 1 row ):  8 rows =  8 steps max! (9.00mm x 1)
		}
		else		 // 48 Columns
		{
			//---------------------------------------------------
			//--- Init Liquid Valve Times: ----------------------
			//---------------------------------------------------

			if (Current_Cmd == DISP1 || Current_Cmd == DISPL1)	// Head1
			{
				DispTime = (long)((long)Current_Value * (long)1000L + (long)(Param[0].Liq1Cal_b[Head1-1] / 2)) / (long)Param[0].Liq1Cal_b[Head1-1];	// 0.5-10ul
				DispTime = (long)((long)DispTime * (long)Program[PrgmNo].Liq1Factor) / (long)100L;	// 50-250 procent
				WellPos1 = Param[0].DispPos1_b+PlateOffset;	// Middle of first well.
				WellPos1 += DispOffset[PlateType];	// (96-Head: +0.3mm, 384-Head: +0.1mm)
			}
			else if (Current_Cmd == DISP2 || Current_Cmd == DISPL2)	// Head2
			{
				DispTime = (long)((long)Current_Value * (long)1000L + (long)(Param[0].Liq2Cal_b[Head2-1] / 2)) / (long)Param[0].Liq2Cal_b[Head2-1];	// 0.5-10ul
				DispTime = (long)((long)DispTime * (long)Program[PrgmNo].Liq2Factor) / (long)100L;	// 50-250 procent
				WellPos1 = Param[0].DispPos2_b+PlateOffset;	// Middle of first well.
				WellPos1 += DispOffset[PlateType];	// (96-Head: +0.3mm, 384-Head: +0.1mm)
			}
			else if (Current_Cmd == DISP3 || Current_Cmd == DISPL3)	// Head3
			{
				DispTime = (long)((long)Current_Value * (long)1000L + (long)(Param[0].Liq3Cal_b[Head3-1] / 2)) / (long)Param[0].Liq3Cal_b[Head3-1];	// 0.5-10ul
				DispTime = (long)((long)DispTime * (long)Program[PrgmNo].Liq3Factor) / (long)100L;	// 50-250 procent
				WellPos1 = Param[0].DispPos3_b+PlateOffset;	// Middle of first well.
				WellPos1 += DispOffset[PlateType];	// (96-Head: +0.3mm, 384-Head: +0.1mm)
			}
			else if (Current_Cmd == DISP4 || Current_Cmd == DISPL4)	// Head4
			{
				DispTime = (long)((long)Current_Value * (long)1000L + (long)(Param[0].Liq4Cal_b[Head4-1] / 2)) / (long)Param[0].Liq4Cal_b[Head4-1];	// 0.5-10ul
				DispTime = (long)((long)DispTime * (long)Program[PrgmNo].Liq4Factor) / (long)100L;	// 50-250 procent
				WellPos1 = Param[0].DispPos4_b+PlateOffset;	// Middle of first well.
				WellPos1 += DispOffset[PlateType];	// (96-Head: +0.3mm, 384-Head: +0.1mm)
			}
			else	// Aspirate, Head 5!
			{
				WellPos1 = AspPos1[PlateType] + PlateOffset + AspOffset;	// Middle of first well.
				//		      if (!LowBasePlate)
				//			 start_motor1 (Asp_Hi_Speed, PlateTop+ASP_AIR_GAP);	// Run lift down!
			}

			//---------------------------------------------------
			//--- Init number of Disp-positions: ----------------
			//---------------------------------------------------
			if      (PlateType == 3) MaxFlgs = 24;	// (1536-Head, 2 cols): 48 cols = 24 steps max! (2.25mm x 2)
			else if (PlateType == 2) MaxFlgs = 12;	// ( 384-Head, 2 cols): 24 cols = 12 steps max! (4.50mm x 2)
			else if (PlateType == 1) MaxFlgs = 12;	// (  96-Head, 1 col ): 12 cols = 12 steps max! (9.00mm x 1)
		}

		FlgCnt = 0;		// Initialize Counter, 0-31.
		RowCnt = 0;		// Initialize Counter, 0-31.

		LocalState++;
		break;


		//---------------------------------------------------
		//--- Init done, loop for activated rows: -----------
		//---------------------------------------------------

	case 201: 
		if (Motor2_Busy < 3)	// Wait for lift to be close to down-pos, before starting carriage!
			LocalState = 230;	// Asp/Disp n times!
		break;



		//------------------------------------------------------------------
		//--  Asp or Disp in position 1-8, 1-16 or 1-32 (loop MaxFlgs):
		//------------------------------------------------------------------

	case 230: 
		RowFlag = 0;		// Check next row:
		if (FlgCnt < 16)
		{
			if (PlateRows0 & (0x0001<<FlgCnt))
				RowFlag = 1;
		}
		else
		{
			if (PlateRows1 & (0x0001<<(FlgCnt-16)))
				RowFlag = 1;
		}

		if (RowFlag == 1)	// This row activated?
		{

			//---------------------------------------------------------------
			// Activated: Display current Rows (at current display-position):
			//---------------------------------------------------------------

			lcd_clrl (InfoLine);

			//---------------------------------------------------------------
			if (RowCol == 0) // 32 Rows (A to FF)
				//---------------------------------------------------------------
			{
				lcd_puts (InfoLine, "row:");

				if (RowCnt <= 25)	// (25 = Z)
					lcd_putc (0, (byte)(RowCnt+'A'));	// First row (A-Z)
				else
				{
					lcd_putc (0, (byte)(RowCnt+'A'-26));	// First row (AA-FF)
					lcd_putc (0, (byte)(RowCnt+'A'-26));	// First row (AA-FF)
				}

				if (PlateType > 1)	// 384/1536-plate (2 rows):
				{
					lcd_puts (0, "+");

					if (RowCnt <= 24)	// (24+1 = Z)
						lcd_putc (0, (byte)(RowCnt+'B'));	// Second row (B-Z)
					else
					{
						lcd_putc (0, (byte)(RowCnt+'B'-26));	// Second row (AA-FF)
						lcd_putc (0, (byte)(RowCnt+'B'-26));	// Second row (AA-FF)
					}
				}
			}

			//---------------------------------------------------------------
			else		 // 48 Columns (48 to 1)
				//---------------------------------------------------------------
			{
				lcd_puts (InfoLine, "col:");

				if (PlateType == 1)	// 96-plate (1 row):
					sprintf (TmpTxt, "%d", RowCnt+1);
				else			// 384/1536-plate (2 rows):
					sprintf (TmpTxt, "%d+%d", RowCnt+1, RowCnt+2);

				lcd_puts (0, TmpTxt);
			}
			//---------------------------------------------------------------

			lcd_puts (0, " = ");
			lcd_puts (0, LcdTxt);	// Add Asp/Disp Value-text to current line!

			//---------------------------------------------------------------
			// Calculate new position for this row:
			//---------------------------------------------------------------
			NextPos = WellPos1;		// 1/100 mm!
			NextPos += ((long)WellOffset * (long)RowCnt) / 10L;

			start_motor0 (RunSpeed, NextPos);	// Run to next 1536 well.

			//---------------------------------------------------------------
			// Run ASP or DISP-subrutine:
			//---------------------------------------------------------------
			ReturnState = LocalState+1;	// Return after Sub-Rutine!

			if      (Current_Cmd == DISP1 || Current_Cmd == DISPL1) LocalState  = 270;	// Run Disp-Subrutine!
			else if (Current_Cmd == DISP2 || Current_Cmd == DISPL2) LocalState  = 270;	// Run Disp-Subrutine!
			else if (Current_Cmd == DISP3 || Current_Cmd == DISPL3) LocalState  = 270;	// Run Disp-Subrutine!
			else if (Current_Cmd == DISP4 || Current_Cmd == DISPL4) LocalState  = 270;	// Run Disp-Subrutine!
			else 			  			   LocalState  = 280;	// Run ASP-Subrutine!
		}
		else	// Not activated, check the next row!
		{
			LocalState++;
		}
		break;

	case 231: //------- Loop for more rows? ------------------------------
		FlgCnt++;		// One RowFlag done!

		if (FlgCnt < MaxFlgs)	// Is all rows done?
			LocalState--;	// No: check the next row!
		else
			LocalState++;	// Yes: All rows finished!

		if (PlateType == 1)
			RowCnt = FlgCnt;	// 96-plate (1 rows)
		else
			RowCnt = FlgCnt*2;	// 384/1536-plate (2 rows)
		break;


		//---------------------------------------------------
		//------- Asp/Disp End: -----------------------------
		//---------------------------------------------------

		// V2.0:
	case 232: if (!Motor2_Busy && !Motor1_Busy)	// Wait for lift-movement!
				  LocalState++;
		break;


	case 233: start_motor2 (DispSpeed, Param[0].M2_HomePos);		// Run Disp-lift up!
		start_motor1 (Asp_Hi_Speed, Param[0].M1_HomePos);	// Run Asp-lift up!
		LocalState++;
		break;

	case 234: if (!Motor2_Busy && !Motor1_Busy)	// Wait for lift-movement!
				  LocalState++;
		break;


	case 235: LocalState = 20;	// Asp/Disp is finished, next command.
		//		Asp_Pump_off;		// MegaPump OFF!
		break;



		//-------------------------------------------------------------------
		//--  DISP-SUBRUTINE:
		//-------------------------------------------------------------------
		//--  1. Lift-Down
		//--  2. Valve Open (dispense) controlled by timer1!
		//--  3. Lift-Up
		//-------------------------------------------------------------------

	case 270: 
		if (!Motor0_Busy && !Motor2_Busy)	  // wait until motors is finnished.
			LocalState++;
		break;

		//------- DispValve ON, (controlled by timer1): --------------------

	case 271: 
		Asp_Pump_off;		// MegaPump OFF!
		start_motor2 (DispSpeed, PlateTop+DISP_AIR_GAP);	// Run DispHead-lift down, high speed!
		LocalState++;
		break;

	case 272:
		if (!Motor2_Busy)	  // wait until motors is finnished.
			LocalState++;
		break;

	case 273: 
		if (PressureOk > 5)	  	// wait here for correct pressure!
		{
			if (SensorError)
			{
				PressureError = 1;		// Set error to Main!
				break;
			}

			if(P_Fine_Reg < 3)		// If High accuracy regulation:
				P_Fine_Reg = 3;   		// -> Limited accuracy regulation!

			if (Current_Cmd == DISP1 || Current_Cmd == DISPL1)
			{
				Timer_100uS = adjust_disptime (DispTime, 1);	// Adjust valve-time to current pressure!
				set_Disp_Valve1; // Set port adress (Valave1)
			}
			else if (Current_Cmd == DISP2 || Current_Cmd == DISPL2)
			{
				Timer_100uS = adjust_disptime (DispTime, 2);	// Adjust valve-time to current pressure!
				set_Disp_Valve2; // Set port adress (Valave1)
			}
			else if (Current_Cmd == DISP3 || Current_Cmd == DISPL3)
			{
				Timer_100uS = adjust_disptime (DispTime, 3);	// Adjust valve-time to current pressure!
				set_Disp_Valve3; // Set port adress (Valave1)
			}
			else if (Current_Cmd == DISP4 || Current_Cmd == DISPL4)
			{
				Timer_100uS = adjust_disptime (DispTime, 4);	// Adjust valve-time to current pressure!
				set_Disp_Valve4; // Set port adress (Valave1)
			}

			DispFlag = 1;		// Valve ON! (Timer1, Timer_100uS).
			LocalState++;
		}
		break;

			  //------- wait while valve is open! (No pressure-regulation!): --

	case 274:	if (!DispFlag)			// Wait while valve is open!
					LocalState++;
		break;


		//------- Short Pause, after disp: ----------------------------

	case 275: Timer_1mS = 100;		// Pause = 25mS
		LocalState++;
		break;

	case 276:	if (!Timer_1mS)			// Wait...
					LocalState++;
		break;


		//------- Return from Sub-Rutine: ----------------------------------

	case 277:	LocalState = ReturnState;	// Return from DISP-SUBRUTINE!
		break;




		//-------------------------------------------------------------------
		//--  ASP-SUBRUTINE:
		//-------------------------------------------------------------------
		//--  1. Lift-Down, Plate-Top
		//--  2. Lift-Down, Well-Bottom
		//--  3. Asp-Pause (AspTime)
		//--  4. Lift-Up, Plate-Top + x mm.
		//-------------------------------------------------------------------

	case 280: if (!Motor0_Busy && !Motor1_Busy)	  // wait until motor is finnished.
				  LocalState++;
		break;

		//------- Run AspHead-lift down: -----------------------------------

	case 281:	start_motor1 (Asp_Hi_Speed, PlateTop+ASP_AIR_GAP);	// Run AspHead-lift down, high speed!
		start_motor2 (DispSpeed, Param[0].M2_HomePos);		// Run Disp-lift up!
		LocalState++;
		break;

	case 282: if (!Motor1_Busy)	// Wait for lift-movement!
				  LocalState++;
		break;


		//------- Run AspHead-lift down: -----------------------------------

	case 283:	start_motor1 (Asp_Lo_Speed, Asp_Lo_Pos);	// Run AspHead-lift down, low speed!
		LocalState++;
		break;

	case 284: if (!Motor1_Busy)	// Wait for lift-movement!
				  LocalState++;
		break;


		//------- Asp-Pause/2, at buttom of well: ----------------------------

	case 285: Timer_1mS = AspTime/2;		// Pause = AspTime/2!
		LocalState++;
		break;

	case 286:	if (!Timer_1mS)			// Wait...
				{
					Timer_1mS = AspTime/2;	// Pause = AspTime/2!
					LocalState++;
				}
				break;

				//------- Asp-Sweep, at buttom of well: ----------------------------

	case 287: if (AspSweep)
			  {
				  SweepPos = NextPos - AspOffset - AspOffset + 1;	// 1/100 mm!
				  start_motor0 (4, SweepPos);			// Sweep.
			  }
			  LocalState++;
			  break;

	case 288: if (!Motor0_Busy)	// Wait for sweep-movement!
				  LocalState++;
		break;


		//------- Asp-Pause/2, at buttom of well: ----------------------------

	case 289:	if (!Timer_1mS)			// Wait...
					LocalState++;
		break;

		//------- Run AspHead-lift up: ------------------------------------

	case 290:	start_motor1 (Asp_Hi_Speed, PlateTop+ASP_AIR_GAP);	// Run lift UP!
		LocalState++;
		break;

	case 291: if (!Motor1_Busy)	// Wait for lift-movement!
				  LocalState++;
		break;


		//------- Return from Sub-Rutine: ----------------------------------

	case 292:	LocalState = ReturnState;	// Return from ASP-SUBRUTINE!
		break;





		//**************************************************************
		//
		//       Soak (pause for n sek.):
		//
		//**************************************************************

	case 300: start_motor0 (RunSpeed, SoakPos);	// Run to 1. DispPos.
		LocalState++;

	case 301: if (!Motor0_Busy)		// wait until carriage-motor is finnished.
				  LocalState++;
		break;

	case 302: Asp_Pump_off;			// MegaPump OFF (in case of running)!
		Timer_1sec = SoakTime;		// Start Soak-timer!
		SecFlag = 1;			// Set display-flag.
		LocalState++;

		//------- Soak Pause: ----------------------------------------------

	case 303: if (SecFlag)	// Loop while SoakTime: A new 1sec value?
			  {
				  SecFlag = 0;			// Clear Display-Flag.
				  sprintf(LcdTxt, "%d:%02d:%02d ", Timer_1sec/3600, (Timer_1sec%3600)/60, Timer_1sec%60);
				  lcd_puts (InfoLine+12, LcdTxt);	// Write remaining time!

				  if (Timer_1sec == 0)
					  LocalState++;
			  }
			  break;

			  //------- Soak End: ------------------------------------------------


	case 304: start_motor2 (DispSpeed, Param[0].M2_HomePos);	// Run Disp-lift up! (In case of Soak is first command)
		LocalState++;

	case 305: if (!Motor2_Busy)	// wait until Disp-motor is finnished.
				  LocalState++;
		break;

	case 306: LocalState = 20;	// Soak finnished!
		break;



		//**************************************************************
		//
		//       Program Finnished!
		//
		//**************************************************************

	case 1000: /***** Program Finnished! *******/

		CurrentADC = 0;
		P_Fine_Reg = 0;			// Not High accuracy regulation!
		start_motor2 (DispSpeed, Param[0].M2_HomePos);		// Vert.Upper head position
		start_motor1 (Asp_Hi_Speed, Param[0].M1_HomePos);	// Vert.Upper head position
		LocalState++;
		break;

	case 1001: if (!Motor1_Busy && !Motor2_Busy)	// wait until motor is finnished.
				   LocalState++;
		break;

	case 1002: start_motor0 (RunSpeed, Param[0].M0_HomePos);	// Goto Home-position.
		LocalState++;
		break;

	case 1003: if (!Motor0_Busy)	// wait until motor is finnished.
				   LocalState++;
		break;

	case 1004: Asp_Pump_off;		// Stop MegaPump    (in case of running)!
		Vac_Pump_off;		// Stop Vacuum-Pump (in case of running)!
		Waste_Pump_off;		// Stop Waste-Pump  (in case of running)!
		Pgm_Finnished = TRUE; break;  // command = finnished!

	}	/* End switch */

	*PrgmState = LocalState;
	return Pgm_Finnished;
}

