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

            //this.Logger.Debug($"{this.CanvasObject} enter");

            if (e.CurrentTarget == target) {
                this.behavior?.Invoke(new HoverBehaviorArgs(true));
            }
        }

        protected void OnMouseLeave(Event @event) {
            var e = @event as MouseEvent;
            var target = this.CanvasObject;

            //this.Logger.Debug($"{this.CanvasObject} leave");

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
                //this.Logger.Debug($"{target.GetType()} MouseClick");

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

            //if (this.PhaseEnumerator == null) {
            //    this.PhaseEnumerator = this.PhaseCollection.GetEnumerator();
            //}

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


            //if (action.Method.Name.Contains("Phase0")) {
            //    this.Center = e.Pointer;

            //}
            //else if (action.Method.Name.Contains("Phase1")) {
            //    this.VirtualEnd = e.Pointer;
            //    this.Radius = SKPoint.Distance(this.Center, e.Pointer);
            //}
        }
    }
}
