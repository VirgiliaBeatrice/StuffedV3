using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using SkiaSharp;
using MathNetExtension;
//using NumSharp;
using Numpy;
using TaskMaker.Node;

namespace TaskMaker.SimplicialMapping {
    public class SimplexBary {
        public List<Entity> Basis;
        public int Dimension => Basis.Count;

        private NDarray A;

        public SimplexBary(IEnumerable<Entity> basis) {
            Basis = new List<Entity>(basis);
        }

        private void InitializeA() {
            var iter = Basis.Select(b => np.array(b.Location.ToArray()));
            var basis = np.array(iter).T;
            var affineFactor = np.ones(Dimension);

            //A = np.vstack(affineFactor, basis);

            if (Dimension == 2) {
                A = basis;
            }
            else {
                A = np.vstack(affineFactor, basis);
            }
        }

        public double[] GetLambdas(SKPoint p) {
            InitializeA();

            var b = np.array(p.ToArray());
            var B = np.hstack(np.ones(1), b);
            
            if (Dimension == 2) {
                B = b;
            }

            var ret = np.linalg.solve(A, B).GetData<float>();
            return ret.Select(e => Convert.ToDouble(e)).ToArray();
        }

        public double[] GetZeroLambdas() {
            return np.zeros(Dimension).GetData<double>();
        }
    }

    public class ComplexBary {
        public Entity[] Basis { get; set; }
        public Simplex[] Complex { get; set; }
        public Exterior Exterior { get; set; }

        public ComplexBary() { }

        public void AddBary(Entity[] basis, Simplex[] complex, Exterior exterior) {
            Basis = basis;
            Complex = complex;
            Exterior = exterior;
        }


        public double[] GetLambda(SKPoint p) {
            var results = new Dictionary<Entity, double>();

            Basis.ToList().ForEach(b => results.Add(b, 0.0f));

            foreach (var bary in Complex.Select(s => s.Bary)) {
                var sBasis = bary.Basis;
                var lambdas = bary.GetLambdas(p);

                if (lambdas.Any(e => e < 0.0f)) {
                    lambdas = bary.GetZeroLambdas();
                }

                for (var idx = 0; idx < sBasis.Count; ++idx) {
                    results[sBasis[idx]] += lambdas[idx];
                }
            }

            //Console.WriteLine(string.Join(", ", results.Keys.Select(k => results[k])));
            if (!results.All(e => e.Value == 0))
                return Basis.Select(b => results[b]).ToArray();

            foreach (var r in Exterior.Regions) {
                if (r.GetType() == typeof(VoronoiRegion_Rect)) {
                    var bary = r.GetBary();
                    var sBasis = bary.Basis;
                    var lambdas = bary.GetLambdas(p);

                    if (!r.Contains(p)) {
                        lambdas = bary.GetZeroLambdas();
                    }

                    for (var idx = 0; idx < sBasis.Count; ++idx) {
                        results[sBasis[idx]] += lambdas[idx];
                    }
                }
                else if (r.GetType() == typeof(VoronoiRegion_CircularSector)) {
                    if ((r as VoronoiRegion_CircularSector).IsSingleGovernor) {
                        var bary = r.GetBary();
                        var sBasis = bary.Basis;
                        var lambdas = bary.GetLambdas(p);

                        if (!r.Contains(p)) {
                            lambdas = bary.GetZeroLambdas();
                        }

                        for (var idx = 0; idx < sBasis.Count; ++idx) {
                            results[sBasis[idx]] += lambdas[idx];
                        }
                    }
                    else {

                        var (bary0, bary1) = (r as VoronoiRegion_CircularSector).GetBarys();
                        var basis0 = bary0.Basis;
                        var basis1 = bary1.Basis;
                        var lambdas0 = bary0.GetLambdas(p);
                        var lambdas1 = bary1.GetLambdas(p);
                        var (f0, f1) = (r as VoronoiRegion_CircularSector).GetFactors(p);

                        if (!r.Contains(p)) {
                            lambdas0 = bary0.GetZeroLambdas();
                            lambdas1 = bary1.GetZeroLambdas();
                        }

                        for (var idx = 0; idx < basis0.Count; ++idx) {
                            results[basis0[idx]] += f0 * lambdas0[idx];
                        }

                        for (var idx = 0; idx < basis0.Count; ++idx) {
                            results[basis1[idx]] += f1 * lambdas1[idx];
                        }
                    }

                }
            }

            //Console.WriteLine(string.Join(", ", results.Keys.Select(k => results[k])));
            return Basis.Select(b => results[b]).ToArray();
        }
    }


