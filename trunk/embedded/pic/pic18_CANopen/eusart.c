#include "eusart.h"
void InitializeEUSART(unsigned baud)
{
	// WITH ADVANCED BAUD CONTROL (BRG16 = 1)
	// baud = fosc / ( 4 * (spbrg + 1))
	// spbrg = (fosc /(4 * baud)) - 1

	unsigned spbrg;
	switch(baud)
	{
/*		// 8 MHZ: ----------------
		case 115200:
			// spbrg = (8000000 / (4 * 115200)) - 1
			// spbrg = 16
			spbrg = 16;
			break;
		case 9600:				
			// spbrg = (8000000 / (4 * 9600)) - 1
			// spbrg = 207
			spbrg = 207;
			break; // */

/*		// 16 MHZ: ---------------
		case 115200:
			// spbrg = (16000000 / (4 * 115200)) - 1
			// spbrg = 33
			spbrg = 33;
			break;
		case 9600:				
			// spbrg = (16000000 / (4 * 9600)) - 1
			// spbrg = 415
			spbrg = 415; //
			break; */

/*		// 25 MHZ: ---------------
		case 115200:
			// spbrg = (25000000 / (4 * 115200)) - 1
			// spbrg = 53
			spbrg = 53;
			break;
		case 9600:				
			// spbrg = (25000000 / (4 * 9600)) - 1
			// spbrg = 650
			spbrg = 650; //
			break;  // */

		// 64 MHZ: ---------------
		case 115200:
			// spbrg = (64000000 / (4 * 115200)) - 1
			// spbrg = 137
			spbrg = 138;
			break;
		case 9600:				
			// spbrg = (64000000 / (4 * 9600)) - 1
			// spbrg = 1665
			spbrg = 1665; // luckily this is a 16 bit value!
			break;  // 
	}
	
	Open1USART( 
		USART_TX_INT_OFF	& 
		USART_RX_INT_OFF	& 
		USART_ASYNCH_MODE	& 
		USART_EIGHT_BIT		& 
		USART_CONT_RX		&
		USART_BRGH_HIGH,
		spbrg				);

	baud1USART(
		BAUD_IDLE_RX_PIN_STATE_HIGH &
		BAUD_IDLE_TX_PIN_STATE_HIGH &
//		BAUD_IDLE_CLK_LOW	&	// FUCKING DATA SHEET IS WRONG -- YOU MUST CONTROL BOTH RX AND TX IDLE STATE!
		BAUD_16_BIT_RATE	&
		BAUD_WAKEUP_OFF		&
		BAUD_AUTO_OFF		);
}

