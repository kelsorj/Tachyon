/*
 *  Copyright (c) 1995, Intel Corporation
 *
 *  $Workfile:   80c196np.h  $
 *  $Revision:   1.1  $
 *  $Modtime:   Mar 22 1995 16:59:52  $
 *
 *  Purpose:
 *
 *
 *
 *
 *
 *  Compiler:       
 *
 *  Ext Packages:   
 *
 * 
 *
 */
/* 80C196NP.h
 * Copyright (c) 1990-1994 Intel Corporation, ALL RIGHTS RESERVED.
 */

/****************************************************************************/
/*                                                                          */
/* Standard SFRs.                                                           */
/*                                                                          */
/****************************************************************************/

#ifndef _80c196nph
#define _80c196nph

extern volatile register unsigned short zero_reg;   /* at 0x00: r   zero */
extern volatile register unsigned short ff_reg;     /* at 0x02: r   ff */
extern volatile register unsigned short ptssel;     /* at 0x04: r/w */
extern volatile register unsigned short ptssrv;     /* at 0x06: r/w */
extern volatile register unsigned char  int_mask;   /* at 0x08: r/w */
extern volatile register unsigned char  int_pend;   /* at 0x09: r/w */
extern volatile register unsigned char  int_pend1;  /* at 0x12: r/w */
extern volatile register unsigned char  int_mask1;  /* at 0x13: r/w */
extern volatile register unsigned char  wsr;        /* at 0x14: r/w */

void enable(void);
void disable(void);
void enable_pts(void);
void disable_pts(void);

/****************************************************************************/
/*                                                                          */
/* Additional C96.LIB functions supported by the 80C196 only                */
/*                                                                          */
/****************************************************************************/

void power_down(void);
void idle(void);

/****************************************************************************/
/*                                                                          */
/* Additional SFRs of the 80C196NP                                          */
/*                                                                          */
/****************************************************************************/

extern volatile unsigned char  p1_pin;
extern volatile unsigned char  p1_reg;
extern volatile unsigned char  p1_dir;
extern volatile unsigned char  p1_mode;

/*   128 BYTE WINDOW WITH WSR = 1FH  */
extern volatile register unsigned char  p1_pin_1F;
extern volatile register unsigned char  p1_reg_1F;
extern volatile register unsigned char  p1_dir_1F;
extern volatile register unsigned char  p1_mode_1F;

/*   64 BYTE WINDOW WITH WSR = 3FH  */
extern volatile register unsigned char  p1_pin_3F;
extern volatile register unsigned char  p1_reg_3F;
extern volatile register unsigned char  p1_dir_3F;
extern volatile register unsigned char  p1_mode_3F;

/*   32 BYTE WINDOW WITH WSR = 7EH  */
extern volatile register unsigned char  p1_pin_7E;
extern volatile register unsigned char  p1_reg_7E;
extern volatile register unsigned char  p1_dir_7E;
extern volatile register unsigned char  p1_mode_7E;

extern volatile unsigned char  p2_pin;
extern volatile unsigned char  p2_reg;
extern volatile unsigned char  p2_dir;
extern volatile unsigned char  p2_mode;

/*   128 BYTE WINDOW WITH WSR = 1FH  */
extern volatile register unsigned char  p2_pin_1F;
extern volatile register unsigned char  p2_reg_1F;
extern volatile register unsigned char  p2_dir_1F;
extern volatile register unsigned char  p2_mode_1F;

/*   64 BYTE WINDOW WITH WSR = 3FH  */
extern volatile register unsigned char  p2_pin_3F;
extern volatile register unsigned char  p2_reg_3F;
extern volatile register unsigned char  p2_dir_3F;
extern volatile register unsigned char  p2_mode_3F;

/*   32 BYTE WINDOW WITH WSR = 7EH  */
extern volatile register unsigned char  p2_pin_7E;
extern volatile register unsigned char  p2_reg_7E;
extern volatile register unsigned char  p2_dir_7E;
extern volatile register unsigned char  p2_mode_7E;

extern volatile unsigned char  p3_pin;
extern volatile unsigned char  p3_reg;
extern volatile unsigned char  p3_dir;
extern volatile unsigned char  p3_mode;

