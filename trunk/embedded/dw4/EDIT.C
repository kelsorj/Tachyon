#include "globdata.h"

#include "keybrd.h"
#include "display.h"


//***********************************************************************
//* FILE        : edit.c
//*
//* DESCRIPTION : Local edit of parameters (UI).
//*
//***********************************************************************

//---------------------------------------------------------
#define SHORT_BEEP  	 50		// Beep 100mS
#define LONG_BEEP  	200		// Beep 300mS
//---------------------------------------------------------

void AquaMaxInfo(void);
void EditIdleTime(void);

void ProgramEdit(void);
void SelectPrgmParameter(void);
int  SelectAspParameter(byte EdCode);
void EditLiqFactor(word *OldValue,word MinVal,word MaxVal);
void EditPressure(word *OldValue);
void EditSoakTime(int *OldValue);
void EditAspHeight(word *OldValue,word MinVal,word MaxVal);
void EditWellPos(word *OldValue,word MinVal,word MaxVal);
void EditAspOffset(int *OldValue);
void EditCommand(byte EdCode);
void EditCmdValue(byte EdCode);
void EditRows(void);
byte GetRowCnt(void);

//---------------------------------------------------------
// Global data:
//---------------------------------------------------------

struct ProgramBlock Prg;	// Working Copy in RAM.

byte CurrentCmd;	// Selected Prgm Command.
int  CurrentVal;	// Selected Command-Value.
int  AspTime;		// Command-Value MSB.
word AspHeight;		// Command-Value LSB.
byte MaxRows;		// Number of rows in Plate.


//---------------------------------------------------------
// external Global data:
//---------------------------------------------------------
extern byte Head1;	// Type of Head mounted! (check_heads())
extern byte Head2;	// Type of Head mounted!
extern byte Head3;	// Type of Head mounted!
extern byte Head4;	// Type of Head mounted!
extern byte Head5;	// Type of Head mounted!
extern byte Disp1;	// Commands in use in wanted program! (check_programs)
extern byte Disp2;	// Commands in use in wanted program!
extern byte Disp3;	// Commands in use in wanted program!
extern byte Disp4;	// Commands in use in wanted program!
extern byte DispLow1;	// Commands in use in wanted program! (check_programs)
extern byte DispLow2;	// Commands in use in wanted program!
extern byte DispLow3;	// Commands in use in wanted program!
extern byte DispLow4;	// Commands in use in wanted program!
extern byte Asp;	// Commands in use in wanted program!

//---------------------------------------------------------
// external functions:
//---------------------------------------------------------
extern void lcd_puts (byte adr, const char *str);	// (display.c)
extern void lcd_putc (byte adr, byte data);		// (display.c)
extern void lcd_clrl (byte adr);
extern byte get_key(void);
extern void put_key(byte NewKey);	// Puts a key into the keyboard-buffer
extern void delay_ms (word time);
extern void InterpretCommand();		// Check serial commands
extern void check_heads(void);
extern void regulate_pressure (int RegPressure);
//---------------------------------------------------------


//******************************************************************************
// INFO():
//******************************************************************************
void AquaMaxInfo(void)
{
char HeadType[5][8];	// List of HeadType-TxtLines.
char LcdTxt[5][32];	// List of TxtLines.
int  mB[5];		// Current pressure (mBar-value).
byte Key, t;

   Timer_1sec = 20;	// Start timer!
   Pres_Pump_off;		// Stop Pressure-Pump!
   Pres_Valve_off;	  	// Close drain valve!

   mB[0] = 0;
   mB[1] = 0;
   mB[2] = 0;
   mB[3] = 0;
   mB[4] = 0;

   sprintf (HeadType[0], " -- ");
   sprintf (HeadType[1], " 96 ");
   sprintf (HeadType[2], " 384");
   sprintf (HeadType[3], "1536");


//------------------------------------------------------------------------------
INFO_SCREEN_1:
//------------------------------------------------------------------------------

 //----------------------------------------------------
   sprintf (LcdTxt[1], "-AQUAMAX DW4 INFO:--");
   sprintf (LcdTxt[2], "Serialno.:%s          ",  Param[0].SerialNoTxt);
   sprintf (LcdTxt[3], "Firmware : V%X.%X      ",  VerCode/0x10, VerCode%0x10);
   sprintf (LcdTxt[4], "Idletime :%2d minutes  ",  Param[0].MaxIdleTime/60);
 //----------------------------------------------------
   LcdTxt[1][20] = 0;	// max 20 chars
   LcdTxt[2][20] = 0;	// max 20 chars
   LcdTxt[3][20] = 0;	// max 20 chars
   LcdTxt[4][20] = 0;	// max 20 chars
 //----------------------------------------------------
   lcd_puts (LCD1, LcdTxt[1]);
   lcd_puts (LCD2, LcdTxt[2]);
   lcd_puts (LCD3, LcdTxt[3]);
   lcd_puts (LCD4, LcdTxt[4]);
 //----------------------------------------------------

   Timer_1sec = 10;	// Start timer!
   while (Timer_1sec)
   {
      do { idle();}
      while (!Start_Flag);	// Stay in IDLE mode, wait for startflag (1mS).
      Start_Flag = 0;		// Ok, clear startflag (every 1mS).
    //------------------------------------------------
      regulate_pressure (RegPressure);	// Read pressure, set AlarmFlag, run Pump.
      InterpretCommand();  		// Check for seial commands
      if (MainState == 20)		// Check Flash Idle State
	  return;
    //------------------------------------------------

      Key = get_key();
      if (Key)
	  Timer_1sec = 10;	// Reload timer!

      switch (Key)
      {
	 case UP_KEY    :  Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			   break;

	 case DOWN_KEY  :  Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			   goto INFO_SCREEN_2;
			   break;

	 case ENTER_KEY :  EditIdleTime();
			   goto INFO_SCREEN_1;
			   break;
	 case START_KEY :  break;
	 case STOP_KEY  :  return;
	 case CANCEL_KEY:  return;
      }
   }
   Beep_Cnt = LONG_BEEP;	// End: BEEP.
   return;


//------------------------------------------------------------------------------
INFO_SCREEN_2:
//------------------------------------------------------------------------------

   t = 248;

   while (1)
   {
      do { idle();}
      while (!Start_Flag);	// Stay in IDLE mode, wait for startflag.
      Start_Flag = 0;		// Ok, clear startflag (every 1mS).

    //------------------------------------------------
      regulate_pressure (RegPressure);	// Read pressure, set AlarmFlag, run Pump.
      InterpretCommand();  		// Check for seial commands
      if (MainState == 20)		// Check Flash Idle State
	  return;
    //------------------------------------------------

      Key = get_key();
      if (Key)
      switch (Key)
      {
	 case UP_KEY    :  Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			   goto INFO_SCREEN_1;
			   break;

	 case DOWN_KEY  :  Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			   goto INFO_SCREEN_3;
			   break;

	 case ENTER_KEY :  break;
	 case START_KEY :  break;
	 case STOP_KEY  :  return;
	 case CANCEL_KEY:  return;
      }
    //------------------------------------------------
      if (++t == 250)
      {
	   t = 0;
	//-------------------------------------
	   check_heads();			// Find type of heads mounted!
	//-------------------------------------
	   sprintf (LcdTxt[1], "-DISP/ASP HEADS:----");
	   sprintf (LcdTxt[2], "DH1: %4s           ", HeadType[Head1]);
	   sprintf (LcdTxt[3], "DH2: %4s DH4: %4s  ", HeadType[Head2], HeadType[Head4]);
	   sprintf (LcdTxt[4], "DH3: %4s Asp: %4s  ", HeadType[Head3], HeadType[Head5]);
	//-------------------------------------
	   LcdTxt[1][20] = 0;	// max 20 chars
	   LcdTxt[2][20] = 0;	// max 20 chars
	   LcdTxt[3][20] = 0;	// max 20 chars
	   LcdTxt[4][20] = 0;	// max 20 chars
	//-------------------------------------
	   lcd_puts (LCD1, LcdTxt[1]);
	   lcd_puts (LCD2, LcdTxt[2]);
	   lcd_puts (LCD3, LcdTxt[3]);
	   lcd_puts (LCD4, LcdTxt[4]);
	//-------------------------------------
      }
   }
   Beep_Cnt = LONG_BEEP;	// End: BEEP.
   return;


//------------------------------------------------------------------------------
INFO_SCREEN_3:
//------------------------------------------------------------------------------

   t = 248;

   while (1)
   {
      do { idle();}
      while (!Start_Flag);	// Stay in IDLE mode, wait for startflag.
      Start_Flag = 0;		// Ok, clear startflag (every 1mS).

    //------------------------------------------------
      regulate_pressure (RegPressure);	// Read pressure, set AlarmFlag, run Pump.
      InterpretCommand();  		// Check for seial commands
      if (MainState == 20)		// Check Flash Idle State
	  return;
    //------------------------------------------------

      Key = get_key();
      if (Key)
      switch (Key)
      {
	 case UP_KEY    :  Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			   goto INFO_SCREEN_2;
			   break;

	 case DOWN_KEY  :  Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			   break;

	 case ENTER_KEY :  break;
	 case START_KEY :  break;
	 case STOP_KEY  :  return;
	 case CANCEL_KEY:  return;
      }

    //------------------------------------------------
      if (++t == 250)
      {
	   t = 0;
	//-------------------------------------
	   Select_ADC6;		// Multiplexer input-NEG.
	   delay_4uS		// +4uS
	   Select_ADC0;		// Multiplexer input.
	   delay_ms(2);
	   mB[0]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
	   mB[0] = mB[0] + 1 - Param[0].PresOffset[0];
	   mB[0] = (long)((long)mB[0] * (long)1000L) / (long)Param[0].PresCal[0];
	//-------------------------------------
	   Select_ADC6;		// Multiplexer input-NEG.
	   delay_4uS		// +4uS
	   Select_ADC1;		// Multiplexer input.
	   delay_ms(2);
	   mB[1]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
	   mB[1] = mB[1] + 1 - Param[0].PresOffset[1];
	   mB[1] = (long)((long)mB[1] * (long)1000L) / (long)Param[0].PresCal[1];
	//-------------------------------------
	   Select_ADC6;		// Multiplexer input-NEG.
	   delay_4uS		// +4uS
	   Select_ADC2;		// Multiplexer input.
	   delay_ms(2);
	   mB[2]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
	   mB[2] = mB[2] + 1 - Param[0].PresOffset[2];
	   mB[2] = (long)((long)mB[2] * (long)1000L) / (long)Param[0].PresCal[2];
	//-------------------------------------
	   Select_ADC6;		// Multiplexer input-NEG.
	   delay_4uS		// +4uS
	   Select_ADC3;		// Multiplexer input.
	   delay_ms(2);
	   mB[3]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
	   mB[3] = mB[3] + 1 - Param[0].PresOffset[3];
	   mB[3] = (long)((long)mB[3] * (long)1000L) / (long)Param[0].PresCal[3];
	//-------------------------------------
	   Select_ADC6;		// Multiplexer input-NEG.
	   delay_4uS		// +4uS
	   Select_ADC4;		// Multiplexer input.
	   delay_ms(2);
	   mB[4]= READ_ADC ();	// Read Internal pressure (0,2mS rutine)
	   mB[4] = mB[4] + 1 - Param[0].PresOffset[4];
	   mB[4] = (long)((long)mB[4] * (long)1000L) / (long)Param[0].PresCal[4];
	//-------------------------------------
	   sprintf  (LcdTxt[1], "-PRESSURE(mBar):---");
	   sprintf  (LcdTxt[2], "Int.: %3d            ", mB[0]);
	   sprintf  (LcdTxt[3], "Liq1: %3d Liq3: %3d ", mB[1], mB[3]);
	   sprintf  (LcdTxt[4], "Liq2: %3d Liq4: %3d ", mB[2], mB[4]);
	//-------------------------------------
	   LcdTxt[1][20] = 0;	// max 20 chars
	   LcdTxt[2][20] = 0;	// max 20 chars
	   LcdTxt[3][20] = 0;	// max 20 chars
	   LcdTxt[4][20] = 0;	// max 20 chars
	//-------------------------------------
	   lcd_puts (LCD1, LcdTxt[1]);
	   lcd_puts (LCD2, LcdTxt[2]);
	   lcd_puts (LCD3, LcdTxt[3]);
	   lcd_puts (LCD4, LcdTxt[4]);
	//-------------------------------------

      }
   }
   Beep_Cnt = LONG_BEEP;	// End: BEEP.
   return;

}

