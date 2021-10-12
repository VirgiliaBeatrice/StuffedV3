using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using MathNetExtension;
using TaskMaker.SimplicialMapping;

namespace TaskMaker.Node {
    public partial class NodeEditor : Form {

        private Timer _updateTimer;
        private MoveObject bMove;
        private LinkNode bLink;

        //private List<NodeBaseShape> _shapes = new List<NodeBaseShape>();
        //private List<LinkShape> _links = new List<LinkShape>();
        private IBehavior _behavior;

        public NodeEditor() {
            InitializeComponent();

            _updateTimer = new Timer();
            _updateTimer.Interval = 1;
            _updateTimer.Tick += _updateTimer_Tick;

            skglControl1.PaintSurface += SkglControl1_PaintSurface;

            KeyPreview = true;
            KeyPress += NodeEditor_KeyPress;

            bMove = new MoveObject();
            bMove.Parent = skglControl1;
            bLink = new LinkNode();
            bLink.Parent = skglControl1;

            _updateTimer.Start();

            if (Services.Flow == null) {
                Services.Flow = new Flow();

                Services.Canvas.Layers.ForEach(l => Services.Flow.Layers.Add(new LayerNode(l)));
            }
        }

        public void InitializeNodes() {
            //var execute = new ExcuteNodeShape() {
            //    Label = "Execute"
            //};

            //_shapes.Clear();
            //_shapes.Add(execute);

            //foreach (var layer in Services.Canvas.Layers) {
            //    var shape = new LayerNodeShape();

            //    shape.Label = layer.Name;
            //    shape.Location = new SKPoint(40, 40);

            //    _shapes.Add(shape);
            //}

            //bMove.Shapes = _shapes;
            //bLink.Shapes = _shapes;

            //_links.Clear();
            //bLink.Targets = _links;
        }

        private void ChangeBehavior(IBehavior behavior = null) {
            _behavior?.UnregisterHandler();

            _behavior = behavior;
            _behavior?.RegisterHandler();
        }

        private void NodeEditor_KeyPress(object sender, KeyPressEventArgs e) {
            switch (e.KeyChar) {
                case 'i':
                    InitializeNodes();
                    break;
                case 'm':
                    ChangeBehavior(bMove);
                    break;
                case 'l':
                    ChangeBehavior(bLink);
                    break;
                case (char)Keys.Escape:
                    ChangeBehavior();
                    break;
            }
        }

        private void SkglControl1_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintGLSurfaceEventArgs e) {
            var canvas = e.Surface.Canvas;

            canvas.Clear(SKColors.White);

            Services.Flow.GetNodes().ToList().ForEach(n => n.Shape.Draw(canvas));
            Services.Flow.Links.ForEach(l => l.Shape.Draw(canvas));
            //_shapes.ForEach(s => s.Draw(canvas));
            //_links.ForEach(l => l.Draw(canvas));
        }

        private void _updateTimer_Tick(object sender, EventArgs e) {
            skglControl1.Invalidate();
        }

        private void button1_Click(object sender, EventArgs e) {
            Services.Flow.SetAdjacencyList();
        }

        private void button2_Click(object sender, EventArgs e) {
            var motor = new MotorNode(null);
            var motors = Services.Flow.Motors.OfType<MotorNode>().ToArray();

            motor.Shape.Label = $"Motor{motors.Count() + 1}";
            motor.Shape.Location = new SKPoint(100, 100);

            Services.Flow.Motors.Add(motor);
        }

        private void button3_Click(object sender, EventArgs e) {
            //Services.Map = new NLinearMap();
            //var map = new NLinearMapNode(Services.Map);
            //var maps = Services.Flow.Nodes.OfType<NLinearMapNode>().ToArray();

            //map.Shape.Label = $"Map{maps.Count() + 1}";
            //map.Shape.Location = new SKPoint(200, 200);

            //Services.Flow.Nodes.Add(map);

            //(sender as Button).Enabled = false;
        }

        private void connectToMotorsToolStripMenuItem_Click(object sender, EventArgs e) {

        }
    }

    public interface IBehavior {
        void RegisterHandler();
        void UnregisterHandler();
    }

    public class MoveObject : IBehavior {
        public Control Parent { get; set; }
        private List<INode> Nodes => Services.Flow.GetNodes().ToList();

        private SKPoint origin;
        private SKPoint initialLocation;
        private INode node;

        public void RegisterHandler() {
            Parent.MouseMove += Parent_MouseMove;
            Parent.MouseDown += Parent_MouseDown;
            Parent.MouseUp += Parent_MouseUp;
        }

        private void Parent_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();
                var offset = p - origin;

                if (node != null) {
                    node.Shape.Location = initialLocation + offset;
                    node = null;
                }
            }
        }

        private void Parent_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();
                var target = Nodes.FirstOrDefault(s => s.Shape.Contains(p));

                if (Nodes.Contains(target)) {
                    origin = p;
                    initialLocation = target.Shape.Location;
                    node = target;
                }
            }
        }

        private void Parent_MouseMove(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();
                var offset = p - origin;

                if (node != null) {
                    node.Shape.Location = initialLocation + offset;
                }
            }
        }

        public void UnregisterHandler() {
            Parent.MouseMove -= Parent_MouseMove;
            Parent.MouseDown -= Parent_MouseDown;
            Parent.MouseUp -= Parent_MouseUp;
        }
    }

    public class LinkNode : IBehavior {
        public Control Parent { get; set; }
        private List<INode> Nodes => Services.Flow.GetNodes().ToList();

        private INode @in;
        private INode @out;
        private Link _link;

        public void RegisterHandler() {
            Parent.MouseMove += Parent_MouseMove;
            Parent.MouseDown += Parent_MouseDown;
            Parent.MouseUp += Parent_MouseUp;
        }

        public void UnregisterHandler() {
            Parent.MouseMove -= Parent_MouseMove;
            Parent.MouseDown -= Parent_MouseDown;
            Parent.MouseUp -= Parent_MouseUp;
        }

        private void Parent_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();
                var target = Nodes.Find(s => s.Shape.Contains(p));

                if (Nodes.Contains(target) & target != @out) {
                    @in = target;

                    _link.SetIn(@in);
                    _link = null;
                }
                else {
                    Services.Flow.Links.Remove(_link);
                    _link = null;
                }
            }
        }

        private void Parent_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();
                var target = Nodes.FirstOrDefault(s => s.Shape.Contains(p));


                if (Nodes.Contains(target)) {
                    @out = target;

                    _link = new Link();
                    _link.SetOut(@out);

                    Services.Flow.Links.Add(_link);
                }
            }
            //else if (e.Button == MouseButtons.Right) {
            //    var p = e.Location.ToSKPoint();
            //    var target = Shapes.FirstOrDefault(s => s.Contains(p));

            //    if (Shapes.Contains(target)) {
            //        var link = target.Connector1.Binding;

            //        if (link != null) {
            //            link.P0Ref.Binding = null;
            //            link.P1Ref.Binding = null;
            //            Links.Remove(_link);
            //        }
            //    }
            //}
        }

        private void Parent_MouseMove(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();

                if (_link != null) {
                    _link.Update(p);
                }
            }
        }


    }
}
