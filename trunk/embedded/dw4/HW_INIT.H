#ifndef __HWINIT_H
#define __HWINIT_H

//**************************************************************
// hw_init.h		(PCB rev. 2.0)
//**************************************************************

//---------------------------
#define IDLE_PRESSURE    0	// 0.000 bar
#define NORM_PRESSURE    550	// 0.550 bar
#define PRESSURE_TIMEOUT 600	// 600s = 10 min
//-----------------------------------------------------------------
// 1.0Bar = 48.3mV (@5v) => 4.833v (4096 / 5 * 4.833 = 3960)
//-----------------------------------------------------------------
#define PRESSURE_CAL    3960L	// 1 bar=3960  (3960=4.833v)
#define PRESSURE_OFS    240	// (1.6k/100)  (241=294mV)
//-----------------------------------------------------------------
//- M0 Carriage Motor, 1/100mm ------------------------------------
//-----------------------------------------------------------------
#define M0_HOMEPOS	0		//   0.00mm Carriage HomePos
#define DISP1_POS	12200		// 104.50mm Head1 Pos (to Plate Edge).
#define DISP2_POS	DISP1_POS +4407	// +44.00mm Head2 Pos (to Plate Edge).
#define DISP3_POS	DISP2_POS +4407	// +44.00mm Head3 Pos (to Plate Edge).
#define DISP4_POS	DISP3_POS +4407	// +44.00mm Head4 Pos (to Plate Edge).
#define ASP_POS		29200		// +38.00mm Head5 Pos (to Plate Edge).
//---------------------------
#define RowColDiff  	1220	// Row-Col position difference (12,20mm)
//-----------------------------------------------------------------
//- M1 AspLift, M2 DispLift, 1/100mm ------------------------------
//-----------------------------------------------------------------
#define M1_HOMEPOS	1800	// 18.00mm AspLift HomePos
#define M2_HOMEPOS	1800	// 18.00mm DispLift HomePos
//---------------------------
#define DISP_AIR_GAP	100	// 1.0mm DispHead AirGap (from PlateTop)
#define ASP_AIR_GAP	200	// 2.0mm Asp-Head AirGap (from PlateTop)
//---------------------------
#define DISP_DELAY1  	150	// 100-500mS delay from last dispence
//--------------------------------------------------------------
#define PL_TOP_1	1450	// 96-Top of plate (1/100 mm).
#define PL_DEPTH_1	1140	// Depth of Well from PlateTop (1/100 mm).
#define PL_OFFSET_1	1140	// Distance from PlateEdge to center of first Well (1/100 mm)..
#define WELL_WELL_1	9000L	// Distance between Wells (1/1000mm),(2250=2.250mm, 4500=4.500mm, 9000=9.000mm)
//---------------------------
#define PL_TOP_2	1440	// 384-Top of plate
#define PL_DEPTH_2	1160	// Depth of Well from PlateTop (1/10 mm).
#define PL_OFFSET_2	900	// Distance from PlateEdge to center of first Well (1/10 mm)..
#define WELL_WELL_2	4500L	// Distance between Wells (1/1000mm),(2250=2.250mm, 4500=4.500mm, 9000=9.000mm)
//---------------------------
#define PL_TOP_3	740	// 1536-Top of plate
#define PL_DEPTH_3	500	// Depth of Well from PlateTop (1/10 mm).
#define PL_OFFSET_3	780	// Distance from PlateEdge to center of first Well (1/10 mm)..
#define WELL_WELL_3	2250L	// Distance between Wells (1/1000mm),(2250=2.250mm, 4500=4.500mm, 9000=9.000mm)
//--------------------------------------------------------------
#define  Cal1Size     1000	// 10.00 mm = calibration block thicknes
#define  Cal2Size     1000	// 10.00 mm = calibration block thicknes
#define  Cal3Size     1000	// 10.00 mm = calibration block thicknes
//--------------------------------------------------------------


