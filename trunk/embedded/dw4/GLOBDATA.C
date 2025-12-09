#include "struct.h"	// Definition of struct's and table sizes.

//----------------------------------------------------------------------
// Global data declarations in: globdata.h
//----------------------------------------------------------------------

//--------------------------------------------------------------
// Memory mapped I/O ports:
//--------------------------------------------------------------
// LCD-Display:
//--------------------------------
byte LCD_Data_In;		// Read 8 bits Display-Data (not used).
#pragma locate (LCD_Data_In = 0x02000)

byte LCD_Data_Out;			// Write 8 bits Display-Data.
#pragma locate (LCD_Data_Out = 0x02001)

byte LCD_Inst_In;		// Read LCD Intruction byte
#pragma locate (LCD_Inst_In = 0x02002)

byte LCD_Inst_Out;		// Write LCD Intruction byte
#pragma locate (LCD_Inst_Out = 0x02003)

#define LCD_Busy (LCD_Inst_In & 0x80)			// Read 8 bits Display-ControlReg (Busy-flag).

//--------------------------------
// SENSOR-SIGNAL input port:
//--------------------------------
byte In_Port0;		// 8 bits SENSOR SIGNAL input port.
#pragma locate (In_Port0 = 0x03000)
// Bit 0 = Buffer1 Alarm
// Bit 1 = Buffer2 Alarm
// Bit 2 = Buffer3 Alarm
// Bit 3 = Buffer4 Alarm
// Bit 4 = Waste Alarm
// Bit 5 = RinseHead Mounted
// Bit 6 = Vacuum Sensor
// Bit 7 = Aux Input

//--------------------------------
// Keypad + HeadCoding input port:
//--------------------------------
byte In_Port1;		// 8 bits KEYPAD+Head-Codeing input port.
#pragma locate (In_Port1 = 0x04000)
// Bit 0 = DOWN-Key
// Bit 1 = START-Key
// Bit 2 = UP-Key
// Bit 3 = CANCEL-Key
// Bit 4 = ENTER-Key
// Bit 5 = STOP-Key
// Bit 6 = Head_Coding (5)
// Bit 7 = Head_Coding (6)

//--------------------------------
// OutPort 0-4:
//--------------------------------
byte Out_Port0;		// 8 bits output port.
#pragma locate (Out_Port0 = 0x05000)
// Bit 0 = Disp Valve 1a, full current.
// Bit 1 = Disp Valve 1b, reduced current.
// Bit 2 = Disp Valve 2a, full current.
// Bit 3 = Disp Valve 2b, reduced current.
// Bit 4 = Disp Valve 3a, full current.
// Bit 5 = Disp Valve 3b, reduced current.
// Bit 6 = Disp Valve 4a, full current.
// Bit 7 = Disp Valve 4b, reduced current.

byte Out_Port1;		// 8 bits output port.
#pragma locate (Out_Port1 = 0x05001)
// Bit 0 = Head_Coding (1)
// Bit 1 = Head_Coding (2)
// Bit 2 = Head_Coding (3)
// Bit 3 = Head_Coding (4)
// Bit 4 = ADC4 (multiplexer adress 0)
// Bit 5 = ADC4 (multiplexer adress 1)
// Bit 6 = Motor Reset (all 3 drivers)
// Bit 7 = Rinse Valve 9.

byte Out_Port2;		// 8 bits output port.
#pragma locate (Out_Port2 = 0x05002)
// Bit 0 = Rinse Valve 1.
// Bit 1 = Rinse Valve 2.
// Bit 2 = Rinse Valve 3.
// Bit 3 = Rinse Valve 4.
// Bit 4 = Rinse Valve 5.
// Bit 5 = Rinse Valve 6.
// Bit 6 = Rinse Valve 7.
// Bit 7 = Rinse Valve 8.

byte Out_Port3;		// 8 bits output port.
#pragma locate (Out_Port3 = 0x05003)
// Bit 0 = M0_Enable (stop/run).
// Bit 1 = M0_Direction.
// Bit 2 = M0_Power (high/low current).
// Bit 3 = not used...
// Bit 4 = M1_Enable (stop/run).
// Bit 5 = M1_Direction.
// Bit 6 = M1_Power (high/low current).
// Bit 7 = not used...

byte Out_Port4;		// 8 bits output port.
#pragma locate (Out_Port4 = 0x05004)
// Bit 0 = M2_Enable (stop/run).
// Bit 1 = M2_Direction.
// Bit 2 = M2_Power (high/low current).
// Bit 3 = not used...
// Bit 4 = Vacuum Valve.
// Bit 5 = Vacuum Pump.
// Bit 6 = Waste Pump.
// Bit 7 = Aux Out.
//--------------------------------------------------------------

register byte Port0;	// Byte for OutPort0 outputs.
register byte Port1;	// Byte for OutPort1 outputs.
register byte Port2;	// Byte for OutPort2 outputs.
register byte Port3;	// Byte for OutPort3 outputs.
register byte Port4;	// Byte for OutPort4 outputs.

//--------------------------------------------------------------

struct ParamBlock Param[1];  	// Parameters from external EEPROM.
struct ProgramBlock RamProg[1];	// Copy of Current-Program (from Flash).

