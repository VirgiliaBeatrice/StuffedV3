#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"
#include "esp_event_loop.h"
#include "esp_log.h"

#include "lwip/opt.h"
#include "lwip/tcpip.h"
#include "lwip/mem.h"
#include "lwip/udp.h"
#include "lwip/ip_addr.h"
#ifndef _WIN32
#include "esp_system.h"
#include "esp_wifi.h"
#include "nvs_flash.h"
#endif

#include <algorithm>
#include <string.h>

#include "AllBoards.h"
#include "UdpCom.h"
#include "UartForBoards.h"
#include "CommandWROOM.h"
#include "Movement.h"
#ifndef _WIN32
#include "../websocketServer/ws_ws.h"
#endif

UdpCom udpCom;

static const int NHEADER = UdpPacket::HEADERLEN/2;
int UdpCmdPacket::CommandLen() {
	switch (command)
	{
	case CI_BOARD_INFO:		//
		return NHEADER * 2;
	//	case CI_SET_CMDLEN is only for uart
	case CI_ALL:			// direct/interpolate/forceControl + mode
	 	return (NHEADER + allBoards.GetNTotalMotor()*3 + 2 + 1) * 2;
	case CI_SENSOR:			//
		return NHEADER * 2;
	case CI_DIRECT:			//	pos vel
		return (NHEADER + allBoards.GetNTotalMotor() * 2) * 2;
	case CI_CURRENT:		//	current
		return (NHEADER + allBoards.GetNTotalMotor() ) * 2;
	case CI_INTERPOLATE: 	//	pos period targetCount
		return (NHEADER + allBoards.GetNTotalMotor() + 2) * 2;
	case CI_FORCE_CONTROL: 	//	pos JK period targetCount
		return (NHEADER + allBoards.GetNTotalMotor() + allBoards.GetNTotalForce()*3 + 2) * 2;
	case CI_SET_PARAM: 		//	K and B, torque min/max, A, boardId, heat ...
		return (NHEADER + 1 + allBoards.GetNTotalMotor() * 2) * 2;
	case CI_GET_PARAM: 		//	K and B, torque min/max, A, boardId, heat ...
		return (NHEADER + 1) * 2;
	case CI_RESET_SENSOR:
		return (NHEADER + 1) * 2;	//	flags
	case CIU_SET_IPADDRESS:	//  Set ip address to return the packet: command only
		return NHEADER * 2;
	case CIU_GET_IPADDRESS:	//  Get ip address to return the packet: command only
		return NHEADER * 2;
	case CIU_GET_SUBBOARD_INFO:	//	index
		return (NHEADER + 1) * 2;
	case CIU_MOVEMENT:		// index
		switch (*(uint8_t*)data)
		{
		case CI_M_ADD_KEYFRAME:
			return NHEADER*2 + 1 + (2 + 1 + allBoards.GetNTotalMotor() + 2 + allBoards.GetNTotalMotor() * 4 + 2 + 1 + 2 + 1);
		case CI_M_PAUSE_INTERPOLATE:
			return NHEADER*2 + 1;
		case CI_M_RESUME_INTERPOLATE:
			return NHEADER*2 + 1;
		case CI_M_PAUSE_MOV:
			return NHEADER*2 + 1 + (1 + 1 + allBoards.GetNTotalMotor());
		case CI_M_RESUME_MOV:
			return NHEADER*2 + 1 + (1 + 1);
		case CI_M_CLEAR_MOV:
			return NHEADER*2 + 1 + (1 + 1 + allBoards.GetNTotalMotor());
		case CI_M_CLEAR_PAUSED:
			return NHEADER*2 + 1;
		case CI_M_CLEAR_ALL:
			return NHEADER*2 + 1;
		case CI_M_QUERY:
			return NHEADER*2 + 1;
		default:
			ESP_LOGE(Tag(), "CommandLen() given invalid CIU_MOVEMENT command %d", *(uint8_t*)data);
			break;
		}
	}
	ESP_LOGE(Tag(), "CommandLen() given invalid command %d", command);
	assert(0);
	return 0;
}
void UdpRetPacket::SetLength() {
	switch (command){
	case CI_BOARD_INFO:		//	model nTarget nMotor nCurrent nForce nTouch macAddress
		length = (NHEADER + 6) * 2 + 6; break;
	//	case CI_SET_CMDLEN is only for uart
	case CI_ALL:			//	pos vel current force
		length = (NHEADER + allBoards.GetNTotalMotor()*2 + allBoards.GetNTotalCurrent() + allBoards.GetNTotalForce()) * 2; break;
	break;
	case CI_SENSOR:			//	pos force touch
		length = (NHEADER + allBoards.GetNTotalMotor() + allBoards.GetNTotalCurrent() + allBoards.GetNTotalForce() + allBoards.GetNTotalTouch()) * 2; break;
	case CI_DIRECT:			//	pos vel
		length = (NHEADER + allBoards.GetNTotalMotor() * 2) * 2; break;
	case CI_CURRENT:			//	pos vel current
		length = (NHEADER + allBoards.GetNTotalMotor() * 2 + allBoards.GetNTotalCurrent()) * 2; break;
	case CI_INTERPOLATE:	//	pos targetCountReadMin targetCountReadMax tickMin tickMax
	case CI_FORCE_CONTROL:
		length = (NHEADER + allBoards.GetNTotalMotor() + 4) * 2; break;
	case CI_SET_PARAM:
	case CI_RESET_SENSOR:
		length = NHEADER*2; break;
	case CI_GET_PARAM:
		length = (NHEADER + 1 + allBoards.GetNTotalMotor() * 2) * 2;  break;
	case CIU_TEXT:			//	return text message: type, len, str.
		length = (NHEADER + 1 + 1) * 2 + data[1]; break;
	case CIU_SET_IPADDRESS:	//  Set ip address to return the packet
		length = NHEADER * 2; break;
	case CIU_GET_IPADDRESS:	//  Get ip address to return the packet
		length = (NHEADER + 16) * 2; break;
	case CIU_GET_SUBBOARD_INFO:	//	uart id model nTarget nMotor nCurrent nForce
		length = (NHEADER + 7) * 2 ; break;
	case CIU_MOVEMENT:
		switch (*(uint8_t*)data)
		{
		length = 0;
		case CI_M_PAUSE_INTERPOLATE:
		case CI_M_RESUME_INTERPOLATE:
		case CI_M_PAUSE_MOV:
		case CI_M_RESUME_MOV:
		case CI_M_CLEAR_MOV:
		case CI_M_CLEAR_PAUSED:
		case CI_M_CLEAR_ALL:
			length = NHEADER*2 + 1; break;
		case CI_M_ADD_KEYFRAME:
			length = NHEADER*2 + 1 + (2 + 1 + 2 * 2);
		case CI_M_QUERY:
			length += NHEADER*2 + 1 + (allBoards.GetNTotalMotor() + 1 + movementInfos.size() * 2 + 2);
			break;
		default:
			break;
		}
		break;
	default:				//	error
		ESP_LOGE(Tag(), "SetLength() called with undefined command %d.", command);
		assert(0);
		length = 0;
		break;
	}
}
void UdpRetPacket::ClearData() {
	SetLength();
	memset(data, 0, length - NHEADER);
}
void UdpRetPacket::SetAll(ControlMode controlMode, unsigned char targetCountReadMin, unsigned char targetCountReadMax,
		unsigned short tickMin, unsigned short tickMax,
		SDEC* pos, SDEC* vel, SDEC* current, SDEC* force, SDEC* touch)
	{
	assert(command == CI_ALL);
	int cur = 0;
	data[cur++] = controlMode;
	data[cur++] = targetCountReadMin;
	data[cur++] = targetCountReadMax;
	data[cur++] = tickMin;
	data[cur++] = tickMax;
	int nMotor = allBoards.GetNTotalMotor();
	int nCurrent = allBoards.GetNTotalCurrent();
	int nForce = allBoards.GetNTotalForce();
	int nTouch = allBoards.GetNTotalTouch();
	for(int i=0; i!=nMotor; ++i){
		data[cur++] = pos ? pos[i] : 0;
	}
	for(int i=0; i!=nMotor; ++i){
		data[cur++] = vel ? vel[i] : 0;
	}
	for(int i=0; i!=nCurrent; ++i){
		data[cur++] = current ? current[i] : 0;
	}
	for(int i=0; i!=nForce; ++i){
		data[cur++] = force ? force[i] : 0;
	}
	for(int i=0; i!=nTouch; ++i){
		data[cur++] = touch ? touch[i] : 0;
	}
}

