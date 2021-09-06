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
        private SKControl sKControl = new SKControl();
        private Layer parentLayer;

        public TinyCanvasForm(Layer layer) {
            InitializeComponent();

            this.parentLayer = layer;

            this.sKControl.Dock = DockStyle.Fill;
            this.panel1.Controls.Add(this.sKControl);

            this.sKControl.PaintSurface += this.SKControl_PaintSurface;
            this.sKControl.MouseMove += this.SKControl_MouseMove;
        }

        private void SKControl_MouseMove(object sender, MouseEventArgs e) {
            this.Invalidate(true);
        }

        private void SKControl_PaintSurface(object sender, SKPaintSurfaceEventArgs ev) {
            var canvas = ev.Surface.Canvas;

            canvas.Clear();

            this.parentLayer.Draw(canvas);
        }
    }
}
