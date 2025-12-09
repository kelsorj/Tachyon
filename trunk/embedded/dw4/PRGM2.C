#include "globdata.h"

#include "keybrd.h"
#include "display.h"
#include "comm.h"

//***********************************************************************
//* FILE        : prgm2.c
//*
//* DESCRIPTION : Div. rutines regarding USER PROGRAMS
//*
//***********************************************************************

//---------------------------------------------------------
//----- GLOBAL DATA: --------------------------------------
//---------------------------------------------------------

char PrgNameTxt[24];
char PlateNameTxt[24];
char PrgStepsTxt[24];


byte Head1;	// Type of Head mounted! (check_heads())
byte Head2;	// Type of Head mounted!
byte Head3;	// Type of Head mounted!
byte Head4;	// Type of Head mounted!
byte Head5;	// Type of Head mounted!

byte Disp1;		// Commands in use in wanted program! (check_programs)
byte Disp2;		// Commands in use in wanted program!
byte Disp3;		// Commands in use in wanted program!
byte Disp4;		// Commands in use in wanted program!
byte DispLow1;		// Commands in use in wanted program! (check_programs)
byte DispLow2;		// Commands in use in wanted program!
byte DispLow3;		// Commands in use in wanted program!
byte DispLow4;		// Commands in use in wanted program!
byte Asp;		// Commands in use in wanted program!

byte ProgError;	// Head/Pressure/Buffer/Waste Conflicts for wanted program!
word PrgSteps;		// Total number of steps (inkluding REP)

//---------------------------------------------------------

void get_PrgNameTxt(byte PrgmNo);
void check_heads(void);
int  check_program (byte PrgmNo);
void display_prog_error(void);
void send_error_reply(void);

//---------------------------------------------------------

#define ProgLine   LCD2		// LCD-line
#define PlateLine  LCD3		// LCD-line
#define StatusLine LCD4		// LCD-line

#define Err1Line   LCD3		// LCD-line
#define Err2Line   LCD4		// LCD-line

//---------------------------------------------------------
//----- EXTERNAL FUNCTIONS --------------------------------
//---------------------------------------------------------

extern void lcd_puts (byte adr, const char *str);	// (display.c)
extern void lcd_putc (byte adr, byte data);		// (display.c)
extern void lcd_word (byte adr, word num);		// (display.c)
extern void lcd_adr (byte adr);				// (display.c)

extern byte get_key(void);
extern void delay_ms (word time);
extern void reply (far char *str);
//---------------------------------------------------------

//---------------------------------------------------------

