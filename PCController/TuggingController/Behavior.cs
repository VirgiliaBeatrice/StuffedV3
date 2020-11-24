using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace TuggingController {
    /// <summary>
    ///  Common behavior for canvas object
    /// </summary>
    public abstract class BaseBehavior : ILog {
        protected Behavior behavior;

        public ICanvasObject CanvasObject { get; set; }
        public string State { get; set; }

        public abstract string Tag { get; }
        public Logger Logger => LogManager.GetCurrentClassLogger();

        public abstract void Subscribe();
        public abstract void Unsubscribe();

        public virtual void RegisterBehavior(Behavior behavior) {
            this.behavior += behavior;
        }
    }

    public delegate void Behavior(BehaviorArgs args);

    public class HoverBehavior : BaseBehavior {
        public override string Tag => "Hover";

        public HoverBehavior() { }

        protected void OnMouseEnter(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            if (e.CurrentTarget == target) {
                this.behavior?.Invoke(new HoverBehaviorArgs(true));
            }
        }

        protected void OnMouseLeave(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            if (e.CurrentTarget == target) {
                this.behavior?.Invoke(new HoverBehaviorArgs(false));
            }
        }

        public override void Subscribe() {
            this.CanvasObject.MouseEnter += this.OnMouseEnter;
            this.CanvasObject.MouseLeave += this.OnMouseLeave;
        }

        public override void Unsubscribe() {
            this.CanvasObject.MouseEnter -= this.OnMouseEnter;
            this.CanvasObject.MouseLeave -= this.OnMouseLeave;
        }
    }

    public class SelectableBehavior : BaseBehavior {
        public override string Tag => "Selectable";

        public SelectableBehavior() {
            this.behavior += this.OnSelect;
        }

        protected void OnMouseClick(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            if (e.CurrentTarget == target) {
                var args = new SelectableBehaviorArgs {
                    Location = e.Pointer,
                };

                this.behavior?.Invoke(args);
            }
        }

        private void OnSelect(BehaviorArgs args) {
            this.CanvasObject.IsSelected = !this.CanvasObject.IsSelected;
        }

        public override void Subscribe() {
            this.CanvasObject.MouseClick += this.OnMouseClick;
        }

        public override void Unsubscribe() {
            this.CanvasObject.MouseClick -= this.OnMouseClick;
        }
    }

    public class AddableBehaviorArgs : BehaviorArgs {
        public string PhaseName { get; set; }
        public SKPoint Point { get; set; }
    }

    public class AddableBehavior : BaseBehavior {
        public override string Tag => "Addable";
        public List<Action<SKCanvas>> PhaseCollection = new List<Action<SKCanvas>>();

        public IEnumerator<Action<SKCanvas>> PhaseEnumerator = null;

        public void AddPhaseCallback(Action<SKCanvas> action) {
            this.PhaseCollection.Add(action);
            this.PhaseEnumerator = this.PhaseCollection.GetEnumerator();
            this.PhaseEnumerator.MoveNext();
        }

        public override void Subscribe() {
            this.CanvasObject.MouseClick += OnMouseClick;
            this.CanvasObject.MouseMove += OnMouseMove;
        }

        public override void Unsubscribe() {
            this.CanvasObject.MouseClick -= OnMouseClick;
            this.CanvasObject.MouseMove -= OnMouseMove;
        }

        private void OnMouseClick(Event @event) {
            var e = @event as MouseEvent;

            if (this.PhaseEnumerator.Current == this.PhaseCollection.Last()) {
                this.PhaseEnumerator = this.PhaseCollection.GetEnumerator();
                this.CanvasObject.Dispatcher.Unlock();
                this.CanvasObject.Dispatcher.OnTargetUnlocked(new TargetUnlockedEventArgs(this.CanvasObject));
            }
            else {
                this.PhaseEnumerator.MoveNext();
            }
        }

        private void OnMouseMove(Event @event) {
            var e = @event as MouseEvent;
            var action = this.PhaseEnumerator?.Current;

            if (action != null) {
                this.behavior?.Invoke(
                    new AddableBehaviorArgs {
                        PhaseName = action.Method.Name,
                        Point = e.Pointer,
                    }
                );
            }
        }
    }

    public class DnDBehavior : BaseBehavior {
        public override string Tag => "DnD";

        private SKPoint anchor;
        private SKPoint origin;

        public DnDBehavior() {
            this.behavior += this.OnDnD;
        }

        protected void OnMouseDown(Event @event) {
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

        protected void OnMouseUp(Event @event) {
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

        protected void OnMouseMove(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            if (target.Dispatcher.CapturedTarget != null) {
                var dragEvent = e.Clone();

                dragEvent.Type = "Dragging";
                target.Dispatcher.DispatchMouseEvent(dragEvent);
            }
        }

        protected void OnDragStart(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            //this.Logger.Debug($"{target.GetType()} DragStart");

            // Execute Behavior
            this.anchor = e.Pointer;
            this.origin = this.CanvasObject.Location;
        }

        protected void OnDragEnd(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            //this.Logger.Debug($"{target.GetType()} DragEnd");

            // Execute Behavior
            this.anchor = new SKPoint();
            this.origin = new SKPoint();
        }

        protected void OnDragging(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            //this.Logger.Debug($"{target.GetType()} DragMove");

            // Execute Behavior
            var behaviorArgs = new DragAndDropBehaviorArgs(e.Pointer) {
                Anchor = this.anchor,
                Origin = this.origin,
            };

            this.behavior?.Invoke(behaviorArgs);
        }

        public void OnDnD(BehaviorArgs args) {
            var castArgs = args as DragAndDropBehaviorArgs;
            var target = this.CanvasObject;
            var origin = castArgs.Origin;
            var lPointer = target.Transform.WorldToLocalMatrix.MapPoint(castArgs.Location);
            var lAnchor = target.Transform.WorldToLocalMatrix.MapPoint(this.anchor);
            var translationVector = lPointer - lAnchor;

            target.Location = origin + translationVector;
        }

        public override void Subscribe() {
            this.CanvasObject.MouseMove += this.OnMouseMove;
            this.CanvasObject.MouseDown += this.OnMouseDown;
            this.CanvasObject.MouseUp += this.OnMouseUp;
            this.CanvasObject.DragStart += this.OnDragStart;
            this.CanvasObject.Dragging += this.OnDragging;
            this.CanvasObject.DragEnd += this.OnDragEnd;
        }

        public override void Unsubscribe() {
            this.CanvasObject.MouseMove -= this.OnMouseMove;
            this.CanvasObject.MouseDown -= this.OnMouseDown;
            this.CanvasObject.MouseUp -= this.OnMouseUp;
            this.CanvasObject.DragStart -= this.OnDragStart;
            this.CanvasObject.Dragging -= this.OnDragging;
            this.CanvasObject.DragEnd -= this.OnDragEnd;
        }
    }
}
