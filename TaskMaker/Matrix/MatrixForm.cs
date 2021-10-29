using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Numpy;
using SkiaSharp;
using System.Reactive;
using SkiaSharp.Views.Desktop;

namespace TaskMaker.Matrix {
    public partial class MatrixForm : Form {
        public Timer timer;
        public NDarray<bool> mat;
        public MatrixShape MatrixShape;

        public MatrixForm() {
            InitializeComponent();

            timer = new Timer();
            timer.Interval = 16;
            timer.Tick += Timer_Tick;
            timer.Enabled = true;

            //var table = new bool[4];
            //mat = np.array(table);

            MatrixShape = new MatrixShape(new int[] { 3, 4 });
            //MatrixShape.Bounds.Size.ToDrawingSize();
            
            ClientSize = MatrixShape.Bounds.Size.ToDrawingSize().ToSize();

            skglControl1.PaintSurface += SkglControl1_PaintSurface;
            skglControl1.DoubleClick += SkglControl1_DoubleClick;

            timer.Start();
        }

        private void SkglControl1_DoubleClick(object sender, EventArgs e) {
            var args = e as MouseEventArgs;

            MatrixShape.OnDoubleClick(args.Location.ToSKPoint());
        }

        private void SkglControl1_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintGLSurfaceEventArgs e) {
            var canvas = e.Surface.Canvas;

            canvas.Clear(SKColors.White);

            DrawThis(canvas);
        }

        private void Timer_Tick(object sender, EventArgs e) {
            skglControl1.Invalidate();
        }

        private void DrawThis(SKCanvas sKCanvas) {
            MatrixShape.Location = new SKPoint(0, 0);
            MatrixShape.Draw(sKCanvas);
        }
    }

    public class MatrixShape {
        public SKPoint Location { get; set; } = SKPoint.Empty;
        public SKRect Bounds { get; set; }
        public SKMatrix Transform => SKMatrix.CreateTranslation(Location.X, Location.Y);
        public List<ElementShape> Elements { get; set; } = new List<ElementShape>();

        private int[] _shape;

        public MatrixShape(int[] shape) {
            _shape = shape;
            Initialize();
        }

        public int[] IndexOf(ElementShape element) {
            var flatIdx = Elements.IndexOf(element);

            return new int[] {
                flatIdx / _shape[1] % _shape[0],
                flatIdx / _shape[0] % _shape[1] };
        }

        public void OnDoubleClick(SKPoint p) {
            var local = Transform.MapPoint(p);

            var target = Elements.Find(e => e.Contains(p));

            if (Elements.Contains(target)) {
                target.OnDoubleClick(p);
                var idx = IndexOf(target);
            }
        }

        public void Initialize() {
            var start = new SKPoint(20, 20);

            if (_shape.Length == 2) {
                for (var i=0;i<_shape[0]; ++i) {
                    for(var j=0;j<_shape[1]; ++j) {
                        var location = start + new SKPoint(j * 40, i * 40);
                        var element = new ElementShape() {
                            Location = location,
                        };

                        Elements.Add(element);
                    }
                }
            }
            else if (_shape.Length == 1) {
                for (var i = 0; i < _shape[0]; ++i) {
                    var location = start + new SKPoint(0, i * 40);
                    var element = new ElementShape() {
                        Location = location,
                    };

                    Elements.Add(element);
                }
            }
            else {
                throw new ArgumentOutOfRangeException();
            }

            Invalidate();
        }

        public bool Contains(SKPoint p) {
            var localP = Transform.MapPoint(p);

            return Bounds.Contains(localP);
        }

        public void Invalidate() {
            if (_shape.Length == 1) {
                Bounds = SKRect.Create(40, _shape[0] * 40);
            }
            else {
                Bounds = SKRect.Create(_shape[1] * 40, _shape[0] * 40);
            }
        }

        public SKPicture DrawThis() {
            var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bounds);

            var strokePaint = new SKPaint();
            strokePaint.Color = SKColors.DarkSlateGray;
            strokePaint.StrokeWidth = 2;
            strokePaint.Style = SKPaintStyle.Stroke;

            var rev = Elements.Reverse<ElementShape>().ToList();
            rev.ForEach(e => e.Draw(canvas));

            canvas.DrawRect(Bounds, strokePaint);

            var pic = recorder.EndRecording();

            recorder.Dispose();
            canvas.Dispose();

            return pic;
        }

        public void Draw(SKCanvas canvas) {
            var pic = DrawThis();

            canvas.Save();

            var mat = Transform;
            canvas.Concat(ref mat);

            canvas.DrawPicture(pic);
            canvas.Restore();

            pic.Dispose();
        }
    }

    public class ElementShape {
        public bool IsSelected { get; set; } = false;
        public SKPoint Location { get; set; } = SKPoint.Empty;
        public SKRect Bounds { get; set; }
        public float Radius { get; set; } = 20.0f;
        public SKMatrix Transform => SKMatrix.CreateTranslation(Location.X, Location.Y);

        public ElementShape() {
            Invalidate();
        }

        public bool Contains(SKPoint p) {
            var localP = Transform.Invert().MapPoint(p);

            return SKPoint.Distance(SKPoint.Empty, localP) <= Radius;
        }

        public void OnDoubleClick(SKPoint p) {
            IsSelected = true;
        }

        public void Invalidate() {
            Bounds = SKRect.Create(Radius, Radius);
            
            var anchor = SKMatrix.CreateTranslation(-Radius, -Radius);

            Bounds = anchor.MapRect(Bounds);
        }

        public SKPicture DrawThis() {
            var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bounds);

            using(var fill = new SKPaint()) 
            using(var stroke = new SKPaint()) {
                stroke.Color = SKColors.DarkGray;
                stroke.StrokeWidth = 2;
                stroke.Style = SKPaintStyle.Stroke;

                if (IsSelected)
                    fill.Color = SKColors.CadetBlue;
                else
                    fill.Color = SKColors.Aqua;

                canvas.DrawCircle(SKPoint.Empty, Radius, fill);
                canvas.DrawCircle(SKPoint.Empty, Radius, stroke);
            }

            var pic = recorder.EndRecording();

            recorder.Dispose();

            return pic;
        }

        public void Draw(SKCanvas sKCanvas) {
            var pic = DrawThis();

            sKCanvas.Save();

            var mat = Transform;

            sKCanvas.Concat(ref mat);
            sKCanvas.DrawPicture(pic);

            sKCanvas.Restore();

            pic.Dispose();
        }
    }
}
