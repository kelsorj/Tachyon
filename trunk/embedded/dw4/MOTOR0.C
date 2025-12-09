#include "globdata.h"


//**********************************************************************
// FILE :      Motor0.c
//             HORISONTAL MOTOR (Carriage) rutines.
//**********************************************************************
//
//	Motor0 uses the PWM0 output to set a variable voltage to a
//	VCO (voltage controlled oscillator).
//	The PWM signal is first filtered trough a 2. order
//	analog filter.
//	The VCO clock signal goes to the steppermotor-controller, and
//	also to the EPA0-input, to count the position of the motor.
//
// Interrupt execution times:
// --------------------------
//	epa0_interrupt() execution time:	ca 10uS - 50uS (close to End-pos).
//      Repetion rate at full motor speed:	4.5kHz (0.222mS) (22nF in VCO).
//
//***********************************************************************

//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------

void start_motor0 (byte speed, long Position);

void stop_motor0 (void);		// Stop motor0 imideately.
void soft_stop_motor0 (void);		// Stop motor0 (de-accelerate).

void init_motor0 (void);
void init_motor0_home_position (void);

//--------------------------------------------------------------
// LOCAL functions:
//--------------------------------------------------------------

void init_epa0(void);
void init_pwm0(void);
void epa0_interrupt(void);

//--------------------------------------------------------------
// EXTERNAL FUNCTIONS:
//--------------------------------------------------------------

extern void delay_ms (word time);


//--------------------------------------------------------------
// Local variables:
//--------------------------------------------------------------
static byte Sensor0_Ok;        // Position-sensor flag.
static register byte Old_Sensor;
static register byte New_Sensor;

//--------------------------------------------------------------

void motor0_test (void)
{

//---------------------------
  start_motor0 (30, 36000);
  while(Motor0_Busy)
  {}
  delay_ms(500);

  start_motor0 (90, 0);
  while(Motor0_Busy)
  {}
  delay_ms(500);
//---------------------------
  start_motor0 (30, 18000);
  while(Motor0_Busy)
  {}
  delay_ms(500);

  start_motor0 (90, 0);
  while(Motor0_Busy)
  {}
  delay_ms(500);
//---------------------------
  start_motor0 (30, 9000);
  while(Motor0_Busy)
  {}
  delay_ms(500);

  start_motor0 (90, 0);
  while(Motor0_Busy)
  {}
  delay_ms(500);
//---------------------------
  start_motor0 (30, 4500);
  while(Motor0_Busy)
  {}
  delay_ms(500);

  start_motor0 (90, 0);
  while(Motor0_Busy)
  {}
  delay_ms(500);
//---------------------------
  start_motor0 (30, 2250);
  while(Motor0_Busy)
  {}
  delay_ms(500);

  start_motor0 (90, 0);
  while(Motor0_Busy)
  {}
  delay_ms(500);
//---------------------------
}

//*-------------------------------------------------------------*/
//*	NAME:  stop_motor0 ()					*/
//*-------------------------------------------------------------*/

void stop_motor0 (void)		// Stop motor0 imideately.
{
   PWM0_Out (0x00);		// Speed = 0!
   Motor0_Busy = 0;		// Clear busy flag.
   Motor0_Stop;			// Disable Motor-clk.
   Motor0_LoPower;		// Reduce motor-current!
}

void soft_stop_motor0 (void)	// Stop motor0 (de-accelerate).
{
   PWM0_Out (0x00);		// Speed = 0!
   if (Motor0_Direction)
      Motor0_TachoPos_End = Motor0_TachoPos + 310;	// 314/17.1 = 18mm!
   else
      Motor0_TachoPos_End = Motor0_TachoPos - 310;	// 314/17.1 = 18mm!
}

//*--------------------------------------------------------------*/
//*								*/
//*	NAME:  start_motor0 (speed, Position)		*/
//*								*/
//*--------------------------------------------------------------*/

