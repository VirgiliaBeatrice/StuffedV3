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
                var outputConvex = new LinkedList<int>(this.triangulation.RunConvexHull_v1(2, input.Length / 2, ref input));

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
            foreach(var node in extremes.Reverse()) { 
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
                    var rotate = SKMatrix.CreateRotationDegrees(-90);
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
        }

        public Vector<float> GetLambdas(SKPoint point) {
            var result = new List<float>();

            this.ForEach(s => result.AddRange(s.GetLambdas(point).ToArray()));

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

            // Test
            //if (this.voronoiRegions.Count != 0) {
            //    this.voronoiRegions[0].Draw(sKCanvas);
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

    public class VoronoiRegions : List<IVoronoiRegion> { }

    public struct LineSegment {
        public SKPoint P0 { get; set; }
        public SKPoint P1 { get; set; }
    }

    public interface IVoronoiRegion {
        void Draw(SKCanvas sKCanvas);
    }

    /// <summary>
    /// VoronoiRegion Type0 : two different vertices, no excluded simplex.
    /// </summary>
    public class VoronoiRegion_Type0 : CanvasObject_v2, IVoronoiRegion {
        public ExteriorRay_v3 ExRay0 { get; set; }
        public ExteriorRay_v3 ExRay1 { get; set; }
        public Simplex_v2 Triangle { get; set; }

        private SKPaint stroke = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DeepSkyBlue,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public VoronoiRegion_Type0(ExteriorRay_v3 ray0, ExteriorRay_v3 ray1, Simplex_v2 triangle) {
            this.ExRay0 = ray0;
            this.ExRay1 = ray1;
            this.Triangle = triangle;
        }

        public override bool ContainsPoint(SKPoint p) {
            var ret0 = Geometry.LineSegment.GetSide(ExRay0.Location, ExRay0.Location + ExRay0.Direction, p) <= 0;
            var ret1 = Geometry.LineSegment.GetSide(ExRay0.Location, ExRay1.Location, p) >= 0;
            var ret2 = Geometry.LineSegment.GetSide(ExRay1.Location, ExRay1.Location + ExRay1.Direction, p) >= 0;

            return ret0 & ret1 & ret2;
        }

        private SKPoint[] GetCorners(SKRect bounds, SKPoint start, SKPoint end) {
            var lt = new SKPoint(bounds.Left, bounds.Top);
            var lb = new SKPoint(bounds.Left, bounds.Bottom);
            var rt = new SKPoint(bounds.Right, bounds.Top);
            var rb = new SKPoint(bounds.Right, bounds.Bottom);

            var ccw = new CircularList<LineSegment> {
                new LineSegment() { P0 = lt, P1 = lb },
                new LineSegment() { P0 = lb, P1 = rb },
                new LineSegment() { P0 = rb, P1 = rt },
                new LineSegment() { P0 = lb, P1 = lt },
            };
            var cw = new CircularList<LineSegment> {
                new LineSegment() {P0 = lt, P1 = rt },
                new LineSegment() {P0 = rt, P1 = rb },
                new LineSegment() {P0 = rb, P1 = lb },
                new LineSegment() {P0 = lb, P1 = lt },
            };

            var it = cw.First;
            var points = new List<SKPoint>();

            var init = it;
            do {
                if (Geometry.LineSegment.IsPointOnLine(start, it.Value.P0, it.Value.P1))
                    break;
                else
                    it = it.Next;
            } while (it != init);

            init = it;
            do {
                if (Geometry.LineSegment.IsPointOnLine(end, it.Value.P0, it.Value.P1))
                    break;
                else {
                    points.Add(it.Value.P1);
                    it = it.Next;
                }
            } while (it != init);

            return points.ToArray();
        }

        private SKPoint[] Measure(SKRect bounds) {
            var (t0min, t0max) = ExRay0.Intersect(bounds);
            var (t1min, t1max) = ExRay1.Intersect(bounds);
            var (tlmin, tlmax) = Geometry.LineSegment.IntersectWithBox(ExRay0.Location, ExRay1.Location, bounds);

            var iPointsExRay0 = new List<SKPoint>();
            var iPointsExRay1 = new List<SKPoint>();
            var iPointsL = new List<SKPoint>();
            var iPoints = new List<SKPoint>();
            var dirL = ExRay1.Location - ExRay0.Location;

            if (!float.IsNaN(t0min) & !float.IsNaN(t0max)) {
                if (t0min >= 0 & t0max >= 0) {
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0max));
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0min));
                }
                else if (t0min < 0 & t0max >= 0) {
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0max));
                    //iPointsExRay0.Add(ExRay0.Location);
                }
                else if (t0min < 0 & t0max < 0) {
                    // Outside
                    Console.WriteLine("ExRay0 Outside");
                    ;
                }
                else {
                    throw new Exception("Unhandled condition.");
                }
            }

            if (!float.IsNaN(tlmin) & !float.IsNaN(tlmax)) {
                if (tlmin < 0.0f & tlmax >= 1.0f) {
                    iPointsL.Add(ExRay0.Location);
                    iPointsL.Add(ExRay1.Location);
                    //iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmin));
                    //iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmax));
                }
                else if (tlmin < 0 & tlmax >= 0 & tlmax <= 1.0f) {
                    iPointsL.Add(ExRay0.Location);
                    iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmax));
                }
                else if (tlmin >= 0 & tlmin <= 1.0f & tlmax >= 0 & tlmax <= 1.0f) {
                    iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmin));
                    iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmax));
                }
                else if (tlmin >= 0 & tlmin <= 1.0f & tlmax > 1.0f) {
                    iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmin));
                    iPointsL.Add(ExRay1.Location);
                }
                else if (tlmin < 0 & tlmax < 0) {
                    Console.WriteLine("Line Segment Outside");
                }
                else if (tlmin > 1.0f & tlmax > 1.0f) {
                    Console.WriteLine("Line Segment Outside");
                }
                else {
                    throw new Exception("Unhandled condition.");
                }
            }

            if (!float.IsNaN(t1min) & !float.IsNaN(t1max)) {
                if (t1min >= 0 & t1max >= 0) {
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1min));
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1max));
                }
                else if (t1min < 0 & t1max >= 0) {
                    //iPointsExRay1.Add(ExRay1.Location);
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1max));
                }
                else if (t1min < 0 & t1max < 0) {
                    // Outside
                    Console.WriteLine("ExRay1 Outside");
                    ;
                }
                else {
                    throw new Exception("Unhandled condition.");
                }
            }

            iPoints.AddRange(iPointsExRay0);
            iPoints.AddRange(iPointsL);
            iPoints.AddRange(iPointsExRay1);


            var lt = new SKPoint(bounds.Left, bounds.Top);
            var lb = new SKPoint(bounds.Left, bounds.Bottom);
            var rt = new SKPoint(bounds.Right, bounds.Top);
            var rb = new SKPoint(bounds.Right, bounds.Bottom);
            // Special case 1: no intersection at all
            if (iPoints.Count == 0) {
                var ret0 = this.ContainsPoint(lt);
                var ret1 = this.ContainsPoint(lb);
                var ret2 = this.ContainsPoint(rb);
                var ret3 = this.ContainsPoint(rt);

                if (ret0 & ret1 & ret2 & ret3)
                    iPoints.AddRange(new SKPoint[] { lt, lb, rb, rt });
            }
            else {
                SKPoint first, last;
                SKPoint[] contains;

                if (iPointsExRay0.Count == 2 & iPointsExRay1.Count == 2 & iPointsL.Count == 0) {
                    first = iPoints[1];
                    last = iPoints[2];
                    contains = this.GetCorners(bounds, first, last);

                    iPoints.InsertRange(2, contains);
                }

                first = iPoints.First();
                last = iPoints.Last();
                contains = this.GetCorners(bounds, last, first);

                iPoints.AddRange(contains);
            }

            return iPoints.ToArray();
        }

        public override void Draw(SKCanvas sKCanvas) {
            var stroke = new SKPaint() { Color = SKColors.DeepSkyBlue, IsAntialias = true, StrokeWidth = 2.0f, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Bevel };
            var fill = new SKPaint() { Color = SKColors.BlueViolet.WithAlpha(0.5f), IsAntialias = true, Style = SKPaintStyle.Fill, };
            var text = new SKPaint() { Color = SKColors.IndianRed, IsAntialias = true, StrokeWidth = 1.0f, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Bevel, TextSize = 18.0f };
            var path = new SKPath();

            SKRectI bounds;
            sKCanvas.GetDeviceClipBounds(out bounds);

            try {
                SKPoint[] points;

                points = this.Measure(bounds);


                var idx = 0;
                foreach (var p in points) {
                    sKCanvas.DrawCircle(p, 10.0f, stroke);
                    sKCanvas.DrawText($"{idx + 1}", p, text);

                    if (idx == 0) {
                        path.MoveTo(p);
                    }
                    else {
                        path.LineTo(p);
                    }

                    ++idx;
                }

                path.Close();

                if (ExRay0.Location != ExRay1.Location) {
                    var colors = new SKColor[] {
                        SKColors.Blue.WithAlpha(0.5f),
                        SKColors.White.WithAlpha(0.5f)
                    };
                    var dir = ExRay1.Location - ExRay0.Location;
                    var perp = SKMatrix.CreateRotationDegrees(90).MapPoint(dir);
                    var shader = SKShader.CreateLinearGradient(
                        ExRay0.Location + dir.Multiply(0.5f),
                        ExRay0.Location + dir.Multiply(0.5f) + perp,
                        colors,
                        null,
                        SKShaderTileMode.Clamp);

                    fill.Shader = shader;
                }

                sKCanvas.DrawPath(path, fill);
            }
            catch {
                ;
            }
            finally {
                //ExRay0.Draw(sKCanvas);
                //sKCanvas.DrawText("Ray0", ExRay0.Location, text);
                //ExRay1.Draw(sKCanvas);
                //sKCanvas.DrawText("Ray1", ExRay1.Location, text);
                stroke.StrokeWidth = 3.0f;
                sKCanvas.DrawLine(ExRay0.Location, ExRay1.Location, stroke);
            }
        }
    }

    /// <summary>
    /// VoronoiRegion Type1 : same vertex, no excluded simplex.
    /// </summary>
    public class VoronoiRegion_Type1 : CanvasObject_v2, IVoronoiRegion {
        public ExteriorRay_v3 ExRay0 { get; set; }
        public ExteriorRay_v3 ExRay1 { get; set; }
        public Simplex_v2 Triangle0 { get; set; }
        public Simplex_v2 Triangle1 { get; set; }

        private SKPaint stroke = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DeepSkyBlue,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public VoronoiRegion_Type1(ExteriorRay_v3 ray0, ExteriorRay_v3 ray1, Simplex_v2 tri0, Simplex_v2 tri1) {
            this.ExRay0 = ray0;
            this.ExRay1 = ray1;
            this.Triangle0 = tri0;
            this.Triangle1 = tri1;
        }

        public override bool ContainsPoint(SKPoint p) {
            var ret0 = Geometry.LineSegment.GetSide(ExRay0.Location, ExRay0.Location + ExRay0.Direction, p) <= 0;
            //var ret1 = Geometry.LineSegment.GetSide(ExRay0.Location, ExRay1.Location, p) >= 0;
            var ret2 = Geometry.LineSegment.GetSide(ExRay1.Location, ExRay1.Location + ExRay1.Direction, p) >= 0;

            return ret0 & ret2;
        }

        private SKPoint[] GetCorners(SKRect bounds, SKPoint start, SKPoint end) {
            var lt = new SKPoint(bounds.Left, bounds.Top);
            var lb = new SKPoint(bounds.Left, bounds.Bottom);
            var rt = new SKPoint(bounds.Right, bounds.Top);
            var rb = new SKPoint(bounds.Right, bounds.Bottom);

            var ccw = new CircularList<LineSegment> {
                new LineSegment() { P0 = lt, P1 = lb },
                new LineSegment() { P0 = lb, P1 = rb },
                new LineSegment() { P0 = rb, P1 = rt },
                new LineSegment() { P0 = lb, P1 = lt },
            };
            var cw = new CircularList<LineSegment> {
                new LineSegment() {P0 = lt, P1 = rt },
                new LineSegment() {P0 = rt, P1 = rb },
                new LineSegment() {P0 = rb, P1 = lb },
                new LineSegment() {P0 = lb, P1 = lt },
            };

            var it = cw.First;
            var points = new List<SKPoint>();

            var init = it;
            do {
                if (Geometry.LineSegment.IsPointOnLine(start, it.Value.P0, it.Value.P1))
                    break;
                else
                    it = it.Next;
            } while (it != init);

            init = it;
            do {
                if (Geometry.LineSegment.IsPointOnLine(end, it.Value.P0, it.Value.P1))
                    break;
                else {
                    points.Add(it.Value.P1);
                    it = it.Next;
                }
            } while (it != init);

            return points.ToArray();
        }

        private SKPoint[] Measure(SKRect bounds) {
            var (t0min, t0max) = ExRay0.Intersect(bounds);
            var (t1min, t1max) = ExRay1.Intersect(bounds);

            var iPointsExRay0 = new List<SKPoint>();
            var iPointsExRay1 = new List<SKPoint>();
            var iPoints = new List<SKPoint>();

            if (!float.IsNaN(t0min) & !float.IsNaN(t0max)) {
                if (t0min >= 0 & t0max >= 0) {
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0max));
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0min));
                }
                else if (t0min < 0 & t0max >= 0) {
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0max));
                    //iPointsExRay0.Add(ExRay0.Location);
                }
                else if (t0min < 0 & t0max < 0) {
                    // Outside
                    Console.WriteLine("ExRay0 Outside");
                    ;
                }
                else {
                    throw new Exception("Unhandled condition.");
                }
            }

            if (!float.IsNaN(t1min) & !float.IsNaN(t1max)) {
                if (t1min >= 0 & t1max >= 0) {
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1min));
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1max));
                }
                else if (t1min < 0 & t1max >= 0) {
                    //iPointsExRay1.Add(ExRay1.Location);
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1max));
                }
                else if (t1min < 0 & t1max < 0) {
                    // Outside
                    Console.WriteLine("ExRay1 Outside");
                    ;
                }
                else {
                    throw new Exception("Unhandled condition.");
                }
            }

            iPoints.AddRange(iPointsExRay0);
            //iPoints.Add(ExRay0.Location);
            iPoints.AddRange(iPointsExRay1);

            var lt = new SKPoint(bounds.Left, bounds.Top);
            var lb = new SKPoint(bounds.Left, bounds.Bottom);
            var rt = new SKPoint(bounds.Right, bounds.Top);
            var rb = new SKPoint(bounds.Right, bounds.Bottom);
            // Special case 1: no intersection at all
            if (iPoints.Count == 0) {
                var ret0 = this.ContainsPoint(lt);
                var ret1 = this.ContainsPoint(lb);
                var ret2 = this.ContainsPoint(rt);
                var ret3 = this.ContainsPoint(rb);

                if (ret0 & ret1 & ret2 & ret3)
                    iPoints.AddRange(new SKPoint[] { lt, lb, rt, rb });
            }
            else {
                SKPoint first, last;
                SKPoint[] contains;

                if (iPointsExRay0.Count == 2 & iPointsExRay1.Count == 2) {
                    first = iPoints[1];
                    last = iPoints[2];
                    contains = this.GetCorners(bounds, first, last);

                    iPoints.InsertRange(2, contains);
                }

                first = iPoints.First();
                last = iPoints.Last();
                contains = this.GetCorners(bounds, last, first);

                iPoints.AddRange(contains);
            }

            if (iPointsExRay0.Count == 1) {
                iPoints.Insert(1, ExRay0.Location);
            }

            return iPoints.ToArray();
        }

        public override void Draw(SKCanvas sKCanvas) {
            var stroke = new SKPaint() { Color = SKColors.DeepSkyBlue, IsAntialias = true, StrokeWidth = 2.0f, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Bevel };
            var fill = new SKPaint() { Color = SKColors.BlueViolet.WithAlpha(0.5f), IsAntialias = true, Style = SKPaintStyle.Fill, };
            var text = new SKPaint() { Color = SKColors.IndianRed, IsAntialias = true, StrokeWidth = 1.0f, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Bevel, TextSize = 18.0f };
            var path = new SKPath();

            SKRectI bounds;
            sKCanvas.GetDeviceClipBounds(out bounds);

            try {
                SKPoint[] points;

                points = this.Measure(bounds);

                var idx = 0;
                foreach (var p in points) {
                    sKCanvas.DrawCircle(p, 10.0f, stroke);
                    sKCanvas.DrawText($"{idx + 1}", p, text);

                    if (idx == 0) {
                        path.MoveTo(p);
                    }
                    else {
                        path.LineTo(p);
                    }

                    ++idx;
                }

                path.Close();

                if (ExRay0.Location != ExRay1.Location) {
                    var colors = new SKColor[] {
                        SKColors.Blue.WithAlpha(0.5f),
                        SKColors.White.WithAlpha(0.5f)
                    };
                    var dir = ExRay1.Location - ExRay0.Location;
                    var perp = SKMatrix.CreateRotationDegrees(90).MapPoint(dir);
                    var shader = SKShader.CreateLinearGradient(
                        ExRay0.Location + dir.Multiply(0.5f),
                        ExRay0.Location + dir.Multiply(0.5f) + perp,
                        colors,
                        null,
                        SKShaderTileMode.Clamp);

                    fill.Shader = shader;
                }

                sKCanvas.DrawPath(path, fill);
            }
            catch {
                ;
            }
            finally {
                //ExRay0.Draw(sKCanvas);
                //sKCanvas.DrawText("Ray0", ExRay0.Location, text);
                //ExRay1.Draw(sKCanvas);
                //sKCanvas.DrawText("Ray1", ExRay1.Location, text);
                stroke.StrokeWidth = 3.0f;
                sKCanvas.DrawLine(ExRay0.Location, ExRay1.Location, stroke);
            }
        }
    }

    /// <summary>
    /// VoronoiRegion Type1 : same vertex, no excluded simplex.
    /// </summary>
    public class VoronoiRegion_Type2 : VoronoiRegion_Type0 {

        public VoronoiRegion_Type2(ExteriorRay_v3 ray0, ExteriorRay_v3 ray1, Simplex_v2 triangle) : base(ray0, ray1, triangle) { }

        public override bool ContainsPoint(SKPoint p) {
            return base.ContainsPoint(p) & !this.Triangle.ContainsPoint(p);
        }
    }

    public class VoronoiRegion : CanvasObject_v2 {
        public ExteriorRay_v3 ExteriorRay0 { get; set; }
        public ExteriorRay_v3 ExteriorRay1 { get; set; }
        public Entity_v2 ExcludedEntity { get; set; }

        private SKPaint stroke = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DeepSkyBlue,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public VoronoiRegion(ExteriorRay_v3 ray0, ExteriorRay_v3 ray1, Entity_v2 exEntity) {
            this.ExteriorRay0 = ray0;
            this.ExteriorRay1 = ray1;
            this.ExcludedEntity = exEntity;
        }

        public override bool ContainsPoint(SKPoint point) {
            var lines = new List<LineSegment>();

            lines.Add(new LineSegment() {
                P0 = ExteriorRay0.E0.Location + ExteriorRay0.Direction,
                P1 = ExteriorRay0.E0.Location
            });

            if (ExcludedEntity != null) {
                lines.Add(new LineSegment {
                    P0 = ExteriorRay0.E0.Location,
                    P1 = ExcludedEntity.Location
                });
                lines.Add(new LineSegment {
                    P0 = ExcludedEntity.Location,
                    P1 = ExteriorRay1.E0.Location,
                });
            }
            else {
                if (ExteriorRay0.E0 != ExteriorRay1.E0) {
                    lines.Add(new LineSegment {
                        P0 = ExteriorRay0.E0.Location,
                        P1 = ExteriorRay1.E0.Location
                    });
                }
            }

            lines.Add(new LineSegment() {
                P0 = ExteriorRay1.E0.Location,
                P1 = ExteriorRay1.E0.Location + ExteriorRay1.Direction
            });

            var results = new List<int>();
            foreach (var line in lines) {
                var a = line.P1 - line.P0;
                var b = point - line.P0;
                var crossProduct = a.X * b.Y - a.Y * b.X;
                var theta = Math.Asin(crossProduct / (a.Length * b.Length));

                results.Add(Math.Sign(theta));
            }

            // left < 0, right > 0 , in = 0
            if (results.Any(res => res > 0)) {
                return false; // out
            }
            else {
                return true; // on or in
            }
        }

        private int GetSide(SKPoint target) {
            var lines = new List<LineSegment>();

            lines.Add(new LineSegment() {
                P0 = ExteriorRay0.E0.Location + ExteriorRay0.Direction,
                P1 = ExteriorRay0.E0.Location
            });

            if (ExcludedEntity != null) {
                lines.Add(new LineSegment {
                    P0 = ExteriorRay0.E0.Location,
                    P1 = ExcludedEntity.Location
                });
                lines.Add(new LineSegment {
                    P0 = ExcludedEntity.Location,
                    P1 = ExteriorRay1.E0.Location,
                });
            } else {
                if (ExteriorRay0.E0 != ExteriorRay1.E0) {
                    lines.Add(new LineSegment {
                        P0 = ExteriorRay0.E0.Location,
                        P1 = ExteriorRay1.E0.Location
                    });
                }
            }

            lines.Add(new LineSegment() {
                P0 = ExteriorRay1.E0.Location,
                P1 = ExteriorRay1.E0.Location + ExteriorRay1.Direction
            });

            var results = new List<int>();
            foreach(var line in lines) {
                var a = line.P1 - line.P0;
                var b = target - line.P0;
                var crossProduct = a.X * b.Y - a.Y * b.X;
                var theta = Math.Asin(crossProduct / (a.Length * b.Length));

                results.Add(Math.Sign(theta));
            }

            // left < 0, right > 0 , in = 0
            if (results.Any(res => res > 0)) {
                return -1; // out
            } else if (results.All(res => res < 0)) {
                return 1; // in
            } else {
                return 0; // on
            }
        }
        
        private SKPath GetClipPath(SKCanvas sKCanvas) {
            var path = new SKPath();

            sKCanvas.GetDeviceClipBounds(out var bounds);

            // CCW
            var corners = new SKPoint[] {
                new SKPoint(bounds.Left, bounds.Top),
                new SKPoint(bounds.Left, bounds.Bottom),
                new SKPoint(bounds.Right, bounds.Bottom),
                new SKPoint(bounds.Right, bounds.Top),
            };

            path.MoveTo(ExteriorRay0.E0.Location + ExteriorRay0.Direction.Multiply(10));
            path.LineTo(ExteriorRay0.E0.Location);

            if (ExcludedEntity != null)
                path.LineTo(ExcludedEntity.Location);

            if (ExteriorRay0.E0 != ExteriorRay1.E0)
                path.LineTo(ExteriorRay1.E0.Location);

            path.LineTo(ExteriorRay1.E0.Location + ExteriorRay1.Direction.Multiply(10));

            foreach(var corner in corners) {
                if (this.ContainsPoint(corner))
                    path.LineTo(corner);
            }

            path.Close();


            return path;
        }

        public override void Draw(SKCanvas sKCanvas) {
            var path = this.GetClipPath(sKCanvas);

            //sKCanvas.DrawPath(path, stroke);
            sKCanvas.GetDeviceClipBounds(out var bounds);

            var region = new SKRegion(bounds);
            region.SetPath(path);
            sKCanvas.DrawRegion(region, stroke);
            //this.ExteriorRay0.Draw(sKCanvas);

            //if (this.ExcludedEntity != null) {
            //    sKCanvas.DrawLine(this.ExteriorRay0.E0.Location, this.ExcludedEntity.Location, stroke);
            //    sKCanvas.DrawLine(this.ExcludedEntity.Location, this.ExteriorRay1.E0.Location, stroke);
            //} else if (this.ExcludedEntity == null & this.ExteriorRay0.E0 != this.ExteriorRay1.E0) {
            //    sKCanvas.DrawLine(this.ExteriorRay0.E0.Location, this.ExteriorRay1.E0.Location, stroke);
            //}

            //this.ExteriorRay1.Draw(sKCanvas);
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

