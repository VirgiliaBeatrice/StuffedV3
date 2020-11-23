using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
