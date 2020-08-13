using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
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
using LA = MathNet.Numerics.LinearAlgebra;
using Reparameterization;
using Xamarin.Forms.Internals;
using System.Configuration.Internal;
using PCController;
using System.Security.Policy;
using System.Runtime.CompilerServices;
using Xamarin.Forms.Xaml;
using MathNet.Numerics.LinearAlgebra;
using NLog.Filters;
using MathNet.Numerics.Interpolation;
using System.CodeDom;
using MathNet.Numerics.Distributions;
using System.Security.Cryptography;
using System.Collections;

namespace TuggingController {
    public partial class Form1 : Form {
        public PointChart Chart;
        public ConfigurationCanvas ConfigurationCanvas;
        //public RobotConfiguration RobotConfiguration { get; set; }
        public RobotConfigurationSpace RobotConfigurationSpace { get; set; }
        
        public MainForm PCController { get; set; }
        //public SimplicialComplex Mapping { get; set; } = new SimplicialComplex();

        //public SimplicialComplex mapping = new SimplicialComplex();
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private bool IsDragging { get; set; } = false;
        private bool IsDoubleClick { get; set; } = false;
        private Point StartLocationOnDrag { get; set; }
        private Point CurrentLocationOnDrag { get; set; }

        private int DragTargetConfiguration;
        private Entry DragTarget;
        private Triangulation Tri { get; set; }
        public Form1() {
            InitializeComponent();

            this.KeyPreview = true;

            var config = new NLog.Config.LoggingConfiguration();
            var logConsole = new NLog.Targets.ColoredConsoleTarget("Form1");
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            NLog.LogManager.Configuration = config;

            Logger.Debug("Hello World");
            //RobotController ctrl = new RobotController();
            this.Tri = new Triangulation();
            this.Tri.DataReceived += Triangle_DataReceived;
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
            TuggingController.MouseWheel += TuggingController_MouseWheel;
            this.SizeChanged += Form1_SizeChanged;
            this.Chart = new PointChart();
            //this.Chart.Entries.CollectionChanged += Entries_CollectionChanged;

            this.ConfigurationCanvas = new ConfigurationCanvas(mid.X, mid.Y);
            ConfigurationSpace.Location = new Point(mid.X, 0);
            ConfigurationSpace.Size = new Size(mid.X, mid.Y);
            ConfigurationSpace.PaintSurface += ConfigurationSpace_PaintSurface1;
            ConfigurationSpace.MouseMove += ConfigurationSpace_MouseMove;
            ConfigurationSpace.MouseDown += ConfigurationSpace_MouseDown;
            ConfigurationSpace.MouseUp += ConfigurationSpace_MouseUp;
            //ConfigurationSpace.MouseHover += (sender, e) => ConfigurationSpace.Focus();

            this.comboBox1.DataSource = Enum.GetValues(typeof(ConfigurationCanvas.CanvasState));
            this.comboBox1.SelectedIndexChanged += this.ConfigurationSpace_ChangeState;

            //this.RobotConfiguration = new RobotConfiguration();
            this.RobotConfigurationSpace = new RobotConfigurationSpace();
        }

        private void ReceiveHandler(float[] config) {
            this.RobotConfigurationSpace = new RobotConfigurationSpace();
            this.RobotConfigurationSpace.Add(config);
            //this.RobotConfiguration = new RobotConfiguration();
            //this.RobotConfiguration.AddRange(config);

            this.Logger.Info($"Received {config}");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            //if (keyData == Keys.E) {
            //    this.comboBox1.SelectedItem = ConfigurationCanvas.CanvasState.Edit;
            //    //this.configuration.State = ConfigurationCanvas.CanvasState.Edit;
            //    return true;
            //}
            //else if (keyData == Keys.Escape) {
            //    this.comboBox1.SelectedItem = ConfigurationCanvas.CanvasState.Control;
            //    return true;
            //}

            switch (keyData) {
                case Keys.P:
                    bool result = this.CreatePair();
                    if (result) {
                        MessageBox.Show("Pair created.");
                    }
                    else {
                        MessageBox.Show("No entry has been selected.");
                    }
                    return true;
                case Keys.E:
                    this.comboBox1.SelectedItem = ConfigurationCanvas.CanvasState.Edit;
                    return true;
                case Keys.F:
                    this.Chart.forceUpdateScale = true;
                    this.TuggingController.Invalidate();
                    return true;
                case Keys.Escape:
                    this.comboBox1.SelectedItem = ConfigurationCanvas.CanvasState.Control;
                    return true;
                case Keys.C:
                    this.RunConvexHullTask();
                    return true;
                case Keys.T:
                    this.RunDelaunayNTask();
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ConfigurationSpace_ChangeState(object sender, EventArgs e) {
            this.ConfigurationCanvas.State = (ConfigurationCanvas.CanvasState) this.comboBox1.SelectedItem;

            this.ConfigurationSpace.Invalidate();
        }

        private void ConfigurationSpace_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this.ConfigurationCanvas.IsDown = false;
                this.ConfigurationCanvas.IsDragging = false;
                DragTargetConfiguration = 0;
            }
        }

