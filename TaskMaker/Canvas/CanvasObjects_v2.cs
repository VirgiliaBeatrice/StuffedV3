using SkiaSharp;
using System;
using System.Collections.Generic;
//using Reparameterization;
using MathNet.Numerics.LinearAlgebra;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using TaskMaker.SimplicialMapping;
using MathNetExtension;
using PCController;
using System.Text.Json;
using System.Drawing;
using TaskMaker.Geometry;

namespace TaskMaker {

    public class Canvas {
        public Services Services { get; set; }
        public bool IsShownPointer { get; set; } = false;
        public bool IsShownPointerTrace { get; set; } = false;
        public Layer RootLayer { get; set; } = new Layer("Root");
        public Layer SelectedLayer { get; set; }
        public ISelectionTool SelectionTool { get; set; }
        public PointerTrace PointerTrace { get; set; }
        public CrossPointer Pointer { get; set; }

        private Mapping.Triangulation triangulation;

        public Canvas(Services services) {
            this.Services = services;
            this.triangulation = new Mapping.Triangulation();

            this.RootLayer.Nodes.Add(new Layer("New Layer 1"));
            this.SelectedLayer = this.RootLayer.FirstNode as Layer;

            this.Pointer = new CrossPointer();
        }

        public void Reset() {
            this.SelectedLayer.Entities.ForEach(e => e.IsSelected = false);
            this.IsShownPointer = false;
            this.IsShownPointerTrace = false;
        }

        public void Draw(SKCanvas sKCanvas) {
            this.SelectedLayer.Draw(sKCanvas);
            //this.SelectedLayer.Complex.Draw(sKCanvas);
            //this.SelectedLayer.Entities.ForEach(e => e.Draw(sKCanvas));

            if (this.SelectionTool != null) {
                this.SelectionTool.DrawThis(sKCanvas);
            }

            if (this.IsShownPointer) {
                this.Pointer.Draw(sKCanvas);
            }

            if (this.IsShownPointerTrace) {
                this.PointerTrace.Draw(sKCanvas);
            }
        }

        //public void Interpolate(SKPoint pointerLocation, Layer layer) {
        //    var pointer = layer.Pointer;
        //    pointer.Location = pointerLocation;

        //    var lambdas = layer.Complex.GetLambdas(pointer.Location);
        //    var configVector = layer.Complex.GetConfigVectors(pointer.Location);

        //    if (layer.LayerStatus == LayerStatus.WithMotor) {
        //        var configs = layer.MotorConfigs;

        //        configs.FromVector(configs, configVector);
        //    }


        //    if (layer.LayerStatus == LayerStatus.WithLayer) {
        //        var configs = layer.LayerConfigs;

        //        configs.FromVector(configs, configVector);
                
        //        foreach(var l in configs) {
        //            this.Interpolate(l.Pointer.Location, l);
        //        }

        //    }
        //}

        public bool Triangulate() {
            var selectedEntities = this.SelectedLayer.Entities.Where(e => e.IsSelected);
            this.SelectedLayer.Complex = new SimplicialComplex_v2();

            // Case: amount less than 3
            if (selectedEntities.Count() < 3) {
                return false;
            }
            else if (selectedEntities.Count() == 3) {
                var tri = selectedEntities.ToArray();

                this.SelectedLayer.Complex.Add(new Simplex_v2(tri));
                //this.SelectedLayer.Complex.
            }
            else {
                // Case: amount larger than 3
                var vectors = selectedEntities.Select(e => new double[] { e.Location.X, e.Location.Y });
                var flattern = new List<double>();

                foreach (var e in vectors) {
                    flattern.AddRange(e);
                }

                var input = flattern.ToArray();
                var output = this.triangulation.RunDelaunay_v1(2, input.Length / 2, ref input);

                // ccw => cw
                var outputConvexList = this.triangulation.RunConvexHull_v1(2, input.Length / 2, ref input);
                outputConvexList.Reverse();
                var outputConvex = new LinkedList<int>(outputConvexList);

                foreach (var triIndices in output) {
                    var arrSelectedEntities = selectedEntities.ToArray();
                    var tri = new Entity_v2[] {
                            arrSelectedEntities[triIndices[0]],
                            arrSelectedEntities[triIndices[1]],
                            arrSelectedEntities[triIndices[2]]
                        };

                    this.SelectedLayer.Complex.Add(new Simplex_v2(tri));
                }

                // Get all edges of convex hull.
                for (var it = outputConvex.First; it != null; it = it.Next) {
                    Entity_v2 e1;
                    var arrSelectedEntities = selectedEntities.ToArray();

                    var e0 = arrSelectedEntities[it.Value];

                    this.SelectedLayer.Complex.AddExtreme(e0);

                    if (it == outputConvex.Last) {
                        e1 = arrSelectedEntities[outputConvex.First.Value];
                    }
                    else {
                        e1 = arrSelectedEntities[it.Next.Value];
                    }

                    var edge = this.SelectedLayer.Complex.GetAllEdges().Where(e => e.Contains(e0) & e.Contains(e1));

                    this.SelectedLayer.Complex.AddComplexEdge(edge.First());
                }

                this.SelectedLayer.Complex.SetVoronoiRegions();
            }

            // Reset entities' states
            this.Reset();

            return true;
        }
    }

