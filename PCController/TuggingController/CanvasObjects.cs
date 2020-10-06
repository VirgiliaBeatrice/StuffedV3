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
using System.ServiceModel.Channels;
using Xamarin.Forms.Internals;
using TuggingController.ShapeElements;

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

        public override string ToString() {
            return collection.ToString();
        }

        public bool Contains(T item) {
            return this.collection.Select(e => e.Value).Contains(item);
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

    public class DataZone_v1 : ContainerCanvasObject_v1 {
        private List<Entity_v1> _entities = new List<Entity_v1>();
        private List<Triangle_v1> triangles = new List<Triangle_v1>();
        private List<Entity_v1> extremes = new List<Entity_v1>();
        private CircularList<Entity_v1> newExtremes = new CircularList<Entity_v1>();
        private HashSet<Edge_v1> triangleEdges = new HashSet<Edge_v1>();
        private HashSet<Triangle_v1> outerTriangles = new HashSet<Triangle_v1>();
        private List<Edge_v1> convexhullEdges = new List<Edge_v1>();
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

        private Edge_v1 FindTriangleEdge(Entity_v1 e0, Entity_v1 e1) {
            return this.triangleEdges.Where(edge => edge.HasVertex(e0) && edge.HasVertex(e1)).FirstOrDefault();
        }

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
                        } else if (isPrev & !isNext) {
                            return (tri, new Entity_v1[] {
                                it, prev
                            });
                        } else if (!isPrev & isNext) {
                            return (tri, new Entity_v1[] {
                                it, next
                            });
                        } else {
                            return (null, null);
                        }
                    }
                ).Where(item => item.Item1 != null);

                foreach(var target in targets) {
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
                    } else {
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

        public void Add(Entity_v1 entity) {
            entity.SetParent(this);
            this._entities.Add(entity);

            if (this._entities.Count == 3) {
                var triangle = new Triangle_v1(this[0], this[1], this[2]);

                triangle.Edges.Add(new Edge_v1() {
                    E0 = this[0],
                    E1 = this[1]
                });
                triangle.Edges.Add(new Edge_v1() {
                    E0 = this[1],
                    E1 = this[2]
                });
                triangle.Edges.Add(new Edge_v1() {
                    E0 = this[2],
                    E1 = this[0]
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

                //var oppsiteVertices = new List<Entity_v1>();
                //var trianglesOfConvexEdge = new HashSet<Triangle_v1>();
                
                //foreach(var edge in this.convexhullEdges) {
                //    var targetTriangle = this.triangles.Where(tri => tri.HasEdge(edge)).FirstOrDefault();
                //    var targetVertex = targetTriangle.GetRestVertices(edge).FirstOrDefault();

                //    trianglesOfConvexEdge.Add(targetTriangle);
                //}

                this.SetTriangleExtensionRay();

                //List<Edge_v1> ridges = new List<Edge_v1>();

                //foreach (var extreme in this.extremes) {
                //    var edges = this.triangleEdges.Where(edge => edge.HasVertex(extreme)).ToArray();
                //    var ridges = edges.Except(this.convexhullEdges).ToArray();
                //    var validRidges = ridges.Where(r => trianglesOfConvexEdge.Any(tri => tri.HasEdge(r))).ToArray();

                //    this.Logger.Debug($"Extreme {extreme} has {ridges.Count()} ridge(s).");
                //    this.Logger.Debug($"Extreme {extreme} has {validRidges.Count()} valid ridge(s).");

                //    foreach(var ridge in validRidges) {

                //        if (this.extremes.Contains(ridge.E0)) {
                //            var ray = new Ray_v1();

                //            ray.P0 = ridge.E0.Point;
                //            ray.P1 = ridge.E0.Point - ridge.E1.Point + ridge.E0.Point;

                //            ridge.Children.Add(ray);
                //        }

                //        if (this.extremes.Contains(ridge.E1)) {
                //            var ray = new Ray_v1();

                //            ray.P0 = ridge.E1.Point;
                //            ray.P1 = ridge.E1.Point - ridge.E0.Point + ridge.E1.Point;

                //            ridge.Children.Add(ray);
                //        }


                //    }
                //}

                //List<(Entity_v1, Edge_v1, Triangle_v1)> vertexEdgeTriPairs = new List<(Entity_v1, Edge_v1, Triangle_v1)>();

                //foreach (var edge in this.convexhullEdges) {
                //    var targetTri = this.triangles.Where(tri => tri.IsEdge(edge)).First();
                //    var targetVertex = targetTri.GetRestVertices(edge).First();

                //    vertexEdgeTriPairs.Add((targetVertex, edge, targetTri));
                //}

                //foreach(var pair in vertexEdgeTriPairs) {
                //    var ray0 = new Ray_v1();
                //    var ray1 = new Ray_v1();

                //    ray0.P0 = pair.Item1.Point;
                //    ray0.P1 = pair.Item1.Point + pair.Item2.P0;
                //    pair.Item3.ExtensionRays.Add(ray0);

                //    ray1.P0 = pair.Item1.Point;
                //    ray1.P1 = pair.Item1.Point + pair.Item2.P1;
                //    pair.Item3.ExtensionRays.Add(ray1);
                //}
            }

            this.Children.Clear();
            this.Children.AddRange(this._entities);
            this.Children.AddRange(this.triangles);
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
        };

        public SKColor Color {
            set {
                this.rayPaint.Color = value;
            }
        }

        public float RayWidth {
            set {
                this.rayPaint.StrokeWidth = value;
            }
        }

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

            if (this.isHovered) {
                this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DimGray, 0.8f);
            } else {
                this.fillPaint.Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DimGray, 0.3f);
            }

            this.Children.Clear();
            this.Children.AddRange(this.Exterior.Rays);
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
}
