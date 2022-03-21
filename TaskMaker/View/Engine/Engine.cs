using System;
using System.Collections.Generic;
using System.Linq;
using TaskMaker.Utilities;
using SkiaSharp;
using TaskMaker.View.Widgets;

namespace TaskMaker.View {

    public class Engine {
        static private Stack<object> _ctx = new Stack<object>();
        static public void DFSUtil(TreeElement curr, Func<TreeElement, object> preAction, Func<TreeElement, object, object> action, Func<TreeElement, object, object> postAction) {
            // Do something
            object ctx = preAction(curr);

            // Recursively do
            foreach (var node in curr.GetAllChild()) {
                // Push context into _ctx stack before going to next level recursively
                _ctx.Push(ctx);
                DFSUtil(node, preAction, action, postAction);
                ctx = _ctx.Pop();

                ctx = action(node, ctx);
            }

            // Do something
            ctx = postAction(curr, ctx);
            //_ctx.Pop();
        }

        static public void _Build(Widget element) {
            element.Build();

            foreach (var child in element.GetAllChild()) {
                _Build(child);
            }
        }

        static public void Build(Widget root) {
            _Build(root);
        }

        static public void _Layout(Widget widget) {

            if (widget is ContainerWidget) {
                (widget as ContainerWidget).Layout();
            }

            foreach (var child in widget.GetAllChild()) {
                _Layout(child);
            }
        }

        static public void Layout(Widget root) {
            _Layout(root);
        }

        static public void _Paint(Widget widget, SKCanvas canvas) {
            if (widget is ContainerWidget)
                (widget as ContainerWidget).OnPainting(canvas);
            else
                (widget as RenderWidget<NodeWidgetState>).Paint(canvas);


            foreach (var child in widget.GetAllChild()) {
                _Paint(child, canvas);
            }

            if (widget is ContainerWidget)
                (widget as ContainerWidget).OnPainted(canvas);
        }

        static public void Paint(Widget root, SKCanvas canvas) {
            _Paint(root, canvas);
        }
    }
}
