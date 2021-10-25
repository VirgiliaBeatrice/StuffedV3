using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaker.Node {
    public class Graph {
        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Link> Links { get; set; } = new List<Link>();

        public Graph() { }

        public void AddNode(Node node) {
            Nodes.Add(node);
        }

        public void AddLink(IInput a, IOutput b) {
            var link = new Link() { Source = a, Destination = b };

            Links.Add(link);
            a.Outs.Add(link);
            b.Ins.Add(link);
        }
    }
}
