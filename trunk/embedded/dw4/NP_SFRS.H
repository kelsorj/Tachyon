/*
 * np_sfrs.h
 * Generated with gen-sfrs.pl 1.9
 */

#ifndef _np_sfrs_h
#define _np_sfrs_h

/*                                                                   */
/* Special function register definitions for the                     */
/*                                                                   */
/* 8XC196NP                                                          */
/*                                                                   */
/* Generated from @(#)np_sfrs.db	1.2                                 */
/*                                                                   */
/* Copyright (C) 1994 Tasking Software B.V.                          */
/*                                                                   */
/* Note, that windowing 1FE0-1FFF is not possible                    */
/*                                                                   */
/* CPU SFR's                                                         */
extern volatile unsigned short register r0;                 /* 0x0000: R/W */
extern volatile unsigned short register zero_reg;           /* 0x0000: R/W */
extern volatile unsigned short register ff_reg;             /* 0x0002: R/W */
extern volatile unsigned short register ptssel;             /* 0x0004: R/W */
extern volatile unsigned short register ptssrv;             /* 0x0006: R/W */
extern volatile unsigned char  register int_mask;           /* 0x0008: R/W */
extern volatile unsigned char  register int_pend;           /* 0x0009: R/W */
extern volatile unsigned char  register int_pending;        /* 0x0009: R/W */
extern volatile unsigned char  register ipend1;             /* 0x0012: R/W */
extern volatile unsigned char  register int_pend1;          /* 0x0012: R/W */
extern volatile unsigned char  register imask1;             /* 0x0013: R/W */
extern volatile unsigned char  register int_mask1;          /* 0x0013: R/W */
extern volatile unsigned char  register wsr;                /* 0x0014: R/W */
extern volatile unsigned short register sp;                 /* 0x0018: R/W */
/* Ports 1 to 4 SFR's                                                */
extern volatile unsigned char  near     p1_dir;             /* 0x1fd2: R/W */
extern volatile unsigned char  register p1_dir_7e;          /* 0x00f2: R/W */
extern volatile unsigned char  register p1_dir_3f;          /* 0x00d2: R/W */
extern volatile unsigned char  register p1_dir_1f;          /* 0x00d2: R/W */

extern volatile unsigned char  near     p1_mode;            /* 0x1fd0: R/W */
extern volatile unsigned char  register p1_mode_7e;         /* 0x00f0: R/W */
extern volatile unsigned char  register p1_mode_3f;         /* 0x00d0: R/W */
extern volatile unsigned char  register p1_mode_1f;         /* 0x00d0: R/W */

extern volatile unsigned char  near     p1_pin;             /* 0x1fd6: R/W */
extern volatile unsigned char  register p1_pin_7e;          /* 0x00f6: R/W */
extern volatile unsigned char  register p1_pin_3f;          /* 0x00d6: R/W */
extern volatile unsigned char  register p1_pin_1f;          /* 0x00d6: R/W */

extern volatile unsigned char  near     p1_reg;             /* 0x1fd4: R/W */
extern volatile unsigned char  register p1_reg_7e;          /* 0x00f4: R/W */
extern volatile unsigned char  register p1_reg_3f;          /* 0x00d4: R/W */
extern volatile unsigned char  register p1_reg_1f;          /* 0x00d4: R/W */

extern volatile unsigned char  near     p1io;               /* 0x1fd2: R/W */
extern volatile unsigned char  register p1io_7e;            /* 0x00f2: R/W */
extern volatile unsigned char  register p1io_3f;            /* 0x00d2: R/W */
extern volatile unsigned char  register p1io_1f;            /* 0x00d2: R/W */

extern volatile unsigned char  near     p1pin;              /* 0x1fd6: R/W */
extern volatile unsigned char  register p1pin_7e;           /* 0x00f6: R/W */
extern volatile unsigned char  register p1pin_3f;           /* 0x00d6: R/W */
extern volatile unsigned char  register p1pin_1f;           /* 0x00d6: R/W */

extern volatile unsigned char  near     p1reg;              /* 0x1fd4: R/W */
extern volatile unsigned char  register p1reg_7e;           /* 0x00f4: R/W */
extern volatile unsigned char  register p1reg_3f;           /* 0x00d4: R/W */
extern volatile unsigned char  register p1reg_1f;           /* 0x00d4: R/W */

