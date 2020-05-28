#include "AllBoards.h"
#include "UartForBoards.h"
#include "Board.h"
#include "Movement.h"
#include "driver/uart.h"
#include "CPPNVS.h"

//	PIN definition
#if defined BOARD3_SEPARATE || defined BOARD4
#define U1TXPIN	16
#define U1RXPIN	17
#define U2TXPIN	5
#define U2RXPIN	18
#elif defined BOARD2_COMBINATION
#define U1TXPIN	16
#define U1RXPIN	17
#define U2TXPIN	5
#define U2RXPIN	18
#elif defined BOARD1_MOTORDRIVER
#define U1TXPIN	33
#define U1RXPIN	32
#define U2TXPIN	17
#define U2RXPIN	16
#else
#error
#endif


AllBoards allBoards;
UartForBoards uart1(UART_NUM_1, &allBoards);
UartForBoards uart2(UART_NUM_2, &allBoards);
AllBoards::AllBoards(): motorPos(NULL), motorOffset(NULL){
}
AllBoards::~AllBoards(){
	delete boardDirect;
}
static void execLoop(void* arg){
	((AllBoards*)arg)->ExecLoop();
}
void AllBoards::ExecLoop(){
	while (1){
		UdpCmdPacket* recv = &udpCom.recvs.Peek();

		// change resume/pause movement manager
		if (recv->count == CS_DUKTAPE || recv->count == CS_WEBSOCKET || recv->returnIp.u_addr.ip4.addr) {
			onChangeControlMode((CommandId)recv->command, recv->data);
		}

		if (recv->command != 7){
			ESP_LOGD(Tag(), "ExecLoop(): command %d received.", recv->command);
		}else{
			ESP_LOGV(Tag(), "ExecLoop(): command %d received.", recv->command);
		}
		if (CI_BOARD_INFO < recv->command && recv->command < CI_NCOMMAND) {
			//	send packet to allBoards
			ESP_LOGV(Tag(), "ExecLoop(): write cmd %d.", recv->command);
			WriteCmd(recv->command, *recv);
			if (recv->command == CI_RESET_SENSOR){
				short rsf = recv->GetResetSensorFlags();
				if (rsf & RSF_MOTOR){
					for(int i=0; i<motorMap.size(); ++i){
						motorPos[i] = motorPos[i] + motorOffset[i];		//	motorPos comes to board's value
						motorOffset[i] = motorPos[i] % SDEC_ONE;		//	resting offset in board
						motorPos[i] = 0;								//	set motorPos to 0.
					}
					ESP_LOGD(Tag(), "ExecLoop(): motorPos reset.");
				}
			}
			udpCom.PrepareRetPacket(*recv);
			if (HasRet(recv->command)){
				ReadRet(recv->command, udpCom.send);
				ESP_LOGV(Tag(), "ExecLoop() read ret %d.", recv->command);
			}
			udpCom.SendReturn(*recv);
		}
		else {
			udpCom.ExecUdpCommand(*recv);
		}
		udpCom.recvs.Read();
	}
}
void AllBoards::Init() {
	assert(NUART == 2);	//NUART must be much to followings.
	nBoard = 0;
	uart[0] = &uart1;
	uart[1] = &uart2;
	boardDirect = new BoardDirect();

	uart_config_t uconf;
	uconf.baud_rate = 2000000;
	uconf.data_bits = UART_DATA_8_BITS;
    uconf.parity = UART_PARITY_DISABLE;
    uconf.stop_bits = UART_STOP_BITS_1;
    uconf.flow_ctrl = UART_HW_FLOWCTRL_DISABLE;
    uconf.rx_flow_ctrl_thresh = 0;
    uconf.use_ref_tick = false;

	uart[0]->Init(uconf, U1TXPIN, U1RXPIN); // pin must be changed. IO6-11 are reserved. (RX=32 Yellow, TX=33 Green)
	uart[1]->Init(uconf, U2TXPIN, U2RXPIN);
	EnumerateBoard();
	xTaskCreate(execLoop, "SR_Exec", 1024*6, this, tskIDLE_PRIORITY+5, &taskExec);
}

