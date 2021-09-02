using SkiaSharp;
using System;
using System.Collections.Generic;
using Reparameterization;
using MathNet.Numerics.LinearAlgebra;
using System.Text;
using System.Windows.Forms;

namespace TuggingController {
    public class Canvas {
        //public WorldSpaceCoordinate World { get; set; }
        public List<Layer> Layers { get; set; } = new List<Layer>();
        public List<Entity_v2> Entities { get; set; } = new List<Entity_v2>();
        public List<Simplex_v2> Simplices { get; set; } = new List<Simplex_v2>();

        public Canvas() { }

        //public void AddEntity(Entity_v2 entity) {
        //    this.Entities.Add()
        //}
        public void Draw(SKCanvas sKCanvas) {
            foreach(var s in this.Simplices) {
                s.DrawThis(sKCanvas);
            }

            foreach(var e in this.Entities) {
                e.Draw(sKCanvas);
            }

        }
    }

    [Serializable]
    public class Layer : TreeNode {
        public Layer NextLayer => (Layer)this.NextNode;

        public Layer() {
            this.Text = "NewLayer";
        }

        public Layer(string name) {
            this.Text = name;
        }

        protected Layer(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) {
            throw new NotImplementedException();
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

    public class SelectionTool {
        public SKPoint Location { get; set; }
        //public SKSize Size { get; set; }
        //public SKRect Rect => new SKRect() {
        //    Location = this.Location,
        //    Size = this.Size,
        //};
        public SKPath Path { get; set; }
        
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

        public SelectionTool(SKPoint start) {
            this.Path = new SKPath();
            this.Location = start;

            this.Path.MoveTo(start);
        }

        public bool Contains(SKPoint point) {
            return this.Path.Contains(point.X, point.Y);
        }

        public void AddNode(SKPoint node) {
            this.Path.LineTo(node);
        }

        public void ClosePath() {
            this.Path.Close();
        }

        public void Invalidate() {

        }

        public void DrawThis(SKCanvas skCanvas) {
            skCanvas.DrawPath(this.Path.Simplify(), this.strokePaint);
            //skCanvas.DrawRect(this.Path.GetRect(), this.strokePaint);
        }

    }

    public abstract class CanvasObject_v2 {
        public Canvas Canvas { get; set; }
        public Layer Layer { get; set; }
        public List<CanvasObject_v2> Children { get; set; } = new List<CanvasObject_v2>();
        public List<IComponent> Components { get; set; } = new List<IComponent>();
        public bool IsSelected { get; set; }
        //public Transform_v2 T {
        //    get => this._transform;
        //    set {
        //        this._transform = value;
        //    }
        //}
        virtual public SKPoint Location { get; set; }
        //public SKPoint GlobalLocation => this.T.GlobalTransform.MapPoint(this.Location);


        //protected Transform_v2 _transform = new Transform_v2();

        protected CanvasObject_v2() {

        }


        /// <summary>
        /// <see langword="abstract"/>
        /// This method is called before draw anything on the canvas,
        /// and returns a global position for drawing.
        /// </summary>
        protected virtual void Invalidate() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <see langword="abstract"/>
        /// This method is called by Draw() method, if there is anything
        /// of itself need to be drawed.
        /// </summary>
        /// <param name="canvas">Target Canvas</param>
        protected virtual void DrawThis(SKCanvas canvas) {
            throw new NotImplementedException();
        }

        public virtual bool ContainsPoint(SKPoint point) {
            throw new NotImplementedException();
        }

        public void Draw(SKCanvas canvas) {
            // Redraw
            // Invalidate() first, then DrawThis() and Draw() of all children.
            this.Invalidate();
            this.DrawThis(canvas);

            foreach (var child in this.Children) {
                child.Draw(canvas);
            }
        }
    }

    public class Entity_v2 : CanvasObject_v2 {
        public int Index { get; set; }
        public Pair Pair { get; set; } = new Pair();
        public Vector<float> PointVector { get; set; }
        public SKPoint Point {
            get => SkiaExtension.SkiaHelper.ToSKPoint(this.PointVector);
            set {
                this.PointVector = SkiaHelper.ToVector(value);
            }
        }
        override public SKPoint Location {
            get => this.Point;
            set {
                this.PointVector = SkiaHelper.ToVector(value);
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
        private SKPoint _gLocation;
        private float _radius = 5.0f;
        private float _gRadius;

        public Entity_v2() : base() { }

        public override string ToString() {
            StringBuilder str = new StringBuilder();

            str.Append($"[Entity {this.Index}] - {this.Point}");
            return str.ToString();
        }

        public override bool ContainsPoint(SKPoint point) {
            return SKPoint.Distance(point, this.Location) <= this._radius;
        }

        protected override void Invalidate() {
            this._gLocation = this.Location;
            this._gRadius = this._radius;

            if (this.IsSelected) {
                this._gRadius += 2.0f;
                this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Chocolate, 0.8f);
            } else {
                if (this.Pair.IsPaired) {
                    this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Red, 0.8f);
                } else {
                    this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.8f);
                }
            }
        }

        protected override void DrawThis(SKCanvas canvas) {
            canvas.DrawCircle(this._gLocation, this._gRadius, this.fillPaint);
            canvas.DrawCircle(this._gLocation, this._gRadius, this.strokePaint);
        }
    }

    public class Simplex_v2 {
        public SKPoint Location { get; set; }
        public List<Entity_v2> Vertices { get; set; } = new List<Entity_v2>();

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

        public Simplex_v2() { }

        public Simplex_v2(ICollection<Entity_v2> entities) : base() {
            this.Vertices.AddRange(entities);
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


    public class SimplicialComplex_v2 {
        //public 
    }
}
