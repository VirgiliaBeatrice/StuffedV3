//using Reparameterization;
using MathNet.Numerics.LinearAlgebra;
using MathNetExtension;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TaskMaker.MementoPattern;
using TaskMaker.SimplicialMapping;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskMaker.Mapping;

namespace TaskMaker {

    public class CanvasState : BaseState {
        [JsonInclude]
        public IList<LayerState> Layers { get; private set; }

        public CanvasState(Canvas c) {
            Layers = new List<LayerState>(c.Layers.Select(l => l.Save() as LayerState)).AsReadOnly();
        }

        [JsonConstructor]
        public CanvasState(IList<LayerState> layers) {
            Layers = layers;
        }

        public override object GetState() => (Layers);
    }

    public partial class Canvas : IOriginator {
        public SKRect Bounds { get; set; }
        public bool IsShownPointer { get; set; } = false;
        public bool IsShownPointerTrace { get; set; } = false;

        public List<Layer> Layers { get; set; } = new List<Layer>();
        public Layer SelectedLayer => Layers.Find(l => l.IsSelected);
        public ISelectionTool SelectionTool { get; set; }
        public PointerTrace PointerTrace { get; set; }
        public CrossPointer Pointer { get; set; }


        public Canvas() {
            Layers.Add(new Layer("New Layer") { IsSelected = true });

            Pointer = new CrossPointer();
        }


        public IMemento Save() => new CanvasState(this);

        public void Restore(IMemento m, object info = null) {
            var state = (IList<LayerState>)m.GetState();

            Layers.Clear();

            foreach (var ls in state) {
                var item = new Layer("New Layer");

                item.Restore(ls);
                Layers.Add(item);
            }

            // Reset selection
            Layers[0].IsSelected = true;
            Reset();
        }

        public void Reset() {
            Layers.ForEach(l => l.Reset());
            //IsShownPointer = false;
            //IsShownPointerTrace = false;
        }

        public void Draw(SKCanvas sKCanvas) {
            foreach(var l in Layers) {
                if (l.IsVisible) {
                    l.Draw(sKCanvas);
                }
            }

            if (SelectionTool != null) {
                SelectionTool.DrawThis(sKCanvas);
            }

            if (IsShownPointer) {
                Pointer.Draw(sKCanvas);
            }

            if (IsShownPointerTrace) {
                PointerTrace.Draw(sKCanvas);
            }

            
        }


    }

    /// <summary>
    /// Immutable layer state class
    /// </summary>
    public class LayerState : BaseState {
        [JsonInclude]
        public string Name { get; private set; }
        [JsonInclude]
        public IList<EntityState> Entities { get; private set; }
        [JsonInclude]
        public SimplicialComplexState Complex { get; private set; }

        public LayerState(Layer l) {
            Name = l.Name;
            Entities = l.Entities.Select(e => (EntityState)e.Save()).ToList().AsReadOnly();
            Complex = new SimplicialComplexState(l.Complex);
        }

        [JsonConstructor]
        public LayerState(
            string name,
            IList<EntityState> entities,
            SimplicialComplexState complex) =>
            (Name, Entities, Complex) =
            (name, entities, complex);

        public override object GetState() {
            //return (Name, Entities );
            return (Name, Entities, Complex);
        }

    }

    public partial class Layer : IOriginator {
        public string Name { get; set; }
        public bool IsVisible { get; set; } = true;
        //public bool IsShownPointer { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public Controller Controller { get; set; } = new Controller();

        #region Data
        public List<Entity> Entities { get; set; } = new List<Entity>();
        public SimplicialComplex Complex { get; set; } = new SimplicialComplex();
        public Exterior Exterior { get; set; }
        public Target BindedTarget { get; set; }
        #endregion

        public object Input { get; set; }
        public object Output { get; set; }

        private List<Layer> _children = new List<Layer>();


        //public List<SimplicialComplex> Complexes { get; set; } = new List<SimplicialComplex>();
        //public Vector<float> ControllerVector {
        //    get {
        //        var mat = Matrix<float>.Build.DenseOfColumnVectors(Complexes.Select(c => c.Controller.Location.ToVector()));

