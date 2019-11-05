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
            skControl1.Size = this.ClientSize;
            skControl1.PaintSurface += SkControl1_PaintSurface;
            skControl1.MouseMove += skControl1_MouseMove;
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

        private void skControl1_MouseMove(object sender, MouseEventArgs e) {
            if (this.chart.isInZone(e.Location)) {
                this.chart.Hovered = true;
                this.chart.PointerLocation = new SKPoint(e.Location.X, e.Location.Y);
                skControl1.Invalidate();
            }
            else {
                this.chart.Hovered = false;
                skControl1.Invalidate();
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
        public bool Hovered { get; set; } = false;
        public SKPoint PointerLocation { get; set; }

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
            if (this.Hovered) {
                this.DrawHover(this);
            }
        }

        public abstract void DrawArea(object ctx);
        public abstract void DrawContent(object ctx);
        public abstract void DrawHover(Chart chart);
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

        public bool isInZone(Point pointerLocation) {
            var pos = new SKPoint(pointerLocation.X, pointerLocation.Y);
            var ret = false;

            foreach(var e in this.Entries) {
                var dist = SKPoint.Distance(pos, e.GlobalCoordinate);
                if (dist <= 5) {
                    e.isHovered = true;
                    ret = true;
                }
                else {
                    e.isHovered = false;
                }
            }

            return ret;
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

        public override void DrawHover(Chart ctx) {
            //Hover.DrawHover(this.Canvas, ctx.PointerLocation);
        
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
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 1.0f)),
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



            // Draw axes
            this.Axes.Clear();
            this.Axes.Add(new Axis("X", this.chartArea.Width, this.Transform));
            this.Axes.Add(new Axis("Y", this.chartArea.Height, this.Transform));
            this.DrawAxes();

            // Apply a local transform
            this.Canvas.Concat(ref this.Transform);
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

    public class Hover {
        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public Hover() { }

        public static void DrawHover(SKCanvas canvas, SKPoint pointerLocation) {
            var anchor = pointerLocation;
            var size = new SKSize(100, 50);
            var offset = new SKPoint(10, -25);
            anchor += offset;
            var rect = new SKRect(anchor.X, anchor.Y, anchor.X + size.Width, anchor.Y + size.Height);
            var pathPaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.7)),
                Style = SKPaintStyle.Fill,
                PathEffect = SKPathEffect.CreateCorner(5)
            };
           
            //var strokePaint = new SKPaint {
            //    IsAntialias = true,
            //    Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.4)),
            //    Style = SKPaintStyle.Stroke,
            //    StrokeWidth = 2,
            //    PathEffect = SKPathEffect.CreateCorner(5)
            //};

            canvas.DrawRect(rect, pathPaint);

            var textPaint = new SKPaint {
                Color = SKColors.White
            };
            textPaint.MeasureText(pointerLocation.ToString(), ref rect);

            canvas.DrawText(pointerLocation.ToString(), anchor + new SKPoint(5, 25), textPaint);
            //canvas.DrawRect(rect, strokePaint);
        }
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
        public bool isHovered { get; set; } = false;

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
            float radius = 5;

            if (this.isHovered) {
                radius += 2;
                Hover.DrawHover(canvas, this.GlobalCoordinate);
            }

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

            // Draw entry shape
            canvas.DrawCircle(this.GlobalCoordinate, radius, fillPaint);
            canvas.DrawCircle(this.GlobalCoordinate, radius, strokePaint);
        }

    }

    public abstract class CanvasObject {
        public SKMatrix Transform { get; set; }
        public SKPoint Location { get; set; }
        public SKPoint GlobalLocation {
            get {
                return this.Transform.MapPoint(this.Location);
            }
        }

        public CanvasObject(SKPoint location, SKMatrix transform) {
            this.Location = location;
            this.Transform = transform;
        }

        public abstract void Draw(SKCanvas canvas);
    }

    public class Axis : CanvasObject
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
        //public SKMatrix Transform { get; set; }
        //public SKPoint Location { get; set; }
        //public SKPoint GlobalLocation {
        //    get {
        //        return this.Transform.MapPoint(this.Location);
        //    }
        //}
        
        public Axis(string label, float min, float max) : base(new SKPoint(0, 0), SKMatrix.MakeIdentity()) {
            this.MaxValue = max;
            this.MinValue = min;
            this.Label = label;

            var interval = (MaxValue - MinValue) / this.MaxTicksLimit;

            for (var i = 0; i < this.MaxTicksLimit; i ++) {
                var tickValue = this.MinValue + interval * i;
                Ticks.Add(new Tick(tickValue, 0, this.Transform));
            }
        }

        public Axis(string label, float length) : base(new SKPoint(0, 0), SKMatrix.MakeIdentity()) {
            this.Label = label;
            this.LengthInPixel = length;
            this.MaxValue = this.MaxTicksLimit * this.Scale;
            this.MinValue = 0;

            if (this.Label == "X") {
                for (var i = 0; i < this.MaxTicksLimit; i++) {
                    var tickValue = this.MinValue + this.Scale * i;
                    Ticks.Add(new Tick(tickValue, 0, this.Transform));
                }
            }
            else {
                for (var i = 0; i < this.MaxTicksLimit; i++) {
                    var tickValue = this.MinValue + this.Scale * i;
                    Ticks.Add(new Tick(0, tickValue, Tick.Directions.LEFT));
                }
            }

        }

        public Axis(string label, float length, SKMatrix transform) : base(new SKPoint(0, 0), transform) {
            this.Label = label;
            this.LengthInPixel = length;
            this.MaxValue = this.MaxTicksLimit * this.Scale;
            this.MinValue = 0;
            //this.Transform = transform;
            //this.Location = new SKPoint(0, 0);

            if (this.Label == "X") {
                for (var i = 0; i < this.MaxTicksLimit; i++) {
                    var tickValue = this.MinValue + this.Scale * i;
                    Ticks.Add(new Tick(tickValue, 0, this.Transform));
                }
            }
            else {
                for (var i = 0; i < this.MaxTicksLimit; i++) {
                    var tickValue = this.MinValue + this.Scale * i;
                    Ticks.Add(new Tick(0, tickValue, Tick.Directions.LEFT));
                }
            }
        }

        public void DrawAxis(SKCanvas canvas) {
            foreach(var t in this.Ticks) {
                //t.DrawTick(canvas);
                t.Draw(canvas);
            }
        }

        public override void Draw(SKCanvas canvas) {

        }

    }

    public class Tick : CanvasObject {
        public enum Directions {
            DOWN = 0,
            UP = 1,
            LEFT = 2,
            RIGHT = 3
        }
        public Label Label {
            get {
                Label label = new Label("", new SKPoint(0, 0), this.Transform);

                switch (this.Direction) {
                    case Directions.DOWN:
                        label = new Label(this.Location.X.ToString(), this.Location, this.Transform);
                        break;
                    case Directions.UP:
                        label = new Label(this.Location.X.ToString(), this.Location, this.Transform);
                        break;
                    case Directions.LEFT:
                        label = new Label(this.Location.Y.ToString(), this.Location, this.Transform);
                        break;
                    case Directions.RIGHT:
                        label = new Label(this.Location.Y.ToString(), this.Location, this.Transform);
                        break;
                }
                return label;
            }
        }
        public float Length { get; set; } = 10;
        //public string Direction { get; set; } = "DOWN";
        public Directions Direction { get; set; } = Directions.DOWN;
        private SKPoint OffsetOfDirection {
            get {
                SKPoint offset = new SKPoint(0, 0);

                switch(this.Direction) {
                    case Directions.DOWN:
                        offset = new SKPoint(0, -this.Length);
                        break;
                    case Directions.UP:
                        offset = new SKPoint(0, this.Length);
                        break;
                    case Directions.LEFT:
                        offset = new SKPoint(-this.Length, 0);
                        break;
                    case Directions.RIGHT:
                        offset = new SKPoint(this.Length, 0);
                        break;
                }
                return offset;
            }
        }
        //public SKPoint Location { get; set; }
        //public SKPoint GloabalLocation { get; set; }
        
        public Tick(float x, float y, SKMatrix transform) : base(new SKPoint(0, 0), transform) {
            this.Location = new SKPoint(x, y);
        }
        
        public Tick(float x, float y, Directions dir) : base(new SKPoint(0, 0), SKMatrix.MakeIdentity()) {
            this.Location = new SKPoint(x, y);
            this.Direction = dir;
        }

        public override void Draw(SKCanvas canvas) {
            var paint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.4))
            };
            var textPaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.6))
            };

            canvas.Save();
            canvas.ResetMatrix();

            // Draw shape
            canvas.DrawLine(this.GlobalLocation, this.Transform.MapPoint(this.Location + this.OffsetOfDirection), paint);

            canvas.Restore();
        }
        public void DrawTick(SKCanvas canvas) {
            //SKPoint dir = new SKPoint(0, 0);

            //switch (this.Direction) {
            //    case Directions:
            //        dir -= new SKPoint(0, this.Length);
            //        break;
            //    case "LEFT":
            //        dir -= new SKPoint(this.Length, 0);
            //        break;
            //    default:
            //        break;
            //}

            var paint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.4))
            };
            var textPaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.6))
            };

            // Draw shape
            canvas.DrawLine(this.Location, this.Location + this.OffsetOfDirection, paint);

            // Draw label
            this.Label.DrawLabel(canvas);
            //var originalMat = canvas.TotalMatrix;
            //var mat = SKMatrix.MakeScale(1, -1);
            //canvas.Concat(ref mat);
            //canvas.DrawText((this.Location + dir).X.ToString(), this.Location + dir, textPaint);
            //canvas.ResetMatrix();
            //canvas.Concat(ref originalMat);
        }
    }

    public class Label : CanvasObject {
        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public SKPoint Anchor { get; set; }
        public string Name { get; set; }

        //public Label() { }
        public Label(string name, SKPoint location, SKMatrix transform) : base(location, transform) {
            this.Name = name;
        }

        public override void Draw(SKCanvas canvas) {
            throw new NotImplementedException();
        }
        public void DrawLabel(SKCanvas canvas) {
            var paint = new SKPaint {
                IsAntialias = true,
                Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Black, 0.8f),
                TextSize = 14,
                TextAlign = SKTextAlign.Center
            };
            var textWidth = paint.MeasureText(this.Name);
            var offset = new SKPoint(0, paint.TextSize / 2);
            var totalMatrix = canvas.TotalMatrix;

            //var path = new SKPath();
            //var scale = SKMatrix.MakeScale(1, -1);
            //path.MoveTo(this.Anchor);
            //path.RLineTo(new SKPoint(textWidth, 0));
            //path.Transform(scale);
            //canvas.DrawTextOnPath(this.Name, path, new SKPoint(0, 0), paint);

            canvas.Save();
            canvas.ResetMatrix();

            canvas.DrawText(this.Name, this.Transform.MapPoint(this.Location + offset), paint);
            //canvas.DrawText(this.Name, this.GlobalLocation, paint);

            canvas.Restore();



            //canvas.ResetMatrix();
            //canvas.Save();
            //canvas.ResetMatrix();
            //var translate = SKMatrix.MakeTranslation();
            //var scale = SKMatrix.MakeScale(1, -1);
            //path.Transform(scale);

            //canvas.Concat(ref scale);
            //canvas.DrawTextOnPath(this.Name, path, this.Anchor + offset, paint);
            //canvas.DrawText(this.Name, this.Anchor + offset, paint);
            //canvas.Restore();
            //canvas.Concat(ref totalMatrix);
        }

    }

    public class SkiaHelper {
        public static SKPoint GenerateZeroPoint() {
            return new SKPoint(0, 0);
        }
        public static SKColor ConvertColorWithAlpha(SKColor baseColor, float alpha) {
            return baseColor.WithAlpha((byte)(0xFF * alpha));
        }

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
