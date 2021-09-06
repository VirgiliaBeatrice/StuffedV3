using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization;
using PCController;
using System.IO.Ports;
using MathNet.Numerics.LinearAlgebra;
using MathNetExtension;

namespace TaskMaker {
    public partial class TargetSelection : UserControl {
        public ProgramInfo ProgramInfo { get; set; }

        public TargetSelection(ProgramInfo info) {
            this.ProgramInfo = info;

            InitializeComponent();
            InitializeSerialPort();
            InitializeLayer();
        }

        private void InitializeSerialPort() {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            this.comboBox1.Items.AddRange(ports);

            if (this.comboBox1.Items.Count > 0) {
                this.comboBox1.Text = this.comboBox1.Items[0].ToString();
            }
        }

        private void InitializeLayer() {
            var selectableRoot = SelectableLayer.CreateSelectableLayer(this.ProgramInfo.RootLayer);

            this.treeView2.Nodes.Add(selectableRoot);
        }

        private void ResetMotor() {
            this.ProgramInfo.Motors.Clear();
   
            for (int i = 0; i < this.ProgramInfo.Boards.NMotor; ++i) {
                Motor m = new Motor();

                this.ProgramInfo.Motors.Add(m);
            }

            short[] k = new short[this.ProgramInfo.Boards.NMotor];
            short[] b = new short[this.ProgramInfo.Boards.NMotor];
            short[] a = new short[this.ProgramInfo.Boards.NMotor];
            short[] limit = new short[this.ProgramInfo.Boards.NMotor];
            short[] release = new short[this.ProgramInfo.Boards.NMotor];
            short[] torqueMin = new short[this.ProgramInfo.Boards.NMotor];
            short[] torqueMax = new short[this.ProgramInfo.Boards.NMotor];

            this.ProgramInfo.Boards.RecvParamPd(ref k, ref b);
            this.ProgramInfo.Boards.RecvParamCurrent(ref a);
            this.ProgramInfo.Boards.RecvParamTorque(ref torqueMin, ref torqueMax);
            this.ProgramInfo.Boards.RecvParamHeat(ref limit, ref release);

            for (int i = 0; i < this.ProgramInfo.Boards.NMotor; ++i) {
                this.ProgramInfo.Motors[i].pd.K = k[i];
                this.ProgramInfo.Motors[i].pd.B = b[i];
                this.ProgramInfo.Motors[i].pd.A = a[i];
                if (limit[i] > 32000) limit[i] = 32000;
                if (limit[i] < 0) limit[i] = 0;
                this.ProgramInfo.Motors[i].heat.HeatLimit = limit[i] * release[i];
                this.ProgramInfo.Motors[i].heat.HeatRelease = release[i];
                this.ProgramInfo.Motors[i].torque.Minimum = torqueMin[i];
                this.ProgramInfo.Motors[i].torque.Maximum = torqueMax[i];
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) {
            if (this.radioButton1.Checked) {
                this.radioButton2.Checked = false;
                this.treeView2.Enabled = false;

                this.treeView1.Enabled = true;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e) {
            if (this.radioButton2.Checked) {
                this.radioButton1.Checked = false;
                this.treeView1.Enabled = false;

                this.treeView2.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e) {
            var serialPort = this.ProgramInfo.Boards.Serial;
            if (serialPort.IsOpen)
                serialPort.Close();

            if (this.comboBox1.Text.Length == 0) return;

            serialPort.PortName = this.comboBox1.Text;
            serialPort.BaudRate = 2000000;

            try {
                serialPort.Open();
            } catch {
                return;
            }

            if (serialPort.IsOpen) {
                this.treeView1.Nodes.Clear();
                this.ProgramInfo.Boards.Clear();
                this.ProgramInfo.Boards.EnumerateBoard();

                this.ResetMotor();

                for (int i = 0; i < this.ProgramInfo.Boards.NMotor; ++i) {
                    this.treeView1.Nodes.Add(new SelectableMotor($"Motor{i}") { Target = this.ProgramInfo.Motors[i] });
                }
            }
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e) {

            
        }

        private void button1_Click(object sender, EventArgs e) {
            if (this.radioButton1.Checked) {
                this.ProgramInfo.SelectedLayer.InitializeMotorConfigs();

                foreach (SelectableMotor m in this.treeView1.Nodes) {
                    if (m.Checked) {
                        this.ProgramInfo.SelectedLayer.MotorConfigs.Add(m.Target);
                    }
                }

                this.ProgramInfo.Timer.Enabled = true;
                this.ProgramInfo.Timer.Start();
            }

            if (this.radioButton2.Checked) {
                this.ProgramInfo.SelectedLayer.InitializeLayerConfigs();

                foreach(SelectableLayer l in this.treeView2.Nodes) {
                    if (l.Checked)
                        this.ProgramInfo.SelectedLayer.LayerConfigs.Add(l.Target);
                }
            }

            MessageBox.Show("New configs are set.");
        }

        private void treeView2_AfterCheck(object sender, TreeViewEventArgs e) {
            //Vector<float> result = Vector<float>.Build.Dense(0);
            
            //foreach(SelectableLayer l in this.treeView2.Nodes) {
            //    if (l.Checked)
            //        result = result.Concatenate(l.ToVector());
            //}
        }
    }

    public class SelectableObject<T> : TreeNode {
        public T Target { get; set; }
        public SelectableObject(string name) {
            this.Text = name;
        }
    }

    public class SelectableMotor : SelectableObject<Motor> {
        public SelectableMotor(string name) : base(name) { }

        public Vector<float> ToVector() {
            return Vector<float>.Build.Dense(1, this.Target.position.Value);
        }
    }

    public class SelectableLayer : SelectableObject<Layer> {
        public SelectableLayer(string name) : base(name) { }

        public Vector<float> ToVector() {
            return Vector<float>.Build.Dense(new float[] {
                this.Target.Pointer.X, this.Target.Pointer.Y
            });
        }

        public static SelectableLayer CreateSelectableLayer(Layer layer) {
            var selectableLayer = new SelectableLayer(layer.Text) {
                Target = layer
            };
            
            foreach(Layer node in layer.Nodes) {
                var child = CreateSelectableLayer(node);

                selectableLayer.Nodes.Add(child);
            }

            return selectableLayer;
        }
    }

    //public interface IConfigs<T> {
    //    public Action<T> ToVector;
    //    Action<T, Vector<float>> FromVector;
    //}

    public class Configs<T> : List<T> {
        public Func<Configs<T>, Vector<float>> ToVector;
        public Action<Configs<T>, Vector<float>> FromVector;

        public Configs(Func<Configs<T>, Vector<float>> funcTo, Action<Configs<T>, Vector<float>> funcFrom) {
            this.ToVector += funcTo;
            this.FromVector += funcFrom;
        }
    }

    //public class MotorConfigs : Configs<Motor> {
    //    public MotorConfigs(Action<MotorConfigs> funcTo, Action<MotorConfigs, Vector<float>> funcFrom) : base(funcTo, funcFrom) {
    //    }
    //}

    //public class LayerConfigs : Configs<Layer> {
    //    public LayerConfigs(Action<LayerConfigs> funcTo, Action<LayerConfigs, Vector<float>> funcFrom) : base(funcTo, funcFrom) {
    //    }
    //}

}
