//***********************************************************************
//*  <c196init.h>  Common includes and macros
//*  <globdata.c>  Global data declarations
//***********************************************************************
#include  <stdio.h>
#include  <stdlib.h>

#include "globdata.h"	// Global data definitions
#include "keybrd.h"
#include "display.h"
#include "comm.h"


//-----------------------------------------------------------------------
//----- GLOBAL FUNCTIONS --------------------------------------------------
//-----------------------------------------------------------------------

void init_all (void);			// Init hardware and funtion parameters.
void calibrate_positions (void);	// Calibration of motor positions.
byte uppdate_alarms (void);		// Read and uppdate Alarms!
void copy_CParam_to_Param (void);	// Read default parameters from FlashProm

//-----------------------------------------------------------------------
//----- EXTERNAL FUNCTIONS --------------------------------------------------
//-----------------------------------------------------------------------

extern void init_timer1 (void);
extern void init_timer2 (void);

extern void init_motor0 (void);
extern void init_motor1 (void);
extern void init_motor2 (void);

extern void init_key_board();

extern void init_serial();	 // Initialize RS-232.
extern void init_comm();	 // Init command interpreter for RS232 commands
extern void init_pressure (void);



//**************************************************************************
//  copy_CParam_to_Param
//**************************************************************************

void copy_CParam_to_Param (void)
{
  far const byte *ptr1;
  byte *ptr2;
  int t;

  ptr1 = (far const byte*)&CParam[0];	// Flash-data.
  ptr2 = (byte*)&Param[0];		// User-data.

  for (t=0; t<256; t++)
  {
     *(ptr2+t) = *(ptr1+t);
  }
}


/**************************************************************************
*  FUNCTION    : init_all
*  METHODE     : Initialize all except for init done in CStartup
*  AUTHOR      : HJ 96-08
*  L. MODFIED  :
***************************************************************************/
void init_all (void)
{

  pwm0_control = 0;	// Speed = 0!
  pwm1_control = 0;	// Speed = 0!
  pwm2_control = 0;	// Speed = 0!
  clr_beep;		// Beeper-output = 0.

  OutPorts_Disable;	// Set all Out-Ports to tri-state.
  Port0 = 0;		// Clear OutPort register!
  Port1 = 0;		// Clear OutPort register!
  Port2 = 0;		// Clear OutPort register!
  Port3 = 0;		// Clear OutPort register!
  Port4 = 80;		// Clear OutPort register! (Not motor Reset!)
  Out_Port0 = Port0;	// Clear Out-Port!
  Out_Port1 = Port1;	// Clear Out-Port!
  Out_Port2 = Port2;	// Clear Out-Port!
  Out_Port3 = Port3;	// Clear Out-Port!
  Out_Port4 = Port4;	// Clear Out-Port!
  OutPorts_Enable;	// Enable all Out-Ports.

  ExtCommand = 0;
  ExtComVal = 0;
  ExtStart = 0;

  //Timers
  init_timer1 ();	// User-timer.
  init_timer2 ();	// Init before motor1 and motor2.

  //Motors
  init_motor1 ();	// Variables, PWM1, EPA1.
  init_motor2 ();	// Variables, PWM2, EPA2.
  init_motor0 ();	// Variables, PWM0, EPA0.
  Motor0_Stop;		// Disable Motor-clk.
  Motor1_Stop;		// Disable Motor-clk.
  Motor2_Stop;		// Disable Motor-clk.

  init_comm();    // Init command interpreter for RS232 commands
  init_serial();	// RS232 = 9600,N,8,1.
  init_key_board();
  init_pressure ();

  //Interrupts
  int_pend  = 0;	// clear pending interrupts!
  int_pend1 = 0;	// clear pending interrupts!
  enable ();		// Globally enable interrupts.

  Motor_Reset_off;	// Remove Reset signal to all 3 Stepper-Motor IC.

  //State machine in main program
  MainState = 0;	// Init main state.
  RunningState = 0;	// No action do be done.
  PrgmState = 0;	// Init-state of program to be run.


  //Alarms
  PlateAlarm   = 0;	// Alarm-Flag
  Buffer1Alarm = 0;	// Alarm-Flag
  Buffer2Alarm = 0;	// Alarm-Flag
  Buffer3Alarm = 0;	// Alarm-Flag
  Buffer4Alarm = 0;	// Alarm-Flag
  WasteAlarm   = 0;	// Alarm-Flag
  PressureAlarm = 0;	// Low/High Pressure Alarm (Low Pressure Timeout).
  PressureOk = 0;	// Low/High Pressure.

}




//**************************************************************************
//*  FUNCTION    : calibrate_positions
//**************************************************************************

void calibrate_positions (void)
{
// (code to be implemented)
}



//**************************************************************************
//*  FUNCTION    : uppdate_alarms
//**************************************************************************

byte uppdate_alarms (void)
{
  byte AlarmByte;

   //----------------------------------------------------
   // In_Port0:		// 8 bits SENSOR SIGNAL input port.
   //----------------------------------------------------
   //  Bit 0 = Buffer1 Alarm
   //  Bit 1 = Buffer2 Alarm
   //  Bit 2 = Buffer3 Alarm
   //  Bit 3 = Buffer4 Alarm
   //  Bit 4 = Waste Alarm
   //  Bit 5 = RinseHead Mounted
   //  Bit 6 = Vacuum Sensor
   //  Bit 7 = Aux Input
   //----------------------------------------------------
   // AlarmByte bits:
   //----------------------------------------------------
   //  BUFFER1_ALARM_BIT    0	// Sensor_In_Port bit 0
   //  BUFFER2_ALARM_BIT    1	// Sensor_In_Port bit 2
   //  BUFFER3_ALARM_BIT    2	// Sensor_In_Port bit 0
   //  BUFFER4_ALARM_BIT    3	// Sensor_In_Port bit 2
   //  WASTE_ALARM_BIT	    4	// Sensor_In_Port bit 4
   //  PLATE_ALARM_BIT	    5	// Sensor_In_Port bit 5
   //  PRESSURE_ALARM_BIT   6	// Sensor_In_Port bit 3
   //  PRESSURE_LOW_BIT	    7	// Sensor_In_Port bit 1
   //----------------------------------------------------

    AlarmByte = In_Port0;		// Get alarm-port

    Buffer1Alarm = checkbit(AlarmByte, BUFFER1_ALARM_BIT);	// If alarm (Buffer empty)
    Buffer2Alarm = checkbit(AlarmByte, BUFFER2_ALARM_BIT);	// If alarm (Buffer empty)
    Buffer3Alarm = checkbit(AlarmByte, BUFFER3_ALARM_BIT);	// If alarm (Buffer empty)
    Buffer4Alarm = checkbit(AlarmByte, BUFFER4_ALARM_BIT);	// If alarm (Buffer empty)
    WasteAlarm   = checkbit(AlarmByte, WASTE_ALARM_BIT);	// If alarm (Waste full)

//-- Microplate-sensor: -------------------------


//----------------------------------------------------
//- Uppdate Alarm-bits in AlarmByte:
//----------------------------------------------------

    if (PlateAlarm)	 setbit(AlarmByte, 5);	// Set or reset alarm-bit.
    if (PressureAlarm)	 setbit(AlarmByte, 6);	// Set or reset alarm-bit.
    if (!PressureOk)	 setbit(AlarmByte, 7);	// Set or reset alarm-bit.

  return (AlarmByte);
}



