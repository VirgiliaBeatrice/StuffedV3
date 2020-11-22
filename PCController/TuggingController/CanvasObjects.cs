using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using NLog;
using Reparameterization;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Xamarin.Forms.Internals;

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
        IScene Scene { get; set; }
        bool IsSelected { get; set; }

        SKRect BoarderBox { get; }
        Transform Transform { get; set; }
        SKPoint Location { get; set; }
        SKPoint GlobalLocation { get; }
        List<IComponent> Components { get; set; }
        //void Draw(SKCanvas canvas);
        void Draw(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate);
        BehaviorResult Execute(BehaviorArgs e, string tag);
        void AddComponent(IComponent component);
        void AddComponents(IEnumerable<IComponent> components);
        void SetBoarderBoxFromParent(SKRect pBoarderBox);
        //SKSize GetSize();
    }

    public interface ICanvasObjectNode {
        List<ICanvasObject> Children { get; set; }
        bool IsNodeVisible { get; set; }
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

        event EventHandler_v1 KeyDown;
        event EventHandler_v1 KeyPress;
        event EventHandler_v1 KeyUp;
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
        public event EventHandler_v1 KeyDown;
        public event EventHandler_v1 KeyPress;
        public event EventHandler_v1 KeyUp;

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

        public virtual void OnKeyDown(Event @event) {
            this.KeyDown?.Invoke(@event);
        }

        public virtual void OnKeyPress(Event @event) {
            this.KeyPress?.Invoke(@event);
        }

        public virtual void OnKeyUp(Event @event) {
            this.KeyUp?.Invoke(@event);
        }
    }

    public abstract partial class CanvasObject_v1 : ILog, ICanvasObject {

        protected bool _isDebug = true;
        protected Transform _transform = new Transform();
        //protected float _scale = 1.0f;
        protected SKRect _pBoarderBox = new SKRect();

        public EventDispatcher<ICanvasObject> Dispatcher => this.Scene.Dispatcher;

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
        public bool IsNodeVisible { get; set; } = true;

        private IScene scene;
        public IScene Scene {
            get => this.scene;
            set {
                this.scene = value;

                this.Children.ForEach(child => child.Scene = this.scene);
            }
        }

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
        }

        //public virtual SKSize GetSize() {
        //    return this.BoarderBox.Size;
        //}

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
        protected virtual void Invalidate(WorldSpaceCoordinate worldCoordinate) {
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


        internal void SetParent(CanvasObject_v1 parent) {
            this.Transform.Parent = parent._transform;
            this.Scene = parent.Scene;
        }

        //public virtual void Draw(SKCanvas canvas) {
        //    // Redraw
        //    // Invalidate() first, then DrawThis() and Draw() of all children.
        //    this.Invalidate();
        //    this.DrawThis(canvas);

        //    foreach(var child in this.Children) {
        //        child.Draw(canvas);
        //    }
        //}

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
            this.DrawThis(canvas);

            foreach (var child in this.Children) {
                child.Draw(canvas, worldCoordinate);
            }
        }

        public TreeNode[] GetChildrenTreeNodes() {
            var childrenNodes = new List<TreeNode>();

            foreach(var child in this.Children) {
                if (child.IsNodeVisible) {
                    childrenNodes.Add(new TreeNode(child.ToString(),    child.GetChildrenTreeNodes()) { Tag = child });
                }
            }
            return childrenNodes.ToArray();
        }
    }

    public abstract class ContainerCanvasObject_v1 : CanvasObject_v1 {
        public new bool IsNodeVisible { get; set; } = false;
        //public override void Draw(SKCanvas canvas) {
        //    // Redraw
        //    // Invalidate() first, then DrawThis() and Draw() of all children.

        //    foreach (var child in this.Children) {
        //        child.Draw(canvas);
        //    }
        //}

        public override bool ContainsPoint(SKPoint point) {
            return true;
        }
    }

    public partial class Entity_v1 : CanvasObject_v1 {
        private DragAndDropComponent dragAndDropComponent;
        private SelectableComponent selectableComponent;
        private SKPoint _gLocation;
        private float _radius = 5.0f;
        private float _gRadius;

        public int Index { get; set; }

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
            this.dragAndDropComponent = new DragAndDropComponent();
            this.selectableComponent = new SelectableComponent();

            this.AddComponents(new IComponent[] {
                this.dragAndDropComponent,
                this.selectableComponent,
            });
        }

        public override string ToString() {
            StringBuilder str = new StringBuilder();

            str.Append($"[Entity {this.Index}] - {this.Point}");
            return str.ToString();
        }

        public override bool ContainsPoint(SKPoint point) {
            return SKPoint.Distance(point, this.Location) <= this._radius;
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this._gLocation = worldCoordinate.TransformToDevice(this.Location);
            this._gRadius = worldCoordinate.WorldToDeviceTransform.MapRadius(this._radius);
            
            if (this.IsSelected) {
                this._gRadius += 2.0f;
                this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Chocolate, 0.8f);
            } else {
                this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.8f);
            }
        }

        protected override void DrawThis(SKCanvas canvas) {
            canvas.DrawCircle(this._gLocation, this._gRadius, this.fillPaint);
            canvas.DrawCircle(this._gLocation, this._gRadius, this.strokePaint);
        }
    }

    public class VertexIndexPair : HashSet<int> {

        public override bool Equals(object obj) {
            return obj is VertexIndexPair pair &&
                   this.Contains(pair.ElementAt(0)) &&
                   this.Contains(pair.ElementAt(1));
        }

        public override int GetHashCode() {
            int hashCode = 1224354199;
            hashCode = hashCode * -1521134295 + this.ElementAt(0).GetHashCode() + this.ElementAt(1).GetHashCode();

            return hashCode;
        }
    }

    public class CircularList<T> : IEnumerable<CircularListNode<T>> {
        private List<CircularListNode<T>> collection = new List<CircularListNode<T>>();

        public CircularListNode<T> First => this.collection.First();
        public CircularListNode<T> Last => this.collection.Last();

        public CircularList() { }

        public CircularList(IEnumerable<T> ts) {
            this.AddRange(ts);
        }

        public CircularListNode<T> this[int index] {
            get { return this.collection[index]; }
        }

        public override string ToString() {
            return collection.ToString();
        }

        public int IndexOf(T item) {
            return this.collection.Select(e => e.Value).ToArray().IndexOf(item);
        }

        public bool Contains(T item) {
            return this.collection.Select(e => e.Value).Contains(item);
        }

        public CircularList<T> Clone() {
            var clone = new CircularList<T>();

            foreach(var node in this.collection) {
                clone.Add(node.Value);
            }

            return clone;
        }

        public void Insert(int at, T item) {
            if (at < 0 | at > this.collection.Count - 1) {
                throw new Exception();
            }

            CircularListNode<T> newItem = new CircularListNode<T>() {
                Value = item,
            };

            var nextItem = this.collection[at];
            var prevItem = this.collection[at].Prev;

            newItem.Next = nextItem;
            newItem.Prev = prevItem;

            this.collection.Insert(at, newItem);

            nextItem.Prev = newItem;
            prevItem.Next = newItem;
        }

        public void RemoveAt(int at) {
            if (at < 0 | at > this.collection.Count - 1) {
                throw new Exception();
            }

            var targetItem = this.collection[at];
            var prevItem = targetItem.Prev;
            var nextItem = targetItem.Next;
            
            this.collection.RemoveAt(at);

            prevItem.Next = nextItem;
            nextItem.Prev = prevItem;
        }

        public void AddRange(IEnumerable<T> ts) {
            foreach (var value in ts) {
                this.Add(value);
            }
        }

        public void Add(T item) {
            CircularListNode<T> newItem;
            
            if (this.collection.Count == 0) {
                newItem = new CircularListNode<T>() {
                    Value = item,
                };

                newItem.Next = newItem;
                newItem.Prev = newItem;
            }
            else {
                newItem = new CircularListNode<T>() {
                    Value = item,
                    Prev = this.collection.Last(),
                    Next = this.collection.First()
                };
                this.collection.Last().Next = newItem;
                this.collection.First().Prev = newItem;
            }

            this.collection.Add(newItem);
        }

        public void Clear() {
            this.collection.Clear();
        }

        public IEnumerator<CircularListNode<T>> GetEnumerator() {
            return this.collection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }

    public class CircularListNode<T> {
        public T Value { get; set; }

        public CircularListNode<T> Prev { get; set; }
        public CircularListNode<T> Next { get; set; }

        public override string ToString() {
            return this.Value.ToString();
        }
    }

    public struct ExteriorRay {
        public Ray_v1 Ray { get; set; }
        public Triangle_v1 Govorner { get; set; }
        public Triangle_v1 ExcludedTri { get; set; }
    }

    public class DataZone_v1 : ContainerCanvasObject_v1 {
        private List<Entity_v1> _entities = new List<Entity_v1>();
        private List<Triangle_v1> triangles = new List<Triangle_v1>();
        private List<Entity_v1> extremes = new List<Entity_v1>();
        private CircularList<Entity_v1> newExtremes = new CircularList<Entity_v1>();
        private HashSet<Edge_v1> triangleEdges = new HashSet<Edge_v1>();
        private HashSet<Triangle_v1> outerTriangles = new HashSet<Triangle_v1>();
        private List<Edge_v1> convexhullEdges = new List<Edge_v1>();
        private Triangulation triangulation = new Triangulation();
        private List<VoronoiRegion_v1> voronoiRegions = new List<VoronoiRegion_v1>();

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

        private Edge_v1 FindTriangleEdge(Entity_v1 e0, Entity_v1 e1) {
            return this.triangleEdges.Where(edge => edge.HasVertex(e0) && edge.HasVertex(e1)).FirstOrDefault();
        }

        #region Unused
        private void SetTriangleExtensionRay() {
            this.outerTriangles.Clear();
            //var outerTriangles = new HashSet<Triangle_v1>();

            foreach (var extreme in this.newExtremes) {
                this.Logger.Debug($"{extreme}");
                var it = extreme.Value;
                var prev = extreme.Prev.Value;
                var next = extreme.Next.Value;
                var targets = this.triangles.Select(
                    tri => {
                        var isPrev = tri.IsVertex(it) && tri.IsVertex(prev);
                        var isNext = tri.IsVertex(it) && tri.IsVertex(next);

                        if (isPrev & isNext) {
                            return (tri, new Entity_v1[] {
                                it, prev, next
                            });
                        }
                        else if (isPrev & !isNext) {
                            return (tri, new Entity_v1[] {
                                it, prev
                            });
                        }
                        else if (!isPrev & isNext) {
                            return (tri, new Entity_v1[] {
                                it, next
                            });
                        }
                        else {
                            return (null, null);
                        }
                    }
                ).Where(item => item.Item1 != null);

                foreach (var target in targets) {
                    var targetTri = target.Item1;
                    var targetVertices = target.Item2;

                    if (targetVertices.Length == 3) {
                        var notRedundant = this.outerTriangles.Add(targetTri);

                        //if (notRedundant) {
                        targetTri.Exterior = new ExteriorZone();
                        //}

                        // Situation 1 has the highest priority!
                        //this.Logger.Debug($"[{targetTri}, {targetVertices[0]}, {targetVertices[1]}, {targetVertices[2]}]Triangle Extension Ray: Situation 1.");

                        var ray0 = new Ray_v1();
                        var ray1 = new Ray_v1();

                        ray0.P0 = next.Point;
                        ray0.P1 = next.Point + next.Point - prev.Point;
                        ray0.Color = SKColors.DarkGoldenrod;
                        ray0.RayWidth = 4.0f;
                        ray1.P0 = prev.Point;
                        ray1.P1 = prev.Point + prev.Point - next.Point;
                        ray1.Color = SKColors.DarkGoldenrod;
                        ray1.RayWidth = 4.0f;

                        targetTri.Exterior.Rays.Add(ray0);
                        targetTri.Exterior.Rays.Add(ray1);
                        targetTri.Exterior.Extreme = it;
                    }
                    else {
                        var targetVertexIt = targetVertices[0];
                        var targetVertexNeighbor = targetVertices[1];
                        var notRedundant = this.outerTriangles.Add(targetTri);

                        if (notRedundant) {
                            targetTri.Exterior = new ExteriorZone();
                        }

                        //if (notRedundant) {
                        if (notRedundant) {
                            //this.Logger.Debug($"[{targetTri}, {targetVertices[0]}, {targetVertices[1]}]Triangle Extension Ray: Situation 2.");

                            var ray0 = new Ray_v1();
                            var ray1 = new Ray_v1();
                            var oppsiteVertex = targetTri.GetRestVertices(targetVertices).First();

                            targetTri.Exterior.Edge = this.convexhullEdges.Where(e => e.HasVertex(targetVertexIt) & e.HasVertex(targetVertexNeighbor)).ElementAt(0);

                            if (!this.convexhullEdges.Any(e => e.HasVertex(targetVertexIt) & e.HasVertex(oppsiteVertex))) {
                                ray0.P0 = targetVertexIt.Point;
                                ray0.P1 = targetVertexIt.Point + targetVertexIt.Point - oppsiteVertex.Point;
                                ray0.Color = SKColors.DarkCyan;

                                targetTri.Exterior.Rays.Add(ray0);
                            }

                            if (!this.convexhullEdges.Any(e => e.HasVertex(targetVertexNeighbor) & e.HasVertex(oppsiteVertex))) {
                                ray1.P0 = targetVertexNeighbor.Point;
                                ray1.P1 = targetVertexNeighbor.Point + targetVertexNeighbor.Point - oppsiteVertex.Point;
                                ray1.Color = SKColors.DarkCyan;

                                targetTri.Exterior.Rays.Add(ray1);
                            }
                        }
                    }
                }
                //}
            }
        }
        #endregion

        private void SetExteriorRays() {
            var exteriorRays = new List<ExteriorRay>();
            this.voronoiRegions.Clear();
            
            // For Order: CCW
            foreach(var extreme in this.newExtremes) {
                var edges = this.triangleEdges.Where(edge => edge.HasVertex(extreme.Value)).ToArray();
                var edgeCnt = edges.Count();

                if (edgeCnt == 2) {
                    this.Logger.Debug($"No splitter for {extreme.Value}.");

                    var prev = extreme.Prev.Value;
                    var it = extreme.Value;
                    var next = extreme.Next.Value;

                    exteriorRays.Add(new ExteriorRay() {
                        ExcludedTri = this.triangles.Find(tri => tri.IsVertex(prev) & tri.IsVertex(it) & tri.IsVertex(next))
                    });
                }
                else if (edgeCnt == 3) {
                    // Note: this case has a special condition which needs to be handled.
                    // Angle between ray and each neighbor edge that is less than 90 needs to be restricted.
                    this.Logger.Debug($"One splitter(Extension of edge) for {extreme.Value}.");

                    var targetEdge = edges.Where(edge => !this.convexhullEdges.Contains(edge)).ElementAt(0);

                    // Extend this edge
                    var start = extreme.Value;
                    var end = targetEdge.E0 == start ? targetEdge.E1 : targetEdge.E0;
                    var edgeDirection = start.Point - end.Point;
                    var rayOfEdgeExtension = Ray_v1.CreateRay(start.Point, edgeDirection);

                    rayOfEdgeExtension.Color = SKColors.Green;
                    rayOfEdgeExtension.E0 = start;

                    // Compare with Normals
                    var prev = extreme.Prev.Value;
                    var it = extreme.Value;
                    var next = extreme.Next.Value;

                    var dirOfEdgePrevToIt = it.PointVector - prev.PointVector;
                    var dirOfEdgeItToNext = next.PointVector - it.PointVector;
                    var normalOfEdgePI = new SKPoint(dirOfEdgePrevToIt[1], -dirOfEdgePrevToIt[0]);
                    var normalOfEdgeIN = new SKPoint(dirOfEdgeItToNext[1], -dirOfEdgeItToNext[0]);
                    var rayOfNormalPI = Ray_v1.CreateRay(it.Point, normalOfEdgePI);
                    var rayOfNoramlIN = Ray_v1.CreateRay(it.Point, normalOfEdgeIN);

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
                            Ray = rayOfEdgeExtension
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
            for (var idx = 0; idx < exteriorRays.Count; idx ++) {
                Triangle_v1 excludedTri = null;
                var idx0 = idx;
                var idx1 = idx + 1 < exteriorRays.Count ? idx + 1 : 0;

                var exRay0 = exteriorRays[idx0];
                var exRay1 = exteriorRays[idx1];

                if (exRay0.ExcludedTri == null) {
                    if (exRay1.ExcludedTri != null) {
                        excludedTri = exRay1.ExcludedTri;

                        idx1 = idx1 + 1 < exteriorRays.Count ? idx1 + 1 : 0;
                        exRay1 = exteriorRays[idx1];

                        idx++;
                    }

                    this.voronoiRegions.Add(
                        new VoronoiRegion_v1() {
                            Index = this.voronoiRegions.Count,
                            ExteriorRay0 = exRay0,
                            ExteriorRay1 = exRay1,
                            ExcludedTri = excludedTri 
                        }
                    );
                }
            }
        }

        public void Add(Entity_v1 entity) {
            entity.SetParent(this);
            this._entities.Add(entity);

            if (this._entities.Count == 3) {
                var triangle = new Triangle_v1(this[0], this[1], this[2]);

                triangle.Edges.Add(new Edge_v1() {
                    E0 = this[0],
                    E1 = this[1],
                });
                triangle.Edges.Add(new Edge_v1() {
                    E0 = this[1],
                    E1 = this[2],
                });
                triangle.Edges.Add(new Edge_v1() {
                    E0 = this[2],
                    E1 = this[0],
                });
                this.triangles.Clear();
                this.triangles.Add(triangle);
            }
            else if (this._entities.Count > 3) {
                var flattenPoints = this.Flatten(this._entities);
                var triangleIndicesCollection = this.triangulation.RunDelaunay_v1(2, this._entities.Count, ref flattenPoints);
                var convexhullIndicesCollection = new LinkedList<int>(this.triangulation.RunConvexHull_v1(2, this._entities.Count, ref flattenPoints));

                this.triangles.Clear();
                this.extremes.Clear();
                this.newExtremes.Clear();
                this.triangleEdges.Clear();
                this.convexhullEdges.Clear();

                var pairSet = new HashSet<VertexIndexPair>();

                // Create all edges of triangles.
                foreach (var i in triangleIndicesCollection) {
                    pairSet.Add(new VertexIndexPair { i[0], i[1] });
                    pairSet.Add(new VertexIndexPair { i[1], i[2] });
                    pairSet.Add(new VertexIndexPair { i[2], i[0] });
                }

                foreach(var pair in pairSet) {
                    this.triangleEdges.Add(new Edge_v1 {
                        E0 = this[pair.ElementAt(0)],
                        E1 = this[pair.ElementAt(1)]
                    });
                }

                // Create triangles.
                foreach(var i in triangleIndicesCollection) {
                    var triangle = new Triangle_v1(
                        this[i[0]],
                        this[i[1]],
                        this[i[2]]
                    );

                    triangle.Edges.Add(this.FindTriangleEdge(this[i[0]], this[i[1]]));
                    triangle.Edges.Add(this.FindTriangleEdge(this[i[1]], this[i[2]]));
                    triangle.Edges.Add(this.FindTriangleEdge(this[i[2]], this[i[0]]));

                    this.triangles.Add(triangle);
                }

                // Get all extremes.
                foreach(var i in convexhullIndicesCollection) {
                    this.extremes.Add(this[i]);
                    this.newExtremes.Add(this[i]);
                }

                // Get all edges of convex hull.
                for (var it = convexhullIndicesCollection.First; it != null; it = it.Next) {
                    Entity_v1 e1;
                    var e0 = this[it.Value];

                    if (it == convexhullIndicesCollection.Last) {
                        e1 = this[convexhullIndicesCollection.First.Value];
                    } else {
                        e1 = this[it.Next.Value];
                    }

                    var edge = this.triangleEdges.Where(e => e.HasVertex(e0) & e.HasVertex(e1));

                    this.convexhullEdges.AddRange(edge);
                }

                //this.SetTriangleExtensionRay();
                this.SetExteriorRays();
            }

            this.Children.Clear();
            this._entities.ForEach(e => e.SetParent(this));
            this.Children.AddRange(this._entities);
            this.triangles.ForEach(tri => tri.SetParent(this));
            this.Children.AddRange(this.triangles);
            this.voronoiRegions.ForEach(v => v.SetParent(this));
            this.Children.AddRange(this.voronoiRegions);

            this.Dispatcher.OnCanvasObjectChanged(null);
        }

        public void Add(SKPoint point) {
            var index = this._entities.Count;
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

        protected override void DrawThis(SKCanvas canvas) { }

        //protected override void ExecuteThis(BehaviorArgs e, string tag = "") { }
    }

    public partial class Line_v1 : CanvasObject_v1 {
        public SKPaint Paint { get; set; } = new SKPaint() {
            Color = SKColors.Gray,
            StrokeWidth = 1,
            IsAntialias = true,
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

    public class Edge_v1 : Line_v1 {
        private Entity_v1 e0;
        private Entity_v1 e1;

        public Entity_v1 E0 {
            get => this.e0;
            set {
                this.P0 = value.Point;
                this.e0 = value;
            }
        }
        public Entity_v1 E1 {
            get => this.e1;
            set {
                this.P1 = value.Point;
                this.e1 = value;
            }
        }

        private Entity_v1[] Vertices => new Entity_v1[] {
            this.E0, this.E1
        };

        public Edge_v1() : base() { }

        public Edge_v1(Entity_v1 e0, Entity_v1 e1) {
            this.E0 = e0;
            this.E1 = e1;
        }

        public override string ToString() {
            var str = new StringBuilder();

            str.Append($"Edge - {this.E0}, {this.E1}");

            return str.ToString();
        }

        public bool HasVertex(Entity_v1 target) {
            return this.Vertices.Contains(target);
        }

        public Entity_v1[] GetVertices() {
            return this.Vertices;
        }
    }

    public class Ray_v1 : Line_v1 {
        private SKPaint rayPaint = new SKPaint() {
            Color = SKColors.Green,
            StrokeWidth = 2,
            IsAntialias = true,
        };

        public SKColor Color {
            set {
                this.rayPaint.Color = value;
            }
        }

        public float RayWidth {
            get => this.rayPaint.StrokeWidth;
            set {
                this.rayPaint.StrokeWidth = value;
            }
        }

        public CanvasObject_v1 Lock { get; set; } = null;

        public Entity_v1 E0 { get; set; }

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
        }

        protected override void DrawThis(SKCanvas canvas) {
            canvas.DrawLine(
                this._gP0,
                this._gP1,
                this.rayPaint
            );
        }

        public bool GetLock(CanvasObject_v1 target) {
            if (this.Lock == null) {
                this.Lock = target;

                return true;
            }
            else if (this.Lock != target) {
                return false;
            }
            else {
                return true;
            }
        }

        public void ReleaseLock() {
            this.Lock = null;
        }

        public bool SetRayWidth(CanvasObject_v1 target, float width) {
            if(this.Lock == target) {
                this.RayWidth = width;

                return true;
            }

            return false;
        }

        public static Ray_v1 CreateRay(SKPoint origin, SKPoint direction) {
            return new Ray_v1() {
                P0 = origin,
                P1 = origin + direction
            };
        }
    }

    public interface IExteriorZone {
        List<Ray_v1> Rays { get; set; }
        Edge_v1 Edge { get; set; }
        Entity_v1 Extreme { get; set; }
    }

    public class ExteriorZone : IExteriorZone {
        public List<Ray_v1> Rays { get; set; } = new List<Ray_v1>();
        public Edge_v1 Edge { get; set; }
        public Entity_v1 Extreme { get; set; }
        public bool HasExterior => this.Edge != null | this.Extreme != null;
    }

    public class VoronoiRegion_v1 : CanvasObject_v1 {
        private SKPaint fillPaint = new SKPaint {
            IsAntialias = true,
            Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DarkOliveGreen, 0.3f),
            Style = SKPaintStyle.Fill
        };
        private CircularList<SKPoint> path = new CircularList<SKPoint>();
        private CircularList<SKPoint> gPath = new CircularList<SKPoint>();

        private Ray_v1 edge0;
        private Ray_v1 edge1;
        private ExteriorRay exteriorRay0;
        private ExteriorRay exteriorRay1;
        private bool isHovered;
        private HoverComponent hoverComponent;

        public ExteriorRay ExteriorRay0 {
            get => this.exteriorRay0;
            set {
                this.exteriorRay0 = value;
                this.edge0 = this.exteriorRay0.Ray;

                this.Children.Clear();
                this.edge0.SetParent(this);
                this.Children.Add(this.edge0);
            }
        }
        public ExteriorRay ExteriorRay1 {
            get => this.exteriorRay1;
            set {
                this.exteriorRay1 = value;
                this.edge1 = this.exteriorRay1.Ray;

                this.Children.Clear();
                this.edge1.SetParent(this);
                this.Children.Add(this.edge1);
            }
        }

        public Triangle_v1 ExcludedTri { get; set; }

        public int Index { get; set; } = 0;

        public VoronoiRegion_v1() {
            this.hoverComponent = new HoverComponent();

            this.hoverComponent.PreventDefault(this.VoronoiRegion_v1_HoverBehavior);
            this.AddComponent(this.hoverComponent);
        }

        public override string ToString() {
            StringBuilder str = new StringBuilder();

            str.Append($"[Voronoi Region] - {this.Index}");

            return str.ToString();
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this.gPath.Clear();

            foreach (var node in this.path) {
                this.gPath.Add(worldCoordinate.TransformToDevice(node.Value));
            }

            if (this.isHovered) {
                if (this.edge0.GetLock(this)) {
                    this.edge0.SetRayWidth(this, 4.0f);
                }

                if (this.edge1.GetLock(this)) {
                    this.edge1.SetRayWidth(this, 4.0f); 
                }

                this.Clip(worldCoordinate);
            }
            else {
                if (this.edge0.GetLock(this)) {
                    this.edge0.SetRayWidth(this, 2.0f);
                    this.edge0.ReleaseLock();
                }

                if (this.edge1.GetLock(this)) {
                    this.edge1.SetRayWidth(this, 2.0f);
                    this.edge1.ReleaseLock();
                }
            }

        }
        protected override void DrawThis(SKCanvas canvas) {
            if (!this.isHovered) {
                return;
            }

            var path = new SKPath();
            var nodes = new List<SKPoint>();

            foreach(var node in this.gPath) {
                if (this.gPath.First == node) {
                    path.MoveTo(node.Value);
                }
                else {
                    path.LineTo(node.Value);
                }

                nodes.Add(node.Value);
            }

            var textPaint = new SKPaint() {
                TextSize = 64.0f,
                IsAntialias = true,
                Color = new SKColor(0x42, 0x81, 0xA4),
                IsStroke = false,
                TextAlign = SKTextAlign.Center,
            };

            var pointPaint = new SKPaint() {
                IsAntialias = true,
                Color = SKColors.Blue,
                IsStroke = true,
                StrokeWidth = 2.0f,
            };

            path.Close();
            canvas.DrawPath(path, this.fillPaint);
            //canvas.DrawText(this.Index.ToString(), path.Bounds.MidX, path.Bounds.MidY, textPaint);

            //var idx = 0;
            //nodes.ForEach(node => {
            //    canvas.DrawCircle(node, 5.0f + idx * 4, pointPaint);
            //    idx++;
            //});
        }

        private CircularList<SKPoint> IteratePath(CircularList<SKPoint> path, SkiaHelper.Line2D targetLine) {
            var newPath = new CircularList<SKPoint>();

            foreach (var node in path) {
                var s = node.Value;
                var e = node.Next.Value;

                var sideOfS = SkiaHelper.GetSide(targetLine, s);
                var sideOfE = SkiaHelper.GetSide(targetLine, e);

                var l0 = new SkiaHelper.Line2D {
                    P0 = s,
                    P1 = e
                };
                var result = SkiaHelper.CheckIsIntersected(l0, targetLine);
                var i = new SKPoint {
                    X = result[0, 0] * l0.Direction[0] + s.X,
                    Y = result[0, 0] * l0.Direction[1] + s.Y
                };

                if (sideOfS >= 0 & sideOfE >= 0) {
                    // Keep E
                    newPath.Add(e);
                }
                else if (sideOfS > 0 & sideOfE < 0) {
                    // Keep I
                    newPath.Add(i);
                }
                else if (sideOfS < 0 & sideOfE >= 0) {
                    // Keep I and E
                    newPath.Add(i);
                    newPath.Add(e);
                }
            }

            return newPath;
        }

        private void Clip(WorldSpaceCoordinate worldCoordinate) {
            var lt = new SKPoint() {
                X = worldCoordinate.Window.Left,
                Y = worldCoordinate.Window.Top
            };
            var rt = new SKPoint() {
                X = worldCoordinate.Window.Right,
                Y = worldCoordinate.Window.Top
            };
            var rb = new SKPoint() {
                X = worldCoordinate.Window.Right,
                Y = worldCoordinate.Window.Bottom
            };
            var lb = new SKPoint() {
                X = worldCoordinate.Window.Left,
                Y = worldCoordinate.Window.Bottom
            };
            var left = new SkiaHelper.Line2D { P0 = lt, P1 = lb };
            var right = new SkiaHelper.Line2D { P0 = rb, P1 = rt };
            var bottom = new SkiaHelper.Line2D { P0 = lb, P1 = rb };
            var top = new SkiaHelper.Line2D { P0 = rt, P1 = lt };
            var cornerSites = new CircularList<SKPoint> {
                lt, rt, rb, lb
            };
            var boxEdges = new CircularList<SkiaHelper.Line2D> {
                left, right, bottom, top
            };

            var intersectionsOfEdge0 = new SortedDictionary<float, SKPoint>();
            var intersectionsOfEdge1 = new SortedDictionary<float, SKPoint>();

            foreach (var node in boxEdges) {
                var result = SkiaHelper.CheckIsIntersected(edge0, node.Value);

                if (result[0, 0] >= 0.0f & result[1, 0] <= 1.0f & result[1, 0] >= 0.0f) {
                    intersectionsOfEdge0[result[0, 0]] = new SKPoint {
                        X = result[0, 0] * edge0.Direction[0] + edge0.P0.X,
                        Y = result[0, 0] * edge0.Direction[1] + edge0.P0.Y
                    };
                }
            }

            foreach (var node in boxEdges) {
                var result = SkiaHelper.CheckIsIntersected(edge1, node.Value);

                if (result[0, 0] >= 0.0f & result[1, 0] <= 1.0f & result[1, 0] >= 0.0f) {
                    intersectionsOfEdge1[result[0, 0]] = new SKPoint {
                        X = result[0, 0] * edge1.Direction[0] + edge1.P0.X,
                        Y = result[0, 0] * edge1.Direction[1] + edge1.P0.Y
                    };
                }
            }

            // Path
            var path = new CircularList<SKPoint>();

            // Intersections that edge1 intersects with window box. (Reverse)
            path.AddRange(intersectionsOfEdge1.Values.ToArray().Reverse());

            // P or P0, P1
            if (edge0.P0 == edge1.P0) {
                path.Add(edge0.P0);
            }
            else {
                path.Add(edge1.P0);
                path.Add(edge0.P0);
            }

            // Intersections that edge0 intersects with window box.
            path.AddRange(intersectionsOfEdge0.Values.ToArray());

            // Inner sites
            var innerSites = cornerSites.Where(
                site => {
                    var sideOfE0 = SkiaHelper.GetSide(edge0, site.Value);
                    var sideOfE1 = SkiaHelper.GetSide(edge1, site.Value);

                    return sideOfE0 >= 0 & sideOfE1 <= 0;
                }).ToList().Select(e => e.Value).ToList();

            innerSites.ForEach(site => path.Add(site));

            var centroid = new SKPoint {
                X = path.Select(node => node.Value.X).Sum() / path.Count(),
                Y = path.Select(node => node.Value.Y).Sum() / path.Count()
            };

            path = new CircularList<SKPoint>(path.OrderBy(
                node => SkiaHelper.GetIncludedAngle(
                    centroid + new SKPoint(1.0f, 0.0f),
                    node.Value,
                    centroid)
                ).Select(node => node.Value));

            // Exclude triangle's vertex
            if (this.ExcludedTri != null) {
                var vertices = this.ExcludedTri.GetVertices();
                var targetVertex = vertices.Where(v => v.Point != edge0.P0 & v.Point != edge1.P0).ElementAt(0);
                var targetIdx = path.IndexOf(edge0.P0);

                path.Insert(targetIdx, targetVertex.Point);
            }

            // lt -> lb
            path = this.IteratePath(path, left);
            // rt -> rb
            path = this.IteratePath(path, right);
            // lb -> rb
            path = this.IteratePath(path, bottom);
            // lt -> rt
            path = this.IteratePath(path, top);

            this.path = path;
        }

        public override bool ContainsPoint(SKPoint point) {
            if (edge0.P0 == edge1.P0) {
                //this.Logger.Debug("Situation 1: Voronoi Region has same start point");

                var p = point - edge0.P0;
                var angleOfPToE0 = SkiaHelper.GetIncludedAngle(p, edge0.P1 - edge0.P0);
                var angleOfPToE1 = SkiaHelper.GetIncludedAngle(p, edge1.P1 - edge0.P0);

                return angleOfPToE0 < 0.0f & angleOfPToE1 > 0.0f;
            }
            else {
                //this.Logger.Debug("Situation 2: Voronoi Region has different start points.");

                var p0 = point - edge0.P0;
                var angleOfPToE0 = SkiaHelper.GetIncludedAngle(p0, edge0.P1 - edge0.P0);
                var angleOfPToE = SkiaHelper.GetIncludedAngle(p0, edge1.P0 - edge0.P0);

                var p1 = point - edge1.P0;
                var angleOfPToE1 = SkiaHelper.GetIncludedAngle(p1, edge1.P1 - edge1.P0);

                var result = (angleOfPToE0 < 0.0f) & (angleOfPToE > 0.0f) & (angleOfPToE1 > 0.0f);

                // More elegant!
                if (result) {
                    if (this.ExcludedTri != null) {
                        result = !this.ExcludedTri.ContainsPoint(point);
                    }
                }

                return result;
            }
        }
        private void VoronoiRegion_v1_HoverBehavior(BehaviorArgs args) {
            var castArgs = args as HoverBehaviorArgs;

            if (castArgs.IsInside) {
                this.isHovered = true;
            }
            else {
                this.isHovered = false;
            }
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

        private Edge_v1 _edge01;
        private Edge_v1 _edge12;
        private Edge_v1 _edge20;

        public List<Edge_v1> Edges = new List<Edge_v1>();

        public int ExtensionRayMode { get; set; } = 0;
        public List<Ray_v1> ExtensionRays { get; set; } = new List<Ray_v1>();
        public (Edge_v1, Entity_v1) ExtensionRayContext { get; set; }
        public ExteriorZone Exterior { get; set; } = new ExteriorZone();

        private bool isInsideExterior = false;
        private bool isInsideInterior = false;

        public Entity_v1 P0 { get; set; }
        public Entity_v1 P1 { get; set; }
        public Entity_v1 P2 { get; set; }
        private Entity_v1[] Vertices => new Entity_v1[] {
            this.P0, this.P1, this.P2
        };

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

            str.Append($"[Triangle - {this.P0.Index}, {this.P1.Index}, {this.P2.Index}]");

            return str.ToString();
        }

        public bool IsVertex(Entity_v1 target) {
            return this.Vertices.Contains(target);
        }

        public bool HasEdge(Edge_v1 target) {
            return this.Edges.Contains(target);
        }

        public Edge_v1[] GetEdges() {
            return new Edge_v1[] {
                this._edge01, this._edge12, this._edge20
            };
        }

        public Entity_v1[] GetVertices() {
            return new Entity_v1[] {
                this.P0, this.P1, this.P2
            };
        }

        public Entity_v1[] GetRestVertices(Entity_v1[] testSet) {
            var vertexSet = new List<Entity_v1>() {
                this.P0, this.P1, this.P2
            };

            return vertexSet.Except(testSet).ToArray();
        }

        public Entity_v1[] GetRestVertices(Edge_v1 targetEdge) {
            return this.Vertices.Except(targetEdge.GetVertices()).ToArray();
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

            this._edge01 = this.Edges[0];
            this._edge12 = this.Edges[1];
            this._edge20 = this.Edges[2];

            var edgeCollection = new Line_v1[] {
                this._edge01, this._edge12, this._edge20
            };

            if (this.isHovered) {
                this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DimGray, 0.8f);
            } else {
                this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DimGray, 0.3f);
            }

            this.Children.Clear();
            this.Exterior.Rays.ForEach(r => r.SetParent(this));
            this.Children.AddRange(this.Exterior.Rays);
            edgeCollection.ForEach(e => e.SetParent(this));
            this.Children.AddRange(edgeCollection);

            this.UpdateSimplex();
        }

        protected override void DrawThis(SKCanvas canvas) {
            var path = new SKPath();

            path.MoveTo(this._gP0);
            path.LineTo(this._gP1);
            path.LineTo(this._gP2);
            path.Close();

            canvas.DrawPath(path, this.fillPaint);

            //var exteriorPath = new SKPath();
            //exteriorPath
        }

        public override bool ContainsPoint(SKPoint point) {
            this.isInsideInterior = this.simplex.IsInside(SkiaExtension.SkiaHelper.ToVector(point));

            if (this.Exterior.HasExterior) {
                this.isInsideExterior = this.ContainsPointInExteriorZone(point);
            }

            //this.Logger.Debug($"{this} interior {this.isInsideInterior} exterior {this.isInsideExterior & !this.isInsideInterior}");
            return this.isInsideExterior | this.isInsideInterior;
            //return this.simplex.IsInside(SkiaExtension.SkiaHelper.ToVector(point));
        }

        public bool ContainsPointInExteriorZone(SKPoint point) {
            if (this.Exterior.Edge != null) {
                var ray0 = this.Exterior.Rays[0];
                var ray1 = this.Exterior.Rays[1];
                var testPoint = SkiaExtension.SkiaHelper.ToVector(point);
                var testLine = new SkiaHelper.StLine() {
                    V0 = testPoint,
                    V1 = testPoint + ray0.V0 - ray1.V0,
                };

                var intersectionRay0 = SkiaHelper.CheckIsIntersected(testLine, ray0);
                var intersectionRay1 = SkiaHelper.CheckIsIntersected(testLine, ray1);

                //this.Logger.Debug($"{intersectionRay0[0, 0]} {intersectionRay1[0, 0]}");

                return Math.Sign(intersectionRay0[0, 0] * intersectionRay1[0, 0]) == -1 & Math.Sign(intersectionRay0[1, 0]) == 1 & Math.Sign(intersectionRay1[1, 0]) == 1;
            }


            // https://stackoverflow.com/questions/1560492/how-to-tell-whether-a-point-is-to-the-right-or-left-side-of-a-line/1560510#1560510
            if (this.Exterior.Extreme != null) {
                var ray0 = this.Exterior.Rays[0];
                var ray1 = this.Exterior.Rays[1];
                var t = SkiaExtension.SkiaHelper.ToVector(point);
                var e = this.Exterior.Extreme.PointVector;
                var v0 = ray0.V0;
                var v1 = ray1.V0;

                var signOfT = Math.Sign(
                    (v0[0] - v1[0]) * (t[1] - v1[1]) -
                    (v0[1] - v1[1]) * (t[0] - v1[0])
                );
                var signOfE = Math.Sign(
                    (v0[0] - v1[0]) * (e[1] - v1[1]) -
                    (v0[1] - v1[1]) * (e[0] - v1[0])
                );

                //this.Logger.Debug($"{this} - {signOfE == signOfT}");
                return signOfE == signOfT;
            }

            return false;
        }
    }

    public class PointObject_v1 : CanvasObject_v1 {
        public bool IsVisible { get; set; } = true;
        public SKPoint Point {
            get => this.Location;
            set {
                this.Location = value;
            }
        }

        private SKPaint fillPaint = new SKPaint {
            IsAntialias = true,
            Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Coral, 0.8f),
            Style = SKPaintStyle.Fill
        };
        private SKPaint strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        private float radius = 5.0f;

        private SKPoint gPoint;
        private float gRadius;

        public PointObject_v1() : base() {
            var dragAndDropComponent = new DragAndDropComponent();
            var selectableComponent = new SelectableComponent();

            this.AddComponent(dragAndDropComponent);
            this.AddComponent(selectableComponent);
        }

        public override bool ContainsPoint(SKPoint point) {
            return this.IsVisible? SKPoint.Distance(point, this.Point) <= this.radius : false;
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this.gPoint = worldCoordinate.TransformToDevice(this.Point);
            this.gRadius = worldCoordinate.WorldToDeviceTransform.MapRadius(this.radius);

            if (this.IsSelected) {
                this.fillPaint.Color = SKColors.Blue;
            } else {
                this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Coral, 0.8f);
            }
        }

        protected override void DrawThis(SKCanvas canvas) {
            if (!this.IsVisible) {
                return;
            }

            canvas.DrawCircle(this.gPoint, this.gRadius, fillPaint);
            canvas.DrawCircle(this.gPoint, this.gRadius, strokePaint);
        }
    }

    public class LineSegmentObject_v1 : CanvasObject_v1 {
        public PointObject_v1 P0 { get; set; }
        public PointObject_v1 P1 { get; set; }

        private SKPaint strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        private CircularList<SKPoint> extremes = new CircularList<SKPoint>();
        private CircularList<SKPoint> gExtremes = new CircularList<SKPoint>();

        private SKPoint gP0;
        private SKPoint gP1;
        private List<Action<SKCanvas>> addToPhases;
        private IEnumerator<Action<SKCanvas>> addToPhaseItor;

        public LineSegmentObject_v1() : base() {
            this.P0 = new PointObject_v1() {
                IsNodeVisible = false,
            };
            this.P1 = new PointObject_v1() {
                IsNodeVisible = false,
            };

            this.P0.SetParent(this);
            this.P1.SetParent(this);
            this.Children.Add(this.P0);
            this.Children.Add(this.P1);

            var selectableComponent = new SelectableComponent();

            this.AddComponent(selectableComponent);
        }


        public void StartAddToBehavior() {
            this.MouseClick += this.LineSegmentObject_v1_MouseClick;
            this.MouseMove += this.LineSegmentObject_v1_MouseMove;

            this.addToPhases = new List<Action<SKCanvas>>() {
                this.DrawAddToPhase0,
                this.DrawAddToPhase1,
            };
            this.addToPhaseItor = this.addToPhases.GetEnumerator();
            this.Dispatcher.Lock(this);
            this.addToPhaseItor.MoveNext();
        }

        public void StopAddToBehavior() {
            this.MouseClick -= this.LineSegmentObject_v1_MouseClick;
            this.MouseMove -= this.LineSegmentObject_v1_MouseMove;

            this.Dispatcher.Unlock();
        }

        private void LineSegmentObject_v1_MouseClick(Event @event) {
            var e = @event as MouseEvent;

            this.addToPhaseItor.MoveNext();

            if (this.addToPhaseItor.Current == null) {
                this.StopAddToBehavior();
            }
        }

        private void LineSegmentObject_v1_MouseMove(Event @event) {
            var e = @event as MouseEvent;
            var action = this.addToPhaseItor.Current;

            if (action.Method.Name.Contains("Phase0")) {
                this.P0.Point = e.Pointer;

            }
            else if (action.Method.Name.Contains("Phase1")) {
                this.P1.Point = e.Pointer;
            }
        }

        public override bool ContainsPoint(SKPoint point) {
            var l = this.P1.Point - this.P0.Point;
            var normal = new SKPoint {
                X = l.Y, Y = -l.X
            };
            normal = SKPoint.Normalize(normal);

            var rect = SKRect.Create(
                this.P0.Point,
                new SKSize { Height = 10.0f, Width = l.Length }
            );

            this.extremes = new CircularList<SKPoint> {
                new SKPoint {
                    X = this.P0.Point.X + normal.X * 5.0f,
                    Y = this.P0.Point.Y + normal.Y * 5.0f
                },
                new SKPoint {
                    X = this.P1.Point.X + normal.X * 5.0f,
                    Y = this.P1.Point.Y + normal.Y * 5.0f
                },
                new SKPoint {
                    X = this.P1.Point.X - normal.X * 5.0f,
                    Y = this.P1.Point.Y - normal.Y * 5.0f
                },
                new SKPoint {
                    X = this.P0.Point.X - normal.X * 5.0f,
                    Y = this.P0.Point.Y - normal.Y * 5.0f
                },
            };

            var result = true;

            foreach (var node in this.extremes) {
                var prev = node.Prev.Value;
                var it = node.Value;
                var side = SkiaHelper.GetSide(prev, it, point);

                result &= (side >= 0);
            }

            return result;
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this.gP0 = worldCoordinate.TransformToDevice(this.P0.Point);
            this.gP1 = worldCoordinate.TransformToDevice(this.P1.Point);

            if (this.extremes.Count() != 0) {
                this.gExtremes.Clear();

                foreach (var node in this.extremes) {
                    this.gExtremes.Add(worldCoordinate.TransformToDevice(node.Value));
                }
            }

            if (this.IsSelected) {
                this.P0.IsVisible = true;
                this.P1.IsVisible = true;
            } else {
                this.P0.IsVisible = false;
                this.P1.IsVisible = false;
            }
        }

        protected override void DrawThis(SKCanvas canvas) {
            if (this.addToPhaseItor.Current != null) {
                this.addToPhaseItor.Current?.Invoke(canvas);
            } else {
                // Invalidate Method also needed to be subscribed
                //this.P0.IsVisible = false;
                //this.P1.IsVisible = false;

                strokePaint.PathEffect = null;

                canvas.DrawLine(this.gP0, this.gP1, strokePaint);

                if (this.gExtremes.Count() != 0) {
                    var path = new SKPath();

                    path.MoveTo(this.gExtremes[0].Value);
                    path.LineTo(this.gExtremes[1].Value);
                    path.LineTo(this.gExtremes[2].Value);
                    path.LineTo(this.gExtremes[3].Value);

                    path.Close();

                    canvas.DrawPath(path, strokePaint);
                }
            }
        }

        private void DrawAddToPhase0(SKCanvas canvas) {
            // Invalidate Method also needed to be subscribed
            this.P1.IsVisible = false;
        }
        private void DrawAddToPhase1(SKCanvas canvas) {
            // Invalidate Method also needed to be subscribed
            this.P1.IsVisible = true;

            strokePaint.PathEffect = SKPathEffect.CreateDash(new float[] { 5.0f, 5.0f }, 0.0f);

            canvas.DrawLine(this.gP0, this.gP1, strokePaint);
        }
    }

    public class CircleObject_v1 : CanvasObject_v1 {
        public SKPoint Center {
            get => this.Location;
            set {
                this.Location = value;
            }
        }
        public float Radius { get; set; } = 50.0f;
        public SKPoint VirtualEnd { get; set; } = new SKPoint();

        private SKPoint gCenter;
        private SKPoint gVirtualEnd;
        private float gRadius;
        private float gCenteoidShapeRadius;
        private float gVirtualCircleRadius;
        private List<Action<SKCanvas>> addToPhases;
        private IEnumerator<Action<SKCanvas>> addToPhaseItor;

        public CircleObject_v1() : base() {
            var dragAndDropComponent = new DragAndDropComponent();
            var selectableComponent = new SelectableComponent();

            this.AddComponent(dragAndDropComponent);
            this.AddComponent(selectableComponent);
        }

        public void StartAddToBehavior() {
            this.MouseClick += this.CircleObject_v1_MouseClick;
            this.MouseMove += this.CircleObject_v1_MouseMove;

            this.addToPhases = new List<Action<SKCanvas>>() {
                this.DrawAddToPhase0,
                this.DrawAddToPhase1,
            };
            this.addToPhaseItor = this.addToPhases.GetEnumerator();
            this.Dispatcher.Lock(this);
            this.addToPhaseItor.MoveNext();
        }

        public void StopAddToBehavior() {
            this.MouseClick -= this.CircleObject_v1_MouseClick;
            this.MouseMove -= this.CircleObject_v1_MouseMove;

            this.Dispatcher.Unlock();
        }

        private void CircleObject_v1_MouseClick(Event @event) {
            var e = @event as MouseEvent;

            this.addToPhaseItor.MoveNext();

            if (this.addToPhaseItor.Current == null) {
                this.StopAddToBehavior();
            }
            else {
                this.Center = e.Pointer;
            }
        }

        private void CircleObject_v1_MouseMove(Event @event) {
            var e = @event as MouseEvent;
            var action = this.addToPhaseItor.Current;

            if (action.Method.Name.Contains("Phase0")) {
                this.Center = e.Pointer;

            }
            else if (action.Method.Name.Contains("Phase1")) {
                this.VirtualEnd = e.Pointer;
                this.Radius = SKPoint.Distance(this.Center, e.Pointer);
            }
        }

        public override bool ContainsPoint(SKPoint point) {
            return SKPoint.Distance(point, this.Center) <= this.Radius;
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this.gCenter = worldCoordinate.TransformToDevice(this.Center);
            this.gVirtualEnd = worldCoordinate.TransformToDevice(this.VirtualEnd);
            this.gRadius = worldCoordinate.WorldToDeviceTransform.MapRadius(this.Radius);

            this.gVirtualCircleRadius = worldCoordinate.WorldToDeviceTransform.MapRadius(50.0f);
            this.gCenteoidShapeRadius = worldCoordinate.WorldToDeviceTransform.MapRadius(5.0f);
        }

        protected override void DrawThis(SKCanvas canvas) {
            if (this.addToPhaseItor.Current != null) {
                this.addToPhaseItor.Current?.Invoke(canvas);
            } else {
                var _fillPaint = new SKPaint {
                    IsAntialias = true,
                    Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.8f),
                    Style = SKPaintStyle.Fill
                };
                var _strokePaint = new SKPaint {
                    IsAntialias = true,
                    Color = SKColors.Black,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2
                };

                canvas.DrawCircle(this.gCenter, this.gRadius, _fillPaint);
                canvas.DrawCircle(this.gCenter, this.gRadius, _strokePaint);
            }
        }

        private void DrawAddToPhase0(SKCanvas canvas) {
            var _fillPaint = new SKPaint {
                IsAntialias = true,
                Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Coral, 0.8f),
                Style = SKPaintStyle.Fill
            };
            var _strokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                PathEffect = SKPathEffect.CreateDash(new float[] { 5.0f, 5.0f }, 0.0f),
                StrokeWidth = 2
            };

            canvas.DrawCircle(this.gCenter, this.gVirtualCircleRadius, _strokePaint);

            _strokePaint.PathEffect = null;

            canvas.DrawCircle(this.gCenter, this.gCenteoidShapeRadius, _fillPaint);
            canvas.DrawCircle(this.gCenter, this.gCenteoidShapeRadius, _strokePaint);
        }

        private void DrawAddToPhase1(SKCanvas canvas) {
            var _fillPaint = new SKPaint {
                IsAntialias = true,
                Color = SkiaHelper.ConvertColorWithAlpha(SKColors.Coral, 0.8f),
                Style = SKPaintStyle.Fill
            };
            var _strokePaint = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                PathEffect = SKPathEffect.CreateDash(new float[] { 5.0f, 5.0f }, 0.0f),
                StrokeWidth = 2
            };

            canvas.DrawCircle(this.gCenter, this.gRadius, _fillPaint);
            canvas.DrawLine(this.gCenter, this.gVirtualEnd, _strokePaint);
            canvas.DrawCircle(this.gCenter, this.gRadius, _strokePaint);
        }
    }
}
