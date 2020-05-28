#pragma once
#include "commandCommon.h"

#if 0
#define BOARD1_MOTORDRIVER		//	1st board without WROOM
#elif 0
#define BOARD2_COMBINATION		//	2nd board with WROOM
#elif 0
#define BOARD3_SEPARATE         //	3nd board WROOM only / pic only New
#elif 1
#define BOARD4                  //	4th board WROOM Updated (new pin assing) and current read fast RX PIC board
#elif 1
# ifdef PIC
# define BOARD4
# else
# define BOARD3_SEPARATE
#endif

#endif

#ifndef WROOM

#if defined BOARD1_MOTORDRIVER
 CHOOSE_BoardInfo(B1F);	//	B1M or B1F
 #define BOARD_ID	1
#elif defined BOARD2_COMBINATION
 CHOOSE_BoardInfo(B2M);	//	B2M or B2F
 #define BOARD_ID	1
#elif defined BOARD3_SEPARATE
 CHOOSE_BoardInfo(B3M);	//	B3M or B3F
 #define BOARD_ID	1
#elif defined BOARD4
 CHOOSE_BoardInfo(B3M);	//	B3M or B3F
 #define BOARD_ID	1
#endif

#endif
