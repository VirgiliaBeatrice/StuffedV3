using System;
using System.Collections.Generic;
using System.Linq;
using TaskMaker.Utilities;

namespace TaskMaker.View.Engine {
    /// <summary>
    /// Widget - code description of UI widget for engine
    /// </summary>
    public class Widget {
        public RenderObject RenderObject { get; set; }

        public object State { get; set; }
        public bool IsVisited { get; set; }
    }

    public class ContainerWidget : Widget {
        // Constraints

        public ContainerWidget() { }
    }

    public class RenderObject {
        public MetaInfo MetaInfo { get; set; } = new MetaInfo();
    }

    public class NullRenderObject {

    }

    public struct MetaInfo {
        public string Name { get; set; }
    }

    public class Engine {
        public TreeElement WidgetTree { get; set; }

        //static public void DFSUtil(TreeElement curr, object ctx, Action<TreeElement, object> action) {
        //    // Do something
        //    action(curr, ctx);

        //    // Recursively do
        //    foreach (var node in curr.GetAllChild()) {
        //        DFSUtil(node, ctx, action);
        //    }
        //}
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

        //static public List<T> DFSUtil<T>(TreeElement<T> curr, Func<TreeElement<T>, T> func) {
        //    // Do something
        //    var ret = new List<T> { func(curr) };

        //    // Recursively do
        //    foreach (var node in curr.GetAllChild()) {
        //        ret.AddRange(DFSUtil(node, func));
        //    }

        //    return ret;
        //}

        //static public void Build(ref Widget root) {
        //    var curr = root;

        //    // Create RenderObject
        //    DFSUtil(curr, (node) => node.Data = new RenderObject { Data = new MetaInfo { Name = "RenderObject " + node.Name } });

        //    // Trim RenderObjectTree 
        //    DFSUtil(curr, (node) => {
        //        if (node.GetType() == typeof(ContainerWidget)) {
        //            foreach (var child in node.GetAllChild()) {
        //                child.Data.AddChild(node.Data);
        //                node.GetAllChild()[0].Data.Parent = node.Data.Parent;
        //            }
        //        }
        //        else {
        //            foreach (var child in node.GetAllChild()) {
        //                child.Data.AddChild(node.Data);
        //            }
        //        }
        //    });
        //}

        //static public void Clone(TreeElement tree) {
        //    DFSUtil(tree,
        //        node => {
        //            var newNode = new TreeElement {
        //                Name = node.Name
        //            };
        //        },
        //        node => {
        //            var newNode = new TreeElement {
        //                Name = node.Name
        //            };

        //            newNode
        //        }
        //    );
        //}

        //static public void Build(ref TreeElement rootWidget) {
        //    // Create RenderObject tree
        //    DFSUtil(rootWidget, (node) => {
        //        var renderObject = new RenderObject {
        //            MetaInfo = new MetaInfo { Name = $"RO-{node.Name}" }
        //        };

        //        (node.Data as Widget).RenderObject = renderObject;
        //    });

        //    // Create RenderObject for all widgets
        //    DFSUtil(rootWidget, 
        //        node => {
        //            var renderObject = new RenderObject {
        //                Name = $"RenderObject[{node.Name}]",
        //                Data = new MetaInfo(),
        //            };
        //        },
        //        (node) => {
        //            var children = node.GetAllChild();

        //            foreach (var child in children) {
        //                node.Data.AddChild(child.Data);
        //            }
        //        });

        //    // Trim RenderObject tree
        //    var rootRenderObject = rootWidget.Data;

        //    DFSUtil(rootRenderObject, (node) => {
        //        if (node.GetType() == typeof(NullRenderObject)) {
        //            node.GetAllChild().ForEach(c => c.Parent = node.Parent);
        //        }
        //    });
        //}

        //public void Build() {
        //    var curr = WidgetTree;

        //    // Create RenderObject
        //    DFSUtil(curr, node => node.Data = new RenderObject { Data = new MetaInfo { Name = "RenderObject " + node.Name } });

        //    // Trim RenderObjectTree 
        //    DFSUtil(curr, (node) => {
        //        if (node.GetType() == typeof(ContainerWidget)) {
        //            node.GetAllChild()[0].Data.Parent = node.Data.Parent;
        //        }
        //    });

        //    //DFSUtil(curr, () => Console.WriteLine("wawawa"));
        //}

        public void Build() {
            
        }

        public void Layout() {

        }

        public void Paint() {

        }
    }
}