//******************************************************************************
//
// EditIdleTime ()
//
// - Set IdleTime from 1 to 60 minutes, and store in EEPROM.
//
//******************************************************************************
void EditIdleTime(void)
{
word OldValue;
word Value;
char LcdTxt[21];
byte key;

   Value = Param[0].MaxIdleTime/60;
   OldValue = Value;

 //-----------------------------------------------------------------------------
   lcd_puts (LCD1, "Idle Pressure Time: ");
   lcd_clrl (LCD2);
   lcd_clrl (LCD3);
   lcd_clrl (LCD4);
 //-----------------------------------------------------------------------------

   sprintf(LcdTxt, "    %2d minutes ", Value);
   lcd_puts (LCD3, LcdTxt);

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return;
   //------------------------------------------------
     key = get_key();
     if (key)
     {
	switch (key)
	{
	 case UP_KEY    : if(Value < 60) Value++;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case DOWN_KEY  : if(Value > 1) Value--;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case ENTER_KEY : if (Value != OldValue)
			  {
			     Param[0].MaxIdleTime = Value * 60;	// Time in seconds
			     write_eeprom_param ();
			  }
			  return;
	 case CANCEL_KEY: return;
	}
	sprintf(LcdTxt, "    %2d minutes ", Value);
	lcd_puts (LCD3, LcdTxt);
     }
   }
}



