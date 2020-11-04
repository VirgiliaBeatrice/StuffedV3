using SkiaSharp;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using System.Linq;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Security.Cryptography;

namespace TuggingController {
    public delegate void EventHandler_v1(Event @event);

    public class Event {
        public string Type { get; set; }
        public object Target { get; set; }
        public object CurrentTarget { get; set; }
        public List<object> Path { get; set; } = new List<object>();

        public Event(string type) {
            this.Type = type;
        }

        public virtual Event Clone() {
            return new Event(this.Type);
        }
    }

    public class MouseEvent : Event {
        public float X { get; set; } = 0.0f;
        public float Y { get; set; } = 0.0f;
        public SKPoint Pointer { get; set; } = new SKPoint();
        public float Delta { get; set; } = 0.0f;
        public Keys ModifierKey { get; set; } = Keys.None;
        public MouseButtons Button { get; set; } = MouseButtons.None;
        public MouseEvent(string type) : base(type) { }

        public new MouseEvent Clone() {
            return new MouseEvent(this.Type) { X = this.X, Y = this.Y, Button = this.Button, Delta = this.Delta, ModifierKey = this.ModifierKey, Pointer = this.Pointer };
        }
    }

    public class CanvasTargetChangedEventArgs : EventArgs {
        public ICanvasObject Target { get; set; }

        public CanvasTargetChangedEventArgs(ICanvasObject target) {
            this.Target = target;
        }
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

        private List<object> targets = new List<object>();
        public T Root { get; set; }

        public event EventHandler<CanvasTargetChangedEventArgs> CanvasTargetChanged;
        public event EventHandler<EventArgs> CanvasObjectChanged;

        protected NLog.Logger Logger => NLog.LogManager.GetCurrentClassLogger();

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

        public virtual void OnCanvasTargetChanged(CanvasTargetChangedEventArgs e) {
            this.CanvasTargetChanged?.Invoke(this, e);
        }

        public virtual void OnCanvasObjectChanged(EventArgs e) {
            this.CanvasObjectChanged?.Invoke(this, e);
        }

        private void DispatchMouseMoveEvent(Event @event) {
            var castEvent = @event as MouseEvent;
            var newAllTargets = castEvent.Path;

            var intersection = newAllTargets.Intersect(this.targets);
            var cNew = new HashSet<object>(newAllTargets);
            var cOld = new HashSet<object>(this.targets);

            cNew.ExceptWith(intersection);
            cOld.ExceptWith(intersection);

            //cNew.RemoveRange(0, intersection.Count());
            //cOld.RemoveRange(0, intersection.Count());

            foreach(var target in cOld) {
                var mouseEvent = new MouseEvent("MouseLeave");
                mouseEvent.CurrentTarget = target;
                mouseEvent.Target = newAllTargets.Last();
                mouseEvent.Pointer = castEvent.Pointer;

                //this.Logger.Debug($"{target.GetType()} MouseLeave");

                (target as CanvasObject_v1).OnMouseLeave(mouseEvent);
            }

            foreach (var target in cNew) {
                var mouseEvent = new MouseEvent("MouseEnter");
                mouseEvent.CurrentTarget = target;
                mouseEvent.Target = newAllTargets.Last();
                mouseEvent.Pointer = castEvent.Pointer;

                //this.Logger.Debug($"{target.GetType()} MouseEnter");

                (target as CanvasObject_v1).OnMouseEnter(mouseEvent);
            }


            if (cNew.Count() == 0 & cOld.Count() == 0) {
                if (this.CapturedTarget != null) {
                    var mouseEvent = new MouseEvent("MouseMove");
                    mouseEvent.CurrentTarget = this.CapturedTarget;
                    mouseEvent.Target = this.CapturedTarget;
                    mouseEvent.Pointer = castEvent.Pointer;

                    (this.CapturedTarget as CanvasObject_v1).OnMouseMove(mouseEvent);
                }
                else {
                    foreach (var target in newAllTargets) {
                        var mouseEvent = new MouseEvent("MouseMove");
                        mouseEvent.CurrentTarget = target;
                        mouseEvent.Target = newAllTargets.Last();
                        mouseEvent.Pointer = castEvent.Pointer;

                        //this.Logger.Debug($"{target.GetType().ToString()} MouseMove");

                        (target as CanvasObject_v1).OnMouseMove(mouseEvent);
                    }
                }
            } else {
                var target = newAllTargets.Last() as ICanvasObject;

                this.OnCanvasTargetChanged(new CanvasTargetChangedEventArgs(target));
            }

            


            //if (newAllTargets.Count > this.targets.Count) {
            //    var newAddedTargets = new List<object>(newAllTargets);

            //    newAddedTargets.RemoveRange(0, this.targets.Count);

            //    foreach (var target in newAddedTargets) {
            //        var mouseEvent = new MouseEvent("MouseEnter");
            //        mouseEvent.CurrentTarget = target;
            //        mouseEvent.Target = newAllTargets.Last();
            //        mouseEvent.Pointer = castEvent.Pointer;

            //        //this.Logger.Debug($"{target.GetType()} MouseEnter");

            //        (target as CanvasObject_v1).OnMouseEnter(mouseEvent);
            //    }
            //}
            //else if (newAllTargets.Count < this.targets.Count) {
            //    var newRemovedTarget = new List<object>(this.targets);

            //    newRemovedTarget.RemoveRange(0, newAllTargets.Count);
            //    newRemovedTarget.Reverse();

            //    foreach (var target in newRemovedTarget) {
            //        var mouseEvent = new MouseEvent("MouseLeave");
            //        mouseEvent.CurrentTarget = target;
            //        mouseEvent.Target = newAllTargets.Last();
            //        mouseEvent.Pointer = castEvent.Pointer;

            //        //this.Logger.Debug($"{target.GetType()} MouseLeave");

            //        (target as CanvasObject_v1).OnMouseLeave(mouseEvent);
            //    }
            //}
            //else {
            //    if (this.CapturedTarget != null) {
            //        var mouseEvent = new MouseEvent("MouseMove");
            //        mouseEvent.CurrentTarget = this.CapturedTarget;
            //        mouseEvent.Target = this.CapturedTarget;
            //        mouseEvent.Pointer = castEvent.Pointer;

            //        (this.CapturedTarget as CanvasObject_v1).OnMouseMove(mouseEvent);
            //    } else {
            //        foreach (var target in newAllTargets) {
            //            var mouseEvent = new MouseEvent("MouseMove");
            //            mouseEvent.CurrentTarget = target;
            //            mouseEvent.Target = newAllTargets.Last();
            //            mouseEvent.Pointer = castEvent.Pointer;

            //            //this.Logger.Debug($"{target.GetType().ToString()} MouseMove");

            //            (target as CanvasObject_v1).OnMouseMove(mouseEvent);
            //        }
            //    }
            //}
        }

