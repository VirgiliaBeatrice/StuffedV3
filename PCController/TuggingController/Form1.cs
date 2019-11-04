﻿using System;
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
            skControl1.Size = this.ClientSize;
            skControl1.PaintSurface += SkControl1_PaintSurface;
            this.SizeChanged += Form1_SizeChanged;
            this.chart = new PointChart();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            skControl1.Size = this.ClientSize;
        }

        private void SkControl1_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            this.chart.Draw(e.Surface.Canvas, this.ClientSize.Width, this.ClientSize.Height);
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
        public float Margin { get; set; } = 80;
        public float LabelTextSize { get; set; } = 16;
        public SKRect chartArea;
        protected SKRect frameArea;

        public SKCanvas Canvas;
        protected float width;
        protected float height;
        public SKMatrix Transform;
        protected SKMatrix InverseTransform;

        protected readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Methods
        public void Draw(SKCanvas canvas, int width, int height)
        {
            this.Canvas = canvas;
            this.width = width;
            this.height = height;

            chartArea = new SKRect {
                Size = new SKSize(this.width - this.Margin * 2, this.height - this.Margin * 2),
                Location = new SKPoint(this.Margin, this.Margin)
            };

            frameArea = new SKRect {
                Size = new SKSize(this.width, this.height)
            };

            this.Canvas.Clear(SKColor.Empty);
            this.Canvas.ResetMatrix();

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
        //public List<SKPoint> Points = new List<SKPoint>();
        public List<Entry> Entries = new List<Entry>();
        public List<Axis> Axes = new List<Axis>();
        #endregion

        #region Methods

        public PointChart() {
            //this.Entries = new List<Entry>();

            //this.SetDefault();
        }
        public PointChart(Entry[] entries) {
            this.Entries = new List<Entry>(entries);

            this.Axes.Add(new Axis("X", this.GetMinValueInEntries("X"), this.GetMaxValueInEntries("X")));
            this.Axes.Add(new Axis("Y", this.GetMinValueInEntries("Y"), this.GetMaxValueInEntries("Y")));
        }
        private double DegreeToRadian(double degree) {
            return Math.PI * degree / 180.0;
        }

        private void SetDefault() {
            this.Axes.Add(new Axis("X", 200));
        }

        private float GetMaxValueInEntries(string axis) {
            if (axis == "X") {
                return this.Entries.Max(e => e.LocalCoordinate.X);
            }
            else {
                return this.Entries.Max(e => e.LocalCoordinate.Y);
            }
        }

        private float GetMinValueInEntries(string axis) {
            if (axis == "X") {
                return this.Entries.Min(e => e.LocalCoordinate.X);
            }
            else {
                return this.Entries.Min(e => e.LocalCoordinate.Y);
            }
        }

        //private enum ArrowDirections {
        //    Top = 90,
        //    Bottom = -90,
        //    Left = 180,
        //    Right = 0
        //}
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

        public void SetLocalTransform() {
            SKMatrix mat = SKMatrix.MakeScale(1, -1);
            //SKMatrix.PreConcat(ref mat, );
            SKMatrix.PostConcat(ref mat, SKMatrix.MakeTranslation(this.Margin, (this.chartArea.Height + this.Margin)));
            this.Transform = mat;
            mat.TryInvert(out this.InverseTransform);
        }
        public override void DrawArea(object ctx) {

            // Before drawing, do the coordinates transformation.
            this.SetLocalTransform();
            //SKMatrix mat = SKMatrix.MakeIdentity();
            //SKMatrix.Concat(ref mat, SKMatrix.MakeScale(1, -1), SKMatrix.MakeTranslation(this.Margin, -this.chartArea.Height - this.Margin));

            //SKMatrix matTest = SKMatrix.MakeIdentity();
            //SKMatrix.PreConcat(ref matTest, SKMatrix.MakeScale(1, -1));
            //SKMatrix.PreConcat(ref matTest, SKMatrix.MakeTranslation(this.Margin, -this.chartArea.Height - this.Margin));

            //var p0 = new SKPoint(0, 0);
            //var p1 = new SKPoint(200, 50);

            var origin = new SKPoint(0, 0);
            var yMax = new SKPoint(0, this.chartArea.Height);
            var xMax = new SKPoint(this.chartArea.Width, 0);

            //var shader = SKShader.CreateLinearGradient(
            //    p0,
            //    p1,
            //    new[] { SKColors.Red, SKColors.DarkGreen },
            //    null,
            //    SKShaderTileMode.Clamp
            //    );
            //var paint = new SKPaint {
            //    IsAntialias = true,
            //    Shader = shader
            //};
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

            // For Debug: Draw frame region and debug info
            //this.DrawFrame();
            //this.Canvas.DrawText(string.Format("{0} {1}", this.chartArea.Width, this.chartArea.Height), new SKPoint(0, this.Margin), textPaint);


            // Apply a local transform
            this.Canvas.Concat(ref this.Transform);

            // Draw axes
            this.Axes.Add(new Axis("X", this.chartArea.Width));
            this.Axes.Add(new Axis("Y", this.chartArea.Height));
            this.DrawAxes();

            //this.Canvas.DrawCircle(origin, 4, originPaint);
            this.DrawArrow(origin, xMax);
            this.DrawArrow(origin, yMax);

            // Reset local transform
            this.Canvas.ResetMatrix();
        }

        private void DrawAxes() {
            foreach(var a in this.Axes) {
                a.DrawAxis(this.Canvas);
            }
        }

        private void TranformTest() {
            List<SKPoint> locals = new List<SKPoint> {
                new SKPoint(0, 0),
                new SKPoint(100, 0),
                new SKPoint(0, 100)
            };

            List<SKPoint> globals = new List<SKPoint>();

            foreach(var local in locals) {
                globals.Add(this.Transform.MapPoint(local));
            }

            Logger.Debug("--------------");
            Logger.Debug("Target Coordinate: {0}", locals[0]);
            Logger.Debug("Transformed Coordinate: {0}", globals[0]);

            Logger.Debug("Target Coordinate: {0}", locals[1]);
            Logger.Debug("Transformed Coordinate: {0}", globals[1]);

            Logger.Debug("Target Coordinate: {0}", locals[2]);
            Logger.Debug("Transformed Coordinate: {0}", globals[2]);

            locals.Clear();
            foreach(var g in globals) {
                locals.Add(this.InverseTransform.MapPoint(g));
            }

            Logger.Debug("--------------");
            Logger.Debug("Target Coordinate: {0}", locals[0]);
            Logger.Debug("Transformed Coordinate: {0}", globals[0]);

            Logger.Debug("Target Coordinate: {0}", locals[1]);
            Logger.Debug("Transformed Coordinate: {0}", globals[1]);

            Logger.Debug("Target Coordinate: {0}", locals[2]);
            Logger.Debug("Transformed Coordinate: {0}", globals[2]);

        }
        public override void DrawContent(object ctx)
        {
            if (this.Entries.Count > 0) {
                foreach(var e in this.Entries) {
                    e.DrawEntry(this.Canvas);
                }
            }
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
            //this.Points.Add(new SKPoint(point.X, point.Y));
            this.Entries.Add(new Entry(point.X, point.Y, this.Transform, false));
        }

        //public void DrawAxes(SKCanvas canvas)
        //{
        //    if (this.Axes.Count > 0) {
        //        foreach(var axis in this.Axes) {
        //            var paint = new SKPaint {
        //                IsAntialias = true,
        //                Color = SKColors.BlueViolet,
        //                Style = SKPaintStyle.Stroke
        //            };
        //            //canvas.DrawLine();
        //        }

        //    }
        //}

        private void DrawFrame() {
            var paint = new SKPaint {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Purple,
                StrokeCap = SKStrokeCap.Round,
                StrokeWidth = 2
            };


            Logger.Debug("Window Size: {0}, {1}", this.frameArea.Width, this.frameArea.Height);
            Logger.Debug("Frame Size: {0} {1}", this.chartArea.Width, this.chartArea.Height);

            this.Canvas.DrawRect(this.frameArea, paint);
            paint.Color = SKColors.Blue;
            this.Canvas.DrawRect(this.chartArea, paint);
        }

        #endregion
    }

    public class Entry {
        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public SKPoint LocalCoordinate { get; set; }
        public SKPoint GlobalCoordinate {
            get {
                return this.Transform.MapPoint(this.LocalCoordinate);
            }
            set { }
        }
        public SKMatrix Transform { get; set; } = SKMatrix.MakeIdentity();
        private SKMatrix InverseTransform { get; set; }

        public Entry() {
            this.LocalCoordinate = new SKPoint(0, 0);
        }

        public Entry(float x, float y) {
            this.LocalCoordinate = new SKPoint(x, y);
        }

        public Entry(float x, float y, SKMatrix transform, bool isLocal) {
            SKMatrix inverse;
            transform.TryInvert(out inverse);

            this.InverseTransform = inverse;
            this.Transform = transform;

            if (isLocal) {
                this.LocalCoordinate = new SKPoint(x, y);
            } else {
                this.LocalCoordinate = this.InverseTransform.MapPoint(new SKPoint(x, y));
            }

            Logger.Debug("Global Coordinate: {0}", this.GlobalCoordinate);
            Logger.Debug("Local Coordinate: {0}", this.LocalCoordinate);
        }

        public void DrawEntry(SKCanvas canvas) {
            var fillPaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.ForestGreen,
                Style = SKPaintStyle.Fill
            };
            var strokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            canvas.DrawCircle(this.GlobalCoordinate, 14 / 2, fillPaint);
            canvas.DrawCircle(this.GlobalCoordinate, 14 / 2, strokePaint);
        }

    }

    public class Axis
    {
        public string Label { get; set; } = "X";
        public List<Tick> Ticks { get; set; } = new List<Tick>();
        public int MaxTicksLimit { get; set; } = 11;
        public float MaxValue { get; set; }
        public float MinValue { get; set; }
        public float LengthInPixel { get; set; }
        private float Scale {
            get {
                return this.LengthInPixel / this.MaxTicksLimit;
            }
        }
        
        public Axis(string label, float min, float max) {
            this.MaxValue = max;
            this.MinValue = min;
            this.Label = label;

            var interval = (MaxValue - MinValue) / this.MaxTicksLimit;

            for (var i = 0; i < this.MaxTicksLimit; i ++) {
                var tickValue = this.MinValue + interval * i;
                Ticks.Add(new Tick(tickValue, 0));
            }
        }

        public Axis(string label, float length) {
            this.Label = label;
            this.LengthInPixel = length;
            this.MaxValue = this.MaxTicksLimit * this.Scale;
            this.MinValue = 0;

            if (this.Label == "X") {
                for (var i = 0; i < this.MaxTicksLimit; i++) {
                    var tickValue = this.MinValue + this.Scale * i;
                    Ticks.Add(new Tick(tickValue, 0));
                }
            }
            else {
                for (var i = 0; i < this.MaxTicksLimit; i++) {
                    var tickValue = this.MinValue + this.Scale * i;
                    Ticks.Add(new Tick(0, tickValue, "LEFT"));
                }
            }

        }

        public void DrawAxis(SKCanvas canvas) {
            foreach(var t in this.Ticks) {
                t.DrawTick(canvas);
            }
        }

    }

    public class Tick {
        public string Label { get; set; }
        public float Length { get; set; } = 10;
        public string Direction { get; set; } = "DOWN";
        public SKPoint Location { get; set; }
        
        public Tick(float x, float y) {
            this.Location = new SKPoint(x, y);
        }
        
        public Tick(float x, float y, string dir) {
            this.Location = new SKPoint(x, y);
            this.Direction = dir;
        }

        public void DrawTick(SKCanvas canvas) {
            SKPoint dir = new SKPoint(0, 0);

            switch (this.Direction) {
                case "DOWN":
                    dir -= new SKPoint(0, this.Length);
                    break;
                case "LEFT":
                    dir -= new SKPoint(this.Length, 0);
                    break;
                default:
                    break;
            }

            var paint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.4))
            };

            canvas.DrawLine(this.Location, this.Location + dir, paint);
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
