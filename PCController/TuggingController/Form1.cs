using System;
using System.Numerics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using NLog;


namespace TuggingController {
    public partial class Form1 : Form
    {
        public PointChart chart;
        public ConfigurationCanvas configuration;
        public SimplicialComplex mapping = new SimplicialComplex();
       
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private bool IsDragging { get; set; } = false;
        private Point StartLocationOnDrag { get; set; }
        private Point CurrentLocationOnDrag { get; set; }

        private int DragTargetConfiguration;
        private Entry DragTarget;
        private Triangulation Tri { get; set; }
        public Form1()
        {
            InitializeComponent();

            var config = new NLog.Config.LoggingConfiguration();
            var logConsole = new NLog.Targets.ColoredConsoleTarget("Form1");
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            NLog.LogManager.Configuration = config;

            Logger.Debug("Hello World");
            //RobotController ctrl = new RobotController();
            this.Tri = new Triangulation();
            this.Tri.OnDataReceived += Triangle_DataReceived;
            //this.Tri.RunRbox();
            //this.Tri.StartTask();

            Point mid = new Point {
                X = (this.groupbox1.Location.X) / 2,
                Y = this.ClientSize.Height
            };

            TuggingController.Location = new Point(0, 0);
            TuggingController.Size = new Size(mid.X, mid.Y);
            
            TuggingController.PaintSurface += SkControl1_PaintSurface;
            TuggingController.MouseMove += skControl1_MouseMove;
            TuggingController.MouseDown += skControl1_MouseDown;
            TuggingController.MouseUp += skControl1_MouseUp;
            //TuggingController.MouseClick += skControl1_MouseClick;
            TuggingController.MouseDoubleClick += skControl1_MouseDoubleClick;
            this.SizeChanged += Form1_SizeChanged;
            this.chart = new PointChart();
            this.chart.Entries.CollectionChanged += Entries_CollectionChanged;

            this.configuration = new ConfigurationCanvas(mid.X, mid.Y);
            ConfigurationSpace.Location = new Point(mid.X, 0);
            ConfigurationSpace.Size = new Size(mid.X, mid.Y);
            ConfigurationSpace.PaintSurface += ConfigurationSpace_PaintSurface1;
            ConfigurationSpace.MouseMove += ConfigurationSpace_MouseMove;
            ConfigurationSpace.MouseDown += ConfigurationSpace_MouseDown;
            ConfigurationSpace.MouseUp += ConfigurationSpace_MouseUp;


        }

