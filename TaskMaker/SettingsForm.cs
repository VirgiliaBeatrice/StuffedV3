using PCController;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskMaker {
    public partial class SettingsForm : Form {
        private Services services;

        public SettingsForm(Services services) {
            this.services = services;

            InitializeComponent();
            this.InitializeSerialPort();
        }

        private void InitializeSerialPort() {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            this.comboBox1.Items.AddRange(ports);

            if (this.comboBox1.Items.Count > 0) {
                this.comboBox1.Text = this.comboBox1.Items[0].ToString();
            }
        }

        private void ConectToBoards() {
            var serialPort = this.services.Boards.Serial;
            if (serialPort.IsOpen)
                serialPort.Close();

            if (this.comboBox1.Text.Length == 0) return;

            serialPort.PortName = this.comboBox1.Text;
            serialPort.BaudRate = 2000000;

            try {
                serialPort.Open();
            }
            catch {
                return;
            }

            if (serialPort.IsOpen) {
                this.services.Boards.Clear();
                this.services.Boards.EnumerateBoard();

                this.ResetMotor();
                
                if (this.services.Boards.NMotor != 0) {
                    MessageBox.Show("Motor ready.");
                    this.Close();
                }
            }
        }

        private void ResetMotor() {
            this.services.Motors.Clear();

            for (int i = 0; i < this.services.Boards.NMotor; ++i) {
                Motor m = new Motor();

                this.services.Motors.Add(m);
            }

            short[] k = new short[this.services.Boards.NMotor];
            short[] b = new short[this.services.Boards.NMotor];
            short[] a = new short[this.services.Boards.NMotor];
            short[] limit = new short[this.services.Boards.NMotor];
            short[] release = new short[this.services.Boards.NMotor];
            short[] torqueMin = new short[this.services.Boards.NMotor];
            short[] torqueMax = new short[this.services.Boards.NMotor];

            this.services.Boards.RecvParamPd(ref k, ref b);
            this.services.Boards.RecvParamCurrent(ref a);
            this.services.Boards.RecvParamTorque(ref torqueMin, ref torqueMax);
            this.services.Boards.RecvParamHeat(ref limit, ref release);

            for (int i = 0; i < this.services.Boards.NMotor; ++i) {
                this.services.Motors[i].pd.K = k[i];
                this.services.Motors[i].pd.B = b[i];
                this.services.Motors[i].pd.A = a[i];
                if (limit[i] > 32000) limit[i] = 32000;
                if (limit[i] < 0) limit[i] = 0;
                this.services.Motors[i].heat.HeatLimit = limit[i] * release[i];
                this.services.Motors[i].heat.HeatRelease = release[i];
                this.services.Motors[i].torque.Minimum = torqueMin[i];
                this.services.Motors[i].torque.Maximum = torqueMax[i];
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            this.ConectToBoards();
        }
    }
}