//-----------------------------------------------------------------------
// get_PrgNameTxt ()	(Disp1, Disp2, Disp3, Disp4, Asp)
//-----------------------------------------------------------------------
void get_PrgNameTxt(byte PrgmNo)
{
	int i;

	// DKM 092309 I assume that there's really a space limitation, not
	//			  a limitation on the number of programs.  After all, the
	//			  programs could be really short OR long.  I need to figure
	//			  out how they are currently stored.
	//		      STRUCT.H line 121 says 256 bytes per program
	if (PrgmNo <= MAX_SUBPROGRAMS)	// User Programs?
	{

		//--------------------------------------------------
		// Make a ProgramName LCD-text:
		//--------------------------------------------------

		sprintf(PrgNameTxt, "P%02d: ", PrgmNo);
		// DKM 092309 max of 16 chars (15 + null) in a program name
		//			  note the small bug -- staring from 4+i
		//			  will actually overwrite the space char
		for (i=0; i<16; i++)
		{
			PrgNameTxt[4+i] = Program[PrgmNo-1].ProgName[i];
			if (PrgNameTxt[4+i] == 0)
			{
				while (i < 16)
					PrgNameTxt[4 + i++] = ' ';
			}
		}

		//--------------------------------------------------
		// Make a PlateName LCD-text:
		//--------------------------------------------------

		if (Program[PrgmNo-1].RowColumn == 0)
			strcpy (PlateNameTxt, "Row. ");
		else
			strcpy (PlateNameTxt, "Col. ");

		for (i=0; i<16; i++)
		{
			PlateNameTxt[4+i] = Program[PrgmNo-1].PlateName[i];
			if (PlateNameTxt[4+i] == 0)
			{
				while (i < 16)
					PlateNameTxt[4 + i++] = ' ';
			}
		}

		sprintf(PrgStepsTxt, "    %d steps         ", PrgSteps);
		PrgStepsTxt[20] = 0;		// Max 20 chars!
	}
	else		// Spesial Programs:
	{
		strcpy (PlateNameTxt, "                    ");
		strcpy (PrgStepsTxt,  "                    ");

		// DKM 092309 these service programs should be permanent -- can't be overwritten
		//			  looks like there are actually 9 service programs on the DW4 right now
		if(PrgmNo >= SERVICE_PROG1 && PrgmNo <= SERVICE_PROG4)
		{
			if      (ExtComVal2 == 1) strcpy (PlateNameTxt, "    Maintenance     ");	// Prime
			else if (ExtComVal2 == 2) strcpy (PlateNameTxt, "    Full Prime (Row)");	// PrimeR
			else if (ExtComVal2 == 3) strcpy (PlateNameTxt, "    Full Prime (Col)");	// PrimeC
		}

		switch(PrgmNo)
		{
		case SERVICE_PROG1:
			strcpy (PrgNameTxt,   "S1: Prime Head 1    ");
			break;
		case SERVICE_PROG2:
			strcpy (PrgNameTxt,   "S2: Prime Head 2    ");
			break;
		case SERVICE_PROG3:
			strcpy (PrgNameTxt,   "S3: Prime Head 3    ");
			break;
		case SERVICE_PROG4:
			strcpy (PrgNameTxt,   "S4: Prime Head 4    ");
			break;
		case SERVICE_PROG5:
			strcpy (PrgNameTxt,   "S5: Dispense Clean  ");
			break;
		case SERVICE_PROG6:
			strcpy (PrgNameTxt,   "S6: Aspirate Clean  ");
			strcpy (PlateNameTxt, "    (Rows)          ");
			break;
		case SERVICE_PROG7:
			strcpy (PrgNameTxt,   "S7: Flush           ");
			break;
		case SERVICE_PROG8:
			strcpy (PrgNameTxt,   "S8: Alignment       ");
			break;
		case SERVICE_PROG9:
			strcpy (PrgNameTxt,   "S9: Reverse Flush   ");
			strcpy (PlateNameTxt, "    (Vacuum On)     ");
			break;
		}
	}
	PrgNameTxt[20] = 0;	// No more than 20 chars!
	PlateNameTxt[20] = 0;	// No more than 20 chars!
}