//--------------------------------------------------------------
// FirmWare Version:
//--------------------------------------------------------------
// V4.0: (6/10 -03) 1/100mm all motors, LiqPressure -> LiqFactor.
//--------------------------------------------------------------
//#define __TestBoard  	1	// In case of CPU-board only!
//#define __SetHeadType	1	// In case of Prototype wihout Head-decode!
//#define __VER_3  	1	// Select PCB Version 3 (old) or 4 (new) (DispHeadSwitches)


#define VerCode	  0x23			// V1.0
#define VerTxt	 "AquaMax DW4-RC V2.3\0"

//--------------------------------------------------------------
// Device Version Codes:
//--------------------------------------------------------------

#define  DevCode1	 0x13		// 13=AquaMax DW4 (12395)
#define  DevCode2	 0x10		// 10=Washer
#define  Serial_No_Txt  "12395-0000\0"	// Serial Number[32]	(Blokk 0)
#define  DISP1_CAL	 94		// DispenseCal 1	(int x 1)
#define  DISP2_CAL	 94		// DispenseCal 2	(int x 1)
#define  DISP_CORR	 28		// (28 = +2,8mS) Dispense Valve Unlinearity Correction


//--------------------------------------------------------------
// sensor inputs:
//--------------------------------------------------------------

// checkbit(In_Port0, xx_BIT);

#define	BUFFER1_ALARM_BIT	0	// In_Port0 bit 0
#define	BUFFER2_ALARM_BIT	1	// In_Port0 bit 2
#define	BUFFER3_ALARM_BIT	2	// In_Port0 bit 2
#define	BUFFER4_ALARM_BIT	3	// In_Port0 bit 2
#define	WASTE_ALARM_BIT		4	// In_Port0 bit 2
#define	RINSE_HEAD_BIT		5	// In_Port0 bit 4
#define	AUX_INPUT_BIT		6	// In_Port0 bit 6
#define	PLATE_ERROR_BIT		7	// In_Port0 bit 7

#define	PLATE_ALARM_BIT		5	// Alarm-bits
#define	PRESSURE_ALARM_BIT	6	// Alarm-bits
#define	PRESSURE_LOW_BIT	7	// Alarm-bits

// checkbit(In_Port1, xx_BIT);

#define	KEY1_BIT		0	// In_Port1 bit 0
#define	KEY2_BIT		1	// In_Port1 bit 1
#define	KEY3_BIT		2	// In_Port1 bit 2
#define	KEY4_BIT		3	// In_Port1 bit 3
#define	KEY5_BIT		4	// In_Port1 bit 4
#define	KEY6_BIT		5	// In_Port1 bit 5
#define	DISP_HEAD5_BIT		6	// In_Port1 bit 6
#define	DISP_HEAD6_BIT		7	// In_Port1 bit 7

// checkbit(p1_pin, MOTOR1_SENSOR_BIT);

#define	 M0_POS_SENS_BIT	6	// P1.3
#define	 M1_POS_SENS_BIT	4	// P1.4
#define	 M2_POS_SENS_BIT	5	// P1.5
#define	 PLATE_SENS_BIT		3	// P1.6
#define	 P_PUMP_BIT		7	// P1.7


//--------------------------------------------------------------
// language definitions:
//--------------------------------------------------------------

#define  byte  unsigned char
#define  word  unsigned int
#define  bool  byte
#define  TRUE  1
#define  FALSE 0
#define  FINNISHED TRUE

/****************************************************************************/
/* Usefull bit macros.                                                      */
/****************************************************************************/

#define   checkbit(var,bit)  (var & (0x01 << (bit)))
#define   setbit(var,bit)    (var |= (0x01 << (bit)))
#define   clrbit(var,bit)    (var &= (~(0x01 << (bit))))


//--------------------------------------------------------------
// MACRO's:
//--------------------------------------------------------------

