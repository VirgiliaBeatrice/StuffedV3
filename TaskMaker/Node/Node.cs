using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaker.SimplicialMapping;
using PCController;
using SkiaSharp;
using Numpy;
using MathNetExtension;

namespace TaskMaker.Node {
    //public class Flow {
    //    public List<NodeBase> Nodes { get; private set; } = new List<NodeBase>();
    //    public List<Link> Links { get; set; } = new List<Link>();

    //    public LinkedList<int>[] _adj;
    //    private int _count => Nodes.Count;

    //    public List<MotorNode> Motors { get; set; } = new List<MotorNode>();
    //    public List<LayerNode> Layers { get; set; } = new List<LayerNode>();

    //    public SplitNode _split = new SplitNode(); 
    //    public JoinNode _join = new JoinNode();
    //    public NLinearMapNode _map = new NLinearMapNode();

    //    public INode[] GetNodes() {
    //        var ret = new List<INode>();

    //        ret.Add(_split);
    //        ret.Add(_join);
    //        ret.Add(_map);

    //        ret.AddRange(Motors);
    //        ret.AddRange(Layers);

    //        return ret.ToArray();
    //    }

    //    public Flow() {}

    //    public Flow(NodeBase[] nodes) {
    //        Nodes.AddRange(nodes);
    //    }

    //    public void AddNode(NodeBase node) {
    //        node.Parent = this;
    //        Nodes.Add(node);
    //    }

    //    public void RemoveNode(NodeBase node) {
    //        node.Parent = null;
    //        Nodes.Remove(node);
    //    }

    //    public void AddSource(LayerNode node) {
    //        _join.Inputs.Add(node);
    //    }

    //    public void AddSink<T>(T node) {
    //        _split.Outputs.Add(node);
    //    }


    //    public void ConfigMap() {
    //        _join.Inputs.ForEach(l => _map.Map.AddBary(l.Layer.Bary, 2));
    //    }

    //    public void SetAdjacencyList() {
    //        _adj = new LinkedList<int>[_count];

    //        for(int i = 0; i < _adj.Length; ++i) {
    //            _adj[i] = new LinkedList<int>();
    //        }
    //    }

    //    public NodeBase[] GetAdjacencyList(NodeBase node) {
    //        var idx = Nodes.IndexOf(node);

    //        return _adj[idx].Select(e => Nodes[e]).ToArray();
    //    }

    //    /// <summary>
    //    /// Before call this function, <c>SetAdjacencyList</c> must be called.
    //    /// </summary>
    //    /// <param name="a"></param>
    //    /// <param name="b"></param>
    //    public void AddLink(NodeBase a, NodeBase b) {
    //        _adj[Nodes.IndexOf(a)].AddLast(Nodes.IndexOf(b));
    //        //Links.Add(new LinkShape() { P0Ref = a.Shape.Connector0, P1Ref = b.Shape.Connector1 });
    //    }

    //    public int[] BFS(int idx) {
    //        var visited = new bool[_count];

    //        for (var i = 0; i < _count; ++i) {
    //            visited[i] = false;
    //        }

    //        var bfs = new Queue<int>();
    //        var result = new List<int>();

    //        visited[idx] = true;

    //        bfs.Enqueue(idx);

    //        while(bfs.Any()) {
    //            idx = bfs.Dequeue();

    //            result.Add(idx);

    //            var list = _adj[idx];

    //            foreach (var val in list) {
    //                if (!visited[val]) {
    //                    visited[val] = true;

    //                    bfs.Enqueue(val);
    //                }
    //            }
    //        }

    //        return result.ToArray();
    //    }

    //    public NodeBase[] BFSToNodes(NodeBase start) {
    //        return BFS(Nodes.IndexOf(start)).Select(idx => Nodes[idx]).ToArray();
    //    }

    //    static public Flow CreateFlow(NodeBase[] nodes) {
    //        return new Flow(nodes);
    //    }

    //    public void Invoke() {
    //        var root = Nodes[0];

    //        var bfs = BFSToNodes(root);

    //        object data = null;

    //        foreach(var n in bfs) {
    //            data = n.Invoke(data);
    //        }
    //    }
    ////}

    public interface IInput {
        List<IOutput> Out { get; set; }
    }

    public interface IOutput {
        List<IInput> In { get; set; }
    }

    public class PointerNode : IInput {
        public List<IOutput> Out { get; set; }
    }

    public class MotorNode : IOutput {
        public List<IInput> In { get; set; }
    }

    public class MapNode : IInput, IOutput {
        public List<IInput> In { get; set; }
        public List<IOutput> Out { get; set; }
        public NLinearMap Map { get; set; }

        private List<int> _shapes { get; set; }

        public void Initialize() {
            // Check output shape
            _shapes = new List<int>();

            foreach(var o in Out) {
                if (o.GetType() == typeof(MotorNode)) {
                    _shapes.Add(1);
                }
                else if (o.GetType() == typeof(MapNode)) {
                    _shapes.Add(2);
                }
            }
        }

        public void Validate() {

        }
    }

}
