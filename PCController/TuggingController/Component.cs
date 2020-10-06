using System;
using System.Windows.Forms;
using SkiaSharp;
using NLog;
using NLog.Layouts;
using NLog.LayoutRenderers;

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

    public class HoverComponent : ILog, IComponent {
        private ICanvasObject _canvasObject;
        private BehaviorHandler behaviors;

        public ICanvasObject CanvasObject {
            get => this._canvasObject;
            set {
                this._canvasObject = value;

                SetCanvasObject();
            }
        }
        public string Tag => "Hover";
        public Logger Logger => LogManager.GetCurrentClassLogger();

        private void SetCanvasObject() {
            this.CanvasObject.MouseEnter += this.CanvasObject_MouseEnter;
            this.CanvasObject.MouseLeave += this.CanvasObject_MouseLeave;

            this.behaviors += this.DefaultBehavior;
        }

        private void CanvasObject_MouseEnter(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            this.Logger.Debug($"{this.CanvasObject} enter");

            if (e.CurrentTarget == target) {
                var args = new HoverBehaviorArgs(true);

                this.behaviors?.Invoke(args);
            }
        }

        private void CanvasObject_MouseLeave(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            this.Logger.Debug($"{this.CanvasObject} leave");

            if (e.CurrentTarget == target) {
                var args = new HoverBehaviorArgs(false);

                this.behaviors?.Invoke(args);
            }
        }

        //private void CanvasObject_MouseMove(Event @event) {
        //    var e = @event as MouseEvent;
        //    var target = this.CanvasObject;
        //    var currentState = this.CanvasObject.ContainsPoint(e.Pointer);

        //    if (this.prevState != currentState) {
        //        if (currentState) {
        //            var enterEvent = e.Clone();
        //            enterEvent.Type = "MouseEnter";

        //            target.Dispatcher.DispatchEvent(enterEvent);
        //        } else {
        //            var leaveEvent = e.Clone();
        //            leaveEvent.Type = "MouseLeave";

        //            target.Dispatcher.DispatchEvent(leaveEvent);
        //        }

        //        this.prevState = currentState;
        //    }
        //}

        //private void CanvasObject_MouseLeave(Event @event) {
        //    var e = @event as MouseEvent;
        //    var target = this.CanvasObject;

        //    if (e.Target == target) {
        //        var args = new HoverBehaviorArgs(false);

        //        this.behaviors?.Invoke(args);
        //    }
        //}

        //private void CanvasObject_MouseEnter(Event @event) {
        //    var e = @event as MouseEvent;
        //    var target = this.CanvasObject;

        //    if (e.Target == target) {
        //        var args = new HoverBehaviorArgs(true);

        //        this.behaviors?.Invoke(args);
        //    }
        //}

        public void AddBehavior(BehaviorHandler behavior) {
            throw new NotImplementedException();
        }

        public void DefaultBehavior(BehaviorArgs e) { }

        public void PreventDefault(BehaviorHandler behavior) {
            this.behaviors -= this.DefaultBehavior;
            this.behaviors += behavior;
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

            if (e.CurrentTarget == target) {
                //this.Logger.Debug($"{target.GetType()} MouseClick");

                var args = new BehaviorArgs();
                this.behaviors?.Invoke(args);
            }
        }

        public void DefaultBehavior(BehaviorArgs e) {
            this.CanvasObject.IsSelected = !this.CanvasObject.IsSelected;
        }

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

            if (@event.CurrentTarget == target) {
                //this.Logger.Debug($"{target.GetType()} MouseDown");
                if (target.IsSelected) {
                    target.Dispatcher.Capture(target);

                    var dragEvent = e.Clone();
                    dragEvent.Type = "DragStart";

                    target.Dispatcher.DispatchMouseEvent(dragEvent);
                }
            }
        }

        protected void CanvasObject_MouseUp(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            if (@event.CurrentTarget == target) {
                //this.Logger.Debug($"{target.GetType()} MouseUp");

                if (!target.IsSelected) {
                    target.Dispatcher.Release();

                    var dragEvent = e.Clone();
                    dragEvent.Type = "DragEnd";

                    target.Dispatcher.DispatchMouseEvent(dragEvent);
                }
            }
        }

        protected void CanvasObject_MouseMove(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            if (target.Dispatcher.CapturedTarget != null) {
                var dragEvent = e.Clone();

                dragEvent.Type = "Dragging";
                target.Dispatcher.DispatchMouseEvent(dragEvent);
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