extern volatile unsigned char  near     p1ssel;             /* 0x1fd0: R/W */
extern volatile unsigned char  register p1ssel_7e;          /* 0x00f0: R/W */
extern volatile unsigned char  register p1ssel_3f;          /* 0x00d0: R/W */
extern volatile unsigned char  register p1ssel_1f;          /* 0x00d0: R/W */

extern volatile unsigned char  near     p2_dir;             /* 0x1fd3: R/W */
extern volatile unsigned char  register p2_dir_7e;          /* 0x00f3: R/W */
extern volatile unsigned char  register p2_dir_3f;          /* 0x00d3: R/W */
extern volatile unsigned char  register p2_dir_1f;          /* 0x00d3: R/W */

extern volatile unsigned char  near     p2_mode;            /* 0x1fd1: R/W */
extern volatile unsigned char  register p2_mode_7e;         /* 0x00f1: R/W */
extern volatile unsigned char  register p2_mode_3f;         /* 0x00d1: R/W */
extern volatile unsigned char  register p2_mode_1f;         /* 0x00d1: R/W */

extern volatile unsigned char  near     p2_pin;             /* 0x1fd7: R/W */
extern volatile unsigned char  register p2_pin_7e;          /* 0x00f7: R/W */
extern volatile unsigned char  register p2_pin_3f;          /* 0x00d7: R/W */
extern volatile unsigned char  register p2_pin_1f;          /* 0x00d7: R/W */

extern volatile unsigned char  near     p2_reg;             /* 0x1fd5: R/W */
extern volatile unsigned char  register p2_reg_7e;          /* 0x00f5: R/W */
extern volatile unsigned char  register p2_reg_3f;          /* 0x00d5: R/W */
extern volatile unsigned char  register p2_reg_1f;          /* 0x00d5: R/W */

extern volatile unsigned char  near     p2io;               /* 0x1fd3: R/W */
extern volatile unsigned char  register p2io_7e;            /* 0x00f3: R/W */
extern volatile unsigned char  register p2io_3f;            /* 0x00d3: R/W */
extern volatile unsigned char  register p2io_1f;            /* 0x00d3: R/W */

extern volatile unsigned char  near     p2pin;              /* 0x1fd7: R/W */
extern volatile unsigned char  register p2pin_7e;           /* 0x00f7: R/W */
extern volatile unsigned char  register p2pin_3f;           /* 0x00d7: R/W */
extern volatile unsigned char  register p2pin_1f;           /* 0x00d7: R/W */

extern volatile unsigned char  near     p2reg;              /* 0x1fd5: R/W */
extern volatile unsigned char  register p2reg_7e;           /* 0x00f5: R/W */
extern volatile unsigned char  register p2reg_3f;           /* 0x00d5: R/W */
extern volatile unsigned char  register p2reg_1f;           /* 0x00d5: R/W */

extern volatile unsigned char  near     p2ssel;             /* 0x1fd1: R/W */
extern volatile unsigned char  register p2ssel_7e;          /* 0x00f1: R/W */
extern volatile unsigned char  register p2ssel_3f;          /* 0x00d1: R/W */
extern volatile unsigned char  register p2ssel_1f;          /* 0x00d1: R/W */

extern volatile unsigned char  near     p3_dir;             /* 0x1fda: R/W */
extern volatile unsigned char  register p3_dir_7e;          /* 0x00fa: R/W */
extern volatile unsigned char  register p3_dir_3f;          /* 0x00da: R/W */
extern volatile unsigned char  register p3_dir_1f;          /* 0x00da: R/W */

extern volatile unsigned char  near     p3_mode;            /* 0x1fd8: R/W */
extern volatile unsigned char  register p3_mode_7e;         /* 0x00f8: R/W */
extern volatile unsigned char  register p3_mode_3f;         /* 0x00d8: R/W */
extern volatile unsigned char  register p3_mode_1f;         /* 0x00d8: R/W */

extern volatile unsigned char  near     p3_pin;             /* 0x1fde: R/W */
extern volatile unsigned char  register p3_pin_7e;          /* 0x00fe: R/W */
extern volatile unsigned char  register p3_pin_3f;          /* 0x00de: R/W */
extern volatile unsigned char  register p3_pin_1f;          /* 0x00de: R/W */

