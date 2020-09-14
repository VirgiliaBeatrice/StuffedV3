using System;
using System.Windows.Forms;
using SkiaSharp;
using NLog;
using NLog.Layouts;

namespace TuggingController {
    public delegate void BehaviorHandler(BehaviorArgs args);

    public interface IComponent {
        ICanvasObject CanvasObject { get; set; }
        string Tag { get; }

        void Behavior(BehaviorArgs e);
    }

    public struct PaintComponent : IComponent {
        public ICanvasObject CanvasObject { get; set; }
        public string Tag { get; set; }
        public SKPaint FillPaint { get; set; }
        public SKPaint StrokePaint { get; set; }

        public void Behavior(BehaviorArgs e) {
            throw new NotImplementedException();
        }
    }

    public class HoverComponent : IComponent {
        public ICanvasObject CanvasObject { get; set; }
        public string Tag { get; set; }

        public void Behavior(BehaviorArgs e) {
            throw new NotImplementedException();
        }
    }

    //public class SelectableComponent : IComponent {
    //    public ICanvasObject CanvasObject { get; set; }
    //    public string Tag => "Select";
    //    public bool IsSelected { get; set; } = false;
    //    public SKPoint ClickLocation { get; set; } = new SKPoint();
    //    public event EventHandler SelectStatusChanged;

    //    public SelectableComponent() {
    //        this.SelectStatusChanged += this.OnSelectStatusChanged;
    //    }

    //    protected virtual void OnSelectStatusChanged(object sender, EventArgs e) { }

    //public void Behavior(BehaviorArgs e) {
    //    var type = this.CanvasObject.GetType();

    //    if (type == typeof(Entity_v1)) {
    //        return this.PointBehavior(e);
    //    } else if (type == typeof(Grid_v1)) {
    //        return this.RectBehavior(e);
    //    } else {
    //        return new BehaviorResult();
    //    }
    //}

    //public void PointBehavior(BehaviorArgs e) {
    //    var gPointer = ((SelectableBehaviorArgs)e).Location;
    //    var lPointer =
    //        this.CanvasObject.Transform.InvGlobalTransformation.MapPoint(gPointer);
    //    var distance = SKPoint.Distance(lPointer, this.CanvasObject.Location);

    //    this.ClickLocation = lPointer;

    //    if (distance < 5.0f) {
    //        this.IsSelected = !this.IsSelected;
    //        this.SelectStatusChanged.Invoke(this, null);

    //        return new SelectableBehaviorResult(this.CanvasObject) { ToNext = false };
    //    }

    //    return new BehaviorResult();
    //}

    //public void RectBehavior(BehaviorArgs e) {
    //    var gPointer = ((SelectableBehaviorArgs)e).Location;
    //    var lPointer =
    //        this.CanvasObject.Transform.InvGlobalTransformation.MapPoint(gPointer);
    //    var lBox = this.CanvasObject.BoarderBox;
    //    var gBox = this.CanvasObject.Transform.GlobalTransformation.MapRect(lBox);
    //    var isInside = gBox.Contains(gPointer);

    //    this.ClickLocation = lPointer;

    //    if (isInside) {
    //        this.IsSelected = !this.IsSelected;
    //        this.SelectStatusChanged.Invoke(this, null);

    //        return new SelectableBehaviorResult(this.CanvasObject) { ToNext = false };
    //    }

    //    return new BehaviorResult();
    //}
    //}

    public class DragAndDropComponent : ILog, IComponent {
        private ICanvasObject _canvasObject;
        private BehaviorHandler _mouseMoveBehavior;
        private SKPoint _anchor;
        private SKPoint _origin;
        private SKMatrix _initTranslate;
        public ICanvasObject CanvasObject {
            get => this._canvasObject;
            set {
                this._canvasObject = value;

                SetCanvasObject();
            }
        }
        public string Tag { get; } = "D&D";

        public Logger Logger => LogManager.GetCurrentClassLogger();

        public DragAndDropComponent() { }

        private void SetCanvasObject() {
            this._canvasObject.MouseMove += this.CanvasObject_MouseMove;
            this._canvasObject.MouseDown += this.CanvasObject_MouseDown;
            this._canvasObject.MouseUp += this.CanvasObject_MouseUp;
            this._canvasObject.DragStart += this.CanvasObject_DragStart;
            this._canvasObject.DragEnd += this.CanvasObject_DragEnd;
            this._canvasObject.Dragging += this.CanvasObject_Dragging;

            this._mouseMoveBehavior += this.Behavior;
        }

        public void PreventDefault(BehaviorHandler behavior) {
            this._mouseMoveBehavior -= this.Behavior;
            this._mouseMoveBehavior += behavior;
        }

        public void Behavior(BehaviorArgs e) {
            var args = e as DragAndDropBehaviorArgs;
            var target = this.CanvasObject;
            var origin = args.Origin;
            var lPointer = target.Transform.WorldToLocalMatrix.MapPoint(args.Location);
            var lAnchor = target.Transform.WorldToLocalMatrix.MapPoint(this._anchor);
            var translationVector = lPointer - lAnchor;

            target.Location = origin + translationVector;
        }

        protected void CanvasObject_MouseDown(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            if (@event.Target == target) {
                //this.Logger.Debug($"{target.GetType()} MouseDown");

                target.Dispatcher.Capture(target);

                var dragEvent = e.Clone();
                dragEvent.Type = "DragStart";

                target.Dispatcher.DispatchEvent(dragEvent);
            }
        }

        protected void CanvasObject_MouseUp(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            if (@event.Target == target) {
                //this.Logger.Debug($"{target.GetType()} MouseUp");

                target.Dispatcher.Release();

                var dragEvent = e.Clone();
                dragEvent.Type = "DragEnd";

                target.Dispatcher.DispatchEvent(dragEvent);
            }
        }

        protected void CanvasObject_MouseMove(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            if (target.Dispatcher.CapturedTarget != null) {
                var dragEvent = e.Clone();

                dragEvent.Type = "Dragging";
                target.Dispatcher.DispatchEvent(dragEvent);
            }
        }

        protected void CanvasObject_DragStart(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            //this.Logger.Debug($"{target.GetType()} DragStart");

            // Execute Behavior
            this._anchor = e.Pointer;
            this._origin = this._canvasObject.Location;
        }

        protected void CanvasObject_DragEnd(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            //this.Logger.Debug($"{target.GetType()} DragEnd");

            // Execute Behavior
            this._anchor = new SKPoint();
            this._origin = new SKPoint();
        }

        protected void CanvasObject_Dragging(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            //this.Logger.Debug($"{target.GetType()} DragMove");

            // Execute Behavior
            var behaviorArgs = new DragAndDropBehaviorArgs(e.Pointer) {
                Anchor = this._anchor,
                Origin = this._origin
            };

            this._mouseMoveBehavior?.Invoke(behaviorArgs);
        }
    }
}
