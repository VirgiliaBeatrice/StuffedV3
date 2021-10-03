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
using SkiaSharp.Views.Desktop;

namespace TaskMaker {
    public partial class TinyCanvasForm : Form {

        public Canvas Canvas { get; set; }
        private SKGLControl sKControl;
        private Layer parentLayer;
        private CrossPointer pointer;
        private Timer timer;
        private SKRect _viewport;
        private SKRect _window;
        private SKPoint _panCenterInView;
        private SKPoint _panStartInWorld;
        public TinyCanvasForm(Layer layer) {
            InitializeComponent();

            this.parentLayer = layer;
            this.timer = new Timer();
            this.timer.Interval = 10;
            this.timer.Tick += this.Timer_Tick;
            this.timer.Enabled = true;

            this.sKControl = new SKGLControl();
            this.sKControl.Dock = DockStyle.Fill;
            this.groupBox1.Controls.Add(this.sKControl);

            this.sKControl.PaintSurface += this.SKControl_PaintSurface;
            this.sKControl.MouseDown += this.SKControl_MouseDown;
            this.sKControl.MouseMove += this.SKControl_MouseMove;
            this.sKControl.MouseUp += this.SKControl_MouseUp;

            this.groupBox1.Text = layer.Text;

            this.KeyPreview = true;

            Reset();
            this.sKControl.Invalidate();
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            this.Invalidate(true);
        }

        private void Reset() {
            _window = new SKRect() { Location = new SKPoint(), Size = ClientSize.ToSKSize() };
            _viewport = new SKRect() { Location = new SKPoint(), Size = ClientSize.ToSKSize() };
        }

        private void SKControl_MouseUp(object sender, MouseEventArgs e) {
            if (ModifierKeys == Keys.None) {
                if (e.Button == MouseButtons.Left) {
                    pointer = null;
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
                    var local = this.ViewportToWorld().MapPoint(e.Location.ToSKPoint());
                    pointer = new CrossPointer(local);
                    this.parentLayer.IsShownPointer = true;
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
                    pointer.Location = this.ViewportToWorld().MapPoint(e.Location.ToSKPoint());
                    this.parentLayer.Pointer.Location = pointer.Location;

                    // Interpolation
                    parentLayer.Interpolate(this.ViewportToWorld().MapPoint(e.Location.ToSKPoint()));
                    //this.parentLayer.Interpolate(e.Location.ToSKPoint());
                }
            }
        }

        private void SKControl_PaintSurface(object sender, SKPaintGLSurfaceEventArgs ev) {
            var canvas = ev.Surface.Canvas;
            var mat = this.WorldToViewport();

            canvas.Clear(SKColors.White);
            canvas.Concat(ref mat);

            this.parentLayer.Draw(canvas);

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
            return this.WorldToViewport().Invert();
        }

        private void Pan(SKPoint offset) {
            _window.Location = _panStartInWorld - ViewportToWorld().MapVector(offset);
        }
    }
}