        //        return Vector<float>.Build.DenseOfArray(mat.ToColumnMajorArray());
        //    }
        //}
 

        //public List<Exterior> Exteriors { get; set; } = new List<Exterior>();

        public LayerStatus LayerStatus {
            get {
                if (BindedTarget == null)
                    return LayerStatus.None;
                if (BindedTarget.GetType() == typeof(MotorTarget))
                    return LayerStatus.WithMotor;
                else if (BindedTarget.GetType() == typeof(LayerTarget))
                    return LayerStatus.WithLayer;
                else
                    return LayerStatus.None;
            }
        }

        public Layer(string name) {
            Name = name;
        }

        public void Add(Layer child) {
            _children.Add(child);
        }

        public void Remove(Layer child) {
            _children.Remove(child);
        }

        public Layer[] GetChildren() => _children.ToArray();

        public void CreateExterior() {
            Exterior = Complex.CreateExterior();
        }

        public void Reset() {
            Entities.ForEach(e => e.IsSelected = false);

            _children.ForEach(c => c.Reset());
        }

        public void Interpolate(SKPoint p) {
            if (BindedTarget == null)
                return;

            // Input ==InputBary.==> Lambdas ==OutputBary.==> Output
            var complexConfigs = Complex.GetInterpolatedConfigs(p);
            var exteriorConfigs = Exterior.GetInterpolatedConfigs(p);

            BindedTarget.FromVector(complexConfigs + exteriorConfigs);
        }

        public void GetLambdas(SKPoint p) {
            var complexLambdas = Complex.GetInterpolatedLambdas(p);
            var exteriorLambdas = Exterior.GetInterpolatedLambdas(p);
        }

        public void ShowController() {
            Controller.IsVisible = true;
        }

        public void HideController() {
            Controller.IsVisible = false;
        }

        public void ShowTargetSelectionForm() {
            var form = new Form();
            var selection = new TargetSelection();
            var group = new GroupBox();

            form.Text = $"Target Selection - {Name}";
            form.Size = new Size(600, 600);
            group.Text = Name;
            group.Dock = DockStyle.Fill;
            selection.Dock = DockStyle.Fill;

            group.Controls.Add(selection);
            form.Controls.Add(group);

            form.ShowDialog();
            form.Dispose();
        }

        public bool ShowTargetControlForm() {
            if (BindedTarget == null) {
                MessageBox.Show("No config has been set. Abort.");
                return false;
            }


            if (BindedTarget.GetType() == typeof(LayerTarget)) {
                ShowTinyCanvases();

                return true;
            }

            else if (BindedTarget.GetType() == typeof(MotorTarget)) {
                ShowMotorControllers();

                return true;
            }
            else { return false; }
        }

        public void ShowTinyCanvases() {
            foreach (var layer in (BindedTarget as LayerTarget).Layers) {
                var form = new TinyCanvasForm(layer);

                form.Text = $"Tiny Canvas - {Name}";
                form.Size = new Size(600, 600);

                form.Show();

                //form.ShowDialog();
                //form.Dispose();
            }

        }

        public void ShowMotorControllers() {
            var form = new Form();
            var panel = new FlowLayoutPanel();
            var btn = new Button();
            var target = BindedTarget as MotorTarget;

            form.Text = $"Motor Position - {Name}";
            form.Size = new Size(600, 600);
            form.AutoSize = true;
            panel.Dock = DockStyle.Fill;
            btn.Text = "All Return Zero";
            btn.AutoSize = true;


            foreach (var motor in target.Motors) {
                var motorController = new MotorController(motor);
                motorController.MotorName = $"Motor{target.Motors.IndexOf(motor) + 1}";
                btn.Click += (sender, e) => {
                    motorController.ReturnZero();
                };

                panel.Controls.Add(motorController);
            }

            panel.Controls.Add(btn);
            form.Controls.Add(panel);
            form.Show();
        }

        public void Invalidate() {
            foreach (var s in Complex) {
                s.Invalidate();
            }
        }

