using System;
using System.Windows.Forms;

namespace TuggingController {
    public partial class TaskMaker : Form {
        //public Layer RootLayer { get; set; } = new Layer("RootLayer");
        //public Layer SelectedLayer { get; set; }
        //public Modes SelectedMode { get; set; } = Modes.None;

        public TaskMaker() {
            InitializeComponent();

            //this.SelectedLayer = this.RootLayer;
            //this.treeView1.Nodes.Add(this.RootLayer);
            if (Environment.Is64BitProcess)
                Console.WriteLine("64-bit process");
            else
                Console.WriteLine("32-bit process");

            this.KeyDown += TaskMaker_KeyDown;
        }

        private void TaskMaker_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.A) {
                this.canvasControl1.SelectedMode = Modes.AddNode;
                //this.SelectedMode = Modes.AddNode;
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
            this.canvasControl1.SelectedLayer.Nodes.Add(new Layer());
        }

        /// <summary>
        /// Delete layer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedLayer.Remove();
        }

        /// <summary>
        /// Set selected property after select.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e) {
            this.canvasControl1.SelectedLayer = (Layer)e.Node;
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
