using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TuggingController
{
    public partial class TaskMaker : Form
    {
        public Layer RootLayer { get; set; } = new Layer("RootLayer");
        public Layer SelectedLayer { get; set; }

        public TaskMaker()
        {
            InitializeComponent();

            this.SelectedLayer = this.RootLayer;
            this.treeView1.Nodes.Add(this.RootLayer);
        }

        //Add node
        private void button1_Click(object sender, EventArgs e)
        {

        }

        // Edit node
        private void button2_Click(object sender, EventArgs e)
        {

        }

        // Manipulate
        private void button3_Click(object sender, EventArgs e)
        {

        }

        // Delete node
        private void button4_Click(object sender, EventArgs e)
        {

        }

        // Add layer
        private void button5_Click(object sender, EventArgs e)
        {
            this.SelectedLayer.Nodes.Add(new Layer());
        }

        /// <summary>
        /// Add layer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            this.SelectedLayer.Remove();
        }

        /// <summary>
        /// Set selected property after select.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            this.SelectedLayer = (Layer)e.Node;
        }
    }

    public class Layer : TreeNode
    {
        public Layer NextLayer => (Layer)this.NextNode;
        
        public Layer()
        {
            this.Text = "NewLayer";
        }

        public Layer(string name)
        {
            this.Text = name;
        }
    }
}
