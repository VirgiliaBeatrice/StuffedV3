//#define SHOW_UDP_PACKET
//#define UDP_NORETRY

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.IO;


namespace Robokey
{
    class RobotInfo : ICloneable, IComparable
    {
        public int systemId;
        public int nTarget;
        public int nMotor;
        public int nCurrent;
        public int nForce;
        public int nTouch;
        public byte[] macAddress = new byte[6];
        public void Clear()
        {
            for (int i = 0; i < macAddress.Count(); ++i) {
                macAddress[i] = 0;
            }
        }
        public Object Clone()
        {
            RobotInfo rv = new RobotInfo();
            rv.systemId = systemId;
            rv.nTarget = nTarget;
            rv.nMotor = nMotor;
            rv.nCurrent = nCurrent;
            rv.nForce = nForce;
            rv.nTouch = nTouch;
            macAddress.CopyTo(rv.macAddress, 0);
            return rv;
        }
        public int CompareTo(object o) {
            RobotInfo s = (RobotInfo)o;
            if (systemId - s.systemId != 0) return systemId - s.systemId;
            if (nTarget - s.nTarget != 0) return nTarget - s.nTarget;
            if (nMotor - s.nMotor != 0) return nMotor - s.nMotor;
            if (nCurrent - s.nCurrent != 0) return nCurrent - s.nCurrent;
            if (nForce - s.nForce != 0) return nForce - s.nForce;
            return nTouch - s.nTouch;
        }
    };

    class UdpComm
    {
        const int localBcPort = 9091;
        const int remotePort = 9090;
        const int localPort = 9090;
        bool bFindRobot = false;
        const ushort CONTROLFREQ = 3;   //  3kHz
        public StreamWriter log;
        public ushort recvCount;

        //  board info
        RobotInfo robotInfo = new RobotInfo();
        public RobotInfo RobotInfo
        {
            set { SetRobotInfo(value); }
            get { return (RobotInfo)robotInfo.Clone(); }
        }
        public void SetRobotInfo(RobotInfo info)
        {
            if (info.CompareTo(robotInfo) != 0)
            {
                byte[] prevMacAddress = new byte[6];
                robotInfo.macAddress.CopyTo(prevMacAddress, 0);
                robotInfo = info;
                pose = new Pose(robotInfo.nMotor);
                velocity = new Pose(robotInfo.nMotor);
                torqueMin = new short [robotInfo.nMotor];
                torqueMax = new short[robotInfo.nMotor];
                heatLimit = new short[robotInfo.nMotor];
                heatRelease = new short[robotInfo.nMotor];
                paramK = new short[robotInfo.nMotor];
                paramB = new short[robotInfo.nMotor];
                paramA = new short[robotInfo.nMotor];

                current = new short[robotInfo.nCurrent];
                force = new short[robotInfo.nForce];
                touch = new short[robotInfo.nTouch];
                nInterpolateTotal = robotInfo.nTarget;
                if (owner != null) owner.Invoke(new UpdateRobotInfoHandlerType(OnUpdateRobotInfo), prevMacAddress);
            }
        }
        //  network
        volatile System.Net.Sockets.UdpClient udp = null;
        volatile System.Net.Sockets.UdpClient udpBc = null;
        public System.Net.IPEndPoint sendPoint = null;
        public System.Net.IPEndPoint recvPoint = null;
        bool bConnected = false;
        public bool IsConnected { get { return bConnected; } }

        //  robot state
        public PoseData pose = null;                        //  current angles of motors; ReadPose
        public PoseData velocity = null;                    //  current velocities of motors; ReadVelocity
        public short[] current = null;                      //  current sensor's values
        public short[] force = null;                        //  force sensor's values
        public short[] touch = null;                        //  touch sensor's values
        public int nInterpolateTotal=0;                     //  capacity of interpolation targets.
        public byte interpolateTargetCountWrite;          //  count of interpolation at write cursor
        public byte interpolateTargetCountReadMin;        //  count of interpolation at read cursor
        public byte interpolateTargetCountReadMax;        //  count of interpolation at read cursor
        public int interpolateTickMin = 0;                  //  tick of interpolation
        public int interpolateTickMax = 0;                  //  tick of interpolation
        public enum ControlMode {
            CM_DIRECT,
            CM_INTERPOLATE,
            CM_CURRENT,
            CM_FORCE
        };
        //  robot param
        public short[] torqueMin = null;
        public short[] torqueMax = null;
        public short[] heatLimit = null;
        public short[] heatRelease = null;
        public short[] paramK = null;
        public short[] paramB = null;
        public short[] paramA = null;