//******************************************************************************
//
// 1:  ProgramEdit
//
//******************************************************************************
void ProgramEdit(void)
{
    byte *ptr1;		// Pointer to RamProg
far byte *ptr2;		// Pointer to FlashProg

byte NewLine, OldLine;
byte Changed = 0;
char LcdTxt[24];
int t;


   Pres_Pump_off;	// Stop Pressure-Pump!
   Pres_Valve_off;	// Close drain valve!

 //----------------------------------------------------
 // Only Normal Programs here (not Special programs):
 //----------------------------------------------------
   if (Wanted_Prgm > MAX_SUBPROGRAMS)
   {
     Beep_Cnt = LONG_BEEP;		// A long BEEP.
     return;
   }

 //----------------------------------------------------
 // Make a Copy of this ProgramBlock from Flash to RAM:
 //----------------------------------------------------
   ptr1 = (byte *) &Prg.ProgNo;
   ptr2 = (far byte*) &Program[Wanted_Prgm-1].ProgNo;

   for (t=0; t<256; t++)
       *(ptr1++) = *(ptr2++);

 //----------------------------------------------------
 // Check for valid program:
 //----------------------------------------------------
   if (Prg.Command[0] == 0)	// Any valid Command?
   {
     Beep_Cnt = LONG_BEEP;	// No: A long BEEP.
     return;
   }


 //---------------------------------------------------
 //--- Init global data:
 //---------------------------------------------------

   CurrentCmd = 0;
   CurrentVal = 0;
   AspTime   = 0;	// Asp-Time
   AspHeight = 0;	// Asp-Height

   if (Prg.RowColumn == 0)	// Rows:
   {
      if      (Prg.PlateType == 3 || Prg.PlateType == 13) MaxRows = 16;	// (1536) 32 rows (16 steps max! 2.25mm x 2)
      else if (Prg.PlateType == 2 || Prg.PlateType == 12) MaxRows =  8;	// ( 384) 16 rows (8 steps max! 4.50mm x 2)
      else if (Prg.PlateType == 1 || Prg.PlateType == 11) MaxRows =  8;	// (  96)  8 rows (8 steps max! 9.00mm x 1)
   }
   else		// Columns:
   {
      if      (Prg.PlateType == 3 || Prg.PlateType == 13) MaxRows = 24;	// (1536) 48 cols (24 steps max! 2.25mm x 2)
      else if (Prg.PlateType == 2 || Prg.PlateType == 12) MaxRows = 12;	// ( 384) 24 cols (12 steps max! 4.50mm x 2)
      else if (Prg.PlateType == 1 || Prg.PlateType == 11) MaxRows = 12;	// (  96) 12 cols (12 steps max! 9.00mm x 1)
   }


 //-------------------------------------------------
 // Call rutine for selecting parameter to edit:
 //-------------------------------------------------

LCD_UPPDATE:
   SelectPrgmParameter();	// Edit Program Parameters!

 //-------------------------------------------------


 //-------------------------------------------------
 // Check for changes (compare Flash and RAM):
 //-------------------------------------------------
   ptr1 = (byte *) &Prg.ProgNo;
   ptr2 = (far byte*) &Program[Wanted_Prgm-1].ProgNo;
   Changed = 0;

   for (t=0; t<256; t++)
   {
      if(*(ptr1++) != *(ptr2++))
	 Changed = 1;
   }

 //-------------------------------------------------
 // If no changes: EXIT!
 //-------------------------------------------------

   if (Changed == 0) return;	// No changes! EXIT!


 //-------------------------------------------------
 // Program has been changed: Ask for saving:
 //-------------------------------------------------

   Prg.LocalEdit = 1;	// Flag = changed!
   sprintf(LcdTxt, "Save changes to P%02d?", Wanted_Prgm);

   lcd_puts (LCD1, LcdTxt);			// Program Name
   lcd_puts (LCD2, "     -> Yes <-      ");
   lcd_puts (LCD3, "        No          ");
   lcd_clrl (LCD4);

   NewLine = 2;
   OldLine = 1;

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return;
   //------------------------------------------------
     if (OldLine != NewLine)
     {
       OldLine = NewLine;
       if (NewLine == 1)
       {
	  lcd_puts (LCD2, "     -> Yes <-");
	  lcd_puts (LCD3, "        No    ");
       }
       else
       {
	  lcd_puts (LCD2, "        Yes   ");
	  lcd_puts (LCD3, "     -> No  <-");
       }
     }

     switch (get_key())
     {
      case UP_KEY   : 	if(NewLine > 1) NewLine--;	// Select line.
			else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			break;
      case DOWN_KEY : 	if(NewLine < 2) NewLine++;	// Select line.
			else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			break;

      case ENTER_KEY : 	if (NewLine == 1)	// Yes = Save Program in Flash!
			    Flash_Reprogram ((far const byte *)&Program[Wanted_Prgm-1].ProgNo, (byte *)&Prg.ProgNo, 256);
			return;

      case CANCEL_KEY: 	Beep_Cnt = LONG_BEEP;		// A long BEEP.
			goto LCD_UPPDATE;	// No = continue Edit.
     }
   }

}