/*   128 BYTE WINDOW WITH WSR = 1FH  */
extern volatile register unsigned char  p3_pin_1F;
extern volatile register unsigned char  p3_reg_1F;
extern volatile register unsigned char  p3_dir_1F;
extern volatile register unsigned char  p3_mode_1F;

/*   64 BYTE WINDOW WITH WSR = 3FH  */
extern volatile register unsigned char  p3_pin_3F;
extern volatile register unsigned char  p3_reg_3F;
extern volatile register unsigned char  p3_dir_3F;
extern volatile register unsigned char  p3_mode_3F;

/*   32 BYTE WINDOW WITH WSR = 7EH  */
extern volatile register unsigned char  p3_pin_7E;
extern volatile register unsigned char  p3_reg_7E;
extern volatile register unsigned char  p3_dir_7E;
extern volatile register unsigned char  p3_mode_7E;

extern volatile unsigned char  p4_pin;
extern volatile unsigned char  p4_reg;
extern volatile unsigned char  p4_dir;
extern volatile unsigned char  p4_mode;

/*   128 BYTE WINDOW WITH WSR = 1FH  */
extern volatile register unsigned char  p4_pin_1F;
extern volatile register unsigned char  p4_reg_1F;
extern volatile register unsigned char  p4_dir_1F;
extern volatile register unsigned char  p4_mode_1F;

/*   64 BYTE WINDOW WITH WSR = 3FH  */
extern volatile register unsigned char  p4_pin_3F;
extern volatile register unsigned char  p4_reg_3F;
extern volatile register unsigned char  p4_dir_3F;
extern volatile register unsigned char  p4_mode_3F;

/*   32 BYTE WINDOW WITH WSR = 7EH  */
extern volatile register unsigned char  p4_pin_7E;
extern volatile register unsigned char  p4_reg_7E;
extern volatile register unsigned char  p4_dir_7E;
extern volatile register unsigned char  p4_mode_7E;

extern volatile unsigned short timer1;
extern volatile unsigned short timer2;
extern volatile unsigned char  t1control;
extern volatile unsigned char  t2control;

/*   128 BYTE WINDOW WITH WSR = 1FH  */
extern volatile register unsigned short timer1_1F;
extern volatile register unsigned short timer1_1F;
extern volatile register unsigned short timer2_1F;
extern volatile register unsigned char  t1control_1F;
extern volatile register unsigned char  t2control_1F;

/*   64 BYTE WINDOW WITH WSR = 3EH  */
extern volatile register unsigned short timer1_3E;
extern volatile register unsigned short timer1_3E;
extern volatile register unsigned short timer2_3E;
extern volatile register unsigned char  t1control_3E;
extern volatile register unsigned char  t2control_3E;

/*   32 BYTE WINDOW WITH WSR = 7CH  */
extern volatile register unsigned short timer1_7C;
extern volatile register unsigned short timer1_7C;
extern volatile register unsigned short timer2_7C;
extern volatile register unsigned char  t1control_7C;
extern volatile register unsigned char  t2control_7C;

extern volatile unsigned short sp_bd_count;
extern volatile unsigned short sp_baud;
extern volatile unsigned char  sp_con;
extern volatile unsigned char  sp_status;
extern volatile unsigned char  sbuf_tx;
extern volatile unsigned char  sbuf_rx;

/*   128 BYTE WINDOW WITH WSR = 1FH  */
extern volatile register unsigned short sp_bd_count_1F;
extern volatile register unsigned short sp_baud_1F;
extern volatile register unsigned char  sp_control_1F;
extern volatile register unsigned char  sp_status_1F;
extern volatile register unsigned char  sbuf_tx_1F;
extern volatile register unsigned char  sbuf_rx_1F;

/*   64 BYTE WINDOW WITH WSR = 3EH  */
extern volatile register unsigned short sp_bd_count_3E;
extern volatile register unsigned short sp_baud_3E;
extern volatile register unsigned char  sp_control_3E;
extern volatile register unsigned char  sp_status_3E;
extern volatile register unsigned char  sbuf_tx_3E;
extern volatile register unsigned char  sbuf_rx_3E;