//----------------------
// Short dely-loops:
//----------------------
#define   delay_1uS	asm{NOP;NOP;NOP;NOP;}
#define   delay_2uS	asm{NOP;NOP;NOP;NOP; NOP;NOP;NOP;NOP;}
#define   delay_3uS	asm{NOP;NOP;NOP;NOP; NOP;NOP;NOP;NOP; NOP;NOP;NOP;NOP;}
#define   delay_4uS	asm{NOP;NOP;NOP;NOP; NOP;NOP;NOP;NOP; NOP;NOP;NOP;NOP; NOP;NOP;NOP;NOP;}
#define   delay_5uS	asm{NOP;NOP;NOP;NOP; NOP;NOP;NOP;NOP; NOP;NOP;NOP;NOP; NOP;NOP;NOP;NOP; NOP;NOP;NOP;NOP;}

//----------------------
// Div. ports:
//----------------------

#define  OutPorts_Disable  (setbit(p3_reg,6))	// P3.6
#define  OutPorts_Enable   (clrbit(p3_reg,6))	// P3.6

#define  set_beep	   (setbit(p3_reg,7))	// P3.7
#define  clr_beep	   (clrbit(p3_reg,7))	// P3.7

#define  set_tp1	   (setbit(p2_reg,7))	// P2.7
#define  clr_tp1	   (clrbit(p2_reg,7))	// P2.7

#define  AuxIn		  (checkbit(In_Port0,6))

//----------------------
// Plate Sensors:
//----------------------
#define  Plate_Sensor	   ( checkbit(p1_pin,PLATE_SENS_BIT))	// P1.6
#define  PlateErr_SW	   ( checkbit(In_Port0, 7))	// In_Port0.7

//------------------------------------------
// Motor1, Motor2, Motor3:
//------------------------------------------
#define  Motor_Reset_on	  {clrbit(Port4,7); Out_Port4 = Port4;}
#define  Motor_Reset_off  {setbit(Port4,7); Out_Port4 = Port4;}

#define  Motor0_Sensor	   ( checkbit(p1_pin,M0_POS_SENS_BIT))	// Carriage
#define  Motor1_Sensor	   (!checkbit(p1_pin,M1_POS_SENS_BIT))	// ASP-lift
#define  Motor2_Sensor	   (!checkbit(p1_pin,M2_POS_SENS_BIT))	// DISP-lift

#define  PWM0_Out(value)  {pwm0_control = value;}
#define  PWM1_Out(value)  {pwm1_control = value;}
#define  PWM2_Out(value)  {pwm2_control = value;}

//------------------------------------------
#define	 Motor0_Run	  (checkbit(Port3,4))
#define  Motor0_Start	    {setbit(Port3,4); Out_Port3 = Port3;}
#define  Motor0_Stop	    {clrbit(Port3,4); Out_Port3 = Port3;}

#define	 Motor0_Direction (!checkbit(Port3,5))
#define  Motor0_Dir_1	     {clrbit(Port3,5); Out_Port3 = Port3;}
#define  Motor0_Dir_0	     {setbit(Port3,5); Out_Port3 = Port3;}

#define  Motor0_HiPower	    {setbit(Port3,6); Out_Port3 = Port3;}
#define  Motor0_LoPower	    {clrbit(Port3,6); Out_Port3 = Port3;}
//------------------------------------------
#define	 Motor1_Run	  (checkbit(Port4,0))
#define  Motor1_Start	    {setbit(Port4,0); Out_Port4 = Port4;}
#define  Motor1_Stop	    {clrbit(Port4,0); Out_Port4 = Port4;}

#define	 Motor1_Direction  (checkbit(Port4,1))
#define  Motor1_Dir_1	     {setbit(Port4,1); Out_Port4 = Port4;}
#define  Motor1_Dir_0	     {clrbit(Port4,1); Out_Port4 = Port4;}

#define  Motor1_HiPower	    {setbit(Port4,2); Out_Port4 = Port4;}
#define  Motor1_LoPower	    {clrbit(Port4,2); Out_Port4 = Port4;}
//------------------------------------------
#define	 Motor2_Run	  (checkbit(Port4,4))
#define  Motor2_Start	    {setbit(Port4,4); Out_Port4 = Port4;}
#define  Motor2_Stop	    {clrbit(Port4,4); Out_Port4 = Port4;}

