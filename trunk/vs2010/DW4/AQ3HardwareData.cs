using System;
using System.Runtime.InteropServices;

namespace AQ3
{
	public class AQ3HardwareData
	{
		public AQ3HardwareData()
		{
		}
	}

	//-------------------------------------------------------------------------------------------------------
	public class ProgInfoBlock	// 256 bytes
	//-------------------------------------------------------------------------------------------------------
	{
		public byte	SubPrgNo; 	//     1 x (byte)	=    1 bytes  (0-0)	Blokk 0
		// Dette er kun et blokknummer (alltid 0) som skiller denne blokken 
		// fra programblokkene (1-99).

		public byte[]	FileName = new byte[32+1];	//   33 x (char)	=   33bytes  (1-33) 	(”ASCII-navn” + Null)
		// displayet blir 20 karakterer pr. linje. Vises ved oppstart / Edit.
		// Avsetter plass til 32 karakterer, men begrenser til max 20 i bruk.

		public byte[]	FileDate = new byte[8+1];	//     9 x (byte)	=    9 bytes  (34-42)	(”31122001” + Null) 
		// Viser når filen er laget/modifisert på PC.
		// Vises i display ved Oppstart / Edit.

		public byte[]	unused_bytes = new byte[256-43];	// 213 x (char)	= 213 bytes (43-255)
	};

	//-------------------------------------------------------------------------------------------------------
	public class ProgramBlock	// 256 bytes each subprogram!
	//-------------------------------------------------------------------------------------------------------
	{
		public byte	SubPrgNo;	//  1 x (byte)	=    1 bytes  (0-0)	Blokk 1-99
		// Program-nummer (99 programmer er tilstrekkelig).

		public byte[]	SubPrgName = new byte[32+1];	//  33 x (char)	=    33 bytes  (1-33)	«Einars program 1» 
		// Avsetter plass til 32 karakterer, men begrenser til max 15 i bruk. 
		// Display eksempel: 	”P01:Einars program 1 ”

		public byte	LocalEdit;	//     1 x (byte)	=    1 bytes  (34-34)
		// 0 = Orginal PC-fil, 1 = editert lokalt på maskinen.

		public byte[] 	PlateName = new byte[32+1];	//  33 x (char)	=    33 bytes  (36-68)	«Nunc abcdefghij» 
		// Max 32 karakterer. (Displayet viser bare så mange det er plass til).
		// Display eksempel: 	”P01:Einars program 1 ”
		//						”    1536 Nunc abcdefg”
			
		public byte 	PlateType;	//  1 x (byte)	=    1 bytes  (69-69)	1-3
		//  (1 = 96,   2=384,   3=1536) 

		public ushort 	PlateHeight;	//  1 x (word)	=    2 bytes  (70-71) 	int x 100
		// Platens høyde ( i 1/100 mm)

		public ushort 	PlateDepth;	//  1 x (word)	=    2 bytes  (72-73) 	int x 100
		// Brønnens dybde fra platetop ( i 1/100 mm)

		public ushort 	PlateOffset;	//  1 x (word)	=    2 bytes  (74-75) 	int x 100
		// Avstand fra platekant til senter av første brønn (i 1/100 mm)

		public ushort 	PlateVolume;	//  1 x (word)	=    2 bytes  (76-77) 	int x 10
		// Max volum pr. brønn (500 = 5.0 ul) (Kun for PC-uppload).

		public ushort 	PlateDbwc;	//  1 x (word)	=    2 bytes  (78-79) 	int x 1000
		// Avstand mellom brønner (i 1/1000 mm)
		// (Nominell avstand =  2.250mm(1536) ,  4.500(384) ,  9.000(96)) 
		// Nominell avstand skal kunne korrigeres av bruker (+/- 0.25mm)

		public ushort 	PlateRows0;	//  1 x (word)	=    2 bytes  (80-81) 	32 bit-flags
		// Hvilke rader som skal brukes i denne platen.
		// Bit 0 = row A, Bit 1 = row B, Bit 2 = row C, osv.
		
		public ushort 	PlateRows1;	//  1 x (word)	=    2 bytes  (82-83) 	32 bit-flags
		// Hvilke rader som skal brukes i denne platen (1536 only)
		// Bit 0 = row Q, Bit 1 = row R, Bit 2 = row S, osv.

		public ushort AspOffset;	//  1 x (int)	=    2 bytes  (84-85) 	int x 10
		// Asp-posisjon, offset fra senter av brønnen! (i 1/10 mm)
		// Default = 0.0mm, max (1536=1.125)(384=2.25)(96=4.5)

		public ushort 	Liq1Factor;	//  1 x (word)	=    2 bytes  (86-87) 	int x 100
		public ushort 	Liq2Factor;	//  1 x (word)	=    2 bytes  (88-89) 	int x 100
		public ushort 	Liq3Factor;	//  1 x (word)	=    2 bytes  (90-91) 	int x 100
		public ushort 	Liq4Factor;	//  1 x (word)	=    2 bytes  (92-93) 	int x 100
		// Viskositetsfaktor for de forskjellige væskene (100 = 1,00).
		
		public ushort 	DispLowPr1;	//  1 x (word)	=    2 bytes  (94-95) 	(int x 1 mBar) 
		public ushort	DispLowPr2;	//  1 x (word)	=    2 bytes  (96-97) 	(int x 1 mBar) 
		public ushort 	DispLowPr3;	//  1 x (word)	=    2 bytes  (98-99) 	(int x 1 mBar) 
		public ushort 	DispLowPr4;	//  1 x (word)	=    2 bytes  (100-101) 	(int x 1 mBar) 
		// Dispenseringstrykk for de forskjellige væskene (30 – 550mBar).
			
		public byte[] 	unused_bytes = new byte[256-(102+150)];	// 4 x (char)	=   4 bytes (102-105) 
		//public byte[] 	unused_bytes = new byte[256-(94+150)];	// 12 x (char)	=   12 bytes (94-105) 

		public byte[] 	Command = new byte[50];	// 50 x (byte)	= 50 bytes (106-155) 
		public ushort[] CmdValue = new ushort[50];	// 50 x (int)	= 100 bytes (156-255) 
	}; //  Total	= 256 bytes!

