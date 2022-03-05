using System.Collections.Generic;
using SkiaSharp;
using Numpy;
using TaskMaker.SimplicialMapping;

namespace TaskMaker.Model.ControlUI {
    public class ControlUIUnit : Unit {
        public List<Node<NDarray, NDarray>> Nodes { get; set; }
        public object Complex { get; set; }

        public ControlUIUnit() { }
        public void AddNode(object node) { }
        public void RemoveNode(object node) { }
        public void ModifyNode(object data) { }

        /// <summary>
        /// Build a simplicial complex, according to current nodes
        /// </summary>
        public void Build() { }

    }

    public struct Node<T0, T1> {
        public T0 Data0 { get; set; }
        public T1 Data1 { get; set; }
    }
}

namespace TaskMaker.Model.SimplicialMapping {
    public class VoronoiRegion_Rect {
        public NDarray E0;
        public NDarray E1;
        public Simplex Governor;
    }

    public class VoronoiRegion_Sector {
        public NDarray E0;
        public NDarray E1;
        public Simplex[] Governors;
    }

    public class DelaunayRegion {
        public Simplex Governor;
    }


    // Immutable object
    /// <summary>
    /// Immutable simplex object.
    /// Dim() = [?=2, 3]
    /// </summary>
    public class Simplex {
        public NDarray Basis { get; set; }
        public int Dimension => Basis.shape[1];

        private NDarray _mat_a;
        
        public Simplex(IEnumerable<NDarray> basis) {
            Basis = np.array(basis).T;

            var affineFactor = np.ones(Dimension);

            if (Dimension == 2) {
                _mat_a = Basis;
            }
            else {
                _mat_a = np.vstack(affineFactor, Basis);
            }
        }

        public double[] GetLambdas(NDarray b, bool isZero=false) {
            if (isZero) {
                return np.zeros(Dimension).GetData<double>();
            }
            else {
                NDarray B;

                if (Dimension == 2) {
                    B = b;
                }
                else {
                    B = np.hstack(np.ones(1), b);
                }

                return np.linalg.solve(_mat_a, B).GetData<double>();
            }
        }
    }

    // Immutable object
    public class Complex {

    }
}