UdpCmdPackets::UdpCmdPackets() {
	smAvail = xSemaphoreCreateCounting(sizeof(buf) / sizeof(buf[0]), 0);
	smFree = xSemaphoreCreateCounting(sizeof(buf) / sizeof(buf[0]), sizeof(buf) / sizeof(buf[0]));

	isReading = xSemaphoreCreateMutex();
	isWriting = xSemaphoreCreateMutex();
}
UdpCmdPacket& UdpCmdPackets::Peek() {
	xSemaphoreTake(isReading, portMAX_DELAY);

	xSemaphoreTake(smAvail, portMAX_DELAY);
	xSemaphoreGive(smAvail);
	return base::Peek();
}
void UdpCmdPackets::Read() {
	xSemaphoreTake(smAvail, portMAX_DELAY);
	base::Read();
	xSemaphoreGive(smFree);

	xSemaphoreGive(isReading);
}
void UdpCmdPackets::Lock(){
	xSemaphoreTake(isWriting, portMAX_DELAY);
}
void UdpCmdPackets::Unlock(){
	xSemaphoreGive(isWriting);
}
UdpCmdPacket& UdpCmdPackets::Poke() {
	xSemaphoreTake(smFree, portMAX_DELAY);
	xSemaphoreGive(smFree);
	return base::Poke();
}
void UdpCmdPackets::Write() {
	xSemaphoreTake(smFree, portMAX_DELAY);
	base::Write();
	xSemaphoreGive(smAvail);
}

