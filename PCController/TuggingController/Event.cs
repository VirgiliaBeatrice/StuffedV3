using SkiaSharp;
using System.Collections.Generic;
using System.Windows.Forms;

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
        public float PointerX { get; set; } = 0.0f;
        public float PointerY { get; set; } = 0.0f;
        public SKPoint Pointer { get; set; } = new SKPoint();
        public float Delta { get; set; } = 0.0f;
        public Keys ModifierKey { get; set; } = Keys.None;
        public MouseButtons Button { get; set; } = MouseButtons.None;
        public MouseEvent(string type) : base(type) { }

        public new MouseEvent Clone() {
            return new MouseEvent(this.Type) { PointerX = this.PointerX, PointerY = this.PointerY, Button = this.Button, Delta = this.Delta, ModifierKey = this.ModifierKey, Pointer = this.Pointer };
        }
    }
}
