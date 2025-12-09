#include "globdata.h"
//#include "motor.h"

//--------------------------------------------------------------


//***********************************************************************
//* FILE :        Motor2.c
//*               DISP-LIFT MOTOR
//***********************************************************************
//*
//	Motor2 uses the PWM2 output to set a variable voltage to a
//	VCO (voltage controlled oscillator).
//	The clock signal goes to the steppermotor-controller, and
//	to the EPA2 input to controll the position of the motor.
//*
//***********************************************************************
//*  <c196init.h>  Common includes and macros
//*  <globdata.c>  Global data declarations
//***********************************************************************


//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------

void start_m2 (byte speed, int Position, byte tacho);
void stop_motor2 (void);		// Stop motor2.

void init_motor2 (void);
void init_motor2_home_position (void);

//--------------------------------------------------------------
// LOCAL functions:
//--------------------------------------------------------------

void init_epa2(void);
void init_pwm2(void);
void epa2_interrupt(void);

//--------------------------------------------------------------
// EXTERNAL FUNCTIONS:
//--------------------------------------------------------------

extern void delay_ms (word time);


//--------------------------------------------------------------
// Local variables:
//--------------------------------------------------------------
static register byte Old_Sensor;
static register byte New_Sensor;

//--------------------------------------------------------------


//*-------------------------------------------------------------*/
//*	NAME:  stop_motor2 ()					*/
//*-------------------------------------------------------------*/

void stop_motor2 (void)		// Stop motor2.
{
   PWM2_Out (0x00);		// Speed = 0!
   Motor2_Busy = 0;		// Clear busy flag (stops epa2 actions).
   Motor2_Stop;			// Disable Motor-clk.
   Motor2_LoPower;		// Reduce motor-current!
}




/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  start_motor2 (speed, Position)		*/
/*								*/
/*--------------------------------------------------------------*/

void start_motor2 (byte speed, int Position)
{

  //--------------------------------------------------------------
  //	Init variables used by epa2_interrupt()
  //--------------------------------------------------------------

  if (speed)
  {
     Motor2_TachoPos_End = (long)((long)Position * (long)Param[0].M2_Tacho)/(long)10000L;	// End of movement in tacho-counts!

     New_Sensor = Motor2_Sensor;	// Read position sensor (P1.7).
     Old_Sensor = New_Sensor;		// Set to no change from last step.

     if (speed < 2)  speed = 2;		// Minimum speed (%).
     if (speed > 100) speed = 100;	// Maximum speed (%).

     //----------------------------------------
     // Inspite of unsigned char and ints,
     // (unsigned) must be spesified everywhere,
     // else mul and div will be signed!!
     //----------------------------------------

     Motor2_PWM =  (int)speed * (int)255 / (int)100;

     //----------------------------------------
     //	Find direction, and start the motor:
     //	(epa2_interrupt() will do the rest).
     //----------------------------------------

     if (Motor2_TachoPos < Motor2_TachoPos_End)	// Minimum 1 tacho steps.
     {
	 Motor2_Dir_1;			// Set direction +;
	 Motor2_HiPower;		// Set hi-current drive!
	 PWM2_Out (Motor2_PWM);		// Start oscillator.
	 Motor2_Busy = 3;		// Set busy flag.

//	 while(!Motor2_Clk) {}	// Wait while Clk is LOW
//	 while(Motor2_Clk) {}	// Wait while Clk is HIGH
	 Motor2_Start_Flag = 0;		// Reset flag.
	 while(!Motor2_Start_Flag) {}	// Wait while Clk is LOW
	 Motor2_Start;		// Must be syncronised with clock!?!
     }

     else if (Motor2_TachoPos > Motor2_TachoPos_End)	// Minimum 1 tacho steps.
     {
	 Motor2_Dir_0;			// Set direction -;
	 Motor2_HiPower;		// Set hi-current drive!
	 PWM2_Out (Motor2_PWM);		// Start oscillator.
	 Motor2_Busy = 3;		// Set busy flag.

//	 while(!Motor2_Clk) {}	// Wait while Clk is LOW
//	 while(Motor2_Clk) {}	// Wait while Clk is HIGH
	 Motor2_Start_Flag = 0;		// Reset flag.
	 while(!Motor2_Start_Flag) {}	// Wait while Clk is LOW
	 Motor2_Start;		// Must be syncronised with clock!!!
     }
  }
}


//*-------------------------------------------------------------*/
//*								*/
//*	NAME:  init_motor2_home_position ()			*/
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

void init_motor2_home_position (void)
{
  int M2_SensorPos;	// Sensor position in mm.

  M2_SensorPos = (long)((long)Param[0].Sensor2_TachoPos*(long)10000L)/(long)Param[0].M2_Tacho;	// SENSOR position.

  while (Motor2_Busy) {}	// Wait if motor is running!

//-------------------------------------------------------------

     if (!Motor2_Sensor)		// UP-Position?
     {
	Motor2_TachoPos = 1925;		// Assume Top-position (25mm*77).
	start_motor2 (90, M2_SensorPos-200);	// Goto Sensor + 2mm
	while (Motor2_Busy) {}
	delay_ms (50);
     }
     else				// Down-Position!
     {
	Motor2_TachoPos = -1925;		// Assume bottom-position (-25mm).
     }

     start_motor2 (90, Param[0].M2_HomePos);	// Goto HOME (Microswitch) position.
     while (Motor2_Busy) {}
//-------------------------------------------------------------

}



