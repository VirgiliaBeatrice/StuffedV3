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
    public partial class TaskMaker : Form {
        private CanvasControl canvasControl1;

        public TaskMaker() {
            InitializeComponent();
            InitializeSkControl();

            if (Environment.Is64BitProcess)
                Console.WriteLine("64-bit process");
            else
                Console.WriteLine("32-bit process");

            this.KeyDown += TaskMaker_KeyDown;
            this.canvasControl1.LayerUpdated += this.CanvasControl1_LayerUpdated;
            this.canvasControl1.ModeChanged += this.CanvasControl1_ModeChanged;
            this.canvasControl1.Interpolated += this.CanvasControl1_Interpolated;

            this.UpdateTreeview();
        }

        private void CanvasControl1_Interpolated(object sender, InterpolatingEventArgs e) {
            this.toolStripStatusLabel3.Text = String.Join(",", e.Values.ToArray());
        }

        private void CanvasControl1_ModeChanged(object sender, EventArgs e) {
            this.toolStripStatusLabel2.Text = this.canvasControl1.SelectedMode.ToString();
        }

        private void CanvasControl1_LayerUpdated(object sender, EventArgs e) {
            this.UpdateTreeview();
        }

        private void InitializeSkControl() {
            // Bug: UserControl is not working on design-time.
            this.canvasControl1 = new CanvasControl();
            this.canvasControl1.Dock = DockStyle.Fill;
            this.canvasControl1.Location = new Point(4, 17);
            this.canvasControl1.Margin = new Padding(4);
            this.canvasControl1.Name = "canvasControl1";
            this.canvasControl1.Padding = new Padding(4);
            this.canvasControl1.Size = new Size(892, 609);
            this.canvasControl1.TabIndex = 0;

            this.groupBox2.Controls.Add(this.canvasControl1);
        }

        private void UpdateTreeview() {
            this.treeView1.BeginUpdate();
            this.treeView1.Nodes.Clear();

            var root = this.canvasControl1.GetRootLayer();
            this.treeView1.Nodes.Add(root);
            this.treeView1.SelectedNode = this.canvasControl1.GetCurrentSelectedLayer();

            this.treeView1.EndUpdate();
        }

        private void TaskMaker_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.A) {
                this.canvasControl1.SelectedMode = Modes.AddNode;
                e.Handled = true;
            }
        }

        //Add node
        private void button1_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedMode = Modes.AddNode;
        }

        // Edit node
        private void button2_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedMode = Modes.EditNode;
        }

        // Manipulate
        private void button3_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedMode = Modes.Manipulate;
        }

        /// <summary>
        /// Delete node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedMode = Modes.DeleteNode;
        }

        /// <summary>
        /// Add layer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e) {
            this.canvasControl1.AddLayer();
        }

        /// <summary>
        /// Delete layer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e) {
            this.canvasControl1.RemoveLayer();
        }

        /// <summary>
        /// Set selected property after select.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e) {
            this.canvasControl1.ChangeLayer(e.Node);
            this.canvasControl1.Invalidate(true);
        }

        /// <summary>
        /// Selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedMode = Modes.Selection;
        }
    }
}