#define	 Motor2_Direction (checkbit(Port4,5))
#define  Motor2_Dir_1	    {setbit(Port4,5); Out_Port4 = Port4;}
#define  Motor2_Dir_0	    {clrbit(Port4,5); Out_Port4 = Port4;}

#define  Motor2_HiPower	    {setbit(Port4,6); Out_Port4 = Port4;}
#define  Motor2_LoPower	    {clrbit(Port4,6); Out_Port4 = Port4;}

//------------------------------------------
// Pressure:
//------------------------------------------
#define	 Pres_Pump_on	   clrbit(p1_reg,7)	// (P1.7) Pressure motor ON.
#define	 Pres_Pump_off	   setbit(p1_reg,7)	// (P1.7) Pressure motor OFF.
#define	 Pres_Pump     (!checkbit(p1_reg,7))	// (P1.7) Read Pressure motor.

#define	 Pres_Valve_on	   clrbit(p4_reg,3)	// (P4.3) Pressure Valve OPEN.
#define	 Pres_Valve_off    setbit(p4_reg,3)	// (P4.3) Pressure Valve Closed.
#define	 Pres_Valve    (!checkbit(p4_reg,3))	// (P4.3) Pressure Valve.

//------------------------------------------
// Vacuum (Pump & Valve):
//------------------------------------------
#define  Vac_Pump_on	  {setbit(Port3,0); Out_Port3 = Port3;}
#define  Vac_Pump_off	  {clrbit(Port3,0); Out_Port3 = Port3;}
#define	 Vac_Pump     	(checkbit(Port3,0))	// (P3.0) Read Pressure motor.

#define  Vac_Valve_on	  {setbit(Port3,3); Out_Port3 = Port3;}
#define  Vac_Valve_off	  {clrbit(Port3,3); Out_Port3 = Port3;}
#define	 Vac_Valve     	(checkbit(Port3,3))	// (P3.3) Read Vacuum Valve.

//------------------------------------------
// ASP-Vacuum (Mega Pump):
//------------------------------------------
#define  Asp_Pump_on	  {setbit(Port3,1); Out_Port3 = Port3;}
#define  Asp_Pump_off	  {clrbit(Port3,1); Out_Port3 = Port3;}
#define	 Asp_Pump     	(checkbit(Port3,1))	// (P1.7) Read Pressure motor.

//------------------------------------------
// Waste:
//------------------------------------------
#define  Waste_Pump_on	  {setbit(Port3,2); Out_Port3 = Port3;}
#define  Waste_Pump_off	  {clrbit(Port3,2); Out_Port3 = Port3;}
#define	 Waste_Pump     (checkbit(Port3,2))	// (P3.2) Read Pressure motor.


//------------------------------------------
// Disp-Head Coding V3:
//------------------------------------------
#ifdef __VER_3
  #define  DispHead_0_Out	{Port1 |= 0x1f;                 Out_Port1 = Port1;}
  #define  DispHead_4_Out	{Port1 |= 0x1f; Port1 &= ~0x01; Out_Port1 = Port1;}
  #define  DispHead_3_Out	{Port1 |= 0x1f; Port1 &= ~0x02; Out_Port1 = Port1;}
  #define  DispHead_2_Out	{Port1 |= 0x1f; Port1 &= ~0x04; Out_Port1 = Port1;}
  #define  DispHead_1_Out	{Port1 |= 0x1f; Port1 &= ~0x08; Out_Port1 = Port1;}
  #define  DispHead_5_Out	{Port1 |= 0x1f; Port1 &= ~0x10; Out_Port1 = Port1;}
  #define  DispHead_In	  	((In_Port1 & 0xC0) >> 6)	// Head = Bit 6-7