//-----------------------------------------------------------------------
// check_heads ()	(Disp1, Disp2, Disp3, Disp4, Asp)
//			(Disp1/2 are different from Disp3/4 coding!)
//-----------------------------------------------------------------------
// DKM 092309 basic idea here:
//            round robin through all of the heads by selecting the
//			  appropriate mux address and reading the input bits for
//            the selected head
void check_heads(void)
{
	word t;
	byte Code;
	//--------------------------------------------------------------
	// Binary Codes from Head-Switches:
	//--------------------------------------------------------------
#define HeadCode0	0	// No-head!
#define HeadCode1	1	// 1536-head
#define HeadCode2	2	//  384-head (used for some 96-plates)
#define HeadCode3	3	//   96-head (not available)
	//--------------------------------------------------------------


	DispHead_1_Out;		// Select Head1_SW (multiplexed).
	for (t=0; t<15; t++) {}	// 150uS pause
	Code = DispHead_In;		// Read switches.
	switch(Code)
	{
	case HeadCode0: Head1 = 0;	// No Head!
		break;
	case HeadCode1: Head1 = 3;	// 1536-Head
		break;
	case HeadCode2: Head1 = 2;	//  384-Head
		break;
	case HeadCode3: Head1 = 1;	//   96-Head
		break;
	}

	DispHead_2_Out;		// Select Head2_SW (multiplexed).
	for (t=0; t<15; t++) {}	// 100uS
	Code = DispHead_In;	// Read switches.
	switch(Code)
	{
	case HeadCode0: Head2 = 0;	// No Head!
		break;
	case HeadCode1: Head2 = 3;	// 1536-Head
		break;
	case HeadCode2: Head2 = 2;	//  384-Head
		break;
	case HeadCode3: Head2 = 1;	//   96-Head
		break;
	}

	DispHead_3_Out;		// Select Head3_SW (multiplexed).
	for (t=0; t<15; t++) {}	// 100uS
	Code = DispHead_In;	// Read switches.
	switch(Code)
	{
	case HeadCode0: Head3 = 0;	// No Head!
		break;
	case HeadCode2: Head3 = 3;	// 1536-Head
		break;
	case HeadCode1: Head3 = 2;	//  384-Head
		break;
	case HeadCode3: Head3 = 1;	//   96-Head
		break;
	}

	DispHead_4_Out;		// Select Head4_SW (multiplexed).
	for (t=0; t<15; t++) {}	// 100uS
	Code = DispHead_In;	// Read switches.
	switch(Code)
	{
	case HeadCode0: Head4 = 0;	// No Head!
		break;
	case HeadCode2: Head4 = 3;	// 1536-Head
		break;
	case HeadCode1: Head4 = 2;	//  384-Head
		break;
	case HeadCode3: Head4 = 1;	//   96-Head
		break;
	}

	DispHead_5_Out;		// Select Head5_SW (ASP-Head, multiplexed).
	for (t=0; t<15; t++) {}	// 100uS
	Code = DispHead_In;	// Read switches.
	switch(Code)
	{
	case HeadCode0: Head5 = 0;	// No Head!
		break;
	case HeadCode1: Head5 = 3;	// 1536-Head
		break;
	case HeadCode2: Head5 = 2;	//  384-Head
		break;
	case HeadCode3: Head5 = 1;	//   96-Head
		break;
	}

	DispHead_0_Out;		// No Selected HeadSwitches!

}