    public class Layer : TreeNode {
        public bool IsShownPointer { get; set; } = false;
        public Point_v2 Pointer { get; set; } = new Point_v2();
        public List<Entity_v2> Entities { get; set; } = new List<Entity_v2>();
        public SimplicialComplex_v2 Complex { get; set; } = new SimplicialComplex_v2();
        public Layer NextLayer => (Layer)this.NextNode;
        public Configs<Motor> MotorConfigs { get; set; }
        public Configs<Layer> LayerConfigs { get; set; }

        public LayerStatus LayerStatus {
            get {
                if (this.MotorConfigs != null)
                    return LayerStatus.WithMotor;
                if (this.LayerConfigs != null)
                    return LayerStatus.WithLayer;

                return LayerStatus.None;
            }
        }

        static public void Interpolate(SKPoint pointerLocation, Layer layer) {
            var pointer = layer.Pointer;
            pointer.Location = pointerLocation;

            var lambdas = layer.Complex.GetLambdas(pointer.Location);
            var configVector = layer.Complex.GetConfigVectors(pointer.Location);

            if (layer.LayerStatus == LayerStatus.WithMotor) {
                var configs = layer.MotorConfigs;

                configs.FromVector(configs, configVector);
            }


            if (layer.LayerStatus == LayerStatus.WithLayer) {
                var configs = layer.LayerConfigs;

                configs.FromVector(configs, configVector);

                foreach (var l in configs) {
                    Interpolate(l.Pointer.Location, l);
                }

            }
        }

        public Layer() {
            this.Text = "New Layer";
        }

        public Layer(string name) {
            this.Text = name;
        }

        public void ShowTargetSelectionForm(Services info) {
            var form = new Form();
            var selection = new TargetSelection(info);
            var group = new GroupBox();

            form.Text = $"Target Selection - {this.Text}";
            form.Size = new Size(600, 600);
            group.Text = this.Text;
            group.Dock = DockStyle.Fill;
            selection.Dock = DockStyle.Fill;

            group.Controls.Add(selection);
            form.Controls.Add(group);

            form.ShowDialog();
            form.Dispose();
        }

        public bool ShowTargetControlForm() {
            if (this.LayerConfigs != null) {
                this.ShowTinyCanvases();

                return true;
            }

            if (this.MotorConfigs != null) {
                this.ShowMotorControllers();
             
                return true;
            }

            MessageBox.Show("No config has been set. Abort.");
            return false;
        }

        public void ShowTinyCanvases() {
            foreach(var layer in this.LayerConfigs) {
                var form = new TinyCanvasForm(layer);

                form.Text = $"Tiny Canvas - {this.Text}";
                form.Size = new Size(600, 600);

                form.Show();

                //form.ShowDialog();
                //form.Dispose();
            }

        }

        public void ShowMotorControllers() {
            var form = new Form();
            var panel = new FlowLayoutPanel();

            form.Text = $"Motor Position - {this.Text}";
            form.Size = new Size(600, 600);
            panel.Dock = DockStyle.Fill;

            foreach(var motor in this.MotorConfigs) {
                var motorController = new MotorController(motor);
                motorController.MotorName = $"Motor{this.MotorConfigs.IndexOf(motor) + 1}";

                panel.Controls.Add(motorController);
            }

            form.Controls.Add(panel);
            form.Show();
        }

