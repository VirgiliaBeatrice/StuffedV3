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
        public SKPicture Picture => _cachedPicture;

        protected SKPicture _cachedPicture;

    }

    public struct MetaInfo {
        public string Name { get; set; }
    }

    /// <summary>
    /// Widget - code description of UI widget for engine
    /// </summary>
    public class Widget : TreeElement {
        public RenderObject RenderObject { get; set; }

        //public virtual void CreateRenderObject() { }

        public virtual void Build() { }

        public virtual bool Contains(SKPoint p) {
            return false;
        }

        public virtual void OnClick() { }

        public Widget(string name) {
            Name = name;
        }

        public new List<Widget> GetAllChild() => base.GetAllChild().Cast<Widget>().ToList();
    }

    public class RenderWidget<T> : Widget {
        public T State { get; set; }

        public RenderWidget(string name) : base(name) { }

        public RenderWidget(string name, T initState) : base(name) {
            State = initState;
        }

        public void Paint(SKCanvas canvas) {
            canvas.DrawPicture(RenderObject.Picture);
        }
    }

    public class RenderWidget : Widget {
        public RenderWidget(string name) : base(name) { }

        public void Paint(SKCanvas canvas) {
            canvas.DrawPicture(RenderObject.Picture);
        }
    }

    public struct MarginSize {
        public float Top;
        public float Left;
        public float Right;
        public float Bottom;

        static public MarginSize Zero() {
            return new MarginSize {
                Top = 0,
                Left = 0,
                Right = 0,
                Bottom = 0
            };
        }
    }

    public struct PaddingSize {
        public float Top;
        public float Left;
        public float Right;
        public float Bottom;

        static public PaddingSize Zero() {
            return new PaddingSize {
                Top = 0,
                Left = 0,
                Right = 0,
                Bottom = 0
            };
        }
    }

    public abstract class ContainerWidget : Widget {
        // Constraints
        public MarginSize Margin { get; set; }
        public PaddingSize Padding { get; set; }

        public float Width { get; set; }
        public float Height { get; set; }
        public SKMatrix T => _transform;

        protected SKMatrix _transform = SKMatrix.Identity;

        public ContainerWidget(string name) : base(name) { }
        public abstract void SetTransform(float x, float y);

        public void Layout() { }

        public virtual void OnPainting(SKCanvas canvas) { }
        public virtual void OnPainted(SKCanvas canvas) { }
    }

    public class RootContainerWidget : ContainerWidget {
        public RootContainerWidget(string name) : base(name) { }

        public override bool Contains(SKPoint p) {
            return true;
        }

        public override void SetTransform(float x, float y) {
            throw new NotImplementedException();
        }
    }
}
