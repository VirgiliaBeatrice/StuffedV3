namespace Robokey
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.track = new System.Windows.Forms.TrackBar();
            this.openPose = new System.Windows.Forms.OpenFileDialog();
            this.savePose = new System.Windows.Forms.SaveFileDialog();
            this.laCurTime = new System.Windows.Forms.Label();
            this.panelCom = new System.Windows.Forms.Panel();
            this.txErrorMsg = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lbRecvCount = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lbSendCount = new System.Windows.Forms.Label();
            this.btCalibForce = new System.Windows.Forms.Button();
            this.ckLog = new System.Windows.Forms.CheckBox();
            this.ckSense = new System.Windows.Forms.CheckBox();
            this.fpFoundRobot = new System.Windows.Forms.FlowLayoutPanel();
            this.btFindRobot = new System.Windows.Forms.Button();
            this.ckMotor = new System.Windows.Forms.CheckBox();
            this.btResetMotors = new System.Windows.Forms.Button();
            this.runTimer = new System.Windows.Forms.Timer(this.components);
            this.panelTop = new System.Windows.Forms.Panel();
            this.del = new System.Windows.Forms.Button();
            this.add = new System.Windows.Forms.Button();
            this.ckForce = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.udTick = new System.Windows.Forms.NumericUpDown();
            this.laPort = new System.Windows.Forms.Label();
            this.udTime = new System.Windows.Forms.NumericUpDown();
            this.udLoopTime = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.udStep = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.lbCurTime = new System.Windows.Forms.Label();
            this.ckRun = new System.Windows.Forms.CheckBox();
            this.ckRunOnce = new System.Windows.Forms.CheckBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPoseEditor = new System.Windows.Forms.TabPage();
            this.flPose = new System.Windows.Forms.FlowLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btCopy = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.btSave = new System.Windows.Forms.Button();
            this.btLoad = new System.Windows.Forms.Button();
            this.udkeyTime = new System.Windows.Forms.NumericUpDown();
            this.tabMotors = new System.Windows.Forms.TabPage();
            this.flLength = new System.Windows.Forms.FlowLayoutPanel();
            this.tabTorque = new System.Windows.Forms.TabPage();
            this.flTorque = new System.Windows.Forms.FlowLayoutPanel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.btSetLimitMax = new System.Windows.Forms.Button();
            this.btSendTorque = new System.Windows.Forms.Button();
            this.tabPdParam = new System.Windows.Forms.TabPage();
            this.flPd = new System.Windows.Forms.FlowLayoutPanel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.btSendPD = new System.Windows.Forms.Button();
            this.tabLForce = new System.Windows.Forms.TabPage();
            this.jeLeft = new Robokey.UCJacobianEditor();
            this.tabRForce = new System.Windows.Forms.TabPage();
            this.jeRight = new Robokey.UCJacobianEditor();
            this.tabInfo = new System.Windows.Forms.TabControl();
            this.tpState = new System.Windows.Forms.TabPage();
            this.tbState = new System.Windows.Forms.TextBox();
            this.tpMessage = new System.Windows.Forms.TabPage();
            this.tbMessage = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.track)).BeginInit();
            this.panelCom.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udTick)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udLoopTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udStep)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabPoseEditor.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udkeyTime)).BeginInit();
            this.tabMotors.SuspendLayout();
            this.tabTorque.SuspendLayout();
            this.panel4.SuspendLayout();
            this.tabPdParam.SuspendLayout();
            this.panel3.SuspendLayout();
            this.tabLForce.SuspendLayout();
            this.tabRForce.SuspendLayout();
            this.tabInfo.SuspendLayout();
            this.tpState.SuspendLayout();
            this.tpMessage.SuspendLayout();
            this.SuspendLayout();
            // 
            // track
            // 
            this.track.AutoSize = false;
            this.track.Dock = System.Windows.Forms.DockStyle.Top;
            this.track.LargeChange = 20;
            this.track.Location = new System.Drawing.Point(0, 0);
            this.track.Margin = new System.Windows.Forms.Padding(0);
            this.track.Maximum = 10000;
            this.track.Name = "track";
            this.track.Size = new System.Drawing.Size(935, 50);
            this.track.TabIndex = 0;
            this.track.TickStyle = System.Windows.Forms.TickStyle.None;
            this.track.ValueChanged += new System.EventHandler(this.track_ValueChanged);
            this.track.MouseMove += new System.Windows.Forms.MouseEventHandler(this.track_MouseMove);
            this.track.MouseUp += new System.Windows.Forms.MouseEventHandler(this.track_MouseUp);
            // 
            // openPose
            // 
            this.openPose.FileName = "pose.txt";
            this.openPose.Filter = "姿勢ファイル|*.txt|姿勢ファイル(csv)|*.csv|すべてのファイル|*.*";
            this.openPose.InitialDirectory = "Motion";
            this.openPose.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // savePose
            // 
            this.savePose.DefaultExt = "txt";
            this.savePose.FileName = "pose.txt";
            this.savePose.Filter = "姿勢ファイル|*.txt|姿勢CSV|*.csv|すべてのファイル|*.*";
            this.savePose.FileOk += new System.ComponentModel.CancelEventHandler(this.saveFileDialog1_FileOk);
            // 
            // laCurTime
            // 
            this.laCurTime.BackColor = System.Drawing.Color.Red;
            this.laCurTime.Location = new System.Drawing.Point(842, 35);
            this.laCurTime.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.laCurTime.Name = "laCurTime";
            this.laCurTime.Size = new System.Drawing.Size(4, 7);
            this.laCurTime.TabIndex = 12;
            // 
            // panelCom
            // 
            this.panelCom.Controls.Add(this.txErrorMsg);
            this.panelCom.Controls.Add(this.panel2);
            this.panelCom.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelCom.Location = new System.Drawing.Point(797, 87);
            this.panelCom.Margin = new System.Windows.Forms.Padding(2);
            this.panelCom.Name = "panelCom";
            this.panelCom.Size = new System.Drawing.Size(138, 484);
            this.panelCom.TabIndex = 66;
            // 
            // txErrorMsg
            // 
            this.txErrorMsg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txErrorMsg.Location = new System.Drawing.Point(0, 312);
            this.txErrorMsg.Margin = new System.Windows.Forms.Padding(2);
            this.txErrorMsg.Multiline = true;
            this.txErrorMsg.Name = "txErrorMsg";
            this.txErrorMsg.ReadOnly = true;
            this.txErrorMsg.Size = new System.Drawing.Size(138, 172);
            this.txErrorMsg.TabIndex = 68;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.lbRecvCount);
            this.panel2.Controls.Add(this.label5);
            this.panel2.Controls.Add(this.lbSendCount);
            this.panel2.Controls.Add(this.btCalibForce);
            this.panel2.Controls.Add(this.ckLog);
            this.panel2.Controls.Add(this.ckSense);
            this.panel2.Controls.Add(this.fpFoundRobot);
            this.panel2.Controls.Add(this.btFindRobot);
            this.panel2.Controls.Add(this.ckMotor);
            this.panel2.Controls.Add(this.btResetMotors);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Margin = new System.Windows.Forms.Padding(2);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(138, 312);
            this.panel2.TabIndex = 69;
            // 
            // lbRecvCount
            // 
            this.lbRecvCount.AutoSize = true;
            this.lbRecvCount.Location = new System.Drawing.Point(68, 176);
            this.lbRecvCount.Name = "lbRecvCount";
            this.lbRecvCount.Size = new System.Drawing.Size(15, 15);
            this.lbRecvCount.TabIndex = 84;
            this.lbRecvCount.Text = "0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(47, 176);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(15, 15);
            this.label5.TabIndex = 83;
            this.label5.Text = ">";
            // 
            // lbSendCount
            // 
            this.lbSendCount.AutoSize = true;
            this.lbSendCount.Location = new System.Drawing.Point(5, 176);
            this.lbSendCount.Name = "lbSendCount";
            this.lbSendCount.Size = new System.Drawing.Size(15, 15);
            this.lbSendCount.TabIndex = 82;
            this.lbSendCount.Text = "0";
            // 
            // btCalibForce
            // 
            this.btCalibForce.Location = new System.Drawing.Point(6, 278);
            this.btCalibForce.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btCalibForce.Name = "btCalibForce";
            this.btCalibForce.Size = new System.Drawing.Size(119, 28);
            this.btCalibForce.TabIndex = 81;
            this.btCalibForce.Text = "&Calib force";
            this.btCalibForce.UseVisualStyleBackColor = true;
            this.btCalibForce.Click += new System.EventHandler(this.btCalibForce_Click);
            // 
            // ckLog
            // 
            this.ckLog.AutoSize = true;
            this.ckLog.Location = new System.Drawing.Point(84, 197);
            this.ckLog.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ckLog.Name = "ckLog";
            this.ckLog.Size = new System.Drawing.Size(52, 19);
            this.ckLog.TabIndex = 80;
            this.ckLog.Text = "Log";
            this.ckLog.UseVisualStyleBackColor = true;
            this.ckLog.CheckedChanged += new System.EventHandler(this.ckLog_CheckedChanged);
            // 
            // ckSense
            // 
            this.ckSense.AutoSize = true;
            this.ckSense.Location = new System.Drawing.Point(7, 197);
            this.ckSense.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ckSense.Name = "ckSense";
            this.ckSense.Size = new System.Drawing.Size(69, 19);
            this.ckSense.TabIndex = 79;
            this.ckSense.Text = "&Sense";
            this.ckSense.UseVisualStyleBackColor = true;
            this.ckSense.CheckedChanged += new System.EventHandler(this.ckSense_CheckedChanged);
            // 
            // fpFoundRobot
            // 
            this.fpFoundRobot.AutoScroll = true;
            this.fpFoundRobot.Location = new System.Drawing.Point(0, 32);
            this.fpFoundRobot.Margin = new System.Windows.Forms.Padding(2);
            this.fpFoundRobot.Name = "fpFoundRobot";
            this.fpFoundRobot.Size = new System.Drawing.Size(138, 142);
            this.fpFoundRobot.TabIndex = 45;
            // 
            // btFindRobot
            // 
            this.btFindRobot.Location = new System.Drawing.Point(4, 3);
            this.btFindRobot.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btFindRobot.Name = "btFindRobot";
            this.btFindRobot.Size = new System.Drawing.Size(130, 28);
            this.btFindRobot.TabIndex = 44;
            this.btFindRobot.Text = "&Find Robot";
            this.btFindRobot.UseVisualStyleBackColor = true;
            this.btFindRobot.Click += new System.EventHandler(this.btFindRobot_Click);
            // 
            // ckMotor
            // 
            this.ckMotor.AutoSize = true;
            this.ckMotor.Checked = true;
            this.ckMotor.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ckMotor.Location = new System.Drawing.Point(7, 222);
            this.ckMotor.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ckMotor.Name = "ckMotor";
            this.ckMotor.Size = new System.Drawing.Size(104, 19);
            this.ckMotor.TabIndex = 35;
            this.ckMotor.Text = "&Drive Motor";
            this.ckMotor.UseVisualStyleBackColor = true;
            this.ckMotor.CheckedChanged += new System.EventHandler(this.ckMotor_CheckedChanged);
            // 
            // btResetMotors
            // 
            this.btResetMotors.Location = new System.Drawing.Point(6, 246);
            this.btResetMotors.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btResetMotors.Name = "btResetMotors";
            this.btResetMotors.Size = new System.Drawing.Size(119, 28);
            this.btResetMotors.TabIndex = 43;
            this.btResetMotors.Text = "&Reset motors";
            this.btResetMotors.UseVisualStyleBackColor = true;
            this.btResetMotors.Click += new System.EventHandler(this.btResetMotors_Click);
            // 
            // runTimer
            // 
            this.runTimer.Enabled = true;
            this.runTimer.Interval = 50;
            this.runTimer.Tick += new System.EventHandler(this.runTimer_Tick);
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.del);
            this.panelTop.Controls.Add(this.add);
            this.panelTop.Controls.Add(this.ckForce);
            this.panelTop.Controls.Add(this.label3);
            this.panelTop.Controls.Add(this.udTick);
            this.panelTop.Controls.Add(this.laPort);
            this.panelTop.Controls.Add(this.udTime);
            this.panelTop.Controls.Add(this.udLoopTime);
            this.panelTop.Controls.Add(this.label1);
            this.panelTop.Controls.Add(this.udStep);
            this.panelTop.Controls.Add(this.label10);
            this.panelTop.Controls.Add(this.lbCurTime);
            this.panelTop.Controls.Add(this.ckRun);
            this.panelTop.Controls.Add(this.ckRunOnce);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 50);
            this.panelTop.Margin = new System.Windows.Forms.Padding(2);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(935, 37);
            this.panelTop.TabIndex = 69;
            // 
            // del
            // 
            this.del.Location = new System.Drawing.Point(702, 2);
            this.del.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.del.Name = "del";
            this.del.Size = new System.Drawing.Size(66, 27);
            this.del.TabIndex = 67;
            this.del.Text = "&Del";
            this.del.UseVisualStyleBackColor = true;
            this.del.Click += new System.EventHandler(this.del_Click);
            // 
            // add
            // 
            this.add.Location = new System.Drawing.Point(635, 2);
            this.add.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.add.Name = "add";
            this.add.Size = new System.Drawing.Size(66, 27);
            this.add.TabIndex = 66;
            this.add.Text = "&Add";
            this.add.UseVisualStyleBackColor = true;
            this.add.Click += new System.EventHandler(this.add_Click);
            // 
            // ckForce
            // 
            this.ckForce.AutoSize = true;
            this.ckForce.Location = new System.Drawing.Point(539, 8);
            this.ckForce.Margin = new System.Windows.Forms.Padding(2);
            this.ckForce.Name = "ckForce";
            this.ckForce.Size = new System.Drawing.Size(94, 19);
            this.ckForce.TabIndex = 79;
            this.ckForce.Text = "Force Ctrl";
            this.ckForce.UseVisualStyleBackColor = true;
            this.ckForce.CheckedChanged += new System.EventHandler(this.ckForce_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(238, 11);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(25, 15);
            this.label3.TabIndex = 78;
            this.label3.Text = "ms";
            // 
            // udTick
            // 
            this.udTick.Location = new System.Drawing.Point(178, 7);
            this.udTick.Margin = new System.Windows.Forms.Padding(2);
            this.udTick.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.udTick.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udTick.Name = "udTick";
            this.udTick.Size = new System.Drawing.Size(58, 22);
            this.udTick.TabIndex = 77;
            this.udTick.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.udTick.ValueChanged += new System.EventHandler(this.udTick_ValueChanged);
            // 
            // laPort
            // 
            this.laPort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.laPort.AutoSize = true;
            this.laPort.Location = new System.Drawing.Point(799, 17);
            this.laPort.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.laPort.Name = "laPort";
            this.laPort.Size = new System.Drawing.Size(50, 15);
            this.laPort.TabIndex = 76;
            this.laPort.Text = "Closed";
            // 
            // udTime
            // 
            this.udTime.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.udTime.ForeColor = System.Drawing.SystemColors.MenuText;
            this.udTime.Location = new System.Drawing.Point(358, 7);
            this.udTime.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.udTime.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.udTime.Name = "udTime";
            this.udTime.Size = new System.Drawing.Size(80, 22);
            this.udTime.TabIndex = 70;
            this.udTime.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.udTime_KeyPress);
            // 
            // udLoopTime
            // 
            this.udLoopTime.Location = new System.Drawing.Point(451, 7);
            this.udLoopTime.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.udLoopTime.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.udLoopTime.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.udLoopTime.Name = "udLoopTime";
            this.udLoopTime.Size = new System.Drawing.Size(80, 22);
            this.udLoopTime.TabIndex = 68;
            this.udLoopTime.Value = new decimal(new int[] {
            4000,
            0,
            0,
            0});
            this.udLoopTime.ValueChanged += new System.EventHandler(this.udLoopTime_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(439, 9);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(15, 15);
            this.label1.TabIndex = 75;
            this.label1.Text = "/";
            // 
            // udStep
            // 
            this.udStep.Location = new System.Drawing.Point(310, 8);
            this.udStep.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.udStep.Name = "udStep";
            this.udStep.Size = new System.Drawing.Size(40, 22);
            this.udStep.TabIndex = 73;
            this.udStep.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(263, 10);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(46, 15);
            this.label10.TabIndex = 72;
            this.label10.Text = "Speed";
            // 
            // lbCurTime
            // 
            this.lbCurTime.Location = new System.Drawing.Point(122, 10);
            this.lbCurTime.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbCurTime.Name = "lbCurTime";
            this.lbCurTime.Size = new System.Drawing.Size(45, 16);
            this.lbCurTime.TabIndex = 71;
            this.lbCurTime.Text = "0";
            this.lbCurTime.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // ckRun
            // 
            this.ckRun.AutoSize = true;
            this.ckRun.Location = new System.Drawing.Point(72, 9);
            this.ckRun.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ckRun.Name = "ckRun";
            this.ckRun.Size = new System.Drawing.Size(139, 19);
            this.ckRun.TabIndex = 69;
            this.ckRun.Text = "R&un(            )   ";
            this.ckRun.UseVisualStyleBackColor = true;
            this.ckRun.CheckedChanged += new System.EventHandler(this.ckRun_CheckedChanged);
            // 
            // ckRunOnce
            // 
            this.ckRunOnce.AutoSize = true;
            this.ckRunOnce.Location = new System.Drawing.Point(7, 9);
            this.ckRunOnce.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ckRunOnce.Name = "ckRunOnce";
            this.ckRunOnce.Size = new System.Drawing.Size(64, 19);
            this.ckRunOnce.TabIndex = 74;
            this.ckRunOnce.Text = "Once";
            this.ckRunOnce.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 87);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(2);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tabControl);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabInfo);
            this.splitContainer1.Size = new System.Drawing.Size(797, 484);
            this.splitContainer1.SplitterDistance = 529;
            this.splitContainer1.SplitterWidth = 3;
            this.splitContainer1.TabIndex = 70;
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPoseEditor);
            this.tabControl.Controls.Add(this.tabMotors);
            this.tabControl.Controls.Add(this.tabTorque);
            this.tabControl.Controls.Add(this.tabPdParam);
            this.tabControl.Controls.Add(this.tabLForce);
            this.tabControl.Controls.Add(this.tabRForce);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(529, 484);
            this.tabControl.TabIndex = 6;
            // 
            // tabPoseEditor
            // 
            this.tabPoseEditor.AutoScroll = true;
            this.tabPoseEditor.Controls.Add(this.flPose);
            this.tabPoseEditor.Controls.Add(this.panel1);
            this.tabPoseEditor.Location = new System.Drawing.Point(4, 25);
            this.tabPoseEditor.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabPoseEditor.Name = "tabPoseEditor";
            this.tabPoseEditor.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabPoseEditor.Size = new System.Drawing.Size(521, 455);
            this.tabPoseEditor.TabIndex = 0;
            this.tabPoseEditor.Text = "Pose";
            this.tabPoseEditor.UseVisualStyleBackColor = true;
            // 
            // flPose
            // 
            this.flPose.AutoSize = true;
            this.flPose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flPose.Location = new System.Drawing.Point(4, 34);
            this.flPose.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.flPose.Name = "flPose";
            this.flPose.Size = new System.Drawing.Size(513, 418);
            this.flPose.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btCopy);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.btSave);
            this.panel1.Controls.Add(this.btLoad);
            this.panel1.Controls.Add(this.udkeyTime);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(4, 3);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(513, 31);
            this.panel1.TabIndex = 9;
            // 
            // btCopy
            // 
            this.btCopy.AutoSize = true;
            this.btCopy.Location = new System.Drawing.Point(318, 2);
            this.btCopy.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btCopy.Name = "btCopy";
            this.btCopy.Size = new System.Drawing.Size(71, 26);
            this.btCopy.TabIndex = 10;
            this.btCopy.Text = "&Copy";
            this.btCopy.UseVisualStyleBackColor = true;
            this.btCopy.Click += new System.EventHandler(this.btCopy_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 8);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 15);
            this.label2.TabIndex = 9;
            this.label2.Text = "Time";
            // 
            // btSave
            // 
            this.btSave.Location = new System.Drawing.Point(251, 2);
            this.btSave.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btSave.Name = "btSave";
            this.btSave.Size = new System.Drawing.Size(66, 26);
            this.btSave.TabIndex = 7;
            this.btSave.Text = "&Save";
            this.btSave.UseVisualStyleBackColor = true;
            this.btSave.Click += new System.EventHandler(this.btSave_Click);
            // 
            // btLoad
            // 
            this.btLoad.Location = new System.Drawing.Point(182, 2);
            this.btLoad.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btLoad.Name = "btLoad";
            this.btLoad.Size = new System.Drawing.Size(66, 26);
            this.btLoad.TabIndex = 6;
            this.btLoad.Text = "&Load";
            this.btLoad.UseVisualStyleBackColor = true;
            this.btLoad.Click += new System.EventHandler(this.btLoad_Click);
            // 
            // udkeyTime
            // 
            this.udkeyTime.Location = new System.Drawing.Point(45, 5);
            this.udkeyTime.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.udkeyTime.Name = "udkeyTime";
            this.udkeyTime.Size = new System.Drawing.Size(132, 22);
            this.udkeyTime.TabIndex = 8;
            // 
            // tabMotors
            // 
            this.tabMotors.Controls.Add(this.flLength);
            this.tabMotors.Location = new System.Drawing.Point(4, 25);
            this.tabMotors.Margin = new System.Windows.Forms.Padding(2);
            this.tabMotors.Name = "tabMotors";
            this.tabMotors.Size = new System.Drawing.Size(521, 455);
            this.tabMotors.TabIndex = 1;
            this.tabMotors.Text = "Length";
            this.tabMotors.UseVisualStyleBackColor = true;
            // 
            // flLength
            // 
            this.flLength.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flLength.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flLength.Location = new System.Drawing.Point(0, 0);
            this.flLength.Margin = new System.Windows.Forms.Padding(2);
            this.flLength.Name = "flLength";
            this.flLength.Size = new System.Drawing.Size(521, 455);
            this.flLength.TabIndex = 2;
            // 
            // tabTorque
            // 
            this.tabTorque.Controls.Add(this.flTorque);
            this.tabTorque.Controls.Add(this.panel4);
            this.tabTorque.Location = new System.Drawing.Point(4, 25);
            this.tabTorque.Margin = new System.Windows.Forms.Padding(2);
            this.tabTorque.Name = "tabTorque";
            this.tabTorque.Size = new System.Drawing.Size(521, 455);
            this.tabTorque.TabIndex = 2;
            this.tabTorque.Text = "Torque";
            this.tabTorque.UseVisualStyleBackColor = true;
            // 
            // flTorque
            // 
            this.flTorque.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flTorque.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flTorque.Location = new System.Drawing.Point(0, 30);
            this.flTorque.Margin = new System.Windows.Forms.Padding(2);
            this.flTorque.Name = "flTorque";
            this.flTorque.Size = new System.Drawing.Size(521, 425);
            this.flTorque.TabIndex = 1;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.btSetLimitMax);
            this.panel4.Controls.Add(this.btSendTorque);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel4.Location = new System.Drawing.Point(0, 0);
            this.panel4.Margin = new System.Windows.Forms.Padding(2);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(521, 30);
            this.panel4.TabIndex = 2;
            // 
            // btSetLimitMax
            // 
            this.btSetLimitMax.Location = new System.Drawing.Point(278, 3);
            this.btSetLimitMax.Margin = new System.Windows.Forms.Padding(2);
            this.btSetLimitMax.Name = "btSetLimitMax";
            this.btSetLimitMax.Size = new System.Drawing.Size(84, 25);
            this.btSetLimitMax.TabIndex = 1;
            this.btSetLimitMax.Text = "Set Max";
            this.btSetLimitMax.UseVisualStyleBackColor = true;
            this.btSetLimitMax.Click += new System.EventHandler(this.btSetLimitMax_Click);
            // 
            // btSendTorque
            // 
            this.btSendTorque.Location = new System.Drawing.Point(366, 2);
            this.btSendTorque.Margin = new System.Windows.Forms.Padding(2);
            this.btSendTorque.Name = "btSendTorque";
            this.btSendTorque.Size = new System.Drawing.Size(155, 25);
            this.btSendTorque.TabIndex = 0;
            this.btSendTorque.Text = "Send Torque Limit";
            this.btSendTorque.UseVisualStyleBackColor = true;
            this.btSendTorque.Click += new System.EventHandler(this.btSendTorque_Click);
            // 
            // tabPdParam
            // 
            this.tabPdParam.Controls.Add(this.flPd);
            this.tabPdParam.Controls.Add(this.panel3);
            this.tabPdParam.Location = new System.Drawing.Point(4, 25);
            this.tabPdParam.Margin = new System.Windows.Forms.Padding(2);
            this.tabPdParam.Name = "tabPdParam";
            this.tabPdParam.Padding = new System.Windows.Forms.Padding(2);
            this.tabPdParam.Size = new System.Drawing.Size(521, 455);
            this.tabPdParam.TabIndex = 3;
            this.tabPdParam.Text = "PD";
            this.tabPdParam.UseVisualStyleBackColor = true;
            // 
            // flPd
            // 
            this.flPd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flPd.Location = new System.Drawing.Point(2, 30);
            this.flPd.Margin = new System.Windows.Forms.Padding(2);
            this.flPd.Name = "flPd";
            this.flPd.Size = new System.Drawing.Size(517, 423);
            this.flPd.TabIndex = 1;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.btSendPD);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(2, 2);
            this.panel3.Margin = new System.Windows.Forms.Padding(2);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(517, 28);
            this.panel3.TabIndex = 0;
            // 
            // btSendPD
            // 
            this.btSendPD.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btSendPD.Location = new System.Drawing.Point(392, 2);
            this.btSendPD.Margin = new System.Windows.Forms.Padding(2);
            this.btSendPD.Name = "btSendPD";
            this.btSendPD.Size = new System.Drawing.Size(124, 23);
            this.btSendPD.TabIndex = 0;
            this.btSendPD.Text = "Send Param";
            this.btSendPD.UseVisualStyleBackColor = true;
            this.btSendPD.Click += new System.EventHandler(this.btSendPD_Click);
            // 
            // tabLForce
            // 
            this.tabLForce.Controls.Add(this.jeLeft);
            this.tabLForce.Location = new System.Drawing.Point(4, 25);
            this.tabLForce.Margin = new System.Windows.Forms.Padding(2);
            this.tabLForce.Name = "tabLForce";
            this.tabLForce.Size = new System.Drawing.Size(521, 455);
            this.tabLForce.TabIndex = 4;
            this.tabLForce.Text = "L Force";
            this.tabLForce.UseVisualStyleBackColor = true;
            // 
            // jeLeft
            // 
            this.jeLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jeLeft.FileName = "forcesL.txt";
            this.jeLeft.Location = new System.Drawing.Point(0, 0);
            this.jeLeft.Margin = new System.Windows.Forms.Padding(2);
            this.jeLeft.Name = "jeLeft";
            this.jeLeft.Size = new System.Drawing.Size(521, 455);
            this.jeLeft.TabIndex = 0;
            // 
            // tabRForce
            // 
            this.tabRForce.Controls.Add(this.jeRight);
            this.tabRForce.Location = new System.Drawing.Point(4, 25);
            this.tabRForce.Name = "tabRForce";
            this.tabRForce.Size = new System.Drawing.Size(521, 455);
            this.tabRForce.TabIndex = 5;
            this.tabRForce.Text = "R Force";
            this.tabRForce.UseVisualStyleBackColor = true;
            // 
            // jeRight
            // 
            this.jeRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jeRight.FileName = "forcesR.txt";
            this.jeRight.Location = new System.Drawing.Point(0, 0);
            this.jeRight.Margin = new System.Windows.Forms.Padding(2);
            this.jeRight.Name = "jeRight";
            this.jeRight.Size = new System.Drawing.Size(521, 455);
            this.jeRight.TabIndex = 0;
            // 
            // tabInfo
            // 
            this.tabInfo.Controls.Add(this.tpState);
            this.tabInfo.Controls.Add(this.tpMessage);
            this.tabInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabInfo.Location = new System.Drawing.Point(0, 0);
            this.tabInfo.Margin = new System.Windows.Forms.Padding(2);
            this.tabInfo.Name = "tabInfo";
            this.tabInfo.SelectedIndex = 0;
            this.tabInfo.Size = new System.Drawing.Size(265, 484);
            this.tabInfo.TabIndex = 69;
            // 
            // tpState
            // 
            this.tpState.Controls.Add(this.tbState);
            this.tpState.Location = new System.Drawing.Point(4, 25);
            this.tpState.Margin = new System.Windows.Forms.Padding(2);
            this.tpState.Name = "tpState";
            this.tpState.Padding = new System.Windows.Forms.Padding(2);
            this.tpState.Size = new System.Drawing.Size(257, 455);
            this.tpState.TabIndex = 0;
            this.tpState.Text = "State";
            this.tpState.UseVisualStyleBackColor = true;
            // 
            // tbState
            // 
            this.tbState.BackColor = System.Drawing.SystemColors.Window;
            this.tbState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbState.Font = new System.Drawing.Font("MS UI Gothic", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.tbState.Location = new System.Drawing.Point(2, 2);
            this.tbState.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tbState.Multiline = true;
            this.tbState.Name = "tbState";
            this.tbState.ReadOnly = true;
            this.tbState.Size = new System.Drawing.Size(253, 451);
            this.tbState.TabIndex = 1;
            this.tbState.TabStop = false;
            // 
            // tpMessage
            // 
            this.tpMessage.Controls.Add(this.tbMessage);
            this.tpMessage.Location = new System.Drawing.Point(4, 25);
            this.tpMessage.Margin = new System.Windows.Forms.Padding(2);
            this.tpMessage.Name = "tpMessage";
            this.tpMessage.Padding = new System.Windows.Forms.Padding(2);
            this.tpMessage.Size = new System.Drawing.Size(257, 455);
            this.tpMessage.TabIndex = 1;
            this.tpMessage.Text = "Message";
            this.tpMessage.UseVisualStyleBackColor = true;
            // 
            // tbMessage
            // 
            this.tbMessage.BackColor = System.Drawing.SystemColors.Window;
            this.tbMessage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbMessage.Location = new System.Drawing.Point(2, 2);
            this.tbMessage.Margin = new System.Windows.Forms.Padding(2);
            this.tbMessage.Multiline = true;
            this.tbMessage.Name = "tbMessage";
            this.tbMessage.ReadOnly = true;
            this.tbMessage.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbMessage.Size = new System.Drawing.Size(253, 451);
            this.tbMessage.TabIndex = 0;
            this.tbMessage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbMessage_KeyPress);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(935, 571);
            this.Controls.Add(this.laCurTime);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panelCom);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.track);
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MinimumSize = new System.Drawing.Size(484, 341);
            this.Name = "MainForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Robokey";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.track)).EndInit();
            this.panelCom.ResumeLayout(false);
            this.panelCom.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udTick)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udLoopTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udStep)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.tabPoseEditor.ResumeLayout(false);
            this.tabPoseEditor.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udkeyTime)).EndInit();
            this.tabMotors.ResumeLayout(false);
            this.tabTorque.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.tabPdParam.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.tabLForce.ResumeLayout(false);
            this.tabRForce.ResumeLayout(false);
            this.tabInfo.ResumeLayout(false);
            this.tpState.ResumeLayout(false);
            this.tpState.PerformLayout();
            this.tpMessage.ResumeLayout(false);
            this.tpMessage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TrackBar track;
        private System.Windows.Forms.OpenFileDialog openPose;
        private System.Windows.Forms.SaveFileDialog savePose;
        private System.Windows.Forms.Label laCurTime;
        private System.Windows.Forms.Panel panelCom;
        private System.Windows.Forms.CheckBox ckMotor;
        private System.Windows.Forms.Button btResetMotors;
        private System.Windows.Forms.Timer runTimer;
        private System.Windows.Forms.Button btFindRobot;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.CheckBox ckRunOnce;
        private System.Windows.Forms.NumericUpDown udStep;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label lbCurTime;
        private System.Windows.Forms.NumericUpDown udTime;
        private System.Windows.Forms.CheckBox ckRun;
        private System.Windows.Forms.NumericUpDown udLoopTime;
        private System.Windows.Forms.Button del;
        private System.Windows.Forms.Button add;
        private System.Windows.Forms.TextBox txErrorMsg;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPoseEditor;
        private System.Windows.Forms.Button btCopy;
        private System.Windows.Forms.Button btLoad;
        private System.Windows.Forms.Button btSave;
        private System.Windows.Forms.FlowLayoutPanel flPose;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown udkeyTime;
        private System.Windows.Forms.TabPage tabMotors;
        private System.Windows.Forms.TabPage tabTorque;
        private System.Windows.Forms.FlowLayoutPanel flTorque;
        private System.Windows.Forms.TabControl tabInfo;
        private System.Windows.Forms.TabPage tpState;
        private System.Windows.Forms.TextBox tbState;
        private System.Windows.Forms.TabPage tpMessage;
        private System.Windows.Forms.TextBox tbMessage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.FlowLayoutPanel fpFoundRobot;
        private System.Windows.Forms.Label laPort;
        private System.Windows.Forms.NumericUpDown udTick;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox ckSense;
        private System.Windows.Forms.TabPage tabPdParam;
        private System.Windows.Forms.FlowLayoutPanel flPd;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button btSendPD;
        private System.Windows.Forms.FlowLayoutPanel flLength;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Button btSendTorque;
        private System.Windows.Forms.CheckBox ckForce;
        private System.Windows.Forms.TabPage tabLForce;
        private System.Windows.Forms.CheckBox ckLog;
        private System.Windows.Forms.TabPage tabRForce;
        private UCJacobianEditor jeLeft;
        private UCJacobianEditor jeRight;
        private System.Windows.Forms.Button btCalibForce;
        private System.Windows.Forms.Button btSetLimitMax;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lbSendCount;
        private System.Windows.Forms.Label lbRecvCount;
    }
}