        public void InitializeMotorConfigs() {
            this.MotorConfigs = new Configs<Motor>(
                (me) => {
                    var newConfigVector = me.Select(e => (float)e.position.Value).ToArray();

                    return Vector<float>.Build.Dense(newConfigVector);
                },
                (me, input) => {
                    for(int i = 0; i < input.Count; ++i) {
                        me[i].position.Value = (int)input[i];
                    }
                }
            );

            this.LayerConfigs = null;
        }

        public void InitializeLayerConfigs() {
            this.LayerConfigs = new Configs<Layer>(
                (me) => {
                    var newConfigVector = me.Select(e => e.Pointer.Location.ToVector()).ToList();
                    var flattern = new List<float>();
                    newConfigVector.ForEach(v => flattern.AddRange(v));

                    return Vector<float>.Build.Dense(flattern.ToArray());
                },
                (me, input) => {
                    for(int i = 0; i < input.Count / 2; ++i) {
                        me[i].Pointer.Location = new SKPoint(input[i * 2], input[i * 2 + 1]);
                    }
                });

            this.MotorConfigs = null;
        }

        public void Interpolate(SKPoint pointerLocation) {
            Layer.Interpolate(pointerLocation, this);
            //var layer = this;
            //var pointer = layer.Pointer;
            //pointer.Location = pointerLocation;

            //var lambdas = layer.Complex.GetLambdas(pointer.Location);
            //var configVector = layer.Complex.GetConfigVectors(pointer.Location);

            //if (layer.LayerStatus == LayerStatus.WithMotor) {
            //    var configs = layer.MotorConfigs;

            //    configs.FromVector(configs, configVector);
            //}


            //if (layer.LayerStatus == LayerStatus.WithLayer) {
            //    var configs = layer.LayerConfigs;

            //    configs.FromVector(configs, configVector);

            //    foreach (var l in configs) {
            //        this.Interpolate(l.Pointer.Location);
            //    }

            //}
        }

        public void Draw(SKCanvas sKCanvas) {
            this.Complex.Draw(sKCanvas);

            var reverse = new List<Entity_v2>(this.Entities);
            reverse.Reverse();
            reverse.ForEach(e => e.Draw(sKCanvas));

            if (this.IsShownPointer) {
                this.Pointer.Draw(sKCanvas);
            }
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
        public SKPath Path { get; set; }

        private SKPaint strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public PointerTrace(SKPoint start) {
            this.Location = start;
            this.Path = new SKPath();

            this.Path.MoveTo(start);
        }

        public void Update(SKPoint point) {
            this.Path.LineTo(point);
        }

        public override void Draw(SKCanvas sKCanvas) {
            sKCanvas.DrawPath(this.Path, this.strokePaint);
        }

        public override void Invalidate() {
            base.Invalidate();
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
            sKCanvas.DrawCircle(this.Location, 2.0f, fillPaint);
            sKCanvas.DrawCircle(this.Location, 2.0f, strokePaint);
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
            this.Location = start;
        }

        public override void Draw(SKCanvas sKCanvas) {
            // horizontal line segment
            var hStart = this.Location + new SKSize(-this.length / 2, 0);
            var hEnd = hStart + new SKSize(this.length, 0);
            // vertical line segment
            var vStart = this.Location + new SKSize(0, -this.length / 2);
            var vEnd = vStart + new SKSize(0, this.length);

            sKCanvas.DrawLine(hStart, hEnd, this.strokePaint);
            sKCanvas.DrawLine(vStart, vEnd, this.strokePaint);
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
            this._bound = new SKRect() {
                Location = location,
                Size = new SKSize(this.Length, 20),
            };

            this.Location = location;
        }

        public bool Contains(SKPoint point) {
            return this._bound.Contains(point);
        }

        public void Invalidate() {
            if (this.IsSelected) {
                barStrokePaint.Color = SKColors.Bisque;
            } else {
                barStrokePaint.Color = SKColors.Blue;
            }
        }

        public void Draw(SKCanvas sKCanvas) {
            // Draw baseline
            var baselinePostion = this.Location + new SKSize(0, 10);
            sKCanvas.DrawLine(baselinePostion, baselinePostion + new SKSize(this.Length, 0), this.sliderStrokePaint);

            // Draw bar
            var barPosition = this.Location + new SKSize(this.Length * this.Percentage / 100, 0);
            sKCanvas.DrawLine(barPosition, barPosition + new SKSize(0, 20), this.barStrokePaint);
        }
    }


