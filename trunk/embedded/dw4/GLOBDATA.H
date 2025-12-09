#include "struct.h"

//**************************************************************************
//*  FUNCTION    : GLOBDATA.h
//*  METHODE     : Global data declarations (globdata.c & flashdat.c)
//**************************************************************************

//******************************************************************************
// Const-Data in flash-PROM (flashdat.c):
//******************************************************************************
extern const byte   FwID[];			// [ 3] Firmware ID (New adresses!)
extern const char   FirmwareVerTxt[];		// [64] Firmware Version
extern const struct ParamBlock CParam[];  	// [ 1] Default parameters
extern const struct ProgInfoBlock ProgInfo[];	// [ 1] User Programs Block-0.
extern const struct ProgramBlock Program[];	// [MAX_SUBPROGRAMS] 99 user-program blocks
//------------------------------------------------------------------------------



//******************************************************************************
// RAM-Data: (globdata.c):
//******************************************************************************

extern struct ParamBlock   Param[];  	// Parameters read from external EEPROM!
extern struct ProgramBlock RamProg[];  // Copy of Current-Program (flash).

//--------------------------------------------------------------
// Memory mapped I/O ports:
//--------------------------------------------------------------
extern byte LCD_Data_In;	// Read 8 bits Display-Data (not used).
extern byte LCD_Data_Out;	// Write 8 bits Display-Data.
extern byte LCD_Inst_In;	// Read LCD Intruction byte
extern byte LCD_Inst_Out;	// Write LCD Intruction byte
extern byte In_Port0;		// 8 bits SENSOR SIGNAL input port.
extern byte In_Port1;		// 8 bits KEYPAD+AUX input port.
extern byte Out_Port0;		// 8 bits output port.
extern byte Out_Port1;		// 8 bits output port.
extern byte Out_Port2;		// 8 bits output port.
extern byte Out_Port3;		// 8 bits output port.
extern byte Out_Port4;		// 8 bits output port.
//--------------------------------------------------------------
// I/O ports ram-registers:
//--------------------------------------------------------------
extern register byte Port0;	// Byte for OutPort0 outputs.
extern register byte Port1;	// Byte for OutPort1 outputs.
extern register byte Port2;	// Byte for OutPort2 outputs.
extern register byte Port3;	// Byte for OutPort3 outputs.
extern register byte Port4;	// Byte for OutPort4 outputs.
//--------------------------------------------------------------


//----- MAIN: ------------------------------------------------------------

extern word MainState;		// Current state, main idle program
extern word DispState;		// Current state, main idle program
extern word RunningState;	// Current state, Action do be done
extern word PrgmState;		// LocalState in run_prgm() state machine (prgm.c)
extern byte Wanted_Prgm;	// Program shown on display

extern word ExtCommand;	// Command from RS232 (robot).
extern word ExtComVal;		// Command-value from RS232 (robot).
extern word ExtComVal2;		// Command-value from RS232 (robot, used for PRIME)
extern word ExtStart;

extern byte PlateAlarm;	// Alarm-Flag
extern byte Buffer1Alarm;	// Alarm-Flag
extern byte Buffer2Alarm;	// Alarm-Flag
extern byte Buffer3Alarm;	// Alarm-Flag
extern byte Buffer4Alarm;	// Alarm-Flag
extern byte WasteAlarm;	// Alarm-Flag
extern byte WasteError;	// Alarm-Flag

