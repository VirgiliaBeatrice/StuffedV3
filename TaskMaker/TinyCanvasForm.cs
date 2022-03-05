using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Linq;
using System.Windows.Forms;
using MathNet.Numerics.LinearAlgebra;

namespace TaskMaker {
    public partial class TinyCanvasForm : Form {

        public ViewWidget Canvas { get; set; }
        private SKGLControl sKControl;
        private ControlUIWidget _layer;
        private CrossPointer pointer;
        private Timer timer;
        private SKRect _viewport;
        private SKRect _window;
        private SKPoint _panCenterInView;
        private SKPoint _panStartInWorld;
        private Controller _controller;
        public TinyCanvasForm(ControlUIWidget layer) {
            InitializeComponent();

            _layer = layer;
            timer = new Timer();
            timer.Interval = 10;
            timer.Tick += Timer_Tick;
            timer.Enabled = true;

            sKControl = new SKGLControl();
            sKControl.Dock = DockStyle.Fill;
            groupBox1.Controls.Add(sKControl);

            sKControl.PaintSurface += SKControl_PaintSurface;
            sKControl.MouseDown += SKControl_MouseDown;
            sKControl.MouseMove += SKControl_MouseMove;
            sKControl.MouseUp += SKControl_MouseUp;

            groupBox1.Text = layer.Name;

            KeyPreview = true;

            Reset();
            sKControl.Invalidate();
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            Invalidate(true);
        }

        private void Reset() {
            _window = new SKRect() { Location = new SKPoint(), Size = ClientSize.ToSKSize() };
            _viewport = new SKRect() { Location = new SKPoint(), Size = ClientSize.ToSKSize() };
        }

        private void SKControl_MouseUp(object sender, MouseEventArgs e) {
            if (ModifierKeys == Keys.None) {
                if (e.Button == MouseButtons.Left) {
                    pointer = null;
                    //_layer.Controller.IsVisible = false;

                }
                else if (e.Button == MouseButtons.Right) {
                    Reset();
                }
            }
        }

        private void SKControl_MouseDown(object sender, MouseEventArgs e) {
            if (ModifierKeys == Keys.Control) {
                if (e.Button == MouseButtons.Left) {
                    _panCenterInView = e.Location.ToSKPoint();
                    _panStartInWorld = _window.Location;
                }
            }
            else if (ModifierKeys == Keys.None) {
                if (e.Button == MouseButtons.Left) {
                    var wP = ViewportToWorld().MapPoint(e.Location.ToSKPoint());
                    pointer = new CrossPointer(wP);

                    _layer.Controller.IsVisible = true;

                    //var controller = _layer.Controller.Contains(wP)? _layer.Controller : null;

                    //if(controller != null) {
                    //    _controller = controller;
                    //}
                }
            }
        }

        private void SKControl_MouseMove(object sender, MouseEventArgs e) {
            if (ModifierKeys == Keys.Control) {
                if (e.Button == MouseButtons.Left) {
                    Pan(e.Location.ToSKPoint() - _panCenterInView);
                }
            }
            else if (ModifierKeys == Keys.None) {
                if (e.Button == MouseButtons.Left) {
                    var wP = ViewportToWorld().MapPoint(e.Location.ToSKPoint());

                    pointer.Location = wP;
                    _layer.Controller.Location = wP;

                    //_controller.Location = wP;

                    // Interpolation
                    //_layer.Interpolate(ViewportToWorld().MapPoint(e.Location.ToSKPoint()));
                    if (_layer.BindedTarget != null) {
                        //var result = _layer.MultiBary.Interpolate(wP);

                        //_layer.BindedTarget.FromVector(Vector<float>.Build.Dense(result.Cast<float>().ToArray()));
                    }
                }
            }
        }

        private void SKControl_PaintSurface(object sender, SKPaintGLSurfaceEventArgs ev) {
            var canvas = ev.Surface.Canvas;
            var mat = WorldToViewport();

            canvas.Clear(SKColors.White);
            canvas.Concat(ref mat);

            _layer.Draw(canvas);

            if (pointer != null) {
                pointer.Draw(canvas);
            }
        }

        private SKMatrix WorldToViewport() {
            var translate = SKMatrix.CreateTranslation(-_window.Left, -_window.Top);
            var scaleMat = SKMatrix.CreateScale(_viewport.Width / _window.Width, _viewport.Height / _window.Height);
            var translateInv = SKMatrix.CreateTranslation(_viewport.Left, _viewport.Top);

            // T_i * S * T
            return translate.PostConcat(scaleMat).PostConcat(translateInv);
        }

        private SKMatrix ViewportToWorld() {
            return WorldToViewport().Invert();
        }

        private void Pan(SKPoint offset) {
            _window.Location = _panStartInWorld - ViewportToWorld().MapVector(offset);
        }
    }
}
