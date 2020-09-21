using MathNet.Spatial.Units;
using NLog;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml.Schema;
using System.Reflection;
using System.Windows.Forms;
using Reparameterization;
using NLog.LayoutRenderers.Wrappers;
using System.ComponentModel;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Single;
using System.Xml.XPath;
using MathNet.Numerics.LinearAlgebra;

namespace TuggingController {

    public class BehaviorArgs {
        public BehaviorArgs() { }
    }

    public class BehaviorResult {
        public bool ToNext { get; set; } = true;
        public BehaviorResult() { }
    }

    public class DragAndDropBehaviorArgs : BehaviorArgs {
        private SKPoint _location;
        public SKPoint Location => this._location;
        public SKPoint Origin { get; set; }
        public SKPoint Anchor { get; set; }
        public SKMatrix InitialTranslation { get; set; }

        public DragAndDropBehaviorArgs(int x, int y) {
            this._location = new SKPoint() { X = x, Y = y };
        }

        public DragAndDropBehaviorArgs(SKPoint location) {
            this._location = location;
        }
    }

    public class SelectableBehaviorArgs : BehaviorArgs {
        private SKPoint _location;
        public SKPoint Location => this._location;

        public SelectableBehaviorArgs(int x, int y) {
            this._location = new SKPoint() { X = x, Y = y };
        }
    }

    public class HoverBehaviorArgs : BehaviorArgs {
        public bool IsInside { get; set; }

        public HoverBehaviorArgs(bool isInside) {
            this.IsInside = isInside;
        }
    }

    public class SelectableBehaviorResult : BehaviorResult {
        public ICanvasObject Target { get; }

        public SelectableBehaviorResult(ICanvasObject target) {
            this.Target = target;
        }
    }


    public class DragAndDropComponentEventArgs : EventArgs {
        public SKPoint Anchor { get; set; }
        private SKPoint _newLocation;
        public SKPoint NewLocation => this._newLocation;
        
        public DragAndDropComponentEventArgs(SKPoint point, SKPoint anchor) {
            this._newLocation = point;
            this.Anchor = anchor;
        }
    }


    public interface ICanvasObject : ICanvasObjectNode, ICanvasObjectEvents {
        EventDispatcher<ICanvasObject> Dispatcher { get; }

        bool IsSelected { get; set; }

        SKRect BoarderBox { get; }
        //SKSize Size { get; set; }
        //float Scale { get; set; }
        Transform Transform { get; set; }
        SKPoint Location { get; set; }
        SKPoint GlobalLocation { get; }
        //List<ICanvasObject> Children { get; set; }
        List<IComponent> Components { get; set; }
        void Draw(SKCanvas canvas);
        void Draw(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate);
        BehaviorResult Execute(BehaviorArgs e, string tag);
        void AddComponent(IComponent component);
        void AddComponents(IEnumerable<IComponent> components);
        void SetBoarderBoxFromParent(SKRect pBoarderBox);
        SKSize GetSize();
    }

    public interface ICanvasObjectNode {
        List<ICanvasObject> Children { get; set; }
        //List<TreeNode> TreeNodeChildren { get; }
        bool ContainsPoint(SKPoint point);
        TreeNode[] GetChildrenTreeNodes();
    }

    public interface ICanvasObjectEvents {
        event EventHandler_v1 MouseEnter;
        event EventHandler_v1 MouseLeave;
        event EventHandler_v1 MouseMove;
        event EventHandler_v1 MouseUp;
        event EventHandler_v1 MouseDown;
        event EventHandler_v1 MouseClick;
        event EventHandler_v1 MouseDoubleClick;
        event EventHandler_v1 MouseWheel;
        event EventHandler_v1 DragStart;
        event EventHandler_v1 DragEnd;
        event EventHandler_v1 Dragging;
    }

    public abstract partial class CanvasObject_v1 {
        public event EventHandler_v1 MouseEnter;
        public event EventHandler_v1 MouseLeave;
        public event EventHandler_v1 MouseMove;
        public event EventHandler_v1 MouseUp;
        public event EventHandler_v1 MouseDown;
        public event EventHandler_v1 MouseClick;
        public event EventHandler_v1 MouseDoubleClick;
        public event EventHandler_v1 MouseWheel;
        public event EventHandler_v1 DragStart;
        public event EventHandler_v1 DragEnd;
        public event EventHandler_v1 Dragging;