/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  init_motor2()					*/
/*								*/
/*--------------------------------------------------------------*/

void init_motor2(void)
{
  Motor2_Busy = 0;		// Clear Motor2 busy flag.
  Motor2_TachoPos = 0;	// Home position.

  init_epa2();		// Initialize EPA input.
  init_pwm2();		// Initialize PWM output.
}

//--------------------------------------------------------------

void init_pwm2(void)
{
// PWM2 configuration:
//  prescaler mode  = divide by 2
//  PWM output      = enabled
//  PWM duty cycle  = 0.00 %

  setbit(con_reg0, PWM_PRESCALE0);
  clrbit(p4_dir, 2);
  setbit(p4_mode, 2);

  Motor2_Dir_1;			// Set direction +;
  PWM2_Out (0x00);		// Zero pwm output.
}


void init_epa2(void)
{
 epa2_con = CAPTURE | NEG_EDGE | OVERWRITE_NEW_DATA | USE_TIMER2;
 setbit(p1_reg, 2);   /*  int reg  */
 setbit(p1_dir, 2);   /*  make input pin  */
 setbit(p1_mode, 2);   /*  select EPA mode  */

 setbit(int_mask1, EPA2_INT_BIT);    /*  un-mask epa interrupt  */
}


/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  epa2_interrupt()					*/
/*								*/
/*--------------------------------------------------------------*/
/*	-							*/
/*	This interrupt rutine starts for every tacho-puls	*/
/*								*/
/*--------------------------------------------------------------*/

#pragma interrupt(epa2_interrupt = 25)

void epa2_interrupt(void)
{
 static register word Old_Epa_Value;
 static register word New_Epa_Value;

 register word Dif_time;
 register int  Frq;
 register int  Remaining;


 //----------------------------------------------------
 //	Set Start-Flag:
 //----------------------------------------------------

  Motor2_Start_Flag = 1;	// Set start-flag.

 //----------------------------------------------------
 //	Read the captured speed time:
 //----------------------------------------------------

  Old_Epa_Value = New_Epa_Value;
  New_Epa_Value = epa2_time;   // (must read to prevent epa2-overrun).


 //****************************************************
 //	Check motor speed and positions:
 //****************************************************

  if (Motor2_Run)	// Only if a sequense is running:
  {

    //----------------------------------------------------
    //	Check home position sensor:
    //----------------------------------------------------

     Old_Sensor = New_Sensor;
     New_Sensor = Motor2_Sensor;

     if (New_Sensor != Old_Sensor)
     {
	 Motor2_TachoPos = Param[0].Sensor2_TachoPos;
     }

    //----------------------------------------------------
    //	Uppdate position counter:
    //----------------------------------------------------

     if (Motor2_Direction)	// Is Motor2 running up?
     {
	++Motor2_TachoPos;
	Remaining = Motor2_TachoPos_End - Motor2_TachoPos; 	// 3.5 = 1mm
     }
     else				// Motor2 is running down.
     {
	--Motor2_TachoPos;
	Remaining = Motor2_TachoPos - Motor2_TachoPos_End; 	// 3.5 = 1mm
     }

    //----------------------------------------------------
    //	Calculate speed (Frequency):
    //----------------------------------------------------

      Dif_time = New_Epa_Value - Old_Epa_Value;	// New speed time.
      Frq = (long)(93750L)/Dif_time;			// Frequency.


    //----------------------------------------------------
    //	If close to End:Position, reduce speed:
    //----------------------------------------------------

     if (Remaining <= 154)	// < 2mm?
     {
	if (Remaining <= 77)	// < 1mm?
	{
	   Motor2_Busy = 1;	// Reduce busy flag (2->1).
	   if (Motor2_PWM > 25)
	       Motor2_PWM = 25;	// Slow down to 10% speed.

	}
	else
	{
	   Motor2_Busy = 2;	// Reduce busy flag (3->2).
	   if (Motor2_PWM > 128)
	       Motor2_PWM = 50;	// Slow down to 20% speed.

	}
	PWM2_Out(Motor2_PWM);	// Output (new) PWM-value!
     }

   //----------------------------------------------------
   //	Check end position:
   //----------------------------------------------------

     if (Remaining <= 0)	// End-position?
     {
	 Motor2_Busy = 0;	// Clear busy flag.
	 Motor2_Stop;		// Disable Motor Clk-input!
	 Motor2_LoPower;	// Reduce motor-current!
	 Motor2_PWM = 1;	// Set clk-speed to 0!
	 PWM2_Out(Motor2_PWM);	// Output (new) PWM-value!
     }

   //----------------------------------------------------


 //----------------------------------------------------
   }	// End if (Motor2_Run).
 //----------------------------------------------------

}

