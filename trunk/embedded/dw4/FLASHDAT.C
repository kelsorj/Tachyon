#include "struct.h"
#include "motor.h"

//***********************************************************************
//* FILE        : flashdat.c
//*
//* DESCRIPTION : Parameters and user programs stored in Flash-EPROM
//*
//***********************************************************************

//------------------------------------------------------------------------------
// Const Data in flash-PROM:
//------------------------------------------------------------------------------
// extern const unsigned char FwID[3];				// Firmware ID (New adresses!)
// extern const char FirmwareVerTxt[64];				// Firmware Version
// extern const struct ParamBlockType CParam; =  		// Default parameters
// extern const struct SubProgInfoType ProgInfo;			// Block 0.
// extern const struct SubProgramType Program[MAX_SUBPROGRAMS];	// 99 program blocks
//------------------------------------------------------------------------------

//------------------------------------------------------------------------------
// Adress loacate's (at end of this file):
//------------------------------------------------------------------------------
// #pragma locate (FwID = 0x0FD0000)		// FD-0000: Identifier (A5,AA,A5)
// #pragma locate (FirmwareVerTxt = 0x0FD0003)	// FD-0003: FirmwareVerTxt[64]
// #pragma locate (CParam = 0x0FD0100)		// FD-0100: CParam[256 x 1]
// #pragma locate (ProgInfo = 0x0FD1000)	// FD-1000: ProgInfo[256 x 1]
// #pragma locate (Program = 0x0FD2000)      	// FD-2000: Program[256 x 99]
//------------------------------------------------------------------------------
// #pragma locate (NoCode1 = 0x0FEFFFA)		// FE-FFFA: NoCode (6 bytes)
// #pragma locate (NoCode2 = 0x0FFFFFA)		// FE-FFFA: NoCode (6 bytes)
//------------------------------------------------------------------------------

//-------------------------------------------------------------------
const unsigned char FwID[3] = {0xA5, 0xAA, VerCode};	// Firmware ID (New adresses!)
const char FirmwareVerTxt[64] = VerTxt;		// Firmware Version

//-------------------------------------------------------------------
// const char NoCode1[6] = "NoCode";	// 6 upper bytes in 64k page (not to be used as code!)
// const char NoCode2[6] = "NoCode";	// 6 upper bytes in 64k page (not to be used as code!)
//-------------------------------------------------------------------


//-------------------------------------------------------------------
// Default parameters (parameters stored in EEPROM):
//-------------------------------------------------------------------

