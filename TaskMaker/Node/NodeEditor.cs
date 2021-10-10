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

namespace TaskMaker.Node {
    public partial class NodeEditor : Form {

        private Timer _updateTimer;
        private MoveObject bMove;
        private LinkNode bLink;

        private List<NodeBaseShape> _shapes = new List<NodeBaseShape>();
        private List<LinkShape> _links = new List<LinkShape>();

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
        }

        public void InitializeNodes() {
            var execute = new ExcuteNodeShape() {
                Label = "Execute"
            };

            _shapes.Clear();
            _shapes.Add(execute);

            foreach (var layer in Services.Canvas.Layers) {
                var shape = new LayerNodeShape();

                shape.Label = layer.Name;
                shape.Location = new SKPoint(40, 40);

                _shapes.Add(shape);
            }

            bMove.Shapes = _shapes;
            bLink.Shapes = _shapes;

            _links.Clear();
            bLink.Targets = _links;
        }

        private void NodeEditor_KeyPress(object sender, KeyPressEventArgs e) {
            switch (e.KeyChar) {
            case 'i':
                InitializeNodes();
                break;
            case 'm':
                bMove.BindHandler();
                bLink.ResetHandler();
                break;
            case 'l':
                bLink.BindHandler();
                bMove.ResetHandler();
                break;
            }
        }

        private void SkglControl1_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintGLSurfaceEventArgs e) {
            var canvas = e.Surface.Canvas;

            canvas.Clear(SKColors.White);

            _shapes.ForEach(s => s.Draw(canvas));
            _links.ForEach(l => l.Draw(canvas));
        }

        private void _updateTimer_Tick(object sender, EventArgs e) {
            skglControl1.Invalidate();
        }
    }

    public class MoveObject {
        public Control Parent { get; set; }
        public List<NodeBaseShape> Shapes { get; set; }

        private SKPoint origin;
        private SKPoint initialLocation;
        private NodeBaseShape shape;

        public void BindHandler() {
            Parent.MouseMove += Parent_MouseMove;
            Parent.MouseDown += Parent_MouseDown;
            Parent.MouseUp += Parent_MouseUp;
        }

        private void Parent_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();
                var offset = p - origin;

                if (shape != null) {
                    shape.Location = initialLocation + offset;
                    shape = null;
                }
            }
        }

        private void Parent_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();
                var target = Shapes.FirstOrDefault(s => s.Contains(p));

                if (Shapes.Contains(target)) {
                    origin = p;
                    initialLocation = target.Location;
                    shape = target;
                }
            }
        }

        private void Parent_MouseMove(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();
                var offset = p - origin;

                if (shape != null) {
                    shape.Location = initialLocation + offset;
                }
            }
        }

        public void ResetHandler() {
            Parent.MouseMove -= Parent_MouseMove;
            Parent.MouseDown -= Parent_MouseDown;
            Parent.MouseUp -= Parent_MouseUp;
        }
    }

    public class LinkNode {
        public Control Parent { get; set; }
        public List<NodeBaseShape> Shapes { get; set; }

        public List<LinkShape> Targets { get; set; }

        //private SKPoint origin;
        //private SKPoint initialLocation;

        private NodeBaseShape start;
        private NodeBaseShape end;
        private LinkShape link;

        public void BindHandler() {
            Parent.MouseMove += Parent_MouseMove;
            Parent.MouseDown += Parent_MouseDown;
            Parent.MouseUp += Parent_MouseUp;
        }

        public void ResetHandler() {
            Parent.MouseMove -= Parent_MouseMove;
            Parent.MouseDown -= Parent_MouseDown;
            Parent.MouseUp -= Parent_MouseUp;
        }

        private void Parent_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();
                var target = Shapes.Find(s => s.Contains(p));

                if (Shapes.Contains(target) & target != start) {
                    end = target;

                    link.P1Dummy = null;
                    link.P1Ref = target.Connector0;
                    target.Connector0.Binding = link;
                }
                else {
                    Targets.Remove(link);
                    link = null;
                }
            }
        }

        private void Parent_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();
                var target = Shapes.FirstOrDefault(s => s.Contains(p));

                if (Shapes.Contains(target)) {
                    start = target;
                    link = new LinkShape();
                    link.P0Ref = target.Connector1;
                    link.P1Dummy = new PortShape() { Location = p };
                    target.Connector1.Binding = link;

                    Targets.Add(link);
                }
            }
            else if (e.Button == MouseButtons.Right) {
                var p = e.Location.ToSKPoint();
                var target = Shapes.FirstOrDefault(s => s.Contains(p));

                if (Shapes.Contains(target)) {
                    var link = target.Connector1.Binding;

                    if (link != null) {
                        link.P0Ref.Binding = null;
                        link.P1Ref.Binding = null;
                        Targets.Remove(link);
                    }
                }
            }
        }

        private void Parent_MouseMove(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var p = e.Location.ToSKPoint();

                if (link != null) {
                    link.P1Dummy.Location = p;
                }
            }
        }


    }
}
