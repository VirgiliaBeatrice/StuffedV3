using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaskMaker.View.Engine;
using TaskMaker.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaker.View.Engine.Tests {
    [TestClass()]
    public class EngineTests {
        static public Widget GenerateTree() {
            //var root = new Widget() { Name = "root" };
            //var a = new Widget() { Name = "a" };
            //var b = new Widget() { Name = "b" };
            //var c = new ContainerWidget() { Name = "c" };
            //var d = new ContainerWidget() { Name = "d" };
            //var e = new Widget() { Name = "e" };
            //var f = new Widget() { Name = "f" };
            //var g = new Widget() { Name = "g" };
            //var h = new Widget() { Name = "h" };

            //root.AddChild(a);
            //root.AddChild(b);

            //a.AddChild(c);
            //a.AddChild(e);
            //b.AddChild(d);
            //b.AddChild(f);
            //c.AddChild(h);
            //d.AddChild(g);

            //return root;
            return null;
        }

        [TestMethod()]
        public void TreeElementTest() {
            var root = new TreeElement() { Name = "root" };
            var a = new TreeElement() { Name = "a" };
            var b = new TreeElement() { Name = "b" };
            var c = new TreeElement() { Name = "c" };
            var d = new TreeElement() { Name = "d" };
            var e = new TreeElement() { Name = "e" };
            var f = new TreeElement() { Name = "f" };
            var g = new TreeElement() { Name = "g" };
            var h = new TreeElement() { Name = "h" };

            root.AddChild(a);
            root.AddChild(b);

            a.AddChild(c);
            a.AddChild(e);
            b.AddChild(d);
            b.AddChild(f);
            c.AddChild(h);
            d.AddChild(g);

            Console.Write(root.PrintAllChild());

            var newRoot = root.Clone();

            Console.Write(newRoot.PrintAllChild());

            Engine.DFSUtil(
                root,
                "",
                (node, ctx) => {
                    Console.WriteLine($"PreAction-{node.Name}");
                    Console.WriteLine($"PreAction-Context-{ctx}");

                    return ctx + $"{node.Name}";
                },
                (node, ctx) => {
                    Console.WriteLine($"Action-{node.Name}");
                    Console.WriteLine($"Action-Context-{ctx}");

                    return ctx + $"{node.Name}";
                },
                (node, ctx) => {
                    Console.WriteLine($"PostAction-{node.Name}");
                    Console.WriteLine($"PostAction-Context-{ctx}");

                    return ctx + $"{node.Name}";
                }
            );

            //Assert.();
        }

        [TestMethod()]
        public void DFSUtilTest() {
            var root = GenerateTree();

            //Engine.DFSUtil(root, (widget) => Console.WriteLine($"{widget.Name}"));

            //Assert.();
        }

        [TestMethod()]
        public void BuildTest() {
            var root = GenerateTree();

            //Engine.Build(ref root);

            //Console.WriteLine(root.Data.PrintAllChild());
            //Engine.DFSUtil(root.Data, (render) => Console.WriteLine($"{render.Data.Name}"));


            //Assert.Fail();
        }

        [TestMethod()]
        public void LayoutTest() {
            Assert.Fail();
        }

        [TestMethod()]
        public void PaintTest() {
            Assert.Fail();
        }
    }
}