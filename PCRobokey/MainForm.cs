//#define RUNTICK_DEBUG

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Xml;

namespace Robokey
{
    public partial class MainForm : Form
    {
        // Singleton Instance
        public static MainForm instance;

        public List<Pose> poses = new List<Pose>();
        public Motors motors = new Motors();
        int curTime;
        int sentTime;
        UdpComm udpComm;

        public MainForm()
        {
            System.Diagnostics.Debug.Assert(CommandId.CI_NCOMMAND <= CommandId.CI_NCOMMAND_MAX);
            instance = this;
            InitializeComponent();
            udpComm = new UdpComm(this);
            udpComm.OnRobotFound += OnRobotFound;
            udpComm.OnUpdateRobotInfo += OnUpdateRobotInfo;
            udpComm.OnUpdateRobotState += OnUpdateRobotState;
            udpComm.OnUpdateRobotParam += OnUpdateRobotParam;
            udpComm.OnMessageReceive += SetMessage;
            UpdateMotorPanel();
            udLoopTime_ValueChanged(udLoopTime, null);
            openPose.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            savePose.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
        }
        protected override void OnClosed(EventArgs e) {
            udpComm.owner = null;
            udpComm.OnUpdateRobotInfo -= OnUpdateRobotInfo;
            udpComm.OnUpdateRobotState -= OnUpdateRobotState;
            udpComm.OnUpdateRobotParam -= OnUpdateRobotParam;
            System.Threading.Thread.Sleep(100);
        }
        public void SetErrorMessage(string s)
        {
            txErrorMsg.Text = s;
        }
        public void SetMessage(int type, string s)
        {
            if (type == -1)
            {
                SetErrorMessage(s);
            }
            string ts = "";
            switch (type)
            {
                case -1:
                    ts = "E:";
                    break;
            }
            tbMessage.Text += "\r\n" + ts + s;
        }
        void UpdateMotorPanel()
        {
            Motor motor = null;
            flPose.Controls.Clear();
            flLength.Controls.Clear();
            flTorque.Controls.Clear();
            flPd.Controls.Clear();
            while (motors.Count > udpComm.RobotInfo.nMotor)
            {
                motors.RemoveAt(motors.Count - 1);
            }
            while (motors.Count < udpComm.RobotInfo.nMotor)
            {
                motor = new Motor();
                motors.Add(motor);
            }
            for (int i=0; i < udpComm.RobotInfo.nMotor; ++i)
            {
                motor = motors[i];
                motor.position.ValueChanged += GetEditedValue;
                flPose.Controls.Add(motor.position.panel);
                flLength.Controls.Add(motor.limit.panel);
                flTorque.Controls.Add(motor.torque.panel);
                flPd.Controls.Add(motor.pd.panel);
            }
            if (motor != null) flLength.Width = motor.limit.panel.Width * 3 + 20;
        }
        
        //  Return interpolated pose of poses in the timeline + Offset.
        PoseData Interpolate(double time)
        {
            if (poses.Count < 2) return null;
            int i;
            for (i = 0; i < poses.Count; ++i)
            {
                Pose pose = (Pose)poses[i];
                if (pose.Time > time) break;
            }
            if (i == 0) i = poses.Count;
            Pose pose0 = (Pose)poses[i - 1];
            Pose pose1 = (Pose)poses[i % poses.Count];
            double dt = pose1.Time - pose0.Time;
            if (dt < 0)
            {
                dt += track.Maximum + 1;
                if (time < pose1.Time) time += track.Maximum + 1;
            }
            double rate = (time - pose0.Time) / dt;
            PoseData rv = new PoseData(udpComm.RobotInfo.nMotor);
            for (int j = 0; j < udpComm.RobotInfo.nMotor; ++j)
            {
                double val = (1 - rate) * (int)pose0.values[j] + rate * (int)pose1.values[j];
                rv.values[j] = (int)val;// + motors[j].Offset;
            }
            rv.Time = (int)time % track.Maximum;
            return rv;
        }

