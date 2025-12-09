#include "globdata.h"	// Definitions

//#include "motor.h"



//***********************************************************************
//* FILE :        Motor1.c
//***********************************************************************
//*
//*               ASP-LIFT MOTOR.
//*               ---------------
//	Motor1 uses the PWM1 output to set a variable voltage to a
//	VCO (voltage controlled oscillator).
//	The clock signal goes to the steppermotor-controller, and
//	to the EPA1 input to controll the position of the motor.
//*
//***********************************************************************

//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------

void start_motor1 (byte speed, int Position);
void stop_motor1 (void);		// Stop motor1 imideately.
void init_motor1 (void);
void init_motor1_home_position (void);
int  calibrate_motor_positions (void);

//--------------------------------------------------------------
// LOCAL functions:
//--------------------------------------------------------------

void init_epa1(void);
void init_pwm1(void);
void epa1_interrupt(void);

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


//-------------------------------------------------------------*/
//	NAME:  stop_motor1 ()					*/
//-------------------------------------------------------------*/

void stop_motor1 (void)		// Stop motor1.
{
   PWM1_Out (0x00);		// Speed = 0!
   Motor1_Busy = 0;		// Clear busy flag (stops epa1 actions).
   Motor1_Stop;			// Disable Motor-clk.
   Motor1_LoPower;		// Reduce motor-current!
}



//*-------------------------------------------------------------*/
//*	NAME:  calibrate_motor_positions ()			*/
//*-------------------------------------------------------------*/

int  calibrate_motor_positions (void)
{
  return (1);
}


//--------------------------------------------------------------*/
//								*/
//	NAME:  start_motor1 (speed, Position)		*/
//								*/
//--------------------------------------------------------------*/

void start_motor1 (byte speed, int Position)
{

  //--------------------------------------------------------------
  //	Init variables used by epa1_interrupt()
  //--------------------------------------------------------------

  if (speed)
  {
     Motor1_TachoPos_End = (long)((long)Position * (long)Param[0].M1_Tacho)/(long)10000L;	// End of movement in tacho-counts!

     New_Sensor = Motor1_Sensor;	// Read position sensor (P1.7).
     Old_Sensor = New_Sensor;		// Set to no change from last step.

     if (speed < 2)  speed = 2;		// Minimum speed (%).
     if (speed > 100) speed = 100;	// Maximum speed (%).

     //----------------------------------------
     // Inspite of unsigned char and ints,
     // (unsigned) must be spesified everywhere,
     // else mul and div will be signed!!
     //----------------------------------------

     Motor1_PWM =  (int)speed * (int)255 / (int)100;


     //----------------------------------------
     //	Find direction, and start the motor:
     //	(epa1_interrupt() will do the rest).
     //----------------------------------------

     if (Motor1_TachoPos < Motor1_TachoPos_End)	// Minimum 1 tacho steps.
     {
	 Motor1_Dir_1;			// Set direction +;
	 Motor1_HiPower;		// Set hi-current drive!
	 PWM1_Out (Motor1_PWM);		// Start oscillator.
	 Motor1_Busy = 3;		// Set busy flag.

//	 while(!Motor1_Clk) {}	// Wait while Clk is LOW
//	 while(Motor1_Clk) {}	// Wait while Clk is HIGH
	 Motor1_Start_Flag = 0;		// Reset flag.
	 while(!Motor1_Start_Flag) {}	// Wait while Clk is LOW
	 Motor1_Start;		// Must be syncronised with clock!?!
     }

     else if (Motor1_TachoPos > Motor1_TachoPos_End)	// Minimum 1 tacho steps.
     {
	 Motor1_Dir_0;			// Set direction -;
	 Motor1_HiPower;		// Set hi-current drive!
	 PWM1_Out (Motor1_PWM);		// Start oscillator.
	 Motor1_Busy = 3;		// Set busy flag.

//	 while(!Motor1_Clk) {}	// Wait while Clk is LOW
//	 while(Motor1_Clk) {}	// Wait while Clk is HIGH
	 Motor1_Start_Flag = 0;		// Reset flag.
	 while(!Motor1_Start_Flag) {}	// Wait while Clk is LOW
	 Motor1_Start;		// Must be syncronised with clock!!!
     }
  }
}



//*-------------------------------------------------------------*/
//*								*/
//*	NAME:  init_motor1_home_position ()			*/
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

void init_motor1_home_position (void)
{
  int M1_SensorPos;	// Sensor position in mm.

  M1_SensorPos = (long)((long)Param[0].Sensor1_TachoPos*(long)10000L)/(long)Param[0].M1_Tacho;	// SENSOR position.

  while (Motor1_Busy) {}	// Wait if motor is running!

//-------------------------------------------------------------

     if (!Motor1_Sensor)	// UP-Position?
     {
	Motor1_TachoPos = 1925;		// Assume Top-position (25mm*77).
	start_motor1 (90, M1_SensorPos-200);	// Goto Sensor + 2mm
	while (Motor1_Busy) {}
	delay_ms (50);
     }
     else			// Down-Position!
     {
	Motor1_TachoPos = -1925;		// Assume bottom-position (-25mm).
     }

     start_motor1 (90, Param[0].M1_HomePos);		// Goto HOME (Microswitch) position.
     while (Motor1_Busy) {}
//-------------------------------------------------------------
}



