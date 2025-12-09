#include  <stdio.h>
#include  <stdlib.h>

#include "globdata.h"
#include "comm.h"
#include "motor.h"
#include "keybrd.h"
#include "display.h"

//***********************************************************************
//* FILE        : comm2.c
//*
//* DESCRIPTION : Parse buffer for external commands (RS-232), and execute them.
//*
//***********************************************************************


//--------------------------------------------------------------
// Functions in COMM2.C:
// PUBLIC functions:
//--------------------------------------------------------------

void init_comm();
void InterpretCommand();

//--------------------------------------------------------------
// Functions in COMM1.C:
//--------------------------------------------------------------
extern void ClrAllFlashPrograms (void);
extern void Send_ParameterBlock (void);
extern int Receive_ParameterBlock (void);
extern int Upload_All_SubProgram_Blocks (char Comand);
extern int Download_SubProgram_Block (void);
extern void Send_Device_Code (void);
extern void Send_Device_Code2 (void);
extern void Send_SerialNo (void);
extern void Send_FirmVer (void);
extern void Send_ProgName (void);
extern void Send_ProgDate (void);
extern void SetFlashParam (char CommandType, byte *InputStr);

//-----------------------------------------------------------------------
//----- EXTERNAL FUNCTIONS: ---------------------------------------------
//-----------------------------------------------------------------------
extern void Load_New_Flash_Program (void);

extern byte get_char (void);	        // Get RS-232 byte.
extern int put_char (byte tx_byte);	// Transmit RS-232 byte.
extern int put_word (word RS_word);	// Transmit RS-232 word.
extern int put_s (far char *str);	// Transmit RS-232 string (Pointer to text-string).

extern void lcd_puts (byte adr, const char *str);
extern void lcd_clrl (byte adr);
extern void delay_ms(word time);
extern void stop_motor0 (void);		// Stop motor1.
extern void stop_motor1 (void);		// Stop motor1.
extern void stop_motor2 (void);		// Stop motor1.
extern int write_eeprom_param (void);
//-------------------------------------------------------------------
extern void calibrate_press (void);	// Calibration rutine in Calib.c!
extern void calibrate_disp_lift (void);	// Calibration rutine in Calib.c!
extern void calibrate_carriage (void);	// Calibration rutine in Calib.c!
extern void calibrate_liquids (void);	// Calibration rutine in Calib.c!
//-------------------------------------------------------------------


//Global variables for communication module

word Comm_State;	 //current state of the command state machine

#define STR_LEN 64

byte InputStr[STR_LEN+1]; //contains received parameter value
int  InputStrIndex;
char CommandType;	 //Contains type of command

//-------------------------------------------------------------------
extern int rec_bufcnt;	        // Counter for serial received data.
//-------------------------------------------------------------------

//*******************************************************************
//  FUNCTION  : init_comm
//*******************************************************************

void init_comm (void)
{
	Comm_State =  0;
}




