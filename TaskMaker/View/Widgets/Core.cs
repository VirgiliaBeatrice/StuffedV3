using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using TaskMaker.Utilities;

namespace TaskMaker.View.Widgets {
    public class RenderObject {
        public MetaInfo MetaInfo { get; set; } = new MetaInfo();
        public virtual SKPicture Picture { get; }
    }

    public struct MetaInfo {
        public string Name { get; set; }
    }

    /// <summary>
    /// Widget - code description of UI widget for engine
    /// </summary>
    public class Widget {
        public RenderObject RenderObject { get; set; }

        public virtual void CreateRenderObject() { }
    }

    public class RenderWidget : Widget {
        public void Paint(SKCanvas canvas) {
            canvas.DrawPicture(RenderObject.Picture);
        }
    }

    public abstract class ContainerWidget : Widget {
        // Constraints
        public object Margin { get; set; }
        public object Padding { get; set; }

        public float Width { get; set; }
        public float Height { get; set; }

        protected SKMatrix _transform;

        public ContainerWidget() { }
        public abstract void SetTransform(float x, float y);

        public void Layout() { }

        public virtual void OnPainting(SKCanvas canvas) { }
        public virtual void OnPainted(SKCanvas canvas) { }
    }
}