//******************************************************************************
//
// 1.1:  SelectProgamParameter (Parameters and Commands):
//
//       Scroll trough the list of current parameters and commands,
//       to select what to edit:
//
//******************************************************************************
void SelectPrgmParameter(void)
{
byte TopLine = 1;	// TxtLine shown in upper LCD.
byte CurLine = 1;	// Selected LCD-line (1-3).
byte Select = 1;	// Selected TxtLine.
byte EdCode;		// Selected TxtLine Code.
byte MaxLines;

byte t, i;
char Header[32];	// HeaderLine.
char LcdTxt[70][24];	// List of TxtLines.
char Txt[64];		// TxtLine.
byte RowCnt;		// Number of Rows in use.

byte Key;
int  status;
int  Tmp;


//------------------------------------------------------------------------------
// Program Parameters:
//------------------------------------------------------------------------------
//   Program[No].LocalEdit;	// Flag!
//   Program[No].PlateType;	// --
//   Program[No].PlateHeight;	// --
//   Program[No].PlateDepth;	// --
//   Program[No].PlateOffset;	// Edit (Linked?)
//   Program[No].PlateVolume;	// --
//   Program[No].PlateDbwc;	// --
//   Program[No].PlateRows0;	// Edit <-
//   Program[No].PlateRows1;	// Edit <-
//   Program[No].Liq1Factor;	// Edit (Linked?)
//   Program[No].Liq2Factor;	// Edit (Linked?)
//   Program[No].Liq3Factor;	// Edit (Linked?)
//   Program[No].Liq4Factor;	// Edit (Linked?)
//------------------------------------------------------------------------------

   sprintf(Header, "Edit Program P%02d:   ", Wanted_Prgm);

LCD_UPPDATE1:

   RowCnt = GetRowCnt();
   i = 0;
//------------------------------------------------------------------------------
// Program Parameters:
//------------------------------------------------------------------------------
   if (Disp1 || Disp2 || Disp3 || Disp4 || DispLow1 || DispLow2 || DispLow3 || DispLow4 || Asp)
   {
     if (Prg.RowColumn == 0)	// Rows:
	sprintf (LcdTxt[i], "  RowSelect: %2d rows ", RowCnt);
     else
	sprintf (LcdTxt[i], "  ColSelect: %2d cols ", RowCnt);
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xA1;

     sprintf (LcdTxt[i], "  WellPos. : %2d.%02dmm ", Prg.PlateOffset/100, Prg.PlateOffset%100);
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xA2;
   }
   //----------------------
   if (Asp)
   {
     if(Prg.AspOffset < 0)
     {
	Tmp = Prg.AspOffset * -1;
	sprintf (LcdTxt[i], "  AspOffset: -%d.%dmm  ", Tmp/10, Tmp%10);
     }
     else
	sprintf (LcdTxt[i], "  AspOffset:  %d.%dmm  ", Prg.AspOffset/10, Prg.AspOffset%10);
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xA3;
   }
   //----------------------
   if (Disp1 || DispLow1)
   {
     sprintf (LcdTxt[i], "  Liq1Fact.:  %d.%02d  ",  Prg.Liq1Factor/100,  Prg.Liq1Factor%100);
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xB1;
   }
   if (Disp2 || DispLow2)
   {
     sprintf (LcdTxt[i], "  Liq2Fact.:  %d.%02d  ",  Prg.Liq2Factor/100,  Prg.Liq2Factor%100);
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xB2;
   }
   if (Disp3 || DispLow3)
   {
     sprintf (LcdTxt[i], "  Liq3Fact.:  %d.%02d  ",  Prg.Liq3Factor/100,  Prg.Liq3Factor%100);
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xB3;
   }
   if (Disp4 || DispLow4)
   {
     sprintf (LcdTxt[i], "  Liq4Fact.:  %d.%02d  ",  Prg.Liq4Factor/100,  Prg.Liq4Factor%100);
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xB4;
   }
   //----------------------
   if (DispLow1)
   {
     sprintf (LcdTxt[i], "  LowPress1:  %3dmBar   ",  Prg.DispLowPr1);
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xB5;
   }
   if (DispLow2)
   {
     sprintf (LcdTxt[i], "  LowPress2:  %3dmBar   ",  Prg.DispLowPr2);
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xB6;
   }
   if (DispLow3)
   {
     sprintf (LcdTxt[i], "  LowPress3:  %3dmBar   ",  Prg.DispLowPr3);
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xB7;
   }
   if (DispLow4)
   {
     sprintf (LcdTxt[i], "  LowPress4:  %3dmBar   ",  Prg.DispLowPr4);
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xB8;
   }
   //----------------------


   if (i > 0)	// If any parameters:
   {
     sprintf (LcdTxt[i], "  ----------------- ");
     LcdTxt[i][20] = 0;
     LcdTxt[i++][22] = 0xC0;
   }
//------------------------------------------------------------------------------


//------------------------------------------------------------------------------
// Program Commands:
//------------------------------------------------------------------------------

   for (t=0; t<50; t++)	// Check all commands
   {
      CurrentCmd = Prg.Command[t];
      CurrentVal = Prg.CmdValue[t];
      AspTime = Prg.CmdValue[t] >> 8;
      AspHeight = Prg.CmdValue[t] & 0x00ff;

      switch(CurrentCmd)
      {
	case DISP1:
	case DISP2:
	case DISP3:
	case DISP4:
		      if (CurrentVal >= 1000)	// > 100ul ?
			sprintf(Txt, "Disp%d  %dul       ", (CurrentCmd - DISP1 + 1), CurrentVal/10);
		      else				// < 99.9ul
			sprintf(Txt, "Disp%d  %d.%dul    ", (CurrentCmd - DISP1 + 1), CurrentVal/10, CurrentVal%10);
			break;

	case DISPL1:
	case DISPL2:
	case DISPL3:
	case DISPL4:
		      if (CurrentVal >= 1000)	// > 100ul ?
			sprintf(Txt, "DispL%d %dul       ", (CurrentCmd - DISPL1 + 1), CurrentVal/10);
		      else				// < 99.9ul
			sprintf(Txt, "DispL%d %d.%dul    ", (CurrentCmd - DISPL1 + 1), CurrentVal/10, CurrentVal%10);
			break;

	case ASP1:      sprintf(Txt, "AspLo   %d.%dmm   ", AspHeight/10, AspHeight%10);
			break;
	case ASP2:      sprintf(Txt, "AspMe   %d.%dmm   ", AspHeight/10, AspHeight%10);
			break;
	case ASP3:	sprintf(Txt, "AspHi   %d.%dmm   ", AspHeight/10, AspHeight%10);
			break;

	case ASP1sweep: sprintf(Txt, "AspSwLo %d.%dmm   ", AspHeight/10, AspHeight%10);
			break;
	case ASP2sweep: sprintf(Txt, "AspSwMe %d.%dmm   ", AspHeight/10, AspHeight%10);
			break;
	case ASP3sweep:	sprintf(Txt, "AspSwHi %d.%dmm   ", AspHeight/10, AspHeight%10);
			break;

	case ASP_OFS:	if(CurrentVal < 0)
			{
			   Tmp = CurrentVal * -1;
			   sprintf (Txt, "Offset=-%d.%dmm  ", Tmp/10, Tmp%10);
			}
			else
			   sprintf (Txt, "Offset= %d.%dmm  ", CurrentVal/10, CurrentVal%10);
			break;

	case SOAK:	sprintf(Txt, "Soak   %dsec        ", CurrentVal);
			break;

	case ROW0:	if (Prg.RowColumn == 0)	// Rows:
			    sprintf(Txt, "NewRows1:%04X   ", CurrentVal);
			else
			    sprintf(Txt, "NewCols1:%04X   ", CurrentVal);
			break;
	case ROW1:	if (Prg.RowColumn == 0)	// Rows:
			    sprintf(Txt, "NewRows2:%04X   ", CurrentVal);
			else
			    sprintf(Txt, "NewCols2:%04X   ", CurrentVal);
			break;

	case REP0 :
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
			sprintf(Txt, "Rep%dx - S%02d       ", (CurrentCmd - REP0), CurrentVal);
			break;

	case END:  	t = 50;	// Finnished.
			break;

      }	// End switch()

      //----------------------------------------------
      // Make the current TextLine:
      //----------------------------------------------
      Txt[13] = 0;		// Max 14 chars
      sprintf(LcdTxt[i], "  s%02d-%s", t+1, Txt);
      LcdTxt[i][20] = 0;	// NULL
      LcdTxt[i][22] = t;	// Command Number!
      //----------------------------------------------
      i++;
   }		// All Commands checked!

   sprintf(LcdTxt[i], "        END         ");
   LcdTxt[i][22] = 50;	// Command Number!



//------------------------------------------------------------------------------
   MaxLines = i;
//------------------------------------------------------------------------------

   lcd_puts (LCD1, Header);

LCD_UPPDATE2:
   lcd_puts (LCD2, LcdTxt[TopLine-1]);
   lcd_puts (LCD3, LcdTxt[TopLine+0]);
   lcd_puts (LCD4, LcdTxt[TopLine+1]);

LCD_UPPDATE3:
   if (CurLine == 1)
   {
	  lcd_puts (LCD2, "->");
	  lcd_puts (LCD3, "  ");
	  lcd_puts (LCD4, "  ");
   }
   else if (CurLine == 2)
   {
	  lcd_puts (LCD2, "  ");
	  lcd_puts (LCD3, "->");
	  lcd_puts (LCD4, "  ");
   }
   else if (CurLine == 3)
   {
	  lcd_puts (LCD2, "  ");
	  lcd_puts (LCD3, "  ");
	  lcd_puts (LCD4, "->");
   }

 //-------------------------------
   Select = TopLine + CurLine - 2;
   EdCode = LcdTxt[Select][22];

   if (EdCode == 0xC0)	// Dummy line:
       put_key(Key);	// Next Line!
 //-------------------------------

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return;
   //------------------------------------------------
     Key = get_key();
     switch (Key)
     {
      case UP_KEY   : 	if(CurLine > 1)
			{
			   CurLine--;
			   goto LCD_UPPDATE3;	// Uppdate LCD.
			}
			else if(TopLine > 1)
			{
			   TopLine--;
			   goto LCD_UPPDATE2;	// Uppdate LCD.
			}
			Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			break;

      case DOWN_KEY : 	if(CurLine < 3)
			{
			   CurLine++;
			   goto LCD_UPPDATE3;	// Uppdate LCD.
			}
			else if(TopLine < (MaxLines-3))
			{
			   TopLine++;
			   goto LCD_UPPDATE2;	// Uppdate LCD.
			}
			Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			break;

      case ENTER_KEY :  if      (EdCode == 0xA1) EditRows();	// Edit Rows!
			else if (EdCode == 0xA2) EditWellPos(&Prg.PlateOffset, 500, 1500);	// Edit Parameters!
			else if (EdCode == 0xA3) EditAspOffset(&Prg.AspOffset);	// Edit Parameters!

			else if (EdCode == 0xB1) EditLiqFactor(&Prg.Liq1Factor, 50, 250);	// Edit Parameters!
			else if (EdCode == 0xB2) EditLiqFactor(&Prg.Liq2Factor, 50, 250);	// Edit Parameters!
			else if (EdCode == 0xB3) EditLiqFactor(&Prg.Liq3Factor, 50, 250);	// Edit Parameters!
			else if (EdCode == 0xB4) EditLiqFactor(&Prg.Liq4Factor, 50, 250);	// Edit Parameters!

			else if (EdCode == 0xB5) EditPressure(&Prg.DispLowPr1);	// Edit Parameters!
			else if (EdCode == 0xB6) EditPressure(&Prg.DispLowPr2);	// Edit Parameters!
			else if (EdCode == 0xB7) EditPressure(&Prg.DispLowPr3);	// Edit Parameters!
			else if (EdCode == 0xB8) EditPressure(&Prg.DispLowPr4);	// Edit Parameters!

			else if (EdCode <= 49)
			{
			   CurrentCmd = Prg.Command[EdCode];
			   CurrentVal = Prg.CmdValue[EdCode];
			   AspTime   = Prg.CmdValue[EdCode] >> 8;		// Asp-Time (MSB)
			   AspHeight = Prg.CmdValue[EdCode] & 0x00ff;	// Asp-Height (LSB)

			   if (CurrentCmd >= ASP1 && CurrentCmd <= ASP3)
			   {
			      status = SelectAspParameter (EdCode);
			   }
			   else if (CurrentCmd >= ASP1sweep && CurrentCmd <= ASP3sweep)
			   {
			      status = SelectAspParameter (EdCode);
			   }
			   else if (CurrentCmd == ASP_OFS)
			   {
			      EditAspOffset(&Prg.CmdValue[EdCode]);	// Edit Parameters!
			   }
			   else if (CurrentCmd >= DISP1 && CurrentCmd <= DISP4)
			   {
			      EditCmdValue(EdCode);	// Edit Commands!
			   }
			   else if (CurrentCmd >= DISPL1 && CurrentCmd <= DISPL4)
			   {
			      EditCmdValue(EdCode);	// Edit Commands!
			   }
			   else if (CurrentCmd >= REP0 && CurrentCmd <= REP10)
			   {
			      EditCommand(EdCode);	// Edit Commands!
			   }
			   else if (CurrentCmd == SOAK)
			   {
			      EditSoakTime(&Prg.CmdValue[EdCode]);
			   }
			   else Beep_Cnt = LONG_BEEP;		// A long BEEP.

			}
//			else Beep_Cnt = LONG_BEEP;		// A long BEEP.

			goto LCD_UPPDATE1;	// Done! Uppdate LCD.

      case CANCEL_KEY: 	return;
     }
   }


}