    public class Entity_v2 : CanvasObject_v2 {
        public bool IsSelected {
            get => this.isSelected;
            set {
                this.isSelected = value;
                
                if (this.isSelected) {
                    this._radius = 7.0f;
                } else {
                    this._radius = 5.0f;
                }
            }
        }
        public int Index { get; set; }
        public Pair Pair { get; set; }
        public Vector<float> Vector => SkiaExtension.SkiaHelper.ToVector(this.location);

        override public SKPoint Location {
            get => this.location;
            set {
                this.location = value;

                this.LocationUpdated?.Invoke(this, null);
            }
        }

        public event EventHandler LocationUpdated;

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
        private float _radius = 5.0f;
        private bool isSelected = false;

        public Entity_v2(SKPoint point) {
            this.Location = point;
            this.Pair = new Pair(this);
        }

        public override string ToString() {
            StringBuilder str = new StringBuilder();

            str.Append($"[Entity {this.Index}] - {this.Location}");
            return str.ToString();
        }

        public override bool ContainsPoint(SKPoint point) {
            return SKPoint.Distance(point, this.Location) <= this._radius;
        }

        public override void Invalidate() {
            //this._gLocation = this.Location;
            //this._gRadius = this._radius;

            if (this.IsSelected) {
                this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Chocolate, 0.8f);
            } else {
                if (this.Pair.IsPaired) {
                    this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Red, 0.8f);
                }
                else {
                    this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.8f);
                }
            }
        }

        public override void Draw(SKCanvas canvas) {
            this.Invalidate();

            this.DrawThis(canvas);
        }

        private void DrawThis(SKCanvas canvas) {
            canvas.DrawCircle(this.Location, this._radius, this.fillPaint);
            canvas.DrawCircle(this.Location, this._radius, this.strokePaint);

            var text = new SKPaint() {
                TextSize = 12.0f,
                Color = SKColors.Black,
            };
            canvas.DrawText($"Enitity-{this.Index}", this.location, text);
        }
    }

    public class Simplex_v2 {
        public SKPoint Location { get; set; }
        public List<Entity_v2> Vertices { get; set; } = new List<Entity_v2>();
        public Pairs Pairs { get; set; } = new Pairs();

        private SKPaint fillPaint = new SKPaint {
            IsAntialias = true,
            Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.2f),
            Style = SKPaintStyle.Fill
        };
        private SKPaint strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public override string ToString() {
            return $"Simplex - {string.Join(",", Vertices.Select(v => v.Index))}";
        }

        public Simplex_v2(ICollection<Entity_v2> entities) {
            this.Vertices.AddRange(entities);
            this.Pairs.AddRange(entities.Select(e => e.Pair).ToArray());
            //this.Pairs.TaskBary.AddRange(this.Vertices.Select(v => v.Vector).ToArray());
        }

        public bool ContainsPoint(SKPoint p) {
            var ret = this.GetLambdas(p);

            return ret != null;
        }

        public Vector<float> GetLambdas(SKPoint point) {
            return this.Pairs.TaskBary.GetLambdasOnlyInterior(point.ToVector());
        }

        public Vector<float> GetLambdasExterior(SKPoint p) {
            return this.Pairs.TaskBary.GetLambdas(p.ToVector());
        }

        public bool IsVertex(Entity_v2 e) {
            return this.Vertices.Contains(e);
        }

        public void Invalidate() { }

        public void DrawThis(SKCanvas sKCanvas) {
            var path = new SKPath();

            // Triangle only!!!
            path.MoveTo(this.Vertices[0].Location);
            path.LineTo(this.Vertices[1].Location);
            path.LineTo(this.Vertices[2].Location);
            path.Close();

            sKCanvas.DrawPath(path, this.fillPaint);
            sKCanvas.DrawPath(path, this.strokePaint);
        }
    }


    public class SimplicialComplex_v2 : List<Simplex_v2> {
        private List<Edge_v2> edges = new List<Edge_v2>();
        private List<Edge_v2> complexEdges = new List<Edge_v2>();
        private CircularList<Entity_v2> extremes = new CircularList<Entity_v2>();
        private VoronoiRegions voronoiRegions = new VoronoiRegions();
        private Bend bend;
        private Exterior exterior;

        public new void Add(Simplex_v2 simplex) {

            var edge0 = new Edge_v2();
            var edge1 = new Edge_v2();
            var edge2 = new Edge_v2();

            edge0.Add(simplex.Vertices[0], simplex.Vertices[1]);
            edge1.Add(simplex.Vertices[1], simplex.Vertices[2]);
            edge2.Add(simplex.Vertices[2], simplex.Vertices[0]);

            

            if (!this.edges.Any(e => e.SetEquals(edge0)))
                this.edges.Add(edge0);

            if (!this.edges.Any(e => e.SetEquals(edge1)))
                this.edges.Add(edge1);
            
            if (!this.edges.Any(e => e.SetEquals(edge2)))
                this.edges.Add(edge2);

            base.Add(simplex);
        }

        //public void SetExtremes(List<Entity_v2> extremes) {
        //    this.extremes = new CircularList<Entity_v2>(extremes);
        //}

        public void AddExtreme(Entity_v2 extreme) {
            this.extremes.Add(extreme);
        }

        public void AddComplexEdge(Edge_v2 edge) {
            if (this.complexEdges.Where(e => e.SetEquals(edge)).Count() == 0)
                this.complexEdges.Add(edge);
        }

        public List<Edge_v2> GetAllEdges() {
            return this.edges;
        }

        private List<Edge_v2> FindInAllEdges(Entity_v2 target) {
            return this.edges.FindAll(e => e.Contains(target));
        }

        private List<Edge_v2> FindInComplexEdges(Entity_v2 t0, Entity_v2 t1) {
            return this.complexEdges.FindAll(e => e.Contains(t0) & e.Contains(t1));
        }

        private List<Edge_v2> FindInComplexEdges(Entity_v2 target) {
            return this.complexEdges.FindAll(e => e.Contains(target));
        }

        public void SetVoronoiRegions() {
            var traces = new List<ExteriorRay_v3>();
            //var voronoiRegions = new VoronoiRegions();

            // extremesa order: cw
            foreach(var node in extremes) { 
                var it = node.Value;
                var prev = node.Prev.Value;
                var next = node.Next.Value;
                var relatedEdges = this.FindInAllEdges(it);
                
                if (relatedEdges.Count == 3) {
                    var targetEdge = relatedEdges.Find(e => !this.complexEdges.Contains(e));
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

            for(int i = 0; i < traces.Count; ++i) {
                var it = traces[i];
                var next = traces[i + 1 == traces.Count ? 0 : i + 1];

                if (it.E0 == next.E0) {
                    //var region = new VoronoiRegion(it, next, null);
                    var region = new VoronoiRegion_Type1(it, next, null, null);

                    voronoiRegions.Add(region);
                } else if (it.E0 != next.E0) {
                    //var region = new VoronoiRegion(it, next, null);

                    if (this.FindInComplexEdges(it.E0, next.E0).Count == 0) {
                        var target = this.extremes.Where(e => e.Value == it.E0).FirstOrDefault();

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
            this.exterior = Exterior.CreateExterior(this.extremes.Select(e => e.Value).ToArray(), this.ToArray());
        }

        public Vector<float> GetLambdas(SKPoint point) {
            var result = new List<float>();

            this.ForEach(s => result.AddRange(s.GetLambdas(point).ToArray()));
            this.exterior?.Regions.ForEach(r => {
                if (r.Contains(point)) {
                    r.Interpolate(point);
                } 
                });

            return Vector<float>.Build.Dense(result.ToArray());
        }

        public Vector<float> GetConfigVectors(SKPoint point) {
            Vector<float> result = null;

            foreach(var simplex in this) {
                if (result == null) {
                    result = simplex.Pairs.GetConfig(point);
                }
                else {
                    result += simplex.Pairs.GetConfig(point);
                }
            }

            return result;
        }

        public void Draw(SKCanvas sKCanvas) {
            this.ForEach(sim => sim.DrawThis(sKCanvas));
            this.complexEdges.ForEach(edge => edge.Draw(sKCanvas));
            //this.bend?.Draw(sKCanvas);
            this.exterior?.Draw(sKCanvas);

            // Test
            //if (this.voronoiRegions.Count != 0) {
            //    this.voronoiRegions[5].Draw(sKCanvas);
            //}
            this.voronoiRegions.ForEach(v => v.Draw(sKCanvas));
        }
    }

    public class ArrowCap {
        public SKPoint Location { get; set; }
        public SKPoint Direction { get; set; }
        public float Size { get; set; } = 10.0f;

        public ArrowCap(SKPoint location, SKPoint direction) {
            this.Location = location;
            this.Direction = direction;
        }

        public void Draw(SKCanvas sKCanvas) {
            var rotateLeft = SKMatrix.CreateRotationDegrees(-90 - 75);
            var rotateRight = SKMatrix.CreateRotationDegrees(90 + 75);

            var p = Direction.DivideBy(Direction.Length).Multiply(this.Size);
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

    public class Edge_v2 : HashSet<Entity_v2> {
        public HashSet<Entity_v2> Extremes => this;

        private SKPaint stroke = new SKPaint {
            IsAntialias = true,
            Color = SKColors.PaleVioletRed,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        public void Add(Entity_v2 e0, Entity_v2 e1) {
            this.Add(e0);
            this.Add(e1);
        }

        public void Draw(SKCanvas canvas) {
            canvas.DrawLine(this.First().Location, this.Last().Location, stroke);
        }
    }

    public class ExteriorRay_v3 : Ray_v3 {
        public Entity_v2 E0 { get; set; }
        private ArrowCap cap; 

        public ExteriorRay_v3(Entity_v2 entity, SKPoint direction) : base(entity.Location, direction) {
            this.E0 = entity;
        }

        private void Measure() {
            
        } 

        public override void Draw(SKCanvas sKCanvas) {
            base.Draw(sKCanvas);

            cap = new ArrowCap(this.Location, this.Direction);
            cap.Draw(sKCanvas);
        }
    }



    public class Pair {
        public bool IsPaired => this.Config != null;

        public Entity_v2 Task { get; set; }
        public Vector<float> Config { get; set; }
        public event EventHandler PairUpdated;

        public Pair(Entity_v2 task) {
            this.Task = task;
        }

        public void AddPair(Vector<float> target) {
            this.Config = target;
            this.PairUpdated?.Invoke(this, null);
        }

        public void RemovePair() {
            this.Config = null;
            this.PairUpdated?.Invoke(this, null);
        }
    }

    public class Pairs : List<Pair> {
        public bool IsFullyPaired => this.All(e => e.IsPaired);
        public BarycentricCoordinates TaskBary { get; set; } = new BarycentricCoordinates(3);
        public BarycentricCoordinates ConfigBary { get; set; } = new BarycentricCoordinates(3);

        public new void AddRange(IEnumerable<Pair> pairs) {
            base.AddRange(pairs);
            this.TaskBary.AddRange(this.Select(p => p.Task.Vector).ToArray());
            this.ForEach(p => p.PairUpdated += this.P_PairUpdated);
            this.ForEach(p => p.Task.LocationUpdated += this.Task_LocationUpdated);
        }

        private void Task_LocationUpdated(object sender, EventArgs e) {
            if (this.IsFullyPaired) {
                this.TaskBary.UpdateVertices(this.Select(p => p.Task.Vector).ToArray());
            }
        }

        private void P_PairUpdated(object sender, EventArgs e) {
            if (this.IsFullyPaired) {
                this.ConfigBary.AddRange(this.Select(p => p.Config).ToArray());
            }
        }

        public Vector<float> GetConfig(SKPoint taskPoint) {
            var lambda = this.TaskBary.GetLambdasOnlyInterior(taskPoint.ToVector());
            var config = this.ConfigBary.GetB(lambda);

            return config;
        }
    }
}

