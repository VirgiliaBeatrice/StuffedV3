using System;
using System.Numerics;
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
using NLog;


namespace TuggingController
{
    public partial class Form1 : Form
    {
        public PointChart chart;
       
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public Form1()
        {
            InitializeComponent();

            var config = new NLog.Config.LoggingConfiguration();

            var logConsole = new NLog.Targets.ColoredConsoleTarget("Form1");

            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            NLog.LogManager.Configuration = config;

            Logger.Debug("Hello World");
            //RobotController ctrl = new RobotController();
            skControl1.Location = new Point(0, 0);
            skControl1.Size = this.Size;
            skControl1.PaintSurface += SkControl1_PaintSurface;
            this.SizeChanged += Form1_SizeChanged;
            this.chart = new PointChart();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            skControl1.Size = this.Size;
        }

        private void SkControl1_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            this.chart.Draw(e.Surface.Canvas, this.Size.Width, this.Size.Height);
            //SharedPage.OnPainting(sender, e);
        }

        private void skControl1_MouseClick(object sender, MouseEventArgs e)
        {
            // Add new point

            switch (e.Button)
            {
                case MouseButtons.Left:
                    Logger.Debug("Add new point");
                    Logger.Debug(e.Location);
                    this.chart.AddPoint(e.Location);
                    skControl1.Invalidate();
                    break;
                default:
                    break;
            }

        }
    }

    public abstract class Chart
    {
        #region Properties
        public float Margin { get; set; } = 100;
        public float LabelTextSize { get; set; } = 16;

        public SKCanvas Canvas;
        protected float width;
        protected float height;

        protected readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Methods
        public void Draw(SKCanvas canvas, int width, int height)
        {
            this.Canvas = canvas;
            this.width = width - (float)this.Margin * 2.0f;
            this.height = height - (float)this.Margin * 2.0f;

            this.Canvas.Clear(SKColor.Empty);

            this.DrawArea(this);
            this.DrawContent(this);
        }

        public abstract void DrawArea(object ctx);
        public abstract void DrawContent(object ctx);
        #endregion
    }

    public class PointChart : Chart
    {
        #region Properties

        public float PointSize { get; set; } = 14;
        public List<SKPoint> Points = new List<SKPoint>();

        private float left;
        private float right;
        private float top;
        private float bottom;
        #endregion

        #region Methods

        //protected void DrawArrow(SKPoint p0, SKPoint p1) {
        //    var shader = SKShader.CreateLinearGradient(
        //        p0,
        //        p1,
        //        new[] { SKColors.Red, SKColors.DarkGreen },
        //        null,
        //        SKShaderTileMode.Clamp
        //        );
        //    var paint = new SKPaint {
        //        IsAntialias = true,
        //        Shader = shader
        //    };

        //    var dir = SKPoint.Normalize(p1 - p0);
        //    var dir = Vector2()
        //    var lRotMat = SKMatrix.MakeRotationDegrees(30.0f);
        //    var rRotMat = SKMatrix.MakeRotationDegrees(-30.0f);
        //    var tranMat = SKMatrix.MakeTranslation((p1-p0).X, (p1-p0).Y);

        //    this.Canvas.DrawLine(p0, p1, paint);
        //    this.Canvas.DrawLine(p1, tranMat.MapPoint(lRotMat.MapPoint(dir * 20)), paint);
        //    this.Canvas.DrawLine(p1, tranMat.MapPoint(rRotMat.MapPoint(dir * 20)), paint);
        //}

        private double DegreeToRadian(double degree) {
            return Math.PI * degree / 180.0;
        }
        private double DegreeToRadian(int degree) {
            return Math.PI * degree / 180.0;
        }
        private enum ArrowDirections {
            Top = 90,
            Bottom = -90,
            Left = 180,
            Right = 0
        }
        private void DrawArrow(SKPoint start, SKPoint end) {
            var mat = SKMatrix.MakeIdentity();
            var dir = end - start;
            var dist = SKPoint.Distance(start, end);
            var rotMat = SKMatrix.MakeRotation((float)Math.Atan2(dir.Y, dir.X));
            Logger.Debug("{0} {1}", dir.Y, dir.X);

            //SKMatrix.PostConcat(ref mat, rotMat);

            var path = new SKPath();
            var s = new SKPoint(0, 0);
            var e = new SKPoint(100, 0);
            double arrowSize = 16.0;
            double arrowAngle = 15.0;
            double d = Math.Tan(DegreeToRadian(arrowAngle)) * arrowSize;

            var lArrow = SKPoint.Add(e, new SKPoint(-(float)arrowSize, (float)d));
            var rArrow = SKPoint.Add(e, new SKPoint(-(float)arrowSize, -(float)d));
            path.MoveTo(s);
            path.LineTo(e);
            path.LineTo(lArrow);
            path.MoveTo(e);
            path.LineTo(rArrow);

            var paint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.4f)),
                Style = SKPaintStyle.Stroke
                //Shader = shader
            };

            this.Canvas.Concat(ref rotMat);
            this.Canvas.DrawPath(path, paint);
        }
        private void DrawIdentityArrow() {
            var p0 = new SKPoint(0, 0);
            var p1 = new SKPoint(200, 0);
            var p2 = new SKPoint(90, 0);
            var l = 10.0f;

            var path = new SKPath();

            //Logger.Info((float)Math.Sin(DegreeToRadian(30.0))* l);
            //Logger.Info((float)Math.Cos(DegreeToRadian(30.0))* l);

            path.MoveTo(0.0f, 0.0f);
            path.LineTo(200.0f, 0.0f);
            path.LineTo(200.0f - (float)Math.Cos(DegreeToRadian(15.0)) * l, +(float)Math.Sin(DegreeToRadian(15.0)) * l);
            path.MoveTo(200.0f, 0.0f);
            path.LineTo(200.0f - (float)Math.Cos(DegreeToRadian(15.0)) * l, -(float)Math.Sin(DegreeToRadian(15.0)) * l);

            // Create a paint for SKLine
            //var shader = SKShader.CreateLinearGradient(
            //    p0,
            //    p1,
            //    new[] { SKColors.Red, SKColors.DarkGreen },
            //    null,
            //    SKShaderTileMode.Clamp
            //    );
            var paint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.4f)),
                Style = SKPaintStyle.Stroke
                //Shader = shader
            };

            // Transformation Matrix
            var lRotMat = SKMatrix.MakeRotationDegrees(60.0f);
            var rRotMat = SKMatrix.MakeRotationDegrees(-60.0f);

            //var translationMat = SKMatrix.MakeTranslation(p1.X, p1.Y);
            var lMat = SKMatrix.MakeTranslation(p1.X, p1.Y);
            var rMat = SKMatrix.MakeTranslation(p1.X, p1.Y);
            SKMatrix.PostConcat(ref lMat, lRotMat);
            SKMatrix.PostConcat(ref rMat, rRotMat);

            // Draw ax axis line
            //this.Canvas.DrawLine(p0, p1, paint);
            //this.Canvas.DrawLine(p1, lMat.MapPoint(p2), paint);
            //this.Canvas.DrawLine(p1, rMat.MapPoint(p2), paint);
            this.Canvas.DrawPath(path, paint);
        }
        public override void DrawArea(object ctx) {


            // Before drawing, do the coordinates transformation.
            var matTra = SKMatrix.MakeIdentity();
            SKMatrix.Concat(ref matTra, SKMatrix.MakeScale(1, -1), SKMatrix.MakeTranslation(this.Margin, -(this.height - this.Margin)));
            //var p0 = matTra.MapPoint(new SKPoint(0, 200));
            //var p1 = matTra.MapPoint(new SKPoint(200, 200));
            var p0 = new SKPoint(0, 0);
            var p1 = new SKPoint(100, 0);

            var shader = SKShader.CreateLinearGradient(
                p0,
                p1,
                new[] { SKColors.Red, SKColors.DarkGreen },
                null,
                SKShaderTileMode.Clamp
                );
            var paint = new SKPaint {
                IsAntialias = true,
                Shader = shader
            };

            this.Canvas.Concat(ref matTra);
            this.Logger.Debug("Transformed points: p0 - ({0}), p1 - ({1})", p0, p1);
            this.DrawArrow(p0, p1);
            //this.Canvas.ResetMatrix();
            //this.DrawIdentityArrow();
            //this.Canvas.DrawLine(p0, p1, paint);
        }
        public override void DrawContent(object ctx)
        {
            //For test
            if (Points.Count > 0)
            {
                this.DrawPoints(this.Canvas, Points.ToArray());
            }
            //SKPoint[] points = 
            //{
            //    new SKPoint(100, 200),
            //    new SKPoint(200, 200)
            //};

            //this.DrawPoints(canvas, points);
        }

        protected void DrawPoints(SKCanvas canvas, SKPoint[] points)
        {
            if (points.Length > 0)
            {
                foreach(var p in points)
                {
                    var paint = new SKPaint
                    {
                        IsAntialias = true,
                        Color = SKColors.Blue,
                        Style = SKPaintStyle.Fill                        
                    };
                    canvas.DrawCircle(p.X, p.Y, 14 / 2, paint);
                }
            }
        }

        public void AddPoint(Point point)
        {
            this.Points.Add(new SKPoint(point.X, point.Y));
        }

        public List<Axes> Axes;
        public void DrawAxes(SKCanvas canvas)
        {
            if (this.Axes.Count > 0) {
                foreach(var axis in this.Axes) {
                    var paint = new SKPaint {
                        IsAntialias = true,
                        Color = SKColors.BlueViolet,
                        Style = SKPaintStyle.Stroke
                    };
                    //canvas.DrawLine();
                }

            }
        }


        #endregion
    }

    public enum AxesPostions
    {
        Top = 0,
        Bottom,
        Left,
        Right
    }

    public class Axes
    {
        public string Type;
        public AxesPostions Position;

        private int left;
        private int right;
        private int top;
        private int bottom;
        
        public Axes() {
            this.Type = "default";
            this.Position = AxesPostions.Top;

            //this.left
        }
    }
}
