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
    public partial class TaskMakerForm : Form {
        public Services Services { get; set; } = new Services();

        private CanvasControl canvasControl1;
        private ToolTip tooltipBtnAddLayer = new ToolTip();
        private ToolTip tooltipBtnDeleteLayer = new ToolTip();
        private ToolTip tooltipBtnTargetSelection = new ToolTip();

        public delegate void InvalidateDelgate(bool invalidateChildren);


        public TaskMakerForm() {
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

            this.Services.Boards.Serial = this.serialPort1;
            this.Services.RootLayer = this.canvasControl1.RootLayer;
            this.Services.SelectedLayer = this.canvasControl1.SelectedLayer;
            this.Services.Timer = new Timer();
            this.Services.Timer.Interval = 100;
            this.Services.Timer.Tick += this.Timer_Tick;

            this.groupBox2.Text = $"Canvas - [{this.canvasControl1.SelectedLayer.Text}]";

            tooltipBtnAddLayer.SetToolTip(this.button5, "Ctrl+A");
            tooltipBtnDeleteLayer.SetToolTip(this.button6, "Ctrl+D");
            tooltipBtnTargetSelection.SetToolTip(this.button9, "Ctrl+T");

            this.canvasControl1.ContextMenuStrip = this.contextMenuStrip1;

            this.treeView1.AllowDrop = true;
            this.treeView1.ItemDrag += this.TreeView1_ItemDrag;
            this.treeView1.DragOver += this.TreeView1_DragOver;
            this.treeView1.DragDrop += this.TreeView1_DragDrop;
        }

        private void TreeView1_DragDrop(object sender, DragEventArgs e) {
            //ドロップされたデータがTreeNodeか調べる
            if (e.Data.GetDataPresent(typeof(Layer))) {
                TreeView tv = (TreeView)sender;
                //ドロップされたデータ(TreeNode)を取得
                Layer source =
                    (Layer)e.Data.GetData(typeof(Layer));
                //ドロップ先のTreeNodeを取得する
                Layer target =
                    tv.GetNodeAt(tv.PointToClient(new Point(e.X, e.Y))) as Layer;
                //マウス下のNodeがドロップ先として適切か調べる
                if (target != null && target != source &&
                    !IsChildNode(source, target)) {
                    //ドロップされたNodeのコピーを作成
                    var cln = (Layer)source.Clone();
                    //Nodeを追加
                    target.Nodes.Add(cln);
                    //ドロップ先のNodeを展開
                    target.Expand();
                    //追加されたNodeを選択
                    tv.SelectedNode = cln;

                }
                else
                    e.Effect = DragDropEffects.None;
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private void TreeView1_DragOver(object sender, DragEventArgs e) {
            //ドラッグされているデータがTreeNodeか調べる
            if (e.Data.GetDataPresent(typeof(Layer))) {
                if ((e.KeyState & 8) == 8 &&
                    (e.AllowedEffect & DragDropEffects.Copy) ==
                    DragDropEffects.Copy)
                    //Ctrlキーが押されていればCopy
                    //"8"はCtrlキーを表す
                    e.Effect = DragDropEffects.Copy;
                else if ((e.AllowedEffect & DragDropEffects.Move) ==
                    DragDropEffects.Move)
                    //何も押されていなければMove
                    e.Effect = DragDropEffects.Move;
                else
                    e.Effect = DragDropEffects.None;
            }
            else
                //TreeNodeでなければ受け入れない
                e.Effect = DragDropEffects.None;

            //マウス下のNodeを選択する
            if (e.Effect != DragDropEffects.None) {
                TreeView tv = (TreeView)sender;
                //マウスのあるNodeを取得する
                TreeNode target =
                    tv.GetNodeAt(tv.PointToClient(new Point(e.X, e.Y)));
                //ドラッグされているNodeを取得する
                TreeNode source =
                    (TreeNode)e.Data.GetData(typeof(Layer));
                //マウス下のNodeがドロップ先として適切か調べる
                if (target != null && target != source &&
                        !IsChildNode(source, target)) {
                    //Nodeを選択する
                    if (target.IsSelected == false)
                        tv.SelectedNode = target;
                }
                else
                    e.Effect = DragDropEffects.None;
            }
        }

        private void TreeView1_ItemDrag(object sender, ItemDragEventArgs e) {
            var tv = sender as TreeView;

            tv.SelectedNode = e.Item as TreeNode;
            tv.Focus();

            DragDropEffects dde = tv.DoDragDrop(e.Item, DragDropEffects.All);

            if((dde & DragDropEffects.Move) == DragDropEffects.Move) {
                tv.Nodes.Remove(e.Item as TreeNode);
            }
        }

        /// <summary>
        /// あるTreeNodeが別のTreeNodeの子ノードか調べる
        /// </summary>
        /// <param name="parentNode">親ノードか調べるTreeNode</param>
        /// <param name="childNode">子ノードか調べるTreeNode</param>
        /// <returns>子ノードの時はTrue</returns>
        private static bool IsChildNode(TreeNode parentNode, TreeNode childNode) {
            if (childNode.Parent == parentNode)
                return true;
            else if (childNode.Parent != null)
                return IsChildNode(parentNode, childNode.Parent);
            else
                return false;
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
            short[] targets = new short[this.Services.Boards.NMotor];

            for (int i = 0; i < this.Services.Motors.Count; ++i) {
                targets[i] = (short)this.Services.Motors[i].position.Value;
            }

            this.Services.Boards.SendPosDirect(targets);
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
                case Keys.Escape:
                    this.canvasControl1.BeginNoneMode();
                    e.Handled = true;
                    break;
                case Keys.P:
                    this.canvasControl1.Pair();
                    e.Handled = true;
                    break;
            }
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

        private void treeView1_MouseDoubleClick(object sender, MouseEventArgs e) {
            this.canvasControl1.ChooseLayer(this.treeView1.SelectedNode as Layer);
            this.Services.SelectedLayer = (Layer)this.treeView1.SelectedNode;

            this.canvasControl1.Invalidate(true);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) {
            var form = new SettingsForm(this.Services);

            form.ShowDialog();
            form.Dispose();
        }

        private void button10_Click(object sender, EventArgs e) {
            this.canvasControl1.Triangulate();
        }

        private void button9_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedLayer.ShowTargetSelectionForm(this.Services);
            this.canvasControl1.Reset();
        }

        private void button12_Click(object sender, EventArgs e) {
            this.canvasControl1.Pair();
        }

        private void layerToolStripMenuItem_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedLayer.ShowTargetSelectionForm(this.Services);
            this.canvasControl1.Reset();
        }

        private void button11_Click(object sender, EventArgs e) {
            this.canvasControl1.SelectedLayer.ShowTargetControlForm();
        }

        private void button13_Click(object sender, EventArgs e) {
            this.canvasControl1.Unpair();
        }
    }

    public class Services {
        public Boards Boards { get; set; } = new Boards();
        public Motors Motors { get; set; } = new Motors();
        public MotorOffsets MotorOffsets { get; set; } = new MotorOffsets();
        public Layer RootLayer { get; set; }
        public Layer SelectedLayer { get; set; }
        public Timer Timer { get; set; }


        public void ResetMotor() {
            this.Motors.Clear();

            for (int i = 0; i < this.Boards.NMotor; ++i) {
                Motor m = new Motor();

                this.Motors.Add(m);
            }

            short[] k = new short[this.Boards.NMotor];
            short[] b = new short[this.Boards.NMotor];
            short[] a = new short[this.Boards.NMotor];
            short[] limit = new short[this.Boards.NMotor];
            short[] release = new short[this.Boards.NMotor];
            short[] torqueMin = new short[this.Boards.NMotor];
            short[] torqueMax = new short[this.Boards.NMotor];

            this.Boards.RecvParamPd(ref k, ref b);
            this.Boards.RecvParamCurrent(ref a);
            this.Boards.RecvParamTorque(ref torqueMin, ref torqueMax);
            this.Boards.RecvParamHeat(ref limit, ref release);

            for (int i = 0; i < this.Boards.NMotor; ++i) {
                this.Motors[i].pd.K = k[i];
                this.Motors[i].pd.B = b[i];
                this.Motors[i].pd.A = a[i];
                if (limit[i] > 32000) limit[i] = 32000;
                if (limit[i] < 0) limit[i] = 0;
                this.Motors[i].heat.HeatLimit = limit[i] * release[i];
                this.Motors[i].heat.HeatRelease = release[i];
                this.Motors[i].torque.Minimum = torqueMin[i];
                this.Motors[i].torque.Maximum = torqueMax[i];
            }
        }
    }

    public class MotorOffsets: List<int> { }
}