const struct ParamBlock CParam[1] =  /* default parameters */
{
   {
    Serial_No_Txt,	//  char Serial Number[32]	(Blokk 0)
    0xAA55A55AL,	//  PromCode;
    5,1,1,		//  byte FirstDate[3];		// year, month, day
    5,1,1,		//  byte LastDate[3];		// year, month, day
    DevCode1,		//  word DeviceCode1;		// 10 = AquaMax 1536,11 = AquaMax 96/384.
    DevCode2,		//  word DeviceCode2;		// 21 = Dispencer V2.0
//------------------
    1708,		//  word M0_Tacho (17.07 = 0.0586mm)
    7720,		//  word M1_Tacho (77.07 = 0.01295mm)
    7720,		//  word M2_Tacho (12.20 = 0.01295mm)
    SENSOR0_POS,	//  int  Sensor0_TachoPos
    SENSOR1_POS,	//  int  Sensor1_TachoPos
    SENSOR2_POS,	//  int  Sensor2_TachoPos
    M0_HOMEPOS,		//  DispHome	        	 int x 10
    M1_HOMEPOS,		//  M1_HomePos (Upper Pos)	int x 100
    M2_HOMEPOS,		//  M2_HomePos (Upper Pos)	int x 100
    20,			//  AspSpeed1 (2mm/s)
    200,		//  AspSpeed2 (20mm/s)
    300,		//  AspSpeed3 (30mm/s)
//------------------
    NORM_PRESSURE,	//  DispPressure (mBar) (x 1)
    150,		//  DispPause (mS) 	(x 1)
    120,		//  IdlePressure Timeout (180 seconds = 3 minutes)
    650,		//  Prime Pressure (600mBar)
    1000,		//  Prime ValveTime4 (100.0mS)
//------------------
    PRESSURE_OFS,	//  Pressure ZeroOffset 0-4 (241=294mV)
    PRESSURE_OFS,	//  Pressure ZeroOffset 0-4 (241=294mV)
    PRESSURE_OFS,	//  Pressure ZeroOffset 0-4 (241=294mV)
    PRESSURE_OFS,	//  Pressure ZeroOffset 0-4 (241=294mV)
    PRESSURE_OFS,	//  Pressure ZeroOffset 0-4 (241=294mV)
    PRESSURE_OFS,	//  Pressure ZeroOffset  5  (Vacuum)
    PRESSURE_CAL,	//  Pressure Cal 0-4
    PRESSURE_CAL,	//  Pressure Cal 0-4
    PRESSURE_CAL,	//  Pressure Cal 0-4
    PRESSURE_CAL,	//  Pressure Cal 0-4
    PRESSURE_CAL,	//  Pressure Cal 0-4
    PRESSURE_CAL,	//  Pressure Cal 5 (Vacuum)
//-row--------------
    DISP1_POS,		//  DispPos1 (Head1 PlateEdge) 	 int x 10
    DISP2_POS,		//  DispPos2 (Head2 PlateEdge) 	 int x 10
    DISP3_POS,		//  DispPos3 (Head3 PlateEdge) 	 int x 10
    DISP4_POS,		//  DispPos4 (Head4 PlateEdge) 	 int x 10
    ASP_POS,		//  AspPos   (Head5 PlateEdge) 	 int x 10
//-col--------------
    DISP1_POS-RowColDiff,	//  DispPos1 (Head1 PlateEdge, Col-direction)  int x 100
    DISP2_POS-RowColDiff,	//  DispPos2 (Head2 PlateEdge, Col-direction)  int x 100
    DISP3_POS-RowColDiff,	//  DispPos3 (Head3 PlateEdge, Col-direction)  int x 100
    DISP4_POS-RowColDiff,	//  DispPos4 (Head4 PlateEdge, Col-direction)  int x 100
    ASP_POS-RowColDiff,		//  AspPos   (Head5 PlateEdge, Col-direction)  int x 100
//-row--------------
    116,107,96,		//  Liq1Cal (ml/sec)	int x 1
    116,107,96,		//  Liq2Cal (ml/sec)	int x 1
    116,107,96,		//  Liq3Cal (ml/sec)	int x 1
    116,107,96,		//  Liq4Cal (ml/sec)	int x 1
//-col--------------
    116,107,96,		//  Liq1Cal (ml/sec)	int x 1
    116,107,96,		//  Liq2Cal (ml/sec)	int x 1
    116,107,96,		//  Liq3Cal (ml/sec)	int x 1
    116,107,96,		//  Liq4Cal (ml/sec)	int x 1
//-row--------------
    0,33,41,		//  Disp1TimeCorr (mS)	int x 10
    0,33,41,		//  Disp1TimeCorr (mS)	int x 10
    0,33,41,		//  Disp1TimeCorr (mS)	int x 10
    0,33,41,		//  Disp1TimeCorr (mS)	int x 10
//-col--------------
    -20,25,37,		//  Disp1TimeCorr (mS)	int x 10
    -20,25,37,		//  Disp1TimeCorr (mS)	int x 10
    -20,25,37,		//  Disp1TimeCorr (mS)	int x 10
    -20,25,37,		//  Disp1TimeCorr (mS)	int x 10
//-row--------------
    4400,1400,600,	// Prime ValveTime1 (250.0mS)
    1,			// Number of Prime1 Repetitions
    40,40,40,		// Prime ValveTime2 (4.0mS)
    27,18,14,		// Number of Prime2 Repetitions
    400,200,150,	// Prime ValveTime3 (40.0mS)
    42,26,24,		// Number of Prime3 Repetitions
//-col--------------
    7800,1950,1000,	// Prime ValveTime1 (250.0mS)
    1,			// Number of Prime1 Repetitions
    40,40,40,		// Prime ValveTime2 (4.0mS)
    34,20,16,		// Number of Prime2 Repetitions
    500,250,190,	// Prime ValveTime3 (40.0mS)
    65,28,25,		// Number of Prime3 Repetitions
//------------------
    "Unused!\0"		// 18 unused bytes
   }
};


//-------------------------------------------------------------------
//   Default block[0] (global) programs-data:
//-------------------------------------------------------------------

const struct ProgInfoBlock ProgInfo[1] =  /* default programs */
{
   {
    0,				//  byte Sub_PrgNo		(Blokk 0)
    "Skatron Default V1.2\0",	//  char FileName[32+1]
    "00000000",			//  byte FileDate[8];
    "Unused!\0"			//  226  unused bytes
   }
};


//-------------------------------------------------------------------
//   Program[100];  Default sub-programs:
//-------------------------------------------------------------------

