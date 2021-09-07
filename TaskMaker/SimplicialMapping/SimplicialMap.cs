using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

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

    public class ConfigurationVector {
        public List<object> Elements { get; set; }

    }

    class SimplicialMap {
    }
}