static void onReceiveUdp(void *arg, struct udp_pcb *pcb, struct pbuf *p, const ip_addr_t *addr, u16_t port)
{
	((UdpCom*)arg)->ReceiveCommandFromUdp(pcb, p, addr, port);
}
void UdpCom::Init() {
	udp = NULL;
	commandCount = 0;
}
void UdpCom::Start(){
	udp_init();
	if (udp) udp_remove(udp);
	udp = udp_new();
	udp_bind(udp, (ip_addr_t*)IP4_ADDR_ANY, port);
	udp_recv(udp, onReceiveUdp, this);
}

void UdpCom::ReceiveCommandFromUdp(struct udp_pcb * upcb, struct pbuf * top, const ip_addr_t* addr, u16_t port) {
	if (!recvs.WriteAvail()) {
		ESP_LOGE(Tag(), "Udp command receive buffer is full.");
		pbuf_free(top);
		return;
	}
	//	 read buffer and put it on recvs.
	recvs.Lock();
	struct pbuf* p = top;
	int readLen = 0;
	int cur = 0;
	int cmdLen = UdpPacket::HEADERLEN;
	UdpCmdPacket* recv = &recvs.Poke();
	int countDiffMax = 0;
	while (1) {
		int l = p->len - cur;
		if (l > cmdLen+2 - readLen) l = cmdLen+2 - readLen;
		memcpy(recv->bytes + readLen, ((char*)p->payload) + cur, l);
		readLen += l;
		cur += l;
		if (readLen == cmdLen+2) {
			if (cmdLen == UdpPacket::HEADERLEN) {
				cmdLen = recv->CommandLen();
				ESP_LOGD(Tag(), "L=%d Cm=%d Ct=%d received from %s.", recv->length, recv->command, recv->count, ipaddr_ntoa(addr));
			}
			if (readLen == cmdLen+2) {
				recv->returnIp = *addr;
				if (recv->length != cmdLen) {
					ESP_LOGE(Tag(), "cmdLen %d != recvLen %d in cmd:%d \n", cmdLen, recv->length, recv->command);
				}
				if ( (recv->command == CI_INTERPOLATE && recv->GetPeriod() == 0)
					|| (recv->command == CI_FORCE_CONTROL && recv->GetPeriod() == 0) ){
					/*if (recv->command == CI_INTERPOLATE){
						ESP_LOGI(Tag(), "CI_INT tcw:%d, peri=%d, ct=%d", recv->GetTargetCountWrite(), recv->GetPeriod(), recv->count);
					}*/
					recvs.Write();
				}
				else if (recv->count == (unsigned short)(commandCount + 1) ){		// check and update counter
					commandCount++;
					/*if (recv->command == CI_INTERPOLATE){
						ESP_LOGI(Tag(), "CI_INT tcw:%d, peri=%d, ct=%d", recv->GetTargetCountWrite(), recv->GetPeriod(), recv->count);
					}*/
					recvs.Write();
					if (countDiffMax > 0) {
						ESP_LOGI(Tag(), "ignored %d packets Ct:%d Cm:%d", countDiffMax, commandCount, recv->command);
						countDiffMax = 0;
					}
				}
				else if (recv->command == CIU_GET_IPADDRESS){
					commandCount ++;
					recv->count = commandCount;
					recvs.Write();
				}
				else {
					int diff = (short)((short)commandCount - (short)recv->count);
					if (countDiffMax < diff) countDiffMax = diff;
					//	Command count is not matched. There was some packet losses or delay.
					ESP_LOGD(Tag(), "ignore packet with Count:%d!=%d, Cmd:%d", recv->count, commandCount+1, recv->command);
				}
				if (!recvs.WriteAvail()) {
					ESP_LOGE(Tag(), "Udp recv buffer full.\n");
					pbuf_free(top);
					recvs.Unlock();
					return;
				}
				recv = &recvs.Poke();
				cmdLen = UdpPacket::HEADERLEN;
				readLen = 0;
			}
		}
		if (p->len == cur) {
			if (p->len != p->tot_len) {
				p = p->next;
				cur = 0;
			}
			else {
				break;
			}
		}
	}
	recvs.Unlock();
	pbuf_free(top);
	return;
}
UdpCmdPacket* UdpCom::PrepareCommand(CommandId cid, short from) {
	if (!recvs.WriteAvail()) {
		ESP_LOGE(Tag(), "PrepareCommand(): Udp command receive buffer is full.");
		return NULL;
	}
	recvs.Lock();
	UdpCmdPacket* r = &recvs.Poke();
	r->command = cid;
	r->length = r->CommandLen();
	r->count = from;
	memset(&r->returnIp, 0, sizeof(r->returnIp));

	return r;
}
UdpCmdPacket* UdpCom::PrepareMovementCommand(CommandId cid, CommandIdMovement mid, short from) {
	if (!recvs.WriteAvail()) {
		ESP_LOGE(Tag(), "PrepareCommand(): Udp command receive buffer is full.");
		return NULL;
	}
	recvs.Lock();
	UdpCmdPacket* r = &recvs.Poke();
	r->command = cid;
	*(uint8_t*)r->data = mid;
	r->length = r->CommandLen();
	r->count = from;
	memset(&r->returnIp, 0, sizeof(r->returnIp));
	return r;
}
void UdpCom::WriteCommand() {
	recvs.Write();
	recvs.Unlock();
}

