#pragma once
#include "BoardBase.h"
extern "C" {
#include "../../../PIC/control.h"
}

struct RobotState: public BoardRetBase{
	RobotState();
	//	robot's state
	ControlMode mode;				//	PIC/control.h
	unsigned char nTargetMin;			//	nTaret for all board
	unsigned char nTargetVacancy;		//	nTargetVecancy for all board
	unsigned char nTargetRemain;		//	minimum remaining targets in the board. Must be >= 3.
	unsigned char targetCountWrite;		//	targetCount for next writing. 
	unsigned char targetCountReadMax;	//	targetCount for read of one of the board.
	unsigned short tickMin;
	unsigned short tickMax;
	tiny::vector<SDEC> position;		//	motor position
	tiny::vector<SDEC> velocity;		//	motor velocitry
	tiny::vector<SDEC> current;			//	current sensor for motor;
	tiny::vector<SDEC> force;			//	force sensor
	tiny::vector<SDEC> touch;			//	touch sensor

	void SetControlMode(short cm){
		mode = (ControlMode)cm;
	}
	void SetMotorPos(short p, int i) {
		position[i] = p;
	}
	void SetMotorVel(short v, int i) {
		velocity[i] = v;
	}
	//	for interpolate and force control
	void SetTargetCountRead(unsigned char c) {
		targetCountReadMax = c;
	}
	void SetTickMin(short t) {
		tickMin = t;
	}
	void SetTickMax(short t) {
		tickMax = t;
	}
	void SetNTargetRemain(unsigned char t){
		nTargetRemain = t;
	}
	void SetNTargetVacancy(unsigned char t){
		nTargetVacancy = t;
	}
	//	sense
	void SetCurrent(short c, int i) {
		current[i] = c;
	}
	void SetForce(short f, int i) {
		force[i] = f;
	}
	void SetTouch(short t, int i) {
		touch[i] = t;
	}
	void SetBoardInfo(int systemId, int nTarget, int nMotor, int nCurrent, int nForce, int nTouch) {
		//	Do nothing. Only one instance (AllBoards::state is initialzed in AllBoards::EnumerateBoards() ). 
	}
};