#else
//------------------------------------------
// Disp-Head Coding V4:
//------------------------------------------
// DKM 092209 looks the same as coding v3
//            from the schematic, there are only 5 head output lines, so
//            I think Disphead_0_Out is not really a disp head, but
//            just to clear the selection (since the bus is multiplexed).
//            Kind of weird that selection is active low here, but is active
//			  high for the ADC selection?
  #define  DispHead_0_Out	{Port1 |= 0x1f;                 Out_Port1 = Port1;}
  #define  DispHead_1_Out	{Port1 |= 0x1f; Port1 &= ~0x01; Out_Port1 = Port1;}
  #define  DispHead_2_Out	{Port1 |= 0x1f; Port1 &= ~0x02; Out_Port1 = Port1;}
  #define  DispHead_3_Out	{Port1 |= 0x1f; Port1 &= ~0x04; Out_Port1 = Port1;}
  #define  DispHead_4_Out	{Port1 |= 0x1f; Port1 &= ~0x08; Out_Port1 = Port1;}
  #define  DispHead_5_Out	{Port1 |= 0x1f; Port1 &= ~0x10; Out_Port1 = Port1;}
  #define  DispHead_In	  	((In_Port1 & 0xC0) >> 6)	// Head = Bit 6-7
#endif

//------------------------------------------
// ADC-multiplexer:
//------------------------------------------
// DKM 092209 not sure why they don't have the last bit --------v
#define  ADC1	0x00;	// Disp1 Sensor, Multiplexer adress: 000
#define  ADC2	0x20;	// Disp2 Sensor, Multiplexer adress: 001
#define  ADC3	0x40;	// Disp3 Sensor, Multiplexer adress: 010
#define  ADC4	0x60;	// Disp4 Sensor, Multiplexer adress: 011
#define  ADC0	0x80;	// Internal Sensor (presure) adress: 100
#define  ADC5	0xA0;	// Internal Sensor (vacuum)  adress: 101

#define  Select_ADC1	  {Port1 &= 0x1F;                Out_Port1 = Port1;}	// 000
#define  Select_ADC2	  {Port1 &= 0x1F; Port1 |= 0x20; Out_Port1 = Port1;}	// 001
#define  Select_ADC3	  {Port1 &= 0x1F; Port1 |= 0x40; Out_Port1 = Port1;}	// 010
#define  Select_ADC4	  {Port1 &= 0x1F; Port1 |= 0x60; Out_Port1 = Port1;}	// 011
#define  Select_ADC5	  {Port1 &= 0x1F; Port1 |= 0x80; Out_Port1 = Port1;}	// 100
#define  Select_ADC6	  {Port1 &= 0x1F; Port1 |= 0xA0; Out_Port1 = Port1;}	// 101
#define  Select_ADC7	  {Port1 &= 0x1F; Port1 |= 0xC0; Out_Port1 = Port1;}	// 110
#define  Select_ADC8	  {Port1 &= 0x1F; Port1 |= 0xE0; Out_Port1 = Port1;}	// 111

#define  Select_ADC0	  Select_ADC5	// Internal Pressure Sensor (+)
#define  Select_ADC0_Vac  Select_ADC6	// Internal Pressure Sensor (-)

//------------------------------------------
// Keyboard input:
//------------------------------------------
// Bit 0 = DOWN-Key
// Bit 1 = START-Key
// Bit 2 = UP-Key
// Bit 3 = CANCEL-Key
// Bit 4 = ENTER-Key
// Bit 5 = STOP-Key
// Bit 6 = Head_Coding (5)
// Bit 7 = Head_Coding (6)
#define  Keypad_In	  (In_Port1 & 0x3F)	// Keypad = Bit 0-5

//------------------------------------------
// Buffer Alarms input:
//------------------------------------------
// Bit 0 = Buffer1 Alarm
// Bit 1 = Buffer2 Alarm
// Bit 2 = Buffer3 Alarm
// Bit 3 = Buffer4 Alarm
// Bit 4 = Waste Alarm
// Bit 5 = RinseHead Mounted
// Bit 6 = Aux Input
// Bit 7 = Plate Error
#define  Alarms_In	  (In_Port0 & 0x1F)	// Alarms = Bit 0-4


