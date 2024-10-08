﻿using PCController;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskMaker {
    public partial class MotorController : UserControl {
        public string MotorName {
            get => this.groupBox1.Text;
            set {
                this.groupBox1.Text = value;
            }
        }
        // Get real value, set presentational value(w/offset)
        public decimal Max {
            get => this.numMax.Value + this.Offset;
        }
        public decimal Min {
            get => this.numMin.Value + this.Offset;
        }

        // Real Value
        public decimal MotorValue {
            get => this.numMotor.Value + this.Offset;
        }
        public decimal Offset {
            get => this.numOffset.Value;
            set {
                this.numOffset.Value = value;
            }
        }

        private Motor motor;

        public MotorController(Motor motor) {
            this.motor = motor;

            InitializeComponent();
            InitializeMotorValues();
        }

        private void InitializeMotorValues() {
            var m = this.motor;
            var initMax = m.position.Maximum;
            var initMin = m.position.Minimum;
            var motorValue = m.position.Value;

            this.numMax.Maximum = initMax;
            this.numMax.Minimum = initMin;
            this.numMin.Maximum = initMax;
            this.numMin.Minimum = initMin;
            this.numOffset.Maximum = initMax;
            this.numOffset.Minimum = initMin;
            this.numMax.Value = initMax;
            this.numMin.Value = initMin;

            this.numMotor.Maximum = initMax;
            this.trackBar1.Maximum = initMax;
            this.numMotor.Minimum = initMin;
            this.trackBar1.Minimum = initMin;
            this.numMotor.Value = motorValue;
            this.trackBar1.Value = motorValue;

            this.numMax.Increment = (this.Max - this.Min) / 20;
            this.numMin.Increment = (this.Max - this.Min) / 20;
            this.numOffset.Increment = (this.Max - this.Min) / 20;

            this.numOffset.Value = m.NewOffset;

            this.trackBar1.TickFrequency = 40;
            this.trackBar1.SmallChange = (int)(this.Max - this.Min) / 100;
            this.trackBar1.LargeChange = (int)(this.Max - this.Min) / 50;

        }

        public void ReturnZero() {
            this.numMotor.Value = 0;
        }

        private void numMotor_ValueChanged(object sender, EventArgs e) {
            //Console.WriteLine($"Real: {this.MotorValue}");
            motor.position.Value = (int)this.MotorValue;
            this.trackBar1.Value = (int)this.numMotor.Value;
        }

        private void trackBar1_Scroll(object sender, EventArgs e) {
            Console.WriteLine($"Real: {this.MotorValue}");
            motor.position.Value = (int)this.MotorValue;
            this.numMotor.Value = this.trackBar1.Value;
        }

        private void numMin_ValueChanged(object sender, EventArgs e) {
            this.trackBar1.Minimum = (int)this.Min;
            this.numMotor.Minimum = (int)this.Min;
        }

        private void numMax_ValueChanged(object sender, EventArgs e) {
            this.trackBar1.Maximum = (int)this.Max;
            this.numMotor.Maximum = (int)this.Max;
        }

        private void numOffset_ValueChanged(object sender, EventArgs e) {
            this.motor.NewOffset = (int)this.Offset;
            this.Offset = this.numOffset.Value;
        }

        private void button1_Click(object sender, EventArgs e) {
            this.numMotor.Value = 0;
            this.trackBar1.Value = 0;
            motor.position.Value = (int)this.MotorValue;
        }

        private void button2_Click(object sender, EventArgs e) {
            this.Offset = motor.position.Value;
            this.trackBar1.Value = 0;
            this.numMotor.Value = 0;
        }
    }
}