//----- MAIN: ------------------------------------------------------------

word MainState;		// Current state, main idle program
word DispState;		// Current state, main idle program
word RunningState;	// Current state, Action do be done
word PrgmState;		// LocalState in run_prgm() state machine (prgm.c)
byte Wanted_Prgm;	// Program shown on display

word ExtCommand;	// Command from RS232 (robot).
word ExtComVal;		// Command-value from RS232 (robot).
word ExtComVal2;		// Command-value from RS232 (robot, used for PRIME)
word ExtStart;

byte PlateAlarm;	// Alarm-Flag
byte Buffer1Alarm;	// Alarm-Flag
byte Buffer2Alarm;	// Alarm-Flag
byte Buffer3Alarm;	// Alarm-Flag
byte Buffer4Alarm;	// Alarm-Flag
byte WasteAlarm;	// Alarm-Flag
byte WasteError;	// Alarm-Flag
byte PressureAlarm;	// Low/High Pressure Alarm (Low Pressure Timeout).
byte PressureOk;	// Pressure is accurate.
byte SensorError;	// Pressure Sensor Error (DiffPres > 100mBar).
byte PressureError;	// Pressure Sensor Error (Program-Running-Error).
byte P_Fine_Reg;	// High accuracy regulation!
byte CurrentADC;	// Current selected Pressure-Channel!
byte Pres1Alarm;	// Pressure1 Out-of-Range Alarm.
byte Pres2Alarm;	// Pressure2 Out-of-Range Alarm.
byte Pres3Alarm;	// Pressure3 Out-of-Range Alarm.
byte Pres4Alarm;	// Pressure4 Out-of-Range Alarm.
byte IdleFlag;		// IdlePressureFlag
int  RegPressure;	// Regulated pressure (mBar).
int  CurrentPres;	// Current selected pressure.
//----------------------
int  Press[6];		// Current pressure (ADC-value).
int  mBar[7];		// Current pressure (mBar-value).

//----------------------
word MiniPrimeVol;	// Robot-Volume in ul for Maintenence Prime (default 100mS)

//----- TIMER: -----------------------------------------------------------

register long Timer_100uS;		// Timer for DISPense command, 100uS!
register word Count_100uS;		// Timer for DISPense command, 100uS!
register byte DispFlag;			// flag!
register byte Start_Flag;		// Flag from timer1_interrupt()

register byte ms1_prescale;		// Prescale for 1mS.
register byte ms20_prescale;   		// 20ms Prescaler in timer1_interrupt()
register word sec_prescale;		// 1 sec Prescaler in timer1_interrupt()
register word ms1000_prescale;		// 1 sec Prescaler in timer1_interrupt()
register word LED_prescale;		// LED flash prescaler
register byte LED_On;			// LED 800mS counter.
register byte LED_Off;			// LED 200mS counter.
register byte UppdateFlag;		// flag!
register byte SecFlag;			// Flag the new 1sec value.
register word Beep_Cnt;			// Beeper counter.

register word RS_Timer_1mS;		// RS-232 timout counter.
register word Timer_1sec;		// Timer for SOAK-command.

register word Timer1_Delay;		// Timer counter in timer2_interrupt()
register word Timer_1mS;		// Timer for DISPense command.
register word DispPause;		// Timer for DISPense command.

register word IdleTimer;		// Timer counter in timer1_interrupt()
register word PressureTimeout;		// 120 sec timout (PressureAlarm)
register word ADC_Timer;		// Timer in Pressure-regulation.


//----- DISPLAY: ---------------------------------------------------------



//----- MOTORS: ---------------------------------------------------------

register byte Motor0_Start_Flag;	// Flag syncronized with motor-clock!
register byte Motor1_Start_Flag;	// Flag syncronized with motor-clock!
register byte Motor2_Start_Flag;	// Flag syncronized with motor-clock!

//----- MOTOR-0: ---------------------------------------------------------

register int  Motor0_TachoPos;		// Position in tacho-counts.
register int  Motor0_TachoPos_End;	// End-Position in tacho-counts.

register byte Motor0_PWM;		// Current PWM output duty cycle.
register byte Motor0_Busy;		// Status: Motor1 is running.

//----- MOTOR-1: ---------------------------------------------------------

register int  Motor1_TachoPos;		// Position in tacho-counts.
register int  Motor1_TachoPos_End;	// End-Position in tacho-counts.

register byte Motor1_PWM;		// Current PWM output duty cycle.
register byte Motor1_Busy;		// Status: Motor1 is running.

//----- MOTOR-2: ---------------------------------------------------------

register int  Motor2_TachoPos;		// Position in tacho-counts.
register int  Motor2_TachoPos_End;	// End-Position in tacho-counts.

register byte Motor2_PWM;		// Current PWM output duty cycle.
register byte Motor2_Busy;		// Status: Motor1 is running.


//----- KEYBOARD: ----------------------------------------------------------

register byte KeyBuffer;//Leagally pressed key inserted into buffer
register byte NewKey;   //Latest key pressed
register byte NextKey;  //Next pressed key
register byte KeyState; //State of keyboardhandler in Timer1
register byte LastKey;	//Leagally pressed key inserted into buffer
