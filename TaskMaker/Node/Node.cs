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
    public interface IOperation {
        bool Invoke();
    }

    public class Flow {
        public List<NodeBase> Nodes { get; private set; } = new List<NodeBase>();
        public List<Link> Links { get; set; } = new List<Link>();

        public LinkedList<int>[] _adj;
        private int _count => Nodes.Count;

        public List<MotorNode> Motors { get; set; } = new List<MotorNode>();
        public List<LayerNode> Layers { get; set; } = new List<LayerNode>();

        public SplitNode _split = new SplitNode(); 
        public JoinNode _join = new JoinNode();
        public NLinearMapNode _map = new NLinearMapNode();

        public INode[] GetNodes() {
            var ret = new List<INode>();

            ret.Add(_split);
            ret.Add(_join);
            ret.Add(_map);

            ret.AddRange(Motors);
            ret.AddRange(Layers);

            return ret.ToArray();
        }

        public Flow() {}

        public Flow(NodeBase[] nodes) {
            Nodes.AddRange(nodes);
        }

        public void AddNode(NodeBase node) {
            node.Parent = this;
            Nodes.Add(node);
        }

        public void RemoveNode(NodeBase node) {
            node.Parent = null;
            Nodes.Remove(node);
        }

        public void AddSource(LayerNode node) {
            _join.Inputs.Add(node);
        }

        public void AddSink<T>(T node) {
            _split.Outputs.Add(node);
        }


        public void ConfigMap() {
            _join.Inputs.ForEach(l => _map.Map.AddBary(l.Layer.Bary, 2));
        }

        public void SetAdjacencyList() {
            _adj = new LinkedList<int>[_count];

            for(int i = 0; i < _adj.Length; ++i) {
                _adj[i] = new LinkedList<int>();
            }
        }

        public NodeBase[] GetAdjacencyList(NodeBase node) {
            var idx = Nodes.IndexOf(node);

            return _adj[idx].Select(e => Nodes[e]).ToArray();
        }

        /// <summary>
        /// Before call this function, <c>SetAdjacencyList</c> must be called.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public void AddLink(NodeBase a, NodeBase b) {
            _adj[Nodes.IndexOf(a)].AddLast(Nodes.IndexOf(b));
            //Links.Add(new LinkShape() { P0Ref = a.Shape.Connector0, P1Ref = b.Shape.Connector1 });
        }

        public int[] BFS(int idx) {
            var visited = new bool[_count];

            for (var i = 0; i < _count; ++i) {
                visited[i] = false;
            }

            var bfs = new Queue<int>();
            var result = new List<int>();

            visited[idx] = true;

            bfs.Enqueue(idx);

            while(bfs.Any()) {
                idx = bfs.Dequeue();

                result.Add(idx);

                var list = _adj[idx];

                foreach (var val in list) {
                    if (!visited[val]) {
                        visited[val] = true;

                        bfs.Enqueue(val);
                    }
                }
            }

            return result.ToArray();
        }

        public NodeBase[] BFSToNodes(NodeBase start) {
            return BFS(Nodes.IndexOf(start)).Select(idx => Nodes[idx]).ToArray();
        }

        static public Flow CreateFlow(NodeBase[] nodes) {
            return new Flow(nodes);
        }

        public void Invoke() {
            var root = Nodes[0];

            var bfs = BFSToNodes(root);

            object data = null;

            foreach(var n in bfs) {
                data = n.Invoke(data);
            }
        }
    }

    public interface INode {
        INodeShape Shape { get; set; }

    }

    public class JoinNode : INode {
        public INodeShape Shape { get; set; }

        public List<LayerNode> Inputs { get; set; }

        public double[][] Outputs { get; private set; }

        public JoinNode() {
            Shape = new JoinNodeShape(this);
            Inputs = new List<LayerNode>();
        }

        public void Process() {
            var output = Inputs.Select(l => l.Layer.Controller.Location.ToArray<double>()).ToArray();

            Outputs = output;
        }
    }

    public class SplitNode : INode {
        public INodeShape Shape { get; set; }

        public List<object> Outputs { get; private set; }

        public SplitNode() {
            Shape = new SplitNodeShape(this);
            Outputs = new List<object>();
        }

        public void Process(double[] data) {
            var idx = 0;

            foreach(var o in Outputs) {
                if (o.GetType() == typeof(LayerNode)) {
                    var partial = data.Skip(idx).Take(2).ToArray();

                    idx += 2;
                    (o as LayerNode).Process(data);
                }
                else if (o.GetType() == typeof(MotorNode)) {
                    var partial = data.Skip(idx).Take(1).ToArray();

                    idx += 1;
                    (o as MotorNode).Process(data);
                }
            }
        }
    }


    public class LayerNode : INode {
        public Layer Layer { get; set; }
        public INodeShape Shape { get; set; }

        public List<JoinNode> Outputs { get; private set; }


        public LayerNode(Layer parent) {
            Layer = parent;
            Shape = new LayerNodeShape(this);
            Shape.Label = Layer.Name;
        }

        public void Process(double[] data) {
            var location = new SKPoint((float)data[0], (float)data[1]);

            Layer.Controller.Location = location;

            if (Outputs.Count != 0) {
                Outputs.ForEach(o => o.Process());
            }
        }
    }

    public class MotorNode : INode {
        public Motor Motor { get; set; }
        public INodeShape Shape { get; set; }


        public MotorNode(Motor parent) {
            Motor = parent;
            Shape = new MotorNodeShape(this);
            Shape.Label = "Motor";
        }

        public void Process(double[] data) {
            var value = (int) data[0];

            Motor.Value = value;
        }
    }

    public class NLinearMapNode : INode {
        public NLinearMap Map { get; set; }
        public INodeShape Shape { get; set; }

        public JoinNode Input { get; set; }
        public SplitNode Output { get; set; }

        public NLinearMapNode(NLinearMap parent) {
            Map = parent;
            Shape = new MapNodeShape(this);
            Shape.Label = "Map";
        }

        public NLinearMapNode() {
            Map = new NLinearMap();
            Shape = new MapNodeShape(this);
            Shape.Label = "Map";
        }

        public void Process(double[][] data) {
            var lambda = Map.MapTo(data);

            Output.Process(lambda);
        }
    }


    //public class Port {
    //    public NodeBase Parent { get; set; }
    //    public string Label { get; set; } = "";
    //}

    //public class InputPort : Port {
    //    public OutputPort From { get; set; }
    //}

    //public class OutputPort : Port {
    //    public InputPort To { get; set; }
    //    public object Payload { get; set; }
    //}

    public abstract class NodeBase : IOperation {
        public Flow Parent { get; set; }
        public object Input { get; set; }
        public object Output { get; set; }
        //public virtual List<InputPort> Inputs { get; set; }
        //public virtual List<OutputPort> Outputs { get; set; }

        public INodeShape Shape { get; set; }

        public abstract object Invoke(object data);

        public bool Invoke() {
            throw new NotImplementedException();
        }
    }

    //public class ExecuteNode : NodeBase {
    //    public ExecuteNode() {
    //        Shape = new ExcuteNodeShape(this);
    //    }

    //    public override object Invoke(object data) {
    //        return true;
    //    }
    //}

    //public class LayerObjectNode : NodeBase {
    //    public Layer Layer { get; }

    //    public LayerObjectNode(Layer parent) {
    //        Layer = parent;

    //        Shape = new LayerNodeShape(this) {
    //            Label = Layer.Name,
    //        };
    //    }

    //    public override object Invoke(object data) {
    //        try {
    //            // Collect data from Inputs
    //            double[] input;
    //            var adjNodes = Parent.GetAdjacencyList(this);

    //            if (adjNodes.Length == 0) {
    //                input = new double[] { Layer.Controller.Location.X, Layer.Controller.Location.X };
    //            }
    //            else {
    //                input = (double[])data;
    //            }

    //            // Process
    //            var point = new SKPoint((float)input[0], (float)input[1]);
    //            Layer.Controller.Location = point;
    //            var lambda = Layer.GetLambda();


    //            Output = lambda;
    //            // Dispatch data to Outputs
    //            return lambda;
    //        }
    //        catch(InvalidCastException) {
    //            return false;
    //        }
    //    }
    //}

    //public class NLinearMapNode : NodeBase {
    //    public NLinearMap Map { get; }

    //    public NLinearMapNode(NLinearMap parent) {
    //        Map = parent;

    //        Shape = new MapNodeShape(this);
    //    }

    //    public override object Invoke(object data) {
    //        throw new NotImplementedException();
    //    }

    //    //public override object Invoke(object data) {
    //    //    try {
    //    //        // Collect data from Inputs
    //    //        var adjNodes = Parent.GetAdjacencyList(this);

    //    //        var lambdas = adjNodes.Select(n => (double[])n.Output).ToArray();
    //    //        //var lamdas = InputNodes.Select(i => (double[])i.Payload).ToArray();

    //    //        // Process data
    //    //        var output = Map.MapTo(lambdas);

    //    //        // Dispatch data to Outputs
    //    //        Payload = output;

    //    //        for(var i = 0; i < OutputNodes.Count; ++i) {
    //    //            OutputNodes[i].Payload = np.array(output).reshape(OutputNodes.Count, output.Length / OutputNodes.Count)[$"{i},:"].GetData<double>();
    //    //        }

    //    //        return true;
    //    //    }
    //    //    catch (InvalidCastException) {
    //    //        return false;
    //    //    }

    //    //}
    //}

    //public class MotorNode : NodeBase {
    //    public bool IsConnected => Motor == null;
    //    public Motor Motor { get; set; }

    //    public MotorNode() {
    //        Shape = new MotorNodeShape(this);
    //    }

    //    public override object Invoke(object data) {
    //        return null;
    //        //try {
    //        //    // Collect data from Inputs
    //        //    //var input = (int)InputNodes[0].From.Payload;

    //        //    // Process data
    //        //    if (IsConnected)
    //        //        Motor.Value = input;

    //        //    return true;
    //        //}
    //        //catch (InvalidCastException) {
    //        //    return false;
    //        //}
    //    }
    //}
}