        private ControlMode _controlMode = ControlMode.CM_DIRECT;
        public ControlMode controlMode {
            set {
                if (_controlMode != value)
                {
                    if (value == ControlMode.CM_INTERPOLATE || value == ControlMode.CM_FORCE)
                    {
                        interpolateTargetCountReadMin = interpolateTargetCountReadMax = 0x100 - 2;
                        interpolateTargetCountWrite = 0;
                    }
                    _controlMode = value;
                }
            }
            get { return _controlMode; }
        }
        public struct Packet {
            byte[] data;
            int length;
            public Packet(byte[] d, int l)
            {
                data = d;
                length = l;
            }
        }
        public class CommandQueue{
            public ushort commandCount;
            const int bufferLen = 50;
            byte[][] buffers = new byte [bufferLen][];
            volatile int write = 0;
            volatile int read = 0;
            volatile int sent = 0;
            public int writeAvail { get { return (read-1 + bufferLen - write) % bufferLen; } }
            public int readAvail { get { return (write + bufferLen - read) % bufferLen; } }
            public int nWait { get { return (write + bufferLen - sent) % bufferLen; } }
            public int UpdateRead() {
                int rv = (sent - read + bufferLen) % bufferLen;
                read = sent;
                return rv;
            }
            public bool Write(byte[] b)
            {
                if (writeAvail == 0) return false;
                commandCount++;
                UdpComm.PokeCounter(commandCount, 0, b);
                buffers[write] = b;
                if (write < bufferLen - 1) write++;
                else write = 0;
                return true;
            }
            public void Clear()
            {
                read = write = sent = 0;
            }
            public void FreeTo(ushort ct) {
                while (sent != write) {
                    ushort cmdCount = PeekCount(0, buffers[sent]);
                    if (ct >= cmdCount)
                    {   //  erase command
                        if (sent < bufferLen - 1) sent++;
                        else sent = 0;
                    }
                    else {
                        break;
                    }
                }
            }
            public byte[] Peek(int i = 0)
            {
                if (readAvail > i) return buffers[(read + i) % bufferLen];
                else return null;
            }
        } 
        public CommandQueue sendQueue = new CommandQueue();
        public UdpComm(Control o)
        {
            owner = o;
        }
        public void SetAddress(String adr)
        {
            if (udp != null)
            {
                udp.Close();
                udp = null;
            }
            System.Net.IPAddress[] adrs = null;
            try
            {
                adrs = System.Net.Dns.GetHostAddresses(adr);
            }
            catch
            {
            }
            if (adrs != null && adrs.Length > 0)
            {
                sendPoint = new System.Net.IPEndPoint(adrs[0], remotePort);
                recvPoint = null;
            }
        }
        static void WriteShort(int s, ref int cur, byte[] buf) {
            buf[cur] = (byte)s;
            buf[cur+1] = (byte)(s >> 8);
            cur += 2;
        }
        static byte ReadByte(ref int cur, byte[] buf)
        {
            byte s = buf[cur];
            cur ++;
            return s;
        }
        static short ReadShort(ref int cur, byte[] buf)
        {
            int s = buf[cur] | (buf[cur + 1] << 8);
            cur += 2;
            return (short)s;
        }
        static void ReadShortExt(ref int s, ref int cur, byte[] buf)
        {
            int t = buf[cur] | (buf[cur + 1] << 8);
            short diff = (short)(t - (s & 0xFFFF));
            s += diff;
            cur += 2;
        }
        static void PokeLengthAtHeader(int len, int start, byte[] buf)
        {
            int cur = start + 2;
            WriteShort(len, ref cur, buf);
        }
        static void PokeCounter(int ct, int start, byte[] buf)
        {
            int cur = start;
            WriteShort(ct, ref cur, buf);
        }
        static void WriteHeader(int cmd, ref int cur, byte[] buf)
        {
            WriteShort(0, ref cur, buf);
            WriteShort(0, ref cur, buf);
            WriteShort(cmd, ref cur, buf);
        }
        static void ReadHeader(ref ushort len, ref ushort ct, ref ushort cmd, ref int cur, byte[] buf)
        {
            ct = (ushort)ReadShort(ref cur, buf);
            len = (ushort)ReadShort(ref cur, buf);
            cmd = (ushort)ReadShort(ref cur, buf);
        }
        static ushort PeekCount(int st, byte[] buf)
        {
            return (ushort)ReadShort(ref st, buf);
        }
        static ushort PeekLength(int st, byte[] buf)
        {
            int cur = st + 2;
            return (ushort)ReadShort(ref st, buf);
        }
        static ushort PeekCommand(int st, byte[] buf)
        {
            int cur = st + 4;
            return (ushort)ReadShort(ref st, buf);
        }
        static int ReadLength(int start, byte[] buf) {
            int cur = start+2;
            return ReadShort(ref cur, buf);
        }
        void ReadBoard(ref int cur, byte[] buf){
            RobotInfo info = new RobotInfo();
            info.systemId = ReadShort(ref cur, buf);
            info.nTarget = ReadShort(ref cur, buf);
            info.nMotor = ReadShort(ref cur, buf);
            info.nCurrent = ReadShort(ref cur, buf);
            info.nForce = ReadShort(ref cur, buf);
            info.nTouch = ReadShort(ref cur, buf);
            for (int i = 0; i < 6; ++i)
            {
                info.macAddress[i] = ReadByte(ref cur, buf);
            }
            SetRobotInfo(info);
            bConnected = true;
        }
        void ReadPose(ref int cur, byte[] buf)
        {
            for (int i = 0; i < pose.values.Length; ++i) {
                ReadShortExt(ref pose.values[i], ref cur, buf);
            }
        }
        void ReadCurrent(ref int cur, byte[] buf)
        {
            for (int i = 0; i < current.Length; ++i)
            {
                current[i] = ReadShort(ref cur, buf);
            }
        }
        void ReadForce(ref int cur, byte[] buf)
        {
            for (int i = 0; i < force.Length; ++i)
            {
                force[i] = ReadShort(ref cur, buf);
            }
        }
        void ReadTouch(ref int cur, byte[] buf)
        {
            for (int i = 0; i < touch.Length; ++i)
            {
                touch[i] = ReadShort(ref cur, buf);
            }
        }
        void ReadVelocity(ref int cur, byte[] buf)
        {
            for (int i = 0; i < velocity.values.Length; ++i)
            {
                velocity.values[i] = ReadShort(ref cur, buf);
            }
        }
        void ReadTick(ref int cur, byte[] buf)
        {
            interpolateTargetCountReadMin = (byte)ReadShort(ref cur, buf);
            interpolateTargetCountReadMax = (byte)ReadShort(ref cur, buf);
            interpolateTickMin = ReadShort(ref cur, buf);
            interpolateTickMax = ReadShort(ref cur, buf);
        }
        void CallUpdateRobotState()
        {
            if (owner != null) owner.Invoke(new UpdateRobotStateHandlerType(OnUpdateRobotState));
        }
        void CallUpdateRobotParam()
        {
            if (owner != null) owner.Invoke(new UpdateRobotStateHandlerType(OnUpdateRobotParam));
        }
        void ReadPeerIPAddress(ref int cur, byte[] buf)
        {
            int[] adr = new int[16];
            for (int i = 0; i < 16; ++i) {
                adr[i] = ReadShort(ref cur, buf);
            }
        }

