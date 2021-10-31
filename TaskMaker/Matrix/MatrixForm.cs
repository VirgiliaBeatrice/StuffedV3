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
using TaskMaker.SimplicialMapping;
using System.Diagnostics;

namespace TaskMaker.Matrix {
    public partial class MatrixForm : Form {
        public Timer timer;
        public MatrixShape MatrixShape;
        public NLinearMap Map;

        public MatrixForm(NLinearMap map) {
            InitializeComponent();

            Map = map;

            timer = new Timer();
            timer.Interval = 16;
            timer.Tick += Timer_Tick;
            timer.Enabled = true;

            MatrixShape = new MatrixShape(Map.Tensor.shape.Dimensions.Skip(1).ToArray(), Map.Tensor);
            ClientSize = MatrixShape.Bounds.Size.ToDrawingSize().ToSize();

            skglControl1.PaintSurface += SkglControl1_PaintSurface;
            skglControl1.DoubleClick += SkglControl1_DoubleClick;

            timer.Start();
        }

        private void SkglControl1_DoubleClick(object sender, EventArgs e) {
            var args = e as MouseEventArgs;

            var idx = MatrixShape.OnDoubleClick(args.Location.ToSKPoint());
        
            if(idx != null) {
                var slice = $":,{string.Join(",", idx.AsEnumerable())}";
                Map.Tensor[slice] = Map.Layers[0].BindedTarget.CreateTargetState().ToVector().ToArray();
            }
        }

        private void SkglControl1_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e) {
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
        private NDarray _tensor;

        public MatrixShape(int[] shape, NDarray tensor) {
            _shape = shape;
            _tensor = tensor;

            Initialize();
        }

        public int[] IndexOf(ElementShape element) {
            var flatIdx = Elements.IndexOf(element);

            if (_shape.Length == 1) {
                return new int[] { flatIdx };
            }
            else if (_shape.Length == 2){
                var row = flatIdx / _shape[1];
                var col = flatIdx - row * _shape[1];
                //row * _shape[1] + col = flatIdx;

                return new int[] {
                    row,
                    col };
            }
            else {
                throw new ArgumentOutOfRangeException();
            }
        }

        public int[] OnDoubleClick(SKPoint p) {
            var local = Transform.MapPoint(p);

            var target = Elements.Find(e => e.Contains(p));

            if (Elements.Contains(target)) {
                target.OnDoubleClick(p);

                var idx = IndexOf(target);

                return idx;
            }

            return null;
        }

        public void Initialize() {
            var start = new SKPoint(20, 20);

            if (_shape.Length == 2) {
                for (var i=0;i<_shape[0]; ++i) {
                    for(var j=0;j<_shape[1]; ++j) {
                        var location = start + new SKPoint(j * 40, i * 40);
                        var isNaN = bool.Parse(np.isnan(_tensor[$":,{i},{j}"].sum()).repr);
                        var element = new ElementShape() {
                            Location = location,
                            Label = $"({i},{j})",
                            IsSelected = !isNaN,
                        };

                        Elements.Add(element);
                    }
                }
            }
            else if (_shape.Length == 1) {
                for (var i = 0; i < _shape[0]; ++i) {
                    var location = start + new SKPoint(0, i * 40);
                    var isNaN = bool.Parse(np.isnan(_tensor[$":,{i}"].sum()).repr);
                    var element = new ElementShape() {
                        Location = location,
                        Label = $"({i})",
                        IsSelected = !isNaN
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

            //canvas.DrawRect(Bounds, strokePaint);

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
        public string Label { get; set; } = "";
        public bool IsSelected { get; set; } = false;
        public SKPoint Location { get; set; } = SKPoint.Empty;
        public SKRect Bounds { get; set; }
        public float Radius { get; set; } = 20.0f;
        public SKMatrix Transform => SKMatrix.CreateTranslation(Location.X, Location.Y);
        public SKPoint Click { get; set; } = SKPoint.Empty;

        private Stopwatch _animator;
        private bool _isPlaying = false;
        private int _duration = 200;

        public ElementShape() {
            Invalidate();
            _animator = new Stopwatch();
        }

        public bool Contains(SKPoint p) {
            var localP = Transform.Invert().MapPoint(p);

            return SKPoint.Distance(SKPoint.Empty, localP) <= Radius;
        }

        public void OnDoubleClick(SKPoint p) {
            //Click = Transform.Invert().MapPoint(p);
            IsSelected = true;
            _animator.Restart();
            _isPlaying = true;
        }

        public void Invalidate() {
            Bounds = SKRect.Create(Radius, Radius);
            
            var anchor = SKMatrix.CreateTranslation(-Radius, -Radius);

            Bounds = anchor.MapRect(Bounds);
        }

        public SKPicture DrawThis() {
            _animator.Stop();

            var step = (decimal)_animator.ElapsedMilliseconds / _duration;

            var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bounds);

            using(var text = new SKPaint())
            using(var fill = new SKPaint()) 
            using(var stroke = new SKPaint()) {
                stroke.Color = SKColors.DarkGray;
                stroke.StrokeWidth = 2;
                stroke.Style = SKPaintStyle.Stroke;

                fill.IsAntialias = true;

                if (IsSelected)
                    fill.Color = SKColor.Parse("#757de8");
                else
                    fill.Color = SKColor.Parse("#3f51b5");

                text.Color = SKColors.White;
                text.TextSize = 10;
                text.IsAntialias = true;

                canvas.DrawCircle(SKPoint.Empty, Radius, fill);
                //canvas.DrawCircle(SKPoint.Empty, Radius, stroke);

                if (_isPlaying) {
                    fill.Color = SKColor.Parse("#002984");
                    canvas.DrawCircle(Click, (float)step * Radius, fill);
                }

                canvas.DrawText(Label, SKPoint.Empty, text);
            }

            var pic = recorder.EndRecording();

            recorder.Dispose();

            if (_animator.ElapsedMilliseconds < _duration)
               _animator.Start();
            else {
                _isPlaying = false;
            }

            return pic;
        }

        public void Draw(SKCanvas sKCanvas) {
            var pic = DrawThis();

            sKCanvas.Save();

            var mat = Transform;
            //var clip = new SKPath();
            //clip.AddCircle(0, 0, Radius);


            sKCanvas.Concat(ref mat);
            //sKCanvas.ClipPath(clip);
            sKCanvas.DrawPicture(pic);

            sKCanvas.Restore();

            pic.Dispose();
            //clip.Dispose();
        }
    }
}