/*   32 BYTE WINDOW WITH WSR = 7DH  */
extern volatile register unsigned short sp_bd_count_7D;
extern volatile register unsigned short sp_baud_7D;
extern volatile register unsigned char  sp_control_7D;
extern volatile register unsigned char  sp_status_7D;
extern volatile register unsigned char  sbuf_tx_7D;
extern volatile register unsigned char  sbuf_rx_7D;

extern volatile unsigned char  pwm0_control;
extern volatile unsigned char  pwm1_control;
extern volatile unsigned char  pwm2_control;
extern volatile unsigned char  con_reg0;

/*   128 BYTE WINDOW WITH WSR = 1FH  */
extern volatile register unsigned char  pwm0_control_1F;
extern volatile register unsigned char  pwm1_control_1F;
extern volatile register unsigned char  pwm2_control_1F;
extern volatile register unsigned char  con_reg0_1F;

/*   64 BYTE WINDOW WITH WSR = 3EH  */
extern volatile register unsigned char  pwm0_control_3E;
extern volatile register unsigned char  pwm1_control_3E;
extern volatile register unsigned char  pwm2_control_3E;
extern volatile register unsigned char  con_reg0_3E;

/*   32 BYTE WINDOW WITH WSR = 7DH  */
extern volatile register unsigned char  pwm0_control_7D;
extern volatile register unsigned char  pwm1_control_7D;
extern volatile register unsigned char  pwm2_control_7D;
extern volatile register unsigned char  con_reg0_7D;



extern volatile unsigned short epa_mask;
extern volatile unsigned short epa_stat;

/*   128 BYTE WINDOW WITH WSR = 1FH  */
extern volatile register unsigned short epa_mask_1F;
extern volatile register unsigned short epa_stat_1F;

/*   64 BYTE WINDOW WITH WSR = 3EH  */
extern volatile register unsigned short epa_mask_3E;
extern volatile register unsigned short epa_stat_3E;


/*   32 BYTE WINDOW WITH WSR = 7CH  */
extern volatile register unsigned short epa_mask_7C;
extern volatile register unsigned short epa_stat_7C;


extern volatile unsigned char  ep_pin;
extern volatile unsigned char  ep_reg;
extern volatile unsigned char  ep_dir;
extern volatile unsigned char  ep_mode;
extern volatile unsigned short epa3_time;
extern volatile unsigned short epa3_con;
extern volatile unsigned short epa2_time;
extern volatile unsigned char  epa2_con;
extern volatile unsigned short epa1_time;
extern volatile unsigned short epa1_con;
extern volatile unsigned short epa0_time;
extern volatile unsigned char  epa0_con;

/*   128 BYTE WINDOW WITH WSR = 1FH  */
extern volatile register unsigned short epa3_time_1F;
extern volatile register unsigned short epa3_con_1F;
extern volatile register unsigned short epa2_time_1F;
extern volatile register unsigned char  epa2_con_1F;
extern volatile register unsigned short epa1_time_1F;
extern volatile register unsigned short epa1_con_1F;
extern volatile register unsigned short epa0_time_1F;
extern volatile register unsigned char  epa0_con_1F;

/*   64 BYTE WINDOW WITH WSR = 3EH  */
extern volatile register unsigned short epa3_time_3E;
extern volatile register unsigned short epa3_con_3E;
extern volatile register unsigned short epa2_time_3E;
extern volatile register unsigned char  epa2_con_3E;
extern volatile register unsigned short epa1_time_3E;
extern volatile register unsigned short epa1_con_3E;
extern volatile register unsigned short epa0_time_3E;
extern volatile register unsigned char  epa0_con_3E;


/*   32 BYTE WINDOW WITH WSR = 7CH  */
extern volatile register unsigned short epa3_time_7C;
extern volatile register unsigned short epa3_con_7C;
extern volatile register unsigned short epa2_time_7C;
extern volatile register unsigned char  epa2_con_7C;
extern volatile register unsigned short epa1_time_7C;
extern volatile register unsigned short epa1_con_7C;
extern volatile register unsigned short epa0_time_7C;
extern volatile register unsigned char  epa0_con_7C;

