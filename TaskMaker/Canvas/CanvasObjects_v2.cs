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

                foreach (var triIndices in output) {
                    var arrSelectedEntities = selectedEntities.ToArray();
                    var tri = new Entity_v2[] {
                            arrSelectedEntities[triIndices[0]],
                            arrSelectedEntities[triIndices[1]],
                            arrSelectedEntities[triIndices[2]]
                        };

                    this.SelectedLayer.Complex.Add(new Simplex_v2(tri));
                }
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

        public Vector<float> GetLambdas(SKPoint point) {
            return this.Pairs.TaskBary.GetLambdasOnlyInterior(point.ToVector());
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
        }
    }


    public class SimplicialComplex_v2 : List<Simplex_v2> {
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
        }
    }

    public class Edge_v2 : CanvasObject_v2 {
        public HashSet<Entity_v2> ExtremeSet { get; set; } = new HashSet<Entity_v2>();

        public void Add(Entity_v2 e0, Entity_v2 e1) {
            this.ExtremeSet.Add(e0);
            this.ExtremeSet.Add(e1);
        }

        public bool SetEquals(HashSet<Entity_v2> targetSet) {
            return this.ExtremeSet.SetEquals(targetSet);
        }

    }

    public class ExteriorZone_v2 : List<ExteriorRegion_v2> {
        public CircularList<Entity_v2> Extremes { get; set; } = new CircularList<Entity_v2>();
        public List<Edge_v2> Edges { get; set; } = new List<Edge_v2>();

        private void GetAllEdges(SimplicialComplex_v2 complex) {
            var edgeSet = this.Edges;

            foreach (var simplex in complex) {
                // Vertices Combination
                var set01 = new Edge_v2();
                var set02 = new Edge_v2();
                var set12 = new Edge_v2();

                set01.Add(simplex.Vertices[0], simplex.Vertices[1]);
                set02.Add(simplex.Vertices[0], simplex.Vertices[2]);
                set12.Add(simplex.Vertices[1], simplex.Vertices[2]);

                foreach (var result in edgeSet.Where(e => !e.SetEquals(set01.ExtremeSet))) {
                    edgeSet.Add(result);
                }
            }
        }

        private Edge_v2[] GetRelatedEdges(Entity_v2 vertex) {
            return this.Edges.Where(e => e.ExtremeSet.Contains(vertex)).ToArray();
        }

        private Edge_v2[] GetRelatedEdges(IEnumerable<Edge_v2> edgeSet, Entity_v2 vertex) {
            return edgeSet.Where(e => e.ExtremeSet.Contains(vertex)).ToArray();
        }

        private Edge_v2 FindEdge(Entity_v2 e0, Entity_v2 e1) {
            return this.GetRelatedEdges(this.GetRelatedEdges(e0), e1).FirstOrDefault();
        }

        private void SetExteriorRays() {
            var exteriorRays = new List<Ray_v2>();
            this.Clear();

            // For Order: CCW
            foreach (var node in this.Extremes) {
                var extreme = node.Value;
                //var edges = this.Edges.Where(edge => edge.HasVertex(extreme)).ToArray();
                var relatedEdges = this.GetRelatedEdges(extreme);
                var edgeCnt = relatedEdges.Count();

                if (edgeCnt == 2) {
                    //this.Logger.Debug($"No splitter for {extreme.Value}.");

                    var prev = node.Prev.Value;
                    var it = node.Value;
                    var next = node.Next.Value;

                    //exteriorRays.Add(new ExteriorRay() {
                    //    ExcludedTri = this.triangles.Find(tri => tri.IsVertex(prev) & tri.IsVertex(it) & tri.IsVertex(next))
                    //});
                }
                else if (edgeCnt == 3) {
                    // Note: this case has a special condition which needs to be handled.
                    // Angle between ray and each neighbor edge that is less than 90 needs to be restricted.
                    //this.Logger.Debug($"One splitter(Extension of edge) for {extreme.Value}.");

                    //var targetEdge = relatedEdges.Where(edge => !this.convexhullEdges.Contains(edge)).ElementAt(0);
                    var targetEdge = relatedEdges.Where(edge => !this.Extremes.Contains(edge.ExtremeSet.ToArray()[0]) | !this.Extremes.Contains(edge.ExtremeSet.ToArray()[1])).First();
                    var e0 = targetEdge.ExtremeSet.ToArray()[0];
                    var e1 = targetEdge.ExtremeSet.ToArray()[1];

                    // Extend this edge
                    var start = extreme;
                    var end = e0 == start ? e1 : e0;
                    var edgeDirection = start.Location - end.Location;
                    var rayOfEdgeExtension = Ray_v2.CreateRay(start.Location, edgeDirection);

                    //rayOfEdgeExtension.Color = SKColors.Green;
                    //rayOfEdgeExtension.p0 = start;

                    // Compare with Normals
                    var prev = node.Prev.Value;
                    var it = node.Value;
                    var next = node.Next.Value;

                    var dirOfEdgePrevToIt = it.Vector - prev.Vector;
                    var dirOfEdgeItToNext = next.Vector - it.Vector;
                    var normalOfEdgePI = new SKPoint(dirOfEdgePrevToIt[1], -dirOfEdgePrevToIt[0]);
                    var normalOfEdgeIN = new SKPoint(dirOfEdgeItToNext[1], -dirOfEdgeItToNext[0]);
                    var rayOfNormalPI = Ray_v2.CreateRay(it.Location, normalOfEdgePI);
                    var rayOfNoramlIN = Ray_v2.CreateRay(it.Location, normalOfEdgeIN);

                    rayOfNormalPI.E0 = it;
                    rayOfNoramlIN.E0 = it;

                    rayOfNormalPI.Color = SKColors.PowderBlue;
                    rayOfNoramlIN.Color = SKColors.PowderBlue;

                    // Recheck condition!
                    var angleOfEdgeExtensionAndNormalPI = Math.Atan2(
                        rayOfEdgeExtension.UnitDirection[0] * rayOfNormalPI.UnitDirection[1] -
                        rayOfEdgeExtension.UnitDirection[1] * rayOfNormalPI.UnitDirection[0],
                        rayOfEdgeExtension.UnitDirection[0] * rayOfNormalPI.UnitDirection[0] + rayOfEdgeExtension.UnitDirection[1] * rayOfNormalPI.UnitDirection[1]
                    );
                    var angleOfEdgeExtensionAndNormalIN = Math.Atan2(
                        rayOfEdgeExtension.UnitDirection[0] * rayOfNoramlIN.UnitDirection[1] -
                        rayOfEdgeExtension.UnitDirection[1] * rayOfNoramlIN.UnitDirection[0],
                        rayOfEdgeExtension.UnitDirection[0] *
                        rayOfNoramlIN.UnitDirection[0] + rayOfEdgeExtension.UnitDirection[1] *
                        rayOfNoramlIN.UnitDirection[1]
                    );

                    var exteriorRayNormalPI = new ExteriorRay() {
                        Ray = rayOfNormalPI,
                        Govorner = this.triangles.Find(tri => tri.IsVertex(prev) & tri.IsVertex(it))
                    };
                    var exteriorRayNormalIN = new ExteriorRay() {
                        Ray = rayOfNoramlIN,
                        Govorner = this.triangles.Find(tri => tri.IsVertex(it) & tri.IsVertex(next))
                    };

                    if (Math.Sign(angleOfEdgeExtensionAndNormalPI) == -1 & Math.Sign(angleOfEdgeExtensionAndNormalIN) == 1) {
                        exteriorRays.Add(new ExteriorRay() {
                            Ray = rayOfEdgeExtension,
                        });
                    }
                    else if (Math.Sign(angleOfEdgeExtensionAndNormalPI) == Math.Sign(angleOfEdgeExtensionAndNormalIN)) {
                        exteriorRays.Add(exteriorRayNormalPI);
                        exteriorRays.Add(exteriorRayNormalIN);
                    }
                }
                else if (edgeCnt > 3) {
                    this.Logger.Debug($"Two perpendicular splitter for {extreme.Value}.");

                    var prev = extreme.Prev.Value;
                    var it = extreme.Value;
                    var next = extreme.Next.Value;

                    var dirOfEdgePrevToIt = it.PointVector - prev.PointVector;
                    var dirOfEdgeItToNext = next.PointVector - it.PointVector;
                    var normalOfEdgePI = new SKPoint(dirOfEdgePrevToIt[1], -dirOfEdgePrevToIt[0]);
                    var normalOfEdgeIN = new SKPoint(dirOfEdgeItToNext[1], -dirOfEdgeItToNext[0]);
                    var rayOfNormalPI = Ray_v1.CreateRay(it.Point, normalOfEdgePI);
                    var rayOfNoramlIN = Ray_v1.CreateRay(it.Point, normalOfEdgeIN);

                    rayOfNormalPI.Color = SKColors.PowderBlue;
                    rayOfNormalPI.E0 = it;
                    rayOfNoramlIN.Color = SKColors.PowderBlue;
                    rayOfNoramlIN.E0 = it;

                    exteriorRays.Add(new ExteriorRay() {
                        Ray = rayOfNormalPI,
                        Govorner = this.triangles.Find(tri => tri.IsVertex(prev) & tri.IsVertex(it))
                    });
                    exteriorRays.Add(new ExteriorRay() {
                        Ray = rayOfNoramlIN,
                        Govorner = this.triangles.Find(tri => tri.IsVertex(it) & tri.IsVertex(next))
                    });
                }
                else {
                    throw new Exception("Splitter Exception");
                }
            }

            // Generate voronoi regions
            for (var idx = 0; idx < exteriorRays.Count; idx++) {
                Triangle_v1 excludedTri = null;
                Triangle_v1 governor = null;
                var idx0 = idx;
                var idx1 = idx + 1 < exteriorRays.Count ? idx + 1 : 0;

                var exRay0 = exteriorRays[idx0];
                var exRay1 = exteriorRays[idx1];
                // TODO: Ugly

                if (exRay0.ExcludedTri == null) {
                    if (exRay1.ExcludedTri != null) {
                        excludedTri = exRay1.ExcludedTri;

                        idx1 = idx1 + 1 < exteriorRays.Count ? idx1 + 1 : 0;
                        exRay1 = exteriorRays[idx1];

                        idx++;
                    }
                    else {
                        if (exRay0.Govorner == null & exRay1.Govorner == null) {
                            governor = this.triangles.Find(tri => tri.IsVertex(exRay0.Ray.E0) & tri.IsVertex(exRay1.Ray.E0));
                        }
                    }


                    this.voronoiRegions.Add(
                        new VoronoiRegion_v1() {
                            Index = this.voronoiRegions.Count,
                            ExteriorRay0 = exRay0,
                            ExteriorRay1 = exRay1,
                            ExcludedTri = excludedTri,
                            Governor = governor,
                        }
                    );
                }
            }
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

