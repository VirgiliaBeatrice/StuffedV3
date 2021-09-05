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

namespace TaskMaker {
    
    public class Canvas {
        public bool IsShownPointer { get; set; } = false;
        public bool IsShownPointerTrace { get; set; } = false;
        public Layer RootLayer { get; set; } = new Layer("Root");
        public Layer SelectedLayer { get; set; }
        public ISelectionTool SelectionTool { get; set; }
        public PointerTrace PointerTrace { get; set; }
        public CrossPointer Pointer { get; set; }
        //public LinearSlider testSlider { get; set; }

        private Mapping.Triangulation _triangulation;

        public Canvas() {
            this._triangulation = new Mapping.Triangulation();

            this.RootLayer.Nodes.Add(new Layer());
            this.SelectedLayer = this.RootLayer;

            this.Pointer = new CrossPointer();

            //this.testSlider = new LinearSlider(new SKPoint(100, 100));
        }

        public void SelectLayer(Layer layer) {
            this.SelectedLayer = layer;
        }

        public void Reset() {
            this.SelectedLayer.Entities.ForEach(e => e.IsSelected = false);
            this.IsShownPointer = false;
            this.IsShownPointerTrace = false;
        }

        public void Draw(SKCanvas sKCanvas) {
            this.SelectedLayer.Complex.Draw(sKCanvas);
            this.SelectedLayer.Entities.ForEach(e => e.Draw(sKCanvas));

            if (this.SelectionTool != null) {
                this.SelectionTool.DrawThis(sKCanvas);
            }

            if (this.IsShownPointer) {
                this.Pointer.Draw(sKCanvas);
            }

            if (this.IsShownPointerTrace) {
                this.PointerTrace.Draw(sKCanvas);
            }

            //this.testSlider.Percentage = (new Random()).Next(0, 100);
            //this.testSlider.Draw(sKCanvas);
        }

        public bool Triangulate() {
            var selectedEntities = this.SelectedLayer.Entities.Where(e => e.IsSelected);
            this.SelectedLayer.Complex = new SimplicialComplex_v2();

            // Case: amount less than 3
            if (selectedEntities.Count() <= 3) {
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
                var output = this._triangulation.RunDelaunay_v1(2, input.Length / 2, ref input);

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
        public SKPoint Pointer { get; set; } = SKPoint.Empty;
        public List<Entity_v2> Entities { get; set; } = new List<Entity_v2>();
        public SimplicialComplex_v2 Complex { get; set; } = new SimplicialComplex_v2();
        public Layer NextLayer => (Layer)this.NextNode;

        public Layer() {
            this.Text = "NewLayer";
        }

        public Layer(string name) {
            this.Text = name;
        }
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
        private float _radius = 5.0f;
        private float _gRadius;
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

        public void Draw(SKCanvas sKCanvas) {
            this.ForEach(sim => sim.DrawThis(sKCanvas));
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

