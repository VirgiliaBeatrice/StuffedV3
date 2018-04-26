﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace Robokey
{
    public partial class Form1 : Form
    {
        // Singleton Instance
        public static Form1 instance;


        public List<Pose> poses = new List<Pose>();
        public List<Motor> motors = new List<Motor>();
        int curTime;
        int sentTime;
        UdpComm udpComm;

        public Form1()
        {
            instance = this;
            InitializeComponent();
            udpComm = new UdpComm(this, runTimer.Interval);
            udpComm.OnRobotFound += OnRobotFound;
            udpComm.OnUpdateRobotInfo += OnUpdateRobotInfo;
            udpComm.OnUpdateRobotState += OnUpdateRobotState;
            udpComm.OnMessageReceive += SetMessage;
            UpdateMotorPanel();
            udLoopTime_ValueChanged(udLoopTime, null);
            openPose.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            savePose.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
        }

        ~Form1()
        {
            udpComm.Close();
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
            motors.Clear();
            Motor motor = null;
            flPose.Controls.Clear();
            flLength.Controls.Clear();
            flTorque.Controls.Clear();
            for (int i = 0; i < udpComm.RobotInfo.nMotor; ++i)
            {
                motor = new Motor();
                motor.position.ValueChanged += GetEditedValue;
                motors.Add(motor);
                flPose.Controls.Add(motor.position.panel);
                flLength.Controls.Add(motor.limit.panel);
                flTorque.Controls.Add(motor.torque.panel);
            }
            if (motor != null) flLength.Width = motor.limit.panel.Width * 3 + 20;
        }

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
                rv.values[j] = (int)val;
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
                udpComm.SendPoseDirect(pose);
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
                udpComm.SendPoseDirect(pose);
            }
            else
            {
                PoseData p = Interpolate(track.Value);
                if (p != null)
                {
                    LoadToEditor(p);
                    udpComm.SendPoseDirect(p);
                }
            }
        }
        private void btLoad_Click(object sender, EventArgs e)
        {
            openPose.ShowDialog();
        }
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            LoadMotion(openPose.FileName);
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            savePose.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(savePose.FileName);
            file.Write(poses.Count); file.Write("\t");
            file.Write(udpComm.RobotInfo.nMotor); file.Write("\t");
            file.Write(udLoopTime.Value); file.Write("\n");
            int lastTime = 0;
            for (int h = 0; h < poses.Count; ++h)
            {
                Pose pose = (Pose)poses[h];
                file.Write(pose.Time - lastTime);
                lastTime = pose.Time;
                for (int i = 0; i < udpComm.RobotInfo.nMotor; ++i)
                {
                    file.Write("\t");
                    file.Write(pose.values[i]);
                }
                file.Write("\n");
            }
            file.Close();
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
                    runTimer.Enabled = false;
                    udpComm.SendPoseDirect(Interpolate(curTime));
                }
            }
            laCurTime.Left = (int)(curTime * TrackScale() + TrackOffset() + laCurTime.Width / 2);
            lbCurTime.Text = curTime.ToString();
        }

        private void ckRun_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRunTimer();
        }
        void UpdateRunTimer()
        {
            runTimer.Enabled = ckRun.Checked || ckSense.Checked;
            sentTime = curTime;
            if (!ckRun.Checked)
            {
                udpComm.SendPoseDirect(Interpolate(curTime));
            }
        }
        private void udTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                UpdateCurTime((int)udTime.Value);
            udpComm.SendPoseDirect(Interpolate(curTime));
        }

        private void btCopy_Click(object sender, EventArgs e)
        {
            String str = "";
            for (int i = 0; i < udpComm.RobotInfo.nMotor; ++i)
            {
                str += ((Motor)motors[i]).Value;
                if (i < udpComm.RobotInfo.nMotor - 1) str += "\t";
            }
            Clipboard.SetDataObject(str);
        }

        private void updateSensorText(object sender, EventArgs e)
        {
        }

        public bool LoadMotion(String Filename)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(Filename);
            String[] cells;
            do
            {
                cells = file.ReadLine().Split('\t');
            } while (cells[0].IndexOf('#') != -1);

            int nPose = int.Parse(cells[0]);
            if (udpComm.IsConnected && udpComm.RobotInfo.nMotor != int.Parse(cells[1]))
            {
                SetErrorMessage("Dofs of file data and connected system do not match.");
                return false;
            }
            else
            {
                RobotInfo info = udpComm.RobotInfo;
                info.nMotor = int.Parse(cells[1]);
                udpComm.SetRobotInfo(info);
            }
            udLoopTime.Value = int.Parse(cells[2]);
            foreach (Pose pose in poses)
            {
                Controls.Remove(pose.button);
            }
            poses.Clear();
            int lastTime = 0;
            while (true)
            {
                String line = file.ReadLine();
                if (line == null) break;
                if (line.IndexOf('#') != -1) continue;
                String[] cells2 = line.Split('\t');
                if (cells2.Count() < udpComm.RobotInfo.nMotor + 1) break;
                Pose pose = NewPose();
                lastTime += int.Parse(cells2[0]);
                pose.Time = lastTime;
                for (int i = 0; i < udpComm.RobotInfo.nMotor; ++i)
                {
                    pose.values[i] = int.Parse(cells2[i + 1]);
                }
                poses.Add(pose);
            }
            file.Close();
            //udLoopTime.Value = lastTime;
            PoseData p = Interpolate(track.Value);
            if (p != null) LoadToEditor(p);

            return (true);
        }

        private void AppIdle(object sender, System.EventArgs e)
        {
            updateSensorText(sender, e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Application.Idle += new EventHandler(AppIdle);
        }

        const int NINTERPOLATEFILL = 6; //  At least two must in buffer for interpolation.

        int zeroDiffCount = 0;
        private void runTimer_Tick(object sender, EventArgs e)
        {
            Timer tmRun = (Timer)sender;
            if (ckRun.Checked)
            {
                int diff = NINTERPOLATEFILL - udpComm.nInterpolateRest;
                System.Diagnostics.Debug.Write("RunTimer: rest = ");
                System.Diagnostics.Debug.Write(udpComm.nInterpolateRest);
                System.Diagnostics.Debug.WriteLine(".");
                if (diff < 1)
                {
                    zeroDiffCount++;
                    if (zeroDiffCount > 10)
                    {
                        diff = 1;
                    }
                }
                for (int i = 0; i < diff; ++i)
                {
                    UpdateCurTime(curTime += tmRun.Interval * (int)udStep.Value, true);
                    if (runTimer.Enabled)
                    {
                        udpComm.SendPoseInterpolate(Interpolate(curTime));
                    }
                }
            }
            if (ckSense.Checked)
            {
                udpComm.SendSensor();
            }
        }


        private void ckMotor_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (cb.Checked)
            {
                int[] minT = new int[motors.Count];
                int[] maxT = new int[motors.Count];
                for (int i = 0; i < motors.Count; ++i)
                {
                    minT[i] = motors[i].torque.Minimum;
                    maxT[i] = motors[i].torque.Maximum;
                }
                udpComm.SendTorqueLimit(motors.Count, minT, maxT);
            }
            else
            {
                int[] zeros = new int[motors.Count];
                for (int i = 0; i < motors.Count; ++i)
                {
                }
                udpComm.SendTorqueLimit(motors.Count, zeros, zeros);
            }
        }


        private void btResetMotors_Click(object sender, EventArgs e)
        {

        }

        private void btFindRobot_Click(object sender, EventArgs e)
        {
            if (btFindRobot.Text.CompareTo("Close") == 0)
            {
                udpComm.Close();
                btFindRobot.Text = "Find Robot";
                laPort.Text = "Closed";
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
            fpFoundRobot.Controls.Clear();
            btFindRobot.Text = "Close";
            Refresh();

            udpComm.Open();
            udpComm.SendSetIp();
            udpComm.SendGetBoardInfo();
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
        void OnUpdateRobotInfo()
        {
            UpdateMotorPanel();
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

        private void ckSense_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRunTimer();
        }

        void OnUpdateRobotState()
        {
            tbState.Text = "Motor:";
            for (int i = 0; i < udpComm.pose.nMotor; ++i)
            {
                double v = SDEC.toDouble(udpComm.pose.values[i]);
                tbState.Text += string.Format("{0,9}", v.ToString("F3"));
            }
            tbState.Text += "\r\nForce:";
            for (int i = 0; i < udpComm.force.Length; ++i)
            {
                tbState.Text += string.Format("{0,9}", udpComm.force[i].ToString());
            }
        }
    }
}