void AllBoards::EnumerateBoard() {
	//	clear
	motorMap.clear();
	currentMap.clear();
	forceMap.clear();
	touchMap.clear();
	//	Enumerate: Start from boardDirect
	for(int m=0; m<boardDirect->GetNMotor(); ++m){
		boardDirect->motorMap.push_back((int)allBoards.motorMap.size());
		allBoards.motorMap.push_back(DeviceMap(m));
	}
	for (int m = 0; m<boardDirect->GetNCurrent(); ++m) {
		boardDirect->currentMap.push_back((int)allBoards.currentMap.size());
		allBoards.currentMap.push_back(DeviceMap(m));
	}
	for (int m = 0; m<boardDirect->GetNForce(); ++m) {
		boardDirect->forceMap.push_back((int)allBoards.forceMap.size());
		allBoards.forceMap.push_back(DeviceMap(m));
	}
	for (int m = 0; m<boardDirect->GetNTouch(); ++m) {
		boardDirect->touchMap.push_back((int)allBoards.touchMap.size());
		allBoards.touchMap.push_back(DeviceMap(m));
	}
	//	Enumerate: UARTs
	for (int i = 0; i < NUART; ++i) {
		uart[i]->EnumerateBoard(i);
	}
	motorMap.shrink_to_fit();
	currentMap.shrink_to_fit();
	forceMap.shrink_to_fit();
	touchMap.shrink_to_fit();

	//	re-allocate motor position and offset.
	if (motorPos) free((void*)motorPos);
	if (motorOffset) free(motorOffset);
	motorPos = (volatile int *) malloc(sizeof(volatile int) * motorMap.size());
	memset((void*)motorPos, 0, sizeof(int) * motorMap.size());
	motorOffset = (short*) malloc(sizeof(short) * motorMap.size());
	memset(motorOffset, 0, sizeof(short) * motorMap.size());

#ifdef SAVE_ALL_MOTOR_PARAM_ON_WROOM
	if (motorKba) free(motorKba);
	motorKba = (SDEC*) malloc(sizeof(SDEC) * 3 * motorMap.size());
	memset(motorKba, 0, sizeof(SDEC) * 3 * motorMap.size());
#endif

	//	compute nTargetMin;
	nTargetMin = boardDirect->GetNTarget();
	nBoard = 1;
	for (int i = 0; i < NUART; ++i) {
		nBoard += (int)uart[i]->boards.size();
		for (int j = 0; j < (int)uart[i]->boards.size(); ++j) {
			int nt = uart[i]->boards[j]->GetNTarget();
			nTargetMin = nt < nTargetMin ? nt : nTargetMin;
		}
	}
	//	Load motor position from NVS
	LoadMotorPos();
}

bool AllBoards::HasRet(unsigned short id){
	if (boardDirect->retPacketLen[id]) return true;
	for (int i = 0; i < NUART; ++i) {
		for (int j = 0; j < (int)uart[i]->boards.size(); ++j) {
			if (uart[i]->boards[j]->retPacketLen[id]) return true;
		}
	}
	return false;
}

void AllBoards::WriteCmd(unsigned short commandId, BoardCmdBase& packet) {
	ESP_LOGV(Tag(), "WriteCmd() cmd=%d\r\n", commandId);
	//	Copy UDP command to the command buffers of each borad;
	boardDirect->WriteCmd(commandId, packet);
	for (int i = 0; i < NUART; ++i) {
		for (int j = 0; j < (int)uart[i]->boards.size(); ++j) {
			uart[i]->boards[j]->WriteCmd(commandId, packet);
		}
	}
	//	send command to borads via uart;
	for (int i = 0; i < NUART; ++i) {
		uart[i]->SendUart();
	}
	for (int i = 0; i < NUART; ++i) {
		uart[i]->RecvUart();
	}
	if (commandId == CI_SET_PARAM){
		vTaskDelay(100 / portTICK_PERIOD_MS);
	}
}
inline void readAndGetMinMax(bool bFirst, BoardBase* board, unsigned short commandId,
	BoardRetBase& packet, unsigned char& tcrMin, unsigned char& tcrMax, unsigned short& tickMin, unsigned short& tickMax){
	board->ReadRet(commandId, packet);
	if (bFirst){
		tcrMin = tcrMax = board->GetTargetCountRead();
		tickMin = tickMax = board->GetTick();
	}else{
		unsigned char tcr = board->GetTargetCountRead();
		if (tcr - tcrMin < 0) tcrMin = tcr;
		if (tcr - tcrMax > 0) tcrMax = tcr;
		unsigned short tick = board->GetTick();
		if (tick - tickMin < 0) tickMin = tick;
		if (tick - tickMax > 0) tickMax = tick;
	}
}
void AllBoards::ReadRet(unsigned short commandId, BoardRetBase& packet){
	if (commandId == CI_INTERPOLATE || commandId == CI_FORCE_CONTROL) {
		unsigned char tcrMin, tcrMax;
		unsigned short tickMin, tickMax;
		readAndGetMinMax(true, boardDirect, commandId, packet, tcrMin, tcrMax, tickMin, tickMax);
		for (int i = 0; i < NUART; ++i) {
			for (int j = 0; j < (int)uart[i]->boards.size(); ++j) {
				readAndGetMinMax(false, uart[i]->boards[j], commandId, packet, tcrMin, tcrMax, tickMin, tickMax);
			}
		}
		packet.SetTargetCountReadMin(tcrMin);
		packet.SetTargetCountReadMax(tcrMax);
		packet.SetTickMin(tickMin);
		packet.SetTickMax(tickMax);
		ESP_LOGV(Tag(), "ReadRet(): Cor:%d--%d", (int)tcrMin, (int) tcrMax);
	}else{
		boardDirect->ReadRet(commandId, packet);
		for (int i = 0; i < NUART; ++i) {
			for (int j = 0; j < (int)uart[i]->boards.size(); ++j) {
				uart[i]->boards[j]->ReadRet(commandId, packet);
			}
		}
	}
}
BoardBase& AllBoards::Board(char uid, char bid){
	if (uid != 0xFF && bid != 0xFF){
		return *uart[(int)uid]->boards[(int)bid];
	}
	return *boardDirect;
}