extern volatile unsigned char  near     p3_reg;             /* 0x1fdc: R/W */
extern volatile unsigned char  register p3_reg_7e;          /* 0x00fc: R/W */
extern volatile unsigned char  register p3_reg_3f;          /* 0x00dc: R/W */
extern volatile unsigned char  register p3_reg_1f;          /* 0x00dc: R/W */

extern volatile unsigned char  near     p3io;               /* 0x1fda: R/W */
extern volatile unsigned char  register p3io_7e;            /* 0x00fa: R/W */
extern volatile unsigned char  register p3io_3f;            /* 0x00da: R/W */
extern volatile unsigned char  register p3io_1f;            /* 0x00da: R/W */

extern volatile unsigned char  near     p3pin;              /* 0x1ffe: R/W */
extern volatile unsigned char  near     p3reg;              /* 0x1ffc: R/W */
extern volatile unsigned char  near     p3ssel;             /* 0x1fd8: R/W */
extern volatile unsigned char  register p3ssel_7e;          /* 0x00f8: R/W */
extern volatile unsigned char  register p3ssel_3f;          /* 0x00d8: R/W */
extern volatile unsigned char  register p3ssel_1f;          /* 0x00d8: R/W */

extern volatile unsigned char  near     p4_dir;             /* 0x1fdb: R/W */
extern volatile unsigned char  register p4_dir_7e;          /* 0x00fb: R/W */
extern volatile unsigned char  register p4_dir_3f;          /* 0x00db: R/W */
extern volatile unsigned char  register p4_dir_1f;          /* 0x00db: R/W */

extern volatile unsigned char  near     p4_mode;            /* 0x1fd9: R/W */
extern volatile unsigned char  register p4_mode_7e;         /* 0x00f9: R/W */
extern volatile unsigned char  register p4_mode_3f;         /* 0x00d9: R/W */
extern volatile unsigned char  register p4_mode_1f;         /* 0x00d9: R/W */

extern volatile unsigned char  near     p4_pin;             /* 0x1fdf: R/W */
extern volatile unsigned char  register p4_pin_7e;          /* 0x00ff: R/W */
extern volatile unsigned char  register p4_pin_3f;          /* 0x00df: R/W */
extern volatile unsigned char  register p4_pin_1f;          /* 0x00df: R/W */

extern volatile unsigned char  near     p4_reg;             /* 0x1fdd: R/W */
extern volatile unsigned char  register p4_reg_7e;          /* 0x00fd: R/W */
extern volatile unsigned char  register p4_reg_3f;          /* 0x00dd: R/W */
extern volatile unsigned char  register p4_reg_1f;          /* 0x00dd: R/W */

extern volatile unsigned char  near     p4io;               /* 0x1fdb: R/W */
extern volatile unsigned char  register p4io_7e;            /* 0x00fb: R/W */
extern volatile unsigned char  register p4io_3f;            /* 0x00db: R/W */
extern volatile unsigned char  register p4io_1f;            /* 0x00db: R/W */

extern volatile unsigned char  near     p4pin;              /* 0x1fff: R/W */
extern volatile unsigned char  near     p4reg;              /* 0x1ffd: R/W */
extern volatile unsigned char  near     p4ssel;             /* 0x1fd9: R/W */
extern volatile unsigned char  register p4ssel_7e;          /* 0x00f9: R/W */
extern volatile unsigned char  register p4ssel_3f;          /* 0x00d9: R/W */
extern volatile unsigned char  register p4ssel_1f;          /* 0x00d9: R/W */

/* EPORT SFR's                                                       */
extern volatile unsigned char  near     ep_dir;             /* 0x1fe3: R/W */
extern volatile unsigned char  near     ep_mode;            /* 0x1fe1: R/W */
extern volatile unsigned char  near     ep_pin;             /* 0x1fe7: R/W */
extern volatile unsigned char  near     ep_reg;             /* 0x1fe5: R/W */
extern volatile unsigned char  near     epdir;              /* 0x1fe3: R/W */
extern volatile unsigned char  near     epmode;             /* 0x1fe1: R/W */
extern volatile unsigned char  near     eppin;              /* 0x1fe7: R/W */
extern volatile unsigned char  near     epreg;              /* 0x1fe5: R/W */
/* Serial I/O and PWM SFR's                                          */
extern volatile unsigned char  near     con_reg0;           /* 0x1fb6: R/W */
extern volatile unsigned char  register con_reg0_7d;        /* 0x00f6: R/W */
extern volatile unsigned char  register con_reg0_3e;        /* 0x00f6: R/W */
extern volatile unsigned char  register con_reg0_1f;        /* 0x00b6: R/W */

