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
        public NodeBaseShape Target { get; set; }
        public SKPoint Mid => new SKPoint(ClientSize.Width / 2, ClientSize.Height / 2);

        private Timer _updateTimer;
        private EditorEventManager _eventManager;

        public NodeEditor() {
            InitializeComponent();

            _updateTimer = new Timer();
            _updateTimer.Interval = 1;
            _updateTimer.Tick += _updateTimer_Tick;

            _eventManager = new EditorEventManager(this);

            skglControl1.PaintSurface += SkglControl1_PaintSurface;

            KeyPreview = true;
            KeyPress += NodeEditor_KeyPress;

            _updateTimer.Start();
        }

        public void InitializeNodes() {
            var node = new MotorNode();
            node.Shape.Location = Mid;
            node.Shape.RegisterEvents(_eventManager);

            Services.Graph = new Graph();
            Services.Graph.AddNode(node);
        }

        private void NodeEditor_KeyPress(object sender, KeyPressEventArgs e) {
            switch (e.KeyChar) {
                case 'i':
                    InitializeNodes();
                    break;
                case 'm':
                    var node = new MapNode();
                    node.Shape.Location = Mid;
                    node.Shape.RegisterEvents(_eventManager);

                    Services.Graph.AddNode(node);
                    break;
                case 'l':

                    break;
                case (char)Keys.Escape:

                    break;
            }
        }

        private void SkglControl1_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintGLSurfaceEventArgs e) {
            var canvas = e.Surface.Canvas;

            canvas.Clear(SKColors.LightGray);

            DrawNodes(canvas);
        }

        private void DrawNodes(SKCanvas sKCanvas) {
            if (Services.Graph == null)
                return;

            foreach(var node in Services.Graph.Nodes) {
                node.Shape.Draw(sKCanvas);
            }
        }

        private void _updateTimer_Tick(object sender, EventArgs e) {
            skglControl1.Invalidate();
        }

        private void button1_Click(object sender, EventArgs e) {
        }

        private void button2_Click(object sender, EventArgs e) {
        }

        private void button3_Click(object sender, EventArgs e) {
        }

        private void connectToMotorsToolStripMenuItem_Click(object sender, EventArgs e) {

        }
    }



    public class EditorEventManager {
        public NodeEditor Editor { get; set; }
        public object Target { get; set; }

        public event EventHandler<EditorMouseEventArgs> Click;
        public event EventHandler<EditorDragEventArgs> DragStart;
        public event EventHandler<EditorDragEventArgs> DragOver;
        public event EventHandler<EditorDragEventArgs> DragEnd;
        //public event EventHandler<EditorConnectEventArgs> ConnectStart;
        //public event EventHandler<EditorConnectEventArgs> ConnectOver;
        //public event EventHandler<EditorConnectEventArgs> ConnectEnd;


        //public event MouseEventHandler OnClicked;
        //public event MouseEventHandler OnClicked;

        private bool _isDrag = false;
        private bool _isPressed = false;
        private bool _isConnect = false;
        private SKPoint _start;
        private float _delta = 6.0f;
        private Timer _timer;
        private LinkShape _connection;

        public EditorEventManager(NodeEditor editor) {
            Editor = editor;

            _timer = new Timer();
            _timer.Interval = 100;

            BindOriginalEventHandlers();
        }

        public void BindOriginalEventHandlers() {
            Editor.skglControl1.MouseDown += Editor_MouseDown;
            Editor.skglControl1.MouseUp += Editor_MouseUp;
            Editor.skglControl1.MouseMove += Editor_MouseMove;


            // For debug
            //Click += OnClick;
            //DragStart += OnDragStart;
            //DragOver += OnDragOver;
            //DragEnd += OnDragEnd;
        }

        private void Editor_MouseMove(object sender, MouseEventArgs e) {
            var diff = e.Location.ToSKPoint() - _start;

            if (!_isPressed)
                return;

            if (!_isDrag) {
                if (Math.Abs(diff.X) > _delta | Math.Abs(diff.Y) > _delta) {
                    var args = e.ToEditorEvent(_start, diff);

                    DragStart?.Invoke(this, args);

                    if (args.Target != null) {
                        if (args.Target is NodeBaseShape) {
                            _isDrag = true;
                            Target = args.Target;
                        }
                        else if (args.Target is PortShape) {
                            _isDrag = true;
                            Target = args.Target;

                            Services.Graph.LinksS.Add(args.Link as LinkShape);
                        }

                    }

                }
                else
                    return;
            }
            else {
                var args = e.ToEditorEvent(_start, diff);
                args.Target = Target;

                DragOver?.Invoke(this, args);
            }
        }

        private void Editor_MouseUp(object sender, MouseEventArgs e) {
            if (_isDrag) {
                var diff = e.Location.ToSKPoint() - _start;
                var args = e.ToEditorEvent(_start, diff);
                args.Target = Target;

                DragEnd?.Invoke(this, args);

                if (args.Link != null) {
                    if ((args.Link as LinkShape).IsDummy) {
                        Services.Graph.LinksS.Remove(args.Link as LinkShape);
                    }
                }

                Target = null;
            }
            else {
                var args = e.ToEditorEvent();

                Click?.Invoke(this, args);
            }

            _isDrag = false;
            _isPressed = false;
            _start = SKPoint.Empty;
        }

        private void Editor_MouseDown(object sender, MouseEventArgs e) {
            _start = e.Location.ToSKPoint();
            _isPressed = true;
        }

        public virtual void OnClick(object sender, EditorMouseEventArgs e) {
            var isHit = e.Location;
            
            Console.WriteLine("OnClick");
        }

        public virtual void OnDragStart(object sender, EditorMouseEventArgs e) {
            Console.WriteLine("OnDragStart");
        }

        public virtual void OnDragOver(object sender, EditorDragEventArgs e) {
            Console.WriteLine("OnDragOver");
        }

        public virtual void OnDragEnd(object sender, EditorMouseEventArgs e) {
            Console.WriteLine("OnDragEnd");
        }
    }

    public class EditorMouseEventArgs : EventArgs {
        public MouseButtons Button { get; private set; }
        public SKPoint Location { get; private set; }
        public bool Handled { get; set; } = false;
        public object Target { get; set; } = null;

        public EditorMouseEventArgs(MouseButtons button, SKPoint location) =>
            (Button, Location) = (button, location);
    }

    public class EditorDragEventArgs : EventArgs {
        public MouseButtons Button { get; private set; }
        public SKPoint Start { get; private set; }
        public SKPoint Delta { get; private set; }
        //public bool Handled { get; set; } = false;
        public object Target { get; set; } = null;
        public object Link { get; set; } = null;

        public EditorDragEventArgs(MouseButtons button, SKPoint start, SKPoint delta) =>
            (Button, Start, Delta) = (button, start, delta);
    }

    public class EditorConnectEventArgs : EventArgs {
        public MouseButtons Buttons { get; set; }
        public PortShape In { get; set; }
        public PortShape Out { get; set; }
        public object Target { get; set; }
    }

    public static class Extension {
        public static EditorMouseEventArgs ToEditorEvent(this MouseEventArgs args) =>
            new EditorMouseEventArgs(args.Button, args.Location.ToSKPoint());

        public static EditorDragEventArgs ToEditorEvent(this MouseEventArgs args, SKPoint start, SKPoint delta) =>
            new EditorDragEventArgs(args.Button, start, delta);
    }
}