//******************************************************************************
// 1.1:  SelectAspParameter
//
//       Scroll trough the list of ASP-parameters,
//       to select what to edit:
//
//******************************************************************************
int  SelectAspParameter(byte EdCode)
{
byte NewLine = 1;
byte OldLine = 2;

char LcdTxt1[24];
char LcdTxt2[24];


   if (CurrentCmd < ASP1 || CurrentCmd > ASP3sweep)	// Only ASP-commands!
      return (0);

 //-----------------------------------------------------------------------------
   lcd_puts (LCD1, "Select:             ");
   lcd_puts (LCD2, "->Asp-Speed : Medium");
   lcd_puts (LCD3, "  Asp-Height: 10.0mm");
   lcd_puts (LCD4, "  Asp-Time  : 10.0s ");
 //-----------------------------------------------------------------------------

LCD_UPPDATE1:
   if      (CurrentCmd == ASP1) lcd_puts (LCD2, "  Asp-Speed : Low   ");
   else if (CurrentCmd == ASP2) lcd_puts (LCD2, "  Asp-Speed : Medium");
   else if (CurrentCmd == ASP3) lcd_puts (LCD2, "  Asp-Speed : High  ");

   sprintf(LcdTxt1, "  Asp-Height: %2d.%dmm", AspHeight/10, AspHeight%10);
   sprintf(LcdTxt2, "  Asp-Time  : %2d.%ds ", AspTime/10, AspTime%10);

   lcd_puts (LCD3, LcdTxt1);
   lcd_puts (LCD4, LcdTxt2);

   OldLine = 0;

 //-----------------------------------------------------------------------------

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return(0);
   //------------------------------------------------
     if (OldLine != NewLine)
     {
       OldLine = NewLine;
       if (NewLine == 1)
       {
	  lcd_puts (LCD2, "->");
	  lcd_puts (LCD3, "  ");
	  lcd_puts (LCD4, "  ");
       }
       else if (NewLine == 2)
       {
	  lcd_puts (LCD2, "  ");
	  lcd_puts (LCD3, "->");
	  lcd_puts (LCD4, "  ");
       }
       else
       {
	  lcd_puts (LCD2, "  ");
	  lcd_puts (LCD3, "  ");
	  lcd_puts (LCD4, "->");
       }
     }

     switch (get_key())
     {
      case UP_KEY   : 	if(NewLine > 1) NewLine--;	// Select line.
			else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			break;
      case DOWN_KEY : 	if(NewLine < 3) NewLine++;	// Select line.
			else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			break;

      case ENTER_KEY :  if (NewLine == 1)
			{
			    EditCommand(EdCode);	// Edit Asp-Command!
			    CurrentCmd = Prg.Command[EdCode];
			}
			else if (NewLine == 2)	// Asp-Height
			{
			    EditAspHeight(&AspHeight, 0, Prg.PlateDepth/10);	// Edit Asp-Height!
			    Prg.CmdValue[EdCode] &= 0xff00;
			    Prg.CmdValue[EdCode] |= AspHeight;
			}
			else if (NewLine == 3)	// Asp-Time
			{
			    EditCmdValue(EdCode);	// Edit Asp-Time!
			}

			goto LCD_UPPDATE1;	// Done! Uppdate LCD.
//			return (NewLine);

      case CANCEL_KEY: 	return (0);
     }
   }

}


//******************************************************************************
//
// EditAspHeight():
//
//******************************************************************************
void EditAspHeight(word *OldValue, word MinVal, word MaxVal)
{
word Value;
char LcdTxt[21];
byte key;

   Value = *OldValue;

 //-----------------------------------------------------------------------------
   lcd_puts (LCD1, "Asp.Probe Height:   ");
   lcd_clrl (LCD2);
   lcd_clrl (LCD3);
   lcd_clrl (LCD4);
 //-----------------------------------------------------------------------------

   sprintf(LcdTxt, "      %2d.%dmm  ", Value/10, Value%10);
   lcd_puts (LCD3, LcdTxt);

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return;
   //------------------------------------------------
     key = get_key();
     if (key)
     {
	switch (key)
	{
	 case UP_KEY    : if(Value < MaxVal) Value++;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case DOWN_KEY  : if(Value > MinVal) Value--;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case ENTER_KEY : *OldValue = Value; return;
	 case CANCEL_KEY: return;
	}
	sprintf(LcdTxt, "      %2d.%dmm  ", Value/10, Value%10);
	lcd_puts (LCD3, LcdTxt);
     }
   }
}



//******************************************************************************
//
// EditWellPos()
//
//******************************************************************************
void EditWellPos(word *OldValue, word MinVal, word MaxVal)
{
word Value;
char LcdTxt[21];
byte key;

   Value = *OldValue;

 //-----------------------------------------------------------------------------
   lcd_puts (LCD1, "Edge-Well dist.:    ");
   lcd_clrl (LCD2);
   lcd_clrl (LCD3);
   lcd_clrl (LCD4);
 //-----------------------------------------------------------------------------

   sprintf(LcdTxt, "      %2d.%02dmm  ", Value/100, Value%100);
   lcd_puts (LCD3, LcdTxt);

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return;
   //------------------------------------------------
     key = get_key();
     if (key)
     {
	switch (key)
	{
	 case UP_KEY    : if(Value < MaxVal) Value++;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case DOWN_KEY  : if(Value > MinVal) Value--;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case ENTER_KEY : *OldValue = Value; return;
	 case CANCEL_KEY: return;
	}
	sprintf(LcdTxt, "      %2d.%02dmm  ", Value/100, Value%100);
	lcd_puts (LCD3, LcdTxt);
     }
   }
}