//-----------------------------------------------------------------------
// check_program ()
//-----------------------------------------------------------------------
// check Platetype <-> Command <-> Headtype
// check Asp  <-> WasteAlarm
// check Disp <-> BufferAlarm
// check Command <-> HeadPressure
//-----------------------------------------------------------------------
int check_program (byte PrgmNo)
{
	int t;
	byte Current_Cmd;
	byte PlateType;
	byte LowBasePlate;	// Flag Extended Rim plate.
	word Repeat;		// Wanted repetitions.
	word RepCnt;		// Repetitions done.

	//--------------------------------------------------------------
	Disp1 = 0;
	Disp2 = 0;
	Disp3 = 0;
	Disp4 = 0;
	DispLow1 = 0;
	DispLow2 = 0;
	DispLow3 = 0;
	DispLow4 = 0;
	Asp   = 0;

	Pres1Alarm = 0;		// Ok!
	Pres2Alarm = 0;		// Ok!
	Pres3Alarm = 0;		// Ok!
	Pres4Alarm = 0;		// Ok!
	ProgError = 0;

	Repeat = 0;		// Wanted repetitions.
	RepCnt = 0;		// Repetitions done.
	PrgSteps = 0;
	//--------------------------------------------------------------


	check_heads();			// Find type of heads mounted!

	//--------------------------------------------------------------
	// Sjekk Pressure-error (use last pressure backup):
	//--------------------------------------------------------------

	if (mBar[6] >= 120)		// Internal pressure > 120mBar?
	{
		if (mBar[1] > (mBar[6]+100) || mBar[1] < (mBar[6]-100)) Pres1Alarm = 1;	// +/-100mBar ?
		if (mBar[2] > (mBar[6]+100) || mBar[2] < (mBar[6]-100)) Pres2Alarm = 1;	// +/-100mBar ?
		if (mBar[3] > (mBar[6]+100) || mBar[3] < (mBar[6]-100)) Pres3Alarm = 1;	// +/-100mBar ?
		if (mBar[4] > (mBar[6]+100) || mBar[4] < (mBar[6]-100)) Pres4Alarm = 1;	// +/-100mBar ?
	}
	else if (mBar[6] >= 40)	// Internal pressure > 40mBar?
	{
		if (mBar[1] > (mBar[6]+100) || mBar[1] < 10) Pres1Alarm = 1;	// +100mBar or < 10mBar?
		if (mBar[2] > (mBar[6]+100) || mBar[2] < 10) Pres2Alarm = 1;	// +100mBar ?
		if (mBar[3] > (mBar[6]+100) || mBar[3] < 10) Pres3Alarm = 1;	// +100mBar ?
		if (mBar[4] > (mBar[6]+100) || mBar[4] < 10) Pres4Alarm = 1;	// +100mBar ?
	}

#ifdef __TestBoard
	Pres1Alarm = 0;	// Ok!
	Pres2Alarm = 0;	// Ok!
	Pres3Alarm = 0;	// Ok!
	Pres4Alarm = 0;	// Ok!
#endif

	//---------------------------------------------------------------------
	// TEST! Head-Coding missing on prototype!
	//---------------------------------------------------------------------
#ifdef __SetHeadType
	PlateType  = 2;	// TEST! (384)
	Head1 = PlateType;	// TEST! Head-Coding missing on prototype!
	Head2 = PlateType;	// TEST! Head-Coding missing on prototype!
	Head3 = PlateType;	// TEST! Head-Coding missing on prototype!
	Head4 = PlateType;	// TEST! Head-Coding missing on prototype!
	Head5 = PlateType;	// TEST! Head-Coding missing on prototype!
#endif
	//---------------------------------------------------------------------

	if (PrgmNo <= MAX_SUBPROGRAMS)	// Normal program:
	{
		PlateType  = Program[PrgmNo-1].PlateType;

		if (PlateType > 10)
		{
			PlateType -= 10;
			LowBasePlate = 1;
		}
		else
			LowBasePlate = 0;

		//---------------------------------------------------------------------
		// TEST! Head-Coding missing on prototype!
		//---------------------------------------------------------------------
#ifdef __SetHeadType
		Head1 = PlateType;
		Head2 = PlateType;
		Head3 = PlateType;
		Head4 = PlateType;
		Head5 = PlateType;
#endif
		//---------------------------------------------------------------------

		//--------------------------------------------------------------
		// Sjekk om Disp- og Asp-kommandoer er i bruk:
		//--------------------------------------------------------------

		for (t=0; t<50; t++)	// Check all commands
		{
NewStart:
			Current_Cmd = Program[PrgmNo-1].Command[t];
			if (PrgSteps > 999)
			{
				t = 50;
				break;
			}


			switch(Current_Cmd)
			{
			case DISP1: Disp1 = 1; PrgSteps++; break;	// Disp Liq1 (Head1)
			case DISP2: Disp2 = 1; PrgSteps++; break; 	// Disp Liq2 (Head2)
			case DISP3: Disp3 = 1; PrgSteps++; break; 	// Disp Liq3 (Head3)
			case DISP4: Disp4 = 1; PrgSteps++; break; 	// Disp Liq4 (Head4)

			case DISPL1: Disp1 = 1; DispLow1 = 1; PrgSteps++; break;	// DispLowPressure Liq1 (Head1)
			case DISPL2: Disp2 = 1; DispLow2 = 1; PrgSteps++; break; 	// DispLowPressure Liq2 (Head2)
			case DISPL3: Disp3 = 1; DispLow3 = 1; PrgSteps++; break; 	// DispLowPressure Liq3 (Head3)
			case DISPL4: Disp4 = 1; DispLow4 = 1; PrgSteps++; break; 	// DispLowPressure Liq4 (Head4)

			case ASP1:     				// Asp (Head5)
			case ASP2:     				// Asp (Head5)
			case ASP3: Asp = 1; PrgSteps++; break;	// Asp (Head5)

			case ASP1sweep:     				// Asp (Head5)
			case ASP2sweep:     				// Asp (Head5)
			case ASP3sweep: Asp = 1; PrgSteps++; break;	// Asp (Head5)

			case SOAK: PrgSteps++; break;

			case ROW0:
			case ROW1: PrgSteps++; break;

			case REP1 :
			case REP2 :
			case REP3 :
			case REP4 :
			case REP5 :
			case REP6 :
			case REP7 :
			case REP8 :
			case REP9 :
			case REP10:
				Repeat = Current_Cmd - REP1 + 1; // Number of repetitons.
				if (RepCnt < Repeat)	// If all repetitions not done:
				{
					RepCnt++;
					t = Program[PrgmNo-1].CmdValue[t]-1;	// Jump to Repeat From Command!
					goto NewStart;
				}
				else
				{
					Repeat = 0;
					RepCnt = 0;
				}
				break;

			case END:  t = 50;	// Finnished.
				break;

			}	// End switch()
		}		// All Commands checked!


		//--------------------------------------------------------------
		// Sjekk om Disp-kommandoer og plate-type passer til Disp-Hoder:
		//--------------------------------------------------------------

		if (Disp1)
		{
			if      (Head1 == 0) ProgError = 0x10;
			else if (PlateType == 3 && Head1 != 3) ProgError = 0x10 + Head1;
			else if (PlateType == 2 && Head1 != 2) ProgError = 0x10 + Head1;
			else if (PlateType == 1 && Head1 != 1) ProgError = 0x10 + Head1;
		}
		if (Disp2 && !ProgError)
		{
			if      (Head2 == 0) ProgError = 0x20;
			else if (PlateType == 3 && Head2 != 3) ProgError = 0x20 + Head2;
			else if (PlateType == 2 && Head2 != 2) ProgError = 0x20 + Head2;
			else if (PlateType == 1 && Head2 != 1) ProgError = 0x20 + Head2;
		}
		if (Disp3 && !ProgError)
		{
			if      (Head3 == 0) ProgError = 0x30;
			else if (PlateType == 3 && Head3 != 3) ProgError = 0x30 + Head3;
			else if (PlateType == 2 && Head3 != 2) ProgError = 0x30 + Head3;
			else if (PlateType == 1 && Head3 != 1) ProgError = 0x30 + Head3;
		}
		if (Disp4 && !ProgError)
		{
			if      (Head4 == 0) ProgError = 0x40;
			else if (PlateType == 3 && Head4 != 3) ProgError = 0x40 + Head4;
			else if (PlateType == 2 && Head4 != 2) ProgError = 0x40 + Head4;
			else if (PlateType == 1 && Head4 != 1) ProgError = 0x40 + Head4;
		}
		if (Asp && !ProgError)
		{
			if      (Head5 == 0) ProgError = 0x50;
			else if (PlateType == 3 && Head5 != 3) ProgError = 0x50 + Head5;
			else if (PlateType == 2 && Head5 != 2) ProgError = 0x50 + Head5;
			else if (PlateType == 1 && Head5 != 1) ProgError = 0x50 + Head5;
		}

		if (LowBasePlate)
			if (Disp4 && !ProgError)
			{
				if      (Head1) ProgError = 0x14;
				else if (Head2) ProgError = 0x24;
				else if (Head3) ProgError = 0x34;
			}
			//--------------------------------------------------------------
			// Sjekk om Pressure er ok:
			//--------------------------------------------------------------

			if (!ProgError)
			{
				if      (Disp1 && Pres1Alarm) ProgError = 0x61;
				else if (Disp2 && Pres2Alarm) ProgError = 0x62;
				else if (Disp3 && Pres3Alarm) ProgError = 0x63;
				else if (Disp4 && Pres4Alarm) ProgError = 0x64;
				//---------------------------------------------------
			}

			//--------------------------------------------------------------
			// Sjekk om Buffer/Waste er ok:
			//--------------------------------------------------------------
			if (!ProgError)
			{
				if      (Disp1 && Buffer1Alarm) ProgError = 0x01;
				else if (Disp2 && Buffer2Alarm) ProgError = 0x02;
				else if (Disp3 && Buffer3Alarm) ProgError = 0x03;
				else if (Disp4 && Buffer4Alarm) ProgError = 0x04;
				else if (WasteAlarm)            ProgError = 0x05;
			}

	}	// End if(normal program)

	//--------------------------------------------------------------
	// Internal Service-programs:
	//--------------------------------------------------------------
	else
	{	// Service Programs:

		switch(PrgmNo)
		{
		case SERVICE_PROG1:	// S01: Prime 1
			if (WasteAlarm)   ProgError = 0x05;
			if (Buffer1Alarm) ProgError = 0x01;
			if (Pres1Alarm)   ProgError = 0x61;
			if (Head1 == 0)   ProgError = 0x10;
			break;
		case SERVICE_PROG2:	// S02: Prime 2
			if (WasteAlarm)   ProgError = 0x05;
			if (Buffer2Alarm) ProgError = 0x02;
			if (Pres2Alarm)   ProgError = 0x62;
			if (Head2 == 0)   ProgError = 0x20;
			break;
		case SERVICE_PROG3:	// S03: Prime 3
			if (WasteAlarm)   ProgError = 0x05;
			if (Buffer3Alarm) ProgError = 0x03;
			if (Pres3Alarm)   ProgError = 0x63;
			if (Head3 == 0)   ProgError = 0x30;
			break;
		case SERVICE_PROG4:	// S04: Prime 4
			if (WasteAlarm)   ProgError = 0x05;
			if (Buffer4Alarm) ProgError = 0x04;
			if (Pres4Alarm)   ProgError = 0x64;
			if (Head4 == 0)   ProgError = 0x40;
			break;
		case SERVICE_PROG5:	// S05: Disp-Clean
			if (WasteAlarm)   ProgError = 0x05;
			if (Head1 == 0 && Head2 == 0 && Head3 == 0 && Head4 == 0)   ProgError = 0x70;
			break;

		case SERVICE_PROG6:	// S06: Asp-Clean
			if (WasteAlarm)   ProgError = 0x05;
			if (Head5 == 0)   ProgError = 0x50;
			else if (Head1 == 0 && Head2 == 0 && Head3 == 0 && Head4 == 0)   ProgError = 0x70;
			break;

		case SERVICE_PROG7:	// S07: Flush
			if (WasteAlarm)   ProgError = 0x05;
			else if (Head1 == 0 && Head2 == 0 && Head3 == 0 && Head4 == 0)   ProgError = 0x70;
			break;

		case SERVICE_PROG8: // S08: Alignment
			break;
		case SERVICE_PROG9: // S09: Asp.Pump (Reverse Flush)
			break;
		}
	}	// End (Service Programs)


	return (ProgError);
}


