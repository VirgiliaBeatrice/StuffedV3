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
        bool ContainsPoint(SKPoint point);
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

    public class EventDispatcher<T> where T : ICanvasObject {
        private static EventDispatcher<T> instance = null;
        public static EventDispatcher<T> GetSingleton() {
            if (instance == null) {
                instance = new EventDispatcher<T>();
            }

            return instance;
        }

        protected Queue<Event> Events { get; set; } = new Queue<Event>();
        public ICanvasObject CapturedTarget { get; set; } = null;
        protected bool _propagate = true;
        public T Root { get; set; }

        public EventDispatcher() { }

        public EventDispatcher(T root) {
            this.Root = root;
        }

        public void Capture(ICanvasObject target) {
            this.CapturedTarget = target;
            this.StopPropagate();
        }

        public void Release() {
            this.CapturedTarget = null;
            this._propagate = true;
        }

        public void StopPropagate() {
            this._propagate = false;
        }

        public virtual void DispatchEvent(Event @event) {
            Event e;
            FieldInfo eventInfo;

            if (@event.GetType() == typeof(MouseEvent)) {
                e = ((MouseEvent)@event).Clone();

                this.FindMouseEventTarget(ref e, this.Root);
                if (e.Path.Count != 0) {
                    e.Target = e.Path.Last();
                }
            } else {
                e = @event.Clone();
            }

            eventInfo = typeof(CanvasObject_v1).GetField(e.Type, BindingFlags.Instance | BindingFlags.NonPublic);

            if (eventInfo != null) {
                if (this.CapturedTarget != null) {
                    EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(this.CapturedTarget);

                    e.CurrentTarget = this.CapturedTarget;
                    handler?.Invoke(e);
                } else {
                    // Capture Phase
                    //foreach(var node in e.Path) {
                    //    EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(node);

                    //    e.CurrentTarget = node;
                    //    handler.Invoke(e);
                    //}

                    // Bubble Phase
                    foreach (var node in e.Path.ToArray().Reverse()) {
                        if (!this._propagate) {
                            break;
                        }

                        EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(node);

                        e.CurrentTarget = node;
                        handler?.Invoke(e);
                    }
                }
            }
        }

        protected void FindMouseEventTarget(ref Event @event, ICanvasObject node) {
            var eventRef = @event as MouseEvent;
            var pointer = eventRef.Pointer;
            var lPointer = node.Transform.InvGlobalTransformation.MapPoint(pointer);

            eventRef.CurrentTarget = node;

            if (node.ContainsPoint(lPointer)) {
                eventRef.Path.Add(node);

                // Children Reversed Order - Top-most Object
                foreach(var childNode in node.Children) {
                    this.FindMouseEventTarget(ref @event, childNode);
                }

            }

            //eventRef.Target = eventRef.Path.Last();
        }
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

        protected virtual void OnMouseEnter(Event @event) {
            this.MouseEnter?.Invoke(@event);
        }

        protected virtual void OnMouseLeave(Event @event) {
            this.MouseLeave?.Invoke(@event);
        }

        protected virtual void OnMouseMove(Event @event) {
            this.MouseMove?.Invoke(@event);
        }

        protected virtual void OnMouseUp(Event @event) {
            this.MouseUp?.Invoke(@event);
        }

        protected virtual void OnMouseDown(Event @event) {
            this.MouseDown?.Invoke(@event);
        }

        protected virtual void OnMouseClick(Event @event) {
            this.MouseClick?.Invoke(@event);
        }

        protected virtual void OnMouseDoubleClick(Event @event) {
            this.MouseDoubleClick?.Invoke(@event);
        }

        protected virtual void OnMouseWheel(Event @event) {
            this.MouseWheel?.Invoke(@event);
        }

        protected virtual void OnDragStart(Event @event) {
            this.DragStart?.Invoke(@event);
        }

        protected virtual void OnDragEnd(Event @event) {
            this.DragEnd?.Invoke(@event);
        }

        protected virtual void OnDragging(Event @event) {
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

        private float _gRadius;
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

            this.AddComponents(new IComponent[] {
                this._dragAndDropComponent,
            });
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
        }

        protected override void DrawThis(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate) {
            canvas.DrawCircle(this._gLocation, this._gRadius, this._fillPaint);
            canvas.DrawCircle(this._gLocation, this._gRadius, this._strokePaint);
        }
    }

    public class DataZone_v1 : ContainerCanvasObject_v1 {
        private List<Entity_v1> _entities = new List<Entity_v1>();

        public DataZone_v1() : base() { }

        public void Add(Entity_v1 entity) {
            entity.SetParent(this);
            this._entities.Add(entity);
            this.Children.Add(entity);
        }

        public void Add(SKPoint point) {
            this.Add(new Entity_v1() { Location = point });
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

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) { }

        protected override void DrawThis(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate) { }

        //protected override void ExecuteThis(BehaviorArgs e, string tag = "") { }
    }

    public class MathCoordinate_v1 : CanvasObject_v1 {
        private Grid_v1 _grid = new Grid_v1();
        public SKPoint Origin => new SKPoint(
            this.Transform.Translation.ScaleX,
            this.Transform.Translation.ScaleY
            );
        public MathCoordinate_v1() {
        }
    }



    public class Chart_v1 : CanvasObject_v1 {
        //private DataZone_v1 _dataZone = new DataZone_v1();
        private Grid_v1 _grid = new Grid_v1();
        private float _scale = 1.0f;



        public Chart_v1() : base() {
            this.Dispatcher.Root = this;

            this._grid.SetParent(this);
            this.Children.Add(this._grid);

            //this._dataZone.SetParent(this);
            //this.Children.Add(this._dataZone);

            // Test Purpose
            //this._dataZone.AddRange(new Entity_v1[] {
            //    new Entity_v1() { Location = new SKPoint(100.0f, 100.0f)},
            //    new Entity_v1() { Location = new SKPoint(-100.0f, 100.0f)},
            //    new Entity_v1() { Location = new SKPoint(100.0f, -100.0f)},
            //});
        }

        protected override void DrawThis(SKCanvas canvas) {
            canvas.DrawCircle(
                this.GlobalLocation,
                5.0f,
                new SKPaint() { Color = SKColors.BlueViolet }
            );

            canvas.DrawRect(
                this.Transform.GlobalTransformation.MapRect(this.BoarderBox),
                new SKPaint() {
                    Color = SKColors.BlueViolet,
                    StrokeWidth = 2.0f,
                    IsStroke = true,
                }
            );
        }
        public void SetSize(SKSize size) {
            var boarderBox = new SKRect() { Size = size };

            this._pBoarderBox = boarderBox;

            foreach (var child in this.Children) {
                child.SetBoarderBoxFromParent(this.BoarderBox);
            }
        }

        public void SetScale(float scale) {
            this._scale = scale;

            //this._dataZone.Transform.Scale = SKMatrix.MakeScale(this._scale, this._scale);
            this._grid.Transform.Scale = SKMatrix.MakeScale(this._scale, this._scale);
        }

        public float GetScale() {
            return this._scale;
        }

        public void AddEntity(Point point) {
            var gSKPoint = new SKPoint(point.X, point.Y);
            //this._dataZone.Add(gSKPoint);
        }

        public override bool ContainsPoint(SKPoint point) {
            return this.BoarderBox.Contains(point);
        }

        protected override void Invalidate() { }
    }

    public class Grid_v1 : CanvasObject_v1 {
        //private SelectableComponent _selectableComponent;
        private DragAndDropComponent _dragAndDropComponent;
        private List<Line_v1> _horizontalLines = new List<Line_v1>();
        private List<Line_v1> _verticalLines = new List<Line_v1>();
        private DataZone_v1 _dataZone = new DataZone_v1();
        private SKPoint _origin = new SKPoint();
        private int _gridScale = 50;

        public SKPoint Anchor { get; set; } = new SKPoint();

        // Note: Coordinate is different to SKRect
        // Chart: Left-Bottom, SKRect: Left-Top

        public SKPaint BoarderPaint { get; set; } = new SKPaint() { Color = SKColors.DarkKhaki, IsStroke = true, StrokeWidth = 6.0f };

        public Grid_v1() : base() {
            this.Transform.TransformChanged += this.Transform_TransformChanged;

            //this._selectableComponent = new SelectableComponent();
            this._dragAndDropComponent = new DragAndDropComponent();

            //this._selectableComponent.SelectStatusChanged += this.SelectableComponent_SelectStatusChanged;

            // !Note! - Order
            // 1. AddComponent
            // 2. PreventDefault
            this.AddComponents(new IComponent[] {
                //this._selectableComponent,
                this._dragAndDropComponent
            });
            this._dragAndDropComponent.PreventDefault(DragAndDropComponent_Dragging);

            this._dataZone.SetParent(this);
            this.Children.Add(this._dataZone);

            // Test Purpose
            this._dataZone.AddRange(new Entity_v1[] {
                new Entity_v1() { Location = new SKPoint(100.0f, 100.0f)},
                new Entity_v1() { Location = new SKPoint(-100.0f, 100.0f)},
                new Entity_v1() { Location = new SKPoint(100.0f, -100.0f)},
            });
        }

        private void DragAndDropComponent_Dragging(BehaviorArgs args) {
            var dndArgs = (DragAndDropBehaviorArgs)args;
            var t = dndArgs.InitialTranslation;
            var newLocation = this.Transform.InvGlobalTransformation.MapPoint(dndArgs.Location);
            var anchor = this.Transform.InvGlobalTransformation.MapPoint(dndArgs.Anchor);
            var lVector = newLocation - anchor;

            SKMatrix.PostConcat(
                ref t,
                SKMatrix.MakeTranslation(lVector.X, lVector.Y)
            );

            this.Transform.Translation = t;
        }

        //private void SelectableComponent_SelectStatusChanged(object sender, EventArgs e) {
        //    var component = (SelectableComponent)sender;

        //    if (component.IsSelected) {
        //        this.BoarderPaint = new SKPaint() { Color = SKColors.DimGray, IsStroke = true, StrokeWidth = 6.0f };
        //    } else {
        //        this.BoarderPaint = new SKPaint() { Color = SKColors.BlueViolet, IsStroke = true, StrokeWidth = 6.0f };
        //    }
        //}

        private void Transform_TransformChanged(object sender, EventArgs e) { }

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
                int quotient = Math.DivRem(minInt, interval, out int reminder);
                int value;
                if (minInt < 0) {
                    value = minInt + Math.Abs(reminder);
                } else {
                    value = minInt + interval - reminder;
                }

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

            this.Children.Add(this._dataZone);
        }

        protected override void DrawThis(SKCanvas canvas) {
            var gBoarderBox = this.Transform.GlobalTransformation.MapRect(this.BoarderBox);

            canvas.DrawRect(gBoarderBox, this.BoarderPaint);
        }

        protected override void Invalidate() {
            this.UpdateGrid();
        }

        //protected override BehaviorResult ExecuteThis(BehaviorArgs e, string tag = "") {
        //    var result = new BehaviorResult();

        //    if (tag == "") {
        //        foreach (var component in this.Components) {
        //            result = component.Behavior(e);
        //        }
        //    }
        //    else {
        //        var targetComponent = this.Components.Find(c => c.Tag == tag);

        //        if (targetComponent != null) {
        //            result = targetComponent.Behavior(e);
        //        }
        //    }

        //    return result;
        //}

        public override bool ContainsPoint(SKPoint point) {
            return this.BoarderBox.Contains(point);
        }
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
                canvas.DrawCircle(
                    worldCoordinate.TransformToDevice(this._gP0),
                    3.0f,
                    new SKPaint() { Color = SKColors.Red }
                );
                canvas.DrawCircle(
                    worldCoordinate.TransformToDevice(this._gP1),
                    3.0f,
                    new SKPaint() { Color = SKColors.DarkOliveGreen }
                );
            }

            canvas.DrawLine(
                worldCoordinate.TransformToDevice(this._gP0),
                worldCoordinate.TransformToDevice(this._gP1),
                //this._gP0,
                //this._gP1,
                this.Paint
            );
        }

        protected override void Invalidate() {
            this._gP0 = this._transform.MapPoint(this.P0);
            this._gP1 = this._transform.MapPoint(this.P1);
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this.Invalidate();
        }

        public override bool ContainsPoint(SKPoint point) {
            var vp = SkiaExtension.SkiaHelper.ToVector(point);
            var v = vp - this.V0;
            var result = v[0] * this.Direction[1] - v[1] * this.Direction[0];
            return result == 0.0f? true : false;
        }
        //protected override void ExecuteThis(BehaviorArgs e, string tag = "") { }
    }
}
