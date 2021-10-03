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
        private SKControl sKControl;
        private Layer parentLayer;
        private CrossPointer pointer;
        private Timer timer;

        public TinyCanvasForm(Layer layer) {
            InitializeComponent();

            this.parentLayer = layer;
            this.timer = new Timer();
            this.timer.Interval = 100;
            this.timer.Tick += this.Timer_Tick;
            this.timer.Enabled = true;

            this.sKControl = new SKControl();
            this.sKControl.Dock = DockStyle.Fill;
            this.groupBox1.Controls.Add(this.sKControl);

            this.sKControl.PaintSurface += this.SKControl_PaintSurface;
            this.sKControl.MouseDown += this.SKControl_MouseDown;
            this.sKControl.MouseMove += this.SKControl_MouseMove;
            this.sKControl.MouseUp += this.SKControl_MouseUp;

            this.groupBox1.Text = layer.Text;

            this.sKControl.Invalidate();

            this.GotFocus += this.TinyCanvasForm_GotFocus;
            this.LostFocus += this.TinyCanvasForm_LostFocus;
        }

        private void TinyCanvasForm_GotFocus(object sender, EventArgs e) {
            this.timer.Stop();
        }

        private void TinyCanvasForm_LostFocus(object sender, EventArgs e) {
            this.timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            this.Invalidate(true);
        }

        private void SKControl_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                pointer = null;
            }
            this.Invalidate(true);
        }

        private void SKControl_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                pointer = new CrossPointer(e.Location.ToSKPoint());
                this.parentLayer.IsShownPointer = true;
            }
            this.Invalidate(true);
        }

        private void SKControl_MouseMove(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                pointer.Location = e.Location.ToSKPoint();
                this.parentLayer.Pointer.Location = pointer.Location;

                // Interpolation
                parentLayer.Interpolate_v1(e.Location.ToSKPoint());
                //this.parentLayer.Interpolate(e.Location.ToSKPoint());
            }

            this.Invalidate(true);
        }

        private void SKControl_PaintSurface(object sender, SKPaintSurfaceEventArgs ev) {
            var canvas = ev.Surface.Canvas;

            canvas.Clear();

            this.parentLayer.Draw(canvas);

            if (pointer != null) {
                pointer.Draw(canvas);
            }


        }
    }
}
