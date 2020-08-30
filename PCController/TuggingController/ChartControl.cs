using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Runtime.CompilerServices;

namespace TuggingController {
    public partial class ChartControl : UserControl {
        private SKControl skControl;
        private Button btTest;

        public Chart_v1 Chart { get; set; }
        public ChartControl() {
            InitializeComponent();

            this.skControl = new SKControl();
            this.skControl.Location = new Point(0, 0);
            this.skControl.Size = new Size(this.Size.Width, this.Size.Height);

            this.Controls.AddRange(new Control[] {
                this.skControl,
            });

            this.Chart = new Chart_v1(new SKSize(this.Size.Width, this.Size.Height));
            this.Chart.Transform.Scale = SKMatrix.MakeScale(1.0f, -1.0f);
            this.Chart.Transform.Translation = SKMatrix.MakeTranslation(this.Size.Width / 2, this.Size.Height / 2);

            this.skControl.PaintSurface += this.SKControl_PaintSurface;
            this.MouseMove += this.ChartControl_MouseMove;

            this.SizeChanged += this.ChartControl_SizeChanged;
        }

        private void ChartControl_SizeChanged(object sender, EventArgs e) {
            this.Chart.Size = new SKSize(this.Size.Width, this.Size.Height);
            this.Chart.Transform.Scale = SKMatrix.MakeScale(1.0f, -1.0f);
            this.Chart.Transform.Translation = SKMatrix.MakeTranslation(this.Size.Width / 2, this.Size.Height / 2);
            this.skControl.Size = new Size(this.Size.Width, this.Size.Height);

            this.Invalidate(true);
        }

        private void ChartControl_MouseMove(object sender, MouseEventArgs e) {
            this.skControl.Invalidate();
        }

        private void SKControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e) {
            this.Draw(e.Surface.Canvas);
        }

        private void Draw(SKCanvas canvas) {
            canvas.Clear();

            this.Chart.Draw(canvas);
            canvas.DrawText("Hello World!", new SKPoint(100, 100), new SKPaint() { Color = SKColors.Black });

        }
    }
}
