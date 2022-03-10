using System.Collections.Generic;
using SkiaSharp;
using Numpy;
using TaskMaker.Model.SimplicialMapping;
using System.Linq;

namespace TaskMaker.Model.ControlUI {
    public class ControlUIUnit : Unit {
        public List<Node> Nodes { get; set; } = new List<Node>();
        public Complex Complex { get; set; } = null;

        private object _cache;

        /// <summary>
        /// Constructor for ControlUI unit
        /// </summary>
        public ControlUIUnit() { }
        public void AddNode(Node node) { }
        /// <summary>
        /// Remove a node from current node tree
        /// </summary>
        public void RemoveNode(Node node) { }
        //public void ModifyNode(Node data) { }

        /// <summary>
        /// Build a simplicial complex, according to current nodes
        /// </summary>
        public void Build() { }


    }

    public class Node {
        public NDarray Data0 {
            get => _data0;
            set {
                IsDirty = true;
                _data0 = value;
            }
        }
        public NDarray Data1 {
            get => _data1;
            set {
                IsDirty = true;
                _data1 = value;
            }
        }

        private NDarray _data0;
        private NDarray _data1;

        public bool IsDirty { get; set; } = true;
    }
}

