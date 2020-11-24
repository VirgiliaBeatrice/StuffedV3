using SkiaSharp;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using System.Linq;
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Security.Cryptography;

namespace TuggingController {
    public delegate void EventHandler_v1(Event @event);

    public class Event {
        public string Type { get; set; }
        public object Target { get; set; }
        public object CurrentTarget { get; set; }
        public object AddtionalParameters { get; set; } = null;
        public object Sender { get; set; }
        public EventArgs OriginalEventArgs { get; set; }
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

    public class KeyEvent : Event {
        public Keys KeyCode { get; set; } = Keys.None;
        public KeyEvent(string type) : base(type) { }

    }

    public class CanvasTargetChangedEventArgs : EventArgs {
        public ICanvasObject Target { get; set; }

        public CanvasTargetChangedEventArgs(ICanvasObject target) {
            this.Target = target;
        }
    }

    public class TargetUnlockedEventArgs : EventArgs {
        public ICanvasObject Target { get; set; }

        public TargetUnlockedEventArgs(ICanvasObject target) {
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


        public CanvasObject_v1 CapturedTarget { get; set; } = null;
        public SKPoint Pointer { get; set; } = new SKPoint();

        protected bool _propagate = true;

        private List<object> targets = new List<object>();
        public T Root { get; set; }
        public IScene Scene { get; set; }

        public event EventHandler<CanvasTargetChangedEventArgs> CanvasTargetChanged;
        public event EventHandler<EventArgs> CanvasObjectChanged;
        public event EventHandler<EventArgs> TargetUnlocked;


        protected NLog.Logger Logger => NLog.LogManager.GetCurrentClassLogger();

        public EventDispatcher() { }

        public void Lock(CanvasObject_v1 target) {
            this.CapturedTarget = target;
        }

        public void Unlock() {
            this.CapturedTarget = null;
        }

        public void Capture(CanvasObject_v1 target) {
            this.CapturedTarget = target;
            //this.StopPropagate();
        }

        public void Release() {
            this.CapturedTarget = null;
            //this._propagate = true;
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

        public virtual void OnTargetUnlocked(EventArgs e) {
            this.TargetUnlocked?.Invoke(this, e);
        }

        public void DispatchMouseClickEvent(Event @event) {
            var castEvent = @event as MouseEvent;
            var targets = this.GetSelectableEventTargets(castEvent.Pointer, this.Root);

            if (this.CapturedTarget != null) {
                this.CapturedTarget.OnMouseClick(castEvent);
            }
            else {
                if (targets.Count == 1) {
                    this.CapturedTarget = targets[0] as CanvasObject_v1;

                    this.CapturedTarget.OnMouseClick(castEvent);
                }
                else if (targets.Count > 1) {
                    // Popup context menu
                    var menu = this.Scene.GenerateClickContextMenu(targets, castEvent);

                    menu.Show(castEvent.Sender as Control, (castEvent.OriginalEventArgs as MouseEventArgs).Location);
                }
            }
        }

        public void DispatchMouseDoubleClickEvent(Event @event) {
            var castEvent = @event as MouseEvent;
            var targets = this.GetSelectableEventTargets(castEvent.Pointer, this.Root);

            if (targets.Count == 0) {
                // Popup context menu
                var menu = this.Scene.GenerateDbClickContextMenu();

                menu.Show(castEvent.Sender as Control, (castEvent.OriginalEventArgs as MouseEventArgs).Location);
            }
        }

        public void DispatchMouseMoveEvent(Event @event) {
            var castEvent = @event as MouseEvent;
            var targets = this.GetEventTargets(castEvent.Pointer, this.Root);

            this.Pointer = castEvent.Pointer;

            if (this.CapturedTarget != null) {
                this.CapturedTarget.OnMouseMove(@event);
            }
            else {
                var newAllTargets = castEvent.Path;

                var intersection = newAllTargets.Intersect(this.targets);
                var cNew = new HashSet<object>(newAllTargets);
                var cOld = new HashSet<object>(this.targets);

                cNew.ExceptWith(intersection);
                cOld.ExceptWith(intersection);

                foreach (var target in cOld) {
                    var mouseEvent = new MouseEvent("MouseLeave");
                    mouseEvent.CurrentTarget = target;
                    mouseEvent.Pointer = castEvent.Pointer;

                    //this.Logger.Debug($"{target.GetType()} MouseLeave");

                    (target as CanvasObject_v1).OnMouseLeave(mouseEvent);
                }

                foreach (var target in cNew) {
                    var mouseEvent = new MouseEvent("MouseEnter");
                    mouseEvent.CurrentTarget = target;
                    mouseEvent.Pointer = castEvent.Pointer;

                    //this.Logger.Debug($"{target.GetType()} MouseEnter");

                    (target as CanvasObject_v1).OnMouseEnter(mouseEvent);
                }

                if (cNew.Count() != 0 | cOld.Count() != 0) {
                    // TODO
                    if (newAllTargets.Count != 0) {
                        var target = newAllTargets.Last() as ICanvasObject;

                        this.OnCanvasTargetChanged(new CanvasTargetChangedEventArgs(target));
                    }
                }
            }
        }

        //private void DispatchMouseMoveEvent_Old(Event @event) {
        //    var castEvent = @event as MouseEvent;
        //    var newAllTargets = castEvent.Path;

        //    this.Pointer = castEvent.Pointer;

        //    var intersection = newAllTargets.Intersect(this.targets);
        //    var cNew = new HashSet<object>(newAllTargets);
        //    var cOld = new HashSet<object>(this.targets);

        //    cNew.ExceptWith(intersection);
        //    cOld.ExceptWith(intersection);

        //    //cNew.RemoveRange(0, intersection.Count());
        //    //cOld.RemoveRange(0, intersection.Count());

        //    foreach(var target in cOld) {
        //        var mouseEvent = new MouseEvent("MouseLeave");
        //        mouseEvent.CurrentTarget = target;
        //        //mouseEvent.Target = newAllTargets.Last();
        //        mouseEvent.Pointer = castEvent.Pointer;

        //        //this.Logger.Debug($"{target.GetType()} MouseLeave");

        //        (target as CanvasObject_v1).OnMouseLeave(mouseEvent);
        //    }

        //    foreach (var target in cNew) {
        //        var mouseEvent = new MouseEvent("MouseEnter");
        //        mouseEvent.CurrentTarget = target;
        //        //mouseEvent.Target = newAllTargets.Last();
        //        mouseEvent.Pointer = castEvent.Pointer;

        //        //this.Logger.Debug($"{target.GetType()} MouseEnter");

        //        (target as CanvasObject_v1).OnMouseEnter(mouseEvent);
        //    }


        //    if (cNew.Count() == 0 & cOld.Count() == 0) {
        //        if (this.CapturedTarget != null) {
        //            var mouseEvent = new MouseEvent("MouseMove");
        //            mouseEvent.CurrentTarget = this.CapturedTarget;
        //            mouseEvent.Target = this.CapturedTarget;
        //            mouseEvent.Pointer = castEvent.Pointer;

        //            this.CapturedTarget.OnMouseMove(mouseEvent);
        //        }
        //        else {
        //            foreach (var target in newAllTargets) {
        //                var mouseEvent = new MouseEvent("MouseMove");
        //                mouseEvent.CurrentTarget = target;
        //                //mouseEvent.Target = newAllTargets.Last();
        //                mouseEvent.Pointer = castEvent.Pointer;

        //                //this.Logger.Debug($"{target.GetType().ToString()} MouseMove");

        //                (target as CanvasObject_v1).OnMouseMove(mouseEvent);
        //            }
        //        }
        //    } else {
        //        var target = newAllTargets.Last() as ICanvasObject;

        //        this.OnCanvasTargetChanged(new CanvasTargetChangedEventArgs(target));
        //    }
        //}

        private void DispatchDefaultMouseEvent(Event @event) {
            var castEvent = @event as MouseEvent;
            var eventInfo = typeof(CanvasObject_v1).GetField(castEvent.Type, BindingFlags.Instance | BindingFlags.NonPublic);
            //var targets = this.GetEventTargets(castEvent.Pointer, this.Root);

            if (this.CapturedTarget != null) {
                if (eventInfo != null) {
                    EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(this.CapturedTarget);

                    castEvent.CurrentTarget = this.CapturedTarget;
                    handler?.Invoke(castEvent);
                }
            }
        }

        //private void DispatchMouseButtonRelatedEvent(Event @event) {
        //    var castEvent = @event as MouseEvent;
        //    var eventInfo = typeof(CanvasObject_v1).GetField(castEvent.Type, BindingFlags.Instance | BindingFlags.NonPublic);

        //    if (eventInfo != null) {
        //        if (this.CapturedTarget != null) {
        //            EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(this.CapturedTarget);

        //            castEvent.CurrentTarget = this.CapturedTarget;
        //            handler?.Invoke(castEvent);
        //        }
        //        else {
        //            // Capture Phase
        //            //foreach(var node in e.Path) {
        //            //    EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(node);

        //            //    e.CurrentTarget = node;
        //            //    handler.Invoke(e);
        //            //}

        //            // Bubble Phase
        //            foreach (var node in castEvent.Path.ToArray().Reverse()) {
        //                if (!this._propagate) {
        //                    break;
        //                }

        //                EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(node);

        //                castEvent.CurrentTarget = node;
        //                handler?.Invoke(castEvent);
        //            }
        //        }
        //    }
        //}

        public void DispatchMouseEvent(Event @event) {
            var castEvent = @event as MouseEvent;
            var newAllTargets = this.GetEventTargets(castEvent.Pointer, this.Root);

            castEvent.Path = newAllTargets;

            switch (castEvent.Type) {
                case "MouseMove":
                    this.DispatchMouseMoveEvent(castEvent);
                    break;
                case "MouseClick":
                    this.DispatchMouseClickEvent(castEvent);
                    break;
                case "MouseDoubleClick":
                    this.DispatchMouseDoubleClickEvent(castEvent);
                    break;
                default:
                    this.DispatchDefaultMouseEvent(castEvent);
                    break;
            }

            this.targets = newAllTargets;
        }

        public void DispatchKeyEvent(Event @event) {
            var castEvent = @event as KeyEvent;
            var path = this.GetEventTargets(this.Root);
            var eventInfo = typeof(CanvasObject_v1).GetField(castEvent.Type, BindingFlags.Instance | BindingFlags.NonPublic);

            if (eventInfo != null) {
                // Bubble Phase
                foreach (var node in castEvent.Path.ToArray().Reverse()) {
                    EventHandler_v1 handler = (EventHandler_v1)eventInfo.GetValue(node);

                    castEvent.CurrentTarget = node;
                    handler?.Invoke(castEvent);
                }
            }
        }

        protected List<object> GetSelectableEventTargets(SKPoint wPointerPos, ICanvasObject node) {
            var ret = new List<object>();
            var lPointerPos = node.Transform.TransformToLocalPoint(wPointerPos);

            if (node.ContainsPoint(lPointerPos)) {
                if (node.Selectable) {
                    ret.Add(node);
                }

                // Children Reversed Order - Top-most Object
                foreach (var childNode in node.Children) {
                    var childRet = this.GetSelectableEventTargets(lPointerPos, childNode);

                    ret.AddRange(childRet);
                }
            }

            return ret;
        }

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

        protected List<object> GetEventTargets(ICanvasObject node) {
            var ret = new List<object> {
                node
            };

            // Children Reversed Order - Top-most Object
            foreach (var childNode in node.Children) {
                var childRet = this.GetEventTargets(childNode);

                ret.AddRange(childRet);
            }

            return ret;
        }
    }
}