extern volatile unsigned char  near     pwm0_control;       /* 0x1fb0: R/W */
extern volatile unsigned char  register pwm0_control_7d;    /* 0x00f0: R/W */
extern volatile unsigned char  register pwm0_control_3e;    /* 0x00f0: R/W */
extern volatile unsigned char  register pwm0_control_1f;    /* 0x00b0: R/W */

extern volatile unsigned char  near     pwm1_control;       /* 0x1fb2: R/W */
extern volatile unsigned char  register pwm1_control_7d;    /* 0x00f2: R/W */
extern volatile unsigned char  register pwm1_control_3e;    /* 0x00f2: R/W */
extern volatile unsigned char  register pwm1_control_1f;    /* 0x00b2: R/W */

extern volatile unsigned char  near     pwm2_control;       /* 0x1fb4: R/W */
extern volatile unsigned char  register pwm2_control_7d;    /* 0x00f4: R/W */
extern volatile unsigned char  register pwm2_control_3e;    /* 0x00f4: R/W */
extern volatile unsigned char  register pwm2_control_1f;    /* 0x00b4: R/W */

extern volatile unsigned char  near     sbuf_rx;            /* 0x1fb8: R/W */
extern volatile unsigned char  register sbuf_rx_7d;         /* 0x00f8: R/W */
extern volatile unsigned char  register sbuf_rx_3e;         /* 0x00f8: R/W */
extern volatile unsigned char  register sbuf_rx_1f;         /* 0x00b8: R/W */

extern volatile unsigned char  near     sbuf_tx;            /* 0x1fba: R/W */
extern volatile unsigned char  register sbuf_tx_7d;         /* 0x00fa: R/W */
extern volatile unsigned char  register sbuf_tx_3e;         /* 0x00fa: R/W */
extern volatile unsigned char  register sbuf_tx_1f;         /* 0x00ba: R/W */

extern volatile unsigned short near     sp_baud;            /* 0x1fbc: R/W */
extern volatile unsigned short register sp_baud_7d;         /* 0x00fc: R/W */
extern volatile unsigned short register sp_baud_3e;         /* 0x00fc: R/W */
extern volatile unsigned short register sp_baud_1f;         /* 0x00bc: R/W */

extern volatile unsigned short near     sp_baud_count;      /* 0x1fbe: R/W */
extern volatile unsigned short register sp_baud_count_7d;   /* 0x00fe: R/W */
extern volatile unsigned short register sp_baud_count_3e;   /* 0x00fe: R/W */
extern volatile unsigned short register sp_baud_count_1f;   /* 0x00be: R/W */

extern volatile unsigned char  near     sp_con;             /* 0x1fbb: R/W */
extern volatile unsigned char  register sp_con_7d;          /* 0x00fb: R/W */
extern volatile unsigned char  register sp_con_3e;          /* 0x00fb: R/W */
extern volatile unsigned char  register sp_con_1f;          /* 0x00bb: R/W */

extern volatile unsigned char  near     sp_control;         /* 0x1fbb: R/W */
extern volatile unsigned char  register sp_control_7d;      /* 0x00fb: R/W */
extern volatile unsigned char  register sp_control_3e;      /* 0x00fb: R/W */
extern volatile unsigned char  register sp_control_1f;      /* 0x00bb: R/W */

extern volatile unsigned char  near     sp_stat;            /* 0x1fb9: R/W */
extern volatile unsigned char  register sp_stat_7d;         /* 0x00f9: R/W */
extern volatile unsigned char  register sp_stat_3e;         /* 0x00f9: R/W */
extern volatile unsigned char  register sp_stat_1f;         /* 0x00b9: R/W */