void start_motor0 (byte speed, long Position)
{

  //--------------------------------------------------------------
  //	Init variables used by epa0_interrupt()
  //--------------------------------------------------------------

  if (speed)
  {
     Motor0_TachoPos_End = (long)((long)Position * (long)Param[0].M0_Tacho)/(long)10000L;	// End of movement in tacho-counts!

     New_Sensor = Motor0_Sensor;	// Read position sensor (P1.7).
     Old_Sensor = New_Sensor;		// Set to no change from last step.

     //----------------------------------------
     // Calculate Speed:
     //----------------------------------------

     if (speed > 100) speed = 100;	// Maximum speed (%).
     if (speed > 1)
	Motor0_PWM =  (int)speed * (int)255 / (int)100;
     else
	Motor0_PWM = 1;		// Minimum speed (%).


     //----------------------------------------
     //	Find direction, and start the motor:
     //	(epa0_interrupt() will do the rest).
     //----------------------------------------

     if (Motor0_TachoPos < Motor0_TachoPos_End)	// Minimum 1 tacho steps.
     {
	 Motor0_Dir_1;			// Set direction +;
	 Motor0_HiPower;		// Set hi-current drive!
	 PWM0_Out (Motor0_PWM);		// Start oscillator.
	 Motor0_Busy = 3;		// Set busy flag.

	 Motor0_Start_Flag = 0;		// Reset flag.
	 while(!Motor0_Start_Flag) {}	// Wait while Clk is LOW
	 Motor0_Start;		// Start on falling edge clock!
     }

     else if (Motor0_TachoPos > Motor0_TachoPos_End)	// Minimum 1 tacho steps.
     {
	 Motor0_Dir_0;			// Set direction -;
	 Motor0_HiPower;		// Set hi-current drive!
	 PWM0_Out (Motor0_PWM);		// Start oscillator.
	 Motor0_Busy = 3;		// Set busy flag.

	 Motor0_Start_Flag = 0;		// Reset flag.
	 while(!Motor0_Start_Flag) {}	// Wait while Clk is LOW
	 Motor0_Start;		// Must be suncronised with clock!!!
     }
  }
}


//*-------------------------------------------------------------*/
//*								*/
//*	NAME:  init_motor0_home_position ()			*/
//*								*/
//*-------------------------------------------------------------*/
//*
//*	This rutine checks if the current position is under or
//*     over the position sensor. The motor_position counter is
//*     then loaded with a upper or lower position number, and
//*     the start rutine is called to run the lift to the
//*     position sensor, and then to the HOME position.
//*
//*     Note:
//*     This rutine will wait for the lift to find the HOME
//*     position!
//*
//*-------------------------------------------------------------*/

void init_motor0_home_position (void)
{
  int M0_SensorPos;	// Sensor position in (1/100)mm.
  int M0_MaxTachoPos;	// Sensor position in TachoCounts.

  M0_SensorPos = (long)((long)Param[0].Sensor0_TachoPos*(long)10000L)/(long)Param[0].M0_Tacho;	// SENSOR position.


  //-------------------------------------------------------------
  //- Check position (left or right side of sensor):
  //-------------------------------------------------------------

     while (Motor0_Busy) {}	// Wait if motor is running!
     Sensor0_Ok = 0;            // Clear sensor-flag.

     if (Motor0_TachoPos < Param[0].Sensor0_TachoPos)	// Left side of sensor?
     {
	start_motor0 (80, M0_SensorPos+1100);		// Goto Sensor + 11.00mm
	while (Motor0_Busy) {}

	if (Sensor0_Ok == 0)
	   Motor0_TachoPos = 8725;		// Error: Assume 50cm right position.

	delay_ms (20);

	start_motor0 (90, Param[0].M0_HomePos);		// Goto HOME (Microswitch) position.
	while (Motor0_Busy) {}

     }
     else	// Right side of sensor:
     {
	start_motor0 (90, Param[0].M0_HomePos);		// Goto HOME (Microswitch) position.
	while (Motor0_Busy) {}

	if (Sensor0_Ok == 0)
	{
	   start_motor0 (80, M0_SensorPos+1100);		// Goto Sensor + 11.00mm
	   while (Motor0_Busy) {}

	   delay_ms (20);

	   start_motor0 (90, Param[0].M0_HomePos);		// Goto HOME (Microswitch) position.
	   while (Motor0_Busy) {}
	}
     }

}




/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  init_motor0()					*/
/*								*/
/*--------------------------------------------------------------*/

void init_motor0(void)
{
  Motor0_Busy = 0;	// Clear Motor0 busy flag.
  Motor0_TachoPos = 0;	// Home position.
  Sensor0_Ok = 0;            // Clear sensor-flag.

  init_epa0();		// Initialize EPA input.
  init_pwm0();		// Initialize PWM output.
}

//---------------------------------------------------

void init_pwm0(void)
{
// PWM0 configuration:
//  prescaler mode  = divide by 2
//  PWM output      = enabled
//  PWM duty cycle  = 0.00 %

  setbit(con_reg0, PWM_PRESCALE0);
  clrbit(p4_dir, 0);
  setbit(p4_mode, 0);

  Motor0_Dir_1;			// Set direction +;
  PWM0_Out (0x00);		// Zero pwm output.
}

//---------------------------------------------------

