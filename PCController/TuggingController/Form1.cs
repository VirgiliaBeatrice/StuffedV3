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

        private bool IsDragging { get; set; } = false;
        private Point StartLocationOnDrag { get; set; }
        private Point CurrentLocationOnDrag { get; set; }
        private Entry DragTarget;
        public Form1()
        {
            InitializeComponent();

            var config = new NLog.Config.LoggingConfiguration();
            var logConsole = new NLog.Targets.ColoredConsoleTarget("Form1");
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            NLog.LogManager.Configuration = config;

            Logger.Debug("Hello World");
            //RobotController ctrl = new RobotController();
            var tri = new Triangulation();
            tri.OnDataReceived += Triangle_DataReceived;
            tri.StartTask();

            skControl1.Location = new Point(0, 0);
            skControl1.Size = this.ClientSize;
            skControl1.PaintSurface += SkControl1_PaintSurface;
            skControl1.MouseMove += skControl1_MouseMove;
            skControl1.MouseDown += skControl1_MouseDown;
            skControl1.MouseUp += skControl1_MouseUp;
            skControl1.MouseDoubleClick += skControl1_MouseClick;
            this.SizeChanged += Form1_SizeChanged;
            this.chart = new PointChart();
        }

        private void Triangle_DataReceived(string data) {
            string[] lines = data.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for(int idx = 0; idx < lines.Length; idx ++) {
                if (idx > 1) {
                    var coordinates = lines[idx].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(e => Convert.ToSingle(e)).ToArray();
                    this.chart.AddPointFromValue(coordinates[0], coordinates[1]);
                }
            }

            Logger.Debug("MinValue: {0}", this.chart.MinValue);
            Logger.Debug("MaxValue: {0}", this.chart.MaxValue);
            //Console.WriteLine(this.chart.PrintEntries());
            skControl1.Invalidate();
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

        private void skControl1_MouseDown(object sender, MouseEventArgs e) {
            switch (e.Button) {
                case MouseButtons.Left:
                    //this.IsDragging = true;
                    this.StartLocationOnDrag = e.Location;
                    this.CurrentLocationOnDrag = e.Location;
                    break;
            }

            Logger.Debug("Detected a mousedown event.");

        }

        private void skControl1_MouseUp(object sender, MouseEventArgs e) {
            switch (e.Button) {
                case MouseButtons.Left:
                    //this.IsDragging = false;
                    this.StartLocationOnDrag = new Point();
                    this.CurrentLocationOnDrag = new Point();
                    Console.WriteLine(this.chart.PrintEntries());

                    break;
            }

            Logger.Debug("Detected a mouseup event.");



        }
        private void skControl1_MouseClick(object sender, MouseEventArgs e)
        {
            // Add new point

            switch (e.Button)
            {
                case MouseButtons.Left:
                    Logger.Debug("Add new point");
                    Logger.Debug(e.Location);
                    this.chart.AddPointFromGlobal(e.Location);
                    skControl1.Invalidate();
                    break;
                default:
                    break;
            }

        }

        private void skControl1_MouseMove(object sender, MouseEventArgs e) {
            this.chart.PointerLocation = new SKPoint(e.Location.X, e.Location.Y);
            
            if (e.Button == MouseButtons.None) {
                this.IsDragging = false;
                if (this.chart.isInZone(e.Location, 5, out _)) {
                    this.chart.Hovered = true;
                    skControl1.Invalidate();
                }
                else {
                    this.chart.Hovered = false;
                }

                if (this.chart.isInArea(e.Location)) {
                    this.chart.hasIndicator = true;
                    skControl1.Invalidate();
                }
                else {
                    this.chart.hasIndicator = false;
                    skControl1.Invalidate();
                }
            }
            else {
                if (this.IsDragging) {
                    this.CurrentLocationOnDrag = e.Location;
                    //var move = this.CurrentLocationOnDrag - this.StartLocationOnDrag;
                    //var targetLocation = new SKPoint(e.Location.X - this.StartLocationOnDrag.X, e.Location.Y - this.StartLocationOnDrag.Y);
                    var targetLoc = new SKPoint(e.Location.X, e.Location.Y);
                    this.DragTarget.UpdateFromGlobalLocation(targetLoc);

                    Logger.Debug("Dragging.");
                    skControl1.Invalidate();
                }
                else {
                    if (this.chart.isInZone(e.Location, 10, out this.DragTarget)) {
                        this.CurrentLocationOnDrag = e.Location;
                        this.IsDragging = true;
                        //var move = this.CurrentLocationOnDrag - this.StartLocationOnDrag;
                        //var targetLocation = new SKPoint(e.Location.X - this.StartLocationOnDrag.X, e.Location.Y - this.StartLocationOnDrag.Y);
                        var targetLoc = new SKPoint(e.Location.X, e.Location.Y); 
                        this.DragTarget.UpdateFromGlobalLocation(targetLoc);

                        Logger.Debug("Dragging.");
                        skControl1.Invalidate();
                    }
                }

            }

            //if (IsMouseReleased) {
            //    if (this.chart.isInZone(e.Location) != -1) {
            //        this.chart.Hovered = true;
            //        skControl1.Invalidate();
            //    }
            //    else {
            //        this.chart.Hovered = false;
            //    }

            //    if (this.chart.isInArea(e.Location)) {
            //        this.chart.hasIndicator = true;
            //        skControl1.Invalidate();
            //    }
            //    else {
            //        this.chart.hasIndicator = false;
            //        skControl1.Invalidate();
            //    }
            //}
            //else {
            //    if (this.chart.isInZone(e.Location) != -1) {

            //    }
            //}
        }
    }

    public abstract class Chart
    {
        #region Properties
        public List<Entry> Entries { get; set; } = new List<Entry>();
        public SKPoint MaxValue {
            get {
                return new SKPoint(this.Entries.Max(e => e.Value.X), this.Entries.Max(e => e.Value.Y));
            }
        }
        public SKPoint MinValue {
            get {
                return new SKPoint(this.Entries.Min(e => e.Value.X), this.Entries.Min(e => e.Value.Y));
            }
        }
        public List<Axis> Axes { get; set; } = new List<Axis>();
        public float Margin { get; set; } = 80;
        public float LabelTextSize { get; set; } = 16;
        public SKRect chartArea;
        protected SKRect frameArea;
        public bool Hovered { get; set; } = false;
        public bool hasIndicator { get; set; } = false;
        public SKPoint PointerLocation { get; set; }

        public SKCanvas Canvas;
        protected float width;
        protected float height;
        public SKMatrix Transform;
        public SKMatrix InverseTransform;
        public SKMatrix Scale { get; set; }

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

            // Calculate the transform of this chart
            var translate = SKMatrix.MakeTranslation(-this.MinValue.X, -this.MinValue.Y);
            SKMatrix.PostConcat(ref translate, SKMatrix.MakeScale(this.chartArea.Width / (this.MaxValue.X - this.MinValue.X), this.chartArea.Height / (this.MaxValue.Y - this.MinValue.Y)));
            this.Scale = translate;
            //this.Scale = SKMatrix.MakeScale(this.chartArea.Width / 10, this.chartArea.Height / 10);

            this.SetTransform();
            // Update data
            this.Entries.ForEach(e => e.UpdateTransform(this.Transform));
            this.Entries.ForEach(e => e.UpdateScale(this.Scale));


            // Draw canvas
            this.Canvas.Clear(SKColor.Empty);
            this.Canvas.ResetMatrix();

            this.DrawArea(this);
            this.DrawContent(this);

            if (this.Hovered) {
                this.DrawHover(this);
            }

            if (this.hasIndicator) {
                this.DrawIndicator();
            }
        }

        public abstract void DrawIndicator();
        public abstract void DrawArea(object ctx);
        public abstract void DrawContent(object ctx);
        public abstract void DrawHover(Chart chart);

        public abstract void SetTransform();

        public virtual string PrintEntries() {
            string ret = String.Format("2\r\n{0}\r\n", this.Entries.Count);
            foreach (var e in this.Entries) {
                ret += e.Value.X + " " + e.Value.Y + "\r\n";
            }

            return ret;
        }
        #endregion
    }

    public class PointChart : Chart
    {
        #region Properties

        public float PointSize { get; set; } = 14;
        //public List<SKPoint> Points = new List<SKPoint>();
        //public List<Entry> Entries = new List<Entry>();
        //public List<Axis> Axes = new List<Axis>();
        #endregion

        #region Methods

        public PointChart() {
            //this.Entries = new List<Entry>();

            //this.SetDefault();
        }
        public PointChart(Entry[] entries) {
            this.Entries = new List<Entry>(entries);

            //this.Axes.Add(new Axis("X", this.GetMinValueInEntries("X"), this.GetMaxValueInEntries("X")));
            //this.Axes.Add(new Axis("Y", this.GetMinValueInEntries("Y"), this.GetMaxValueInEntries("Y")));
        }
        private double DegreeToRadian(double degree) {
            return Math.PI * degree / 180.0;
        }

        private void SetDefault() {
            this.Axes.Add(new Axis("X", 200));
        }

        public bool isInZone(Point pointerLocation, float radius, out Entry target) {
            var pos = new SKPoint(pointerLocation.X, pointerLocation.Y);
            var ret = false;

            target = null;
            foreach(var (e, i) in this.Entries.Select((e, i) => (e, i))) {
                var dist = SKPoint.Distance(pos, e.GlobalLocation);
                if (dist <= radius) {
                    e.isHovered = true;
                    ret = true;
                    target = e;
                }
                else {
                    e.isHovered = false;
                }
            }

            return ret;
        }

        public bool isInArea(Point globalLocation) {
            SKPoint location = this.InverseTransform.MapPoint(new SKPoint(globalLocation.X, globalLocation.Y));
            return (location.X <= this.chartArea.Width & location.X >= 0) && (location.Y <= this.chartArea.Height & location.Y >= 0);
        }

        private float GetMaxValueInEntries(string axis) {
            if (axis == "X") {
                return this.Entries.Max(e => e.Location.X);
            }
            else {
                return this.Entries.Max(e => e.Location.Y);
            }
        }

        private float GetMinValueInEntries(string axis) {
            if (axis == "X") {
                return this.Entries.Min(e => e.Location.X);
            }
            else {
                return this.Entries.Min(e => e.Location.Y);
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
            foreach(var e in this.Entries) {
                e.DrawHover(this.Canvas);
            }
        }

        public override void DrawIndicator() {
            this.DrawCross(this.Canvas, this.chartArea, this.InverseTransform.MapPoint(this.PointerLocation), this.Transform);
        }

        private void DrawCross(SKCanvas canvas, SKRect bound, SKPoint location, SKMatrix transform) {
            var horizonLineStart = new SKPoint(0, location.Y);
            var horizonLineEnd = new SKPoint(bound.Width, location.Y);
            var verticalLineStart = new SKPoint(location.X, 0);
            var verticalLineEnd = new SKPoint(location.X, bound.Height);

            var dashArray = new float[] { 0, 20 };
            var paint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.8f)),
                Style = SKPaintStyle.Stroke
                //PathEffect = SKPathEffect.CreateDash(dashArray, 10)
            };

            var path = new SKPath();
            path.MoveTo(horizonLineStart);
            path.LineTo(horizonLineEnd);
            path.MoveTo(verticalLineStart);
            path.LineTo(verticalLineEnd);

            path.Transform(transform);

            canvas.DrawPath(path, paint);
        }
        private void DrawArrow(SKPoint start, SKPoint end, SKMatrix transform) {
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
                Color = SKColors.Black.WithAlpha((byte)(0xFF * 0.8f)),
                Style = SKPaintStyle.Stroke
                //Shader = shader
            };

            // Line Path
            linePath.MoveTo(start);
            linePath.LineTo(end);
            linePath.Transform(transform);
            this.Canvas.DrawPath(linePath, paint);

            // Arrow Path
            arrowPath.MoveTo(end);
            arrowPath.LineTo(lArrow);
            arrowPath.MoveTo(end);
            arrowPath.LineTo(rArrow);

            //var mat = rotMat;
            //SKMatrix.PreConcat(ref mat, transform);

            arrowPath.Transform(rotMat);
            arrowPath.Transform(transform);
            this.Canvas.DrawPath(arrowPath, paint);

            //this.Canvas.Concat(ref rotMat);
        }

        public override void SetTransform() {
            SKMatrix mat = SKMatrix.MakeScale(1, -1);
            SKMatrix.PostConcat(ref mat, SKMatrix.MakeTranslation(this.Margin, (this.chartArea.Height + this.Margin)));
            //SKMatrix.PreConcat(ref mat, );
            //SKMatrix.PostConcat(ref mat, SKMatrix.MakeTranslation(this.Margin, (this.chartArea.Height + this.Margin)));
            this.Transform = mat;
            mat.TryInvert(out this.InverseTransform);
        }
        public override void DrawArea(object ctx) {

            // Before drawing, do the coordinates transformation.
            //this.SetTransform();
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


            this.Canvas.Save();
            this.Canvas.ResetMatrix();

            // Draw axes
            this.Axes.Clear();

            if (this.Entries.Count == 0) {
                this.Axes.Add(new Axis("X", this.chartArea.Width, this.Transform));
                this.Axes.Add(new Axis("Y", this.chartArea.Height, this.Transform));
            }
            else {
                this.Axes.Add(new Axis("X", this.MinValue.X, this.MaxValue.X, this.chartArea.Width, this.Transform));
                this.Axes.Add(new Axis("Y", this.MinValue.Y, this.MaxValue.Y, this.chartArea.Height, this.Transform));
            }

            this.DrawAxes();

            // Draw arrow
            this.DrawArrow(origin, xMax, this.Transform);
            this.DrawArrow(origin, yMax, this.Transform);

            this.Canvas.Restore();



            // Apply a local transform
            //this.Canvas.Concat(ref this.Transform);
            //this.Canvas.DrawCircle(origin, 4, originPaint);
            //this.DrawArrow(origin, xMax);
            //this.DrawArrow(origin, yMax);

            // Reset local transform
            //this.Canvas.ResetMatrix();
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
                    e.Draw(this.Canvas);
                }
            }
        }

        //protected void DrawPoints(SKCanvas canvas, SKPoint[] points)
        //{
        //    if (points.Length > 0)
        //    {
        //        foreach(var p in points)
        //        {
        //            var paint = new SKPaint
        //            {
        //                IsAntialias = true,
        //                Color = SKColors.Blue,
        //                Style = SKPaintStyle.Fill                        
        //            };
        //            canvas.DrawCircle(p.X, p.Y, 14 / 2, paint);
        //        }
        //    }
        //}

        public void ClearPoints() {
            this.Entries.Clear();
        }
        public void AddPointFromGlobal(Point point)
        {
            //this.Points.Add(new SKPoint(point.X, point.Y));
            //this.Entries.Add(new Entry(this.InverseTransform.MapPoint(new SKPoint(point.X, point.Y)), this.Transform));
            SKMatrix inverse;
            this.Scale.TryInvert(out inverse);

            var tPoint = new SKPoint(point.X, point.Y);
            tPoint = this.InverseTransform.MapPoint(tPoint);
            tPoint = inverse.MapPoint(tPoint);
            this.Entries.Add(new Entry(tPoint, this.Scale, this.InverseTransform));
            //this.Entries.Add(new Entry(tPoint, inverse, this.InverseTransform));
        }

        public void AddPointFromValue(float x, float y) {
            var tPoint = new SKPoint(x, y);
            this.Entries.Add(new Entry(tPoint, this.Scale, this.InverseTransform));
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

        public static void DrawHover(SKCanvas canvas, SKPoint value, SKPoint location, SKMatrix transform) {
            var anchor = transform.MapPoint(location);
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
            textPaint.MeasureText(value.ToString(), ref rect);

            canvas.DrawText(value.ToString(), anchor + new SKPoint(5, 25), textPaint);
            //canvas.DrawRect(rect, strokePaint);
        }
    }


    public class Entry : CanvasObject {
        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public bool isHovered { get; set; } = false;
        public SKPoint Value { get; set; }
        public override SKPoint Location {
            get {
                //Logger.Debug("GetMethod - Location: {0}", this.Scale.MapPoint(this.Value));
                return this.Scale.MapPoint(this.Value);
            }
        }
        public SKMatrix Scale { get; set; }

        public Entry() : this(0, 0, SKMatrix.MakeIdentity()) { }

        public Entry(float x, float y, SKMatrix transform) : this(new SKPoint(x, y), SKMatrix.MakeIdentity(), transform) { }

        //public Entry(SKPoint value, SKPoint location, SKMatrix transform) : base(location, transform) {
        //    //this.Value = value;
        //    Logger.Debug("Create new entry - [Location]: {0}", this.Location);
        //    Logger.Debug("Create new entry - [Value]: {0}", this.Value);
        //}

        public Entry(SKPoint value, SKMatrix scale, SKMatrix transform) : base(transform) {
            this.Value = value;
            this.Scale = scale;
            //this.Value = scale.MapPoint(location);
            Logger.Debug("Create new entry - [Value]: {0}", this.Value);
            Logger.Debug("Create new entry - [Location]: {0}", this.Location);
        }


        //public Entry(float x, float y, SKMatrix transform, bool isLocal) {
        //    SKMatrix inverse;
        //    transform.TryInvert(out inverse);

        //    this.InverseTransform = inverse;
        //    this.Transform = transform;

        //    if (isLocal) {
        //        this.LocalCoordinate = new SKPoint(x, y);
        //    } else {
        //        this.LocalCoordinate = this.InverseTransform.MapPoint(new SKPoint(x, y));
        //    }

        //    Logger.Debug("Global Coordinate: {0}", this.GlobalCoordinate);
        //    Logger.Debug("Local Coordinate: {0}", this.LocalCoordinate);
        //}

        public override void Draw(SKCanvas canvas) {
            float radius = 5;

            if (this.isHovered) {
                radius += 2;
                //Hover.DrawHover(canvas, this.GlobalLocation);
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
            canvas.DrawCircle(this.GlobalLocation, radius, fillPaint);
            canvas.DrawCircle(this.GlobalLocation, radius, strokePaint);

            //Logger.Debug("Entry Global Location: {0}", this.GlobalLocation);
            //Logger.Debug("Entry Local Location: {0}", this.Location);


        }

        public void DrawHover(SKCanvas canvas) {
            if (this.isHovered) {
                Hover.DrawHover(canvas, this.Value, this.Location, this.Transform);
            }
        }

        //public void UpdateTransform(SKMatrix transform) {
        //    this.Transform = transform;
        //}

        public void UpdateScale(SKMatrix scale) {
            this.Scale = scale;
        }

        public void UpdateFromGlobalLocation(SKPoint gLocation) {
            SKMatrix inverseCoordinate, inverseScale;
            this.Transform.TryInvert(out inverseCoordinate);
            this.Scale.TryInvert(out inverseScale);

            this.Value = inverseScale.MapPoint(inverseCoordinate.MapPoint(gLocation));
        }
        public void DoDragDrop() {

        }

        //public void DrawEntry(SKCanvas canvas) {
        //    float radius = 5;

        //    if (this.isHovered) {
        //        radius += 2;
        //        Hover.DrawHover(canvas, this.GlobalCoordinate);
        //    }

        //    var fillPaint = new SKPaint {
        //        IsAntialias = true,
        //        Color = SKColors.ForestGreen,
        //        Style = SKPaintStyle.Fill
        //    };
        //    var strokePaint = new SKPaint {
        //        IsAntialias = true,
        //        Color = SKColors.Black,
        //        Style = SKPaintStyle.Stroke,
        //        StrokeWidth = 2
        //    };

        //    // Draw entry shape
        //    canvas.DrawCircle(this.GlobalCoordinate, radius, fillPaint);
        //    canvas.DrawCircle(this.GlobalCoordinate, radius, strokePaint);
        //}

    }

    public abstract class CanvasObject {
        public SKMatrix Transform { get; set; }
        public virtual SKPoint Location { get; set; }
        public SKPoint GlobalLocation {
            get {
                return this.Transform.MapPoint(this.Location);
            }
        }

        public CanvasObject(SKPoint location, SKMatrix transform) {
            this.Location = location;
            this.Transform = transform;
        }

        public CanvasObject(SKMatrix transform) {
            this.Transform = transform;
        }

        public abstract void Draw(SKCanvas canvas);
        public virtual void UpdateTransform(SKMatrix transform) {
            this.Transform = transform;
        }
    }

    public class Axis : CanvasObject
    {
        public string Label { get; set; } = "X";
        public List<Tick> Ticks { get; set; } = new List<Tick>();
        public int MaxTicksLimit { get; set; } = 11;
        public float MaxValue { get; set; }
        public float MinValue { get; set; }
        public float LengthInPixel { get; set; }
        // Scale = Value / Pixel
        private float Scale {
            get {
                return (this.MaxValue - this.MinValue) / this.LengthInPixel;
            }
        }

        //public SKMatrix Scale { get; set; }
        //public SKMatrix Transform { get; set; }
        //public SKPoint Location { get; set; }
        //public SKPoint GlobalLocation {
        //    get {
        //        return this.Transform.MapPoint(this.Location);
        //    }
        //}

        public Axis(string label, float min, float max, float lengthInPixel, SKPoint location, SKMatrix transform) : base(location, transform) {
            this.MaxValue = max;
            this.MinValue = min;
            this.Label = label;
            this.LengthInPixel = lengthInPixel;
        }
        
        public Axis(string label, float min, float max, float lengthInPixel, SKMatrix transform) : this(label, min, max, lengthInPixel, new SKPoint(0, 0), transform) {
            var interval = (this.MaxValue - this.MinValue) / (this.MaxTicksLimit - 1);

            if (this.Label == "X") {
                for (var i = 0; i < this.MaxTicksLimit; i++) {
                    var tickValue = this.MinValue + interval * i;
                    var tickPixel = (0 + interval * i) / this.Scale;
                    //Ticks.Add(new Tick(tickValue, 0, this.Transform));
                    Ticks.Add(new Tick(tickValue.ToString(), Tick.Directions.DOWN, new SKPoint(tickPixel, 0), this.Transform));
                }
            }
            else {
                for (var i = 0; i < this.MaxTicksLimit; i++) {
                    var tickValue = this.MinValue + interval * i;
                    var tickPixel = (0 + interval * i) / this.Scale;
                    //Ticks.Add(new Tick(tickValue, 0, this.Transform));
                    Ticks.Add(new Tick(tickValue.ToString(), Tick.Directions.LEFT, new SKPoint(0, tickPixel), this.Transform));
                    //var tickValue = this.MinValue + this.Scale * i;
                    //Ticks.Add(new Tick(0, tickValue, Tick.Directions.LEFT, this.Transform));
                }
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
                    Ticks.Add(new Tick(0, tickValue, Tick.Directions.LEFT, this.Transform));
                }
            }

        }

        public Axis(string label, float lengthInPixel, SKMatrix transform) : this(label, 0, 10, lengthInPixel, new SKPoint(0, 0), transform) {
            var interval = (this.MaxValue - this.MinValue) / (this.MaxTicksLimit - 1);

            if (this.Label == "X") {
                for (var i = 0; i < this.MaxTicksLimit; i++) {
                    var tickValue = this.MinValue + interval * i;
                    var tickPixel = (this.MinValue + interval * i) / this.Scale;
                    //Ticks.Add(new Tick(tickValue, 0, this.Transform));
                    Ticks.Add(new Tick(tickValue.ToString(), Tick.Directions.DOWN, new SKPoint(tickPixel, 0), this.Transform));
                }
            }
            else {
                for (var i = 0; i < this.MaxTicksLimit; i++) {
                    var tickValue = this.MinValue + interval * i;
                    var tickPixel = (this.MinValue + interval * i) / this.Scale;
                    //Ticks.Add(new Tick(tickValue, 0, this.Transform));
                    Ticks.Add(new Tick(tickValue.ToString(), Tick.Directions.LEFT, new SKPoint(0, tickPixel), this.Transform));
                    //var tickValue = this.MinValue + this.Scale * i;
                    //Ticks.Add(new Tick(0, tickValue, Tick.Directions.LEFT, this.Transform));
                }
            }
        }
        //public Axis(string label, float length, SKMatrix transform) : base(new SKPoint(0, 0), transform) {
        //    this.Label = label;
        //    this.LengthInPixel = length;
        //    this.MaxValue = this.MaxTicksLimit * this.Scale;
        //    this.MinValue = 0;
        //    //this.Transform = transform;
        //    //this.Location = new SKPoint(0, 0);

        //    if (this.Label == "X") {
        //        for (var i = 0; i < this.MaxTicksLimit; i++) {
        //            var tickValue = this.MinValue + this.Scale * i;
        //            Ticks.Add(new Tick(tickValue, 0, this.Transform));
        //        }
        //    }
        //    else {
        //        for (var i = 0; i < this.MaxTicksLimit; i++) {
        //            var tickValue = this.MinValue + this.Scale * i;
        //            Ticks.Add(new Tick(0, tickValue, Tick.Directions.LEFT, this.Transform));
        //        }
        //    }
        //}

        public void DrawAxis(SKCanvas canvas) {
            var origin = new SKPoint(0, 0);
            var max = new SKPoint(this.LengthInPixel, 0);
            
            foreach(var t in this.Ticks) {
                //t.DrawTick(canvas);
                t.Draw(canvas);
            }

            //SkiaHelper.DrawArrow(canvas, origin, max, this.Transform);
        }

        public override void Draw(SKCanvas canvas) {
            throw new NotImplementedException();
        }

        public override void UpdateTransform(SKMatrix transform) {
            this.Transform = transform;
        }

    }

    public class Tick : CanvasObject {
        public enum Directions {
            DOWN = 0,
            UP = 1,
            LEFT = 2,
            RIGHT = 3
        }
        //public Label Label {
        //    get {
        //        Label label = new Label("", new SKPoint(0, 0), this.Transform);

        //        switch (this.Direction) {
        //            case Directions.DOWN:
        //                label = new Label(this.Location.X.ToString(), this.Location, this.Transform);
        //                break;
        //            case Directions.UP:
        //                label = new Label(this.Location.X.ToString(), this.Location, this.Transform);
        //                break;
        //            case Directions.LEFT:
        //                label = new Label(this.Location.Y.ToString(), this.Location, this.Transform);
        //                label.Type = "V";
        //                break;
        //            case Directions.RIGHT:
        //                label = new Label(this.Location.Y.ToString(), this.Location, this.Transform);
        //                label.Type = "V";
        //                break;
        //        }
        //        return label;
        //    }
        //}

        public Label Label { get; set; }
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
        
        public Tick(string name, Directions direction, SKPoint location, SKMatrix transform) : base(location, transform) {
            this.Direction = direction;
            this.SetLabel(name);
        }

        private void SetLabel(string name) {
            switch (this.Direction) {
                case Directions.DOWN:
                    this.Label = new Label(name, this.Location, this.Transform);
                    break;
                case Directions.UP:
                    this.Label = new Label(name, this.Location, this.Transform);
                    break;
                case Directions.LEFT:
                    this.Label = new Label(name, this.Location, this.Transform) {
                        Type = "V"
                    };
                    break;
                case Directions.RIGHT:
                    this.Label = new Label(name, this.Location, this.Transform) {
                        Type = "V"
                    };
                    break;
            }
        }
        public Tick(float x, float y, SKMatrix transform) : base(new SKPoint(0, 0), transform) {
            this.Location = new SKPoint(x, y);
        }
        
        public Tick(float x, float y, Directions dir, SKMatrix transform) : base(new SKPoint(0, 0), transform) {
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

            // Draw label
            //this.Label.DrawLabel(canvas);
            this.Label.Draw(canvas);

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
            //this.Label.DrawLabel(canvas);
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
        public string Type { get; set; } = "H";
        public SKPoint Anchor { get; set; }
        public string Name { get; set; }

        //public Label() { }
        public Label(string name, SKPoint location, SKMatrix transform) : base(location, transform) {
            this.Name = name;
        }

        public override void Draw(SKCanvas canvas) {
            var paint = new SKPaint {
                IsAntialias = true,
                Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Black, 0.8f),
                TextSize = 14,
                TextAlign = SKTextAlign.Center
            };

            var offset = new SKPoint(0, 0);
            var textWidth = paint.MeasureText(this.Name);

            if (this.Type == "H") {
                offset = new SKPoint(0, -(paint.TextSize + 10));
            }
            else {
                offset = new SKPoint(-(textWidth / 2 + 10), - paint.TextSize / 2);
            }

            canvas.DrawText(this.Name, this.Transform.MapPoint(this.Location + offset), paint);
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
        public static void DrawArrow(SKCanvas canvas, SKPoint start, SKPoint end, SKMatrix transform) {
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

            linePath.Transform(transform);
            canvas.DrawPath(linePath, paint);

            // Arrow Path
            SKPath arrowPath = new SKPath();
            arrowPath.MoveTo(end);
            arrowPath.LineTo(lArrow);
            arrowPath.MoveTo(end);
            arrowPath.LineTo(rArrow);
            
            arrowPath.Transform(rotMat);
            linePath.Transform(transform);
            canvas.DrawPath(arrowPath, paint);
        }
    }
}