//******************************************************************************
//
// EditAspOffset()
//
//------------------------------------------------------------------------------
// Asp-position, offset from center of well! (in 1/10 mm)
// Default = 0.0mm, max = (1536=0.4)(384=1.3)(96=3.0)
//******************************************************************************
void EditAspOffset(int *OldValue)
{
int Value, MinVal, MaxVal, Tmp;
char LcdTxt[21];
byte key;

   Value = *OldValue;

   if (Prg.PlateType == 3 || Prg.PlateType == 13)	//1536:
   {
//      MaxVal = 4;	// 0.4mm
//      MinVal = -4;	// 0.4mm
      MaxVal = 8; 	// 2.4 / 2 - 0.4 = 0.8 (nytt i V2.2)
      MinVal = -8;	// 2.4 / 2 - 0.4 = 0.8 mm
   }
   else if (Prg.PlateType == 2 || Prg.PlateType == 12)	//384:
   {
//      MaxVal = 13; 	// 1.3mm
//      MinVal = -13;	// 1.3mm
      MaxVal = 19; 	// 4.6 / 2 - 0.4 = 1.9 mm
      MinVal = -19;	// 4.6 / 2 - 0.4 = 1.9 mm
   }
   else		//96:
   {
//      MaxVal = 30; 	// 3.0mm
//      MinVal = -30;	// 3.0mm
      MaxVal = 42; 	// 9.2 / 2 - 0.4 = 4.2 mm
      MinVal = -42;	// 9.2 / 2 - 0.4 = 4.2 mm
   }

 //-----------------------------------------------------------------------------
   lcd_puts (LCD1, "Asp.Hor.Offset:     ");
   lcd_clrl (LCD2);
   lcd_clrl (LCD3);
   lcd_clrl (LCD4);
 //-----------------------------------------------------------------------------
   if (Value < 0)
   {
      Tmp = Value * -1;
      sprintf (LcdTxt, "      -%d.%dmm  ", Tmp/10, Tmp%10);
   }
   else
      sprintf (LcdTxt, "       %d.%dmm  ", Value/10, Value%10);
   lcd_puts (LCD3, LcdTxt);

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return;
   //------------------------------------------------
     key = get_key();
     if (key)
     {
	switch (key)
	{
	 case UP_KEY    : if(Value < MaxVal) Value++;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case DOWN_KEY  : if(Value > MinVal) Value--;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case ENTER_KEY : *OldValue = Value;
			  return;
	 case CANCEL_KEY: return;
	}
	if (Value < 0)
	{
	   Tmp = Value * -1;
	   sprintf (LcdTxt, "      -%d.%dmm  ", Tmp/10, Tmp%10);
	}
	else
	   sprintf (LcdTxt, "       %d.%dmm  ", Value/10, Value%10);
	lcd_puts (LCD3, LcdTxt);
     }
   }
}


//******************************************************************************
//
// Edit LiqudFactor: (50-250 procent)
//
//******************************************************************************
void EditLiqFactor(word *OldValue, word MinVal, word MaxVal)
{
word Value;
char LcdTxt[21];
byte key;

   Value = *OldValue;

 //-----------------------------------------------------------------------------
   lcd_puts (LCD1, "Edit Liquid Factor: ");
   lcd_clrl (LCD2);
   lcd_clrl (LCD3);
   lcd_clrl (LCD4);
 //-----------------------------------------------------------------------------

   sprintf(LcdTxt, "        %d.%02d  ", Value/100, Value%100);
   lcd_puts (LCD3, LcdTxt);

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return;
   //------------------------------------------------
     key = get_key();
     if (key)
     {
	switch (key)
	{
	 case UP_KEY    : if(Value < MaxVal) Value++;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case DOWN_KEY  : if(Value > MinVal) Value--;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case ENTER_KEY : *OldValue = Value; return;
	 case CANCEL_KEY: return;
	}
	sprintf(LcdTxt, "        %d.%02d ", Value/100, Value%100);
	lcd_puts (LCD3, LcdTxt);
     }
   }

}


//******************************************************************************
//
// 1.1.x: Edit Pressure: (600 mBar)
//
//******************************************************************************
void EditPressure(word *OldValue)
{
word Value;
char LcdTxt[21];
byte key;

 //-----------------------------------------------------------------------------
   #define PMinVal  30		// Minimum  30mBar
   #define PMaxVal 550		// Maximum 550mBar
   #define PStep    10		// Step = 10
 //-----------------------------------------------------------------------------
   lcd_puts (LCD1, "Edit Pressure:      ");
   lcd_clrl (LCD2);
   lcd_clrl (LCD3);
   lcd_clrl (LCD4);
 //-----------------------------------------------------------------------------

   Value = *OldValue;

   sprintf(LcdTxt, "      %3d mBar  ", Value);
   lcd_puts (LCD3, LcdTxt);

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
   //------------------------------------------------
     key = get_key();
     if (key)
     {
	switch (key)
	{
	 case UP_KEY    : if(Value <= (PMaxVal-PStep)) Value += PStep;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  Value = Value / 10;
			  Value = Value * 10;
			  break;

	 case DOWN_KEY  : if(Value >= (PMinVal+PStep)) Value -= PStep;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  Value = Value / 10;
			  Value = Value * 10;
			  break;

	 case ENTER_KEY : *OldValue = Value; return;
	 case CANCEL_KEY: return;
	}
	sprintf(LcdTxt, "      %3d mBar  ", Value);
	lcd_puts (LCD3, LcdTxt);
     }
   }

}


//******************************************************************************
//
// 1.1.x: Edit Soak Time: (00:00:00)
//
//******************************************************************************
void EditSoakTime(int *OldValue)
{
int Value, MaxVal, MinVal, Step;
char LcdTxt[21];
byte key;

   Value = *OldValue;
   MaxVal = 28800;	// 8 hours!
   MinVal = 1;

 //-----------------------------------------------------------------------------
   lcd_puts (LCD1, "Edit Soak Time:     ");
   lcd_clrl (LCD2);
   lcd_puts (LCD3, "     00:00:00       ");
   lcd_clrl (LCD4);
 //-----------------------------------------------------------------------------

   sprintf(LcdTxt, "     %d:%02d:%02d ", Value/3600, (Value%3600)/60, Value%60);
   lcd_puts (LCD3, LcdTxt);

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return;
   //------------------------------------------------
     key = get_key();
     if (key)
     {
	switch (key)
	{
	 case UP_KEY    :
			  if(Value >= 3600)	// > 1 hour?
			     Step = 1800;	// Step = 30min

			  else if(Value >= 1800)// > 30 min?
			     Step = 600;	// Step = 10min

			  else if(Value >= 600)	// > 10 min?
			     Step = 300;	// Step = 5min

			  else if(Value >= 60)	// > 1 min?
			     Step = 60;		// Step = 1min

			  else if(Value >= 30)	// > 30 sec?
			     Step = 10;		// Step = 10sec

			  else if(Value >= 10)	// > 10 sec?
			     Step = 5;		// Step = 5sec

			  else
			     Step = 1;		// Step = 1sec

			  if(Value <= (MaxVal-Step)) Value += Step;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case DOWN_KEY  :
			  if(Value <= 10)	// < 10 sec?
			     Step = 1;		// Step = 1sec

			  else if(Value <= 30)	// < 30 sec?
			     Step = 5;		// Step = 5sec

			  else if(Value <= 60)	// < 1 min?
			     Step = 10;		// Step = 10sec

			  else if(Value <= 600)	// < 10 min?
			     Step = 60;		// Step = 1min

			  else if(Value <= 1800)// < 30 min?
			     Step = 300;	// Step = 5min

			  else if(Value <= 3600)// > 1 hour?
			     Step = 600;	// Step = 10min

			  else
			     Step = 1800;	// Step = 30min

			  if(Value >= (MinVal+Step)) Value -= Step;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case ENTER_KEY : *OldValue = Value; return;
	 case CANCEL_KEY: return;
	}

	sprintf(LcdTxt, "     %d:%02d:%02d ", Value/3600, (Value%3600)/60, Value%60);
	lcd_puts (LCD3, LcdTxt);
     }
   }

}


