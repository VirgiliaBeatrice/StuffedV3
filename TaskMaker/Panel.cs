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
    public partial class ControlPanel : Form {
        public List<ControlUIWidget> Layers = new List<ControlUIWidget>();
        public Timer timer;
        public List<SKGLControl> canvases = new List<SKGLControl>();
        private BackgroundWorker worker;
        private BackgroundWorker worker1;

        public ControlPanel() {
            InitializeComponent();

            timer = new Timer();
            timer.Interval = 16;
            timer.Tick += Timer_Tick;
            timer.Enabled = true;

            canvases.AddRange(Controls[0].Controls.OfType<SKGLControl>());
            Layers.Add(Services.ViewWidget.Layers[2]);
            Layers.Add(Services.ViewWidget.Layers[3]);

            foreach (var c in canvases) {
                c.PaintSurface += C_PaintSurface;
                c.MouseDown += C_MouseDown;
                c.MouseMove += C_MouseMove;
                c.MouseUp += C_MouseUp;
            }

            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += Worker_DoWork;

            worker1 = new BackgroundWorker();
            worker1.WorkerSupportsCancellation = true;
            worker1.DoWork += Worker_DoWork;

            timer.Start();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e) {
            //if (!(sender as BackgroundWorker).IsBusy) {
            var payload = (Payload)e.Argument;

            payload.Layer.Interpolate(payload.Point);
            //}
        }

        private void C_MouseUp(object sender, MouseEventArgs e) {
            //throw new NotImplementedException();
        }

        private void C_MouseMove(object sender, MouseEventArgs e) {
            var idx = canvases.IndexOf(sender as SKGLControl);
            var p = e.Location.ToSKPoint();

            if (e.Button == MouseButtons.Left) {
                //Layers[idx].Controller.Location = p;


                if (worker.IsBusy | worker1.IsBusy)
                    return;


                var o = new Payload() {
                    Layer = Layers[idx],
                    Point = p
                };


                if (idx == 0) {
                    worker.RunWorkerAsync(o);
                }
                else {
                    worker1.RunWorkerAsync(o);
                }
                
                //Layers[idx].Interpolate(p);
            }
        }

        private void C_MouseDown(object sender, MouseEventArgs e) {
            //var idx = canvases.IndexOf(sender as SKGLControl);
            //var p = e.Location.ToSKPoint();

            //if (e.Button == MouseButtons.Left) {
            //    Layers[idx].Controller.Location = p;
            //    Layers[idx].Interpolate(p);
            //}
        }

        private void C_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e) {
            var recorder = new SKPictureRecorder();
            var picCanvas = recorder.BeginRecording(SKRect.Create(800, 800));
            
            var canvas = e.Surface.Canvas;
            var idx = canvases.IndexOf(sender as SKGLControl);
           
            canvas.Clear(SKColors.White);
            picCanvas.Clear(SKColors.White);

            Layers[idx].Draw(picCanvas);
            Layers[idx].Controller.Draw(picCanvas);

            var pic = recorder.EndRecording();

            canvas.DrawPicture(pic);

            recorder.Dispose();
            picCanvas.Dispose();
            pic.Dispose();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            Invalidate(true);
        }
    }

    public class Payload {
        public ControlUIWidget Layer { get; set; }
        public SKPoint Point { get; set; }
    }
}
