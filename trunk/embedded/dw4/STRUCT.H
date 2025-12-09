#ifndef __STRUCT_H
#define __STRUCT_H

//**************************************************************************
//*  FUNCTION    : STRUCT.h
//*  METHODE     : Definitions for data-struct's
//		   (globdata.c & flashdat.c)
//**************************************************************************

#define   MAX_COMMANDS            50   // max number of programs in a subprogram
#define   MAX_SUBPROGRAMS         99   // max number of subprograms
#define   MAX_SPECIALPROGRAMS     9   // Max number of special programs, A0,A1,A2,A3.
#define   MAX_PROGRAMS            MAX_SUBPROGRAMS+MAX_SPECIALPROGRAMS
#define   MIN_SUBPROGRAMS         1     /* min number of subprograms MIN..MAX */

//-----------------------------------------------
// Commands (in a user-program):
//-----------------------------------------------
#define   END          	0     // end of program sequence.
#define   ASP1         	1     // Asp 1 (Low Speed)
#define   ASP2         	2     // Asp 2 (Medium Speed)
#define   ASP3         	3     // Asp 3 (High Speed)

// V2.0:
#define   ASP_OFS    	10     // change AspOffset value
#define   ASP1sweep    	11     // Asp 1 (Low Speed), sweep bottom (+/- asp offset)
#define   ASP2sweep    	12     // Asp 2 (Medium Speed)
#define   ASP3sweep    	13     // Asp 3 (High Speed)

#define   DISP1        	20	// Disp Liq1 (Head1)
#define   DISP2        	21	// Disp Liq2 (Head2)
#define   DISP3        	22	// Disp Liq3 (Head3)
#define   DISP4        	23	// Disp Liq4 (Head4)

#define   DISPL1       	50	// Disp Liq1 (Head1)
#define   DISPL2       	51	// Disp Liq2 (Head2)
#define   DISPL3       	52	// Disp Liq3 (Head3)
#define   DISPL4       	53	// Disp Liq4 (Head4)

#define   SOAK         	30	// Soak (Pause)

#define   REP0         	39	// Repeat  0 times (no repeat, only a reference-value)!
#define   REP1         	40	// Repeat  1 time
#define   REP2         	41	// Repeat  2 times
#define   REP3         	42	// Repeat  3 times
#define   REP4         	43	// Repeat  4 times
#define   REP5         	44	// Repeat  5 times
#define   REP6         	45	// Repeat  6 times
#define   REP7         	46	// Repeat  7 times
#define   REP8         	47	// Repeat  8 times
#define   REP9         	48	// Repeat  9 times
#define   REP10        	49	// Repeat 10 times

#define   ROW0		60	// Change Row0-flags.
#define   ROW1		61	// Change Row1-flags.

#define   PRIME1	70	// Prime Head 1
#define   PRIME2	71	// Prime Head 2
#define   PRIME3	72	// Prime Head 3
#define   PRIME4	73	// Prime Head 4


//-----------------------------------------------
// Spesial Programs: (internal programs, in addition to user-programs 1-99):
//-----------------------------------------------
#define   SERVICE_PROG1   MAX_SUBPROGRAMS+1	// S1 Prime 1.
#define   SERVICE_PROG2   MAX_SUBPROGRAMS+2	// S2 Prime 2.
#define   SERVICE_PROG3   MAX_SUBPROGRAMS+3	// S3 Prime 3.
#define   SERVICE_PROG4   MAX_SUBPROGRAMS+4	// S4 Prime 4.
#define   SERVICE_PROG5   MAX_SUBPROGRAMS+5	// S5 Rinse.
#define   SERVICE_PROG6	  MAX_SUBPROGRAMS+6	// S6 BackFlush program!
#define   SERVICE_PROG7	  MAX_SUBPROGRAMS+7	// S7 ??
#define   SERVICE_PROG8	  MAX_SUBPROGRAMS+8	// S8 ??
#define   SERVICE_PROG9	  MAX_SUBPROGRAMS+9	// S8 ??
//-----------------------------------------------



//------------------------------------------------------------------------
// User-programs: 1 info-block (Block0) + 99 SubProgramBlocks (programs 1-99):
//------------------------------------------------------------------------

