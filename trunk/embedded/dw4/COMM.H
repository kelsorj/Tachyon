#ifndef __COMM_H
#define __COMM_H

/**************************************************************************
*  FUNCTION    : COMMunication
*  METHODE     : Includes high level communicaton protocol
*  AUTHOR      : HJ 97-01
***************************************************************************/

#define FLASH_DATA_BUF_SIZE  256

//-------------------------------------------------
#define	START_Cmd	1	// Robot Commands
#define	STOP_Cmd	2	// Robot Commands
#define	PROG_Cmd	3	// Robot Commands
#define	STATUS_Cmd	4	// Robot Commands
//-------------------------------------------------
#define	T0_Cmd		10	// Test/Calibration
#define	T1_Cmd		11	// Test/Calibration
#define	T2_Cmd		12	// Test/Calibration
#define	T3_Cmd		13	// Test/Calibration
#define	T4_Cmd		14	// Test/Calibration
#define	T5_Cmd		15	// Test/Calibration
#define	T6_Cmd		16	// Test/Calibration
#define	T7_Cmd		17	// Test/Calibration
#define	T8_Cmd		18	// Test/Calibration
#define	T9_Cmd		19	// Test/Calibration
//-------------------------------------------------
#define	IDLE_ACK	">OK0\n\r"
#define	READY_ACK	">OK1\n\r"
#define	OK_ACK		">OK2\n\r"
#define	OK_STOP_ACK	">OK3\n\r"
#define	BUSY_ACK	">BSY\n\r"
#define	FINNISHED_ACK	">END\n\r"

#define	SYNTAXERROR_ACK	">E01\n\r"
#define	PROG_ERROR_ACK	">E02\n\r"

#define	STOP_ACK	">E99\n\r"	// Stop-Mode!
#define	PL_ALARM_ACK	">E03\n\r"	// Stop-Mode! No Plate!
#define	PZ_ALARM_ACK	">E04\n\r"	// Stop-Mode! Plate High!

#define	E0_ALARM_ACK	">E00\n\r"	// Unknown error
#define	L1_ALARM_ACK	">E11\n\r"
#define	L2_ALARM_ACK	">E12\n\r"
#define	L3_ALARM_ACK	">E13\n\r"
#define	L4_ALARM_ACK	">E14\n\r"
#define	WA_ALARM_ACK	">E15\n\r"
#define H1_ALARM_ACK	">E21\n\r"
#define H2_ALARM_ACK	">E22\n\r"
#define H3_ALARM_ACK	">E23\n\r"
#define H4_ALARM_ACK	">E24\n\r"
#define H5_ALARM_ACK	">E25\n\r"
#define P1_ALARM_ACK	">E31\n\r"
#define P2_ALARM_ACK	">E32\n\r"
#define P3_ALARM_ACK	">E33\n\r"
#define P4_ALARM_ACK	">E34\n\r"
#define PS_ALARM_ACK	">E35\n\r"	// Pressure Sensor Error (Running)
//-------------------------------------------------
#define   ESC	     27
#define   SLASH	     47
#define   ACK	     06
#define   CR	     13
#define   ENDOFFILE  26
//-------------------------------------------------

#endif

