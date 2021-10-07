using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using SkiaSharp;
using MathNetExtension;
using NumSharp;

namespace TaskMaker.SimplicialMapping {

    /// <summary>
    /// Ax = b, x = A^{-1}b
    /// </summary>
    public class BarycentricCoordinates {
        public List<Vector<float>> Vertices { get; set; } = new List<Vector<float>>();
        public bool IsCompleted => this.Vertices.Count == this.dim ? true : false;
        private int dim = 3; 
        private Matrix<float> A;
        private Matrix<float> InverseA;

        public BarycentricCoordinates(int dimension) {
            this.dim = dimension;
        }

        public void Add(Vector<float> item) {
            if (this.Vertices.Count < this.dim) {
                this.Vertices.Add(item);
                
                if (this.Vertices.Count == this.dim) {
                    this.CalculateA();
                }
            }
        }

        public void AddRange(ICollection<Vector<float>> collection) {
            if (this.Vertices.Count + collection.Count <= this.dim) {
                this.Vertices.AddRange(collection);

                if (this.Vertices.Count == this.dim) {
                    this.CalculateA();
                }
            }
        }

        public void UpdateVertices(ICollection<Vector<float>> collection) {
            this.Vertices.Clear();

            this.AddRange(collection);
        }

        public void CalculateA() {
            if (this.IsCompleted) {
                var mat = Matrix<float>.Build.DenseOfColumnVectors(this.Vertices.ToArray());
                this.A = mat.InsertRow(0, Vector<float>.Build.Dense(this.dim, 1.0f));
            }
        }

        public Vector<float> GetLambdas(Vector<float> point) {
            if (this.IsCompleted) {
                var elements = point.ToList();
                elements.Insert(0, 1.0f);
                var expendedVector = Vector<float>.Build.Dense(elements.ToArray());
                var result = this.A.Solve(expendedVector);

                return result;
            }

            return null;
        }

        public Vector<float> GetB(Vector<float> lambda) {
            if (this.IsCompleted) {
                var result = this.A.Multiply(lambda).ToList();

                result.RemoveAt(0);

                return Vector<float>.Build.Dense(result.ToArray());
            }

            return null;
        }

        public Vector<float> GetLambdasOnlyInterior(Vector<float> point) {
            if (this.IsCompleted) {
                var result = this.GetLambdas(point);

                return result.All(e => e >= 0.0f) ? result : Vector<float>.Build.Dense(this.dim, 0.0f);
            }

            return null;
        }
    }

    public class SimplexBary {
        public List<Entity> Basis { get; set; } = new List<Entity>(3);
        public readonly int Dimension = 3;

        private Matrix<float> A;
        private Matrix<float> InverseA;

        private void InitializeA() {
            if (Basis.Count != 3) {
                Console.WriteLine($"{this}: Basis is not fully set.");
                return;
                //throw new Exception($"{this}: Basis is not fully set.");
            }

            var vBasis = Basis.Select(b => b.ToVector()).ToList();
            A = Matrix<float>.Build.DenseOfColumnVectors(vBasis);
            A.InsertRow(0, Vector<float>.Build.Dense(Dimension, 1.0f));

            InverseA = A.Inverse();
        }

        public void AddBasis(Entity e) {
            Basis.Add(e);
            InitializeA();
        }

        public Vector<float> GetLambdas(SKPoint p) {
            if (Basis.Count != 3)
                throw new Exception($"{this}: Basis is not fully set.");

            var vP = p.ToVector().ToColumnMatrix();
            vP.InsertRow(0, Vector<float>.Build.Dense(1, 1.0f));

            return A.Solve(vP.Column(0));
        }

        public Vector<float> GetZeroLambdas() {
            if (Basis.Count != 3)
                throw new Exception($"{this}: Basis is not fully set.");

            return Vector<float>.Build.Dense(Dimension, 0);
        }
    }

    public class ComplexBary<T> {
        public List<Entity> Basis0 { get; set; } = new List<Entity>();
        public List<Entity> Basis1 { get; set; } = new List<Entity>();
        public List<SimplexBary> Barys { get; set; } = new List<SimplexBary>();
        //public Dictionary<Entity, NDArray> Pairs { get; set; } = new Dictionary<Entity, NDArray>();

        public int Dimension => Basis0.Count;

        private NDArray _tensor;
        private NDArray _lambdas;

        private void InitializeScalars() {
            if (Pairs.Values.Any(e => e == null)) {
                Console.WriteLine($"{this} is not fully config.");
                return;
            }


        }

        private void InitializeVertices() {
            Basis0.ForEach(b => Pairs.Add(b, null));
        }

        public void AddTarget(NDArray target, int[] index) {
            // Confirm shape be [1, ], a.k.a. 1D
            target.reshape(new int[] { 1 });

            _tensor.itemset(index, target);
        }

        

        public Dictionary<Entity, float> GetLambdas(SKPoint p) {
            // When full set
            var results = new Dictionary<Entity, float>();

            foreach(var b in Basis) {
                results.Add(b, 0.0f);
            }

            foreach(var bary in Barys) {
                var basis = bary.Basis;
                var lambdas = bary.GetLambdas(p);

                for(var idx = 0; idx < basis.Count; ++idx) {
                    results[basis[idx]] += lambdas[idx];
                }
            }

            return results;
        }

        public void Interpolate(SKPoint p) {
            var lambdas = GetLambdas(p);
            var list = new List<KeyValuePair<Entity, float>>(lambdas);


            _tensor.PointwiseMultiply()
            lambdas
        }
    }

    public class SimplicalMap {
        public BarycentricCoordinates InputBary { get; set; }
        public BarycentricCoordinates OutputBary { get; set; }
        public List<IVectorizable> Inputs { get; set; } = new List<IVectorizable>();
        public List<IVectorizable> Outputs { get; set; } = new List<IVectorizable>();

        private int _dimension = 3;

        public SimplicalMap() {
            InputBary = new BarycentricCoordinates(_dimension);
            OutputBary = new BarycentricCoordinates(_dimension);
        }

        private void SetInputBary() {
            InputBary.UpdateVertices(Inputs.Select(i => i.ToVector()).ToArray());
        }

        private void SetOutputBary() {
            OutputBary.UpdateVertices(Outputs.Select(o => o.ToVector()).ToArray());
        }

        public void Invalidate() {
            SetInputBary();
            SetOutputBary();
        }

        public void Reset() {
            Inputs.Clear();
            Outputs.Clear();
        }

        public void SetPair(IVectorizable input, IVectorizable output) {
            if (Inputs.Count >= _dimension) {
                throw new Exception("Map is fully configed.");
            }
            else {
                Inputs.Add(input);

                if (output != null)
                    Outputs.Add(output);

                Invalidate();
            }
        }

        public Vector<float> GetLambdas(Vector<float> input) => InputBary.GetLambdas(input);

        public Vector<float> Map(Vector<float> input) => OutputBary.GetB(GetLambdas(input));

        public Vector<float> MapToZero() {
            return Vector<float>.Build.Dense(OutputBary.Vertices.First().Count, 0.0f);
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