extern volatile unsigned short addrcom0;
extern volatile unsigned short addrmsk0;
extern volatile unsigned char  buscon0;
extern volatile unsigned short addrcom1;
extern volatile unsigned short addrmsk1;
extern volatile unsigned char  buscon1;
extern volatile unsigned short addrcom2;
extern volatile unsigned short addrmsk2;
extern volatile unsigned char  buscon2;
extern volatile unsigned short addrcom3;
extern volatile unsigned short addrmsk3;
extern volatile unsigned char  buscon3;
extern volatile unsigned short addrcom4;
extern volatile unsigned short addrmsk4;
extern volatile unsigned char  buscon4;
extern volatile unsigned short addrcom5;
extern volatile unsigned short addrmsk5;
extern volatile unsigned char  buscon5;

/*   128 BYTE WINDOW WITH WSR = 1FH  */
extern volatile register unsigned short addrcom0_1F;
extern volatile register unsigned short addrmsk0_1F;
extern volatile register unsigned char  buscon0_1F;
extern volatile register unsigned short addrcom1_1F;
extern volatile register unsigned short addrmsk1_1F;
extern volatile register unsigned char  buscon1_1F;
extern volatile register unsigned short addrcom2_1F;
extern volatile register unsigned short addrmsk2_1F;
extern volatile register unsigned char  buscon2_1F;
extern volatile register unsigned short addrcom3_1F;
extern volatile register unsigned short addrmsk3_1F;
extern volatile register unsigned char  buscon3_1F;
extern volatile register unsigned short addrcom4_1F;
extern volatile register unsigned short addrmsk4_1F;
extern volatile register unsigned char  buscon4_1F;
extern volatile register unsigned short addrcom5_1F;
extern volatile register unsigned short addrmsk5_1F;
extern volatile register unsigned char  buscon5_1F;

/*   64 BYTE WINDOW WITH WSR = 3DH  */
extern volatile register unsigned short addrcom0_3D;
extern volatile register unsigned short addrmsk0_3D;
extern volatile register unsigned char  buscon0_3D;
extern volatile register unsigned short addrcom1_3D;
extern volatile register unsigned short addrmsk1_3D;
extern volatile register unsigned char  buscon1_3D;
extern volatile register unsigned short addrcom2_3D;
extern volatile register unsigned short addrmsk2_3D;
extern volatile register unsigned char  buscon2_3D;
extern volatile register unsigned short addrcom3_3D;
extern volatile register unsigned short addrmsk3_3D;
extern volatile register unsigned char  buscon3_3D;
extern volatile register unsigned short addrcom4_3D;
extern volatile register unsigned short addrmsk4_3D;
extern volatile register unsigned char  buscon4_3D;
extern volatile register unsigned short addrcom5_3D;
extern volatile register unsigned short addrmsk5_3D;
extern volatile register unsigned char  buscon5_3D;

/*   32 BYTE WINDOW WITH WSR = 7AH  */
extern volatile register unsigned short addrcom0_7A;
extern volatile register unsigned short addrmsk0_7A;
extern volatile register unsigned char  buscon0_7A;
extern volatile register unsigned short addrcom1_7A;
extern volatile register unsigned short addrmsk1_7A;
extern volatile register unsigned char  buscon1_7A;
extern volatile register unsigned short addrcom2_7A;
extern volatile register unsigned short addrmsk2_7A;
extern volatile register unsigned char  buscon2_7A;
extern volatile register unsigned short addrcom3_7A;
extern volatile register unsigned short addrmsk3_7A;
extern volatile register unsigned char  buscon3_7A;
/*   32 BYTE WINDOW WITH WSR = 7BH  */
extern volatile register unsigned short addrcom4_7B;
extern volatile register unsigned short addrmsk4_7B;
extern volatile register unsigned char  buscon4_7B;
extern volatile register unsigned short addrcom5_7B;
extern volatile register unsigned short addrmsk5_7B;
extern volatile register unsigned char  buscon5_7B;


/****************************************************************************/
/*                                                                          */
/* Usefull bit macros.                                                      */
/*                                                                          */
/****************************************************************************/

#define   checkbit(var,bit)  (var & (0x01 << (bit)))
#define   setbit(var,bit)    (var |= (0x01 << (bit)))
#define   clrbit(var,bit)    (var &= (~(0x01 << (bit))))

#endif /* _80c196nph */