        void SetMessage(int type, string msg)
        {
            if (OnMessageReceive != null)
            {
                try
                {
                    if (owner != null) owner.Invoke(new MessageRecieveHandlerType(OnMessageReceive), type, msg);
                }catch (Exception ex){
                    System.Diagnostics.Debug.WriteLine("Exception:" + ex.ToString() + " in SetMessage()");
                }

            }
        }
        void ReadText(ref int cur, byte[] buf)
        {
            int type = ReadShort(ref cur, buf);
            int len = ReadShort(ref cur, buf);
            char[] cs = new char[len];
            for (int i=0; i<len; ++i)
            {
                cs[i] = (char)buf[cur + i];
            }
            string str = new string(cs, 0, len);
            SetMessage(type, str);
        }

        private void OnReceive(IAsyncResult ar)
            {
            UdpComm uc = (UdpComm)(ar.AsyncState);
            byte[] receiveBytes;
            if (uc.udp == null) return;
            receiveBytes = uc.udp.EndReceive(ar, ref uc.recvPoint);
            if (uc.recvPoint.Port != localBcPort)   //  In broad cast case, sent packet can be received. Must skip.
            {
                int cur = 0;
                ushort length = 0;
                ushort commandId = 0;
                while (cur < receiveBytes.Length)
                {
                    int start = cur;
                    ReadHeader(ref length, ref recvCount, ref commandId, ref cur, receiveBytes);
                    if (receiveBytes.Length < start + (length + 2)) {   //  current packet(length+2)
                        System.Diagnostics.Debug.WriteLine("Ct:" + recvCount + " Cmd:" + commandId + " received length:" + receiveBytes.Length + "  not match to the length in packet:" + (length+2));
                    }
#if SHOW_UDP_PACKET
                    System.Diagnostics.Debug.WriteLine("Receive cmd=" + commandId + "  count=" + recvCount + "  len="+length);
#endif
                    if (log != null)
                    {
                        log.Write("L" + length + " C" + commandId);
                        for (int i = cur; i < receiveBytes.Length; i += 2)
                        {
                            short s = (short)(receiveBytes[i] | (receiveBytes[i + 1] << 8));
                            log.Write(" " + string.Format("{0,0:X4}", s));
                        }
                        log.Write("\r\n");
                    }
                    sendQueue.FreeTo(recvCount);
                    switch ((CommandId)commandId)
                    {
                        case CommandId.CI_BOARD_INFO:
                            ReadBoard(ref cur, receiveBytes);
                            break;
                        case CommandId.CI_DIRECT:
                            ReadPose(ref cur, receiveBytes);
                            cur += pose.values.Length * 2;
                            CallUpdateRobotState();
                            break;
                        case CommandId.CI_INTERPOLATE:
                        case CommandId.CI_FORCE_CONTROL:
                            ReadPose(ref cur, receiveBytes);
                            ReadTick(ref cur, receiveBytes);
                            //System.Diagnostics.Debug.WriteLine("Receive CorMax:" + (int)interpolateTargetCountReadMax);
                            CallUpdateRobotState();
                            break;
                        case CommandId.CI_SENSOR:
                            ReadPose(ref cur, receiveBytes);
                            ReadCurrent(ref cur, receiveBytes);
                            ReadForce(ref cur, receiveBytes);
                            ReadTouch(ref cur, receiveBytes);
                            CallUpdateRobotState();
                            break;
                        case CommandId.CI_GET_PARAM:
                            OnReceiveGetParam(ref cur, receiveBytes);
                            break;
                        case CommandId.CIU_GET_IPADDRESS:
                            ReadPeerIPAddress(ref cur, receiveBytes);
                            if (bFindRobot)
                            {
                                lock (sendQueue)
                                {
                                    sendQueue.Clear();
                                    sendQueue.commandCount = recvCount;
                                }
                                if (OnRobotFound != null)
                                {
                                    if (owner != null) owner.Invoke(new RobotFindHandlerType(OnRobotFound), recvPoint.Address);
                                }
                            }
                            break;
                        case CommandId.CIU_TEXT:
                            ReadText(ref cur, receiveBytes);
                            break;
                        default:
                            if (commandId >= (int)CommandId.CIU_NCOMMAND)
                            {
                                string msg = "Invalid command " + commandId + " received";
                                SetMessage(-1, msg);
                            }
                            break;
                    }
                    if (cur != start + length + 2) {
                        System.Diagnostics.Debug.WriteLine("Recv lengths not match !");
                    }
                    cur = start + length + 2;
                }
            }
            if (uc.udp != null)
            {
                uc.udp.BeginReceive(OnReceive, ar.AsyncState);
            }
        }
        public void Close()
        {
            bConnected = false;
            robotInfo.Clear();
            if (udp != null) udp.Close();
            udp = null;
        }
        public void Open()
        {
            if (udp == null)
            {
                udp = new System.Net.Sockets.UdpClient(localPort);
                udp.BeginReceive(OnReceive, this);
            }
        }
        public void PutCommand(byte[] cmd, int len) {
            if (udp == null || sendPoint == null) return;
#if UDP_NORETRY    //  No retry 
            sendQueue.Clear();
#endif
            PokeLengthAtHeader(len-2, 0, cmd);
            for (int i=0; i<100 && !sendQueue.Write(cmd); ++i) {
                ushort ct = PeekCommand(0, sendQueue.Peek());
                sendQueue.FreeTo(ct);
            }
        }
        public void SendPackets()
        {
            if (udp == null || sendPoint == null) return;
            sendQueue.UpdateRead();
            if (sendQueue.readAvail <= 0) return;
            //            byte[] buf = new byte[1400];    //  MTU is less than 1500.
            byte[] buf = new byte[1400];    //  MTU is less than 1500.
            int pos = 0;
            for (int i = 0; i < sendQueue.readAvail; ++i)
            {
                byte[] cmd = sendQueue.Peek(i);
                int len = ReadLength(0, cmd) + 2;
                if (pos + len > buf.Length)
                {
                    udp.Send(buf, pos, sendPoint);
                    pos = 0;
                }
                i -= sendQueue.UpdateRead();
                if (i < 0)
                {
                    i = 0;
                } else {
                    for (int p = 0; p < len; ++p)
                    {
                        buf[pos + p] = cmd[p];
                    }
                    pos += len;
                }
            }
            if (pos > 0)
            {
                udp.Send(buf, pos, sendPoint);
                pos = 0;
            }
        }
        public void SendPoseDirect(PoseData pose)
        {
            if (pose == null) return;
            controlMode = ControlMode.CM_DIRECT;
            byte[] packet = new byte[1000];
            int p = 0;
            WriteHeader((int)CommandId.CI_DIRECT, ref p, packet);
            for (int i = 0; i < pose.values.Count(); ++i)
            {
                int v = pose == null ? 0 : pose.values[i];
                WriteShort(v, ref p, packet);
            }
            for (int i = 0; i < pose.values.Count(); ++i)
            {
                WriteShort(0, ref p, packet);   //  Tentative: set velocity to 0 
            }
            PutCommand(packet, p);
        }
        public void SendPoseInterpolate(PoseData pose, ushort peri)
        {
            ushort period = (ushort)(peri * CONTROLFREQ);
            if (pose == null) return;
            controlMode = ControlMode.CM_INTERPOLATE;
            byte[] packet = new byte[1000];
            int p = 0;
            WriteHeader((int)CommandId.CI_INTERPOLATE, ref p, packet);
            for (int i = 0; i < pose.nMotor; ++i)
            {
                int v = pose == null ? 0 : pose.values[i];
                WriteShort(v, ref p, packet);
            }
            WriteShort(period, ref p, packet);
            WriteShort(interpolateTargetCountWrite, ref p, packet);
            //  In case "period == 0" commandCount is ignored and not incremented in robot side. 
            if (peri == 0) sendQueue.commandCount--;
            PutCommand(packet, p);
#if false
            System.Diagnostics.Debug.WriteLine("SendPoseInterpolate  tcw=" + interpolateTargetCountWrite
                + "  peri=" + peri);
#endif
            if (period != 0) interpolateTargetCountWrite++;
        }
        public void SendPoseForceControl(PoseData pose, ushort peri, short [][] jacob)
        {
            ushort period = (ushort)(peri * CONTROLFREQ);
            if (pose == null) return;
            controlMode = ControlMode.CM_FORCE;
            byte[] packet = new byte[1000];
            int p = 0;
            WriteHeader((int)CommandId.CI_FORCE_CONTROL, ref p, packet);
            for (int i = 0; i < pose.nMotor; ++i)
            {
                int v = pose == null ? 0 : pose.values[i];
                WriteShort(v, ref p, packet);
            }
            WriteShort(period, ref p, packet);
            WriteShort(interpolateTargetCountWrite, ref p, packet);
            //  same as interpolate until here. Add jacobian
            for(int f=0; f<robotInfo.nForce; ++f)
            {
                for (int m = 0; m < 3; ++m)
                {
                    WriteShort(jacob[f][m], ref p, packet);
                }
            }
            PutCommand(packet, p);
            if (period != 0) interpolateTargetCountWrite++;
        }