/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  init_motor1()					*/
/*								*/
/*--------------------------------------------------------------*/

void init_motor1(void)
{
  Motor1_Busy = 0;		// Clear Motor1 busy flag.
  Motor1_TachoPos = 0;	// Home position.

  init_epa1();		// Initialize EPA input.
  init_pwm1();		// Initialize PWM output.
}


//--------------------------------------------------------------
//	NAME:  init_pwm1 ()					*/
//--------------------------------------------------------------

void init_pwm1(void)
{
// PWM1 configuration:
//  prescaler mode  = divide by 2
//  PWM output      = enabled
//  PWM duty cycle  = 0.00 %

  setbit(con_reg0, PWM_PRESCALE0);
  clrbit(p4_dir, 1);
  setbit(p4_mode, 1);

  Motor1_Dir_1;			// Set direction +;
  PWM1_Out (0x00);		// Zero pwm output.
}


//--------------------------------------------------------------
//	NAME:  init_epa1 ()					*/
//--------------------------------------------------------------

void init_epa1(void)
{
 epa1_con = CAPTURE | NEG_EDGE | OVERWRITE_NEW_DATA | USE_TIMER2;
 setbit(p1_reg, 1);   /*  int reg  */
 setbit(p1_dir, 1);   /*  make input pin  */
 setbit(p1_mode, 1);   /*  select EPA mode  */

 setbit(int_mask1, EPA1_INT_BIT);    /*  un-mask epa interrupt  */
}


/*--------------------------------------------------------------*/
/*								*/
/*	NAME:  epa1_interrupt()					*/
/*								*/
/*--------------------------------------------------------------*/
/*	-							*/
/*	This interrupt rutine starts for every tacho-puls	*/
/*	to motor1. 						*/
/*	-							*/
/*	Another rutine has to set the motor1 direction bit,	*/
/*	the busy flag, and the PWM output signal.		*/
/*	-							*/
/*--------------------------------------------------------------*/

#pragma interrupt(epa1_interrupt = 24)

void epa1_interrupt(void)
{
 static register word Old_Epa_Value;
 static register word New_Epa_Value;

 register word Dif_time;
 register int  Frq;
 register int  Remaining;


 //----------------------------------------------------
 //	Set Start-Flag:
 //----------------------------------------------------

  Motor1_Start_Flag = 1;	// Set start-flag.

 //----------------------------------------------------
 //	Read the captured speed time:
 //----------------------------------------------------

  Old_Epa_Value = New_Epa_Value;
  New_Epa_Value = epa1_time;   // (must read to prevent epa1-overrun).


 //****************************************************
 //	Check motor speed and positions:
 //****************************************************

  if (Motor1_Run)	// Only if a sequense is running:
  {

    //----------------------------------------------------
    //	Check home position sensor:
    //----------------------------------------------------

     Old_Sensor = New_Sensor;
     New_Sensor = Motor1_Sensor;

     if (New_Sensor != Old_Sensor)
     {
	 Motor1_TachoPos = Param[0].Sensor1_TachoPos;
     }

    //----------------------------------------------------
    //	Uppdate position counter:
    //----------------------------------------------------

     if (Motor1_Direction)	// Is Motor1 running up?
     {
	++Motor1_TachoPos;
	Remaining = Motor1_TachoPos_End - Motor1_TachoPos; 	// 3.5 = 1mm
     }
     else				// Motor1 is running down.
     {
	--Motor1_TachoPos;
	Remaining = Motor1_TachoPos - Motor1_TachoPos_End; 	// 3.5 = 1mm
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
	   Motor1_Busy = 1;	// Reduce busy flag (2->1).
	   if (Motor1_PWM > 25)
	       Motor1_PWM = 25;	// Slow down to 10% speed.

	}
	else
	{
	   Motor1_Busy = 2;	// Reduce busy flag (3->2).
	   if (Motor1_PWM > 128)
	       Motor1_PWM = 50;	// Slow down to 20% speed.

	}
	PWM1_Out(Motor1_PWM);	// Output (new) PWM-value!
     }

   //----------------------------------------------------
   //	Check end position:
   //----------------------------------------------------

     if (Remaining <= 0)	// End-position?
     {
	Motor1_Busy = 0;	// Clear busy flag.
	Motor1_Stop;		// Disable Motor Clk-input!
	Motor1_LoPower;		// Reduce motor-current!
	Motor1_PWM = 1;		// Set clk-speed to 0!
	PWM1_Out(Motor1_PWM);	// Output (new) PWM-value!
     }

   //----------------------------------------------------


 //----------------------------------------------------
   }	// End if (Motor1_Run).
 //----------------------------------------------------

}

