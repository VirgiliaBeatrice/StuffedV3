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

namespace TuggingController {
    public partial class TargetSelection : UserControl {
        public Boards Boards { get; set; } = new Boards();
        public Motors Motors { get; set; } = new Motors();

        public TargetSelection() {
            InitializeComponent();
            this.AddTestData();

            InitializeMotors();

            this.treeView1.AfterCheck += this.TreeView1_AfterCheck;
        }

        private void TreeView1_AfterCheck(object sender, TreeViewEventArgs e) {
            if (e.Action != TreeViewAction.Unknown) {
                foreach(var n in e.Node.Nodes) {
                    (n as TreeNode).Checked = e.Node.Checked;
                }
            }
        }

        private void InitializeMotors() {
            this.Boards.Serial = this.serialPort1;
            
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            this.comboBox1.Items.AddRange(ports);

            if (this.comboBox1.Items.Count > 0) {
                this.comboBox1.Text = this.comboBox1.Items[0].ToString();
            }

        }

        private void AddTestData() {
            var root = new TreeNode("Root");
            var n1 = new TreeNode("n1");
            var n2 = new TreeNode("n2");
            var n3 = new TreeNode("n3");
            var n4 = new TreeNode("n4");

            root.Nodes.AddRange(new TreeNode[] {
                n1, n2, n3, n4
            });

            this.treeView1.Nodes.Add(root);
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
            if (this.serialPort1.IsOpen)
                this.serialPort1.Close();

            if (this.comboBox1.Text.Length == 0) return;

            this.serialPort1.PortName = this.comboBox1.Text;
            this.serialPort1.BaudRate = 2000000;

            try {
                this.serialPort1.Open();
            } catch {
                return;
            }

            if (this.serialPort1.IsOpen) {
                this.treeView1.Nodes.Clear();
                this.Boards.Clear();
                this.Boards.EnumerateBoard();

                foreach (var b in this.Boards) {
                    TreeNode nb = this.treeView1.Nodes.Add("#" + b.boardId
                        + "M" + b.nMotor + "C" + b.nCurrent + "F" + b.nForce
                        );
                    nb.Nodes.Add("ID " + b.boardId);
                    nb.Nodes.Add("model " + b.modelNumber);
                    nb.Nodes.Add("nTarget " + b.nTarget);
                    nb.Nodes.Add("nMotor " + b.nMotor);
                    nb.Nodes.Add("nCurrent " + b.nCurrent);
                    nb.Nodes.Add("nForce " + b.nForce);
                }

                //times = Enumerable.Repeat(0, boards.NMotor).ToArray();
                //ResetPanels();
            }
        }
    }
}
