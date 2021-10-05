using MathNet.Numerics.LinearAlgebra;
using PCController;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;


namespace TaskMaker {
    public partial class TargetSelection : UserControl {

        public TargetSelection() {
            InitializeComponent();
            //InitializeSerialPort();
            InitializeLayer();
            UpdateMotors();
        }

        private void InitializeSerialPort() {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            comboBox1.Items.AddRange(ports);

            if (comboBox1.Items.Count > 0) {
                comboBox1.Text = comboBox1.Items[0].ToString();
            }
        }

        private void InitializeLayer() {
            var selectableRoot = SelectableLayer.CreateSelectableLayer(Services.LayerTree);

            treeView1.BeginUpdate();
            treeView2.Nodes.AddRange(selectableRoot.Nodes.OfType<SelectableLayer>().Where(l => l.Target != Services.Canvas.SelectedLayer).ToArray());

            treeView1.EndUpdate();
            treeView1.ExpandAll();
        }

        private void UpdateMotors() {
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();

            for (int i = 0; i < Services.Boards.NMotor; ++i) {
                treeView1.Nodes.Add(new SelectableMotor($"Motor{i}") { Target = Services.Motors[i] });
            }

            treeView1.EndUpdate();
            treeView1.ExpandAll();
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

        private void radioButton1_CheckedChanged(object sender, EventArgs e) {
            if (radioButton1.Checked) {
                radioButton2.Checked = false;
                treeView2.Enabled = false;

                treeView1.Enabled = true;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e) {
            if (radioButton2.Checked) {
                radioButton1.Checked = false;
                treeView1.Enabled = false;

                treeView2.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e) {
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
                treeView1.Nodes.Clear();
                Services.Boards.Clear();
                Services.Boards.EnumerateBoard();

                ResetMotor();

                for (int i = 0; i < Services.Boards.NMotor; ++i) {
                    treeView1.Nodes.Add(new SelectableMotor($"Motor{i}") { Target = Services.Motors[i] });
                }
            }
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e) {


        }

        private void button1_Click(object sender, EventArgs e) {
            if (radioButton1.Checked) {
                //Services.SelectedLayer.InitializeMotorConfigs();
                var target = new MotorTarget();
                Services.Canvas.SelectedLayer.BindedTarget = target;

                foreach (SelectableMotor m in treeView1.Nodes) {
                    if (m.Checked) {
                        //Services.SelectedLayer.MotorConfigs.Add(m.Target);
                        target.Motors.Add(m.Target);
                    }
                }

                Services.MotorTimer.Enabled = true;
                Services.MotorTimer.Start();
            }

            if (radioButton2.Checked) {
                //Services.SelectedLayer.InitializeLayerConfigs();
                var target = new LayerTarget();
                Services.Canvas.SelectedLayer.BindedTarget = target;

                var topLayers = treeView2.Nodes.OfType<SelectableLayer>().ToList();
                var checkedLayers = new List<SelectableLayer>();
                topLayers.ForEach(l => checkedLayers.AddRange(GetAllLayers(l)));

                foreach (SelectableLayer l in checkedLayers) {
                    target.Layers.Add(l.Target);
                }
            }

            MessageBox.Show("New configs are set.");
            ParentForm.Close();
        }

        static public SelectableLayer[] GetAllLayers(SelectableLayer layer) {
            var results = new List<SelectableLayer>();

            if (layer.Checked)
                results.Add(layer);

            foreach (SelectableLayer child in layer.Nodes) {
                results.AddRange(GetAllLayers(child));
            }

            return results.Where(r => r.Checked).ToArray();
        }
    }

    public class SelectableObject<T> : TreeNode {
        public T Target { get; set; }
        public SelectableObject(string name) {
            Text = name;
        }
    }

    public class SelectableMotor : SelectableObject<Motor> {
        public SelectableMotor(string name) : base(name) { }

        public Vector<float> ToVector() {
            return Vector<float>.Build.Dense(1, Target.position.Value);
        }
    }

    public class SelectableLayer : SelectableObject<Layer> {
        public SelectableLayer(string name) : base(name) { }

        public Vector<float> ToVector() {
            return Vector<float>.Build.Dense(new float[] {
                Target.Pointer.Location.X, Target.Pointer.Location.Y
            }); ;
        }

        public static SelectableLayer CreateSelectableLayer(TreeNode node) {
            SelectableLayer selectableLayer;
            if (node.Parent == null)
                selectableLayer = new SelectableLayer("Root");
            else {
                var layer = node.Tag as Layer;
                selectableLayer = new SelectableLayer(layer.Name) {
                    Target = layer
                };
            }


            foreach (TreeNode childNode in node.Nodes) {
                var children = CreateSelectableLayer(childNode);

                selectableLayer.Nodes.Add(children);
            }

            return selectableLayer;
        }
    }
}