        private void ConfigurationSpace_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                //this.configuration.IsDragging = true;
                this.ConfigurationCanvas.IsDown = true;
            }
        }

        private void ConfigurationSpace_MouseMove(object sender, MouseEventArgs e) {
            SKPoint location = new SKPoint(e.Location.X, e.Location.Y);

            if (this.ConfigurationCanvas.IsDown) {
                if (!this.ConfigurationCanvas.IsDragging) {
                    int? idx = this.ConfigurationCanvas.IsInControlArea(location);
                    if (idx != null) {
                        this.ConfigurationCanvas.ControlPoints[(int) idx] = location;
                        this.ConfigurationCanvas.IsDragging = true;
                        DragTargetConfiguration = (int) idx;
                        
                        this.ConfigurationSpace.Invalidate();
                    }

                    //if (this.configuration.CheckInControlArea(location, out int idx)) {
                    //    this.configuration.ControlPoints[idx] = location;
                    //    this.configuration.IsDragging = true;
                    //    DragTargetConfiguration = idx;

                    //this.ConfigurationSpace.Invalidate();
                    //}
                }
                else {
                    this.ConfigurationCanvas.ControlPoints[DragTargetConfiguration] = location;

                    this.ConfigurationSpace.Invalidate();
                }
            }

        }

        private void ConfigurationSpace_PaintSurface1(object sender, SKPaintSurfaceEventArgs e) {
            this.ConfigurationCanvas.Draw(e.Surface.Canvas);

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
                for (int idx = 0; idx < lines.Length; idx++) {
                    if (idx > 1) {
                        var coordinates = lines[idx].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(e => Convert.ToSingle(e)).ToArray();
                        this.Chart.AddPointFromValue(coordinates[0], coordinates[1]);
                    }
                }

                Logger.Debug("MinValue: {0}", this.Chart.MinValue);
                Logger.Debug("MaxValue: {0}", this.Chart.MaxValue);
                //Console.WriteLine(this.chart.PrintEntries());
                TuggingController.Invalidate();
            }
            else if (fileName == "qdelaunay") {
                List<int[]> triangles = new List<int[]>();

                for (int idx = 0; idx < lines.Length; idx++) {
                    if (idx > 0) {
                        var vertices = lines[idx].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(e => Convert.ToInt32(e)).ToArray();
                        triangles.Add(vertices);
                    }
                }

                //Logger.Debug(triangles);

                this.Chart.Triangulate(triangles);
                TuggingController.Invalidate();
            }
            else if (fileName == "qconvex") {
                List<int> extremeIndexes = new List<int>();

                for (int idx = 1; idx < lines.Length; idx++) {
                    extremeIndexes.Add(Convert.ToInt32(lines[idx]));
                }

                //Logger.Debug(extremeIndexes);
                this.Chart.ConvexHull.SetExtremes(extremeIndexes.Select(idx => new Extreme() { Entry = this.Chart.Entries[idx], Index = idx }));
                //this.Chart.ConvexHull.Triangles = this.Chart.Triangles;
                TuggingController.Invalidate();
            }
            else if (fileName == "qdelaunayN") {
                Logger.Debug(data);
            }
        }
        private void Form1_SizeChanged(object sender, EventArgs e) {
            Point mid = new Point {
                X = this.groupbox1.Location.X / 2,
                Y = this.ClientSize.Height
            };

            TuggingController.Size = new Size(mid.X, mid.Y);
            ConfigurationSpace.Size = new Size(mid.X, mid.Y);
            this.Chart.forceUpdateScale = true;

            this.ConfigurationCanvas.CanvasSize = new SKSize(mid.X, mid.Y);
            TuggingController.Invalidate();
            ConfigurationSpace.Invalidate();
        }

        private void SkControl1_PaintSurface(object sender, SKPaintSurfaceEventArgs e) {
            Point mid = new Point {
                X = this.groupbox1.Location.X / 2,
                Y = this.ClientSize.Height
            };

            this.Chart.Draw(e.Surface, mid.X, mid.Y);
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
            //if (File.Exists("data.txt")) {
            //    File.Delete("data.txt");
            //}
            //using (FileStream fs = File.Create("data.txt")) {
            //    byte[] info = new UTF8Encoding(true).GetBytes(this.Chart.PrintEntries());
            //    fs.Write(info, 0, info.Length);
            //}

            // Test
            var flatPoints = this.Chart.Entries.Flatten();
            var triangles = this.Tri.RunDelaunay_v1(2, this.Chart.Entries.Count, ref flatPoints);

            this.UpdateTriangles(triangles);

            //this.Tri.RunDelaunay();
            //this.Tri.StartTask();
        }

        private void UpdateConvexHull(int[] extremeIndices) {
            var extremes = extremeIndices.Select(
                idx => new Extreme() {
                    Entry = this.Chart.Entries[idx],
                    Index = idx,
                    Parents = new SortedSet<Triangle>(
                        this.Chart.Triangles.Where(
                            tri => (tri.IsVertex(idx) != null))),
                }).ToArray();
            this.Chart.ConvexHull.SetExtremes(extremes);
            this.Chart.ConvexHull.Triangles = this.Chart.Triangles;

            TuggingController.Invalidate();
        }

        private void UpdateTriangles(List<int[]> triangles) {
            this.Chart.Triangulate(triangles);
            TuggingController.Invalidate();
        }

        private void RunConvexHullTask() {
            // Test
            var flatPoints = this.Chart.Entries.Flatten();
            var extremes = this.Tri.RunConvexHull_v1(2, this.Chart.Entries.Count, ref flatPoints);

            // #TODO
            this.UpdateConvexHull(extremes.ToArray());
            // Bug: Old data.txt will cause exception.
            //this.Tri.RunConvexHull();
            //this.Tri.StartTask();
        }

        private void RunDelaunayNTask() {
            this.Tri.RunDelaunayNeighbor();
            this.Tri.StartTask();
        }

        private void skControl1_MouseUp(object sender, MouseEventArgs e) {
            switch (e.Button) {
                case MouseButtons.Left:
                    //this.IsDragging = false;
                    if (!this.IsDragging & !this.IsDoubleClick) {
                        this.skControl1_MouseClick(sender, e);
                    } else if (this.IsDoubleClick) {
                        this.IsDoubleClick = false;
                    }

                    if (this.Chart.Entries.Count > 3) {
                        this.RunTriangulationTask();
                        this.RunConvexHullTask();
                    }
                    break;
                case MouseButtons.Right:
                    this.skControl1_MouseClick(sender, e);
                    break;
            }

            Logger.Debug("Detected a mouseup event.");



        }
        private void skControl1_MouseDoubleClick(object sender, MouseEventArgs e) {
            // Add new point

            switch (e.Button) {
                case MouseButtons.Left:
                    this.IsDoubleClick = true;
                    //if ()
                    Logger.Debug("Add new point");
                    Logger.Debug(e.Location);
                    this.Chart.AddPointFromGlobal(e.Location);
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
                    Entry target = this.Chart.IsInEntryArea(e.Location, 5);

                    //target!.isSelected = !target!.isSelected;
                    if (target != null) {
                        bool prevState = target.IsSelected;
                        this.Chart.Entries.ResetSelectedStates();
                        target.IsSelected = !prevState;
                    }
                    //if (this.chart.isInZone(e.Location, 5, out Entry target)) {
                    //    target.isSelected = !target.isSelected;
                    //}
                    break;
                case MouseButtons.Right:
                    this.Chart.TestPoint.IsDisplayed = !this.Chart.TestPoint.IsDisplayed;
                    this.Chart.TestPoint.UpdateFromGlobalLocation(new SKPoint(e.Location.X, e.Location.Y));
                    //TuggingController.Invalidate();
                    break;
                default:
                    break;
            }
        }

        private void skControl1_MouseMove(object sender, MouseEventArgs e) {
            this.Chart.PointerLocation = new SKPoint(e.Location.X, e.Location.Y);

            if (e.Button == MouseButtons.None) {
                this.IsDragging = false;
                //if (this.chart.isInZone(e.Location, 5, out _)) {
                //    this.chart.Hovered = true;
                //    TuggingController.Invalidate();
                //}
                if (this.Chart.IsInEntryArea(e.Location, 5) != null) {
                    this.Chart.Hovered = true;
                    TuggingController.Invalidate();
                }
                else {
                    this.Chart.Hovered = false;
                }

                if (this.Chart.IsInChartArea(e.Location)) {
                    this.Chart.hasIndicator = true;
                    TuggingController.Invalidate();
                }
                else {
                    this.Chart.hasIndicator = false;
                    TuggingController.Invalidate();
                }

                this.Chart.IsInTriangleArea(this.Chart.PointerLocation);
                //this.chart.IsInTriangle(this.chart.PointerLocation);
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
                    SKPoint targetLoc = SkiaHelper.ToSKPoint(e.Location);
                    //this.Chart.TestPoint.UpdateFromGlobalLocation(targetLoc);
                    this.Chart.TestPoint.UpdateFromGlobalLocation(targetLoc);
                    //Logger.Debug("{0} {1}", targetLoc.X, targetLoc.Y);

                    //Triangle[] collection = this.Chart.Triangles.Where(tri => tri.IsInside_v1(targetLoc, PointType.Global) != null);
                    Triangle[] collection = this.Chart.Triangles.Where(tri => tri.IsInside_Re(this.Chart.TestPoint.Value) != null).ToArray();

                    if (collection.Length != 0) {
                        var result = collection[0].Simplex_Re.GetInterpolatedConfigurationVector(SkiaHelper.ToVector(this.Chart.TestPoint.Value));

                        if (result != null) {
                            var rowArray = result.Vector.ToArray();
                            //var newControlPoints = new SKPoint[] {
                            //    new SKPoint(rowArray[0], rowArray[1]),
                            //    new SKPoint(rowArray[0 + 2], rowArray[1 + 2]),
                            //    new SKPoint(rowArray[0 + 4], rowArray[1 + 4]),
                            //    new SKPoint(rowArray[0 + 6], rowArray[1 + 6]),
                            //};

                            //this.ConfigurationCanvas.ControlPoints = newControlPoints;
                            this.Logger.Info($"{rowArray[0]} {rowArray[1]} {rowArray[2]}");
                            this.PCController.SetMotor(rowArray);
                        }

                        //Logger.Debug("{0} {1} {2} {3}", result.C1, result.C2, result.C3, result.C4);
                    }
                    //Logger.Debug("Dragging.");
                    TuggingController.Invalidate();
                    ConfigurationSpace.Invalidate();
                }
                else {
                    if (this.Chart.TestPoint.CheckIsInZone(e.Location, 10)) {
                        if (this.Chart.IsInChartArea(e.Location)) {
                            this.CurrentLocationOnDrag = e.Location;
                            this.IsDragging = true;
                            var targetLoc = new SKPoint(e.Location.X, e.Location.Y);
                            this.Chart.TestPoint.UpdateFromGlobalLocation(targetLoc);

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
                    this.DragTarget = this.Chart.IsInEntryArea(e.Location, 10);
                    //if (this.chart.isInZone(e.Location, 10, out this.DragTarget)) {
                    if (this.DragTarget != null) {
                        if (this.Chart.IsInChartArea(e.Location)) {
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

        private bool CreatePair() {
            
            Entry[] targets = this.Chart.Entries.Where(e => e.IsSelected).ToArray();

            if (targets.Length != 0) {
                var selectedEntry = targets[0];
                var currentConfiguration = this.ConfigurationCanvas.ControlPoints;
                //var currentConfigurationRobotVector = this.RobotConfiguration.ToArray();
                var currentConfigurationVector = this.RobotConfigurationSpace.Last();

                // Attention!: Array should be cloned in case of assigning a reference.
                selectedEntry.PairedConfig = (SKPoint[]) currentConfiguration.Clone();

                // [New Feature]: Testing
                //var currentConfigurationVector = ToConfigurationVector(currentConfiguration);
                //var currentConfigurationVector = new ConfigurationVector(currentConfigurationRobot);
                //selectedEntry.Pair = 
                //this.Chart.Triangles
                foreach(var tri in this.Chart.Triangles) {
                    var idx = tri.Vertices.GetEntryList().IndexOf(selectedEntry);

                    if (idx != -1) {
                        tri.Simplex_Re[idx].Config = currentConfigurationVector;
                        selectedEntry.Pair = tri.Simplex_Re[idx];
                    }
                }

                //this.Chart.Triangles.ForEach(tri => tri.UpdateSimplexState());

                selectedEntry.IsPaired = true;
                selectedEntry.IsSelected = false;

                // Refresh canvas
                TuggingController.Invalidate();

                return true;
            }
            else {
                return false;
            }
        }

        private ConfigurationVector ToConfigurationVector(SKPoint[] points) {
            var tmp = points.Select(p => new float[] { p.X, p.Y });
            var vectorElements = new List<float>();

            foreach(var element in tmp) {
                vectorElements.AddRange(element);
            }

            return new ConfigurationVector(vectorElements);
        }

        private void button2_Click(object sender, EventArgs e) {
            this.Chart.Initialize();
            this.Chart.forceUpdateScale = true;
            this.TuggingController.Invalidate();
        }

        private void button1_Click(object sender, EventArgs ev) {
            this.CreatePair();
        }

        private void button3_Click(object sender, EventArgs e) {
            this.Chart.SaveToFile();
        }

        private void bt_InvokePCController_Click(object sender, EventArgs e) {
            Button btn = (Button)sender;

            btn.Enabled = false;
            this.PCController = new MainForm();
            this.PCController.handler += this.ReceiveHandler;
            this.PCController.FormClosed += (s, ev) => { ((Button)sender).Enabled = true; };

            this.PCController.Show();
        }

        private void TuggingController_MouseWheel(object sender, MouseEventArgs e) {
            float scaleConstant = 0.01f;

            this.Chart.UpdateScale(scaleConstant * (e.Delta > 0 ? 1.0f : -1.0f));
            this.TuggingController.Invalidate();
        }
    }

    //public class RobotConfiguration : List<float> { }
    public class RobotConfigurationSpace : ConfigurationSpace {
        public override ConfigurationVector[] ToConfigurationVectorArray() {
            throw new NotImplementedException();
        }

        public void Add(float[] rawValues) {
            this.Add(new ConfigurationVector(rawValues));
        }
    }

    public class EntryCollection : List<Entry> {
        public EntryCollection() : base() { }
        
        public new void Add(Entry entry) {
            entry.Index = this.Count;
            base.Add(entry);
        }

        public void UpdateScale(SKMatrix scale) => this.ForEach(e => e.UpdateScale(scale));
        public void UpdateTransform(SKMatrix transform) => this.ForEach(e => e.UpdateTransform(transform));
        public void ResetHoverStates() => this.ForEach(e => e.IsHovered = false);
        public void ResetSelectedStates() => this.ForEach(e => e.IsSelected = false);


        public (Entry entry, float minValue) MinElement(Func<Entry, float> selector) {
            float? minValue = null;
            (Entry, float) ret = (null, 0.0f);

            foreach (var e in this) {
                if (minValue == null) {
                    minValue = selector(e);
                    ret = (e, (float)minValue);
                }
                else {
                    if (selector(e) <= minValue) {
                        minValue = selector(e);
                        ret = (e, (float)minValue);
                    }
                }
            }

            return ret;
        }

        public new void ForEach(Action<Entry> action) {
            for (int i = 0; i < this.Count; i++) {
                action(this[i]);
            }
        }

        public double[] Flatten() {
            var tmpList = new List<double>();

            this.ForEach(e => tmpList.AddRange(new double[] { e.Value.X, e.Value.Y }));

            return tmpList.ToArray();
        }
    }

    public class TriangleCollection : List<Triangle> {
        public TriangleCollection() : base() { }

        public void UpdateScale(SKMatrix scale) => this.ForEach(e => e.UpdateScale(scale));
        public void UpdateTransform(SKMatrix transform) => this.ForEach(e => e.UpdateTransform(transform));

        public Triangle[] GetTrianglesFromVertexIndices(int[] indices) {
            return this.Where(tri => tri.IsVertices(indices) != null).ToArray();
        }

        //public Triangle[] Where(Func<Triangle, bool> predicate) {
        //    var ret = new List<Triangle>();
        //    foreach (var tri in this) {
        //        if (predicate(tri))
        //            ret.Add(tri);
        //    }

        //    return ret.ToArray();
        //}
        //public void ForEach(Action<Triangle> action) {
        //    for (int i = 0; i < this.Count; i++) {
        //        action(this[i]);
        //    }
        //}
    }

    public abstract class Chart {
        #region Properties
        public List<Axis> Axes { get; set; } = new List<Axis>();
        public EntryCollection Entries { get; set; } = new EntryCollection();
        public TriangleCollection Triangles { get; set; } = new TriangleCollection();
        public ConvexHull ConvexHull { get; set; } = new ConvexHull();
        public TestPoint TestPoint { get; set; } = new TestPoint();
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
        public float Padding { get; set; }
        public float LabelTextSize { get; set; } = 16;
        public SKRect ChartArea {
            get {
                return new SKRect {
                    Size = new SKSize(this.width - this.Margin * 2, this.height - this.Margin * 2),
                    Location = new SKPoint(this.Margin, this.Margin)
                };
            }
        }
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
        public SKMatrix Translation;
        public SKMatrix InverseTranslation;
        public SKMatrix Scale;
        public SKMatrix InverseScale;
        public Transform ChartTransform {
            get {
                var scale = SKMatrix.MakeScale(1.0f, -1.0f);
                // Height of SKRect is signed.
                var translation = SKMatrix.MakeTranslation(this.Margin, Math.Abs(this.ChartArea.Height) + this.Margin);

                return new Transform() {
                    Scale = scale,
                    Translation = translation
                };
            }
        }

        protected readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Methods

        public Chart() {
            //this.Scale = SKMatrix.MakeScale(1.0f, -1.0f);
            //// Height of SKRect is signed.
            //this.Translation = SKMatrix.MakeTranslation(this.Margin, Math.Abs(this.ChartArea.Height));
            //this.ChartTransform = new TransformObject() { 
            //    Scale = this.Scale, Translation = this.Translation 
            //};
        }

        // TODO: Calculation still has some problems.
        protected void CalculateSize() {
            if (this.Entries.Count > 0) {
                SKSize margin = new SKSize() { Height = (this.MaxValue.X - this.MinValue.X) * 0.1f, Width = (this.MaxValue.Y - this.MinValue.Y) * 0.1f };
                //this.RangeOfValue = new SKRect(this.MinValue.X, this.MaxValue.Y, this.MaxValue.X, this.MinValue.Y);
                this.RangeOfValue = new SKRect(this.MinValue.X - margin.Width, this.MaxValue.Y + margin.Height, this.MaxValue.X + margin.Width , this.MinValue.Y - margin.Height);
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
            int expR = Math.Abs(right) != 0 ? Convert.ToInt32(Math.Truncate(Math.Log10(Math.Abs(right)))) : 0;

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
            SKMatrix.PreConcat(ref scale, SKMatrix.MakeScale(this.RangeOfInflatedValue.Width / this.ChartArea.Width, Math.Abs(this.RangeOfInflatedValue.Height) / this.ChartArea.Height));
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

        public void UpdateScale(float scaleFactor) {
            SKMatrix scale = this.Scale;

            SKMatrix.PreConcat(ref scale, SKMatrix.MakeScale(scaleFactor, scaleFactor));

            this.Scale = scale;
            this.Scale.TryInvert(out this.InverseScale);

            this.Entries.UpdateScale(this.InverseScale);
            this.Triangles.UpdateScale(this.InverseScale);
            this.TestPoint.UpdateScale(this.InverseScale);
        }

        private void UpdateChartSize(int width, int height) {
            if (width != this.width | height != this.height) {
                this.width = width;
                this.height = height;
            }
        }
        public void Draw(SKSurface surface, int width, int height) {
            this.Surface = surface;
            this.width = width;
            this.height = height;
            this.SavedBitmap = new SKBitmap(width, height);
            this.Canvas = new SKCanvas(SavedBitmap);
            //this.Canvas = surface.Canvas;

            //ChartArea = new SKRect {
            //    Size = new SKSize(this.width - this.Margin * 2, this.height - this.Margin * 2),
            //    Location = new SKPoint(this.Margin, this.Margin)
            //};

            //frameArea = new SKRect {
            //    Size = new SKSize(this.width, this.height)
            //};



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
            if (this.forceUpdateScale) {
                // Calculation
                this.CalculateSize();
                this.UpdateScale();

                this.Entries.UpdateScale(this.InverseScale);
                this.Triangles.UpdateScale(this.InverseScale);
                this.TestPoint.UpdateScale(this.InverseScale);

                //this.Entries.ForEach(e => e.UpdateScale(this.InverseScale));
                //this.Triangles.ForEach(e => e.UpdateScale(this.InverseScale));

                this.forceUpdateScale = !this.forceUpdateScale;
            }
            // Update data

            // TODO: Transformation didn't match to each other.
            this.UpdateTransform();

            this.Entries.UpdateTransform(this.InverseTransform);
            this.Triangles.UpdateTransform(this.InverseTransform);
            //this.Triangles.ForEach(t => t.UpdateTransform(this.InverseTransform));
            this.TestPoint.UpdateTransform(this.InverseTransform);

            //this.Entries.ForEach(e => e.UpdateTransform(this.InverseTransform));

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

    public class PointChart : Chart {
        #region Properties
        public List<SimplicialComplex> Complices { get; set; } = new List<SimplicialComplex>();
        #endregion

        #region Methods

        public PointChart() : base() {
            //this.CalculateSize();
        }

        private double DegreeToRadian(double degree) {
            return Math.PI * degree / 180.0;
        }

        public void Initialize() {
            //this.Entries = new EntryCollection() {
            //    new Entry(new SKPoint(0, 0)),
            //    new Entry(new SKPoint(1, 0)),
            //    new Entry(new SKPoint(0, 1)),
            //};

            this.Entries.Clear();
            this.AddPointFromValue(0.0f, 0.0f);
            this.AddPointFromValue(1.0f, 0.0f);
            this.AddPointFromValue(0.0f, 1.0f);

            this.Triangulate(new List<int[]>() { new int[] { 0, 1, 2 } });
        }

        public Triangle IsInTriangleArea(SKPoint pointerLocation) {
            //SKPoint value = this.Scale.MapPoint(this.Transform.MapPoint(SkiaHelper.ToSKPoint(pointerLocation)));

            Triangle[] retTri = this.Triangles.Where(t => t.IsInsideFromGlobalLocation(pointerLocation) != null).ToArray();
            return retTri.Length != 0 ? retTri[0] : null;
        }

        //public bool isInZone(Point pointerLocation, float radius, out Entry target) {
        //    var pos = new SKPoint(pointerLocation.X, pointerLocation.Y);
        //    var ret = false;

        //    target = null;
        //    foreach (var (e, i) in this.Entries.Select((e, i) => (e, i))) {
        //        var dist = SKPoint.Distance(pos, e.GlobalLocation);
        //        if (dist <= radius) {
        //            e.isHovered = true;
        //            ret = true;
        //            target = e;
        //        }
        //        else {
        //            e.isHovered = false;
        //        }
        //    }

        //    return ret;
        //}

        public Entry IsInEntryArea(Point pointerLoc, float radius) {
            SKPoint location = SkiaHelper.ToSKPoint(pointerLoc);

            this.Entries.ResetHoverStates();

            (Entry entry, float minValue) candidate = this.Entries.MinElement(e => SKPoint.Distance(location, e.GlobalLocation));
            Entry ret = candidate.minValue <= radius ? candidate.entry : null;

            if (ret != null) {
                ret.IsHovered = true;
            }
            return ret;

        }

        public bool IsInChartArea(Point pointerLocation) {
            SKPoint relativeLocation = this.Transform.MapPoint(SkiaHelper.ToSKPoint(pointerLocation));
            return (relativeLocation.X <= this.ChartArea.Width & relativeLocation.X >= 0) && (relativeLocation.Y <= this.ChartArea.Height & relativeLocation.Y >= 0);
        }

        public override void DrawHover(Chart ctx) {
            this.Entries.ForEach(e => e.DrawHover(this.Canvas));
            //Hover.DrawHover(this.Canvas, ctx.PointerLocation);
            //foreach (var e in this.Entries) {
            //    e.DrawHover(this.Canvas);
            //}
        }

        public override void DrawIndicator() {
            this.DrawCross(this.Canvas, this.ChartArea, this.Transform.MapPoint(this.PointerLocation), this.InverseTransform);
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
            SKMatrix transform = SKMatrix.MakeTranslation(-this.Margin, (this.ChartArea.Height + this.Margin));
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
            var yMax = new SKPoint(0, this.ChartArea.Height);
            var xMax = new SKPoint(this.ChartArea.Width, 0);

            var canvasRect = new SKRect() {
                Size = new SKSize(this.ChartArea.Width, this.ChartArea.Height)
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
                this.Axes.Add(new Axis("X", this.ChartArea.Width, this.InverseTransform));
                this.Axes.Add(new Axis("Y", this.ChartArea.Height, this.InverseTransform));
            }
            else {
                //this.Axes.Add(new Axis("X", this.MinValue.X, this.MaxValue.X, this.chartArea.Width, this.Transform));
                //this.Axes.Add(new Axis("Y", this.MinValue.Y, this.MaxValue.Y, this.chartArea.Height, this.Transform));
                this.Axes.Add(new Axis("X", this.RangeOfInflatedValue.Left, this.RangeOfInflatedValue.Right, this.ChartArea.Width, this.InverseTransform));
                this.Axes.Add(new Axis("Y", this.RangeOfInflatedValue.Bottom, this.RangeOfInflatedValue.Top, this.ChartArea.Height, this.InverseTransform));
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
            foreach (var a in this.Axes) {
                a.DrawAxis(this.Canvas);
            }
        }

        // TODO: [Bug] Sync task may cause collections modified exception.
        public override void DrawContent(object ctx) {
            //if (this.Entries.Count == 3) {
            //    this.Triangulate(new List<int[]>() { new int[] { 0, 1, 2 } });
            //}

            if (this.Triangles.Count > 0) {
                this.Triangles.ForEach(tri => tri.Draw(this.Canvas));
            }

            if (this.Entries.Count > 0) {
                this.Entries.ForEach(e => e.Draw(this.Canvas));
            }

            this.ConvexHull.Draw(this.Canvas, this.ChartArea);
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
        public void AddPointFromGlobal(Point point) {
            //this.Points.Add(new SKPoint(point.X, point.Y));
            //this.Entries.Add(new Entry(this.InverseTransform.MapPoint(new SKPoint(point.X, point.Y)), this.Transform));

            var tPoint = new SKPoint(point.X, point.Y);

            Logger.Debug($"New Point: G - {tPoint}, V - {this.ChartTransform.InverseMapPoint(tPoint)}");
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

            // Test
            //this.Entries.Add(new Entry(tPoint, this.ChartTransform));
            Logger.Debug($"New Point: V - {tPoint}, G - {this.ChartTransform.MapPoint(tPoint)}");

        }

        public void Triangulate(List<int[]> triangles) {
            this.Triangles.Clear();

            foreach (var t in triangles) {
                //SKPoint[] vertices = new SKPoint[] { this.Entries[t[0]].Location, this.Entries[t[1]].Location, this.Entries[t[2]].Location };
                int[] verticeIndexes = new int[] {
                    t[0], t[1], t[2]
                };

                this.Triangles.Add(new Triangle(new Entry[] { this.Entries[t[0]], this.Entries[t[1]], this.Entries[t[2]] }, verticeIndexes, this.InverseScale, this.InverseTransform));
                //var complex = new SimplicialComplex();

                //complex.CreateSimplex(vertices);
                //this.Complices.Add(complex);
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
            Logger.Debug("Frame Size: {0} {1}", this.ChartArea.Width, this.ChartArea.Height);

            this.Canvas.DrawRect(this.frameArea, paint);
            paint.Color = SKColors.Blue;
            this.Canvas.DrawRect(this.ChartArea, paint);
        }

        #endregion
    }

    public class Hover {
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Hover() { }

        public static void DrawHover(SKCanvas canvas, SKPoint value, int idx, SKPoint location, SKMatrix transform) {
            var text = $"{idx} - {{ {value.X:F4}, {value.Y:F4} }}";
            var textPaint = new SKPaint {
                Color = SKColors.White
            };
            var bounds = new SKRect();

            textPaint.MeasureText(text, ref bounds);

            var anchor = transform.MapPoint(location);
            var offset = new SKPoint(10, -25);
            anchor += offset;
            var rect = new SKRect(anchor.X, anchor.Y, anchor.X + bounds.Width + offset.X, anchor.Y + bounds.Height * 2);
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
            canvas.DrawText(text, anchor + new SKSize(5, 15), textPaint);
            //canvas.DrawRect(rect, strokePaint);
        }
    }

    public struct TriVertex {
        public Entry Entry { get; set; }
        public int Index { get; set; }
        public Triangle Parent { get; set; }
    }

    [Serializable]
    public class TriVertexCollection : List<TriVertex> {
        public TriVertexCollection(Entry[] entries, int[] indexes, Triangle parent) : base() {
            this.Add(
                new TriVertex {
                    Entry = entries[0], Index = indexes[0], Parent = parent
                });
            this.Add(
                new TriVertex {
                    Entry = entries[1], Index = indexes[1], Parent = parent
                });
            this.Add(
                new TriVertex {
                    Entry = entries[2], Index = indexes[2], Parent = parent
                });
        }
    
        public SKPoint[] ToGlobalLocations() => this.Select(v => v.Entry.GlobalLocation).ToArray();

        public SKPoint[] ToValues() => this.Select(v => v.Entry.Value).ToArray();

        public List<Entry> GetEntryList() => this.Select(v => v.Entry).ToList();
        //public TriVertex this[string idx] {
        //    get {
        //        return this.Vertices[idx];
        //    }
        //    set {
        //        this.Vertices[idx] = value;
        //    }
        //}
        public TriVertex? Any(int targetIdx) {
            var ret = this.Where(value => value.Index == targetIdx).ToArray();

            return ret.Length > 0 ? ret[0] : (TriVertex?)null;
        }

        public void ForEach(Action<Entry> action) {
            Entry[] all = this.Select(triVertex => triVertex.Entry).ToArray();
            Array.ForEach(all, action);
        }

    };

    public enum PointType {
        Value,
        Local,
        Global
    }

    public class ScalableCanvasObject : CanvasObject, IScalable {
        public SKMatrix Scale { get; set; }

        protected ScalableCanvasObject(SKMatrix scale, SKMatrix transform) : base(new SKPoint(0, 0), transform) {
            this.Scale = scale;
        }

        protected ScalableCanvasObject(SKPoint location, SKMatrix transform, SKMatrix scale) : base(location, transform) {
            this.Scale = scale;
        }
        public virtual void UpdateScale(SKMatrix scale) => this.Scale = scale;

        public override void Draw(SKCanvas canvas) {
            throw new NotImplementedException();
        }

        public override CanvasObject Clone() {
            throw new NotImplementedException();
        }
    }

    public interface IScalable {
        SKMatrix Scale { get; set; }
        void UpdateScale(SKMatrix scale);
    }

    public struct Extreme {
        public SortedSet<Triangle> Parents;
        public Entry Entry;
        public int Index;
        
    }

    public class ExtremeCollection : List<Extreme> {
        public ExtremeCollection() : base() { }
        public ExtremeCollection(Extreme[] extremes) {
            this.AddRange(extremes);
        }

        public SKPoint[] ToGlobalLocations() {
            return this.Select(e => e.Entry.GlobalLocation).ToArray();
        }

        public Extreme PrevElement(int idx) {
            return this[idx - 1 < 0 ? this.Count - 1 : idx - 1];
        }
        public Extreme NextElement(int idx) {
            return this[idx + 1 > this.Count - 1 ? 0 : idx + 1];
        }
    }

    public class EdgeCollection<T> : List<Edge<Triangle>> {
        public T Parent { get; set; }
        public EdgeCollection() : base() { }
        public EdgeCollection(T parent) {
            this.Parent = parent;
        }

        public void Add(Extreme[] extremes) {
            for (var idx = 0; idx < extremes.Length; idx++) {
                var extremeLeft = extremes[idx];
                var extremeRight = extremes[(idx + 1) == extremes.Length ? 0 : idx + 1];
                var edge = new Edge<Triangle>(extremeLeft.Entry, extremeRight.Entry, extremeLeft.Parents.Intersect(extremeRight.Parents), ((float)idx) / ((float)extremes.Length - 1.0f));

                this.Add(edge);
            }
        }
    }

    public class RidgeCollection<T> : List<Ridge<Triangle>> {
        public T Parent { get; set; }

        public RidgeCollection(T parent) {
            this.Parent = parent;
        }

        public void Add(Edge<Triangle>[] edges){
            for (var idx = 0; idx < edges.Length; idx++) {
                var edge = edges[idx];
                var triangles = edge.Parents;
                var enumerator = triangles.Select((tri, index) => new { Value = tri, Index = index });

                foreach (var tri in enumerator) {
                    var restVertex = tri.Value.Vertices.Where(
                        v => !edge.Start.Equals(v.Entry) & !edge.End.Equals(v.Entry)).ToArray()[0];
                    var ridgeLeft = new Ridge<Triangle>(
                        restVertex.Entry, edge.Start,
                        new Triangle[] { tri.Value },
                        tri.Index / triangles.Count);
                    var ridgeRight = new Ridge<Triangle>(
                        restVertex.Entry, edge.End,
                        new Triangle[] { tri.Value }, 
                        tri.Index / triangles.Count);
                    Func<Edge<Triangle>, bool> evaluateFunc(Ridge<Triangle> r) =>
                        (e) => {
                            var vertexSet = new HashSet<Entry> {
                                e.Start,
                                e.End
                            };

                            return !vertexSet.SetEquals(r.Vertices);
                    };

                    if (edges.All(evaluateFunc(ridgeLeft))) {
                        this.Add(ridgeLeft);
                    }

                    if (edges.All(evaluateFunc(ridgeRight))) {
                        this.Add(ridgeRight);
                    }
                }
            }
        }

        public new void Add(Ridge<Triangle> ridge) {
            var duplicates = this.FindAll(r => r.HasSameVertices(ridge));

            if (duplicates.Count == 0) {
                base.Add(ridge);
            }
        }
    }

    public class Ridge<T> : Edge<T> {
        public Ridge(Entry start, Entry end, IEnumerable<T> parents, float colorFactor) : base(start, end, parents, colorFactor) { }

        public bool HasSameVertices(Ridge<T> ridge) {
            return this.Vertices.Equals(ridge.Vertices);
        }

        public override void Draw(SKCanvas canvas) {
            this.DrawCentroid(canvas);
            this.DrawExtensionLine(canvas);
        }

        private void DrawCentroid(SKCanvas canvas) {
            var radius = 3.0f;
            var fillPaint = new SKPaint {
                IsAntialias = true,
                Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Cyan, 0.8f),
                Style = SKPaintStyle.Fill
            };
            var strokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };

            // Draw entry shape
            canvas.DrawCircle(this.Centroid.Value.GlobalLocation, radius, fillPaint);
            canvas.DrawCircle(this.Centroid.Value.GlobalLocation, radius, strokePaint);
        }

        private void DrawExtensionLine(SKCanvas canvas) {
            //var startRayV0 = SkiaHelper.ConvertVectorToSKPoint(this.StartRay.V0);
            //var startRayV1 = SkiaHelper.ConvertVectorToSKPoint(this.StartRay.GetNewPointOnLine(1000.0f));

            var endRayV0 = SkiaHelper.ConvertVectorToSKPoint(this.EndRay.V0);
            var endRayV1 = SkiaHelper.ConvertVectorToSKPoint(this.EndRay.GetNewPointOnLine(1000.0f));

            var endRayStrokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.DarkSalmon,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            //var startRayStrokePaint = new SKPaint {
            //    IsAntialias = true,
            //    Color = SKColors.DarkViolet,
            //    Style = SKPaintStyle.Stroke,
            //    StrokeWidth = 2
            //};

            //canvas.DrawLine(startRayV0, startRayV1, startRayStrokePaint);
            canvas.DrawLine(endRayV0, endRayV1, endRayStrokePaint);
        }
    }

    public class Polygon {
        public SKPoint[] Vertices { get; set; }
        public Polygon() { }

        public static bool IsPointInPolygon(Point p, Point[] polygon) {
            double minX = polygon[0].X;
            double maxX = polygon[0].X;
            double minY = polygon[0].Y;
            double maxY = polygon[0].Y;
            for (int i = 1; i < polygon.Length; i++) {
                Point q = polygon[i];
                minX = Math.Min(q.X, minX);
                maxX = Math.Max(q.X, maxX);
                minY = Math.Min(q.Y, minY);
                maxY = Math.Max(q.Y, maxY);
            }

            if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY) {
                return false;
            }

            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++) {
                if ((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y) &&
                     p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X) {
                    inside = !inside;
                }
            }

            return inside;
        }
    }

    public readonly struct ColorGradient {
        public int RMax { get; }
        public int GMax { get; }
        public int BMax { get; }
        public int RMin { get; }
        public int GMin { get; }
        public int BMin { get; }

        public ColorGradient(int[] rgbMax, int[] rgbMin) {
            this.RMax = rgbMax[0];
            this.GMax = rgbMax[1];
            this.BMax = rgbMax[2];
            this.RMin = rgbMin[0];
            this.GMin = rgbMin[1];
            this.BMin = rgbMin[2];
        }
    }
    public readonly struct ColorGradients {
        public static SKColor GetSKColor(float factor, ColorGradient gradient) {
            if (factor > 1 | factor < 0) {
                throw new Exception("Incorrect factor to get SKColor");
            }

            Func<int, int, byte> colorEquation = (int min, int max) => Convert.ToByte(min + (max - min) * factor);
            var newR = colorEquation(gradient.RMin, gradient.RMax);
            var newG = colorEquation(gradient.GMin, gradient.GMax);
            var newB = colorEquation(gradient.BMin, gradient.BMax);

            return new SKColor(newR, newG, newB);
        }
        
        public static ColorGradient ShroomHaze = new ColorGradient(
            new int[] { 0x5c, 0x25, 0x8d }, new int[] { 0x43, 0x89, 0xa2 });
        public static ColorGradient GrapefruitSunset = new ColorGradient(
            new int[] { 0xe9, 0x64, 0x43 }, new int[] { 0x90, 0x4e, 0x95 });
    }

    public class Edge<T> {
        public SKColor Color { get; set; }
        public SortedSet<T> Parents { get; set; }
        public List<Entry> Vertices => new List<Entry>(new Entry[] { this.Start, this.End });
        public SkiaHelper.Ray StartRay {
            get {
                var centerToStart = new SkiaHelper.LineSegment(
                    SkiaHelper.ConvertSKPointToVector(this.Center),
                    SkiaHelper.ConvertSKPointToVector(this.Start.GlobalLocation));

                return SkiaHelper.Ray.CreateRay(centerToStart.V1, centerToStart.Direction);
            }
        }
        public SkiaHelper.Ray EndRay {
            get {
                var centerToEnd = new SkiaHelper.LineSegment(
                    SkiaHelper.ConvertSKPointToVector(this.Center),
                    SkiaHelper.ConvertSKPointToVector(this.End.GlobalLocation));

                return SkiaHelper.Ray.CreateRay(centerToEnd.V1, centerToEnd.Direction);
            }
        }
        public Entry Start { get; set; }
        public Entry End { get; set; }
        public Lazy<Entry> Centroid => new Lazy<Entry>(() => (this.End - this.Start) / 2.0f + this.Start);

        public SKPoint Center {
            get {
                var start = this.Start.GlobalLocation;
                var end = this.End.GlobalLocation;
                var size = new SKSize((end.X - start.X) / 2, (end.Y - start.Y) / 2);

                return new SKPoint(start.X + size.Width, start.Y + size.Height);
            }
        }

        public Edge() { }

        public Edge(Entry start, Entry end, IEnumerable<T> parents, float colorFactor) {
            this.Start = start;
            this.End = end;
            this.Parents = new SortedSet<T>(parents);
            this.Color = ColorGradients.GetSKColor(colorFactor, ColorGradients.GrapefruitSunset);



        }

        public bool Equals(Edge<T> obj) {
            return this.Parents.Equals(obj.Parents) & this.Start.Equals(obj.Start) & this.End.Equals(obj.End);
        }

        private void FindIntersectionPoint() {

        }
        private void DrawExtensionLine(SKCanvas canvas) {
            var startRayV0 = SkiaHelper.ConvertVectorToSKPoint(this.StartRay.V0);
            var startRayV1 = SkiaHelper.ConvertVectorToSKPoint(this.StartRay.GetNewPointOnLine(1000.0f));

            var endRayV0 = SkiaHelper.ConvertVectorToSKPoint(this.EndRay.V0);
            var endRayV1 = SkiaHelper.ConvertVectorToSKPoint(this.EndRay.GetNewPointOnLine(1000.0f));

            var endRayStrokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Chocolate,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            var startRayStrokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.DarkTurquoise,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            canvas.DrawLine(startRayV0, startRayV1, startRayStrokePaint);
            canvas.DrawLine(endRayV0, endRayV1, endRayStrokePaint);
        }

        public virtual void Draw(SKCanvas canvas) {
            this.DrawEdge(canvas);
            this.DrawCenter(canvas);
            //this.DrawExtensionLine(canvas);
        }

        private void DrawCenter(SKCanvas canvas) {
            var radius = 3.0f;
            var fillPaint = new SKPaint {
                IsAntialias = true,
                //Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Azure, 0.8f),
                Color = this.Color,
                Style = SKPaintStyle.Fill
            };
            var strokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            // Draw entry shape
            canvas.DrawCircle(this.Center, radius, fillPaint);
            canvas.DrawCircle(this.Center, radius, strokePaint);
        }

        private void DrawEdge(SKCanvas canvas) {
            var strokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Purple,
                //Color = this.Color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            var path = new SKPath();

            path.MoveTo(this.Start.GlobalLocation);
            path.LineTo(this.End.GlobalLocation);

            canvas.DrawPath(path, strokePaint);
        }
        
    }

    public class ConvexHull : ScalableCanvasObject {

        public TriangleCollection Triangles { get; set; }
        public ExtremeCollection Extremes { get; set; } = new ExtremeCollection();
        public EdgeCollection<ConvexHull> Edges { get; set; }
        public RidgeCollection<ConvexHull> Ridges { get; set; }


        public ConvexHull() : this(Array.Empty<Extreme>()) { }
        public ConvexHull(Extreme[] extremes): this(extremes, new SKPoint(), SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity()) { }
        public ConvexHull(Extreme[] extremes, SKPoint location, SKMatrix transform, SKMatrix scale) : base(location, transform, scale) {
            this.Extremes = new ExtremeCollection(extremes);
            this.Edges = new EdgeCollection<ConvexHull>(this);
            this.Ridges = new RidgeCollection<ConvexHull>(this);

        }
        
        //private void GetNeighborTriangles() {
        //    for (int idx = 0; idx < this.Extremes.Count; idx++) {
        //        Triangle tri1 = this.Triangles.Where(tri => tri.IsVertex(this.Extremes[idx].Index, this.Extremes.PrevElement(idx).Index) != null)[0];
        //        Triangle tri2 = this.Triangles.Where(tri => tri.IsVertex(this.Extremes[idx].Index, this.Extremes.NextElement(idx).Index) != null)[0];

        //        if (tri1 == tri2) {
        //            Logger.Debug("{0} - Single extreme.", this.Extremes[idx].Index);
        //        }
        //        else if (tri1.VertexIndexes.Where(i => !new int[] { this.Extremes[idx].Index, this.Extremes.PrevElement(idx).Index }.Contains(i)).ToArray()[0] == tri2.VertexIndexes.Where(i => !new int[] { this.Extremes[idx].Index, this.Extremes.NextElement(idx).Index }.Contains(i)).ToArray()[0]) {
        //            Logger.Debug("{0} - Two triangle One Extreme.", this.Extremes[idx].Index);
        //        }
        //        else {
        //            Logger.Debug("{0} - More than 2 triangles 1 extreme", this.Extremes[idx].Index);
        //        }
        //    }
        //}

        public void SetExtremes(IEnumerable<Extreme> extremes) {
            this.Extremes.Clear();
            this.Edges.Clear();
            this.Ridges.Clear();

            this.Extremes.AddRange(extremes);
            this.Edges.Add(this.Extremes.ToArray());
            this.Ridges.Add(this.Edges.ToArray());
        }

        private SKPoint GetCentroid(SKPoint[] vertices) {
            return new SKPoint() {
                X = (vertices[0].X + vertices[1].X + vertices[2].X) / 3.0f,
                Y = (vertices[0].Y + vertices[1].Y + vertices[2].Y) / 3.0f
            };
        }

        private void DrawRay(SKCanvas canvas, SKPoint centroid, SKPoint extreme, SKRect area, byte offset) {
            SKPaint rayPaint = new SKPaint {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = new SKColor((byte) (0xFF), (byte)(0x00 + offset), (byte)(0x00 + offset * 8)),
                StrokeWidth = 2,
                StrokeCap = SKStrokeCap.Butt,
                PathEffect = SKPathEffect.CreateDash(new float[] { 10.0f, 10.0f }, 10)
            };

            SkiaHelper.DrawRay(canvas, new SkiaHelper.SKRay() { Start = centroid, Direction = SKPoint.Normalize(extreme - centroid) }, area, rayPaint);
        }

        private void DrawCentroid(SKCanvas canvas, SKPoint centroid) {
            float radius = 2;
            var fillPaint = new SKPaint {
                IsAntialias = true,
                Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DarkRed, 0.8f),
                Style = SKPaintStyle.Fill
            };
            var strokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.MediumVioletRed,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };

            // Draw entry shape
            canvas.DrawCircle(centroid, radius, fillPaint);
            canvas.DrawCircle(centroid, radius, strokePaint);
        }
        //public override void Draw(SKCanvas canvas) {
        //    throw new NotImplementedException();
        //}
        public void Draw(SKCanvas canvas, SKRect area) {

            //if (this.Extremes.Count < 3)
            //    return;
            //var fillPaint = new SKPaint {
            //    IsAntialias = true,
            //    //Color = SKColors.ForestGreen,
            //    Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DimGray, 0.3f),
            //    Style = SKPaintStyle.Fill
            //};
            //var strokePaint = new SKPaint {
            //    IsAntialias = true,
            //    Color = SKColors.Purple,
            //    Style = SKPaintStyle.Stroke,
            //    StrokeWidth = 1
            //};
            //var path = new SKPath();
            //SKPoint[] gExtremes = this.Extremes.ToGlobalLocations();

            //path.MoveTo(gExtremes[0]);

            //for (int idx = 1; idx < gExtremes.Length; idx++) {
            //    path.LineTo(gExtremes[idx]);
            //}

            //path.LineTo(gExtremes[0]);

            //canvas.DrawPath(path, strokePaint);

            this.Edges.ForEach(e => e.Draw(canvas));
            this.Ridges.ForEach(r => r.Draw(canvas));
            //this.GetNeighborTriangles();
            //SkiaHelper.DrawRay(canvas, new SkiaHelper.SKRay() { Start = this.Extremes[0].Entry.GlobalLocation, Direction = SKPoint.Normalize(new SKPoint(1, 1)) }, area);
            //this.Centroids.ToList().ForEach(c => this.DrawCentroid(canvas, c));

            //for (int idx = 0; idx < this.Extremes.Count; idx ++) {
            //    this.DrawRay(canvas, this.Centroids[idx], this.Extremes[idx].GlobalLocation, area, (byte) (idx * 10));
            //}

        }
    }

    public class Triangle : ScalableCanvasObject, IComparable {
        public bool IsHovered { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public bool IsSet {
            get => this.Simplex.IsSet;
        }

        // Vertex [Value]
        public TriVertexCollection Vertices { get; set; }
        public Simplex Simplex_Re { get; set; }
        public SimplicialComplex Simplex { get; set; } = new SimplicialComplex();
        public int[] VertexIndexes { get; set; }

        public Triangle(Entry[] vertices) : this(vertices, new int[]{ 0, 1, 2 }, new SKPoint(0, 0), SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity()) { }

        public Triangle(Entry[] vertices, int[] vertexIndexes, SKMatrix scale, SKMatrix transform) : this(vertices, vertexIndexes, new SKPoint(0, 0), scale, transform) {
            this.UpdateSimplexState();
        }

        public Triangle(Entry[] vertices, int[] vertexIndexes, SKPoint location, SKMatrix scale, SKMatrix transform) : base(location, transform, scale) {
            //this.Vertices = vertices;
            this.Vertices = new TriVertexCollection(vertices, vertexIndexes, this);
            this.VertexIndexes = vertexIndexes;

            // [New Feature]: Testing
            var states = this.Vertices.ToValues();
            this.Simplex_Re = new Simplex(
                SkiaHelper.ToVectorArray(states).Select(
                    v => new StateVector(v)
                ).ToArray()
            );
        }

        public Triangle IsVertex(int targetIdx) => this.Vertices.Any(targetIdx).HasValue ? this : null;

        public Triangle IsVertex(int i1, int i2) => this.Vertices.Any(i1).HasValue & this.Vertices.Any(i2).HasValue ? this : null;

        public Triangle IsVertices(int[] indices) => indices.All(idx => this.Vertices.Any(idx).HasValue) ? this : null;

        public Triangle IsInsideFromGlobalLocation(SKPoint gTarget) => this.IsInside_v1(gTarget, PointType.Global);
        //public Triangle IsInsideFromLocation(SKPoint lTarget) => this.IsInside_v1(lTarget, PointType.Local);
        //public Triangle IsInsideFromValue(SKPoint vTarget) => this.IsInside_v1(vTarget, PointType.Value);

        public Triangle IsInside_v1(SKPoint target, PointType type) {
            string propertyName = "";

            switch (type) {
                case PointType.Value:
                    propertyName = "Value";
                    break;
                case PointType.Local:
                    propertyName = "Location";
                    break;
                case PointType.Global:
                    propertyName = "GlobalLocation";
                    break;
            }

            var barycentricCoordinate = SimplicialComplex.GetBarycentricCoordinate(
                    target,
                    new SimplicialComplex.Simplex3I {
                        V1 = (SKPoint)typeof(Entry).GetProperty(propertyName).GetValue(this.Vertices[0].Entry),
                        V2 = (SKPoint)typeof(Entry).GetProperty(propertyName).GetValue(this.Vertices[1].Entry),
                        V3 = (SKPoint)typeof(Entry).GetProperty(propertyName).GetValue(this.Vertices[2].Entry)
                    });
            this.IsHovered = barycentricCoordinate.IsInside;

            return barycentricCoordinate.IsInside ? this : null;
        }

        public Triangle IsInside_Re(SKPoint target) {
            this.IsHovered = this.Simplex_Re.IsInside(SkiaHelper.ToVector(target));

            return this.IsHovered ? this : null;
        }

        //public void UpdateSimplexState() {
            //this.Simplex.CreatePair_v1(entry);
            //if (this.Vertices["A"].Entry.PairedConfig != null) {
            //    this.Simplex.CreatePair_v1(this.Vertices["A"].Entry, "A");
            //}
            //if (this.Vertices["B"].Entry.PairedConfig != null) {
            //    this.Simplex.CreatePair_v1(this.Vertices["B"].Entry, "B");
            //}
            //if (this.Vertices["C"].Entry.PairedConfig != null) {
            //    this.Simplex.CreatePair_v1(this.Vertices["C"].Entry, "C");
            //}

            //this.Vertices.ForEach(e => { if (e.PairedConfig != null) { this.Simplex.CreatePair_v1(e)}; });
        //}

        public void UpdateSimplexState() {
            this.Simplex_Re.Clear();
            this.Vertices.GetEntryList().ForEach(e => this.Simplex_Re.Add(e.Pair));
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
            SKPoint[] gVertices = this.Vertices.ToGlobalLocations();

            path.MoveTo(gVertices[0]);
            path.LineTo(gVertices[1]);
            path.LineTo(gVertices[2]);
            path.LineTo(gVertices[0]);

            if (this.IsHovered) {
                fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DimGray, 0.6f);
            }

            if (this.IsSet) {
                strokePaint.Color = SKColors.DarkSeaGreen;
            }

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, strokePaint);
        }

        public int CompareTo(object obj) {

            var context = ((Triangle) obj).VertexIndexes.Select(idx => Convert.ToString(idx)).ToArray();
            var contextThis = this.VertexIndexes.Select(idx => Convert.ToString(idx)).ToArray();

            return string.Compare(string.Join("", contextThis), string.Join("", context));
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

        public override CanvasObject Clone() {
            throw new NotImplementedException();
        }
    }

    public interface IOrdered {
        int Index { get; set; }
    }

    public class Entry : ScalableCanvasObject, IComparer<Entry> {
        public Transform TransformNew { get; set; }
        public bool IsSelected { get; set; } = false;
        public bool IsHovered { get; set; } = false;
        public bool IsPaired { get; set; } = false;
        public SKPoint[] PairedConfig { get; set; }
        public int Index { get; set; } = -1;
        public Pair Pair { get; set; }
        public SKPoint Value { get; set; }
        public override SKPoint Location {
            get {
                //Logger.Debug("GetMethod - Location: {0}", this.Scale.MapPoint(this.Value));
                return this.Scale.MapPoint(this.Value);
            }
        }
        // Scale Transformation: Value ==> Local Coordinate

        public Entry() : this(0, 0, SKMatrix.MakeIdentity()) { }

        public Entry(float x, float y, SKMatrix transform) : this(new SKPoint(x, y), SKMatrix.MakeIdentity(), transform) { }

        //public Entry(SKPoint value, SKPoint location, SKMatrix transform) : base(location, transform) {
        //    //this.Value = value;
        //    Logger.Debug("Create new entry - [Location]: {0}", this.Location);
        //    Logger.Debug("Create new entry - [Value]: {0}", this.Value);
        //}

        public Entry(SKPoint value) : this(value, SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity()) { }

        public Entry(SKPoint value, SKMatrix scale, SKMatrix transform) : base(scale, transform) {
            this.Value = value;
            //this.Scale = scale;
            this.Pair = new Pair(new StateVector(SkiaHelper.ToVector(this.Value)));

            //Logger.Debug("Create new entry - [Value]: {0}", this.Value);
            //Logger.Debug("Create new entry - [Location]: {0}", this.Location);
        }

        public Entry(SKPoint value, Transform transform) : this(value) {
            this.TransformNew = transform;

            Logger.Debug($"Test transform from value: {this.TransformNew.MapPoint(this.Value)}");
        }

        public static Entry operator +(Entry entryLeft, Entry entryRight) {
            var leftT = entryLeft.Transform.Values;
            var rightT = entryRight.Transform.Values;
            var leftS = entryLeft.Scale.Values;
            var rightS = entryRight.Scale.Values;

            if (!Enumerable.SequenceEqual(leftT, rightT) & !Enumerable.SequenceEqual(leftS, rightS)) {
                throw new Exception("Transform or Scale is not equal.");
            }
            else {
                return new Entry(
                    entryLeft.Value + entryRight.Value,
                    entryLeft.Scale,
                    entryLeft.Transform);
            }
        }

        public static Entry operator -(Entry entryLeft, Entry entryRight) {
            var leftT = entryLeft.Transform.Values;
            var rightT = entryRight.Transform.Values;
            var leftS = entryLeft.Scale.Values;
            var rightS = entryRight.Scale.Values;

            if (!Enumerable.SequenceEqual(leftT, rightT) & !Enumerable.SequenceEqual(leftS, rightS)) {
                throw new Exception("Transform or Scale is not equal.");
            }
            else {
                return new Entry(
                    entryLeft.Value - entryRight.Value,
                    entryLeft.Scale,
                    entryLeft.Transform);
            }
        }

        public static Entry operator /(Entry entry, float div) {
            return new Entry(
                new SKPoint(entry.Value.X / div, entry.Value.Y / div),
                entry.Scale,
                entry.Transform
                );
        }

        //public override bool Equals(object obj) {
        //    //Check for null and compare run-time types.
        //    if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
        //        return false;
        //    }
        //    else {
        //        Point p = (Point)obj;
        //        return (x == p.x) && (y == p.y);
        //    }
        //}

        public override void Draw(SKCanvas canvas) {
            float radius = 5;

            if (this.IsHovered | this.IsSelected) {
                radius += 2;
                //Hover.DrawHover(canvas, this.GlobalLocation);
            }

            //var fillPaint = new SKPaint {
            //    IsAntialias = true,
            //    Color = SKColors.ForestGreen,
            //    Style = SKPaintStyle.Fill
            //};
            var fillPaint = new SKPaint {
                IsAntialias = true,
                Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.8f),
                Style = SKPaintStyle.Fill
            };
            var strokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            if (this.IsSelected) {
                fillPaint.Color = SKColors.MediumVioletRed;
            }
            else if (!this.IsSelected & this.IsPaired) {
                fillPaint.Color = SKColors.YellowGreen;
            }
            // Draw entry shape
            canvas.DrawCircle(this.GlobalLocation, radius, fillPaint);
            canvas.DrawCircle(this.GlobalLocation, radius, strokePaint);

            //Logger.Debug("Entry Global Location: {0}", this.GlobalLocation);
            //Logger.Debug("Entry Local Location: {0}", this.Location);


        }

        public void DrawHover(SKCanvas canvas) {
            if (this.IsHovered) {
                Hover.DrawHover(canvas, this.Value, this.Index, this.Location, this.Transform);
            }
        }

        //public void UpdateTransform(SKMatrix transform) {
        //    this.Transform = transform;
        //}

        //public override void UpdateScale(SKMatrix scale) {
        //    this.Scale = scale;

        //    Logger.Debug("Update Scale - [Value]: {0}", this.Value);
        //    Logger.Debug("Update Scale - [Location]: {0}", this.Location);
        //}

        public void UpdateFromGlobalLocation(SKPoint gLocation) {
            SKMatrix inverseCoordinate, inverseScale;
            this.Transform.TryInvert(out inverseCoordinate);
            this.Scale.TryInvert(out inverseScale);

            this.Value = inverseScale.MapPoint(inverseCoordinate.MapPoint(gLocation));
        }

        public int Compare(Entry x, Entry y) {
            return x.Index.CompareTo(y.Index);
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

    public interface ILoggable {
        Logger Logger { get; }
    }


    public abstract class CanvasObject : ILoggable {
        public Logger Logger { get; private set; }

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

            this.EnableLogging();
        }

        public CanvasObject(SKMatrix transform) : this(new SKPoint(), transform) { }

        public void EnableLogging() {
            this.Logger = LogManager.GetCurrentClassLogger();
        }
        public void DisableLogging() {
            this.Logger = null;
        }

        public abstract void Draw(SKCanvas canvas);
        public abstract CanvasObject Clone();
        public virtual void UpdateTransform(SKMatrix transform) {
            this.Transform = transform;
        }
    }

    public class StaticCanvasObject : CanvasObject {
        public StaticCanvasObject(SKPoint location, SKMatrix transform) : base(location, transform) { }
        public StaticCanvasObject(SKMatrix transform) : this(new SKPoint(), transform) { }

        public override void Draw(SKCanvas canvas) {
            throw new NotImplementedException();
        }
        public override CanvasObject Clone() {
            throw new NotImplementedException();
        }
    }

    public class Axis : StaticCanvasObject {
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

    public class Tick : StaticCanvasObject {
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

    public class Label : StaticCanvasObject {
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


    public partial class SkiaHelper {
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

        public static SKPoint ToSKPoint(Point p) {
            return new SKPoint { X = p.X, Y = p.Y };
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
        public class SKRay : SKLine {
            public bool IsInLine(float targetT) {
                return targetT >= 0 & targetT != float.PositiveInfinity;
            }

            public SKPoint GetIntersection(float targetT) {
                return this.Start + SKPointMultiply(this.Direction, targetT);
            }
        }

        public class SKLineSegment : SKLine {
            public SKPoint End { get; set; }
            public override SKPoint Direction {
                get {
                    return new SKPoint() {
                        X = (this.End - this.Start).X / (this.End - this.Start).Length,
                        Y = (this.End - this.Start).Y / (this.End - this.Start).Length,
                    };
                }
            }

            public float T {
                get {
                    return (this.End - this.Start).Length;
                }
            }

            public bool IsInLine(float targetT) {
                return targetT <= this.T & targetT >= 0;
            }
        }

        public class SKLine {
            public SKPoint Start { get; set; }
            private LA.Vector<float> VStart {
                get {
                    return LA.Vector<float>.Build.DenseOfArray(new float[] { this.Start.X, this.Start.Y });
                }
            }
            public virtual SKPoint Direction { get; set; }
            public virtual LA.Vector<float> VDirection {
                get {
                    return LA.Vector<float>.Build.DenseOfArray(new float[] { this.Direction.X, this.Direction.Y });
                }
            }

            //public virtual bool IsInLine(SKPoint target) {

            //}
        }

        public class Rect {
            // clockwise
            public LineSegment Top { get; set; }
            public LineSegment Right { get; set; }
            public LineSegment Bottom { get; set; }
            public LineSegment Left { get; set; }

            public Rect(SKRect rect) {
            }
        }

        public class LineSegment : Line {
            public LineSegment(Vector<float> v0, Vector<float> v1) : base(v0, v1) { }
            public LineSegment(Line line) : base(line.V0, line.V1) { }

            public static LineSegment CreateLineSegment(Vector<float> v0, Vector<float> direction) {
                return new LineSegment(CreateLineFromDirection(v0, direction));
            }
        }

        public class Ray : Line {
            public Ray(Vector<float> v0, Vector<float> v1) : base(v0, v1) { }
            public Ray(Line line) : base(line.V0, line.V1) { }

            public static Ray CreateRay(Vector<float> v0, Vector<float> direction) {
                return new Ray(CreateLineFromDirection(v0, direction));
            }

            public bool isOnRay(Vector<float> v) {
                float t1, t2;

                t1 = this.UnitDirection[0] != 0.0f ? (v[0] - this.V0[0]) / this.UnitDirection[0] : float.PositiveInfinity;
                t2 = this.UnitDirection[1] != 0.0f ? (v[1] - this.V0[1]) / this.UnitDirection[1] : float.PositiveInfinity;

                return (t1 == t2 | (float.IsPositiveInfinity(t1) & v[0] == 0.0f) | (float.IsPositiveInfinity(t2) & v[1] == 0.0f)) & t1 >= 0.0f & t2 >= 0.0f;
            }
        }

        public interface ILine {
            Vector<float> V0 { get; set; }
            Vector<float> V1 { get; set; }
            Vector<float> Direction { get; }
            Vector<float> UnitDirection { get; }
            float L2Norm { get; }
        }

        // L = V(V0) + T * V(V1 - V0)
        public class Line : ILine {
            public Vector<float> V0 { get; set; }
            public Vector<float> V1 { get; set; }
            public Vector<float> Direction {
                get {
                    return this.V1 - this.V0;
                }
            }
            public float L2Norm {
                get {
                    return (float) this.Direction.L2Norm();
                }
            }
            public Vector<float> UnitDirection {
                get {
                    return this.Direction.Normalize(2.0f);
                }
            }

            public Line(Vector<float> v0, Vector<float> v1) {
                this.V0 = v0;
                this.V1 = v1;
            }

            public Vector<float> GetNewPointOnLine(float magnitude) {
                return this.V0 + magnitude * this.UnitDirection;
            }

            public static Line CreateLineFromDirection(Vector<float> v0, Vector<float> direction) {
                return new Line(v0, v0 + direction);
            }
        }

        public static (bool, bool) IsIntersectionPointOnLine(Line l1, Line l2) {
            (float t, float u) = GetIntersectionFactors(l1, l2);

            return (t >= 0.0f & t <= 1.0f, u >= 0.0f & u <= 1.0f);
        }

        public static (bool, bool) IsIntersectionPointOnRay(Line l1, Line l2) {
            (float t, float u) = GetIntersectionFactors(l1, l2);

            return (t >= 0.0f, u >= 0.0f);
        }

        public static Vector<float> ConvertSKPointToVector(SKPoint point) {
            return Vector<float>.Build.DenseOfArray(new float[] { point.X, point.Y });
        }

        public static SKPoint ConvertVectorToSKPoint(Vector<float> v) {
            return new SKPoint(v[0], v[1]);
        }


        public static (float, float) GetIntersectionFactors(Line l1, Line l2) {
            var a = l1.V0[0] - l2.V0[0];
            var b = l1.V1[0] - l1.V0[0];
            var c = l2.V1[0] - l2.V0[0];
            var d = l1.V0[1] - l2.V0[1];
            var e = l1.V1[1] - l1.V0[1];
            var f = l2.V1[1] - l2.V0[1];

            var detT = Matrix<float>.Build.DenseOfArray(
                new float[,] {
                    { c, f },
                    { a, d }
                }).Determinant();
            var detU = Matrix<float>.Build.DenseOfArray(
                new float[,] {
                    { b, e },
                    { d, a }
                }).Determinant();
            var det = Matrix<float>.Build.DenseOfArray(
                new float[,] {
                    { b, f },
                    { e, c }
                }).Determinant();


            float t, u;

            if (det == 0.0f) {
                t = float.PositiveInfinity;
                u = float.PositiveInfinity;
            } else {
                t = detT / det;
                u = detU / det;
            }

            return (t, u);
        }

        public static LA.Matrix<float> CheckIsIntersected(SKLine line1, SKLine line2) {

            // Line Representation: line = p + a * v
            // Solve p1 + a1 * v1 = p2 + a2 * v2

            LA.Matrix<float> A = LA.Matrix<float>.Build.DenseOfArray(
                new float[,] {
                    { line1.Direction.X, - line2.Direction.X },
                    { line1.Direction.Y, - line2.Direction.Y }});
            LA.Matrix<float> b = LA.Matrix<float>.Build.DenseOfArray(
                new float[,] {
                    { line2.Start.X - line1.Start.X },
                    { line2.Start.Y - line1.Start.Y }});
            LA.Matrix<float> x = A.Solve(b);

            //Console.WriteLine(x.ToMatrixString());
            return x;
        }

        public static SKPoint SKPointMultiply(SKPoint factor1, float factor2) {
            return new SKPoint() { X = factor1.X * factor2, Y = factor1.Y * factor2 };
        }

        public static LineSegment[] GetAreaLineSegments_v1(SKRect area) {
            var LT = Vector<float>.Build.DenseOfArray(new float[] { area.Left, area.Top });
            var LB = Vector<float>.Build.DenseOfArray(new float[] { area.Left, area.Bottom }) ;
            var RT = Vector<float>.Build.DenseOfArray(new float[] { area.Right, area.Top });
            var RB = Vector<float>.Build.DenseOfArray(new float[] { area.Right,  area.Bottom }) ;

            LineSegment[] lineSegments = new LineSegment[] {
                new LineSegment(LT, LB),
                new LineSegment(LB, RB),
                new LineSegment(RB, RT),
                new LineSegment(RT, LT)};

            return lineSegments;
        }

        public static (SKPoint[], SKLineSegment[]) GetAreaLineSegments(SKRect area) {
            SKPoint LT = new SKPoint() { X = area.Left, Y = area.Top };
            SKPoint LB = new SKPoint() { X = area.Left, Y = area.Bottom };
            SKPoint RT = new SKPoint() { X = area.Right, Y = area.Top };
            SKPoint RB = new SKPoint() { X = area.Right, Y = area.Bottom };

            SKLineSegment[] lineSegments = new SKLineSegment[] {
                new SKLineSegment() {
                    Start = LT,
                    End = LB
                },
                new SKLineSegment() {
                    Start = LB,
                    End = RB
                },
                new SKLineSegment() {
                    Start = RB,
                    End = RT
                },
                new SKLineSegment() {
                    Start = RT,
                    End = LT
                }};

            return (new SKPoint[] { LT, LB, RT, RB }, lineSegments);
        }
        public static void DrawRay(SKCanvas canvas, SKRay ray, SKRect area, SKPaint paint) {
            SKPoint LT = new SKPoint() { X = area.Left, Y = area.Top };
            SKPoint LB = new SKPoint() { X = area.Left, Y = area.Bottom };
            SKPoint RT = new SKPoint() { X = area.Right, Y = area.Top };
            SKPoint RB = new SKPoint() { X = area.Right, Y = area.Bottom };

            SKLineSegment[] lineSegments = new SKLineSegment[] {
                new SKLineSegment() {
                    Start = LT,
                    End = LB
                },
                new SKLineSegment() {
                    Start = LB,
                    End = RB
                },
                new SKLineSegment() {
                    Start = RB,
                    End = RT
                },
                new SKLineSegment() {
                    Start = RT,
                    End = LT
                }};

            SKPoint intersection = new SKPoint();

            for (int idx = 0; idx < lineSegments.Length; idx++) {
                LA.Matrix<float> T = CheckIsIntersected(ray, lineSegments[idx]);
                if (ray.IsInLine(T[0, 0]) & lineSegments[idx].IsInLine(T[1, 0])) {
                    intersection = ray.GetIntersection(T[0, 0]);
                }
            }

            if (paint == null) {
                paint = new SKPaint {
                    IsAntialias = true,
                    Color = SKColors.Blue,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                };
            }

            canvas.DrawLine(ray.Start, intersection, paint);
        }
    }
}