        /// send/recv paramters
        public void SendParamCurrent(int nMotor, int[] a)
        {
            byte[] packet = new byte[1000];
            int p = 0;
            WriteHeader((int)CommandId.CI_SET_PARAM, ref p, packet);
            WriteShort((int)SetParamType.PT_CURRENT, ref p, packet);
            for (int i = 0; i < nMotor; ++i)
            {
                WriteShort(a[i], ref p, packet);
            }
            for (int i = 0; i < nMotor; ++i)
            {
                WriteShort(0, ref p, packet);    //  padding
            }
            PutCommand(packet, p);
        }
        public void SendGetParam(SetParamType pt)
        {
            byte[] packet = new byte[8];
            int p = 0;
            WriteHeader((int)CommandId.CI_GET_PARAM, ref p, packet);
            WriteShort((int)pt, ref p, packet);
            PutCommand(packet, p);
        }
        private void OnReceiveGetParam(ref int cur, byte[] receiveBytes){
            SetParamType pt = (SetParamType)ReadShort(ref cur, receiveBytes);
            switch (pt)
            {
                case SetParamType.PT_CURRENT:
                    for (int i = 0; i < robotInfo.nMotor; ++i)
                        paramA[i] = ReadShort(ref cur, receiveBytes);
                    for (int i = 0; i < robotInfo.nMotor; ++i)
                        ReadShort(ref cur, receiveBytes);
                    break;
                case SetParamType.PT_PD:
                    for (int i = 0; i < robotInfo.nMotor; ++i)
                        paramK[i] = ReadShort(ref cur, receiveBytes);
                    for (int i = 0; i < robotInfo.nMotor; ++i)
                        paramB[i] = ReadShort(ref cur, receiveBytes);
                    break;
                case SetParamType.PT_MOTOR_HEAT:
                    for (int i = 0; i < robotInfo.nMotor; ++i)
                        heatLimit[i] = ReadShort(ref cur, receiveBytes);
                    for (int i = 0; i < robotInfo.nMotor; ++i)
                        heatRelease[i] = ReadShort(ref cur, receiveBytes);
                    break;
                case SetParamType.PT_TORQUE_LIMIT:
                    for (int i = 0; i < robotInfo.nMotor; ++i)
                        torqueMin[i] = ReadShort(ref cur, receiveBytes);
                    for (int i = 0; i < robotInfo.nMotor; ++i)
                        torqueMax[i] = ReadShort(ref cur, receiveBytes);
                    break;
            }
            CallUpdateRobotParam();
        }