        //  motorsにposeの値をロード
        bool SaveFromEditorGuard;
        void LoadToEditor(PoseData pose)
        {
            SaveFromEditorGuard = true;
            udkeyTime.Value = pose.Time;
            for (int i = 0; i < udpComm.RobotInfo.nMotor; ++i)
            {
                Motor m = (Motor)motors[i];
                int val = (int)pose.values[i];
                if (m.Maximum < val) val = m.Maximum;
                if (m.Minimum > val) val = m.Minimum;
                m.Value = val;
            }
            SaveFromEditorGuard = false;
        }
        void SaveFromEditor(Pose pose)
        {
            if (SaveFromEditorGuard) return;
            bool changeTime = pose.Time == track.Value;
            pose.Time = (int)udkeyTime.Value;
            for (int i = 0; i < udpComm.RobotInfo.nMotor; ++i)
            {
                pose.values[i] = ((Motor)motors[i]).Value;
            }
            if (changeTime) track.Value = pose.Time;
        }
        private void GetEditedValue(object sender, EventArgs e)
        {
            Pose pose = null;
            foreach (Pose p in poses)
            {
                if (p.Time == track.Value) pose = p;
            }
            if (pose == null)
            {
                if (SaveFromEditorGuard) return;
                SaveFromEditorGuard = true;
                pose = NewPose();
                udkeyTime.Value = track.Value;
                SaveFromEditorGuard = false;
                SaveFromEditor(pose);
                poses.Add(pose);
            }
            else
            {
                SaveFromEditor(pose);
            }
            poses.Sort();
            if (!ckRun.Checked)
            {
                udpComm.SendPoseDirect(pose + motors.Offset());
                udpComm.SendPackets();
            }
        }

        //  新しいPoseを作る。青いインジケータ(ボタン)も作る
        Pose NewPose()
        {
            Pose pose = new Pose(udpComm.RobotInfo.nMotor);
            Controls.Add(pose.button);
            pose.Time = track.Value;
            pose.button.BringToFront();
            pose.button.Click += pose_Click;
            pose.button.MouseDown += pose_MouseDown;
            pose.button.MouseMove += track_MouseMove;
            pose.button.MouseUp += track_MouseUp;
            return pose;
        }
        //  時間とトラックバーの座標変換
        public double TrackScale()
        {
            return (double)(track.Width - 27) / (double)track.Maximum;
        }
        //  時間とトラックバーの座標変換
        public double TrackOffset()
        {
            return track.Left + 13;
        }


        private void add_Click(object sender, EventArgs e)
        {
            Pose find = null;
            foreach (Pose pose in poses)
            {
                if (pose.Time == track.Value) find = pose;
            }
            if (find == null)
            {
                int time = track.Value;
                Pose pose = NewPose();
                SaveFromEditor(pose);
                if (time != track.Value)
                {
                    pose.Time = time;
                    track.Value = time;
                }
                poses.Add(pose);
            }
            poses.Sort();
        }
        private void del_Click(object sender, EventArgs e)
        {
            Pose find = null;
            foreach (Pose pose in poses)
            {
                if (pose.Time == track.Value) find = pose;
            }
            if (find != null)
            {
                Controls.Remove(find.button);
                poses.Remove(find);
                PoseData p = Interpolate(track.Value);
                if (p != null) LoadToEditor(p);
            }
        }

        private void pose_Click(object sender, EventArgs e)
        {
            Pose find = null;
            foreach (Pose pose in poses)
            {
                if (pose.button == sender) find = pose;
            }
            if (find != null) track.Value = find.Time;
        }