//-------------------------------------------------------------------------------------------------------
struct CommandType
//-------------------------------------------------------------------------------------------------------
{
  byte Cmd;	// Command, (1 byte) (0-255)
		// ASP0=1, ASP1=2, ASP2=3 ….. , ASP9=10
		// DISP1=20, DISP2=21, DISP3=22, DISP4=23
		// SOAK=30 (time = sec)
		// Rep1=40, Rep2=41, …..Rep10=49
		// PROGLINK=50
		// End=0

  int Value;	// Command parameter, 2 bytes (0 - +/-32000).
		// ASP-velocity (1.0 -30.0 mm/s) (200 = 20.0 mm/s)
		// DISP volume (15 = 1,5ul)
		// SOAK-time (time in seconds) (1 - 32000 sec)
		// Repeat from CmdNr (first Cmd = 1, second Cmd = 2, )
		// PROGLINK (next prog. number)(1-99)
};		// Total = 3 bytes!


//-------------------------------------------------------------------------------------------------------
struct ProgInfoBlock		// 256 bytes
//-------------------------------------------------------------------------------------------------------
{
  byte	ProgNo; 		// 1 x (byte)	=    1 bytes  (0-0)	(Blokk 0, allways 0)
  char	FileName[32+1];		// 33 x (char)	=   33bytes  (1-33)	("ASCII-navn" + Null)
				// Shown at PowerUp/Edit. (32 chars, limited to 20 in use)

  byte	FileDate[8+1];		// 9 x (byte)	=    9 bytes  (34-42)	("31122001" + Null)
				// Shown at PowerUp/Edit.
  char	unused_bytes[256-43];	// 213 x (char)	=  213 bytes (46-255)
};				// Total	=  256 bytes!




//-------------------------------------------------------------------------------------------------------
struct ProgramBlock		// 256 bytes each subprogram! (Parameters + 50 commands)
//-------------------------------------------------------------------------------------------------------
{
  byte ProgNo;			//  1 x (byte)	=    1 bytes  (0-0)	Blokk 1-99
				// Program-nummer (1-99).
  char ProgName[32+1];		//  33 x (char)	=    33 bytes  (1-33)	"Einars program 1"
				// Space for 32 chars, but limited to max 15 in use.
				// Display eksempel: 	"P01: Einars program 1"
  byte LocalEdit;		// 1 x (byte)	=    1 bytes  (34-34)
				// 0 = Orginal PC-file, 1 = edited lokaly.
  // DKM 092309 RowColumn determines if the plate is oriented for row dispense or col dispense
  byte RowColumn;		// 1 x (byte)	=    1 bytes  (35-35)
  char PlateName[32+1];		//  33 x (char)	=    33 bytes  (36-68)	"Nunc abcdefghij"
				// Max 32 chars. (Limited by display line-length).
				// Display eksempel: 	"P01: Einars program 1"
				//			"     1536 Nunc abcdef"
  byte PlateType;		//  1 x (word)	=    2 bytes  (67-68)	1-3
				//  (1 = 96,   2=384,   3=1536)
  word PlateHeight;		//  1 x (word)	=    2 bytes  (69-70) 	int x 100
				// Plate height ( in 1/10 mm)
  word PlateDepth;		//  1 x (word)	=    2 bytes  (71-72) 	int x 100
				// Well depth from plate-top ( in 1/10 mm)
  word PlateOffset;		//  1 x (word)	=    2 bytes  (73-74) 	int x 100
				// Distence from plate-edge to center av firs well (in 1/100 mm)
  word PlateVolume;		//  1 x (word)	=    2 bytes  (75-76) 	int x 10
				// Max volum each well (500 = 5.0 ul) (Only used by PC-uppload).
  word PlateDbwc;		//  1 x (word)	=    2 bytes  (77-78) 	int x 1000
				// Distence between wells (in 1/1000 mm)
				// (Nominel distanse =  2.250mm(1536) ,  4.500(384) ,  9.000(96))
				// Edit by user (+/- 0.25mm)
  word PlateRows0;		//  1 x (word)	=    2 bytes  (80-81) 	32 bit-flags
				// Rows in use for current plate.
				// Bit 0 = row A, Bit 1 = row B, Bit 2 = row C, etc.
  word PlateRows1;		//  1 x (word)	=    2 bytes  (82-83) 	32 bit-flags
				// Rows in use for current plate. (1536 only)
				// Bit 0 = row Q, Bit 1 = row R, Bit 2 = row S, etc.
  int  AspOffset;		//  1 x (int)	=    2 bytes  (84-85) 	int x 10
				// Asp-posisjon, offset fra senter av brønnen! (i 1/10 mm)
				// Default = 0.0mm, max (1536=0.4)(384=1.3)(96=3.0)
  word Liq1Factor;		//  1 x (word)	=    2 bytes  (85-86) 	int x 100
  word Liq2Factor;		//  1 x (word)	=    2 bytes  (87-88) 	int x 100
  word Liq3Factor;		//  1 x (word)	=    2 bytes  (89-90) 	int x 100
  word Liq4Factor;		//  1 x (word)	=    2 bytes  (91-92) 	int x 100
				// Viskosity for the different liquids (100 = 1,00).
  word 	DispLowPr1;		//  1 x (word)	=    2 bytes  (94-95) 	(int x 1 mBar)
  word 	DispLowPr2;		//  1 x (word)	=    2 bytes  (96-97) 	(int x 1 mBar)
  word 	DispLowPr3;		//  1 x (word)	=    2 bytes  (98-99) 	(int x 1 mBar)
  word 	DispLowPr4;		//  1 x (word)	=    2 bytes  (100-101)	(int x 1 mBar)
				// Disp-pressure for the different liquids (30 - 550mBar).
  char 	unused_bytes[256-(102+150)];	// 4 x (char)	=   4 bytes (102-105)

  byte Command[50];		// 50 x (byte)	= 50 bytes (106-255)
  int  CmdValue[50];		// 50 x (int)	= 100 bytes (106-255)
};				//  Total	= 256 bytes!

