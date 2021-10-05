using PCController;
using System;
using System.Drawing;
using System.Windows.Forms;
using TaskMaker.MementoPattern;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaker {
    public partial class TaskMakerForm : Form {
        private CanvasControl canvasControl1;
        private ToolTip tooltipBtnAddLayer = new ToolTip();
        private ToolTip tooltipBtnDeleteLayer = new ToolTip();
        private ToolTip tooltipBtnTargetSelection = new ToolTip();
        private Caretaker _caretaker;
        private TreeNode _root;

        public delegate void InvalidateDelgate(bool invalidateChildren);


        public TaskMakerForm() {
            InitializeComponent();
            InitializeSkControl();

            KeyPreview = true;

            if (Environment.Is64BitProcess)
                Console.WriteLine("64-bit process");
            else
                Console.WriteLine("32-bit process");

            KeyDown += TaskMaker_KeyDown;

            Services.Boards.Serial = serialPort1;
            Services.MotorTimer.Tick += Timer_Tick;
            Services.Canvas = canvasControl1.Canvas;

            groupBox2.Text = $"Canvas - [{canvasControl1.SelectedLayer.Name}]";

            tooltipBtnAddLayer.SetToolTip(button5, "Ctrl+A");
            tooltipBtnDeleteLayer.SetToolTip(button6, "Ctrl+D");
            tooltipBtnTargetSelection.SetToolTip(button9, "Ctrl+T");

            canvasControl1.ContextMenuStrip = contextMenuStrip1;

            treeView1.AllowDrop = true;
            treeView1.ItemDrag += TreeView1_ItemDrag;
            treeView1.DragOver += TreeView1_DragOver;
            treeView1.DragDrop += TreeView1_DragDrop;

            _root = new TreeNode("Root");
            Services.LayerTree = _root;

            _root.Nodes.Clear();
            _root.Nodes.Add(new TreeNode() { Text = canvasControl1.Canvas.Layers[0].Name, Tag = canvasControl1.Canvas.Layers[0] });

            UpdateTreeview();
        }

        private void TreeView1_DragDrop(object sender, DragEventArgs e) {
            //ドロップされたデータがTreeNodeか調べる
            if (e.Data.GetDataPresent(typeof(TreeNode))) {
                TreeView tv = (TreeView)sender;
                //ドロップされたデータ(TreeNode)を取得
                var source =
                    (TreeNode)e.Data.GetData(typeof(TreeNode));
                //ドロップ先のTreeNodeを取得する
                var target =
                    tv.GetNodeAt(tv.PointToClient(new Point(e.X, e.Y)));
                //マウス下のNodeがドロップ先として適切か調べる
                if (target != null && target != source &&
                    !IsChildNode(source, target)) {
                    //ドロップされたNodeのコピーを作成
                    var cln = (TreeNode)source.Clone();
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
            if (e.Data.GetDataPresent(typeof(TreeNode))) {
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
                var tv = (TreeView)sender;
                //マウスのあるNodeを取得する
                var target =
                    tv.GetNodeAt(tv.PointToClient(new Point(e.X, e.Y)));
                //ドラッグされているNodeを取得する
                var source =
                    (TreeNode)e.Data.GetData(typeof(TreeNode));
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

            if ((dde & DragDropEffects.Move) == DragDropEffects.Move) {
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
            toolStripStatusLabel4.Text = $"{canvasControl1.SelectedLayer.Name} - {canvasControl1.SelectedLayer.LayerStatus}";
        }

        private void CanvasControl1_LayerFocused(object sender, LayerFocusedEventArgs e) {
            groupBox2.Text = $"Canvas - [{canvasControl1.SelectedLayer.Name}]";
            toolStripStatusLabel4.Text = $"{canvasControl1.SelectedLayer.Name} - {canvasControl1.SelectedLayer.LayerStatus}";
        }

        private void Timer_Tick(object sender, EventArgs e) {
            UpdateMotorPosition(false);
            
            // Get latest pos from boards
            //for (int i = 0; i < boards.NMotor; ++i) {
            //    txMsg.Text += boards.GetPos(i);
            //    txMsg.Text += " ";
            //}
            // Pos ==Output Bary.==> Lambdas ==Input Bary.==> Controller
        }

        private void UpdateMotorPosition(bool returnZero) {
            short[] targets = new short[Services.Boards.NMotor];

            for (int i = 0; i < Services.Motors.Count; ++i) {
                if (returnZero) {
                    targets[i] = 0;
                }
                else {
                    var motor = Services.Motors[i];
                    targets[i] = (short)(motor.position.Value);
                    //targets[i] = (short)this.Services.Motors[i].position.Value;
                }
            }

            Services.Boards.SendPosDirect(targets);
        }

        private void CanvasControl1_Interpolated(object sender, InterpolatingEventArgs e) {
            toolStripStatusLabel3.Text = String.Join(",", e.Values.ToArray());
        }

        private void CanvasControl1_ModeChanged(object sender, EventArgs e) {
            toolStripStatusLabel2.Text = canvasControl1.SelectedMode.ToString();
        }

        private void CanvasControl1_LayerUpdated(object sender, EventArgs e) {
            //UpdateTreeview();
        }

        private void InitializeSkControl() {
            // Bug: UserControl is not working on design-time.
            canvasControl1 = new CanvasControl();
            canvasControl1.Dock = DockStyle.Fill;
            canvasControl1.Location = new Point(4, 17);
            canvasControl1.Margin = new Padding(4);
            canvasControl1.Name = "canvasControl1";
            canvasControl1.Padding = new Padding(4);
            canvasControl1.Size = new Size(892, 609);
            canvasControl1.TabIndex = 0;

            groupBox2.Controls.Add(canvasControl1);

            _caretaker = new Caretaker();
            Services.Caretaker = _caretaker;
        }

        private void UpdateTreeview() {
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();

            treeView1.Nodes.Add(_root);

            treeView1.EndUpdate();
            treeView1.ExpandAll();

            if (treeView1.Nodes.Count != 0)
                treeView1.SelectedNode = treeView1.Nodes[0];
        }

        private void InvalidateTreeView() {
            // Re-assign root layer into _root from current canvas.
            var layers = canvasControl1.Canvas.Layers;
            var nodes = layers.Select(l => new TreeNode() { Text = l.Name, Tag = l });

            _root.Nodes.Clear();
            _root.Nodes.AddRange(nodes.ToArray());

            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();

            treeView1.Nodes.Add(_root);

            treeView1.EndUpdate();
            treeView1.ExpandAll();

            if (treeView1.Nodes.Count != 0)
                treeView1.SelectedNode = treeView1.Nodes[0];
        }

        private void TaskMaker_KeyDown(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.Escape:
                    canvasControl1.BeginNoneMode();
                    e.Handled = true;
                    break;
                case Keys.P:
                    canvasControl1.Pair();
                    e.Handled = true;
                    break;
            }

            if (e.Control && e.KeyCode == Keys.Z) {
                _caretaker.Undo();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.Z) {
                //_caretaker.Redo();
            }
        }

        //Add node
        private void button1_Click(object sender, EventArgs e) {
            canvasControl1.BeginAddNodeMode();
        }

        // Edit node
        private void button2_Click(object sender, EventArgs e) {
            canvasControl1.BeginEditMode();
        }

        // Manipulate
        private void button3_Click(object sender, EventArgs e) {
            canvasControl1.BeginManipulateMode();
        }

        /// <summary>
        /// Delete node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e) {
            var confirmResult = MessageBox.Show("Are you sure to delete this item ?",
                                     "Confirm Delete!!",
                                     MessageBoxButtons.OKCancel);
            if (confirmResult == DialogResult.OK) {
                canvasControl1.RemoveSelectedNodes();
            }
        }

        /// <summary>
        /// Add layer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e) {
            var node = treeView1.SelectedNode;

            treeView1.BeginUpdate();

            if (node.Parent != null) {
                var layerName = $"New Layer {node.Level} {node.Parent.GetNodeCount(false) + 1}";

                var newLayer = new Layer(layerName);
                var newNode = new TreeNode() { Tag = newLayer, Text = layerName };

                node.Parent.Nodes.Add(newNode);
                canvasControl1.AddLayer(newLayer);
            }
            else {
                var layerName = $"New Layer {node.Level + 1} {node.GetNodeCount(false) + 1}";

                var newLayer = new Layer(layerName);
                var newNode = new TreeNode() { Tag = newLayer, Text = layerName };

                node.Nodes.Add(newNode);
                canvasControl1.AddLayer(newLayer);
            }

            treeView1.EndUpdate();
            treeView1.ExpandAll();
        }

        /// <summary>
        /// Delete layer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e) {
            var node = treeView1.SelectedNode;

            if (node.Parent == null) {
                MessageBox.Show("Root could not be deleted.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else {
                var confirmResult = MessageBox.Show("Are you sure to delete this item ?",
                         "Confirm Delete!!",
                         MessageBoxButtons.OKCancel);

                if (confirmResult != DialogResult.OK)
                    return;

                treeView1.BeginUpdate();

                var parent = node.Parent;
                var children = node.Nodes.OfType<TreeNode>().ToArray();

                node.Remove();
                parent.Nodes.AddRange(children);

                canvasControl1.RemoveLayer(node.Tag as Layer);

                treeView1.EndUpdate();
                treeView1.ExpandAll();
            }
        }


        /// <summary>
        /// Selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e) {
            canvasControl1.SelectedMode = Modes.Selection;
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) {
            var form = new SettingsForm();

            form.ShowDialog();
            form.Dispose();
        }

        private void button10_Click(object sender, EventArgs e) {
            canvasControl1.Triangulate();
        }

        private void button9_Click(object sender, EventArgs e) {
            canvasControl1.SelectedLayer.ShowTargetSelectionForm();
            canvasControl1.Reset();
        }

        private void button12_Click(object sender, EventArgs e) {
            canvasControl1.Pair();
        }

        private void layerToolStripMenuItem_Click(object sender, EventArgs e) {
            canvasControl1.SelectedLayer.ShowTargetSelectionForm();
            canvasControl1.Reset();
        }

        private void button11_Click(object sender, EventArgs e) {
            canvasControl1.SelectedLayer.ShowTargetControlForm();
        }

        private void button13_Click(object sender, EventArgs e) {
            canvasControl1.Unpair();
        }

        private void button14_Click(object sender, EventArgs e) {
            if (Services.Motors.Count != 0) {
                UpdateMotorPosition(true);
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e) {
            if (e.Node.Parent == null) {
                return;
            }

            var layer = e.Node.Tag as Layer;

            canvasControl1.ChooseLayer(layer);

            groupBox2.Text = $"Canvas - [{canvasControl1.SelectedLayer.Name}]";
            toolStripStatusLabel4.Text = $"{canvasControl1.SelectedLayer.Name} - {canvasControl1.SelectedLayer.LayerStatus}";
        }

        private void treeView1_AfterLabelEdit(object sender, NodeLabelEditEventArgs e) {
            e.Node.Text = e.Label;
            (e.Node.Tag as Layer).Name = e.Label; 

            groupBox2.Text = $"Canvas - [{e.Node.Text}]";
            toolStripStatusLabel4.Text = $"{canvasControl1.SelectedLayer.Name} - {canvasControl1.SelectedLayer.LayerStatus}";
        }

        private void button8_Click(object sender, EventArgs e) {
            canvasControl1.SaveAsImage();
        }

        private void button15_Click(object sender, EventArgs e) {
            _caretaker.Undo();
        }

        private async void saveProjectToolStripMenuItem_Click(object sender, EventArgs e) {
            await SaveFile();
        }

        private async void loadProjectToolStripMenuItem_Click(object sender, EventArgs e) {
            await LoadFile();
            InvalidateTreeView();
        }

        private async Task SaveFile() {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Json File|*.json";
            dialog.Title = "Save Project";
            dialog.InitialDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";

            dialog.ShowDialog();

            if (dialog.FileName != "") {
                using (var fs = dialog.OpenFile()) {
                    var state = canvasControl1.Canvas.Save() as CanvasState;
                    var jsonUtf8Bytes = state.ToJsonUtf8Bytes();

                    await fs.WriteAsync(jsonUtf8Bytes, 0, jsonUtf8Bytes.Length);
                }
            }
        }

        private async Task LoadFile() {
            using (var dialog = new OpenFileDialog()) {
                dialog.InitialDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
                dialog.Filter = "Json File|*.json";
                dialog.FilterIndex = 2;
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() == DialogResult.OK) {
                    var path = dialog.FileName;

                    using (var fs = File.OpenText(path)) {
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        var state = await JsonSerializer.DeserializeAsync<CanvasState>(fs.BaseStream, options);

                        canvasControl1.Canvas.Restore(state);
                    }
                }
            }
        }

        //public IMemento Save() {
        //    var treeStructure
        //    var m = new ProgramState(canvasControl1.Canvas.Save() as CanvasState, _root);

        //    return m;
        //}

        //private void TreeToJson(TreeView treeView) {
        //    var nodes = treeView.Nodes;

        //    foreach (var n in nodes) {

        //    }
        //}

        //public void Restore(IMemento m) {
        //    var (canvasState, root) = ((CanvasState, TreeNode))m.GetState();

        //    canvasControl1.Canvas.Restore(canvasState);
        //}
    }

    public class ProgramState : BaseState {
        [JsonInclude]
        public CanvasState CanvasState { get; private set; }
        [JsonInclude]
        public TreeNode Root { get; private set; }

        [JsonConstructor]
        public ProgramState(CanvasState canvasState, TreeNode root) =>
            (CanvasState, Root) = (canvasState, root);

        public override object GetState() {
            return (CanvasState, Root);
        }
    }

    static public class Services {
        //public Boards Boards { get; set; } = new Boards();
        //public Motors Motors { get; set; } = new Motors();
        //public Layer RootLayer { get; set; }
        //public Layer SelectedLayer { get; set; }

        static public Caretaker Caretaker { get; set; }
        static public Boards Boards { get; set; } = new Boards();
        static public Motors Motors { get; set; } = new Motors();
        static public Timer MotorTimer { get; set; } = new Timer() { Interval = 100 };
        static public Canvas Canvas { get; set; }
        static public TreeNode LayerTree { get; set; }
    }
}