extern volatile unsigned char  near     sp_status;          /* 0x1fb9: R/W */
extern volatile unsigned char  register sp_status_7d;       /* 0x00f9: R/W */
extern volatile unsigned char  register sp_status_3e;       /* 0x00f9: R/W */
extern volatile unsigned char  register sp_status_1f;       /* 0x00b9: R/W */

/* A/D SFR's                                                         */
extern volatile unsigned char  near     ad_command;         /* 0x1fac: R/W */
extern volatile unsigned char  register ad_command_7d;      /* 0x00ec: R/W */
extern volatile unsigned char  register ad_command_3e;      /* 0x00ec: R/W */
extern volatile unsigned char  register ad_command_1f;      /* 0x00ac: R/W */

extern volatile unsigned short near     ad_result;          /* 0x1faa: R/W */
extern volatile unsigned short register ad_result_7d;       /* 0x00ea: R/W */
extern volatile unsigned short register ad_result_3e;       /* 0x00ea: R/W */
extern volatile unsigned short register ad_result_1f;       /* 0x00aa: R/W */

extern volatile unsigned char  near     ad_test;            /* 0x1fae: R/W */
extern volatile unsigned char  register ad_test_7d;         /* 0x00ee: R/W */
extern volatile unsigned char  register ad_test_3e;         /* 0x00ee: R/W */
extern volatile unsigned char  register ad_test_1f;         /* 0x00ae: R/W */

extern volatile unsigned char  near     ad_time;            /* 0x1faf: R/W */
extern volatile unsigned char  register ad_time_7d;         /* 0x00ef: R/W */
extern volatile unsigned char  register ad_time_3e;         /* 0x00ef: R/W */
extern volatile unsigned char  register ad_time_1f;         /* 0x00af: R/W */

/* EPA, Timer 1 and 2 SFR's                                          */
extern volatile unsigned char  near     epa_mask;           /* 0x1f9c: R/W */
extern volatile unsigned char  register epa_mask_7c;        /* 0x00fc: R/W */
extern volatile unsigned char  register epa_mask_3e;        /* 0x00dc: R/W */
extern volatile unsigned char  register epa_mask_1f;        /* 0x009c: R/W */

extern volatile unsigned char  near     epa_stat;           /* 0x1f9e: R/W */
extern volatile unsigned char  register epa_stat_7c;        /* 0x00fe: R/W */
extern volatile unsigned char  register epa_stat_3e;        /* 0x00de: R/W */
extern volatile unsigned char  register epa_stat_1f;        /* 0x009e: R/W */

extern volatile unsigned char  near     epa_pend;           /* 0x1f9e: R/W */
extern volatile unsigned char  register epa_pend_7c;        /* 0x00fe: R/W */
extern volatile unsigned char  register epa_pend_3e;        /* 0x00de: R/W */
extern volatile unsigned char  register epa_pend_1f;        /* 0x009e: R/W */

extern volatile unsigned char  near     t1control;          /* 0x1f90: R/W */
extern volatile unsigned char  register t1control_7c;       /* 0x00f0: R/W */
extern volatile unsigned char  register t1control_3e;       /* 0x00d0: R/W */
extern volatile unsigned char  register t1control_1f;       /* 0x0090: R/W */

extern volatile unsigned char  near     t2control;          /* 0x1f94: R/W */
extern volatile unsigned char  register t2control_7c;       /* 0x00f4: R/W */
extern volatile unsigned char  register t2control_3e;       /* 0x00d4: R/W */
extern volatile unsigned char  register t2control_1f;       /* 0x0094: R/W */

extern volatile unsigned short near     timer1;             /* 0x1f92: R/W */
extern volatile unsigned short register timer1_7c;          /* 0x00f2: R/W */
extern volatile unsigned short register timer1_3e;          /* 0x00d2: R/W */
extern volatile unsigned short register timer1_1f;          /* 0x0092: R/W */

extern volatile unsigned char  near     timer1_control;     /* 0x1f90: R/W */
extern volatile unsigned char  register timer1_control_7c;  /* 0x00f0: R/W */
extern volatile unsigned char  register timer1_control_3e;  /* 0x00d0: R/W */
extern volatile unsigned char  register timer1_control_1f;  /* 0x0090: R/W */