void UdpCom::ReceiveCommand(void* payload, int len, short from) {
	UdpCmdPacket* recv;
	CommandId cid = (CommandId)(((short*)payload)[1]);
	if (cid == CIU_MOVEMENT) {
		CommandIdMovement mid = (CommandIdMovement)(((uint8_t*)payload)[4]);
		recv = PrepareMovementCommand(cid, mid, from);
	}
	else recv = PrepareCommand(cid, from);	//	[0] is length, [1] is command id
	if (!recv) return;
	memcpy(recv->bytes + 2, payload, len);
	WriteCommand();
}
extern "C" void UdpCom_ReceiveCommand(void* payload, int len, short from) {
	udpCom.ReceiveCommand(payload, len, from);
}

void UdpCom::SendText(char* text, short errorlevel) {
	send.command = CIU_TEXT;
	send.count = commandCount;
	send.data[0] = errorlevel;
	int len = strlen(text);
	const int MAXSTRLEN = UdpCmdPacket::MAXLEN - UdpCmdPacket::HEADERLEN - 2;
	if (len > MAXSTRLEN) len = MAXSTRLEN;
	send.data[1] = len;
	send.SetLength();
	char* str = (char*)(send.data + 2);
	memcpy(str, text, len);
	struct pbuf* pb = pbuf_alloc(PBUF_TRANSPORT, send.length+2, PBUF_RAM);
    memcpy (pb->payload, send.bytes, send.length+2);
#ifndef _WIN32
	//	send to server
	wsOnMessageSr(send);
#endif
	//	send to UDP
#ifdef _WIN32
	if (ownerIp.addr == 0) {
#elif defined WROOM
	if (ownerIp.u_addr.ip4.addr == 0) {
#else
#error
#endif
		err_t e = udp_sendto(udp, pb, &ownerIp, port);
		if (e != ERR_OK){
			ESP_LOGE(Tag(), "udp_sendto() Error %d", e);
			//	  ERR_OK = 0, ERR_MEM = -1, ERR_BUF = -2, ERR_TIMEOUT = -3, ERR_RTE = -4, ERR_INPROGRESS = -5, ERR_VAL = -6, ERR_WOULDBLOCK = -7, ERR_USE = -8, ERR_ALREADY = -9, ERR_ISCONN = -10, ERR_CONN = -11,  ERR_IF = -12, ERR_ABRT = -13, ERR_RST = -14, ERR_CLSD = -15,ERR_ARG = -16
		}
	}
    pbuf_free(pb); //De-allocate packet buffer
//	ESP_LOGI(Tag(), "Ret%d C%d L%d to %s\n", send.command, send.count, send.length, ipaddr_ntoa(&ownerIp));
}
void UdpCom::PrepareRetPacket(UdpCmdPacket& recv) {
	send.command = recv.command;
	send.count = recv.count;
	send.ClearData();
}
void UdpCom::SendReturn(UdpCmdPacket& recv) {
#ifdef _WIN32
	if (recv.returnIp.addr == 0) {
#elif defined WROOM
	if (recv.returnIp.u_addr.ip4.addr == 0) {
#else
#error
#endif
		if (recv.count == CS_WEBSOCKET || recv.count == CS_DUKTAPE) SendReturnServer();
		else if (recv.count == CS_MOVEMENT_MANAGER) SendReturnMovement(send);
	}
	else {
		SendReturnUdp(recv);
	}
}
void UdpCom::SendReturnServer() {
#ifndef _WIN32
	wsOnMessageSr(send);
#endif
}

#include "Movement.h"
void UdpCom::SendReturnMovement(UdpRetPacket& ret) {
	movementOnGetPICInfo(ret);
}
void UdpCom::SendReturnUdp(UdpCmdPacket& recv) {
	if (!udp) return;
	static int sendLen = 0;
	static struct pbuf* pbStart = NULL;
	//	Send packet if buffer is full
	if (sendLen + send.length + 2 > 1400){	// udp packet length should be smaller than 1400.
		err_t e = udp_sendto(udp, pbStart, &recv.returnIp, port);
		if (e != ERR_OK){
			ESP_LOGE(Tag(), "udp_sendto() Error %d", e);
			//	  ERR_OK = 0, ERR_MEM = -1, ERR_BUF = -2, ERR_TIMEOUT = -3, ERR_RTE = -4, ERR_INPROGRESS = -5, ERR_VAL = -6, ERR_WOULDBLOCK = -7, ERR_USE = -8, ERR_ALREADY = -9, ERR_ISCONN = -10, ERR_CONN = -11,  ERR_IF = -12, ERR_ABRT = -13, ERR_RST = -14, ERR_CLSD = -15,ERR_ARG = -16
		}
    	pbuf_free(pbStart); //De-allocate packet buffer
		pbStart = NULL;
		sendLen = 0;
	}
	//	Copy command to buffer
	struct pbuf* pbNext = pbuf_alloc(PBUF_TRANSPORT, send.length + 2, PBUF_RAM);
	memcpy(pbNext->payload, send.bytes, send.length + 2);
	if (pbStart == NULL){
		pbStart = pbNext;
	}else{
		pbuf_cat(pbStart, pbNext);
	}
	sendLen += send.length + 2;
	//	Send buffer if there is no resting command to process.
	if (recvs.ReadAvail() <= 1){	//	Read() will call after sent. So 1 means no command remaining.
		err_t e = udp_sendto(udp, pbStart, &recv.returnIp, port);
		if (e != ERR_OK){
			ESP_LOGE(Tag(), "udp_sendto() Error %d", e);
			//	  ERR_OK = 0, ERR_MEM = -1, ERR_BUF = -2, ERR_TIMEOUT = -3, ERR_RTE = -4, ERR_INPROGRESS = -5, ERR_VAL = -6, ERR_WOULDBLOCK = -7, ERR_USE = -8, ERR_ALREADY = -9, ERR_ISCONN = -10, ERR_CONN = -11,  ERR_IF = -12, ERR_ABRT = -13, ERR_RST = -14, ERR_CLSD = -15,ERR_ARG = -16
		}
    	pbuf_free(pbStart); //De-allocate packet buffer
		pbStart = NULL;
		sendLen = 0;
		ESP_LOGD(Tag(), "Ret Sent to %s", ipaddr_ntoa(&recv.returnIp));
	}else{
		ESP_LOGD(Tag(), "Ret not sent %d", recvs.ReadAvail());
	}
	ESP_LOGD(Tag(), "Ret%d C%d L%d to %s", send.command, send.count, send.length, ipaddr_ntoa(&recv.returnIp));
}
void UdpCom::ExecUdpCommand(UdpCmdPacket& recv) {
	switch (recv.command)
	{
	case CI_BOARD_INFO:
		PrepareRetPacket(recv);
		send.SetBoardInfo(allBoards.GetSystemId(), allBoards.GetNTarget(), allBoards.GetNTotalMotor(), allBoards.GetNTotalCurrent(), allBoards.GetNTotalForce(), allBoards.GetNTotalTouch());
		SendReturn(recv);
		break;
	case CIU_SET_IPADDRESS:
		ownerIp = recv.returnIp;
		PrepareRetPacket(recv);
		SendReturn(recv);
		break;
	case CIU_GET_IPADDRESS:
		PrepareRetPacket(recv);
		#ifndef _WIN32
				if (ownerIp.type ==IPADDR_TYPE_V4){
					send.data[0] = (ownerIp.u_addr.ip4.addr>>3*8) &0xFF;
					send.data[1] = (ownerIp.u_addr.ip4.addr>>2*8) &0xFF;
					send.data[2] = (ownerIp.u_addr.ip4.addr>>1*8) &0xFF;
					send.data[3] = ownerIp.u_addr.ip4.addr&0xFF;
				}else if (ownerIp.type ==IPADDR_TYPE_V6){
					for(int i=0; i<4; ++i){
						u32_t a = ownerIp.u_addr.ip6.addr[i];
						send.data[0+4*i] = (a>>3*8) &0xFF;
						send.data[1+4*i] = (a>>2*8) &0xFF;
						send.data[2+4*i] = (a>>1*8) &0xFF;
						send.data[3+4*i] = a&0xFF;
					}
				}
		#else
				send.data[0] = (ownerIp.addr >> 3 * 8) & 0xFF;
				send.data[1] = (ownerIp.addr >> 2 * 8) & 0xFF;
				send.data[2] = (ownerIp.addr >> 1 * 8) & 0xFF;
				send.data[3] = ownerIp.addr & 0xFF;
		#endif
		#ifdef DEBUG
				logPrintf("GetIPAddress send to ");
				Serial.print(recv.returnIp);
				Serial.print("  ");
				Serial.print(port);
				Serial.print(" Len:");
				Serial.print(send.length);
				Serial.println(".");
				Serial.println();
		#endif
		SendReturn(recv);
		break;
	case CIU_GET_SUBBOARD_INFO:{
		int index = recv.data[0];
		int iu;
		for(iu=0; iu<allBoards.NUART; ++iu){
			if (index > (int)(allBoards.uart[iu]->boards.size())){
				index -= allBoards.uart[iu]->boards.size();
			}else{
				break;
			}
		}
		PrepareRetPacket(recv);
		if (iu < allBoards.NUART){
			recv.data[0] = iu;
			recv.data[1] = index;
			recv.data[2] = allBoards.uart[iu]->boards[index]->GetModelNumber();
			recv.data[3] = allBoards.uart[iu]->boards[index]->GetNTarget();
			recv.data[4] = allBoards.uart[iu]->boards[index]->GetNMotor();
			recv.data[5] = allBoards.uart[iu]->boards[index]->GetNCurrent();
			recv.data[6] = allBoards.uart[iu]->boards[index]->GetNForce();
		}else{
			recv.data[0] = recv.data[1] = -1;
		}
		SendReturn(recv);
	}	break;
	case CIU_MOVEMENT: {
		uint8_t movement_command_id = *(uint8_t*)recv.data;

		// prepare return packet header
		PrepareRetPacket(recv);
		*(uint8_t*)send.data = movement_command_id;
		send.SetLength();
		void* movement_command_data = (void*)((uint8_t*)send.data+1);

		switch (movement_command_id)
		{
			case CI_M_ADD_KEYFRAME: {
				// execute and prepare return packet
				prepareRetAddKeyframe(shiftPointer(recv.data, 1), movement_command_data);
				SendReturn(recv);

				break;
			}
			case CI_M_PAUSE_INTERPOLATE: {
				pauseInterpolate();
				SendReturn(recv);
				break;
			}
			case CI_M_RESUME_INTERPOLATE: {
				resumeInterpolate();
				SendReturn(recv);
				break;
			}
			case CI_M_PAUSE_MOV: {
				prepareRetPauseMov(shiftPointer(recv.data, 1), movement_command_data);
				SendReturn(recv);
				break;
			}
			case CI_M_RESUME_MOV: {
				prepareRetResumeMov(shiftPointer(recv.data, 1), movement_command_data);
				SendReturn(recv);
				break;
			}
			case CI_M_CLEAR_MOV: {
				prepareRetClearMov(shiftPointer(recv.data, 1), movement_command_data);
				SendReturn(recv);
				break;
			}
			case CI_M_CLEAR_PAUSED: {
				clearPausedMovements();
				SendReturn(recv);
				break;
			}
			case CI_M_CLEAR_ALL: {
				clearInterpolateBuffer();
				SendReturn(recv);
				break;
			}
			case CI_M_QUERY: {
				prepareRetQuery(shiftPointer(recv.data, 1), movement_command_data);
				SendReturn(recv);
				break;
			}
			default:
				break;
		}
	} break;
	default:
		ESP_LOGE(Tag(), "Invalid command %d count %d received from %s at %x.\n",
			(int)recv.command, (int)recv.count, ipaddr_ntoa(&recv.returnIp), (unsigned)&recv);
		break;
	}
}