//******************************************************************************
//
// 1.1.x: Edit Commands:
//
//******************************************************************************
void EditCommand(byte EdCode)
{
byte Value, MaxVal, MinVal, Step;
char Header[21];	// Header string.
char LcdTxt[32];
byte key;


   Value = Prg.Command[EdCode];

   if (Value >= ASP1 && Value <= ASP3)
   {
       MaxVal = ASP3;
       MinVal = ASP1;
       Step = 1;
       sprintf (Header, "Aspiration Velocety:");
       if (Value == ASP1) sprintf (LcdTxt, "     Low Speed   ");
       if (Value == ASP2) sprintf (LcdTxt, "    Medium Speed ");
       if (Value == ASP3) sprintf (LcdTxt, "     High Speed  ");
   }
   else if (Value >= REP0 && Value <= REP10)
   {
       MaxVal = REP10;
       MinVal = REP0;
       Step = 1;
       sprintf (Header, "Repetitions:        ");
       sprintf (LcdTxt, "       %d times  ", (Value-REP0));
   }
   else return;


 //-----------------------------------------------------------------------------
   lcd_puts (LCD1, Header);
   lcd_clrl (LCD2);
   lcd_clrl (LCD3);
   lcd_clrl (LCD4);

   lcd_puts (LCD3, LcdTxt);
//-------------------------------------------------------------------------

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return;
   //------------------------------------------------
     key = get_key();
     if (key)
     {
	switch (key)
	{
	 case UP_KEY    : if(Value <= (MaxVal-Step)) Value += Step;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case DOWN_KEY  : if(Value >= (MinVal+Step)) Value -= Step;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case ENTER_KEY : Prg.Command[EdCode] = Value; return;
	 case CANCEL_KEY: return;
	}


	 if      (Value == ASP1) sprintf (LcdTxt, "     Low Speed   ");
	 else if (Value == ASP2) sprintf (LcdTxt, "    Medium Speed ");
	 else if (Value == ASP3) sprintf (LcdTxt, "     High Speed  ");

	 else if (Value >= REP0 && Value <= REP10)
				 sprintf (LcdTxt, "       %d times  ", (Value-REP0));

	lcd_puts (LCD3, LcdTxt);
     }
   }

}

//******************************************************************************
//
// 1.1.x: Edit Command-Value:
//
//******************************************************************************
void EditCmdValue(byte EdCode)
{
int  Value, MaxVal, MinVal, Step;
byte Cmd;
char Header[21];	// Header string.
char LcdTxt[32];
byte key;


   Value = Prg.CmdValue[EdCode];
   Cmd   = Prg.Command[EdCode];



   switch(Cmd)
   {
    case ASP1:
    case ASP2:
    case ASP3:	Value = AspTime;
		MaxVal = 50;	// 5.0 sec
		MinVal = 10;	// 1sec
		Step = 10;
		sprintf (Header, "Aspiration Time:    ");
		sprintf (LcdTxt, "      %2d.%d sec  ", Value/10, Value%10);
		break;

    case DISP1:
    case DISP2:
    case DISP3:
    case DISP4:
    case DISPL1:
    case DISPL2:
    case DISPL3:
    case DISPL4:
		if (Prg.PlateType == 3 || Prg.PlateType == 13)	//1536:
		{
		  MaxVal = 200;	// 20ul
		  MinVal = 5;	// 0.5ul
		  Step = 1;	// 0.1ul
		}
		else if (Prg.PlateType == 2 || Prg.PlateType == 12)	//384:
		{
		  MaxVal = 1500; // 150ul
		  MinVal = 5;	 // 0.5ul
		  Step = 5;	 // 0.5ul
		}
		else		//96:
		{
		  MaxVal = 5000; // 500ul
		  MinVal = 10;	 // 1.0ul
		  Step = 10;	 // 1.0ul
		}
		sprintf (Header, "Dispense Volum:     ");
		sprintf (LcdTxt, "      %2d.%d ul   ", Value/10, Value%10);
		break;

    default:	return;
   }	// End switch()



 //-----------------------------------------------------------------------------
   lcd_puts (LCD1, Header);
   lcd_clrl (LCD2);
   lcd_clrl (LCD3);
   lcd_clrl (LCD4);

   lcd_puts (LCD3, LcdTxt);
 //-----------------------------------------------------------------------------


   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return;
   //------------------------------------------------
     key = get_key();
     if (key)
     {
	switch (key)
	{
	 case UP_KEY    : if(Value <= (MaxVal-Step)) Value+=Step;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case DOWN_KEY  : if(Value >= (MinVal+Step)) Value-=Step;
			  else Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			  break;

	 case ENTER_KEY : if (Cmd >= ASP1 && Cmd <= ASP3)	// Asp-Time:
			  {
			      AspTime = Value;
			      Prg.CmdValue[EdCode] &= 0x00ff;
			      Prg.CmdValue[EdCode] |= (AspTime << 8);
			  }
			  else
			  Prg.CmdValue[EdCode] = Value;
			  return;
	 case CANCEL_KEY: return;
	}

	if (Cmd >= ASP1 && Cmd <= ASP3)	// Asp-Time:
	{
	   sprintf (LcdTxt, "      %2d.%d sec  ", Value/10, Value%10);
	}
	else if (Cmd >= DISP1 && Cmd <= DISP4)
	{
	   sprintf (LcdTxt, "      %2d.%d ul   ", Value/10, Value%10);
	}
	else if (Cmd >= DISPL1 && Cmd <= DISPL4)
	{
	   sprintf (LcdTxt, "      %2d.%d ul   ", Value/10, Value%10);
	}

	lcd_puts (LCD3, LcdTxt);
     }
   }

}