//----------------------
// Dispence Valves:
//----------------------
//#define DispValve1	   0x03
//#define DispValve2	   0x0C
//#define DispValve3	   0x30
//#define DispValve4	   0xC0

#define	 set_Disp_Valve1   (Port0 |= 0x03)	// Add to RamByte!
#define	 set_Disp_Valve2   (Port0 |= 0x0C)	// Add to RamByte!
#define	 set_Disp_Valve3   (Port0 |= 0x30)	// Add to RamByte!
#define	 set_Disp_Valve4   (Port0 |= 0xC0)	// Add to RamByte!

#define	 Disp_Valve_on     (Out_Port0 = Port0)			// Output RamByte to Port!
#define	 Disp_Valve_low    {Port0 &= 0xAA; Out_Port0 = Port0;}	// Only LowCurrent output!
#define	 Disp_Valve_off    {Port0  = 0x00; Out_Port0 = Port0;}	// All valves Off!


//--------------------------------------------------------------

#define  PWM_PRESCALE0		0	// prescale = 2

//--------------------------------------------------------------
// serial.c
//--------------------------------------------------------------

#define   SP_MODE0           0x00
#define   SP_MODE1           0x01
#define   SP_MODE2           0x02
#define   SP_MODE3           0x03
#define   REC_ENABLE         0x08
#define   REC_DISABLE        0x00
#define   EVEN_PARITY        0x08
#define   ODD_PARITY         0x28
#define   NO_PARITY          0x00
#define   SET_BIT_8          0x10
#define   SP_INTERNAL_CLK    0x8000
#define   SP_EXTERNAL_CLK    0x0000
#define   TXD_INTERRUPT      0x20
#define   RXD_INTERRUPT      0x40
#define   TI_BIT             0x05
#define   RI_BIT             0x06
#define   FE_BIT             0x04
#define   OE_BIT             0x02
#define   RPE_BIT            0x07
#define   RB8_BIT            0x07
#define   TXE_BIT            0x03


//--------------------------------------------------------------
//  TIMER init bits:
//--------------------------------------------------------------

#define  COUNT_ENABLE         0x80
#define  COUNT_DISABLE        0x00
#define  COUNT_UP             0x40
#define  COUNT_DOWN           0x00
#define  CLOCK_INTERNAL       0x00
#define  CLOCK_EXTERNAL       0x08
#define  DIRECTION_TXDIR      0x10
#define  CLOCK_T1_OVFL        0x20
#define  DIRECTION_T1_OVFL    0x10
#define  QUADRATURE_CLOCK     0x38
#define  DIVIDE_BY_1          0x00
#define  DIVIDE_BY_2          0x01
#define  DIVIDE_BY_4          0x02
#define  DIVIDE_BY_8          0x03
#define  DIVIDE_BY_16         0x04
#define  DIVIDE_BY_32         0x05
#define  DIVIDE_BY_64         0x06

//--------------------------------------------------------------
//  EPA init bits:
//--------------------------------------------------------------

#define   CAPTURE      0x00
#define   COMPARE      0x40
#define   RE_ENABLE    0x08
#define   POS_EDGE     0x20
#define   SET_PIN      0x20
#define   NEG_EDGE     0x10
#define   CLR_PIN      0x10
#define   BOTH_EDGE    0x30
#define   TOGGLE_PIN   0x30
#define   NOTHING      0x00
#define   RESET_OPP_TIMER          0x02
#define   RESET_TIMER              0x01
#define   OVERWRITE_NEW_DATA       0x01
#define   IGNORE_NEW_DATA          0x00
#define   USE_TIMER1               0x00
#define   USE_TIMER2               0x80
#define   RE_MAP                   0x0100
#define   EPA0_INT_BIT             7
#define   EPA1_INT_BIT             0
#define   EPA2_INT_BIT             1
#define   EPA3_INT_BIT             2
#define   EPA0_1_OVR_INT_BIT       3
#define   EPA2_3_OVR_INT_BIT       4


#endif