	//-------------------------------------------------------------------------------------------------------
	// Command og CmdValue:
	//-------------------------------------------------------------------------------------------------------
	public class CommandWrap
	{
		public byte[] Command = new byte[50];	// Command, (1 byte) (0-255)
		// ASP1=1 (LowSpeed), ASP2=2 (MediumSpeed), ASP3=3 (HighSpeed))
		// DISP1=20, DISP2=21, DISP3=22, DISP4=23 (Liquid 1-4)
		// SOAK=30 (pause)
		// REP1=40, REP2=41, …...REP10=49 (1-10 repetitions)
		// ROW0=60 (Nye rader, 1-15)
		// ROW1=61 (Nye rader, 16-32) (kun 1536)
		// End=0	

		ushort[] CmdValue = new ushort[50];	// Command parameter, 2 bytes (0 - +/-32000).
		// ASP: 	MSB = 	byte AspTime (in 1/10 seconds) (0.1 – 25.5 sec)
		// 	LSB = 	byte AspHeight (in 1/10 mm) (0.0 – 25.5 mm)
		//		= avstand til bunn av brønn (0.0 – Plate_Depth)
		// DISP: 	int DispVolume (in 1/10 ul) (min/max depends on PlateType)
		// SOAK: 	int SoakTime (time in seconds) (1 – 32000 sec)
		// REP: 	int FromCmdNr (first Cmd = 1, second Cmd = 2, …)
		// ROW:	16 bit flags (hvilke rader som er i bruk, fra nå)

	}
}
