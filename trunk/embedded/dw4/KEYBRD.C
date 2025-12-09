#include "globdata.h"
#include "keybrd.h"

//***********************************************************************
//* FILE        : keybrd.c
//*
//* DESCRIPTION : Keyboard rutines
//*
//***********************************************************************

//--------------------------------------------------------------
// PUBLIC functions:
//--------------------------------------------------------------
void init_key_board(void);	// Initialize
byte kb_hit(void);		// Check if any key in keyboard-buffer.
byte get_key(void);		// Read key from keyboard-buffer
void put_key(byte NewKey);	// Puts a key into the keyboard-buffer
void scan_key_board(void);
//--------------------------------------------------------------


void init_key_board()
{
  //Keyboard
  NewKey    = NO_KEY;
  NextKey   = NO_KEY;
  KeyBuffer = NO_KEY;
  KeyState  = 0;
}


byte kb_hit(void)
{
  return (KeyBuffer);
}


byte get_key(void)
{
  byte Key;

  Key = NO_KEY;
  if (KeyBuffer) {
     Key = KeyBuffer;
     KeyBuffer = NO_KEY;  //Clear buffer
  }
  return Key;
}


void put_key(byte NewKey)
{
  KeyBuffer = NewKey;
}


void scan_key_board(void)
{

   NextKey = Keypad_In;

   if (NextKey == NO_KEY)    // If no key pressed
   {
       KeyState = 0;
       return;
   }

   switch(KeyState)
   {
     case 0 : NewKey = Keypad_In;
	      KeyState = 1;
	      break;

     case 1 : if (NextKey == NewKey)  //After debouncing (20mS)
	      {
		 KeyBuffer = NewKey;   	//Update KeyBuffer-always override, later buffer ?
		 LastKey  = NewKey;   	//Update global LastKey.
		 Beep_Cnt = 5;		// A very short BEEP.
		 KeyState = 2;
	      }
	      else
		 KeyState = 0;  //
	      break;

     case 2 : if  (NextKey == UP_KEY || NextKey == DOWN_KEY)	//After (40mS)
		KeyState = 3;	// Repeat if Up/Down
	      break;    	// else Stay here until key unpressed

     case 3 : KeyState++;	//After (60mS)
	      break;
     case 4 : KeyState++;	//After (80mS)
	      break;
     case 5 : KeyState++;	//After (100mS)
	      break;
     case 6 : KeyState++;	//After (120mS)
	      break;
     case 7 : KeyState++;	//After (140mS)
	      break;
     case 8 : KeyState++;	//After (160mS)
	      break;

     case 9 : KeyState = 0;	//After (180mS) Repetition: 5.55 pr. sec
	      break;

   }
}

