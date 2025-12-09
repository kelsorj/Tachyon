
//--------------------------------------------------------------
// motor0:
// Home = 0mm (left)
// Sensor = 8,5mm
// Max = 370mm
//--------------------------------------------------------------

#define  M0_TACHO	 1708	// Aqua3 Stepper: 17.45 tacho counts = 1mm
#define  SENSOR0_POSmm	 2200	// 22.0 mm (changed by calibration!).
#define  SENSOR0_POS	 ((long)((long)SENSOR0_POSmm*(long)M0_TACHO)/(long)10000L)	// Sensor-pos in tacho-counts!


//--------------------------------------------------------------
// motor1:
// Home = 16mm (max = 16,5mm)
// Sensor = 12mm
// Floor = 0mm (min = 0mm)
//--------------------------------------------------------------

#define  M1_TACHO	 7720	// Aqua3 Lift stepper: 77.20 tacho counts = 1mm
#define  SENSOR1_POSmm	 1450	// 12.0 mm over floor (changed by calibration!).
#define  SENSOR1_POS	 ((long)((long)SENSOR1_POSmm*(long)M1_TACHO)/(long)10000L)	// Sensor-pos in tacho-counts!


//--------------------------------------------------------------
// motor2:
// Home = 16mm (max = 22mm)
// Sensor = 12mm
// Floor = 0mm (min = 0mm)
//--------------------------------------------------------------

#define  M2_TACHO	 7720	// Aqua3 Lift stepper: 77.20 tacho counts = 1mm
#define  SENSOR2_POSmm	 1550	// 12.0 mm over floor (changed by calibration!).
#define  SENSOR2_POS	 ((long)((long)SENSOR2_POSmm*(long)M2_TACHO)/(long)10000L)	// Sensor-pos in tacho-counts!