extern volatile unsigned short near     timer2;             /* 0x1f96: R/W */
extern volatile unsigned short register timer2_7c;          /* 0x00f6: R/W */
extern volatile unsigned short register timer2_3e;          /* 0x00d6: R/W */
extern volatile unsigned short register timer2_1f;          /* 0x0096: R/W */

extern volatile unsigned char  near     timer2_control;     /* 0x1f94: R/W */
extern volatile unsigned char  register timer2_control_7c;  /* 0x00f4: R/W */
extern volatile unsigned char  register timer2_control_3e;  /* 0x00d4: R/W */
extern volatile unsigned char  register timer2_control_1f;  /* 0x0094: R/W */

/* EPA SFR's                                                         */
extern volatile unsigned char  near     epa0_con;           /* 0x1f80: R/W */
extern volatile unsigned char  register epa0_con_7c;        /* 0x00e0: R/W */
extern volatile unsigned char  register epa0_con_3e;        /* 0x00c0: R/W */
extern volatile unsigned char  register epa0_con_1f;        /* 0x0080: R/W */

extern volatile unsigned short near     epa0_time;          /* 0x1f82: R/W */
extern volatile unsigned short register epa0_time_7c;       /* 0x00e2: R/W */
extern volatile unsigned short register epa0_time_3e;       /* 0x00c2: R/W */
extern volatile unsigned short register epa0_time_1f;       /* 0x0082: R/W */

extern volatile unsigned short near     epa1_con;           /* 0x1f84: R/W */
extern volatile unsigned short register epa1_con_7c;        /* 0x00e4: R/W */
extern volatile unsigned short register epa1_con_3e;        /* 0x00c4: R/W */
extern volatile unsigned short register epa1_con_1f;        /* 0x0084: R/W */

extern volatile unsigned short near     epa1_time;          /* 0x1f86: R/W */
extern volatile unsigned short register epa1_time_7c;       /* 0x00e6: R/W */
extern volatile unsigned short register epa1_time_3e;       /* 0x00c6: R/W */
extern volatile unsigned short register epa1_time_1f;       /* 0x0086: R/W */

extern volatile unsigned char  near     epa2_con;           /* 0x1f88: R/W */
extern volatile unsigned char  register epa2_con_7c;        /* 0x00e8: R/W */
extern volatile unsigned char  register epa2_con_3e;        /* 0x00c8: R/W */
extern volatile unsigned char  register epa2_con_1f;        /* 0x0088: R/W */

extern volatile unsigned short near     epa2_time;          /* 0x1f8a: R/W */
extern volatile unsigned short register epa2_time_7c;       /* 0x00ea: R/W */
extern volatile unsigned short register epa2_time_3e;       /* 0x00ca: R/W */
extern volatile unsigned short register epa2_time_1f;       /* 0x008a: R/W */

extern volatile unsigned short near     epa3_con;           /* 0x1f8c: R/W */
extern volatile unsigned short register epa3_con_7c;        /* 0x00ec: R/W */
extern volatile unsigned short register epa3_con_3e;        /* 0x00cc: R/W */
extern volatile unsigned short register epa3_con_1f;        /* 0x008c: R/W */

extern volatile unsigned short near     epa3_time;          /* 0x1f8e: R/W */
extern volatile unsigned short register epa3_time_7c;       /* 0x00ee: R/W */
extern volatile unsigned short register epa3_time_3e;       /* 0x00ce: R/W */
extern volatile unsigned short register epa3_time_1f;       /* 0x008e: R/W */

extern volatile unsigned char  near     epa_con0;           /* 0x1f80: R/W */
extern volatile unsigned char  register epa_con0_7c;        /* 0x00e0: R/W */
extern volatile unsigned char  register epa_con0_3e;        /* 0x00c0: R/W */
extern volatile unsigned char  register epa_con0_1f;        /* 0x0080: R/W */

extern volatile unsigned short near     epa_con1;           /* 0x1f84: R/W */
extern volatile unsigned short register epa_con1_7c;        /* 0x00e4: R/W */
extern volatile unsigned short register epa_con1_3e;        /* 0x00c4: R/W */
extern volatile unsigned short register epa_con1_1f;        /* 0x0084: R/W */