        public void Draw(SKCanvas sKCanvas) {
            Complex.Draw(sKCanvas);
            Exterior?.Draw(sKCanvas);

            var reverse = new List<Entity>(Entities);
            reverse.Reverse();
            reverse.ForEach(e => e.Draw(sKCanvas));

            Controller.Draw(sKCanvas);
        }

        public IMemento Save() => new LayerState(this);

        public void Restore(IMemento m, object info = null) {
            var (name, entites, complex) =
                ((string, IList<EntityState>, SimplicialComplexState))m.GetState();

            Name = name;

            Entities.Clear();

            foreach(var e in entites) {
                var item = new Entity(new SKPoint());

                item.Restore(e);
                Entities.Add(item);
            }

            Complex.Restore(complex, this);

            //BindedTarget.Restore(bindedTarget);
        }
    }

    public enum LayerStatus {
        None,
        WithMotor,
        WithLayer
    }

    public enum Modes {
        None,
        AddNode,
        DeleteNode,
        EditNode,
        Manipulate,
        Selection,
    }

    public class CompositeLayer {
        public List<Layer> Layers { get; set; } = new List<Layer>();

        public void Tensor() {
            //var lambdasCollection = Layers.Select(l => l.)
        }
    }


    public class CanvasObject_v2 {
        public virtual SKPoint Location { get; set; }

        public virtual string ToJson() {
            var option = new JsonSerializerOptions { WriteIndented = true };

            return JsonSerializer.Serialize(this, option);
        }
        public virtual void Invalidate() { }
        public virtual bool ContainsPoint(SKPoint point) => false;
        public virtual void Draw(SKCanvas sKCanvas) { }
    }

    public class PointerTrace : CanvasObject_v2 {
        private SKPath _path;

        private SKPaint _strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColor.Parse("#77428D").WithAlpha(0.8f),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            //PathEffect = SKPathEffect.CreateDash(new float[] { 10, 1 }, 0),
        };

        public PointerTrace(SKPoint start) {
            Location = start;
            _path = new SKPath();

            _path.MoveTo(start);
        }

        public void Update(SKPoint point) {
            if (point.X != _path.LastPoint.X & point.Y != _path.LastPoint.Y)
                _path.LineTo(point);
        }