        int dragX, dragTime;
        Pose dragPose;
        private void pose_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            Pose find = null;
            foreach (Pose pose in poses)
            {
                if (pose.button == sender) find = pose;
            }
            if (find != null)
            {
                track.Value = find.Time;
                dragPose = find;
                dragX = Cursor.Position.X;
                dragTime = find.Time;
            }
        }
        private void track_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) dragPose = null;
            if (dragPose == null) return;
            int time = dragTime + (int)((Cursor.Position.X - dragX) / dragPose.Scale);
            if (time < 0) time = 0; if (time > track.Maximum) time = track.Maximum;
            dragPose.Time = time;
        }
        private void track_MouseUp(object sender, MouseEventArgs e)
        {
            if (dragPose == null) return;
            int time = dragTime + (int)((Cursor.Position.X - dragX) / dragPose.Scale);
            if (time < 0) time = 0; if (time > track.Maximum) time = track.Maximum;
            dragPose.Time = time;
            dragPose = null;
            poses.Sort();
        }

        private void track_ValueChanged(object sender, EventArgs e)
        {
            Pose pose = null;
            foreach (Pose p in poses)
            {
                if (p.Time == track.Value)
                {
                    pose = p;
                }
            }
            if (pose != null)
            {
                LoadToEditor(pose);
                udpComm.SendPoseDirect(pose + motors.Offset());
            }
            else
            {
                PoseData p = Interpolate(track.Value);
                if (p != null)
                {
                    LoadToEditor(p);
                    udpComm.SendPoseDirect(p + motors.Offset());
                }
            }
            udpComm.SendPackets();
        }

        private void udLoopTime_ValueChanged(object sender, EventArgs e)
        {
            track.Maximum = (int)udLoopTime.Value;
            udTime.Maximum = udLoopTime.Value;
            udkeyTime.Maximum = udLoopTime.Value;
            foreach (Pose pose in poses)
            {
                pose.Time = pose.Time < track.Maximum ? pose.Time : track.Maximum;
            }
        }

        private void UpdateCurTime(int time, bool bNoSend = false)
        {
            curTime = time;
            if (curTime > udLoopTime.Value)
            {
                curTime = 0;
                if (ckRunOnce.Checked == true) //モーションを一度だけ実行する場合
                {
                    ckRun.Checked = false;
                    udpComm.SendPoseDirect(Interpolate(curTime) + motors.Offset());
                    udpComm.SendPackets();
                }
            }
            laCurTime.Left = (int)(curTime * TrackScale() + TrackOffset() + laCurTime.Width / 2);
            lbCurTime.Text = curTime.ToString();
        }

        private void ckSense_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRunTimer();
        }
        private void ckForce_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRunTimer();
        }
        private void ckRun_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRunTimer();
        }
        void UpdateRunTimer()
        {
            sentTime = curTime;
            if (!ckRun.Checked)
            {
                udpComm.SendPoseDirect(Interpolate(curTime) + motors.Offset());
                udpComm.SendPackets();
            }
        }

        private void AppIdle(object sender, System.EventArgs e)
        {
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Application.Idle += new EventHandler(AppIdle);
            runTimer.Interval = (int)udTick.Value;
        }

        const int NINTERPOLATEFILL = 8; //  At least two must in buffer for interpolation.

        bool bCheckCorInQueue = false;
        private void runTimer_Tick(object sender, EventArgs e)
        {
            lbRecvCount.Text = udpComm.recvCount.ToString();
            lbSendCount.Text = udpComm.sendQueue.commandCount.ToString();
            Timer tmRun = (Timer)sender;
            
            // check the length of sendQueue
            if (udpComm.sendQueue.readAvail > 20)   //  if too much packets wait
            {
                //  Clear the send queue
                udpComm.sendQueue.Clear();
                System.Diagnostics.Debug.WriteLine("Too much commands (" + udpComm.sendQueue.readAvail + ") to send, clear them.");
            }
            //  Interpolate
            if (ckRun.Checked && poses.Count >= 2)
            {
                if (ckForce.Checked)
                {
                    udpComm.controlMode = UdpComm.ControlMode.CM_FORCE;
                }
                else {
                    udpComm.controlMode = UdpComm.ControlMode.CM_INTERPOLATE;
                }
#if true   //  interpolate on motor drivers
                int remain = (byte)(udpComm.interpolateTargetCountWrite - udpComm.interpolateTargetCountReadMax);
                int vacancy = udpComm.nInterpolateTotal - (byte)(udpComm.interpolateTargetCountWrite - udpComm.interpolateTargetCountReadMin);
                int diff = NINTERPOLATEFILL - remain;
                if (diff < 0)
                {
                    System.Diagnostics.Debug.WriteLine("Interpolation targets error diff = " + diff);
                    diff = 0;
                }
                if (vacancy < 2)
                {
                    diff = 0;
                }
                //  send targets
                if (diff > 0)
                {
                    bCheckCorInQueue = false;
                    for (int i = 0; i < diff; ++i)
                    {
                        curTime += tmRun.Interval * (int)udStep.Value;
                        UpdateCurTime(curTime, true);
                        if (ckRun.Checked)  //  onceの場合、UpdateCurTimeでckRunが切れる。
                        {
#if RUNTICK_DEBUG
                            System.Diagnostics.Debug.Write("CI_INT: tcr=" + udpComm.interpolateTargetCountReadMin);
                            System.Diagnostics.Debug.Write("-" + udpComm.interpolateTargetCountReadMax);
                            System.Diagnostics.Debug.Write(" tcw=" + udpComm.interpolateTargetCountWrite);
                            System.Diagnostics.Debug.Write("\tdiff=" + diff);
                            System.Diagnostics.Debug.Write("\tremain=" + remain);
                            System.Diagnostics.Debug.Write(" vac=" + vacancy);
                            System.Diagnostics.Debug.Write("\ttick=" + udpComm.interpolateTickMin);
                            System.Diagnostics.Debug.Write("-" + udpComm.interpolateTickMax);
                            System.Diagnostics.Debug.Write(" pr:" + (ushort)runTimer.Interval*3);
                            if (Interpolate(curTime) != null)
                            {
                                System.Diagnostics.Debug.Write("\tval[0]=" + Interpolate(curTime).values[0]);
                            }
                            System.Diagnostics.Debug.WriteLine(bCheckCorInQueue ? " chk" : "");
#endif
                            if (ckForce.Checked)
                            {
                                short[][] jacob = GetForceControlJacob();
                                udpComm.SendPoseForceControl(Interpolate(curTime) + motors.Offset(), (ushort)runTimer.Interval, jacob);
                            }
                            else
                            {
#if false                       //  test for update target sent later.
                                //  send last two data.
                                udpComm.interpolateTargetCountWrite-=2;
                                udpComm.SendPoseInterpolate(Interpolate(curTime - 2*runTimer.Interval) + motors.Offset(), (ushort)runTimer.Interval);
                                udpComm.SendPoseInterpolate(Interpolate(curTime - runTimer.Interval) + motors.Offset(), (ushort)runTimer.Interval);
#endif
                                udpComm.SendPoseInterpolate(Interpolate(curTime) + motors.Offset(), (ushort)runTimer.Interval);
                            }
                        }
                    }   //  for
                }
                else {  //  diff = 0
                    /*  Need to get latest tcr (target count for read) from robot
                     *  For this purpose, the CI_INTERPOALTE or CI_FORCE_CONTROL with period 0 
                     *  is processed without regarding command count.
                     *  It always executed on the robot side and the robot send return packet for every requrest.
                     */  
                    if (bCheckCorInQueue) {
                        if (udpComm.sendQueue.nWait == 0)
                        {
                            bCheckCorInQueue = false;
                        }
                    }
                    if (!bCheckCorInQueue)
                    {
                        bCheckCorInQueue = true;
                        if (ckForce.Checked)
                        {
                            short[][] jacob = GetForceControlJacob();
                            udpComm.SendPoseForceControl(Interpolate(curTime) + motors.Offset(), (ushort)runTimer.Interval, jacob);
                        }
                        else
                        {
                            //  send command to receive latest cor and cow.
                            udpComm.SendPoseInterpolate(Interpolate(curTime) + motors.Offset(), 0);
                        }
                    }
                }
#else   //  use direct
                UpdateCurTime(curTime += tmRun.Interval * (int)udStep.Value);
                udpComm.SendPoseDirect(Interpolate(curTime) + motors.Offset());
#endif
            }
            else if (ckForce.Checked) // !ckRun.Checked && ckForce.Checked
            {   //  Force control without motion; Send jacobian.
                short[][] jacob = GetForceControlJacob();
                udpComm.SendPoseForceControl(Interpolate(curTime) + motors.Offset(), (ushort)runTimer.Interval, jacob);
            }
            //  Read sensor
            if (ckSense.Checked)
            {
                udpComm.SendSensor();
            }
            udpComm.SendPackets();
        }
        short[][] GetForceControlJacob() {
            double[] mpos = new double[motors.Count];
            for (int i = 0; i < motors.Count; ++i)
            {
                mpos[i] = udpComm.pose.values[i] - motors[i].Offset;
            }
            short[][] jacob = new short[udpComm.RobotInfo.nForce][];
            for (int i=0; i< jacob.Count(); ++i)
            {
                jacob[i] = new short[3];
            }
            if (udpComm.RobotInfo.nForce >= 2)
            {
                jeLeft.Jacobian(jacob, 0, mpos);
            }
            if (udpComm.RobotInfo.nForce >= 4)
            {
                jeRight.Jacobian(jacob, 2, mpos);
                for (int i = 2; i <4; ++i)
                {
                    jacob[i][0] = 0;
                    jacob[i][1] = 0;
                    jacob[i][2] = 0;
                }
            }
            return jacob;
        }
        void SendTorqueLimit() {
            int[] minT = new int[motors.Count];
            int[] maxT = new int[motors.Count];
            for (int i = 0; i < motors.Count; ++i)
            {
                minT[i] = motors[i].torque.Minimum;
                maxT[i] = motors[i].torque.Maximum;
            }
            udpComm.SendParamTorqueLimit(motors.Count, minT, maxT);
            udpComm.SendPackets();
        }
        private void ckMotor_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (cb.Checked)
            {
                SendTorqueLimit();
            }
            else
            {
                int[] zeros = new int[motors.Count];
                for (int i = 0; i < motors.Count; ++i)
                {
                    zeros[i] = 0;
                }
                udpComm.SendParamTorqueLimit(motors.Count, zeros, zeros);
            }
            udpComm.SendPackets();
        }


        private void btResetMotors_Click(object sender, EventArgs e)
        {
            udpComm.SendResetSensor(ResetSensorFlags.RSF_MOTOR);
            udpComm.SendPackets();
            for (int i = 0; i < motors.Count; ++i)
            {
                motors[i].Offset = udpComm.pose.values[i];
            }
            for (int i = 0; i < motors.Count; ++i)
            {
                motors[i].Value = 0;
            }
        }

        private void btFindRobot_Click(object sender, EventArgs e)
        {
            if (btFindRobot.Text.CompareTo("Close") == 0)
            {
                //SaveSetting(udpComm.RobotInfo.macAddress);
                udpComm.SendPoseDirect(Interpolate(curTime));
                udpComm.SendPackets();
                udpComm.Close();
                btFindRobot.Text = "Find Robot";
                laPort.Text = "Closed";
                ckRun.Checked = false;
            }
            else
            {
                udpComm.FindRobot();
            }
        }
        private void OnBtRobotClick(object sender, EventArgs e)
        {
            Button bt = (Button)sender;
            udpComm.StopFindRobot();
            udpComm.SetAddress(bt.Text);
            laPort.Text = "IP " + udpComm.sendPoint.Address;
            btFindRobot.Text = "Close";
            fpFoundRobot.Controls.Clear();
            Refresh();
            udpComm.Open();
            udpComm.SendSetIp();
            udpComm.SendGetBoardInfo();
            udpComm.SendGetParam(SetParamType.PT_PD);           //  Request to set next type as PT_PD.
            udpComm.SendGetParam(SetParamType.PT_CURRENT);      //  next = PT_CURRENT and get PT_PD.
            udpComm.SendGetParam(SetParamType.PT_TORQUE_LIMIT); //  next = PT_TORQUE_LIMIT and get PT_CURRENT.
            udpComm.SendGetParam(SetParamType.PT_PD);           //  PT_TORQUE_LIMIT will return on this request.
            udpComm.SendPackets();
        }
        internal void OnRobotFound(System.Net.IPAddress adr)
        {
            string astr = adr.ToString();
            bool bFound = false;
            foreach (Control c in fpFoundRobot.Controls)
            {
                Button b = (Button)c;
                if (b.Text.CompareTo(astr) == 0)
                {
                    bFound = true;
                    break;
                }
            }
            if (!bFound)
            {
                Button bt = new Button();
                bt.Text = astr;
                bt.Click += OnBtRobotClick;
                bt.Width = fpFoundRobot.Width - 10;
                fpFoundRobot.Controls.Add(bt);
            }
        }
        void OnUpdateRobotInfo(byte[] prevMacAddress)
        {
            //            byte [] zeroMacAddress = new byte[6]{0,0,0,0,0,0};
            //            if (!prevMacAddress.SequenceEqual(zeroMacAddress))
            if (motors.Count > 0)
            {
                //SaveSetting(prevMacAddress);
            }
            //LoadSetting(udpComm.RobotInfo.macAddress);
            UpdateMotorPanel();
            //SendTorqueLimit();
            //SendPd();
            udpComm.SendPoseDirect(Interpolate(curTime));
            udpComm.SendPackets();
        }

        private void tbMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'c' || e.KeyChar == 'C' || e.KeyChar == 'l' || e.KeyChar == 'L')
            {
                tbMessage.Text = "";
            }
        }

        private void udTick_ValueChanged(object sender, EventArgs e)
        {
            runTimer.Interval = (int)udTick.Value;
        }

        private void btSendPD_Click(object sender, EventArgs e)
        {
            SendPd();
        }
        void SendPd() {
            int n = udpComm.RobotInfo.nMotor;
            int[] k = new int[n];
            int[] b = new int[n];
            int[] a = new int[n];
            for (int i = 0; i < n; ++i)
            {
                k[i] = motors[i].pd.K;
                b[i] = motors[i].pd.B;
                a[i] = motors[i].pd.A;
            }
            udpComm.SendParamPd(n, k, b);
            udpComm.SendParamCurrent(n, a);
            udpComm.SendPackets();
        }

        void OnUpdateRobotParam()
        {
            for (int i = 0; i < udpComm.RobotInfo.nMotor; ++i)
            {
                motors[i].torque.Minimum = udpComm.torqueMin[i];
                motors[i].torque.Maximum = udpComm.torqueMax[i];
                motors[i].pd.K = udpComm.paramK[i];
                motors[i].pd.B = udpComm.paramB[i];
                motors[i].pd.A = udpComm.paramA[i];
            }

        }
        void OnUpdateRobotState()
        {
            tbState.Text = "Motor:";
            for (int i = 0; i < udpComm.pose.nMotor; ++i)
            {
                int v = udpComm.pose.values[i] - motors[i].Offset;
                tbState.Text += string.Format("{0,9}", v.ToString("D"));
            }
            tbState.Text += "\r\nCurrent:";
            for (int i = 0; i < udpComm.current.Length; ++i)
            {
                tbState.Text += string.Format("{0,9}", udpComm.current[i].ToString("D"));
            }
            tbState.Text += "\r\nForce:";
            for (int i = 0; i < udpComm.force.Length; ++i)
            {
                tbState.Text += string.Format("{0,9}", udpComm.force[i].ToString("D"));
            }
            if (udpComm.force.Length >= 2)
            {
                jeLeft.SetCurForce(udpComm.force[0], udpComm.force[1]);
            }
            if (udpComm.force.Length >= 4)
            {
                jeRight.SetCurForce(udpComm.force[0], udpComm.force[1]);
            }
            tbState.Text += "\r\nTouch:";
            for (int i = 0; i < udpComm.touch.Length; ++i)
            {
                tbState.Text += string.Format("{0,9}", udpComm.touch[i].ToString("D"));
            }
        }

        //  obsolute
        void SaveSetting(byte[] adr)
        {
            var serializer = new DataContractSerializer(motors.GetType());
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new System.Text.UTF8Encoding(false);
            string str = System.AppDomain.CurrentDomain.BaseDirectory;
            str += "SR";
            foreach (int a in adr)
            {
                str += a.ToString("X2");
            }
            str += ".xml";
            XmlWriter xw = XmlWriter.Create(str, settings);
            //シリアル化し、XMLファイルに保存する
            serializer.WriteObject(xw, motors);
            //ファイルを閉じる
            xw.Close();
        }

        private void btSendTorque_Click(object sender, EventArgs e)
        {
            SendTorqueLimit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //SaveSetting(udpComm.RobotInfo.macAddress);
            udpComm.Close();
        }

        public StreamWriter logFile;
        private void ckLog_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox c = (CheckBox)sender;
            if (c.Checked)
            {
                string name = "Log_" + DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + ".txt";
                logFile = new StreamWriter(name);
                udpComm.log = logFile;
            }
            else {
                logFile.Close();
                logFile = null;
                udpComm.log = logFile;
            }
        }

        private void ReadJacobianEditor(Object sender, EventArgs e) {
            UCJacobianEditor je = (UCJacobianEditor)sender;
            je.WriteToMotors(motors);
        }

        private void btCalibForce_Click(object sender, EventArgs e)
        {
            udpComm.SendResetSensor(ResetSensorFlags.RSF_FORCE);
        }

        private void btSetLimitMax_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < motors.Count; ++i)
            {
                motors[i].torque.udMax.Value = motors[i].torque.udMax.Maximum;
                motors[i].torque.udMin.Value = motors[i].torque.udMin.Minimum;
            }
        }

        //  obsolute
        void LoadSetting(byte[] adr)
        {
            string str = System.AppDomain.CurrentDomain.BaseDirectory;
            str += "SR";
            foreach (int a in adr)
            {
                str += a.ToString("X2");
            }
            str += ".xml";
            //DataContractSerializerオブジェクトを作成
            DataContractSerializer serializer =
                new DataContractSerializer(motors.GetType());
            //読み込むファイルを開く
            XmlReader xr = null;
            try
            {
                xr = XmlReader.Create(str);
                //XMLファイルから読み込み、逆シリアル化する
                motors = (Motors)serializer.ReadObject(xr);
                //ファイルを閉じる
            }
            catch (Exception)
            {
            }
            if (xr != null) xr.Close();
        }
    }
}
