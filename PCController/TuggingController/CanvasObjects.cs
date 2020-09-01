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

        public DragAndDropBehaviorArgs(int x, int y) {
            this._location = new SKPoint() { X = x, Y = y };
        }
    }

    public class SelectableBehaviorArgs : BehaviorArgs {
        private SKPoint _location;
        public SKPoint Location => this._location;

        public SelectableBehaviorArgs(int x, int y) {
            this._location = new SKPoint() { X = x, Y = y };
        }
    }

    public class SelectableBehaviorResult : BehaviorResult {
        public ICanvasObject Target { get; }

        public SelectableBehaviorResult(ICanvasObject target) {
            this.Target = target;
        }
    }


    public interface IComponent {
        ICanvasObject CanvasObject { get; set; }
        string Tag { get; }

        BehaviorResult Behavior(BehaviorArgs e);
    }

    public struct PaintComponent : IComponent {
        public ICanvasObject CanvasObject { get; set; }
        public string Tag { get; set; }
        public SKPaint FillPaint { get; set; }
        public SKPaint StrokePaint { get; set; }

        public BehaviorResult Behavior(BehaviorArgs e) {
            throw new NotImplementedException();
        }
    }

    public class HoverComponent : IComponent {
        public ICanvasObject CanvasObject { get; set; }
        public string Tag { get; set; }

        public BehaviorResult Behavior(BehaviorArgs e) {
            throw new NotImplementedException();
        }
    }

    public class SelectableComponent : IComponent {
        public ICanvasObject CanvasObject { get; set; }
        public string Tag => "Select";
        public bool IsSelected { get; set; } = false;
        public SKPoint ClickLocation { get; set; } = new SKPoint();
        public event EventHandler SelectStatusChanged;

        public SelectableComponent() {
            this.SelectStatusChanged += this.OnSelectStatusChanged;
        }

        protected virtual void OnSelectStatusChanged(object sender, EventArgs e) { }

        public BehaviorResult Behavior(BehaviorArgs e) {
            var type = this.CanvasObject.GetType();

            if (type == typeof(Entity_v1)) {
                return this.PointBehavior(e);
            } else if (type == typeof(Grid_v1)) {
                return this.RectBehavior(e);
            } else {
                return new BehaviorResult();
            }
        }

        public BehaviorResult PointBehavior(BehaviorArgs e) {
            var gPointer = ((SelectableBehaviorArgs)e).Location;
            var lPointer =
                this.CanvasObject.Transform.InvGlobalTransformation.MapPoint(gPointer);
            var distance = SKPoint.Distance(lPointer, this.CanvasObject.Location);

            this.ClickLocation = lPointer;

            if (distance < 5.0f) {
                this.IsSelected = !this.IsSelected;
                this.SelectStatusChanged.Invoke(this, null);

                return new SelectableBehaviorResult(this.CanvasObject) { ToNext = false };
            }

            return new BehaviorResult();
        }

        public BehaviorResult RectBehavior(BehaviorArgs e) {
            var gPointer = ((SelectableBehaviorArgs)e).Location;
            var lPointer =
                this.CanvasObject.Transform.InvGlobalTransformation.MapPoint(gPointer);
            var lBox = this.CanvasObject.BoarderBox;
            var gBox = this.CanvasObject.Transform.GlobalTransformation.MapRect(lBox);
            var isInside = gBox.Contains(gPointer);

            this.ClickLocation = lPointer;

            if (isInside) {
                this.IsSelected = !this.IsSelected;
                this.SelectStatusChanged.Invoke(this, null);

                return new SelectableBehaviorResult(this.CanvasObject) { ToNext = false };
            }

            return new BehaviorResult();
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

    public class DragAndDropComponent : IComponent {
        public ICanvasObject CanvasObject { get; set; }
        public string Tag { get; } = "D&D";
        public event EventHandler Dragging;

        public DragAndDropComponent() {
            this.Dragging += this.OnDragging;
        }

        private void OnDragging(object sender, EventArgs e) { }

        public BehaviorResult Behavior(BehaviorArgs e) {
            var selComponent = (SelectableComponent)this.CanvasObject.Components.Find(c => c.Tag == "Select");

            if (!selComponent.IsSelected) {
                return new BehaviorResult();
            }

            var gPointer = ((DragAndDropBehaviorArgs)e).Location;
            var lPointer =
                this.CanvasObject.Transform.InvGlobalTransformation.MapPoint(gPointer);

            //this.CanvasObject.Location = lPointer;
            this.Dragging.Invoke(this, new DragAndDropComponentEventArgs(lPointer, selComponent.ClickLocation));

            return new BehaviorResult() { ToNext = false };
        }
    }

    public interface ICanvasObject {
        SKRect BoarderBox { get; set; }
        SKSize Size { get; set; }
        Transform Transform { get; set; }
        SKPoint Location { get; set; }
        SKPoint GlobalLocation { get; }
        List<ICanvasObject> Children { get; set; }
        List<IComponent> Components { get; set; }
        void Draw(SKCanvas canvas);
        BehaviorResult Execute(BehaviorArgs e, string tag);
        void AddComponent(IComponent component);
        void AddComponents(IEnumerable<IComponent> components);
    }

    public abstract class CanvasObject_v1 : ILog, ICanvasObject {
        protected bool _isDebug = true;
        protected Transform _transform = new Transform();
        //protected SKSize _size = new SKSize();

        public Logger Logger { get; protected set; } = LogManager.GetCurrentClassLogger();

        public SKRect BoarderBox { get; set; } = new SKRect();
        public virtual SKSize Size {
            get => this.BoarderBox.Size;
            set {
                var oldBoarderBox = this.BoarderBox;

                oldBoarderBox.Size = value;
            }
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
                return this.Transform.MapPoint(this.Location);
            }
        }

        protected PaintComponent PaintComponent { get; set; } = new PaintComponent();

        protected CanvasObject_v1() {
            this._transform.CanvasObject = this;
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
    }

    public abstract class ContainerCanvasObject_v1 : CanvasObject_v1 {
        public override void Draw(SKCanvas canvas) {
            // Redraw
            // Invalidate() first, then DrawThis() and Draw() of all children.

            foreach (var child in this.Children) {
                child.Draw(canvas);
            }
        }
    }

    public partial class Entity_v1 : CanvasObject_v1 {
        private SelectableComponent _selectableComponent;
        private DragAndDropComponent _dragAndDropComponent;
        private SKPoint _gLocation;
        private int _index;
        private float _radius = 5.0f;
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

        public float Radius {
            get => this._radius;
            set {
                this._radius = value;
            }
        }
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
            this._selectableComponent = new SelectableComponent();
            this._dragAndDropComponent = new DragAndDropComponent();

            this._selectableComponent.SelectStatusChanged += this.SelectableComponent_SelectStatusChanged;
            this._dragAndDropComponent.Dragging += this.DragAndDropComponent_Dragging;

            this.AddComponents(new IComponent[] {
                this._dragAndDropComponent ,
                this._selectableComponent
            });
        }

        private void DragAndDropComponent_Dragging(object sender, EventArgs e) {
            var eArgs = (DragAndDropComponentEventArgs)e;

            this.Location = eArgs.NewLocation;
        }

        private void SelectableComponent_SelectStatusChanged(object sender, EventArgs e) {
            var component = (SelectableComponent)sender;

            this.Radius += component.IsSelected ? 2.0f : -2.0f;
        }

        protected override void DrawThis(SKCanvas canvas) {
            canvas.DrawCircle(this._gLocation, this._radius, this._fillPaint);
            canvas.DrawCircle(this._gLocation, this._radius, this._strokePaint);
        }

        protected override void Invalidate() {
            this._gLocation = this._transform.MapPoint(this.Location);
        }
        protected override BehaviorResult ExecuteThis(BehaviorArgs e, string tag = "") {
            BehaviorResult result = new BehaviorResult();

            if (tag.Length == 0) {
                foreach (var component in this.Components) {
                    result = component.Behavior(e);
                }
            } else {
                var targetComponent = this.Components.Find(c => c.Tag == tag);
                result = targetComponent?.Behavior(e);
            }

            return result;
        }
    }

    public class EntityCollection_v1 : ContainerCanvasObject_v1 {
        private List<Entity_v1> _entities = new List<Entity_v1>();

        public EntityCollection_v1() : base() { }

        public void Add(Entity_v1 entity) {
            entity.SetParent(this);
            this._entities.Add(entity);
            this.Children.Add(entity);
        }

        public void Add(SKPoint point) {
            var lSKPoint = this.Transform.InvGlobalTransformation.MapPoint(point);

            this.Add(new Entity_v1() { Location = lSKPoint });
        }

        public void AddRange(IEnumerable<Entity_v1> entities) {
            foreach(var e in entities) {
                this.Add(e);
            }
        }

        //protected override void ExecuteThis(BehaviorArgs e, string tag = "") { }
    }

    public class Chart_v1 : ContainerCanvasObject_v1 {
        private Grid_v1 _grid = new Grid_v1();
        private EntityCollection_v1 _entities = new EntityCollection_v1();
        private float _scale = 1.0f;

        public float Scale {
            get => this._scale;
            set {
                this._scale = value;

                foreach (var child in this.Children) {
                    child.Transform.Scale = 
                        SKMatrix.MakeScale(this._scale, this._scale);
                }
            }
        }

        public override SKSize Size {
            get => this.BoarderBox.Size;
            set {
                this.BoarderBox = new SKRect() { Size = value };
                this._grid.Size = value;
            }
        }

        public Chart_v1() : base() {
            this._grid.SetParent(this);
            this.Children.Add(this._grid);

            this._entities.SetParent(this);
            this.Children.Add(this._entities);

            // Test Purpose
            this._entities.AddRange(new Entity_v1[] {
                new Entity_v1() { Location = new SKPoint(100.0f, 100.0f)},
                new Entity_v1() { Location = new SKPoint(-100.0f, 100.0f)},
                new Entity_v1() { Location = new SKPoint(100.0f, -100.0f)},
            });
        }
        protected override void DrawThis(SKCanvas canvas) {
            var origin = new SKPoint(0.0f, 0.0f);

            canvas.DrawCircle(
                this.Transform.MapPoint(origin),
                5.0f,
                new SKPaint() { Color = SKColors.BlueViolet }
            );
        }

        public void AddEntity(Point point) {
            var gSKPoint = new SKPoint(point.X, point.Y);
            this._entities.Add(gSKPoint);
        }

        //protected override void ExecuteThis(BehaviorArgs e, string tag = "") { }
    }

    public class Grid_v1 : CanvasObject_v1 {
        private SelectableComponent _selectableComponent;
        private DragAndDropComponent _dragAndDropComponent;
        private List<Line_v1> _horizontalLines = new List<Line_v1>();
        private List<Line_v1> _verticalLines = new List<Line_v1>();
        private SKPoint _origin = new SKPoint();
        private int _gridScale = 50;
        private SKSize _size = new SKSize();

        public SKPoint Anchor { get; set; } = new SKPoint();

        // Note: Coordinate is different to SKRect
        // Chart: Left-Bottom, SKRect: Left-Top
        //public new SKRect BoarderBox { get; set; } = new SKRect();
        public override SKSize Size {
            get => this._size;
            set {
                this._size = value;
                this.UpdateBoarderBox();
            }
        }

        public SKPaint BoarderPaint { get; set; } = new SKPaint() { Color = SKColors.BlueViolet, IsStroke = true, StrokeWidth = 6.0f };

        public Grid_v1() : base() {
            this.Transform.TransformChanged += this.Transform_TransformChanged;

            this._selectableComponent = new SelectableComponent();
            this._dragAndDropComponent = new DragAndDropComponent();

            this._selectableComponent.SelectStatusChanged += this.SelectableComponent_SelectStatusChanged;
            this._dragAndDropComponent.Dragging += this.DragAndDropComponent_Dragging;

            this.AddComponents(new IComponent[] {
                this._selectableComponent,
                this._dragAndDropComponent
            });
        }

        private void DragAndDropComponent_Dragging(object sender, EventArgs e) {
            var eArgs = (DragAndDropComponentEventArgs)e;
            var t = this.Transform.Translation;
            var newLocation = eArgs.NewLocation;
            var anchor = eArgs.Anchor;
            var vector = newLocation - anchor;

            SKMatrix.PostConcat(
                ref t,
                SKMatrix.MakeTranslation(vector.X, vector.Y)
            );

            this.Transform.Translation = t;
        }

        private void SelectableComponent_SelectStatusChanged(object sender, EventArgs e) {
            var component = (SelectableComponent)sender;

            if (component.IsSelected) {
                this.BoarderPaint = new SKPaint() { Color = SKColors.DimGray, IsStroke = true, StrokeWidth = 6.0f };
            } else {
                this.BoarderPaint = new SKPaint() { Color = SKColors.BlueViolet, IsStroke = true, StrokeWidth = 6.0f };
            }
        }

        private void Transform_TransformChanged(object sender, EventArgs e) {
            this.UpdateBoarderBox();
        }

        private void UpdateBoarderBox() {
            var lBox = new SKRect() {
                Left = this.Location.X - Math.Abs(this._size.Width) / 2,
                Right = this.Location.X + Math.Abs(this._size.Width) / 2,
                Bottom = this.Location.Y - Math.Abs(this._size.Height) / 2,
                Top = this.Location.Y + Math.Abs(this._size.Height) / 2
            };

            // gBox is standardized SKRect.
            this.BoarderBox = this.Transform.InvLocalTransformation.MapRect(lBox);
        }

        private void UpdateGrid() {
            // Clear lines
            this._horizontalLines.Clear();
            this._verticalLines.Clear();
            this.Children.Clear();

            // !Performance!
            // TODO: overflow
            List<int> calculate(float min, float max, int interval) {
                var ret = new List<int>();
                int minInt = (int)Math.Truncate(min);
                int maxInt = (int)Math.Truncate(max);
                int quotient = Math.DivRem(Math.Abs(minInt), interval, out int reminder);
                int value = minInt + reminder;

                while (true) {
                    if (value > maxInt) {
                        break;
                    }

                    ret.Add(value);

                    value += interval;
                }

                return ret;
            }

            var gridXCoordinates = calculate(this.BoarderBox.Left, this.BoarderBox.Right, this._gridScale);
            var gridYCoordinates = calculate(this.BoarderBox.Top, this.BoarderBox.Bottom, this._gridScale);

            foreach (var x in gridXCoordinates) {
                var vLine = new Line_v1() {
                    P0 = new SKPoint(x, this.BoarderBox.Bottom),
                    P1 = new SKPoint(x, this.BoarderBox.Top)
                };

                if (x == 0) {
                    vLine.Paint.StrokeWidth = 2.0f;
                }

                vLine.SetParent(this);
                this._verticalLines.Add(vLine);
                this.Children.Add(vLine);
            }

            foreach (var y in gridYCoordinates) {
                var hLine = new Line_v1() {
                    P0 = new SKPoint(this.BoarderBox.Left, y),
                    P1 = new SKPoint(this.BoarderBox.Right, y)
                };

                if (y == 0) {
                    hLine.Paint.StrokeWidth = 2.0f;
                }

                hLine.SetParent(this);
                this._horizontalLines.Add(hLine);
                this.Children.Add(hLine);
            }
        }

        protected override void DrawThis(SKCanvas canvas) {
            canvas.DrawRect(this.Transform.MapRect(this.BoarderBox), this.BoarderPaint);
        }

        protected override void Invalidate() {
            this.UpdateGrid();
        }

        protected override BehaviorResult ExecuteThis(BehaviorArgs e, string tag = "") {
            var result = new BehaviorResult();

            if (tag == "") {
                foreach (var component in this.Components) {
                    result = component.Behavior(e);
                }
            }
            else {
                var targetComponent = this.Components.Find(c => c.Tag == tag);

                if (targetComponent != null) {
                    result = targetComponent.Behavior(e);
                }
            }

            return result;
        }
    }

    public partial class Line_v1 : CanvasObject_v1 {
        private SKPaint _paint = new SKPaint() {
            Color = SKColors.Gray,
            StrokeWidth = 1,
        };
        public SKPaint Paint {
            get => this._paint;
            set {
                this._paint = value;
            }
        }
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
                this._paint
            );
        }

        protected override void Invalidate() {
            this._gP0 = this._transform.MapPoint(this.P0);
            this._gP1 = this._transform.MapPoint(this.P1);
        }

        //protected override void ExecuteThis(BehaviorArgs e, string tag = "") { }
    }
}
