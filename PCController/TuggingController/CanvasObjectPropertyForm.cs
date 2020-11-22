using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TuggingController {
    public partial class CanvasObjectPropertyForm : Form {
        public Form Parent { get; set; }

        public CanvasObjectPropertyForm() {
            InitializeComponent();

            this.treeView1.NodeMouseDoubleClick += this.TreeView1_NodeMouseDoubleClick;
        }

        private void TreeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e) {
            var selectedTarget = e.Node.Tag as ICanvasObject;

            this.propertyGrid1.SelectedObject = selectedTarget;
        }

        protected override void OnActivated(EventArgs e) {
            var target = this.Parent as TuggingControlForm;

            this.treeView1.BeginUpdate();

            var root = target.configControl.ChartScene.Root;

            this.treeView1.Nodes.Clear();
            this.treeView1.Nodes.Add(new TreeNode(root.ToString(), root.GetChildrenTreeNodes()) { Tag = root });

            this.treeView1.EndUpdate();
            this.treeView1.ExpandAll();

            base.OnActivated(e);
        }
    }
}