//-----------------------------------------------------------------------
// display_prog_error()
//-----------------------------------------------------------------------

void display_prog_error(void)
{

	if (ProgError)
	{
		switch (ProgError)
		{
		case 0x01: lcd_puts (Err2Line, "Err:Liq.1 empty!    "); break;
		case 0x02: lcd_puts (Err2Line, "Err:Liq.2 empty!    "); break;
		case 0x03: lcd_puts (Err2Line, "Err:Liq.3 empty!    "); break;
		case 0x04: lcd_puts (Err2Line, "Err:Liq.4 empty!    "); break;
		case 0x05: lcd_puts (Err2Line, "Err:Waste full!     "); break;

		case 0x10: lcd_puts (Err2Line, "Err:Head1=absent!   "); break;
		case 0x11: lcd_puts (Err2Line, "Err:Head1=96        "); break;
		case 0x12: lcd_puts (Err2Line, "Err:Head1=384       "); break;
		case 0x13: lcd_puts (Err2Line, "Err:Head1=1536      "); break;
		case 0x14: lcd_puts (Err2Line, "Err:Remove Head1!   "); break;

		case 0x20: lcd_puts (Err2Line, "Err:Head2=absent!   "); break;
		case 0x21: lcd_puts (Err2Line, "Err:Head2=96        "); break;
		case 0x22: lcd_puts (Err2Line, "Err:Head2=384       "); break;
		case 0x23: lcd_puts (Err2Line, "Err:Head2=1536      "); break;
		case 0x24: lcd_puts (Err2Line, "Err:Remove Head2!   "); break;

		case 0x30: lcd_puts (Err2Line, "Err:Head3=absent!   "); break;
		case 0x31: lcd_puts (Err2Line, "Err:Head3=96        "); break;
		case 0x32: lcd_puts (Err2Line, "Err:Head3=384       "); break;
		case 0x33: lcd_puts (Err2Line, "Err:Head3=1536      "); break;
		case 0x34: lcd_puts (Err2Line, "Err:Remove Head3!   "); break;

		case 0x40: lcd_puts (Err2Line, "Err:Head4=absent!   "); break;
		case 0x41: lcd_puts (Err2Line, "Err:Head4=96        "); break;
		case 0x42: lcd_puts (Err2Line, "Err:Head4=384       "); break;
		case 0x43: lcd_puts (Err2Line, "Err:Head4=1536      "); break;

		case 0x50: lcd_puts (Err2Line, "Err:Head5=absent!   "); break;
		case 0x51: lcd_puts (Err2Line, "Err:Head5=96        "); break;
		case 0x52: lcd_puts (Err2Line, "Err:Head5=384       "); break;
		case 0x53: lcd_puts (Err2Line, "Err:Head5=1536      "); break;

		case 0x61: lcd_puts (Err2Line, "Err:Liq.1 Pressure! "); break;
		case 0x62: lcd_puts (Err2Line, "Err:Liq.2 Pressure! "); break;
		case 0x63: lcd_puts (Err2Line, "Err:Liq.3 Pressure! "); break;
		case 0x64: lcd_puts (Err2Line, "Err:Liq.4 Pressure! "); break;

		case 0x70: lcd_puts (Err2Line, "Err:No Dispheads!   "); break;
		}
	}

}


