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

        void DefaultBehavior(BehaviorArgs e);
        void PreventDefault(BehaviorHandler behavior);
        void AddBehavior(BehaviorHandler behavior);
    }

    public struct PaintComponent : IComponent {
        public ICanvasObject CanvasObject { get; set; }
        public string Tag { get; set; }
        public SKPaint FillPaint { get; set; }
        public SKPaint StrokePaint { get; set; }

        public void AddBehavior(BehaviorHandler behavior) {
            throw new NotImplementedException();
        }

        public void DefaultBehavior(BehaviorArgs e) {
            throw new NotImplementedException();
        }

        public void PreventDefault(BehaviorHandler behavior) {
            throw new NotImplementedException();
        }
    }

    public class HoverComponent : IComponent {
        public ICanvasObject CanvasObject { get; set; }
        public string Tag { get; set; }

        public void AddBehavior(BehaviorHandler behavior) {
            throw new NotImplementedException();
        }

        public void DefaultBehavior(BehaviorArgs e) {
            throw new NotImplementedException();
        }

        public void PreventDefault(BehaviorHandler behavior) {
            throw new NotImplementedException();
        }
    }

    public class SelectableComponent : ILog, IComponent {
        private ICanvasObject _canvasObject;
        private BehaviorHandler behaviors;

        public ICanvasObject CanvasObject {
            get => this._canvasObject;
            set {
                this._canvasObject = value;

                SetCanvasObject();
            }
        }
        public string Tag => "Select";
        public SKPoint ClickLocation { get; set; } = new SKPoint();

        public Logger Logger => LogManager.GetCurrentClassLogger();

        public SelectableComponent() { }

        private void SetCanvasObject() {
            this._canvasObject.MouseClick += this.CanvasObject_MouseClick;

            this.behaviors += this.DefaultBehavior;
        }

        private void CanvasObject_MouseClick(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            if (e.Target == target) {
                //this.Logger.Debug($"{target.GetType()} MouseClick");

                var args = new BehaviorArgs();
                this.behaviors?.Invoke(args);
            }
        }

        public void DefaultBehavior(BehaviorArgs e) {
            this.CanvasObject.IsSelected = !this.CanvasObject.IsSelected;
        }

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

        public void PreventDefault(BehaviorHandler behavior) {
            throw new NotImplementedException();
        }

        public void AddBehavior(BehaviorHandler behavior) {
            this.behaviors += behavior;
        }
    }

    public class DragAndDropComponent : ILog, IComponent {
        private ICanvasObject _canvasObject;
        private BehaviorHandler _mouseMoveBehavior;
        private SKPoint _anchor;
        private SKPoint _origin;
        private SKMatrix _initTranslate;
        private bool willExecuteDefault = true;

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

            this._mouseMoveBehavior += this.DefaultBehavior;
        }

        public void PreventDefault(BehaviorHandler behavior) {
            this._mouseMoveBehavior -= this.DefaultBehavior;
            this._mouseMoveBehavior += behavior;
        }

        public void DefaultBehavior(BehaviorArgs e) {
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
                if (target.IsSelected) {
                    target.Dispatcher.Capture(target);

                    var dragEvent = e.Clone();
                    dragEvent.Type = "DragStart";

                    target.Dispatcher.DispatchEvent(dragEvent);
                }
            }
        }

        protected void CanvasObject_MouseUp(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            if (@event.Target == target) {
                //this.Logger.Debug($"{target.GetType()} MouseUp");

                if (!target.IsSelected) {
                    target.Dispatcher.Release();

                    var dragEvent = e.Clone();
                    dragEvent.Type = "DragEnd";

                    target.Dispatcher.DispatchEvent(dragEvent);
                }
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

        public void AddBehavior(BehaviorHandler behavior) {
            this._mouseMoveBehavior += behavior;
        }
    }
}
