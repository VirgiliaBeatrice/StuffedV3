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

        static public void _Build(TreeElement element) {
            (element.Data as Widget).CreateRenderObject();

            foreach (var child in element.GetAllChild()) {
                _Build(child);
            }
        }

        static public void Build(TreeElement root) {
            _Build(root);
        }

        static public void _Layout(TreeElement element) {
            var widget = (element.Data as Widget);

            if (typeof(ContainerWidget).IsInstanceOfType(widget)) {
                (widget as ContainerWidget).Layout();
            }

            foreach (var child in element.GetAllChild()) {
                _Layout(child);
            }
        }

        static public void Layout(TreeElement root) {
            _Layout(root);
        }

        static public void _Paint(TreeElement element, SKCanvas canvas) {
            var widget = (element.Data as Widget);

            if (typeof(ContainerWidget).IsInstanceOfType(widget))
                (widget as ContainerWidget).OnPainting(canvas);
            else
                (widget as RenderWidget).Paint(canvas);


            foreach (var child in element.GetAllChild()) {
                _Paint(child, canvas);
            }

            if (typeof(ContainerWidget).IsInstanceOfType(widget))
                (widget as ContainerWidget).OnPainted(canvas);
        }

        static public void Paint(TreeElement root, SKCanvas canvas) {
            _Paint(root, canvas);
        }
    }
}