        public override void Draw(SKCanvas sKCanvas) {
            var points = _path.Points.Where((p, idx) => idx % 4 == 0);
            var curve = Geometry.BezierCurve.GetBezierCurve(points, 0.4f);

            //sKCanvas.DrawPath(_path, _strokePaint);
            sKCanvas.DrawPath(curve, _strokePaint);
        }
    }

    public class Controller : CanvasObject_v2 {
        public bool IsVisible { get; set; } = false;

        private SKPaint _fill = new SKPaint {
            IsAntialias = true,
            Color = SKColor.Parse("#005CAF"),
            Style = SKPaintStyle.Fill
        };
        private SKPaint _stroke = new SKPaint {
            IsAntialias = true,
            Color = SKColor.Parse("#0F2540"),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public bool Contains(SKPoint p) {
            return SKPoint.Distance(Location, p) <= 5.0f;
        }

        public override void Draw(SKCanvas sKCanvas) {
            if (IsVisible) {
                sKCanvas.DrawCircle(Location, 5.0f, _fill);
                sKCanvas.DrawCircle(Location, 5.0f, _stroke);
            }
        }
    }

    public class Point_v2 : CanvasObject_v2 {
        private SKPaint fillPaint = new SKPaint {
            IsAntialias = true,
            Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.8f),
            Style = SKPaintStyle.Fill
        };
        private SKPaint strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public override void Draw(SKCanvas sKCanvas) {
            sKCanvas.DrawCircle(Location, 2.0f, fillPaint);
            sKCanvas.DrawCircle(Location, 2.0f, strokePaint);
        }
    }

    public class CrossPointer : CanvasObject_v2 {
        private int length = 20;
        private SKPaint strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public CrossPointer() { }

        public CrossPointer(SKPoint start) {
            Location = start;
        }

        public override void Draw(SKCanvas sKCanvas) {
            // horizontal line segment
            var hStart = Location + new SKSize(-length / 2, 0);
            var hEnd = hStart + new SKSize(length, 0);
            // vertical line segment
            var vStart = Location + new SKSize(0, -length / 2);
            var vEnd = vStart + new SKSize(0, length);

            sKCanvas.DrawLine(hStart, hEnd, strokePaint);
            sKCanvas.DrawLine(vStart, vEnd, strokePaint);
        }
    }




    public class LinearSlider {
        public SKPoint Location { get; set; }
        public int Length { get; set; } = 100;
        public int Percentage { get; set; }
        public bool IsSelected { get; set; } = false;

        private SKRect _bound;

        private SKPaint sliderStrokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        private SKPaint barStrokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Blue,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeWidth = 4
        };

        public LinearSlider(SKPoint location) {
            _bound = new SKRect() {
                Location = location,
                Size = new SKSize(Length, 20),
            };

            Location = location;
        }

        public bool Contains(SKPoint point) {
            return _bound.Contains(point);
        }

        public void Invalidate() {
            if (IsSelected) {
                barStrokePaint.Color = SKColors.Bisque;
            }
            else {
                barStrokePaint.Color = SKColors.Blue;
            }
        }

        public void Draw(SKCanvas sKCanvas) {
            // Draw baseline
            var baselinePostion = Location + new SKSize(0, 10);
            sKCanvas.DrawLine(baselinePostion, baselinePostion + new SKSize(Length, 0), sliderStrokePaint);

            // Draw bar
            var barPosition = Location + new SKSize(Length * Percentage / 100, 0);
            sKCanvas.DrawLine(barPosition, barPosition + new SKSize(0, 20), barStrokePaint);
        }
    }


    public class EntityState : BaseState {
        [JsonInclude]
        public int Index { get; private set; }
        [JsonInclude]
        public SKPoint Location { get; private set; }
        [JsonInclude]
        public TargetState TargetState { get; private set; }

        public EntityState(Entity e) {
            Index = e.Index;
            Location = e.Location;
            TargetState = e.TargetState;
        }

        [JsonConstructor]
        public EntityState(int index, SKPoint location, TargetState targetState) {
            Index = index;
            Location = location;
            TargetState = targetState;
        }

        public override object GetState() {
            return (Index, Location, TargetState);
        }
    }

    public class Entity : CanvasObject_v2, IVectorizable, IOriginator {
        public bool IsSelected {
            get => isSelected;
            set {
                isSelected = value;

                if (isSelected) {
                    _radius = 12.0f;
                }
                else {
                    _radius = 10.0f;
                }
            }
        }
        public int Index { get; set; }
        public TargetState TargetState { get; set; }
        public Vector<float> Vector => location.ToVector();

        override public SKPoint Location {
            get => location;
            set {
                location = value;
            }
        }

        private SKPaint fillPaint = new SKPaint {
            IsAntialias = true,
            Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.8f),
            Style = SKPaintStyle.Fill
        };
        private SKPaint strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        private SKPoint location;
        private float _radius = 10.0f;
        private bool isSelected = false;

        public Entity(SKPoint point) {
            Location = point;
        }

        public override string ToString() {
            StringBuilder str = new StringBuilder();

            str.Append($"[Entity {Index}] - {Location}");
            return str.ToString();
        }

        public Vector<float> ToVector() => Location.ToVector();

        public override bool ContainsPoint(SKPoint point) {
            return SKPoint.Distance(point, Location) <= _radius;
        }

        public override void Invalidate() {
            //this._gLocation = this.Location;
            //this._gRadius = this._radius;

            if (IsSelected) {
                fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Chocolate, 0.8f);
            }
            else {
                if (TargetState != null)
                    fillPaint.Color = SKColors.Gold.WithAlpha(0.8f);
                //if (this.Pair.IsPaired) {
                //    this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Red, 0.8f);
                //}
                else {
                    fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.8f);
                }
            }
        }

        public override void Draw(SKCanvas canvas) {
            Invalidate();

            DrawThis(canvas);
        }

        private void DrawThis(SKCanvas canvas) {
            canvas.DrawCircle(Location, _radius, fillPaint);
            canvas.DrawCircle(Location, _radius, strokePaint);

            var text = new SKPaint() {
                TextSize = 12.0f,
                Color = SKColors.Black,
            };
            canvas.DrawText($"Entity - {Index}", location, text);
        }

        public IMemento Save() => new EntityState(this);

        public void Restore(IMemento m, object info = null) {
            var state = ((int, SKPoint, TargetState))m.GetState();
            var index = state.Item1;
            var location = state.Item2;
            var targetState = state.Item3;

            Index = index;
            Location = location;
            TargetState = targetState;
        }
    }

    public class SimplexState : BaseState {
        [JsonInclude]
        public IList<int> VertexIdx { get; private set; }

        public SimplexState(Simplex s) {
            VertexIdx = new List<int>(s.Vertices.Select(v => v.Index)).AsReadOnly();
        }

        [JsonConstructor]
        public SimplexState(IList<int> vertexIdx) =>
            VertexIdx = vertexIdx;

        public override object GetState() {
            return VertexIdx;
        }
    }

    public class Simplex {
        public SKPoint Location { get; set; }
        public List<Entity> Vertices { get; set; } = new List<Entity>();

        //public bool IsPaired => this.Pairs.IsFullyPaired;
        //public Pairs Pairs { get; set; } = new Pairs();
        public SimplicalMap Map { get; set; } = new SimplicalMap();

        private SKPaint fillPaint = new SKPaint {
            IsAntialias = true,
            Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.2f),
            Style = SKPaintStyle.Fill
        };
        private SKPaint strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Gray,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public override string ToString() {
            return $"Simplex - {string.Join(",", Vertices.Select(v => v.Index))}";
        }

        public Simplex(ICollection<Entity> entities) {
            Vertices.AddRange(entities);
            //this.Pairs.AddRange(entities.Select(e => e.Pair).ToArray());
            //this.Pairs.TaskBary.AddRange(this.Vertices.Select(v => v.Vector).ToArray());
        }

        public Simplex() { }

        public bool Contains(SKPoint p) {
            var ret = GetLambdas(p);

            return ret.All(e => e >= 0);
        }

        public void Reset() {
            Vertices.ForEach(e => e.IsSelected = false);
        }

        public Vector<float> GetLambdas(SKPoint p) => Map.GetLambdas(p.ToVector());

        public Vector<float> GetZeroLambdas(SKPoint p) => Map.GetLambdas(p.ToVector()).Multiply(0.0f);

        public Vector<float> GetInterpolatedTargetVector(SKPoint p) => Map.Map(p.ToVector());

        public Vector<float> GetZeroTargetVector() => Map.MapToZero();

        public bool IsVertex(Entity e) {
            return Vertices.Contains(e);
        }

        public void Invalidate() {
            Map.Reset();

            // Invalidate all vertices pair
            foreach (var v in Vertices) {
                Map.SetPair(v, v.TargetState);
            }
        }

        public void DrawThis(SKCanvas sKCanvas) {
            var path = new SKPath();

            // Triangle only!!!
            path.MoveTo(Vertices[0].Location);
            path.LineTo(Vertices[1].Location);
            path.LineTo(Vertices[2].Location);
            path.Close();

            sKCanvas.DrawPath(path, fillPaint);
            sKCanvas.DrawPath(path, strokePaint);
        }

        public IMemento Save() => new SimplexState(this);

        public void Restore(IMemento m, object info = null) {
            var state = m as SimplexState;
            var vertexIdx = state.VertexIdx;
            var layer = info as Layer;

            Vertices.Clear();

            foreach(var idx in vertexIdx) {
                Vertices.Add(layer.Entities[idx]);
            }
        }
    }


    public class SimplicialComplexState : BaseState {
        [JsonInclude]
        public IList<SimplexState> Simplices { get; private set; }


        public SimplicialComplexState(SimplicialComplex c) {
            Simplices = new List<SimplexState>(c.Select(s => new SimplexState(s))).AsReadOnly();
        }

        [JsonConstructor]
        public SimplicialComplexState(IList<SimplexState> simplices) =>
            Simplices = simplices;

        public override object GetState() {
            return Simplices;
        }
    }

    public class SimplicialComplex : List<Simplex> {
        private List<Edge_v2> edges = new List<Edge_v2>();
        private List<Edge_v2> complexEdges = new List<Edge_v2>();
        private CircularList<Entity> _extremes = new CircularList<Entity>();
        private VoronoiRegions voronoiRegions = new VoronoiRegions();

        public new void Add(Simplex simplex) {
            var edge0 = new Edge_v2();
            var edge1 = new Edge_v2();
            var edge2 = new Edge_v2();

            edge0.Add(simplex.Vertices[0], simplex.Vertices[1]);
            edge1.Add(simplex.Vertices[1], simplex.Vertices[2]);
            edge2.Add(simplex.Vertices[2], simplex.Vertices[0]);



            if (!edges.Any(e => e.SetEquals(edge0)))
                edges.Add(edge0);

            if (!edges.Any(e => e.SetEquals(edge1)))
                edges.Add(edge1);

            if (!edges.Any(e => e.SetEquals(edge2)))
                edges.Add(edge2);

            base.Add(simplex);
        }

        public void AddExtreme(Entity extreme) {
            _extremes.Add(extreme);
        }

        public void AddComplexEdge(Edge_v2 edge) {
            if (complexEdges.Where(e => e.SetEquals(edge)).Count() == 0)
                complexEdges.Add(edge);
        }

        public List<Edge_v2> GetAllEdges() {
            return edges;
        }

        private List<Edge_v2> FindInAllEdges(Entity target) {
            return edges.FindAll(e => e.Contains(target));
        }

        private List<Edge_v2> FindInComplexEdges(Entity t0, Entity t1) {
            return complexEdges.FindAll(e => e.Contains(t0) & e.Contains(t1));
        }

        private List<Edge_v2> FindInComplexEdges(Entity target) {
            return complexEdges.FindAll(e => e.Contains(target));
        }

        public bool Exists(Entity e) {
            return this.Any(s => s.IsVertex(e));
        }

        public void Reset() {
            ForEach(s => s.Reset());
        }

        #region Voronoi_Outdate
        public void SetVoronoiRegions() {
            var traces = new List<ExteriorRay_v3>();
            //var voronoiRegions = new VoronoiRegions();

            // extremes order: cw
            foreach (var node in _extremes) {
                var it = node.Value;
                var prev = node.Prev.Value;
                var next = node.Next.Value;
                var relatedEdges = FindInAllEdges(it);

                if (relatedEdges.Count == 3) {
                    var targetEdge = relatedEdges.Find(e => !complexEdges.Contains(e));
                    var direction = it.Location - targetEdge.Where(e => e != it).First().Location;
                    var extension = new ExteriorRay_v3(it, direction);

                    traces.Add(extension);
                }
                else if (relatedEdges.Count > 3) {
                    var rotate = SKMatrix.CreateRotationDegrees(90);
                    var dirPerp0 = rotate.MapPoint(it.Location - prev.Location);
                    var dirPerp1 = rotate.MapPoint(next.Location - it.Location);
                    var perp0 = new ExteriorRay_v3(it, dirPerp0);
                    var perp1 = new ExteriorRay_v3(it, dirPerp1);

                    traces.Add(perp0);
                    traces.Add(perp1);
                }
            }

            for (int i = 0; i < traces.Count; ++i) {
                var it = traces[i];
                var next = traces[i + 1 == traces.Count ? 0 : i + 1];

                if (it.E0 == next.E0) {
                    //var region = new VoronoiRegion(it, next, null);
                    var region = new VoronoiRegion_Type1(it, next, null, null);

                    voronoiRegions.Add(region);
                }
                else if (it.E0 != next.E0) {
                    //var region = new VoronoiRegion(it, next, null);

                    if (FindInComplexEdges(it.E0, next.E0).Count == 0) {
                        var target = _extremes.Where(e => e.Value == it.E0).FirstOrDefault();

                        //region.ExcludedEntity = target.Next.Value;
                        var region = new VoronoiRegion_Type2(it, next, null);

                        voronoiRegions.Add(region);
                    }
                    else {
                        var region = new VoronoiRegion_Type0(it, next, null);

                        voronoiRegions.Add(region);
                    }

                }
            }


            // Set bend
            //this.bend = Bend.GenerateBend(this.extremes.Select(e => e.Value.Location).ToArray());
        }
        #endregion


        public Exterior CreateExterior() => Exterior.CreateExterior(_extremes.Select(e => e.Value).ToArray(), ToArray());


        public Vector<float> GetInterpolatedConfigs(SKPoint p) {
            var values = new List<Vector<float>>();

            foreach (var s in this) {
                var target = s.Contains(p) ? s.GetInterpolatedTargetVector(p) : s.GetZeroTargetVector();

                values.Add(target);
            }

            return values.Sum();
        }

        public Vector<float> GetInterpolatedLambdas(SKPoint p) {
            var columns = new List<Vector<float>>();

            foreach(var s in this) {
                var lambdas = s.Contains(p) ? s.GetLambdas(p) : s.GetZeroLambdas(p);

                columns.Add(lambdas);
            }

            var mat = Matrix<float>.Build.DenseOfColumnVectors(columns.ToArray());

            return Vector<float>.Build.DenseOfArray(mat.AsColumnMajorArray());
        }

        public void Draw(SKCanvas sKCanvas) {
            ForEach(s => s.DrawThis(sKCanvas));
        }

        public IMemento Save() => new SimplicialComplexState(this);

        public void Restore(IMemento m, object info = null) {
            var simplices = (IList<SimplexState>)m.GetState();
            var layer = info as Layer;

            Clear();

            foreach (var s in simplices) {
                var item = new Simplex();

                item.Restore(s, layer);
                Add(item);
            }
        }
    }

    public class ArrowCap {
        public SKPoint Location { get; set; }
        public SKPoint Direction { get; set; }
        public float Size { get; set; } = 10.0f;

        public ArrowCap(SKPoint location, SKPoint direction) {
            Location = location;
            Direction = direction;
        }

        public void Draw(SKCanvas sKCanvas) {
            var rotateLeft = SKMatrix.CreateRotationDegrees(-90 - 75);
            var rotateRight = SKMatrix.CreateRotationDegrees(90 + 75);

            var p = Direction.DivideBy(Direction.Length).Multiply(Size);
            var pLeft = rotateLeft.MapPoint(p) + Location;
            var pRight = rotateRight.MapPoint(p) + Location;

            var path = new SKPath();
            var stroke = new SKPaint() { Color = SKColors.DeepSkyBlue, IsAntialias = true, StrokeWidth = 2.0f, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Bevel };

            path.MoveTo(pLeft);
            path.LineTo(Location);
            path.LineTo(pRight);

            sKCanvas.DrawPath(path, stroke);
        }
    }

    public class Edge_v2 : HashSet<Entity> {
        public HashSet<Entity> Extremes => this;

        private SKPaint stroke = new SKPaint {
            IsAntialias = true,
            Color = SKColors.PaleVioletRed,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        public void Add(Entity e0, Entity e1) {
            Add(e0);
            Add(e1);
        }

        public void Draw(SKCanvas canvas) {
            canvas.DrawLine(this.First().Location, this.Last().Location, stroke);
        }
    }

    public class ExteriorRay_v3 : Ray_v3 {
        public Entity E0 { get; set; }
        private ArrowCap cap;

        public ExteriorRay_v3(Entity entity, SKPoint direction) : base(entity.Location, direction) {
            E0 = entity;
        }

        private void Measure() {

        }

        public override void Draw(SKCanvas sKCanvas) {
            base.Draw(sKCanvas);

            cap = new ArrowCap(Location, Direction);
            cap.Draw(sKCanvas);
        }
    }
}