extern volatile unsigned char  near     epa_con2;           /* 0x1f88: R/W */
extern volatile unsigned char  register epa_con2_7c;        /* 0x00e8: R/W */
extern volatile unsigned char  register epa_con2_3e;        /* 0x00c8: R/W */
extern volatile unsigned char  register epa_con2_1f;        /* 0x0088: R/W */

extern volatile unsigned short near     epa_con3;           /* 0x1f8c: R/W */
extern volatile unsigned short register epa_con3_7c;        /* 0x00ec: R/W */
extern volatile unsigned short register epa_con3_3e;        /* 0x00cc: R/W */
extern volatile unsigned short register epa_con3_1f;        /* 0x008c: R/W */

extern volatile unsigned short near     epa_time0;          /* 0x1f82: R/W */
extern volatile unsigned short register epa_time0_7c;       /* 0x00e2: R/W */
extern volatile unsigned short register epa_time0_3e;       /* 0x00c2: R/W */
extern volatile unsigned short register epa_time0_1f;       /* 0x0082: R/W */

extern volatile unsigned short near     epa_time1;          /* 0x1f86: R/W */
extern volatile unsigned short register epa_time1_7c;       /* 0x00e6: R/W */
extern volatile unsigned short register epa_time1_3e;       /* 0x00c6: R/W */
extern volatile unsigned short register epa_time1_1f;       /* 0x0086: R/W */

extern volatile unsigned short near     epa_time2;          /* 0x1f8a: R/W */
extern volatile unsigned short register epa_time2_7c;       /* 0x00ea: R/W */
extern volatile unsigned short register epa_time2_3e;       /* 0x00ca: R/W */
extern volatile unsigned short register epa_time2_1f;       /* 0x008a: R/W */

extern volatile unsigned short near     epa_time3;          /* 0x1f8e: R/W */
extern volatile unsigned short register epa_time3_7c;       /* 0x00ee: R/W */
extern volatile unsigned short register epa_time3_3e;       /* 0x00ce: R/W */
extern volatile unsigned short register epa_time3_1f;       /* 0x008e: R/W */

/* ChipSelect SFR's                                                  */
extern volatile unsigned short near     addrcom0;           /* 0x1f40: R/W */
extern volatile unsigned short register addrcom0_7a;        /* 0x00e0: R/W */
extern volatile unsigned short register addrcom0_3d;        /* 0x00c0: R/W */
extern volatile unsigned short register addrcom0_1e;        /* 0x00c0: R/W */

extern volatile unsigned short near     addrcom1;           /* 0x1f48: R/W */
extern volatile unsigned short register addrcom1_7a;        /* 0x00e8: R/W */
extern volatile unsigned short register addrcom1_3d;        /* 0x00c8: R/W */
extern volatile unsigned short register addrcom1_1e;        /* 0x00c8: R/W */

extern volatile unsigned short near     addrcom2;           /* 0x1f50: R/W */
extern volatile unsigned short register addrcom2_7a;        /* 0x00f0: R/W */
extern volatile unsigned short register addrcom2_3d;        /* 0x00d0: R/W */
extern volatile unsigned short register addrcom2_1e;        /* 0x00d0: R/W */

extern volatile unsigned short near     addrcom3;           /* 0x1f58: R/W */
extern volatile unsigned short register addrcom3_7a;        /* 0x00f8: R/W */
extern volatile unsigned short register addrcom3_3d;        /* 0x00d8: R/W */
extern volatile unsigned short register addrcom3_1e;        /* 0x00d8: R/W */

extern volatile unsigned short near     addrcom4;           /* 0x1f60: R/W */
extern volatile unsigned short register addrcom4_7b;        /* 0x00e0: R/W */
extern volatile unsigned short register addrcom4_3d;        /* 0x00e0: R/W */
extern volatile unsigned short register addrcom4_1e;        /* 0x00e0: R/W */

extern volatile unsigned short near     addrcom5;           /* 0x1f68: R/W */
extern volatile unsigned short register addrcom5_7b;        /* 0x00e8: R/W */
extern volatile unsigned short register addrcom5_3d;        /* 0x00e8: R/W */
extern volatile unsigned short register addrcom5_1e;        /* 0x00e8: R/W */

