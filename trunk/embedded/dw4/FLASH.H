//--------------------------------------------------------------
// Define programmable area limits (from locate.txt):
//--------------------------------------------------------------
// rom(0F80000h-0FBFFFFh(sin_tab), &	; 256k, sound data.
//     0FC0000h-0FCFFFFh(eventdat), &	;  64k, event log
//     0FD0000h-0FDEFFFh(flashdat), &	;  60k, subprograms & programmable data
//     0FDF000h-0FDFFFFh(new_prog), &	;   4k, protected code (not downloadable)
//     0FE0000h-0FFFEFFh) &		; 128k, program code (last sector not used!)//--------------------------------------------------------------

//--------------------------------------------------------------
// flashprg.c:
//--------------------------------------------------------------

#define FLASH_DATA_START  0x00FC0000L	// Programmable area!
#define FLASH_DATA_END	  0x00FDEFFFL	// Programmable area!

//--------------------------------------------------------------
// new_prog.c:
//--------------------------------------------------------------

#define PROTECTED_START  0x00FDF000L
#define PROTECTED_END    0x00FDFFFFL

#define SECTOR_SIZE	256	// Size of sectors in Atmel Flash
#define RAMCODE_SIZE	512	// Space for copying the programming function to RAM.