//*******************************************************************
//  FUNCTION  : InterpretCommand
//  METHODE   : Parse buffer for commands and execute them.
//*******************************************************************
void InterpretCommand (void)
{
	byte Token;            	//current parsing token
	bool SyntaxError1 = FALSE; 	//Init to no errors
	bool SyntaxError2 = FALSE; 	//Init to no errors
	static byte NewCommand;
	int i;

	//-------------------------------------------------------------------
	//   PC (Embla) commands:	[ESC]/(x);(value);
	//   PC (Robot) commands:	#(x)[\n][\r];
	//-------------------------------------------------------------------

	if (rec_bufcnt)      // if anything in serial input buffer
	{
		Token = get_char();

		if (Token == ESC)
		{
			Comm_State = 0;		//always reset state with a ESC
			for (i=0; i<=STR_LEN; i++)
				InputStr[i] = 0;
			InputStrIndex = 0;
			CommandType = ' ';	//Contains type of command
		}

		if (Token == '#')
		{
			Comm_State = 0;		//always reset state with a '#'
			for (i=0; i<=STR_LEN; i++)
				InputStr[i] = 0;
			InputStrIndex = 0;
			NewCommand = 0;		//Contains type of command
			ExtCommand = 0;		//Contains type of command
			ExtComVal = 0;		//Contains value to command
		}

		switch(Comm_State)
		{
		case 0 : //wait for ESC for start of a new command
			if (Token == ESC)
			{
				Comm_State = 1;   //stay here until ESC
			}
			else if (Token == '#')
			{
				Comm_State = 200;   //stay here until '#'
			}
			break;

			//-------------------------------------------------------------------
			//   PC (Robot) commands:	#(x)[\n][\r];
			//-------------------------------------------------------------------
		case 200: //wait for command type:
			NewCommand = Token;
			switch (NewCommand)
			{
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5': Comm_State = 203; break;	// (Test/Calibration commands)

			case '?': Comm_State = 203; break;	// Status request
			case 'S': Comm_State = 203; break;	// Start command
			case 'R': Comm_State = 203; break;	// Stop command
			case 'P': Comm_State = 201; break;	// Select program number
			case 'V': Comm_State = 210; break;	// Set mini-prime volume

			default : SyntaxError2 = TRUE; break;	// Not valid command!
			}
			break;

		case 201: //Get P-parameter value 1. digit
			if (isdigit(Token))
			{
				InputStr[0] = Token;		// 1. digit
				InputStrIndex = 1;
				ExtComVal2 = 0;	// Not Prime!
				Comm_State = 202;
			}
			else if (Token == 'A')		// Special program A1,A2,A3,A4.
			{
				InputStr[0] = '1';		// 1. digit
				InputStr[1] = '0';		// 1. digit
				InputStrIndex = 2;
				ExtComVal2 = 1;	// Maintenance Prime!
				Comm_State = 202;
			}
			else if (Token == 'R')		// Special program A1,A2,A3,A4.
			{
				InputStr[0] = '1';		// 1. digit
				InputStr[1] = '0';		// 1. digit
				InputStrIndex = 2;
				ExtComVal2 = 2;	// Full Prime Row!
				Comm_State = 202;
			}
			else if (Token == 'C')		// Special program A1,A2,A3,A4.
			{
				InputStr[0] = '1';		// 1. digit
				InputStr[1] = '0';		// 1. digit
				InputStrIndex = 2;
				ExtComVal2 = 3;	// Full Prime Col!
				Comm_State = 202;
			}
			else
				SyntaxError2 = TRUE;

			break;

		case 202: //Get P-parameter value 2. digit
			if (isdigit(Token))
			{
				InputStr[InputStrIndex++] = Token;		// 2. digit
				InputStr[InputStrIndex++] = 0;		// NULL termination!
				ExtComVal = atoi((char *)InputStr);
				if (ExtComVal >= 101)	// A1,A2,A3,A4
					ExtComVal -= 1;
				Comm_State = 203;		// Wait for [CR] or [LF].
			}
			else
				SyntaxError2 = TRUE;

			break;

		case 203: // wait for [CR] or [LF] as end of command:
			if (Token == '\n' || Token == '\r')
			{
				switch (NewCommand)
				{
				case '0': ExtCommand = T0_Cmd; break;	// (Test/Calibration commands)
				case '1': ExtCommand = T1_Cmd; break;	// (Test/Calibration commands)
				case '2': ExtCommand = T2_Cmd; break;	// (Test/Calibration commands)
				case '3': ExtCommand = T3_Cmd; break;	// (Test/Calibration commands)
				case '4': ExtCommand = T4_Cmd; break;	// (Test/Calibration commands)
				case '5': ExtCommand = T5_Cmd; break;	// (Test/Calibration commands)

				case '?': ExtCommand = STATUS_Cmd; break;	// Status request
				case 'S': ExtCommand = START_Cmd; break;	// Start command
				case 'R': ExtCommand = STOP_Cmd; break;	// Stop command
				case 'P': ExtCommand = PROG_Cmd; break;	// Select program number

				default:  SyntaxError2 = TRUE; break;		// Not valid command!
				}
			}
			else
				SyntaxError2 = TRUE;

			Comm_State = 0;		// Wait for next command
			break;

			//-------------------------------------------------------------------
			//- Get mini-prime Volume-Value digits:
			//-------------------------------------------------------------------

		case 210: //Get volume-value digits:
			SyntaxError2 = TRUE;
			if (isdigit(Token) && InputStrIndex < 4)	// 1-4 digits!
			{
				InputStr[InputStrIndex++] = Token;   // Store the digit.
				InputStr[InputStrIndex] = 0;		// NULL termination.
				SyntaxError2 = FALSE;		// Ok!
			}
			else if (Token == '\n' || Token == '\r')
			{
				if (InputStrIndex >= 1 && InputStrIndex <= 4)
				{
					ExtComVal = atoi((char *)InputStr);
					if (ExtComVal <= 9999)
					{
						MiniPrimeVol = ExtComVal;
						put_s(OK_ACK);			// send ACK back to PC
						SyntaxError2 = FALSE;		// Ok!
						Comm_State = 0;		// Wait for next command
					}
				}
			}
			break;


			//-------------------------------------------------------------------
			//-------------------------------------------------------------------
			//   PC (Embla) commands:	[ESC]/(x);(value);
			//-------------------------------------------------------------------
			//-------------------------------------------------------------------

		case 1 : //wait for '/' as second parameter
			if (Token == SLASH)
				Comm_State = 2;
			else
				SyntaxError1 = TRUE;
			break;

		case 2 : //wait for command type:
			InputStrIndex = 0;
			if (isalpha(Token) || (Token == CR ) || (Token == ENDOFFILE))
			{
				CommandType = Token;

				if (Token == CR)
					Comm_State = 0;
				else
					Comm_State = 3;
			}
			else
				SyntaxError1 = TRUE;
			break;

		case 3 : //wait for ";"
			if (Token == ';')
			{
				switch (CommandType)
				{
				case 'A': Download_SubProgram_Block();	//Download Subprogram block (256 bytes).
					MainState = 21;			// Flash Idle State
					Comm_State = 0;
					break;
				case 'B': Upload_All_SubProgram_Blocks ('B');	//Upload Subprogram blocks (256 bytes).
					Comm_State = 0;
					break;
				case 'U': Upload_All_SubProgram_Blocks ('U');	//Upload Subprogram blocks (256 bytes).
					Comm_State = 0;
					break;
				case 'C': Send_Device_Code ();	//Send Device Code
					Comm_State = 0;
					break;
				case 'Q': Send_Device_Code2 ();	//Send UserProg ID-Code (Userprograms in 0x0FC0000)
					Comm_State = 0;
					break;
				case 'D': Send_FirmVer ();		//Send Firmware version tekst.
					Comm_State = 0;
					break;
				case 'E': Send_ProgName ();	//Send Program Name
					Comm_State = 0;
					break;
				case 'F': Send_ProgDate ();	//Send Program Name
					Comm_State = 0;
					break;
				case 'G': Send_SerialNo ();	//Send Serial Number
					Comm_State = 0;
					break;
				case 'H': Send_ParameterBlock ();	//Send Parameters back to PC.
					Comm_State = 0;
					break;
				case 'I': Receive_ParameterBlock ();	//Receive Parameters from PC.
					Comm_State = 0;
					MainState = 0;		// Init Main State! (Uppdate display)
					break;
				case 'J': if (write_eeprom_param () == 0)	//Write Parameters to ext.EEProm!
							  put_char(ACK);		// send ACK back to PC
					Comm_State = 0;
					MainState = 0;		// Init Main State! (Uppdate display)
					break;

					//----------------------------------------------
				case 'L': if(RunningState == 0)	// Calibrate_Pressure!
						  {
							  put_char(ACK);		// send ACK back to PC
							  lcd_puts (LCD1, "--------------------");
							  lcd_puts (LCD2, "-     Pressure     -");
							  lcd_puts (LCD3, "-   Calibration!   -");
							  lcd_puts (LCD4, "--------------------");

							  RunningState = 1;
							  calibrate_press ();	// Calibration rutine in Calib.c!
							  RunningState = 0;
							  Comm_State = 0;
							  MainState = 0;		// Init Main State! (Uppdate display)
						  }
						  break;

				case 'M': if(RunningState == 0)	// Calibrate_Disp_Lift!
						  {
							  put_char(ACK);		// send ACK back to PC
							  lcd_puts (LCD1, "     Alignment!     ");
							  lcd_puts (LCD2, "--------------------");
							  lcd_clrl (LCD3);
							  lcd_clrl (LCD4);
							  calibrate_disp_lift ();	// Calibration rutine in Calib.c!
							  Comm_State = 0;
							  MainState = 0;		// Init Main State! (Uppdate display)
						  }
						  break;

				case 'O': if(RunningState == 0)	// Calibrate_Carriage!
						  {
							  put_char(ACK);		// send ACK back to PC
							  lcd_puts (LCD1, "--------------------");
							  lcd_puts (LCD2, "-     Carriage     -");
							  lcd_puts (LCD3, "-    Alignment!    -");
							  lcd_puts (LCD4, "--------------------");

							  calibrate_carriage ();	// Calibration rutine in Calib.c!
							  Comm_State = 0;
							  MainState = 0;		// Init Main State! (Uppdate display)
						  }
						  break;

				case 'P': if(RunningState == 0)	// Calibrate_Carriage!
						  {
							  put_char(ACK);		// send ACK back to PC
							  lcd_puts (LCD1, "--------------------");
							  lcd_puts (LCD2, "-      LIQUID      -");
							  lcd_puts (LCD3, "-   CALIBRATION!   -");
							  lcd_puts (LCD4, "--------------------");

							  calibrate_liquids ();	// Calibration rutine in Calib.c!
							  Comm_State = 0;
							  MainState = 0;		// Init Main State! (Uppdate display)
						  }
						  break;
						  //----------------------------------------------

				case 'R':
					if (RunningState != 0)   // Program running?
					{
						Disp_Valve_off;	// Close DISP-valve.
						Pres_Pump_off;	// Stop Pressure-Pump!
						Vac_Pump_off;	// Stop Vacuum-Pump!
						Waste_Pump_off;	// Stop Waste-Pump!
						Pres_Valve_off;	// Close Pressue-Valve!
						Vac_Valve_off;	// Close Vacuum-Valve!
						stop_motor0 ();	// Stop motor1.
						stop_motor1 ();	// Stop motor1.
						stop_motor2 ();	// Stop motor2.
					}
					//-------------------------------------
					//- State machine in main program: ----
					//-------------------------------------
					lcd_puts (LCD2, "--------------------");
					lcd_puts (LCD3, "-  REPROGRAMMING!  -");
					lcd_puts (LCD4, "--------------------");
					MainState = 20;		// Flash Idle State
					RunningState = 0;	// No program run!
					//-------------------------------------
					put_char(ACK);		// send ACK back to PC
					Comm_State = 0;		//(ready to receive program-blocks).
					break;

					//-------------------------------------
				case 'z': // Release from reprogramming:
					//-------------------------------------
					MainState = 22;		// Idle State
					RunningState = 0;	// No program run!
					break;

					//-------------------------------------
				case 'Z': // Force a software RESET PROSESSOR!
					//-------------------------------------
					asm rst;		// RESET PROSESSOR!
					Comm_State = 0;
					break;

				case 'X': Comm_State = 100; break;   // Download new FirmWare

				case CR : Comm_State = 0; break;	  //End of line?

				case ENDOFFILE: Comm_State = 999; break;   //End of file -- finnished

				default : SyntaxError1 = TRUE; break;

				} //switch
			} //token
			break;


		case 100: //Download new FirmWare
			if (Token == 0x55)  //Hex 55
				Comm_State = 101;
			else SyntaxError1 = TRUE;
			break;
		case 101: if (Token == 0xAA) //Hex AA
				  {
					  lcd_puts (LCD1, "--------------------");
					  lcd_puts (LCD2, "-     FIRMWARE     -");
					  lcd_puts (LCD3, "-  REPROGRAMMING!  -");
					  lcd_puts (LCD4, "--------------------");
					  if (RunningState != 0)   // Program running?
					  {
						  Disp_Valve_off;	// Close DISP-valve.
						  Pres_Pump_off;	// Stop Pressure-Pump!
						  Vac_Pump_off;	// Stop Vacuum-Pump!
						  Waste_Pump_off;	// Stop Waste-Pump!
						  Pres_Valve_off;	// Close Pressue-Valve!
						  Vac_Valve_off;	// Close Vacuum-Valve!
						  stop_motor0 ();	// Stop motor1.
						  stop_motor1 ();	// Stop motor1.
						  stop_motor2 ();	// Stop motor2.
					  }
					  Load_New_Flash_Program ();
				  }
				  Comm_State = 0;
				  break;  //Never come here; Reset after new program loaded


		case 999: //End of all programs
			Comm_State = 0;
			break;
		} //end switch state


		if (SyntaxError2 == TRUE)   // Error or not implemented:
		{
			SyntaxError2 = FALSE;	// Reset error for next
			Comm_State = 0;		// Start statemachine from beginning of a command
			NewCommand = 0;		// No Command
			ExtCommand = 0;		// No Command
			ExtComVal  = 0;		// No Command Value
			ExtComVal2 = 0;		// No Command Value
			put_s(SYNTAXERROR_ACK);	// send ACK back to PC
		}

		if (SyntaxError1 == TRUE)   // Error or not implemented:
		{
			SyntaxError1 = FALSE;	// Reset error for next
			Comm_State = 0;		// Start statemachine from beginning of a command
			NewCommand = 0;		// No Command
			ExtCommand = 0;		// No Command
			ExtComVal  = 0;		// No Command Value
			// -> Later send error message back to PC
		}

	} //if anything in buffer
}