#ifdef SAVE_ALL_MOTOR_PARAM_ON_WROOM
//	save and load control paramter for motors.
void AllBoards::LoadMotorParam(){
	NVS nvs("motor");
	for(int i=0; i<motorMap.size(); ++i){
		char key[]="paramK012";
		itoa(i, key+6, 10);
		int v;
		if (nvs.get(key, v) == ESP_OK) motorKba[3*i] = v;
		key[5] = 'B';
		if (nvs.get(key, v) == ESP_OK) motorKba[3*i+1] = v;
		key[5] = 'A';
		if (nvs.get(key, v) == ESP_OK) motorKba[3*2] = v;
	}
	UdpCmdPacket* recv = new UdpCmdPacket;
	recv->command = CI_SET_PARAM;
	recv->SetParamType(PT_PD);
	for(int i=0; i<motorMap.size(); ++i){
		recv->SetControlK(motorKba[3*i], i);
		recv->SetControlB(motorKba[3*i+1], i);
	}
	WriteCmd(recv->command, *recv);
	recv->SetParamType(PT_CURRENT);
	for(int i=0; i<motorMap.size(); ++i){
		recv->SetControlA(motorKba[3*i+2], i);
	}
	WriteCmd(recv->command, *recv);
	delete recv;
}
void AllBoards::SaveMotorParam(){
	NVS nvs("motor");
	for(int i=0; i<motorMap.size(); ++i){
		char key[]="paramK012";
		itoa(i, key+6, 10);
		int v;
		v = motorKba[3*i]; nvs.set(key, v);
		key[5] = 'B';
		v = motorKba[3*i+1]; nvs.set(key, v);
		key[5] = 'A';
		v = motorKba[3*i+2]; nvs.set(key, v);
	}
	nvs.commit();
}
#endif

//	save and load motor's position. Motor position is always saved to NVS.
void AllBoards::LoadMotorPos(){
	UdpCmdPacket* recv = new UdpCmdPacket;
	recv->command = CI_SENSOR;
	WriteCmd(recv->command, *recv);
	udpCom.PrepareRetPacket(*recv);
	ReadRet(recv->command, udpCom.send);
	delete recv;

	NVS nvs("motor");
	for(int i=0; i<motorMap.size(); ++i){
		char key[]="pos012";
		itoa(i, key+3, 10);
		int m = motorPos[i];
		nvs.get(key, m);
		motorOffset[i] = (short)motorPos[i] - (short)m;	//	motorPos[i]: in board, m: WROOM side
		motorPos[i] = m;
	}
	ESP_LOGI(Tag(), "LoadMotorPos:%d %d %d offset: %d %d %d", motorPos[0], motorPos[1], motorPos[2], (int)motorOffset[0], (int)motorOffset[1], (int)motorOffset[2]);
}
void AllBoards::SaveMotorPos(){
	//	prepare previous motor angles.
	static int* prevMotorPos = NULL;
	static char motorPosLen = 0;
	if (!prevMotorPos || motorPosLen != motorMap.size()){
		motorPosLen = motorMap.size();
		if(prevMotorPos) free(prevMotorPos);
		prevMotorPos = (int*)malloc(sizeof(int)*motorPosLen);
		for(int i=0; i<motorPosLen; ++i){
			prevMotorPos[i] = motorPos[i];
		}
	}
	//	save motor angles
	NVS* nvs=NULL;
	for(int i=0; i<motorPosLen; ++i){
		int diff = motorPos[i] - prevMotorPos[i];
		if (diff > (SDEC_ONE/8) || diff < (-SDEC_ONE/8)){
			if (!nvs) nvs = new NVS("motor");
			prevMotorPos[i] = motorPos[i];
			char key[]="pos012";
			itoa(i, key+3, 10);
	        nvs->set(key, prevMotorPos[i]);
		}
	}
	if (nvs){
		nvs->commit();
		delete nvs;
		ESP_LOGD(Tag(), "SaveMotorPos Saved:%d %d %d off: %d %d %d", prevMotorPos[0], prevMotorPos[1], prevMotorPos[2], (int)motorOffset[0], (int)motorOffset[1], (int)motorOffset[2]);
	}
}
