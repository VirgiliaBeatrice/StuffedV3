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

        private LinkedList<int>[] _adj;
        private int _count => Nodes.Count;

        public Flow(NodeBase[] nodes) {
            Nodes.AddRange(nodes);

            _adj = new LinkedList<int>[_count];

            for (int i = 0; i < _adj.Length; i++) {
                _adj[i] = new LinkedList<int>();
            }
        }

        public void AddLink(NodeBase a, NodeBase b) {
            _adj[Nodes.IndexOf(a)].AddLast(Nodes.IndexOf(b));
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
            
            foreach(var n in bfs) {
                n.Invoke();
            }
        }
    }


    public class Link {
        public NodeBase InputNode { get; set; }
        public NodeBase OutputNode { get; set; }
    }

    public class Port {
        public NodeBase Parent { get; set; }
        public string Label { get; set; } = "";
    }

    public class InputPort : Port {
        public OutputPort From { get; set; }
    }

    public class OutputPort : Port {
        public InputPort To { get; set; }
        public object Payload { get; set; }
    }

    public abstract class NodeBase : IOperation {
        public virtual List<InputPort> Inputs { get; set; }
        public virtual List<OutputPort> Outputs { get; set; }

        public abstract bool Invoke();
    }

    public class LayerObjectNode : NodeBase {
        public Layer Layer { get; set; }

        public override bool Invoke() {
            try {
                // Collect data from Inputs
                double[] input;

                if (Inputs.Count == 0) {
                    input = new double[] { Layer.Controller.Location.X, Layer.Controller.Location.X };
                }
                else {
                 input = ((double[])Inputs[0].From.Payload);
                }

                // Process
                var point = new SKPoint((float)input[0], (float)input[1]);
                Layer.Controller.Location = point;
                var lambda = Layer.GetLambda();

                // Dispatch data to Outputs
                if (Outputs.Count != 0) {
                    Outputs[0].Payload = lambda;
                }

                return true;
            }
            catch(InvalidCastException) {
                return false;
            }
        }
    }

    public class NLinearMapNode : NodeBase {
        public NLinearMap Map { get; set; }

        public override bool Invoke() {
            try {
                // Collect data from Inputs
                var lamdas = Inputs.Select(i => (double[])i.From.Payload).ToArray();

                // Process data
                var output = Map.MapTo(lamdas);

                // Dispatch data to Outputs
                for(var i = 0; i < Outputs.Count; ++i) {
                    Outputs[i].Payload = np.array(output).reshape(Outputs.Count, output.Length / Outputs.Count)[$"{i},:"].GetData<double>();
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

        public override bool Invoke() {
            try {
                // Collect data from Inputs
                var input = (int)Inputs[0].From.Payload;

                // Process data
                Motor.Value = input;

                return true;
            }
            catch (InvalidCastException) {
                return false;
            }
        }
    }
}
