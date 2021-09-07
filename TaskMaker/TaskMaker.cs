using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MathNet.Numerics.LinearAlgebra;
using PCController;

namespace TaskMaker {
    public partial class TaskMaker : Form {
        public Services ProgramInfo { get; set; } = new Services();

        private CanvasControl canvasControl1;
        private ToolTip tooltipBtnAddLayer = new ToolTip();
        private ToolTip tooltipBtnDeleteLayer = new ToolTip();
        private ToolTip tooltipBtnTargetSelection = new ToolTip();
        public TaskMaker() {
            InitializeComponent();
            InitializeSkControl();

            this.KeyPreview = true;

            if (Environment.Is64BitProcess)
                Console.WriteLine("64-bit process");
            else
                Console.WriteLine("32-bit process");

            this.KeyDown += TaskMaker_KeyDown;
            this.canvasControl1.LayerUpdated += this.CanvasControl1_LayerUpdated;
            this.canvasControl1.LayerFocused += this.CanvasControl1_LayerFocused;
            this.canvasControl1.ModeChanged += this.CanvasControl1_ModeChanged;
            this.canvasControl1.Interpolated += this.CanvasControl1_Interpolated;
            this.canvasControl1.LayerConfigured += this.CanvasControl1_LayerConfigured;

            this.UpdateTreeview();
            this.treeView1.ExpandAll();

            this.ProgramInfo.Boards.Serial = this.serialPort1;
            this.ProgramInfo.RootLayer = this.canvasControl1.RootLayer;
            this.ProgramInfo.SelectedLayer = this.canvasControl1.SelectedLayer;
            this.ProgramInfo.Timer = new Timer();
            this.ProgramInfo.Timer.Interval = 100;
            this.ProgramInfo.Timer.Tick += this.Timer_Tick;

            this.groupBox2.Text = $"Canvas - [{this.canvasControl1.SelectedLayer.Text}]";

            tooltipBtnAddLayer.SetToolTip(this.button5, "Ctrl+A");
            tooltipBtnDeleteLayer.SetToolTip(this.button6, "Ctrl+D");
            tooltipBtnTargetSelection.SetToolTip(this.button9, "Ctrl+T");
        }

        private void CanvasControl1_LayerConfigured(object sender, EventArgs e) {
            this.toolStripStatusLabel4.Text = $"{this.canvasControl1.SelectedLayer.Text} - {this.canvasControl1.SelectedLayer.LayerStatus}";
        }

        private void CanvasControl1_LayerFocused(object sender, LayerFocusedEventArgs e) {
            this.groupBox2.Text = $"Canvas - [{this.canvasControl1.SelectedLayer.Text}]";
            this.toolStripStatusLabel4.Text = $"{this.canvasControl1.SelectedLayer.Text} - {this.canvasControl1.SelectedLayer.LayerStatus}";
        }

        private void Timer_Tick(object sender, EventArgs e) {
            this.UpdateMotor();
        }

        private void UpdateMotor() {
            short[] targets = new short[this.ProgramInfo.Boards.NMotor];

            for (int i = 0; i < this.ProgramInfo.Motors.Count; ++i) {
                targets[i] = (short)this.ProgramInfo.Motors[i].position.Value;
            }

            this.ProgramInfo.Boards.SendPosDirect(targets);
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

            var root = this.canvasControl1.RootLayer;
            this.treeView1.Nodes.Add(root);
            this.treeView1.SelectedNode = this.canvasControl1.SelectedLayer;

            this.treeView1.EndUpdate();
            this.treeView1.ExpandAll();
        }

        private void TaskMaker_KeyDown(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                //case Keys.A:
                //    //this.canvasControl1.BeginAddNodeMode();
                //    break;
                case Keys.Escape:
                    this.canvasControl1.BeginNoneMode();
                    break;
                //case Keys.T:
                //    if (!this.canvasControl1.Triangulate()) {
                //        MessageBox.Show("Amount of nodes is less than 3. Abort.");
                //    }
                //    break;
                //case Keys.S:
                //    this.canvasControl1.BeginSelectionMode();
                //    break;
                case Keys.P:
                    this.canvasControl1.SelectedLayer.ShowTargetSelectionForm(this.ProgramInfo);
                    this.canvasControl1.Reset();
                    break;
                case Keys.M:
                    this.canvasControl1.BeginManipulateMode();
                    break;
                case Keys.Q:
                    this.canvasControl1.SelectedLayer.ShowTargetControlForm();
                    break;
                case Keys.L:
                    this.canvasControl1.Pair();
                    break;
            }

            e.Handled = true;
            //this.Invalidate(true);
        }

        //Add node
        private void button1_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedMode = Modes.AddNode;
        }

        // Edit node
        private void button2_Click(object sender, EventArgs e) {
            this.canvasControl1.BeginEditMode();
        }

        // Manipulate
        private void button3_Click(object sender, EventArgs e) {
            this.canvasControl1.BeginManipulateMode();
        }

        /// <summary>
        /// Delete node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e) {
            this.canvasControl1.RemoveSelectedNodes();
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
            //this.canvasControl1.ChooseLayer(e.Node as Layer);
            //this.ProgramInfo.SelectedLayer = (Layer)e.Node;

            //this.canvasControl1.Invalidate(true);
        }

        /// <summary>
        /// Selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedMode = Modes.Selection;
        }

        private void toolbox_Enter(object sender, EventArgs e) {

        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e) {

        }

        private void treeView1_MouseDoubleClick(object sender, MouseEventArgs e) {
            this.canvasControl1.ChooseLayer(this.treeView1.SelectedNode as Layer);
            this.ProgramInfo.SelectedLayer = (Layer)this.treeView1.SelectedNode;

            this.canvasControl1.Invalidate(true);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) {
            var form = new SettingsForm(this.ProgramInfo);

            form.ShowDialog();
            form.Dispose();
        }

        private void button10_Click(object sender, EventArgs e) {
            this.canvasControl1.Triangulate();
        }

        private void button9_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedLayer.ShowTargetSelectionForm(this.ProgramInfo);
            this.canvasControl1.Reset();
        }

        private void button12_Click(object sender, EventArgs e) {
            this.canvasControl1.Pair();
        }
    }

    public class Services {
        public Boards Boards { get; set; } = new Boards();
        public Motors Motors { get; set; } = new Motors();
        public Layer RootLayer { get; set; }
        public Layer SelectedLayer { get; set; }
        public Timer Timer { get; set; }

    }
}