        public bool IsMouseOver { get; set; } = false;

        public SKPoint PointerPosition { get; set; }

        public virtual void OnMouseEnter(Event @event) {
            this.MouseEnter?.Invoke(@event);
        }

        public virtual void OnMouseLeave(Event @event) {
            this.MouseLeave?.Invoke(@event);
        }

        public virtual void OnMouseMove(Event @event) {
            this.MouseMove?.Invoke(@event);
        }

        public virtual void OnMouseUp(Event @event) {
            this.MouseUp?.Invoke(@event);
        }

        public virtual void OnMouseDown(Event @event) {
            this.MouseDown?.Invoke(@event);
        }

        public virtual void OnMouseClick(Event @event) {
            this.MouseClick?.Invoke(@event);
        }

        public virtual void OnMouseDoubleClick(Event @event) {
            this.MouseDoubleClick?.Invoke(@event);
        }

        public virtual void OnMouseWheel(Event @event) {
            this.MouseWheel?.Invoke(@event);
        }

        public virtual void OnDragStart(Event @event) {
            this.DragStart?.Invoke(@event);
        }

        public virtual void OnDragEnd(Event @event) {
            this.DragEnd?.Invoke(@event);
        }

        public virtual void OnDragging(Event @event) {
            this.Dragging?.Invoke(@event);
        }
    }

    public abstract partial class CanvasObject_v1 : ILog, ICanvasObject {

        protected bool _isDebug = true;
        protected Transform _transform = new Transform();
        //protected float _scale = 1.0f;
        protected event EventHandler ScaleChanged;
        protected SKRect _pBoarderBox = new SKRect();

        public EventDispatcher<ICanvasObject> Dispatcher => EventDispatcher<ICanvasObject>.GetSingleton();
        public Logger Logger { get; protected set; } = LogManager.GetCurrentClassLogger();
        public SKRect BoarderBox {
            get => this.Transform.InvLocalTransformation.MapRect(this._pBoarderBox);
        }
        public Transform Transform { 
            get => this._transform;
            set {
                this._transform = value;
                this._transform.CanvasObject = this;
            } 
        }
        public virtual SKPoint Location { get; set; } = new SKPoint();
        public List<ICanvasObject> Children { get; set; } = new List<ICanvasObject>();
        public List<IComponent> Components { get; set; } = new List<IComponent>();

        public SKPoint GlobalLocation {
            get {
                return this.Transform.GlobalTransformation.MapPoint(this.Location);
            }
        }

        protected PaintComponent PaintComponent { get; set; } = new PaintComponent();
        public bool IsSelected { get; set; } = false;

        /// <summary>
        /// Constructor
        /// </summary>
        protected CanvasObject_v1() {
            this._transform.CanvasObject = this;

            var config = new NLog.Config.LoggingConfiguration();
            var logConsole = new NLog.Targets.ColoredConsoleTarget("Form1");
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            NLog.LogManager.Configuration = config;

            // Register default event handler
            //this.MouseEnter += OnMouseEnter;
            //this.MouseLeave += OnMouseLeave;
            //this.MouseMove += OnMouseMove;

            this.ScaleChanged += OnScaleChanged;
        }

        protected virtual void OnScaleChanged(object sender, EventArgs e) { }

        public virtual SKSize GetSize() {
            return this.BoarderBox.Size;
        }

        public void SetBoarderBoxFromParent(SKRect pBoarderBox) {
            this._pBoarderBox = pBoarderBox;

            foreach (var child in this.Children) {
                child.SetBoarderBoxFromParent(this.BoarderBox);
            }
        }

        /// <summary>
        /// <see langword="abstract"/>
        /// This method is called before draw anything on the canvas,
        /// and returns a global position for drawing.
        /// </summary>
        [Obsolete("This method is obsolete.", false)]
        protected virtual void Invalidate() {
            throw new NotImplementedException();
        }

        protected virtual void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <see langword="abstract"/>
        /// This method is called by Draw() method, if there is anything
        /// of itself need to be drawed.
        /// </summary>
        /// <param name="canvas">Target Canvas</param>
        [Obsolete("This method is obsolete.", false)]
        protected virtual void DrawThis(SKCanvas canvas) {
            throw new NotImplementedException();
        }

