using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace TaskMaker.Node {
    public class Graph {
        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Link> Links { get; set; } = new List<Link>();
        public Link TempLink { get; set; }
        public List<LinkShape> LinksS { get; set; } = new List<LinkShape>();

        public Graph() { }

        public void AddNode(Node node) {
            Nodes.Add(node);
        }

        //public void AddLink(IInput a, IOutput b) {
        //    var link = new Link() { Source = a, Destination = b };

        //    Links.Add(link);
        //    a.Outs.Add(link);
        //    b.Ins.Add(link);
        //}

        public object HitTest(SKPoint p) {
            foreach(var node in Nodes) {
                var hit = node.Shape.HitTest(p);

                if (hit != null)
                    return hit;
            }

            return null;

            //var hit = Nodes.Find(n => n.Shape.HitTest(p) != null);

            //return Nodes.Contains(hit) ? hit.Shape : null;
        }
    }
}