extern byte PressureAlarm;	// Low/High Pressure Alarm (Low Pressure Timeout).
extern byte PressureOk;		// Pressure is accurate.
extern byte SensorError;	// Pressure Sensor Error (DiffPres > 100mBar, Pressureregulator).
extern byte PressureError;	// Pressure Sensor Error (Program-Running-Error).
extern byte P_Fine_Reg;	// High accuracy regulation!
extern byte CurrentADC;	// Current selected Pressure-Channel!
extern byte Pres1Alarm;	// Pressure1 Out-of-Range Alarm.
extern byte Pres2Alarm;	// Pressure2 Out-of-Range Alarm.
extern byte Pres3Alarm;	// Pressure3 Out-of-Range Alarm.
extern byte Pres4Alarm;	// Pressure4 Out-of-Range Alarm.
extern byte IdleFlag;		// IdlePressureFlag
extern int  RegPressure;	// Regulated pressure.
extern int  CurrentPres;	// Current selected pressure.
//----------------------
extern int  Press[6];		// Current pressure (ADC-value).
extern int  mBar[7];		// Current pressure (mBar-value).
//----------------------
extern word MiniPrimeVol;	// Robot-Volume in ul for Maintenence Prime (default 100mS)


//----- TIMER: -----------------------------------------------------------

extern register long Timer_100uS;		// Timer for DISPense command, 100uS!
extern register word Count_100uS;		// Timer for DISPense command, 100uS!
extern register byte DispFlag;			// flag!
extern register byte Start_Flag;		// Flag from timer1_interrupt()

extern register byte ms1_prescale;		// Prescale for 1mS.
extern register byte ms20_prescale;   		// 20ms Prescaler in timer1_interrupt()
extern register word sec_prescale;		// 1 sec Prescaler in timer1_interrupt()
extern register word ms1000_prescale;		// 1 sec Prescaler in timer1_interrupt()
extern register word LED_prescale;		// LED flash prescaler
extern register byte LED_On;			// LED 800mS counter.
extern register byte LED_Off;			// LED 200mS counter.
extern register byte UppdateFlag;		// flag!
extern register byte SecFlag;			// Flag the new 1sec value.
extern register word Beep_Cnt;			// Beeper counter.

extern register word RS_Timer_1mS;		// RS-232 timout counter.
extern register word Timer_1sec;		// Timer for SOAK-command.

extern register word Timer1_Delay;		// Timer counter in timer2_interrupt()
extern register word Timer_1mS;			// Timer for DISPense command.
extern register word DispPause;			// Timer for DISPense command.

extern register word IdleTimer;			// Timer counter in timer1_interrupt()
extern register word PressureTimeout;		// 120 sec timout (PressureAlarm)
extern register word ADC_Timer;			// Timer in Pressure-regulation.


//----- DISPLAY: ---------------------------------------------------------



//----- MOTOR-0: ---------------------------------------------------------
extern register byte Motor0_Start_Flag;		// Flag syncronized with motor-clock!
extern register byte Motor1_Start_Flag;		// Flag syncronized with motor-clock!
extern register byte Motor2_Start_Flag;		// Flag syncronized with motor-clock!

extern register byte Motor0_PWM;		// Current PWM output duty cycle.
extern register byte Motor0_Busy;		// Status: Motor1 is running.

extern register int  Motor0_TachoPos;		// Position in tacho-counts.
extern register int  Motor0_TachoPos_End;	// End-Position in tacho-counts.


//----- MOTOR-1: ----------------------------------------------------------

extern register int  Motor1_TachoPos;		// Position in tacho-counts.
extern register int  Motor1_TachoPos_End;	// End-Position in tacho-counts.

extern register byte Motor1_PWM;		// Current PWM output duty cycle.
extern register byte Motor1_Busy;		// Status: Motor1 is running.

//----- MOTOR-2: -----------------------------------------------------------

extern register int  Motor2_TachoPos;		// Position in tacho-counts.
extern register int  Motor2_TachoPos_End;	// End-Position in tacho-counts.

extern register byte Motor2_PWM;		// Current PWM output duty cycle.
extern register byte Motor2_Busy;		// Status: Motor1 is running.


//----- KEYBOARD: ----------------------------------------------------------

extern register byte KeyBuffer;//Leagally pressed key inserted into buffer
extern register byte NewKey;   //Latest key pressed
extern register byte NextKey;  //Next pressed key
extern register byte KeyState; //State of keyboardhandler in Timer1
extern register byte LastKey;	//Leagally pressed key inserted into buffer