const struct ProgramBlock Program[MAX_SUBPROGRAMS+1] =  /* user-programs */
{
   {
    1,                  // byte ProgNo
    "TestProg 1 \0",    // char ProgName[32+1]
    0,			// byte LocalEdit (0=not edited)
    0,			// byte RowColumn (0=Row, 1=Column)
    "Nunc 1536 2\0",	// char PlateName[32+1]
    3,			// byte Plate Type (1=96, 2=384, 3=1536)
    PL_TOP_3,		// word Plate Height (740 = 7,40mm)
    PL_DEPTH_3,		// word Plate Depth  (410 = 7,40-4,10 = 3.30mm)
    PL_OFFSET_3,	// word Plate Offset (790 = 7,90mm)
    100,		// word PlateVolume (Kun for PC-uppload).
    WELL_WELL_3,	// word PlateDbwc; Avstand mellom brønner (i 1/1000 mm)
			//      (Nominell avstand =  2.250mm(1536) ,  4.500(384) ,  9.000(96))
    0xffff,		// word PlateRows0; bit-flags  0-15; Hvilke rader som skal brukes i denne platen.
    0x0000,		// word PlateRows1; bit-flags 16-31; Hvilke rader som skal brukes i denne platen.
    0,			// word Asp-posisjon, offset fra senter av brønnen! (i 1/100 mm)
    100,100,100,100,	// word Liq1Factor - Liq4Factor;  //  100 = 1.00
    100,200,300,400,	// word DispLowPr1 - DispLowPr4;  //  mBar
    "??\0",		// 4  unused bytes
    DISP1,	ASP3,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    10,		0x050A,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0
   },

   {
    2,                  // ProgNo
    "TestProg 2 \0",    // ProgName[32+1]
    0,			// byte LocalEdit (0=not edited)
    0,			// byte RowColumn (0=Row, 1=Column)
    "Nunc 1536 2\0",	// char PlateName[32+1]
    3,			// byte Plate Type (1=96, 2=384, 3=1536)
    PL_TOP_3,		// word Plate Height (740 = 7,40mm)
    PL_DEPTH_3,		// word Plate Depth  (410 = 7,40-4,10 = 3.30mm)
    PL_OFFSET_3,	// word Plate Offset (790 = 7,90mm)
    100,		// word PlateVolume (Kun for PC-uppload).
    WELL_WELL_3,	// word PlateDbwc; Avstand mellom brønner (i 1/1000 mm)
			//      (Nominell avstand =  2.250mm(1536) ,  4.500(384) ,  9.000(96))
    0x0303,		// word PlateRows0; bit-flags  0-15; Hvilke rader som skal brukes i denne platen.
    0x0303,		// word PlateRows1; bit-flags 16-31; Hvilke rader som skal brukes i denne platen.
    0,			// word Asp-posisjon, offset fra senter av brønnen! (i 1/10 mm)
    100,100,100,100,	// word Liq1Factor - Liq4Factor;	//  100 = 1.00
    100,200,300,400,	// word DispLowPr1 - DispLowPr4;  //  mBar
    "??\0",		// 4  unused bytes
    DISP1,	ASP3,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    10,		0x050A,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0
   },

   {
    3,                  // ProgNo
    "TestProg 3 \0",    // ProgName[32+1]
    0,			// byte LocalEdit (0=not edited)
    0,			// byte RowColumn (0=Row, 1=Column)
    "Nunc 384\0",	// char PlateName[32+1]
    2,			// byte Plate Type (1=96, 2=384, 3=1536)
    PL_TOP_2,		// word Plate Height (740 = 7,40mm)
    PL_DEPTH_2,		// word Plate Depth  (410 = 7,40-4,10 = 3.30mm)
    PL_OFFSET_2,	// word Plate Offset (900 = 9,00mm)
    100,		// word PlateVolume (Kun for PC-uppload).
    WELL_WELL_2,	// word PlateDbwc; Avstand mellom brønner (i 1/1000 mm)
			//      (Nominell avstand =  2.250mm(1536) ,  4.500(384) ,  9.000(96))
    0xffff,		// word PlateRows0; bit-flags  0-15; Hvilke rader som skal brukes i denne platen.
    0x0000,		// word PlateRows1; bit-flags 16-31; Hvilke rader som skal brukes i denne platen.
    0,			// word Asp-posisjon, offset fra senter av brønnen! (i 1/10 mm)
    100,100,100,100,	// word Liq1Factor - Liq4Factor;	//  100 = 1.00
    100,200,300,400,	// word DispLowPr1 - DispLowPr4;  //  mBar
    "??\0",		// 4  unused bytes
    ASP2,	ASP_OFS, ASP2sweep, ASP_OFS, ASP2sweep,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    0x050A,	10,	0x0A0A,	-10,	0x0A0A,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0
   },

   {
    4,                  // ProgNo
    "TestProg 4 \0",    // ProgName[32+1]
    0,			// byte LocalEdit (0=not edited)
    0,			// byte RowColumn (0=Row, 1=Column)
    "Nunc 384\0",	// char PlateName[32+1]
    2,			// byte Plate Type (1=96, 2=384, 3=1536)
    PL_TOP_2,		// word Plate Height (740 = 7,40mm)
    PL_DEPTH_2,		// word Plate Depth  (410 = 7,40-4,10 = 3.30mm)
    PL_OFFSET_2,	// word Plate Offset (900 = 9,00mm)
    100,		// word PlateVolume (Kun for PC-uppload).
    WELL_WELL_2,	// word PlateDbwc; Avstand mellom brønner (i 1/1000 mm)
			//      (Nominell avstand =  2.250mm(1536) ,  4.500(384) ,  9.000(96))
    0xffff,		// word PlateRows0; bit-flags  0-15; Hvilke rader som skal brukes i denne platen.
    0x0000,		// word PlateRows1; bit-flags 16-31; Hvilke rader som skal brukes i denne platen.
    0,			// word Asp-posisjon, offset fra senter av brønnen! (i 1/10 mm)
    100,100,100,100,	// word Liq1Factor - Liq4Factor;	//  100 = 1.00
    100,100,300,400,	// word DispLowPr1 - DispLowPr4;  //  mBar
    "??\0",		// 4  unused bytes
    DISPL2,	ASP3,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    10,		0x050A,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0
   },

   {
    5,                  // ProgNo
    "TestProg 5 \0",    // ProgName[32+1]
    0,			// byte LocalEdit (0=not edited)
    0,			// byte RowColumn (0=Row, 1=Column)
    "Nunc 96\0",	// char PlateName[32+1]
    1,			// byte Plate Type (1=96, 2=384, 3=1536)
    PL_TOP_1,		// word Plate Height (1450 = 14,5mm)
    PL_DEPTH_1,		// word Plate Depth  (1140 = 14,50-11,40 = 3.10mm)
    PL_OFFSET_1,	// word Plate Offset (1120 = 11,20mm)
    100,		// word PlateVolume (Kun for PC-uppload).
    WELL_WELL_1,	// word PlateDbwc; Avstand mellom brønner (i 1/1000 mm)
			//      (Nominell avstand =  2.250mm(1536) ,  4.500(384) ,  9.000(96))
    0x00ff,		// word PlateRows0; bit-flags  0-15; Hvilke rader som skal brukes i denne platen.
    0x0000,		// word PlateRows1; bit-flags 16-31; Hvilke rader som skal brukes i denne platen.
    0,			// word Asp-posisjon, offset fra senter av brønnen! (i 1/10 mm)
    100,100,100,100,	// word Liq1Factor - Liq4Factor;	//  100 = 1.00
    100,200,300,400,	// word DispLowPr1 - DispLowPr4;  //  mBar
    "??\0",		// 4  unused bytes
    DISP1,	ASP3,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    END,	END,	END,	END,	END,
    10,		0x050A,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0,
    0,		0,	0,	0,	0
   }

};


//-------------------------------------------------------------------
// Parameters:
//-------------------------------------------------------------------
#pragma locate (FwID = 0x0FD0000)            // FD-0000: Identifier (A5,AA,A5)
#pragma locate (FirmwareVerTxt = 0x0FD0003)  // FD-0003: FirmwareVerTxt[64]
#pragma locate (CParam = 0x0FD0100)          // FD-0100: CParam[256 x 1]
//-------------------------------------------------------------------
// User Programs:
//-------------------------------------------------------------------
#pragma locate (ProgInfo = 0x0FD1000)        // FD-1000: ProgInfo[256 x 1]
#pragma locate (Program = 0x0FD2000)         // FD-2000: GrpProgram[256 x 100]
					     // FD-9000: (End UserPrograms)
//-------------------------------------------------------------------
// Prosessor error (no code in 6 last bytes in 64k page):
//-------------------------------------------------------------------
//#pragma locate (NoCode1 = 0x0FEFFFA)	     // FE-FFFA: NoCode (6 bytes)
//#pragma locate (NoCode2 = 0x0FFFFFA)	     // FE-FFFA: NoCode (6 bytes)
//-------------------------------------------------------------------