//-------------------------------------------------------------------------------------------------------





//-------------------------------------------------------------------------------------------------------
struct ParamBlock	// 256 bytes
//-------------------------------------------------------------------------------------------------------
{
  char  SerialNoTxt[32];	// Serial Number (ASCII text string)
  unsigned long PromCode;
  byte  FirstYear;		// 00 = 2000 Year(offset).
  byte  FirstMonth;		// 01 = Month.
  byte  FirstDay;		// 01 = Date.
  byte  LastYear;		// 00 = 2000 Year(offset).
  byte  LastMonth;		// 01 = Month.
  byte  LastDay;		// 01 = Date.
  word  DeviceCode1;		// 0x10=Odin (0x2x = Embla)
  word  DeviceCode2;		// 0x21=Dispencer V2.0, 10=Washer, 30=Stacker, 40=PlateHandler
//------------------
  word  M0_Tacho;		// [1400 = 14.00 count/mm] (x 100) Tacho counts/mm (Lift motor)
  word  M1_Tacho;		// [ 350 =  3.50 count/mm] (x 100) Tacho counts/mm (Horisontal motor)
  word  M2_Tacho;		// [1400 = 14.00 count/mm] (x 100) Tacho counts/mm (Lift motor)
  int   Sensor0_TachoPos;	// [??? = 16.0mm]  (x M1_TachoCounts/100) 16.0 mm in tacho-counts!
  int   Sensor1_TachoPos;	// [??? =  2.0mm]  (x M2_TachoCounts/100)  2,0 mm in tacho-counts!
  int   Sensor2_TachoPos;	// 58[??? =  2.0mm]  (x M2_TachoCounts/100)  2,0 mm in tacho-counts!
  word  M0_HomePos;		// [951 = 95.1mm]  (x 10)
  word  M1_HomePos; 		// [220 = 22.0mm]  (x 10)
  word  M2_HomePos;		// [ 10 = 10.0mm]  (x 10)
  word  AspSpeed1;		// Asp Low-Speed (2mm/s)
  word  AspSpeed2;		// Asp Med-Speed (20mm/s)
  word  AspSpeed3;		// Asp High-Speed (30mm/s)
//------------------
  word  DispPressure;		// [550mbar] (x 1)
  word  DispPause;		// [150mS]   (x 1)
  word  MaxIdleTime;		// [180s]    IdlePressure Timeout (in seconds)
  word  PrimePress;		// [650mBar] Prime Pressure (mB)
  word  PrimeTime4;		// [100.0mS] Prime Maintenance Valve Time (x100uS)
//------------------
  int   PresOffset[6];		// Pres.Sensor Zero-offset.
  int   PresCal[6];              // Pres.Sensor Calibation.
//-row--------------
  word  DispPos1;		// [951 = 95.1mm]  (x 10)
  word  DispPos2;		// [ 79 =  7.9mm]  (x 10)
  word  DispPos3;		// [ 10 =  1.0mm]  (x 10)
  word  DispPos4;		// [ 10 =  1.0mm]  (x 10)
  word  AspPos;			// [250 = 25.0mm]  (x 10)
//-col--------------
  word  DispPos1_b;		// Colomn-direction
  word  DispPos2_b;		// Colomn-direction
  word  DispPos3_b;		// Colomn-direction
  word  DispPos4_b;		// Colomn-direction
  word  AspPos_b;		// Colomn-direction
//-row--------------
  byte  Liq1Cal[3];		// [ 90 = 90 ul/s] (x 1)   96/384/1536-head
  byte  Liq2Cal[3];		// [ 90 = 90 ul/s] (x 1)   96/384/1536-head
  byte  Liq3Cal[3];		// [ 90 = 90 ul/s] (x 1)   96/384/1536-head
  byte  Liq4Cal[3];		// [ 90 = 90 ul/s] (x 1)   96/384/1536-head
//-col--------------
  byte  Liq1Cal_b[3];		// Colomn-96/384/1536-head
  byte  Liq2Cal_b[3];		// Colomn-96/384/1536-head
  byte  Liq3Cal_b[3];		// Colomn-96/384/1536-head
  byte  Liq4Cal_b[3];		// Colomn-96/384/1536-head
//-row--------------
  char  Disp1TimeCorr[3];	// [ 25 = 2.5 mS]  (x 10)  96/384/1536-head  (2.5mS DispValve-Time-Correction)
  char  Disp2TimeCorr[3];	// [ 25 = 2.5 mS]  (x 10)  96/384/1536-head
  char  Disp3TimeCorr[3];	// [ 25 = 2.5 mS]  (x 10)  96/384/1536-head
  char  Disp4TimeCorr[3];	// [ 25 = 2.5 mS]  (x 10)  96/384/1536-head
//-col--------------
  char  Disp1TimeCorr_b[3];	// [ 25 = 2.5 mS]  (x 10)  96/384/1536-head  (2.5mS DispValve-Time-Correction)
  char  Disp2TimeCorr_b[3];	// [ 25 = 2.5 mS]  (x 10)  96/384/1536-head
  char  Disp3TimeCorr_b[3];	// [ 25 = 2.5 mS]  (x 10)  96/384/1536-head
  char  Disp4TimeCorr_b[3];	// [ 25 = 2.5 mS]  (x 10)  96/384/1536-head
//-row--------------
  word PrimePress1;		// [650mBar] Prime Pressure (mB)
  word PrimeTime1[3];		// [  50 =  5.0mS]   Prime Valve Time (x100uS)
  word PrimeTime2[3];		// [  50 =  5.0mS]   Prime Valve Time (x100uS)
  word PrimeTime3[3];		// [  50 =  5.0mS]   Prime Valve Time (x100uS)
  word PrimeCount1;		// [  50 =  5.0mS]   Number of Prime Repetitions
  byte PrimeCount2[3];		// [  50 =  5.0mS]   Number of Prime Repetitions
  byte PrimeCount3[3];		// [  50 =  5.0mS]   Number of Prime Repetitions
//-col--------------
  word PrimePress1_b;		// [650mBar] Prime Pressure (mB)
  word PrimeTime1_b[3];		// [  50 =  5.0mS]   Prime Valve Time (x100uS)
  word PrimeTime2_b[3];		// [  50 =  5.0mS]   Prime Valve Time (x100uS)
  word PrimeTime3_b[3];		// [  50 =  5.0mS]   Prime Valve Time (x100uS)
  word PrimeCount1_b;		// [  50 =  5.0mS]   Number of Prime Repetitions
  byte PrimeCount2_b[3];	// [  50 =  5.0mS]   Number of Prime Repetitions
  byte PrimeCount3_b[3];	// [  50 =  5.0mS]   Number of Prime Repetitions
//------------------
  char unused_bytes[256-228];	// 28 unused bytes (to fill the 256 bytes block-size)
};
//-------------------------------------------------------------------------------------------------------


#endif