        private void ConfigurationSpace_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this.configuration.IsDown = false;
                this.configuration.IsDragging = false;
                DragTargetConfiguration = 0;
            }
        }

        private void ConfigurationSpace_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                //this.configuration.IsDragging = true;
                this.configuration.IsDown = true;
            }
        }

        private void ConfigurationSpace_MouseMove(object sender, MouseEventArgs e) {
            SKPoint location = new SKPoint(e.Location.X, e.Location.Y);

            if (this.configuration.IsDown) {
                if (!this.configuration.IsDragging) {
                    if (this.configuration.CheckInControlArea(location, out int idx)) {
                        this.configuration.ControlPoints[idx] = location;
                        this.configuration.IsDragging = true;
                        DragTargetConfiguration = idx;

                        this.ConfigurationSpace.Invalidate();
                    }
                }
                else {
                    this.configuration.ControlPoints[DragTargetConfiguration] = location;

                    this.ConfigurationSpace.Invalidate();
                }
            }

        }

        private void ConfigurationSpace_PaintSurface1(object sender, SKPaintSurfaceEventArgs e) {
            this.configuration.Draw(e.Surface.Canvas);

            //throw new NotImplementedException();
        }

        private void Entries_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            //this.Tri.RunDelaunay();
            //this.Tri.StartTask();

            TuggingController.Invalidate();
        }

        private void Triangle_DataReceived(string fileName, string data) {
            Logger.Debug("Filename: {0}", fileName);

            string[] lines = data.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (fileName == "rbox.exe") {
                for(int idx = 0; idx < lines.Length; idx ++) {
                    if (idx > 1) {
                        var coordinates = lines[idx].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(e => Convert.ToSingle(e)).ToArray();
                        this.chart.AddPointFromValue(coordinates[0], coordinates[1]);
                    }
                }

                Logger.Debug("MinValue: {0}", this.chart.MinValue);
                Logger.Debug("MaxValue: {0}", this.chart.MaxValue);
                //Console.WriteLine(this.chart.PrintEntries());
                TuggingController.Invalidate();
            }
            else if (fileName == "qdelaunay") {
                List<int[]> triangles = new List<int[]>();

                for (int idx = 0; idx < lines.Length; idx ++) {
                    if (idx > 0) {
                        var vertices = lines[idx].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(e => Convert.ToInt32(e)).ToArray();
                        triangles.Add(vertices);
                    }
                }

                //Logger.Debug(triangles);

                this.chart.Triangulate(triangles);
                TuggingController.Invalidate();
            }
        }
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            Point mid = new Point {
                X = this.groupbox1.Location.X / 2,
                Y = this.ClientSize.Height
            };

            TuggingController.Size = new Size(mid.X, mid.Y);
            ConfigurationSpace.Size = new Size(mid.X, mid.Y);
            this.chart.forceUpdateScale = true;

            this.configuration.CanvasSize = new SKSize(mid.X, mid.Y);
            TuggingController.Invalidate();
            ConfigurationSpace.Invalidate();
        }

        private void SkControl1_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            Point mid = new Point {
                X = this.groupbox1.Location.X / 2,
                Y = this.ClientSize.Height
            };

            this.chart.Draw(e.Surface, mid.X, mid.Y);
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

        private void RunTriangulationTask() {
            if (File.Exists("data.txt")) {
                File.Delete("data.txt");
            }
            using (FileStream fs = File.Create("data.txt")) {
                byte[] info = new UTF8Encoding(true).GetBytes(this.chart.PrintEntries());
                fs.Write(info, 0, info.Length);
            }

            this.Tri.RunDelaunay();
            this.Tri.StartTask();
        }
        private void skControl1_MouseUp(object sender, MouseEventArgs e) {
            switch (e.Button) {
                case MouseButtons.Left:
                    //this.IsDragging = false;
                    if (!this.IsDragging) {
                        this.skControl1_MouseClick(sender, e);
                    }
                    
                    this.RunTriangulationTask();
                    //this.StartLocationOnDrag = new Point();
                    //this.CurrentLocationOnDrag = new Point();
                    //Console.WriteLine(this.chart.PrintEntries());
                    //if (File.Exists("data.txt")) {
                    //    File.Delete("data.txt");
                    //}
                    //using (FileStream fs = File.Create("data.txt")) {
                    //    byte[] info = new UTF8Encoding(true).GetBytes(this.chart.PrintEntries());
                    //    fs.Write(info, 0, info.Length);
                    //}

                    //this.Tri.RunDelaunay();
                    //this.Tri.StartTask();
                    break;
                case MouseButtons.Right:
                    this.skControl1_MouseClick(sender, e);
                    break;
            }

            Logger.Debug("Detected a mouseup event.");



        }
        private void skControl1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Add new point

            switch (e.Button)
            {
                case MouseButtons.Left:
                    //if ()
                    Logger.Debug("Add new point");
                    Logger.Debug(e.Location);
                    this.chart.AddPointFromGlobal(e.Location);
                    TuggingController.Invalidate();
                    break;
                default:
                    break;
            }

        }

        private void skControl1_MouseClick(object sender, MouseEventArgs e) {
            Logger.Debug("Detected a mouseclick event.");

            switch (e.Button) {
                case MouseButtons.Left:
                    if (this.chart.isInZone(e.Location, 5, out Entry target)) {
                        target.isSelected = !target.isSelected;
                    }
                    Logger.Debug("");
                    break;
                case MouseButtons.Right:
                    this.chart.TestPoint.IsDisplayed = !this.chart.TestPoint.IsDisplayed;
                    this.chart.TestPoint.UpdateFromGlobalLocation(new SKPoint(e.Location.X, e.Location.Y));
                    //TuggingController.Invalidate();
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
                    TuggingController.Invalidate();
                }
                else {
                    this.chart.Hovered = false;
                }

                if (this.chart.isInArea(e.Location)) {
                    this.chart.hasIndicator = true;
                    TuggingController.Invalidate();
                }
                else {
                    this.chart.hasIndicator = false;
                    TuggingController.Invalidate();
                }

                this.chart.IsInTriangle(this.chart.PointerLocation);
                TuggingController.Invalidate();

                //if (this.chart.IsInComplex(this.chart.PointerLocation, out int idx)) {
                //    this.chart.Triangles[idx].IsHovered = true;
                //    TuggingController.Invalidate();
                //}
                //else {
                //    this.chart.Triangles[idx].IsHovered = false;
                //    TuggingController.Invalidate();
                //}
            }
            else if (e.Button == MouseButtons.Right) {
                if (this.IsDragging) {
                    this.CurrentLocationOnDrag = e.Location;
                    var targetLoc = new SKPoint(e.Location.X, e.Location.Y);
                    this.chart.TestPoint.UpdateFromGlobalLocation(targetLoc);

                    if (this.mapping.Configurations.Count == 3) {
                        var result = this.mapping.GetInterpolatedConfiguration(this.chart.TestPoint.Value);
                        //Logger.Debug("Interpolated Config: {0}", result.ToSKPoint());
                        this.configuration.ControlPoints = result.ToSKPoint();
                    }

                    //Logger.Debug("Dragging.");
                    TuggingController.Invalidate();
                    ConfigurationSpace.Invalidate();
                }
                else {
                    if (this.chart.TestPoint.CheckIsInZone(e.Location, 10)) {
                        if (this.chart.isInArea(e.Location)) {
                            this.CurrentLocationOnDrag = e.Location;
                            this.IsDragging = true;
                            var targetLoc = new SKPoint(e.Location.X, e.Location.Y);
                            this.chart.TestPoint.UpdateFromGlobalLocation(targetLoc);

                            TuggingController.Invalidate();
                        }

                    }
                }
            }
            else {
                if (this.IsDragging) {
                    this.CurrentLocationOnDrag = e.Location;
                    //var move = this.CurrentLocationOnDrag - this.StartLocationOnDrag;
                    //var targetLocation = new SKPoint(e.Location.X - this.StartLocationOnDrag.X, e.Location.Y - this.StartLocationOnDrag.Y);
                    var targetLoc = new SKPoint(e.Location.X, e.Location.Y);
                    this.DragTarget.UpdateFromGlobalLocation(targetLoc);

                    //Logger.Debug("Dragging.");
                    TuggingController.Invalidate();
                }
                else {
                    if (this.chart.isInZone(e.Location, 10, out this.DragTarget)) {
                        if (this.chart.isInArea(e.Location)) {
                            this.CurrentLocationOnDrag = e.Location;
                            this.IsDragging = true;
                            //var move = this.CurrentLocationOnDrag - this.StartLocationOnDrag;
                            //var targetLocation = new SKPoint(e.Location.X - this.StartLocationOnDrag.X, e.Location.Y - this.StartLocationOnDrag.Y);
                            var targetLoc = new SKPoint(e.Location.X, e.Location.Y); 
                            this.DragTarget.UpdateFromGlobalLocation(targetLoc);

                            //Logger.Debug("Dragging.");
                            TuggingController.Invalidate();

                        }
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

        private void button2_Click(object sender, EventArgs e) {
            this.chart.SetInitialization();
            this.chart.forceUpdateScale = true;
            this.TuggingController.Invalidate();
        }

        private void button1_Click(object sender, EventArgs ev) {
            var selectedEntry = this.chart.Entries.Where(e => e.isSelected).ToArray()[0];
            var currentConfiguration = this.configuration.ControlPoints;

            this.mapping.CreatePair(selectedEntry.Value, currentConfiguration);

            selectedEntry.isPaired = true;
            selectedEntry.isSelected = false;

            TuggingController.Invalidate();
        }

        private void button3_Click(object sender, EventArgs e) {
            this.chart.SaveToFile();
        }
    }

    public class Entries : ObservableCollection<Entry> {
        public Entries() : base() { }

        public void ForEach(Action<Entry> action) {
            for (int i = 0; i < this.Count; i++) {
                action(this[i]);
            }
        }
    }

    public abstract class Chart
    {
        #region Properties
        public List<Axis> Axes { get; set; } = new List<Axis>();
        //public List<Entry> Entries { get; set; } = new List<Entry>();
        public Entries Entries { get; set; } = new Entries();
        public TestPoint TestPoint { get; set; } = new TestPoint();
        public List<Triangle> Triangles { get; set; } = new List<Triangle>();
        public SKRect RangeOfValue { get; set; }
        public SKRect RangeOfInflatedValue { get; set; }
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
        public float Margin { get; set; } = 80;
        public float Padding;
        public float LabelTextSize { get; set; } = 16;
        public SKRect chartArea;
        protected SKRect frameArea;
        public bool forceUpdateScale { get; set; } = true;
        public bool Hovered { get; set; } = false;
        public bool hasIndicator { get; set; } = false;
        public SKPoint PointerLocation { get; set; }

        public SKSurface Surface { get; set; }
        public SKBitmap SavedBitmap { get; set; }
        public SKCanvas Canvas;
        protected float width;
        protected float height;
        public SKMatrix Transform;
        public SKMatrix InverseTransform;
        public SKMatrix Scale;
        public SKMatrix InverseScale;

        protected readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Methods

        // TODO: Calculation still has some problems.
        protected void CalculateSize() {
            if (this.Entries.Count > 0) {
                this.RangeOfValue = new SKRect(this.MinValue.X, this.MaxValue.Y, this.MaxValue.X, this.MinValue.Y);
            }
            else {
                this.RangeOfValue = new SKRect(0, 1, 1, 0);
            }
            int validDigits = 2;

            double bottom = this.RangeOfValue.Bottom;
            double left = this.RangeOfValue.Left;
            double top = this.RangeOfValue.Top;
            double right = this.RangeOfValue.Right;

            int expB = Math.Abs(bottom) != 0 ? Convert.ToInt32(Math.Truncate(Math.Log10(Math.Abs(bottom)))) : 0;
            int expL = Math.Abs(left) != 0 ? Convert.ToInt32(Math.Truncate(Math.Log10(Math.Abs(left)))) : 0;
            int expT = Math.Abs(top) != 0 ? Convert.ToInt32(Math.Truncate(Math.Log10(Math.Abs(top)))) : 0;
            int expR = Math.Abs(right) != 0 ? Convert.ToInt32(Math.Truncate(Math.Log10(Math.Abs(right)))): 0;

            double mantissaB = bottom / Math.Pow(10, expB - validDigits);
            double mantissaL = left / Math.Pow(10, expL - validDigits);
            double mantissaT = top / Math.Pow(10, expT - validDigits);
            double mantissaR = right / Math.Pow(10, expR - validDigits);

            //double bExt = Math.Ceiling(Math.Abs(mantissaB)) * Math.Pow(10, expB - validDigits) * (mantissaB > 0 ? 1 : -1);
            //double lExt = Math.Ceiling(Math.Abs(mantissaL)) * Math.Pow(10, expL - validDigits) * (mantissaL > 0 ? 1 : -1);

            double bExt = Math.Floor(mantissaB) * Math.Pow(10, expB - validDigits);
            double lExt = Math.Floor(mantissaL) * Math.Pow(10, expL - validDigits);
            double tExt = Math.Ceiling(mantissaT) * Math.Pow(10, expT - validDigits);
            double rExt = Math.Ceiling(mantissaR) * Math.Pow(10, expR - validDigits);

            // Height of SKRect is written in reverse direction.
            //double width = this.RangeOfValue.Width;
            //double height = this.RangeOfValue.Height;

            //int expW = (int)Math.Truncate(Math.Log10(Math.Abs(width)));
            //int expH = (int)Math.Truncate(Math.Log10(Math.Abs(height)));

            //double mantissaW = width / Math.Pow(10, expW - validDigits);
            //double mantissaH = height / Math.Pow(10, expH - validDigits);

            //double wExt = Math.Ceiling(Math.Abs(mantissaW)) * Math.Pow(10, expW - validDigits) * (mantissaW > 0 ? 1 : -1);
            //double hExt = Math.Ceiling(Math.Abs(mantissaH)) * Math.Pow(10, expH - validDigits) * (mantissaH > 0 ? 1 : -1);
            //double wExt = 1 * Math.Pow(10, expW) * (mantissaW > 0 ? 1 : -1);
            //double hExt = 1 * Math.Pow(10, expH) * (mantissaH > 0 ? 1 : -1);


            //this.RangeOfInflatedValue = SKRect.Create(Convert.ToSingle(lExt), Convert.ToSingle(bExt + Math.Abs(hExt)), Convert.ToSingle(wExt), Convert.ToSingle(hExt));
            this.RangeOfInflatedValue = new SKRect((float)lExt, (float)tExt, (float)rExt, (float)bExt);
            //this.RangeOfInflatedValue = SKRect.Create(Convert.ToSingle(lExt), Convert.ToSingle(bExt + Math.Abs(hExt)), Convert.ToSingle(wExt), Convert.ToSingle(hExt));

            //Logger.Debug("Original Range: {0}", this.RangeOfValue);
            //Logger.Debug("Inflated Range: {0}", this.RangeOfInflatedValue);
        }

        // Scale Transform: Local Coordinate [Screen] =====> Value Coordinate [Chart]
        protected void UpdateScale() {
            SKMatrix scale = SKMatrix.MakeTranslation(this.RangeOfInflatedValue.Left, this.RangeOfInflatedValue.Bottom);
            // Height in SKRect has a sign.
            SKMatrix.PreConcat(ref scale, SKMatrix.MakeScale(this.RangeOfInflatedValue.Width / this.chartArea.Width, Math.Abs(this.RangeOfInflatedValue.Height) / this.chartArea.Height));
            this.Scale = scale;

            this.Scale.TryInvert(out this.InverseScale);
            // Test
            //SKPoint[] tps = {
            //    new SKPoint(0, 0),
            //    new SKPoint(this.chartArea.Width, this.chartArea.Height)
            //};

            //Logger.Debug("Local Coordinate: {0}, {1}", tps[0], tps[1]);
            //SKPoint[] MappedTPs = this.Scale.MapPoints(tps);
            //Logger.Debug("Value Coordinate: {0}, {1}", MappedTPs[0], MappedTPs[1]);

            //tps = this.InverseScale.MapPoints(MappedTPs);
            //Logger.Debug("Local Coordinate: {0}, {1}", tps[0], tps[1]);
            //Logger.Debug("Value Coordinate: {0}, {1}", MappedTPs[0], MappedTPs[1]);
        }
        public void Draw(SKSurface surface, int width, int height)
        {
            this.Surface = surface;
            this.width = width;
            this.height = height;
            this.SavedBitmap = new SKBitmap(width, height);
            this.Canvas = new SKCanvas(SavedBitmap);
            //this.Canvas = surface.Canvas;

            chartArea = new SKRect {
                Size = new SKSize(this.width - this.Margin * 2, this.height - this.Margin * 2),
                Location = new SKPoint(this.Margin, this.Margin)
            };

            frameArea = new SKRect {
                Size = new SKSize(this.width, this.height)
            };



            // Scale on a specific pivot
            //SKMatrix paddingScale = SKMatrix.MakeIdentity();
            //SKMatrix.PreConcat(ref paddingScale, SKMatrix.MakeTranslation(rangeOfInflatedValue.MidX, rangeOfInflatedValue.MidY));
            //SKMatrix.PreConcat(ref paddingScale, SKMatrix.MakeScale(rangeOfInflatedValue.Width / rangeOfValue.Width, rangeOfInflatedValue.Height / rangeOfValue.Height));
            //SKMatrix.PreConcat(ref paddingScale, SKMatrix.MakeTranslation(-rangeOfInflatedValue.MidX, -rangeOfInflatedValue.MidY));

            // Calculate the transform of this chart
            //var translate = SKMatrix.MakeTranslation(-this.MinValue.X, -this.MinValue.Y);
            //SKMatrix.PostConcat(ref translate, SKMatrix.MakeScale(this.chartArea.Width / (this.MaxValue.X - this.MinValue.X), this.chartArea.Height / (this.MaxValue.Y - this.MinValue.Y)));
            //this.Scale = translate;
            //this.Scale = SKMatrix.MakeScale(this.chartArea.Width / 10, this.chartArea.Height / 10);
            if(this.forceUpdateScale) {
                // Calculation
                this.CalculateSize();
                this.UpdateScale();
                this.Entries.ForEach(e => e.UpdateScale(this.InverseScale));
                this.Triangles.ForEach(t => t.UpdateScale(this.InverseScale));
                this.TestPoint.UpdateScale(this.InverseScale);

                this.forceUpdateScale = !this.forceUpdateScale;
            }
            // Update data

            // TODO: Transformation didn't match to each other.
            this.UpdateTransform();
            this.Entries.ForEach(e => e.UpdateTransform(this.InverseTransform));
            this.Triangles.ForEach(t => t.UpdateTransform(this.InverseTransform));
            this.TestPoint.UpdateTransform(this.InverseTransform);

            //if (this.Entries)

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

            this.TestPoint.Draw(this.Canvas);

            surface.Canvas.Clear();
            surface.Canvas.DrawBitmap(this.SavedBitmap, 0, 0);
        }

        public abstract void DrawIndicator();
        public abstract void DrawArea(object ctx);
        public abstract void DrawContent(object ctx);
        public abstract void DrawHover(Chart chart);

        public abstract void UpdateTransform();

        public virtual string PrintEntries() {
            string ret = String.Format("2\r\n{0}\r\n", this.Entries.Count);
            foreach (var e in this.Entries) {
                ret += e.Value.X + " " + e.Value.Y + "\r\n";
            }

            return ret;
        }

        public virtual void SaveToFile() {
            SKBitmap snap = this.SavedBitmap;

            using (FileStream fs = new FileStream("screenshot.png", FileMode.OpenOrCreate))
            using (SKManagedWStream wStream = new SKManagedWStream(fs)) { 
                snap.Encode(wStream, SKEncodedImageFormat.Png, 10);
            }
        }
        #endregion
    }

    public class PointChart : Chart
    {
        #region Properties
        public List<SimplicialComplex> Complices { get; set; } = new List<SimplicialComplex>();
        #endregion

        #region Methods

        public PointChart() : base() {
        
        }
        //public PointChart(Entry[] entries) {
        //    this.Entries = new List<Entry>(entries);
        //}
        private double DegreeToRadian(double degree) {
            return Math.PI * degree / 180.0;
        }

        public void SetInitialization() {
            this.Entries = new Entries() {
                new Entry(new SKPoint(0, 0)),
                new Entry(new SKPoint(1, 0)),
                new Entry(new SKPoint(0, 1)),
            };
        }

        public void IsInTriangle(SKPoint target) {
            var value = this.Scale.MapPoint(this.Transform.MapPoint(target));
            this.Triangles.ForEach(t => t.IsInside(value));
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
            SKPoint location = this.Transform.MapPoint(new SKPoint(globalLocation.X, globalLocation.Y));
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

        public override void DrawHover(Chart ctx) {
            //Hover.DrawHover(this.Canvas, ctx.PointerLocation);
            foreach(var e in this.Entries) {
                e.DrawHover(this.Canvas);
            }
        }

        public override void DrawIndicator() {
            this.DrawCross(this.Canvas, this.chartArea, this.Transform.MapPoint(this.PointerLocation), this.InverseTransform);
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

        public override void UpdateTransform() {
            SKMatrix transform = SKMatrix.MakeTranslation(-this.Margin, (this.chartArea.Height + this.Margin));
            SKMatrix.PreConcat(ref transform, SKMatrix.MakeScale(1, -1));
            //SKMatrix.PreConcat(ref mat, );
            //SKMatrix.PostConcat(ref mat, SKMatrix.MakeTranslation(this.Margin, (this.chartArea.Height + this.Margin)));
            this.Transform = transform;
            this.Transform.TryInvert(out this.InverseTransform);

            // Test
            //SKPoint[] tps = {
            //    new SKPoint(80, 481),
            //    new SKPoint(this.chartArea.Width + this.Margin, this.Margin)
            //};

            //Logger.Debug("Global Coordinate: {0}, {1}", tps[0], tps[1]);
            //SKPoint[] MappedTPs = this.Transform.MapPoints(tps);
            //Logger.Debug("Local Coordinate: {0}, {1}", MappedTPs[0], MappedTPs[1]);

            //tps = this.InverseScale.MapPoints(MappedTPs);
            //Logger.Debug("Local Coordinate: {0}, {1}", tps[0], tps[1]);
            //Logger.Debug("Value Coordinate: {0}, {1}", MappedTPs[0], MappedTPs[1]);
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

            var canvasRect = new SKRect() {
                Size = new SKSize(this.chartArea.Width, this.chartArea.Height)
            };

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
            var bgPaint = new SKPaint {
                Color = SKColors.White
            };

            // For Debug: Draw frame region and debug info
            //this.DrawFrame();
            //this.Canvas.DrawText(string.Format("{0} {1}", this.chartArea.Width, this.chartArea.Height), new SKPoint(0, this.Margin), textPaint);


            //this.Canvas.Save();
            //this.Canvas.ResetMatrix();

            // Draw axes
            this.Axes.Clear();

            if (this.Entries.Count == 0) {
                this.Axes.Add(new Axis("X", this.chartArea.Width, this.InverseTransform));
                this.Axes.Add(new Axis("Y", this.chartArea.Height, this.InverseTransform));
            }
            else {
                //this.Axes.Add(new Axis("X", this.MinValue.X, this.MaxValue.X, this.chartArea.Width, this.Transform));
                //this.Axes.Add(new Axis("Y", this.MinValue.Y, this.MaxValue.Y, this.chartArea.Height, this.Transform));
                this.Axes.Add(new Axis("X", this.RangeOfInflatedValue.Left, this.RangeOfInflatedValue.Right, this.chartArea.Width, this.InverseTransform));
                this.Axes.Add(new Axis("Y", this.RangeOfInflatedValue.Bottom, this.RangeOfInflatedValue.Top, this.chartArea.Height, this.InverseTransform));
            }

            this.DrawAxes();

            // Draw arrow
            this.DrawArrow(origin, xMax, this.InverseTransform);
            this.DrawArrow(origin, yMax, this.InverseTransform);

            //this.Canvas.Restore();

            //this.Canvas.DrawRect(canvasRect, bgPaint);

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

        // TODO: [Bug] Sync task may cause collections modified exception.
        public override void DrawContent(object ctx)
        {
            if (this.Entries.Count > 0) {
                foreach(var e in this.Entries) {
                    e.Draw(this.Canvas);
                }
            }

            if (this.Entries.Count == 3) {
                this.Triangulate(new List<int[]>() { new int[] { 0, 1, 2 } });
            }

            if (this.Triangles.Count > 0) {
                foreach(var t in this.Triangles) {
                    t.Draw(this.Canvas);
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

            var tPoint = new SKPoint(point.X, point.Y);
            tPoint = this.Transform.MapPoint(tPoint);
            tPoint = this.Scale.MapPoint(tPoint);
            //this.Entries.Add(new Entry(tPoint, this.Scale, this.InverseTransform));
            //this.Entries.Add(new Entry(tPoint, inverse, this.InverseTransform));
            this.Entries.Add(new Entry(tPoint, this.InverseScale, this.InverseTransform));
        }

        public void AddPointFromValue(float x, float y) {
            //SKMatrix inverse;
            //this.Scale.TryInvert(out inverse);
            var tPoint = new SKPoint(x, y);
            //this.Entries.Add(new Entry(tPoint, this.Scale, this.InverseTransform));
            //this.Entries.Add(new Entry(tPoint, inverse, this.InverseTransform));
            this.Entries.Add(new Entry(tPoint));
        }

        public void Triangulate(List<int[]> triangles) {
            this.Triangles.Clear();

            foreach(var t in triangles) {
                SKPoint[] vertices = new SKPoint[] { this.Entries[t[0]].Location, this.Entries[t[1]].Location, this.Entries[t[2]].Location };
                this.Triangles.Add(new Triangle(this.Scale.MapPoints(vertices), this.InverseScale, this.InverseTransform));
                var complex = new SimplicialComplex();
                complex.CreateSimplex(vertices);
                this.Complices.Add(complex);
            }
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
            textPaint.MeasureText(value.X.ToString("F4") + ", " + value.Y.ToString("F4"), ref rect);

            canvas.DrawText(value.X.ToString("F4") + ", " + value.Y.ToString("F4"), anchor + new SKPoint(5, 25), textPaint);
            //canvas.DrawRect(rect, strokePaint);
        }
    }

    public class Triangle : CanvasObject {
        public bool IsHovered { get; set; } = false;
        // Vertice [Value]
        public SKPoint[] Vertices { get; set; }
        public SKMatrix Scale { get; set; }

        public Triangle(SKPoint[] vertices) : this(vertices, new SKPoint(0, 0), SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity()) { }

        public Triangle(SKPoint[] vertices, SKMatrix scale, SKMatrix transform) : this(vertices, new SKPoint(0, 0), scale, transform) { }
        public Triangle(SKPoint[] vertices, SKPoint location, SKMatrix scale, SKMatrix transform): base(location, transform) {
            this.Scale = scale;
            this.Vertices = vertices;
        }

        public bool IsInside(SKPoint target) {
            var coor = SimplicialComplex.GetBarycentricCoordinate(target, new SimplicialComplex.Simplex3I { V1 = Vertices[0], V2 = Vertices[1], V3 = Vertices[2] });

            this.IsHovered = coor.IsInside;

            //Logger.Debug("Triangle: {0}", string.Join(",", this.Vertices.Select(v => v.ToString())));
            return coor.IsInside;
        }
        public void UpdateScale(SKMatrix scale) {
            this.Scale = scale;
        }
        public override void Draw(SKCanvas canvas) {
            var fillPaint = new SKPaint {
                IsAntialias = true,
                //Color = SKColors.ForestGreen,
                Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DimGray, 0.3f),
                Style = SKPaintStyle.Fill
            };
            var strokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            var path = new SKPath();
            SKPoint[] gVertices = this.Scale.MapPoints(this.Vertices);
            gVertices = this.Transform.MapPoints(gVertices);

            path.MoveTo(gVertices[0]);
            path.LineTo(gVertices[1]);
            path.LineTo(gVertices[2]);
            path.LineTo(gVertices[0]);

            if (this.IsHovered) {
                fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DimGray, 0.6f);
            }

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, strokePaint);
        }
    }

    // TODO: ChartObject [with Scale]  <-- CanvasObject
    public abstract class ChartObject : CanvasObject {
        public SKPoint Value { get; set; }
        public SKMatrix Scale { get; set; }
        public override SKPoint Location {
            get {
                return this.Scale.MapPoint(this.Value);
            }
        } 

        public ChartObject(SKPoint value, SKMatrix scale, SKMatrix transform) : base(transform) {
            this.Value = value;
            this.Scale = scale;
        }

        public ChartObject(SKPoint value) : this(value, SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity()) { }

        public virtual void UpdateScale(SKMatrix scale) {
            this.Scale = scale;
        }
    }
    public class TestPoint : ChartObject {
        public bool IsDisplayed { get; set; } = false;
        public bool isHovered { get; set; } = false;
        public bool isSelected { get; set; } = false;

        public TestPoint() : base(new SKPoint(0, 0)) { }
        public TestPoint(SKPoint value) : base(value) { }
        public override void Draw(SKCanvas canvas) {
            if (!this.IsDisplayed)
                return;

            float radius = 5;

            if (this.isHovered | this.isSelected) {
                radius += 2;
                //Hover.DrawHover(canvas, this.GlobalLocation);
            }

            var fillPaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.DarkCyan,
                Style = SKPaintStyle.Fill
            };
            var strokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            //if (this.isSelected) {
            //    fillPaint.Color = SKColors.MediumVioletRed;
            //}
            // Draw entry shape
            canvas.DrawCircle(this.GlobalLocation, radius, fillPaint);
            canvas.DrawCircle(this.GlobalLocation, radius, strokePaint);

            //Logger.Debug("Entry Global Location: {0}", this.GlobalLocation);
            //Logger.Debug("Entry Local Location: {0}", this.Location);


        }
        public void UpdateFromGlobalLocation(SKPoint gLocation) {
            SKMatrix inverseCoordinate, inverseScale;
            this.Transform.TryInvert(out inverseCoordinate);
            this.Scale.TryInvert(out inverseScale);

            this.Value = inverseScale.MapPoint(inverseCoordinate.MapPoint(gLocation));
        }

        public bool CheckIsInZone(Point gLocation, float radius) {
            var pos = new SKPoint(gLocation.X, gLocation.Y);
            var ret = false;

            var dist = SKPoint.DistanceSquared(pos, this.GlobalLocation);
            if (dist <= Math.Pow(radius, 2)) {
                this.isHovered = true;
                ret = true;
            }
            else {
                this.isHovered = true;
            }

            return ret;
        }
    }

    public class Entry : CanvasObject {
        public bool isSelected { get; set; } = false;
        public bool isHovered { get; set; } = false;
        public bool isPaired { get; set; } = false;
        public SKPoint Value { get; set; }
        public override SKPoint Location {
            get {
                //Logger.Debug("GetMethod - Location: {0}", this.Scale.MapPoint(this.Value));
                return this.Scale.MapPoint(this.Value);
            }
        }
        // Scale Transformation: Value ==> Local Coordinate
        public SKMatrix Scale { get; set; }

        public Entry() : this(0, 0, SKMatrix.MakeIdentity()) { }

        public Entry(float x, float y, SKMatrix transform) : this(new SKPoint(x, y), SKMatrix.MakeIdentity(), transform) { }

        //public Entry(SKPoint value, SKPoint location, SKMatrix transform) : base(location, transform) {
        //    //this.Value = value;
        //    Logger.Debug("Create new entry - [Location]: {0}", this.Location);
        //    Logger.Debug("Create new entry - [Value]: {0}", this.Value);
        //}

        public Entry(SKPoint value) : this(value, SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity()) { }

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

            if (this.isHovered | this.isSelected) {
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

            if (this.isSelected) {
                fillPaint.Color = SKColors.MediumVioletRed;
            }
            else if (!this.isSelected & this.isPaired) {
                fillPaint.Color = SKColors.YellowGreen;
            }
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

            Logger.Debug("Update Scale - [Value]: {0}", this.Value);
            Logger.Debug("Update Scale - [Location]: {0}", this.Location);
        }

        public void UpdateFromGlobalLocation(SKPoint gLocation) {
            SKMatrix inverseCoordinate, inverseScale;
            this.Transform.TryInvert(out inverseCoordinate);
            this.Scale.TryInvert(out inverseScale);

            this.Value = inverseScale.MapPoint(inverseCoordinate.MapPoint(gLocation));
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
        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Transform: Local Coordinate [Screen: Left-Top] ==> Global Coordinate [Screen: Left-Bottom, Center]
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
                    Ticks.Add(new Tick(tickValue.ToString("F2"), Tick.Directions.DOWN, new SKPoint(tickPixel, 0), this.Transform));
                }
            }
            else {
                for (var i = 0; i < this.MaxTicksLimit; i++) {
                    var tickValue = this.MinValue + interval * i;
                    var tickPixel = (0 + interval * i) / this.Scale;
                    //Ticks.Add(new Tick(tickValue, 0, this.Transform));
                    Ticks.Add(new Tick(tickValue.ToString("F2"), Tick.Directions.LEFT, new SKPoint(0, tickPixel), this.Transform));
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
        public static decimal CeilingOrFloor(decimal dec) {
            return dec < 0 ? decimal.Floor(dec) : decimal.Ceiling(dec);
        }

        // Ref: https://qiita.com/chocolamint/items/80ca5271c6ce1a185430
        public static int GetExpFromDecimal(decimal dec) {
            int[] bin = decimal.GetBits(dec);
            int info = bin[3];
            int signAndExp = info >> 16;
            int exp = signAndExp & 0x00FF;

            return exp;
        }
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