        protected virtual void DrawThis(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate) {
            throw new NotImplementedException();
        }

        public virtual bool ContainsPoint(SKPoint point) {
            throw new NotImplementedException();
        }


        internal void SetParent(CanvasObject_v1 parent) {
            this.Transform.Parent = parent._transform;
        }

        public virtual void Draw(SKCanvas canvas) {
            // Redraw
            // Invalidate() first, then DrawThis() and Draw() of all children.
            this.Invalidate();
            this.DrawThis(canvas);

            foreach(var child in this.Children) {
                child.Draw(canvas);
            }
        }

        protected virtual BehaviorResult ExecuteThis(BehaviorArgs e, string tag = "") { return new BehaviorResult(); }

        public virtual BehaviorResult Execute(BehaviorArgs e, string tag = "") {
            var result = new BehaviorResult();

            // Execute this
            result = this.ExecuteThis(e, tag);

            if (!result.ToNext) {
                return result;
            }

            // Execute children
            foreach(var child in this.Children.ToArray().Reverse()) {
                result = child.Execute(e, tag);

                if (!result.ToNext) {
                    break;
                }
            }

            return result;
        }

        public virtual void AddComponents(IEnumerable<IComponent> components) {
            foreach(var component in components) {
                component.CanvasObject = this;
            }

            this.Components.AddRange(components);
        }

        public virtual void AddComponent(IComponent component) {
            component.CanvasObject = this;

            this.Components.Add(component);
        }

        public CanvasObject_v1 Clone() {
            throw new NotImplementedException();
        }

        public void Draw(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate) {
            // Redraw
            // Invalidate() first, then DrawThis() and Draw() of all children.
            this.Invalidate(worldCoordinate);
            this.DrawThis(canvas, worldCoordinate);

            foreach (var child in this.Children) {
                child.Draw(canvas, worldCoordinate);
            }
        }

        public TreeNode[] GetChildrenTreeNodes() {
            var childrenNodes = new List<TreeNode>();

            foreach(var child in this.Children) {
                childrenNodes.Add(new TreeNode(child.ToString(), child.GetChildrenTreeNodes()) { Tag = child });
            }
            return childrenNodes.ToArray();
        }
    }

    public abstract class ContainerCanvasObject_v1 : CanvasObject_v1 {
        public override void Draw(SKCanvas canvas) {
            // Redraw
            // Invalidate() first, then DrawThis() and Draw() of all children.

            foreach (var child in this.Children) {
                child.Draw(canvas);
            }
        }

        public override bool ContainsPoint(SKPoint point) {
            return true;
        }
    }

    public partial class Entity_v1 : CanvasObject_v1 {
        private DragAndDropComponent _dragAndDropComponent;
        private SelectableComponent selectableComponent;
        private SKPoint _gLocation;
        private float _radius = 5.0f;
        private float _gRadius;

        public int Index { get; set; }

        private SKPaint _fillPaint = new SKPaint {
            IsAntialias = true,
            Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.8f),
            Style = SKPaintStyle.Fill
        };
        private SKPaint _strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public SKPoint Point {
            get => SkiaExtension.SkiaHelper.ToSKPoint(this.PointVector);
            set {
                this.PointVector = SkiaExtension.SkiaHelper.ToVector(value);
            }
        }
        override public SKPoint Location {
            get => this.Point;
            set {
                this.PointVector = SkiaExtension.SkiaHelper.ToVector(value);
            }
        }

        public Entity_v1() : base() {
            this._dragAndDropComponent = new DragAndDropComponent();
            this.selectableComponent = new SelectableComponent();

            this.AddComponents(new IComponent[] {
                this._dragAndDropComponent,
                this.selectableComponent,
            });
        }

        public override string ToString() {
            StringBuilder str = new StringBuilder();

            str.Append($"[Entity {this.Index}] - {this.Point}");
            return str.ToString();
        }

        protected override void DrawThis(SKCanvas canvas) {
            canvas.DrawCircle(this._gLocation, this._radius, this._fillPaint);
            canvas.DrawCircle(this._gLocation, this._radius, this._strokePaint);
        }