        private void DispatchMouseButtonRelatedEvent(Event @event) {
            var castEvent = @event as MouseEvent;
            var eventInfo = typeof(CanvasObject_v1).GetField(castEvent.Type, BindingFlags.Instance | BindingFlags.NonPublic);

            if (eventInfo != null) {
                if (this.CapturedTarget != null) {
                    EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(this.CapturedTarget);

                    castEvent.CurrentTarget = this.CapturedTarget;
                    handler?.Invoke(castEvent);
                }
                else {
                    // Capture Phase
                    //foreach(var node in e.Path) {
                    //    EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(node);

                    //    e.CurrentTarget = node;
                    //    handler.Invoke(e);
                    //}

                    // Bubble Phase
                    foreach (var node in castEvent.Path.ToArray().Reverse()) {
                        if (!this._propagate) {
                            break;
                        }

                        EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(node);

                        castEvent.CurrentTarget = node;
                        handler?.Invoke(castEvent);
                    }
                }
            }
        }

        public void DispatchMouseEvent(Event @event) {
            var castEvent = @event as MouseEvent;
            var newAllTargets = this.GetEventTargets(castEvent.Pointer, this.Root);

            castEvent.Path = newAllTargets;

            switch (castEvent.Type) {
                case "MouseMove":
                    this.DispatchMouseMoveEvent(castEvent);
                    break;
                default:
                    this.DispatchMouseButtonRelatedEvent(castEvent);
                    break;
            }

            this.targets = newAllTargets;
        }

        //public virtual void DispatchEvent(Event @event) {
        //    Event e;
        //    FieldInfo eventInfo;

        //    if (@event.GetType() == typeof(MouseEvent)) {
        //        e = ((MouseEvent)@event).Clone();

        //        this.FindMouseEventTarget(ref e, this.Root);
        //        if (e.Path.Count != 0) {
        //            e.Target = e.Path.Last();
        //        }
        //    }
        //    else {
        //        e = @event.Clone();
        //    }

        //    eventInfo = typeof(CanvasObject_v1).GetField(e.Type, BindingFlags.Instance | BindingFlags.NonPublic);

        //    if (eventInfo != null) {
        //        if (this.CapturedTarget != null) {
        //            EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(this.CapturedTarget);

        //            e.CurrentTarget = this.CapturedTarget;
        //            handler?.Invoke(e);
        //        }
        //        else {
        //            // Capture Phase
        //            //foreach(var node in e.Path) {
        //            //    EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(node);

        //            //    e.CurrentTarget = node;
        //            //    handler.Invoke(e);
        //            //}

        //            // Bubble Phase
        //            foreach (var node in e.Path.ToArray().Reverse()) {
        //                if (!this._propagate) {
        //                    break;
        //                }

        //                EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(node);

        //                e.CurrentTarget = node;
        //                handler?.Invoke(e);
        //            }
        //        }
        //    }
        //}

        //protected void FindMouseEventTarget(ref Event @event, ICanvasObject node) {
        //    var eventRef = @event as MouseEvent;
        //    var pointer = eventRef.Pointer;
        //    var lPointer = node.Transform.InvGlobalTransformation.MapPoint(pointer);

        //    eventRef.CurrentTarget = node;

        //    if (node.ContainsPoint(lPointer)) {
        //        eventRef.Path.Add(node);

        //        // Children Reversed Order - Top-most Object
        //        foreach (var childNode in node.Children) {
        //            this.FindMouseEventTarget(ref @event, childNode);
        //        }

        //    }

        //    //eventRef.Target = eventRef.Path.Last();
        //}

        protected List<object> GetEventTargets(SKPoint wPointerPos, ICanvasObject node) {
            var ret = new List<object>();
            var lPointerPos = node.Transform.TransformToLocalPoint(wPointerPos);

            if (node.ContainsPoint(lPointerPos)) {
                ret.Add(node);

                // Children Reversed Order - Top-most Object
                foreach (var childNode in node.Children) {
                    var childRet = this.GetEventTargets(lPointerPos, childNode);

                    ret.AddRange(childRet);
                }
            }

            return ret;
        }
    }
}
