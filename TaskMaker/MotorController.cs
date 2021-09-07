using PCController;
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
        public decimal Max {
            get => this.numMax.Value;
            set {
                this.numMax.Value = value;
            }
        }
        public decimal Min {
            get => this.numMin.Value;
            set {
                this.numMin.Value = value;
            }
        }
        public decimal MotorValue {
            get => this.numMotor.Value;
            set {
                this.numMotor.Value = value;
            }
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

            this.MotorValue = m.position.Value;
            this.Min = m.position.Minimum;
            this.Max = m.position.Maximum;
            this.Offset = m.NewOffset;

            this.numMax.Maximum = this.Max;
            this.numMax.Minimum = this.Min;
            this.numMin.Maximum = this.Max;
            this.numMin.Minimum = this.Max;
            this.numOffset.Maximum = this.Max;
            this.numOffset.Minimum = this.Min;

            this.numMax.Increment = (this.Max - this.Min) / 20;
            this.numMin.Increment = (this.Max - this.Min) / 20;
            this.numOffset.Increment = (this.Max - this.Min) / 20;

            this.trackBar1.TickFrequency = 20;
            this.trackBar1.SmallChange = (int)(this.Max - this.Min) / 50;
            this.trackBar1.LargeChange = (int)(this.Max - this.Min) / 5;

        }

        private void numMotor_ValueChanged(object sender, EventArgs e) {
            motor.position.Value = (int)this.MotorValue;
        }

        private void trackBar1_Scroll(object sender, EventArgs e) {
            motor.position.Value = (int)this.MotorValue;
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
        }
    }
}