    public class NLinearMap {
        public bool IsSet { get; set; } = false;
        public List<ComplexBary> Barys { get; set; }
        public List<Layer> Layers { get; set; }

        private NDarray _wTensor;
        private int[] _shape;
        private IEnumerator<int[]> _cursor;
        public int Dim => _shape.Skip(1).ToArray().Length;
        public int[] CurrentCursor => _cursor.Current;

        public NLinearMap() {
            Barys = new List<ComplexBary>();
            Layers = new List<Layer>();
        }

        public void AddBary(Layer layer, ComplexBary bary, int dim = 2) {
            Barys.Add(bary);
            Layers.Add(layer);

            _shape = Extensions.Concat(new int[] { dim }, Barys.Select(b => b.Basis.Length).ToArray());
            _wTensor = np.zeros(_shape);
            _cursor = GetIndices(_shape.Skip(1).ToArray()).Cast<int[]>().GetEnumerator();
            _cursor.MoveNext();
        }

        public void Clear() {
            Barys.Clear();
        }

        public static int[][] GetIndices(int[] shape) {
            var values = new List<int[]>();

            for (var i = 0; i < shape[0]; ++i) {
                values.Add(new int[] { i });
            }

            if (shape.Length != 1) {
                var ret = GetIndices(shape.Skip(1).ToArray());
                var newRet = new List<int[]>();

                foreach (var va in values) {
                    foreach (var vb in ret) {
                        newRet.Add(Extensions.Concat(va, vb));
                    }
                }

                return newRet.ToArray();
            }
            else {
                return values.ToArray();
            }
        }

        public bool SetComponent(float[] element = null) {
            if (element == null) {
                // Reset to start
                //_cursor = GetIndices(_shape.Skip(1).ToArray()).Cast<int[]>().GetEnumerator();
                //_cursor.MoveNext();

                var idx = _cursor.Current;
                IsSet = false;

                // Highlight first
                for (var i = 0; i < Dim; ++i) {
                    Barys[i].Basis[idx[i]].IsSelected = true;
                }
            }
            else {
                var idx = _cursor.Current;
                var slice = $":,{string.Join(",", idx.Select(i => i))}";

                _wTensor[slice] = element.Select(e => (double)e).ToArray();

                for (var i = 0; i < Dim; ++i) {
                    Barys[i].Basis[idx[i]].IsSet = true;
                }

                var result = _cursor.MoveNext();
            
                if (result) {
                    var nextIdx = _cursor.Current;
                    IsSet = false;

                    // Highlight next component
                    for (var i = 0; i < Dim; ++i) {
                        Barys[i].Basis[nextIdx[i]].IsSelected = true;
                    }
                }

                IsSet = !result;

                if(!result)
                    _cursor.Dispose();
            }

            return IsSet;
        }

        public NDarray Calculate(double[][] lambdas) {
            if (IsSet) {
                NDarray kronProd = null;

                for(int i = 0; i < lambdas.Length; ++i) {
                    if (i == 0) {
                        kronProd = np.array(lambdas[i]).flatten();
                    }
                    else {
                        kronProd = np.kron(kronProd, np.array(lambdas[i]));
                    }
                }

                var w = np.dot(_wTensor.reshape(_shape[0], -1), kronProd);

                return w;
            }

            return null;
        }

        public double[] MapTo(double[][] lambdas) {
            var result = Calculate(lambdas);

            return result.GetData<double>();
        }
    }

    public interface IVectorizable {
        Vector<float> ToVector();
        //void FromVector(Vector<float> vector);
    }

    public interface IReflectable {
        void FromVector(Vector<float> vector);
    }
}