extern volatile unsigned short near     addrmsk0;           /* 0x1f42: R/W */
extern volatile unsigned short register addrmsk0_7a;        /* 0x00e2: R/W */
extern volatile unsigned short register addrmsk0_3d;        /* 0x00c2: R/W */
extern volatile unsigned short register addrmsk0_1e;        /* 0x00c2: R/W */

extern volatile unsigned short near     addrmsk1;           /* 0x1f4a: R/W */
extern volatile unsigned short register addrmsk1_7a;        /* 0x00ea: R/W */
extern volatile unsigned short register addrmsk1_3d;        /* 0x00ca: R/W */
extern volatile unsigned short register addrmsk1_1e;        /* 0x00ca: R/W */

extern volatile unsigned short near     addrmsk2;           /* 0x1f52: R/W */
extern volatile unsigned short register addrmsk2_7a;        /* 0x00f2: R/W */
extern volatile unsigned short register addrmsk2_3d;        /* 0x00d2: R/W */
extern volatile unsigned short register addrmsk2_1e;        /* 0x00d2: R/W */

extern volatile unsigned short near     addrmsk3;           /* 0x1f5a: R/W */
extern volatile unsigned short register addrmsk3_7a;        /* 0x00fa: R/W */
extern volatile unsigned short register addrmsk3_3d;        /* 0x00da: R/W */
extern volatile unsigned short register addrmsk3_1e;        /* 0x00da: R/W */

extern volatile unsigned short near     addrmsk4;           /* 0x1f62: R/W */
extern volatile unsigned short register addrmsk4_7b;        /* 0x00e2: R/W */
extern volatile unsigned short register addrmsk4_3d;        /* 0x00e2: R/W */
extern volatile unsigned short register addrmsk4_1e;        /* 0x00e2: R/W */

extern volatile unsigned short near     addrmsk5;           /* 0x1f6a: R/W */
extern volatile unsigned short register addrmsk5_7b;        /* 0x00ea: R/W */
extern volatile unsigned short register addrmsk5_3d;        /* 0x00ea: R/W */
extern volatile unsigned short register addrmsk5_1e;        /* 0x00ea: R/W */

extern volatile unsigned char  near     buscon0;            /* 0x1f44: R/W */
extern volatile unsigned char  register buscon0_7a;         /* 0x00e4: R/W */
extern volatile unsigned char  register buscon0_3d;         /* 0x00c4: R/W */
extern volatile unsigned char  register buscon0_1e;         /* 0x00c4: R/W */

extern volatile unsigned char  near     buscon1;            /* 0x1f4c: R/W */
extern volatile unsigned char  register buscon1_7a;         /* 0x00ec: R/W */
extern volatile unsigned char  register buscon1_3d;         /* 0x00cc: R/W */
extern volatile unsigned char  register buscon1_1e;         /* 0x00cc: R/W */

extern volatile unsigned char  near     buscon2;            /* 0x1f54: R/W */
extern volatile unsigned char  register buscon2_7a;         /* 0x00f4: R/W */
extern volatile unsigned char  register buscon2_3d;         /* 0x00d4: R/W */
extern volatile unsigned char  register buscon2_1e;         /* 0x00d4: R/W */

extern volatile unsigned char  near     buscon3;            /* 0x1f5c: R/W */
extern volatile unsigned char  register buscon3_7a;         /* 0x00fc: R/W */
extern volatile unsigned char  register buscon3_3d;         /* 0x00dc: R/W */
extern volatile unsigned char  register buscon3_1e;         /* 0x00dc: R/W */

extern volatile unsigned char  near     buscon4;            /* 0x1f64: R/W */
extern volatile unsigned char  register buscon4_7b;         /* 0x00e4: R/W */
extern volatile unsigned char  register buscon4_3d;         /* 0x00e4: R/W */
extern volatile unsigned char  register buscon4_1e;         /* 0x00e4: R/W */

extern volatile unsigned char  near     buscon5;            /* 0x1f6c: R/W */
extern volatile unsigned char  register buscon5_7b;         /* 0x00ec: R/W */
extern volatile unsigned char  register buscon5_3d;         /* 0x00ec: R/W */
extern volatile unsigned char  register buscon5_1e;         /* 0x00ec: R/W */


#endif /* _np_sfrs_h */