        public void SendParamPd(int nMotor, int[] k, int[] b)
        {
            byte[] packet = new byte[1000];
            int p = 0;
            WriteHeader((int)CommandId.CI_SET_PARAM, ref p, packet);
            WriteShort((int)SetParamType.PT_PD, ref p, packet);
            for (int i = 0; i < nMotor; ++i)
            {
                WriteShort(k[i], ref p, packet);
            }
            for (int i = 0; i < nMotor; ++i)
            {
                WriteShort(b[i], ref p, packet);
            }
            PutCommand(packet, p);
        }
        public void SendParamTorqueLimit(int nMotor, int[] minT, int[] maxT)
        {
            byte[] packet = new byte[1000];
            int p = 0;
            WriteHeader((int)CommandId.CI_SET_PARAM, ref p, packet);
            WriteShort((int)SetParamType.PT_TORQUE_LIMIT, ref p, packet);
            for (int i = 0; i < nMotor; ++i)
            {
                WriteShort(minT[i], ref p, packet);
            }
            for (int i = 0; i < nMotor; ++i)
            {
                WriteShort(maxT[i], ref p, packet);
            }
            PutCommand(packet, p);
        }
        public void SendParamBoardId(int nMotor, int[] bids)
        {
            byte[] packet = new byte[1000];
            int p = 0;
            WriteHeader((int)CommandId.CI_SET_PARAM, ref p, packet);
            WriteShort((int)SetParamType.PT_TORQUE_LIMIT, ref p, packet);
            //  padding
            int tmp = p;
            for (int i = 0; i < 2*nMotor; ++i)
            {
                WriteShort(0, ref tmp, packet);
            }
            //  boardIds
            for (int i = 0; i < bids.Length; ++i)
            {
                WriteShort(bids[i], ref p, packet);
            }
            PutCommand(packet, p);
        }
        public void SendResetSensor(ResetSensorFlags f) {
            byte[] packet = new byte[1000];
            int p = 0;
            WriteHeader((int)CommandId.CI_RESET_SENSOR, ref p, packet);
            WriteShort((short)f, ref p, packet);
            PutCommand(packet, p);
            if ((f & ResetSensorFlags.RSF_MOTOR) != 0 && pose != null)
            {
                for (int i = 0; i < pose.values.Count(); i++)
                {
                    pose.values[i] = pose.values[i] % SDEC.ONE;
                }
            }
        }
        public void SendSetIp()
        {
            byte[] packet = new byte[1000];
            int p = 0;
            WriteHeader((int)CommandId.CIU_SET_IPADDRESS, ref p, packet);
            PutCommand(packet, p);
        }
        public void SendGetIp() {
            byte[] packet = new byte[10];
            int p = 0;
            WriteHeader((int)CommandId.CIU_GET_IPADDRESS, ref p, packet);
            PutCommand(packet, p);
        }
        public void SendGetBoardInfo() {
            byte[] packet = new byte[10];
            int p = 0;
            WriteHeader((int)CommandId.CI_BOARD_INFO, ref p, packet);
            PutCommand(packet, p);
        }
        public void SendSensor() {
            byte[] packet = new byte[10];
            int p = 0;
            WriteHeader((int)CommandId.CI_SENSOR, ref p, packet);
            PutCommand(packet, p);
        }
        public void FindRobot()
        {
            sendPoint = null;
            Open();
            if (udpBc == null)
            {
                udpBc = new UdpClient(localBcPort);
            }
            byte[] packet = new byte[10];
            int p = 0;
            WriteHeader((int)CommandId.CIU_GET_IPADDRESS, ref p, packet);
            PokeCounter(0, 0, packet);         //  count
            PokeLengthAtHeader(4, 0, packet);  //  len
            udpBc.EnableBroadcast = true;
            System.Net.IPEndPoint ep = new System.Net.IPEndPoint(System.Net.IPAddress.Broadcast, remotePort);
            udpBc.Send(packet, p, ep);
            bFindRobot = true;
        }
        public void StopFindRobot()
        {
            bFindRobot = false;
        }
        //  For call backs
        public Control owner;
        public delegate void RobotFindHandlerType(System.Net.IPAddress adr);
        public event RobotFindHandlerType OnRobotFound = null;
        public delegate void UpdateRobotInfoHandlerType(byte[] prevMacAddress);
        public event UpdateRobotInfoHandlerType OnUpdateRobotInfo = null;
        public delegate void MessageRecieveHandlerType(int type, string msg);
        public event MessageRecieveHandlerType OnMessageReceive = null;
        public delegate void UpdateRobotStateHandlerType();
        public event UpdateRobotStateHandlerType OnUpdateRobotState = null;
        public event UpdateRobotStateHandlerType OnUpdateRobotParam = null;
    }
}
