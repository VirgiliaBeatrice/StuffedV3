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

        private double DegreeToRadian(double degree) {
            return Math.PI * degree / 180.0;
        }
        private enum ArrowDirections {
            Top = 90,
            Bottom = -90,
            Left = 180,
            Right = 0
        }
        private void DrawArrow(SKPoint start, SKPoint end) {
            var dirVector = end - start;
            var dirAngle = Math.Atan2(dirVector.Y, dirVector.X);
            var rotMat = SKMatrix.MakeRotation((float)dirAngle, end.X, end.Y);
            //Logger.Debug("{0} {1}", dir.Y, dir.X);
            //SKMatrix.PostConcat(ref mat, rotMat);

            var linePath = new SKPath();
            var arrowPath = new SKPath();
            //var s = new SKPoint(0, 0);
            //var e = new SKPoint(100, 0);
            double arrowSize = 16.0;
            double arrowAngle = 15.0;
            double d = Math.Tan(DegreeToRadian(arrowAngle)) * arrowSize;

            var lArrow = SKPoint.Add(end, new SKPoint(-(float)arrowSize, +(float)d));
            var rArrow = SKPoint.Add(end, new SKPoint(-(float)arrowSize, -(float)d));

            var paint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.4f)),
                Style = SKPaintStyle.Stroke
                //Shader = shader
            };

            // Line Path
            linePath.MoveTo(start);
            linePath.LineTo(end);
            this.Canvas.DrawPath(linePath, paint);

            // Arrow Path
            arrowPath.MoveTo(end);
            arrowPath.LineTo(lArrow);
            arrowPath.MoveTo(end);
            arrowPath.LineTo(rArrow);
            arrowPath.Transform(rotMat);
            this.Canvas.DrawPath(arrowPath, paint);

            //this.Canvas.Concat(ref rotMat);
        }
        public override void DrawArea(object ctx) {
            // Before drawing, do the coordinates transformation.
            var matTra = SKMatrix.MakeIdentity();
            SKMatrix.Concat(ref matTra, SKMatrix.MakeScale(1, -1), SKMatrix.MakeTranslation(this.Margin, -(this.height - this.Margin)));
            //var p0 = matTra.MapPoint(new SKPoint(0, 200));
            //var p1 = matTra.MapPoint(new SKPoint(200, 200));
            var p0 = new SKPoint(0, 0);
            var p1 = new SKPoint(200, 50);

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
            var originPaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Red,
                Style = SKPaintStyle.Fill
            };
            var textPaint = new SKPaint {
                IsAntialias = true,
                TextSize = 20.0f,
                Color = SKColors.Black,
                IsStroke = false
            };

            this.Canvas.Concat(ref matTra);
            this.Logger.Debug("Transformed points: p0 - ({0}), p1 - ({1})", p0, p1);
            this.Canvas.DrawCircle(new SKPoint(0, 0), 4, originPaint);
            this.DrawArrow(p0, p1);
            this.Canvas.ResetMatrix();
            this.Canvas.DrawText(string.Format("{0} {1}", p0, p1), new SKPoint(0, 20), textPaint);
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

    public class SkiaHelper {

        public static double DegreeToRadian(double degree) {
            return Math.PI * degree / 180.0;
        }
        public static void DrawArrow(SKCanvas canvas, SKPoint start, SKPoint end) {
            SKPoint dirVector = end - start;
            double dirAngle = Math.Atan2(dirVector.Y, dirVector.X);
            SKMatrix rotMat = SKMatrix.MakeRotation((float)dirAngle, end.X, end.Y);

            double arrowSize = 16.0;
            double arrowAngle = 15.0;
            double d = Math.Tan(DegreeToRadian(arrowAngle)) * arrowSize;

            SKPoint lArrow = SKPoint.Add(end, new SKPoint(-(float)arrowSize, +(float)d));
            SKPoint rArrow = SKPoint.Add(end, new SKPoint(-(float)arrowSize, -(float)d));

            // Path Paint
            SKPaint paint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.4f)),
                Style = SKPaintStyle.Stroke
            };

            // Line Path
            SKPath linePath = new SKPath();
            linePath.MoveTo(start);
            linePath.LineTo(end);
            canvas.DrawPath(linePath, paint);

            // Arrow Path
            SKPath arrowPath = new SKPath();
            arrowPath.MoveTo(end);
            arrowPath.LineTo(lArrow);
            arrowPath.MoveTo(end);
            arrowPath.LineTo(rArrow);
            arrowPath.Transform(rotMat);
            canvas.DrawPath(arrowPath, paint);
        }
    }
}