//-----------------------------------------------------------------------
// send_error_reply()
//-----------------------------------------------------------------------
void send_error_reply(void)
{
	//--------------------------------------------------------------
	// Robot communication (Error-reply-string):
	//--------------------------------------------------------------

	switch(ProgError)
	{
	case 0x00: reply (READY_ACK); break;	// No errors!

	case 0x10:
	case 0x11:
	case 0x12:
	case 0x13:
	case 0x14: reply (H1_ALARM_ACK); break;	// Head1-error

	case 0x20:
	case 0x21:
	case 0x22:
	case 0x23:
	case 0x24: reply (H2_ALARM_ACK); break;	// Head2-error

	case 0x30:
	case 0x31:
	case 0x32:
	case 0x33:
	case 0x34: reply (H3_ALARM_ACK); break;	// Head3-error

	case 0x40:
	case 0x41:
	case 0x42:
	case 0x43: reply (H4_ALARM_ACK); break;	// Head4-error

	case 0x50:
	case 0x51:
	case 0x52:
	case 0x53: reply (H5_ALARM_ACK); break;	// Head5-error

	case 0x61: reply (P1_ALARM_ACK); break;	// Pressure1-error
	case 0x62: reply (P2_ALARM_ACK); break;	// Pressure2-error
	case 0x63: reply (P3_ALARM_ACK); break;	// Pressure3-error
	case 0x64: reply (P4_ALARM_ACK); break;	// Pressure4-error

	case 0x01: reply (L1_ALARM_ACK); break;	// Liquid1-error
	case 0x02: reply (L2_ALARM_ACK); break;	// Liquid2-error
	case 0x03: reply (L3_ALARM_ACK); break;	// Liquid3-error
	case 0x04: reply (L4_ALARM_ACK); break;	// Liquid4-error
	case 0x05: reply (WA_ALARM_ACK); break;	// Waste-error

	default:	  reply (E0_ALARM_ACK); break;	// Unknown-error
	}

}
