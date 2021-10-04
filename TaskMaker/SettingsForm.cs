using PCController;
using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace TaskMaker {
    public partial class SettingsForm : Form {
        public SettingsForm() {
            InitializeComponent();
            InitializeSerialPort();
        }

        private void InitializeSerialPort() {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            comboBox1.Items.AddRange(ports);

            if (comboBox1.Items.Count > 0) {
                comboBox1.Text = comboBox1.Items[0].ToString();
            }
        }

        private void ConectToBoards() {
            var serialPort = Services.Boards.Serial;
            if (serialPort.IsOpen)
                serialPort.Close();

            if (comboBox1.Text.Length == 0) return;

            serialPort.PortName = comboBox1.Text;
            serialPort.BaudRate = 2000000;

            try {
                serialPort.Open();
            }
            catch {
                return;
            }

            if (serialPort.IsOpen) {
                Services.Boards.Clear();
                Services.Boards.EnumerateBoard();

                ResetMotor();

                if (Services.Boards.NMotor != 0) {
                    MessageBox.Show("Motor ready.");
                    Close();
                }
            }
        }

        private void ResetMotor() {
            Services.Motors.Clear();

            for (int i = 0; i < Services.Boards.NMotor; ++i) {
                Motor m = new Motor();

                Services.Motors.Add(m);
            }

            short[] k = new short[Services.Boards.NMotor];
            short[] b = new short[Services.Boards.NMotor];
            short[] a = new short[Services.Boards.NMotor];
            short[] limit = new short[Services.Boards.NMotor];
            short[] release = new short[Services.Boards.NMotor];
            short[] torqueMin = new short[Services.Boards.NMotor];
            short[] torqueMax = new short[Services.Boards.NMotor];

            Services.Boards.RecvParamPd(ref k, ref b);
            Services.Boards.RecvParamCurrent(ref a);
            Services.Boards.RecvParamTorque(ref torqueMin, ref torqueMax);
            Services.Boards.RecvParamHeat(ref limit, ref release);

            for (int i = 0; i < Services.Boards.NMotor; ++i) {
                Services.Motors[i].pd.K = k[i];
                Services.Motors[i].pd.B = b[i];
                Services.Motors[i].pd.A = a[i];
                if (limit[i] > 32000) limit[i] = 32000;
                if (limit[i] < 0) limit[i] = 0;
                Services.Motors[i].heat.HeatLimit = limit[i] * release[i];
                Services.Motors[i].heat.HeatRelease = release[i];
                Services.Motors[i].torque.Minimum = torqueMin[i];
                Services.Motors[i].torque.Maximum = torqueMax[i];
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            ConectToBoards();
        }
    }
}
