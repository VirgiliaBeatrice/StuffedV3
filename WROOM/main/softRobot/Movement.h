#ifndef _SR_MOVEMENT_H
#define _SR_MOVEMENT_H

#include <stdio.h>

#ifdef __cplusplus
#include <vector>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/semphr.h>
#include <freertos/queue.h>
#include <driver/periph_ctrl.h>
#include <driver/timer.h>

#include "AllBoards.h"
#include "Utils.h"

using namespace std;

typedef struct MotorKeyframeNode MotorKeyframeNode;
typedef struct MotorHead MotorHead;
typedef struct MovementInfoNode MovementInfoNode;

struct MotorKeyframeNode {
	uint16_t id;              // id of the keyframe (movement id + index)
	uint16_t start;           // start time
	uint16_t end;          // end time = start time + period
	int32_t pose;                     // target pose
	MotorKeyframeNode* next; // next node
};

struct MotorHead {
	uint8_t nOccupied;        // count of occupied buffer, includes paused keyframes

	// interpolate parameters, please refer to Bresenham's Line Algorithm
	int32_t currentPose;	// current pose of motor
	uint16_t nextTime;	// the differntial should change when time exceeds nextTime
	bool minus;
	uint16_t slopeInteger;
	uint16_t slopeDecimal;
	uint16_t slopeError;

	MotorKeyframeNode* head; // head of linked list of keyframes
	MotorKeyframeNode* read; // last node of read (since multi keyframes is used in interpolate)
};

struct MovementInfoNode {
	uint8_t movementId;		// movement id
	uint8_t lastDeletedKeyframeId;	// index of last deleted keyframe after interpolation (0xff for none)
	uint8_t lastAddedKeyframeId;	// index of last added keyframe of this movement
	uint16_t remainKeyframeTime;	// FIXME not work well after pause movement, DONNOT USE // sum of time of remaining keyframes (in movement tick, NOT ms)
	uint8_t keyframeCount;	// number of remaining keyframes
	uint8_t movementCount;	// number of remaining movement instance
	bool paused;			// true if the movement is currently paused
};

extern vector<MotorHead> motorHeads;
extern vector<MovementInfoNode> movementInfos;

vector<MovementInfoNode>::iterator getMovementInfo(uint8_t movementId);

///////////////////////////////////////// data structure for WROOM interface /////////////////////////////

#define MS_PER_MOVEMENT_TICK 50				// 50 ms = 1 movement tick

class MovementKeyframe {
public:
	uint16_t id;				// 8-bit movement id (start from 1) + 8-bit keyframe id
	uint8_t motorCount;			// count of motors used in the movement
	vector<uint8_t> motorId;	// the motorIds used
	uint16_t period;			// note that 1. the sum of period in list could not larger than UINT16_MAX (or the sorting might fail); 2. time in movement tick, not ms
	vector<int32_t> pose;			// the poses correspond with motorIds

	uint16_t refId;             // 8-bit movement id (start from 1) + 8-bit keyframe id
								// NOTE When Movement id == 0: If keyframe id ==	0, NO reference
								// 													else, after movement id = keyframe id
	uint8_t refMotorId;
	short timeOffset;

	bool strictMode;
};

struct MovementKeyframeAddState {
    uint16_t id;
    uint8_t success;
}__attribute__((packed));

/////////////////////////////////////////// api for accessing PIC ///////////////////////////////////////////////
#define PIC_INTERPOLATE_BUFFER_VACANCY_MIN 8		// the minimum empty interpolate buffer count
void movementQueryInterpolateState();
void movementOnGetPICInfo(UdpRetPacket& pkt);

/////////////////////////////////////////// debug ///////////////////////////////////////////////////////
#define MOVEMENT_DEBUG 1
void printMotorKeyframes(uint8_t motorId);
void printKeyframe(const MovementKeyframe &keyframe);
void printInterpolateParams();
void printAllMotorKeyframes();
void printMovementInfo();

/////////////////////////////////////////// interface to hardware ///////////////////////////////////////
#define MOVEMENT_MANAGER_TIMER_DIVIDER 16
#define MOVEMENT_MANAGER_TIMER_GROUP TIMER_GROUP_0
#define MOVEMENT_MANAGER_TIMER_IDX TIMER_0
#define MOVEMENT_MANAGER_TIMER_SCALE (TIMER_BASE_CLK / MOVEMENT_MANAGER_TIMER_DIVIDER / 1000)  // convert counter value to ms

void initMovementDS();

void onChangeControlMode(CommandId newCommand, const short* data);

/////////////////////////////////////////// api for command packet ///////////////////////////////////////////////

bool canAddKeyframe(MovementKeyframe& keyframe);
struct MotorKeyframeNode* addKeyframe(MovementKeyframe& keyframe);

void pauseInterpolate();
void resumeInterpolate();
void clearMovement(uint8_t movementId, uint8_t motorCount, const vector<uint8_t> motorId);

void pauseMovement(uint8_t movementId, uint8_t motorCount, const vector<uint8_t> &motorId);
void resumeMovement(uint8_t movementId, uint8_t motorCount);

void clearPausedMovements();
void clearInterpolateBuffer();

/////////////////////////////////////////// api for execute and return packet ///////////////////////////////////////////////
void prepareRetAddKeyframe(const void* movement_command_data_rcv, void* movement_command_data_ret);
void prepareRetPauseMov(const void* movement_command_data_rcv, void* movement_command_data_ret);
void prepareRetResumeMov(const void* movement_command_data_rcv, void* movement_command_data_ret);
void prepareRetClearMov(const void* movement_command_data_rcv, void* movement_command_data_ret);
void prepareRetQuery(const void* movement_command_data_rcv, void* movement_command_data_ret);

#else

void movementAfterStopJSTask();

#endif

#endif
