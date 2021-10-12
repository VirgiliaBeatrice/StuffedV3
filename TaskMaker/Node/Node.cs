using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaker.SimplicialMapping;
using PCController;
using SkiaSharp;
using Numpy;

namespace TaskMaker.Node {
    public interface IOperation {
        bool Invoke();
    }

    public class Flow {
        public List<NodeBase> Nodes { get; set; } = new List<NodeBase>();
        public List<Link> Links { get; set; } = new List<Link>();

        private LinkedList<int>[] _adj;
        private int _count => Nodes.Count;

        public Flow() {
            Nodes.Add(new ExecuteNode());
        }

        public Flow(NodeBase[] nodes) {
            Nodes.AddRange(nodes);
        }

        public void SetAdjacencyList() {
            _adj = new LinkedList<int>[_count];

            for(int i = 0; i < _adj.Length; ++i) {
                _adj[i] = new LinkedList<int>();
            }
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
        public object Payload { get; set; }
        //public virtual List<InputPort> Inputs { get; set; }
        //public virtual List<OutputPort> Outputs { get; set; }

        public virtual List<NodeBase> InputNodes { get; set; }
        public virtual List<NodeBase> OutputNodes { get; set; }
        public INodeShape Shape { get; set; }

        public abstract object Invoke(object data);
    }

    public class ExecuteNode : NodeBase {
        public ExecuteNode() {
            Shape = new ExcuteNodeShape(this);
        }

        public override object Invoke(object data) {
            return true;
        }
    }

    public class LayerObjectNode : NodeBase {
        public Layer Layer { get; }

        public LayerObjectNode(Layer parent) {
            Layer = parent;

            Shape = new LayerNodeShape(this) {
                Label = Layer.Name,
            };
        }

        public override object Invoke(object data) {
            try {
                // Collect data from Inputs
                double[] input;

                if (InputNodes.Count == 0) {
                    input = new double[] { Layer.Controller.Location.X, Layer.Controller.Location.X };
                }
                else {
                    input = (double[])data;
                }

                // Process
                var point = new SKPoint((float)input[0], (float)input[1]);
                Layer.Controller.Location = point;
                var lambda = Layer.GetLambda();

                // Dispatch data to Outputs
                return lambda;
            }
            catch(InvalidCastException) {
                return false;
            }
        }
    }

    public class NLinearMapNode : NodeBase {
        public NLinearMap Map { get; }

        public NLinearMapNode(NLinearMap parent) {
            Map = parent;

            Shape = new MapNodeShape(this);
        }

        public override object Invoke(object data) {
            try {
                // Collect data from Inputs
                var lamdas = InputNodes.Select(i => (double[])i.Payload).ToArray();

                // Process data
                var output = Map.MapTo(lamdas);

                // Dispatch data to Outputs
                Payload = output;

                for(var i = 0; i < OutputNodes.Count; ++i) {
                    OutputNodes[i].Payload = np.array(output).reshape(OutputNodes.Count, output.Length / OutputNodes.Count)[$"{i},:"].GetData<double>();
                }

                return true;
            }
            catch (InvalidCastException) {
                return false;
            }

        }
    }

    public class MotorNode : NodeBase {
        public bool IsConnected => Motor == null;
        public Motor Motor { get; set; }

        public MotorNode() {
            Shape = new MotorNodeShape(this);
        }

        public override object Invoke(object data) {
            try {
                // Collect data from Inputs
                var input = (int)InputNodes[0].From.Payload;

                // Process data
                if (IsConnected)
                    Motor.Value = input;

                return true;
            }
            catch (InvalidCastException) {
                return false;
            }
        }
    }
}