void init_epa0(void)
{
// epa0_con = CAPTURE | POS_EDGE | OVERWRITE_NEW_DATA | USE_TIMER2;
 epa0_con = CAPTURE | NEG_EDGE | OVERWRITE_NEW_DATA | USE_TIMER2;
 setbit(p1_reg, 0);   /*  int reg  */
 setbit(p1_dir, 0);   /*  make input pin  */
 setbit(p1_mode, 0);   /*  select EPA mode  */

 setbit(int_mask, EPA0_INT_BIT);    /*  un-mask epa interrupt  */
}


/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  epa0_interrupt()					*/
/*								*/
/*--------------------------------------------------------------*/
/*	This interrupt rutine starts for every clock-puls	*/
/*	from motor0-VCO (oscillator controlled by PWM0).	*/
/*								*/
/*	Be aware of high repetition rate at high motor-speed!	*/
/*	(up to 4.5kHz: every 222uS)				*/
/*--------------------------------------------------------------*/

#pragma interrupt(epa0_interrupt = 7)

void epa0_interrupt(void)
{
 static register word New_Epa_Value;
 static register word Old_Epa_Value;
 register int  Frq;
 register int  Remaining;
 register int  Tmp;

 //----------------------------------------------------
 //	Set Start-Flag:
 //----------------------------------------------------

  Motor0_Start_Flag = 1;	// Set start-flag.

 //----------------------------------------------------
 //	Read the captured speed time:
 //----------------------------------------------------

  Old_Epa_Value = New_Epa_Value;
  New_Epa_Value = epa0_time;   // (must read to prevent epa0-overrun).


 //****************************************************
 //	Check motor speed and positions:
 //****************************************************

  if (Motor0_Run)	// Only if motor is running:
  {

    //----------------------------------------------------
    //	Check home position sensor:
    //	Check Plate-detector sensor:
    //----------------------------------------------------

     Old_Sensor = New_Sensor;
     New_Sensor = Motor0_Sensor;

     if (New_Sensor)
	Sensor0_Ok = 1;        // Set sensor-flag.

     if (New_Sensor != Old_Sensor)
     {
	if (!New_Sensor && Motor0_Direction)		// Is motor running down?
	{
	   Motor0_TachoPos = Param[0].Sensor0_TachoPos;	// Set calibrated position!

	   if (!Plate_Sensor)
	      PlateAlarm = 1;	// Set alarm-flag!
	}
	else if (New_Sensor && !Motor0_Direction)	// Is motor running up?
	{
	   Motor0_TachoPos = Param[0].Sensor0_TachoPos;	// Set calibrated position!
	}

#ifdef __TestBoard
	PlateAlarm = 0;
#endif
     }


    //----------------------------------------------------
    //	Uppdate position counter:
    //----------------------------------------------------

     if (Motor0_Direction)	// Is Motor0 running up?
     {
	++Motor0_TachoPos;
	Remaining = Motor0_TachoPos_End - Motor0_TachoPos; 	// 3.5 = 1mm
     }
     else				// Motor0 is running down.
     {
	--Motor0_TachoPos;
	Remaining = Motor0_TachoPos - Motor0_TachoPos_End; 	// 3.5 = 1mm
     }


    //----------------------------------------------------
    //	If close to End-Position, reduce speed:
    //----------------------------------------------------
    // (Reduce speed linearly to remaining distance).
    //----------------------------------------------------

     if (Remaining <= 290)	// < 17mm?
     {
	Frq = 6177/(New_Epa_Value - Old_Epa_Value);	// Speed (Max 4400Hz = 290) (21,3 x 290)

	if (Frq > Remaining)		// Actual speed higher than BrakeSpeed?
	   Tmp = 0;			// Yes: set rapid speed reduction!
	else
	   Tmp = Remaining+5;		// No: set to current BrakeSpeed+!

      //--------------------------------
	if (Frq < 14)			// Speed < 200Hz? (very slow)
	   if (Tmp < 12)		//   and output < 200Hz)
	      Tmp = 12;			//   - set output = 200Hz!
      //--------------------------------
	if      (Tmp > 255) Tmp = 255;	// max limit
	else if (Tmp < 0)   Tmp = 0;	// min limit
      //--------------------------------
	if (Tmp > Motor0_PWM)		// Never higher than WantedSpeed!
	    Tmp = Motor0_PWM;
      //--------------------------------


	//----------------------------------------------------
	//  Check end position:
	//----------------------------------------------------
	if (Remaining <= 0)	// End-position?
	{
	   Tmp = 2;
	   Motor0_PWM = 2;	// Set clk-speed to 0! (allmost 0)
	   Motor0_Busy = 0;	// Clear busy flag.
	   Motor0_Stop;		// Disable Motor Clk-input!
	   Motor0_LoPower;	// Reduce motor-current!
	}

	PWM0_Out(Tmp);	// Output (new) PWM-value!
     }


 //----------------------------------------------------
   }	// End if (Motor0_Run).
 //----------------------------------------------------

}