//******************************************************************************
//
// 1.1.x: Edit Selected Rows:
//
//******************************************************************************
void EditRows(void)
{
word Rows0,Rows1;
//---------------
byte TopLine = 1;	// TxtLine shown in upper LCD.
byte CurLine = 1;	// Selected LCD-line (1-3).
byte Select = 1;	// Selected TxtLine.
//---------------
byte Row;
byte TotCnt = 0;
char Header[21];	// Header string.
char LcdTxt[34][24];	// List of TxtLines.
byte key;
//---------------


   Rows0 = Prg.PlateRows0;	// Working Copy
   Rows1 = Prg.PlateRows1;	// Working Copy

 //-----------------------------------------------------------------------------
   lcd_puts (LCD1, "Row Select:         ");
   lcd_clrl (LCD2);
   lcd_clrl (LCD3);
   lcd_clrl (LCD4);
 //-----------------------------------------------------------------------------

   //------------------------
   // "Row Select: 32 rows  "
   // "    01 - A: -1-      "
   // " -> 02 - B: -1-      "
   // "    03 - C:  0       "
   //------------------------


LCD_UPPDATE0:

   //------------------------------------------------------------------
   //--  Uppdate LcdTxt[22] from RowFlags:
   //------------------------------------------------------------------

   if (Prg.RowColumn == 0)	// Rows:
   {
     if (Prg.PlateType == 1 || Prg.PlateType == 11)	// 96:
     {
       sprintf (LcdTxt[0], "        A : -1- ");
       sprintf (LcdTxt[1], "        B : -1- ");
       sprintf (LcdTxt[2], "        C : -1- ");
       sprintf (LcdTxt[3], "        D : -1- ");
       sprintf (LcdTxt[4], "        E : -1- ");
       sprintf (LcdTxt[5], "        F : -1- ");
       sprintf (LcdTxt[6], "        G : -1- ");
       sprintf (LcdTxt[7], "        H : -1- ");
     }
     else
     {
       sprintf (LcdTxt[0], "      A+B : -1- ");
       sprintf (LcdTxt[1], "      C+D : -1- ");
       sprintf (LcdTxt[2], "      E+F : -1- ");
       sprintf (LcdTxt[3], "      G+H : -1- ");
       sprintf (LcdTxt[4], "      I+J : -1- ");
       sprintf (LcdTxt[5], "      K+L : -1- ");
       sprintf (LcdTxt[6], "      M+N : -1- ");
       sprintf (LcdTxt[7], "      O+P : -1- ");
       sprintf (LcdTxt[8], "      Q+R : -1- ");
       sprintf (LcdTxt[9], "      S+T : -1- ");
       sprintf (LcdTxt[10], "      U+V : -1- ");
       sprintf (LcdTxt[11], "      W+X : -1- ");
       sprintf (LcdTxt[12], "      Y+Z : -1- ");
       sprintf (LcdTxt[13], "     AA+BB: -1- ");
       sprintf (LcdTxt[14], "     CC+DD: -1- ");
       sprintf (LcdTxt[15], "     EE+FF: -1- ");
     }
   }
   else		// Columns:
   {
     if (Prg.PlateType == 1 || Prg.PlateType == 11)	// 96:
     {
       sprintf (LcdTxt[0], "        1 : -1- ");
       sprintf (LcdTxt[1], "        2 : -1- ");
       sprintf (LcdTxt[2], "        3 : -1- ");
       sprintf (LcdTxt[3], "        4 : -1- ");
       sprintf (LcdTxt[4], "        5 : -1- ");
       sprintf (LcdTxt[5], "        6 : -1- ");
       sprintf (LcdTxt[6], "        7 : -1- ");
       sprintf (LcdTxt[7], "        8 : -1- ");
       sprintf (LcdTxt[8], "        9 : -1- ");
       sprintf (LcdTxt[9], "       10 : -1- ");
       sprintf (LcdTxt[10], "       11 : -1- ");
       sprintf (LcdTxt[11], "       12 : -1- ");
     }
     else
     {
       sprintf (LcdTxt[0], "      1+2 : -1- ");
       sprintf (LcdTxt[1], "      3+4 : -1- ");
       sprintf (LcdTxt[2], "      5+6 : -1- ");
       sprintf (LcdTxt[3], "      7+8 : -1- ");
       sprintf (LcdTxt[4], "      9+10: -1- ");
       sprintf (LcdTxt[5], "     11+12: -1- ");
       sprintf (LcdTxt[6], "     13+14: -1- ");
       sprintf (LcdTxt[7], "     15+16: -1- ");
       sprintf (LcdTxt[8], "     17+18: -1- ");
       sprintf (LcdTxt[9], "     19+20: -1- ");
       sprintf (LcdTxt[10], "     21+22: -1- ");
       sprintf (LcdTxt[11], "     23+24: -1- ");
       sprintf (LcdTxt[12], "     25+26: -1- ");
       sprintf (LcdTxt[13], "     27+28: -1- ");
       sprintf (LcdTxt[14], "     29+30: -1- ");
       sprintf (LcdTxt[15], "     31+32: -1- ");
       sprintf (LcdTxt[16], "     33+34: -1- ");
       sprintf (LcdTxt[17], "     35+36: -1- ");
       sprintf (LcdTxt[18], "     37+38: -1- ");
       sprintf (LcdTxt[19], "     39+40: -1- ");
       sprintf (LcdTxt[20], "     41+42: -1- ");
       sprintf (LcdTxt[21], "     43+44: -1- ");
       sprintf (LcdTxt[22], "     45+46: -1- ");
       sprintf (LcdTxt[23], "     47+48: -1- ");
     }
   }

   for (Row = 0; Row < 24; Row++)
   {
	LcdTxt[Row][22] = 0;	// Clear selection!

	if (Row < 16)
	{
	    if (Rows0 & (0x0001<<Row))
		LcdTxt[Row][22] = 1;	// Selected row!
	}
	else
	{
	    if (Rows1 & (0x0001<<(Row-16)))	// 1536:
		LcdTxt[Row][22] = 1;	// Selected row!
	}
   }


   //------------------------------------------------------------------
   //--  Uppdate TextStrings, TotCnt, and Flags:
   //------------------------------------------------------------------

LCD_UPPDATE1:

   Rows0 = 0;	// Clear flags.
   Rows1 = 0;	// Clear flags.
   TotCnt= 0;	// Clear count.

   for (Row = 0; Row < MaxRows; Row++)
   {
//      sprintf (LcdTxt[Row], "     AA+BB: -1- ");

	if (LcdTxt[Row][22] > 0)	// Selected row!
	{
	    if (Prg.PlateType == 1 || Prg.PlateType == 11)	// 96:
	       TotCnt++;		// One row selected!
	    else
	       TotCnt+=2;		// Two rows selected!

	    LcdTxt[Row][12] = '-';		// -1-
	    LcdTxt[Row][13] = '1';		// -1-
	    LcdTxt[Row][14] = '-';		// -1-

	    if (Row < 16)
		Rows0 |= (0x0001<<Row);	// Set flag.
	    else
		Rows1 |= (0x0001<<(Row-16));	// Set flag.
	}
	else
	{
	    LcdTxt[Row][12] = ' ';		// 0
	    LcdTxt[Row][13] = '0';		// 0
	    LcdTxt[Row][14] = ' ';		//
	}

   }

   if (Prg.RowColumn == 0)	// Rows:
      sprintf (Header, "Row Select: %d rows  ", TotCnt);
   else
      sprintf (Header, "Col Select: %d cols  ", TotCnt);

   lcd_puts (LCD1, Header);

   //------------------------------------------------------------------
   //--  Uppdate LCD:
   //------------------------------------------------------------------

LCD_UPPDATE2:
   lcd_puts (LCD2, LcdTxt[TopLine-1]);
   lcd_puts (LCD3, LcdTxt[TopLine+0]);
   lcd_puts (LCD4, LcdTxt[TopLine+1]);


LCD_UPPDATE3:
   if (CurLine == 1)
   {
	  lcd_puts (LCD2, "->");
	  lcd_puts (LCD3, "  ");
	  lcd_puts (LCD4, "  ");
   }
   else if (CurLine == 2)
   {
	  lcd_puts (LCD2, "  ");
	  lcd_puts (LCD3, "->");
	  lcd_puts (LCD4, "  ");
   }
   else
   {
	  lcd_puts (LCD2, "  ");
	  lcd_puts (LCD3, "  ");
	  lcd_puts (LCD4, "->");
   }

   while (1)
   {
   //------------------------------------------------
     InterpretCommand();  //Check for seial commands
     if (MainState == 20)	// Flash Idle State
	 return;
   //------------------------------------------------
     key = get_key();
     switch (key)
     {
      case UP_KEY   : 	if(CurLine > 1)
			{
			   CurLine--;
			   goto LCD_UPPDATE3;	// Uppdate LCD.
			}
			else if(TopLine > 1)
			{
			   TopLine--;
			   goto LCD_UPPDATE2;	// Uppdate LCD.
			}
			Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			break;

      case DOWN_KEY : 	if(CurLine < 3)
			{
			   CurLine++;
			   goto LCD_UPPDATE3;	// Uppdate LCD.
			}
			else if(TopLine <= (MaxRows-3))
			{
			   TopLine++;
			   goto LCD_UPPDATE2;	// Uppdate LCD.
			}
			Beep_Cnt = SHORT_BEEP;		// A long BEEP.
			break;


      case ENTER_KEY :	Select =  TopLine + CurLine - 2;
			if (LcdTxt[Select][22] == 0)	// Selected row!
			    LcdTxt[Select][22] = 1;	//Toggle
			else
			    LcdTxt[Select][22] = 0;	//Toggle

			goto LCD_UPPDATE1;	// Done! Uppdate ALL lines!


      case CANCEL_KEY :	Prg.PlateRows0 = Rows0;	// Save Working Copy
			Prg.PlateRows1 = Rows1;	// Save Working Copy
			return;

     }
   }

}



//******************************************************************************
//  Sub-rutine: Count number of Rows in use:
//******************************************************************************
byte GetRowCnt(void)
{
word Rows0,Rows1;
byte Row;
byte TotCnt;

   Rows0 = Prg.PlateRows0;	// Working Copy
   Rows1 = Prg.PlateRows1;	// Working Copy
   TotCnt = 0;

   //------------------------------------------------------------------
   for (Row = 0; Row < MaxRows; Row++)
   {
	   if (Row < 16)
	   {
	      if (Rows0 & (0x0001<<Row))
	      {
		 if (Prg.PlateType == 1 || Prg.PlateType == 11)	// 96 = 1 row!
		    TotCnt++;	// One row done!
		 else
		    TotCnt += 2;
	      }
	   }
	   else
	   {
	      if (Rows1 & (0x0001<<(Row-16)))
	      {
		  TotCnt += 2;
	      }
	   }
   }
   //------------------------------------------------------------------

return (TotCnt);
}




