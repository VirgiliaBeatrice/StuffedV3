
namespace TaskMaker {
    partial class MotorController {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.button2 = new System.Windows.Forms.Button();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.numMotor = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numMin = new System.Windows.Forms.NumericUpDown();
            this.numOffset = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.numMax = new System.Windows.Forms.NumericUpDown();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMotor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOffset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMax)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Controls.Add(this.button2, 3, 3);
            this.tableLayoutPanel1.Controls.Add(this.trackBar1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.numMotor, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.numMin, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.numOffset, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label3, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.numMax, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.button1, 2, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(314, 131);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // button2
            // 
            this.button2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button2.Location = new System.Drawing.Point(237, 106);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(74, 23);
            this.button2.TabIndex = 9;
            this.button2.Text = "Insert Offset";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // trackBar1
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.trackBar1, 4);
            this.trackBar1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trackBar1.Location = new System.Drawing.Point(3, 29);
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(308, 45);
            this.trackBar1.TabIndex = 0;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // numMotor
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.numMotor, 4);
            this.numMotor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numMotor.Location = new System.Drawing.Point(3, 3);
            this.numMotor.Name = "numMotor";
            this.numMotor.Size = new System.Drawing.Size(308, 20);
            this.numMotor.TabIndex = 1;
            this.numMotor.ValueChanged += new System.EventHandler(this.numMotor_ValueChanged);
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(25, 83);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(27, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Min.";
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(21, 111);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Offset";
            // 
            // numMin
            // 
            this.numMin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numMin.Location = new System.Drawing.Point(81, 80);
            this.numMin.Name = "numMin";
            this.numMin.Size = new System.Drawing.Size(72, 20);
            this.numMin.TabIndex = 5;
            this.numMin.ValueChanged += new System.EventHandler(this.numMin_ValueChanged);
            // 
            // numOffset
            // 
            this.numOffset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numOffset.Location = new System.Drawing.Point(81, 106);
            this.numOffset.Name = "numOffset";
            this.numOffset.Size = new System.Drawing.Size(72, 20);
            this.numOffset.TabIndex = 6;
            this.numOffset.ValueChanged += new System.EventHandler(this.numOffset_ValueChanged);
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label3.Location = new System.Drawing.Point(177, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Max.";
            // 
            // numMax
            // 
            this.numMax.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numMax.Location = new System.Drawing.Point(237, 80);
            this.numMax.Name = "numMax";
            this.numMax.Size = new System.Drawing.Size(74, 20);
            this.numMax.TabIndex = 7;
            this.numMax.ValueChanged += new System.EventHandler(this.numMax_ValueChanged);
            // 
            // button1
            // 
            this.button1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button1.Location = new System.Drawing.Point(159, 106);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(72, 23);
            this.button1.TabIndex = 8;
            this.button1.Text = "Zero";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(320, 150);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Motor";
            // 
            // MotorController
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "MotorController";
            this.Size = new System.Drawing.Size(320, 150);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMotor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOffset)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMax)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.NumericUpDown numMotor;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numMin;
        private System.Windows.Forms.NumericUpDown numOffset;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.NumericUpDown numMax;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}
