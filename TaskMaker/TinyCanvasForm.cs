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
        private Layer selectedLayer;

        public TinyCanvasForm() {
            InitializeComponent();



            //this.canvasControl.Dock = DockStyle.Fill;
            this.sKControl.Dock = DockStyle.Fill;
            this.panel1.Controls.Add(this.sKControl);

            this.sKControl.PaintSurface += this.SKControl_PaintSurface;
            this.sKControl.MouseMove += this.SKControl_MouseMove;
        }

        private void SKControl_MouseMove(object sender, MouseEventArgs e) {
            this.Invalidate(true);
        }

        public void InitializeLayers() {
            List<Layer> treeNodeList = this.Canvas.RootLayer.Nodes.OfType<Layer>().ToList();
            this.comboBox1.Items.AddRange(treeNodeList.ToArray());
            this.selectedLayer = treeNodeList[0];

            this.Invalidate(true);
        }

        private void SKControl_PaintSurface(object sender, SKPaintSurfaceEventArgs ev) {
            var canvas = ev.Surface.Canvas;

            canvas.Clear();
            this.selectedLayer?.Complex.Draw(canvas);
            this.selectedLayer?.Entities.ForEach(e => e.Draw(canvas));
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            this.selectedLayer = (this.comboBox1.SelectedItem as Layer);

            this.Invalidate(true);
        }
    }
}