        protected override void Invalidate() {
            this._gLocation = this._transform.MapPoint(this.Location);
        }

        public override bool ContainsPoint(SKPoint point) {
            return SKPoint.Distance(point, this.Location) <= this._radius;
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this._gLocation = worldCoordinate.TransformToDevice(this.Location);
            this._gRadius = worldCoordinate.WorldToDeviceTransform.MapRadius(this._radius);
            
            if (this.IsSelected) {
                this._gRadius += 2.0f;
                this._fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Chocolate, 0.8f);
            } else {
                this._fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.8f);
            }
        }

        protected override void DrawThis(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate) {
            canvas.DrawCircle(this._gLocation, this._gRadius, this._fillPaint);
            canvas.DrawCircle(this._gLocation, this._gRadius, this._strokePaint);
        }
    }

    public class DataZone_v1 : ContainerCanvasObject_v1 {
        private List<Entity_v1> _entities = new List<Entity_v1>();
        private List<Triangle_v1> triangles = new List<Triangle_v1>();
        private Triangulation triangulation = new Triangulation();

        public Entity_v1 this[int index] {
            get => this._entities[index];
        }

        public DataZone_v1() : base() { }

        private double[] Flatten(IEnumerable<Entity_v1> targets) {
            var tmpList = new List<double>();

            foreach(var target in targets) {
                tmpList.AddRange(new double[] { target.Point.X, target.Point.Y });
            }

            return tmpList.ToArray();
        }

        public void Add(Entity_v1 entity) {
            entity.SetParent(this);
            this._entities.Add(entity);

            if (this._entities.Count == 3) {
                this.triangles.Clear();
                this.triangles.Add(new Triangle_v1(this[0], this[1], this[2]));
            } else if (this._entities.Count > 3) {
                var flattenPoints = this.Flatten(this._entities);
                var triangleIndicesCollection = this.triangulation.RunDelaunay_v1(2, this._entities.Count, ref flattenPoints);

                this.triangles.Clear();

                foreach(var i in triangleIndicesCollection) {
                    this.triangles.Add(new Triangle_v1(
                        this[i[0]],
                        this[i[1]],
                        this[i[2]]
                    ));

                }
            }

            this.Children.Clear();
            this.Children.AddRange(this._entities);
            this.Children.AddRange(this.triangles);
        }

        public void Add(SKPoint point) {
            var index = this._entities.Count + 1;
            this.Add(new Entity_v1() { Location = point, Index = index });
        }

        public void AddRange(IEnumerable<Entity_v1> entities) {
            foreach(var e in entities) {
                this.Add(e);
            }
        }

        public void Clear() {
            this._entities.Clear();
            this.Children.Clear();
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {

        }

        protected override void DrawThis(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate) { }

        //protected override void ExecuteThis(BehaviorArgs e, string tag = "") { }
    }

    public partial class Line_v1 : CanvasObject_v1 {
        public SKPaint Paint { get; set; } = new SKPaint() {
            Color = SKColors.Gray,
            StrokeWidth = 1,
        };

        protected SKPoint _gP0;
        protected SKPoint _gP1;

        public SKPoint P0 {
            get => SkiaExtension.SkiaHelper.ToSKPoint(this.V0);
            set {
                this.V0 = SkiaExtension.SkiaHelper.ToVector(value);
            }
        }
        public SKPoint P1 {
            get => SkiaExtension.SkiaHelper.ToSKPoint(this.V1);
            set {
                this.V1 = SkiaExtension.SkiaHelper.ToVector(value);
            }
        }

        static public SKPaint VertexPaint = new SKPaint();

        public Line_v1() : base() { }

        protected override void DrawThis(SKCanvas canvas) {
            if (this._isDebug) {
                canvas.DrawCircle(
                    this._gP0,
                    3.0f,
                    new SKPaint() { Color = SKColors.Brown }
                );
                canvas.DrawCircle(
                    this._gP1,
                    3.0f,
                    new SKPaint() { Color = SKColors.DarkOliveGreen }
                );
            }

            canvas.DrawLine(
                this._gP0,
                this._gP1,
                this.Paint
            );
        }

        protected override void DrawThis(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate) {
            if (this._isDebug) {
                VertexPaint.Color = SKColors.Red;

                canvas.DrawCircle(
                    this._gP0,
                    3.0f,
                    VertexPaint
                );

                VertexPaint.Color = SKColors.DarkOliveGreen;

                canvas.DrawCircle(
                    this._gP1,
                    3.0f,
                    VertexPaint
                );
            }

            canvas.DrawLine(
                this._gP0,
                this._gP1,
                this.Paint
            );
        }

        protected override void Invalidate() {
            this._gP0 = this._transform.MapPoint(this.P0);
            this._gP1 = this._transform.MapPoint(this.P1);
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this._gP0 = worldCoordinate.TransformToDevice(this.P0);
            this._gP1 = worldCoordinate.TransformToDevice(this.P1);
        }

        public override bool ContainsPoint(SKPoint point) {
            var vp = SkiaExtension.SkiaHelper.ToVector(point);
            var v = vp - this.V0;
            var result = v[0] * this.Direction[1] - v[1] * this.Direction[0];
            return result == 0.0f? true : false;
        }
    }

    public class Edge_v1 : Line_v1 { }

    public class Ray_v1 : Line_v1 {
        private SKPaint rayPaint = new SKPaint() {
            Color = SKColors.Green,
            StrokeWidth = 2,
        };

        public SKPoint Origin {
            get => this.P0;
            set {
                this.P0 = value;
            }
        }

        private void ClipRay(WorldSpaceCoordinate worldCoordinate) {
            //uint outcodeOut = worldCoordinate.GetPointViewportCode(this.Origin);
            var p0 = worldCoordinate.WorldToClipTransform.MapPoint(this.P0);
            var p1 = worldCoordinate.WorldToClipTransform.MapPoint(this.P1);
            var ray = new SkiaHelper.StLine() {
                V0 = Vector.Build.Dense(new float[] { p0.X, p0.Y }),
                V1 = Vector.Build.Dense(new float[] { p1.X, p1.Y })
            };
            var ymax = 1.0f;
            var ymin = -1.0f;
            var xmax = 1.0f;
            var xmin = -1.0f;

            var top = new SkiaHelper.StLine() {
                V0 = Vector.Build.Dense(new float[] { xmin, ymax }),
                V1 = Vector.Build.Dense(new float[] { xmax, ymax })
            };
            var bottom = new SkiaHelper.StLine() {
                V0 = Vector.Build.Dense(new float[] { xmin, ymin }),
                V1 = Vector.Build.Dense(new float[] { xmax, ymin })
            };
            var left = new SkiaHelper.StLine() {
                V0 = Vector.Build.Dense(new float[] { xmin, ymin }),
                V1 = Vector.Build.Dense(new float[] { xmin, ymax })
            };
            var right = new SkiaHelper.StLine() {
                V0 = Vector.Build.Dense(new float[] { xmax, ymin }),
                V1 = Vector.Build.Dense(new float[] { xmax, ymax })
            };
            var collection = new List<SkiaHelper.StLine>() {
                top, bottom, left, right
            };

            var factorPairs = new List<Matrix<float>>();

            foreach (var edge in collection) {
                factorPairs.Add(SkiaHelper.CheckIsIntersected(ray, edge));
            }

            var intersections = new List<SKPoint>();

            foreach (var pair in factorPairs) {
                var result = pair[0, 0] > 0.0f & pair[1, 0] >= 0.0f & pair[1, 0] <= 1.0f;

                if (result) {
                    var newIntersection = SkiaExtension.SkiaHelper.ToVector(p0) + pair[0, 0] * ray.Direction;
                    var wNewIntersection = worldCoordinate.ClipToWorldTransform.MapPoint(SkiaExtension.SkiaHelper.ToSKPoint(newIntersection));

                    intersections.Add(wNewIntersection);
                }
            }

            if (intersections.Count == 2) {
                this._gP0 = worldCoordinate.TransformToDevice(intersections[0]);
                this._gP1 = worldCoordinate.TransformToDevice(intersections[1]);
                //this.Logger.Debug("Ray - Origin is outside, but intersected.");
            }
            else if (intersections.Count == 1) {
                this._gP0 = worldCoordinate.TransformToDevice(this.P0);
                this._gP1 = worldCoordinate.TransformToDevice(intersections[0]);
                //this.Logger.Debug("Ray - Origin is inside.");
            }
            else if (intersections.Count == 0) {
                this._gP0 = new SKPoint();
                this._gP1 = new SKPoint();
                //this.Logger.Debug("Ray - Origin is outside, but not intersected.");
            }
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this.ClipRay(worldCoordinate);
            //this._gP1 = worldCoordinate.TransformToDevice(this.P1);
        }

        protected override void DrawThis(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate) {
            canvas.DrawLine(
                this._gP0,
                this._gP1,
                this.rayPaint
            );
        }

        public static Ray_v1 CreateRay(SKPoint origin, SKPoint direction) {
            return new Ray_v1() {
                P0 = origin,
                P1 = origin + direction
            };
        }
    }

    public partial class Triangle_v1 : CanvasObject_v1 {
        private SKPaint fillPaint = new SKPaint {
            IsAntialias = true,
            Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DimGray, 0.3f),
            Style = SKPaintStyle.Fill
        };
        private bool isHovered = false;
        private Simplex simplex;

        private SKPoint _gP0;
        private SKPoint _gP1;
        private SKPoint _gP2;
        private HoverComponent hoverComponent;

        private Line_v1 _edge01 = new Line_v1();
        private Line_v1 _edge12 = new Line_v1();
        private Line_v1 _edge20 = new Line_v1();

        private Ray_v1 _edge01Ext = new Ray_v1();

        public Entity_v1 P0 { get; set; }
        public Entity_v1 P1 { get; set; }
        public Entity_v1 P2 { get; set; }

        public Triangle_v1(Entity_v1 p0, Entity_v1 p1, Entity_v1 p2) {
            this.P0 = p0;
            this.P1 = p1;
            this.P2 = p2;

            this.simplex = new Simplex(
                new StateVector[] {
                    new StateVector(this.P0.PointVector),
                    new StateVector(this.P1.PointVector),
                    new StateVector(this.P2.PointVector),
                }
            );

            this.hoverComponent = new HoverComponent();

            this.hoverComponent.PreventDefault(this.Triangle_v1_HoverBehavior);
            this.AddComponent(this.hoverComponent);
        }

        public override string ToString() {
            StringBuilder str = new StringBuilder();

            str.Append($"[Triangle] - {this.P0}, {this.P1}, {this.P2}");

            return str.ToString();
        }
        private void UpdateSimplex() {
            this.simplex[0].State.Vector = this.V0;
            this.simplex[1].State.Vector = this.V1;
            this.simplex[2].State.Vector = this.V2;
        }

        private void Triangle_v1_HoverBehavior(BehaviorArgs args) {
            var castArgs = args as HoverBehaviorArgs;

            if (castArgs.IsInside) {
                this.isHovered = true;
            } else {
                this.isHovered = false;
            }
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this._gP0 = worldCoordinate.TransformToDevice(this.P0.Point);
            this._gP1 = worldCoordinate.TransformToDevice(this.P1.Point);
            this._gP2 = worldCoordinate.TransformToDevice(this.P2.Point);

            this._edge01.P0 = this.P0.Point;
            this._edge01.P1 = this.P1.Point;
            this._edge12.P0 = this.P1.Point;
            this._edge12.P1 = this.P2.Point;
            this._edge20.P0 = this.P2.Point;
            this._edge20.P1 = this.P0.Point;

            this._edge01Ext.P0 = this.P1.Point;
            this._edge01Ext.P1 = this.P1.Point + SkiaExtension.SkiaHelper.ToSKPoint(this._edge01.UnitDirection);

            if (this.isHovered) {
                this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DimGray, 0.8f);
            } else {
                this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DimGray, 0.3f);
            }

            this.Children.Clear();
            this.Children.Add(this._edge01Ext);
            this.Children.AddRange(new Line_v1[] {
                this._edge01, this._edge12, this._edge20
            });

            this.UpdateSimplex();
        }

        protected override void DrawThis(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate) {
            var path = new SKPath();

            path.MoveTo(this._gP0);
            path.LineTo(this._gP1);
            path.LineTo(this._gP2);
            path.Close();

            canvas.DrawPath(path, this.fillPaint);
        }

        public override bool ContainsPoint(SKPoint point) {
            return this.simplex.IsInside(SkiaExtension.SkiaHelper.ToVector(point));
        }
    }
}
