﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using SkiaSharp;
using MathNetExtension;
//using NumSharp;
using Numpy;

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
        public List<Entity> Basis;
        public int Dimension => Basis.Count;

        private Matrix<float> A;
        private Matrix<float> InverseA;

        private NDarray _A;

        public SimplexBary(IEnumerable<Entity> basis) {
            Basis = new List<Entity>(basis);
        }

        private void InitializeA() {
            var basisArray = np.array(Basis.Select(b => new float[] { b.Location.X, b.Location.Y }).ToArray()).T;
            var affineFactor = np.ones(Dimension);

            _A = np.vstack(affineFactor, basisArray);

            var vBasis = Basis.Select(b => b.ToVector()).ToList();
            A = Matrix<float>.Build.DenseOfColumnVectors(vBasis);
            A = A.InsertRow(0, Vector<float>.Build.Dense(Dimension, 1.0f));

            InverseA = A.Inverse();
        }

        public float[] GetLambdas(SKPoint p) {
            InitializeA();

            var vP = p.ToVector().ToColumnMatrix();
            vP = vP.InsertRow(0, Vector<float>.Build.Dense(1, 1.0f));

            return A.Solve(vP.Column(0)).ToArray();
        }

        public float[] GetZeroLambdas() {
            return Vector<float>.Build.Dense(Dimension, 0).ToArray();
        }
    }

    public class MultiBary {
        public bool IsSet { get; set; } = false;
        public List<Entity[]> Bases { get; set; }
        public List<Simplex[]> Complexes { get; set; }
        public List<Exterior> Exteriors { get; set; }

        private NDarray _wTensor;
        private int[] _shape;
        private IEnumerator<int[]> _cursor;

        public MultiBary() {
            Bases = new List<Entity[]>();
            Complexes = new List<Simplex[]>();
            Exteriors = new List<Exterior>();
        }

        public void AddBary(Entity[] basis, Simplex[] complex, Exterior exterior) {
            Bases.Add(basis);
            Complexes.Add(complex);
            Exteriors.Add(exterior);

            _shape = Bases.Select(b => b.Length).ToArray();
            _cursor = (IEnumerator<int[]>)GetIndices(_shape).GetEnumerator();
            _wTensor = np.array(_shape);
            
        }

        public static T[] Concat<T>(T[] a, T[] b) {
            var val = new T[a.Length + b.Length];

            a.CopyTo(val, 0);
            b.CopyTo(val, a.Length);

            return val;
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
                        newRet.Add(Concat(va, vb));
                    }
                }

                return newRet.ToArray();
            }
            else {
                return values.ToArray();
            }
        }


        public bool SetTensor(float[] element) {
            var result = _cursor.MoveNext();
            var idx = _cursor.Current;

            if (result) {
                _wTensor[$":,{idx[0]},{idx[1]}"] = element;

                for (var i = 0; i < Bases.Count; ++i) {
                    Bases[i][idx[i]].IsSet = true;
                }

                IsSet = false;
            }
            else {
                _wTensor[$":,{idx[0]},{idx[1]}"] = element;

                for (var i = 0; i < Bases.Count; ++i) {
                    Bases[i][idx[i]].IsSet = true;
                }

                IsSet = true;
                _cursor.Dispose();
            }

            return IsSet;
        }

        public void Calculate(params float[][] lambdas) {
            var b11 = new Entity(new SKPoint(1, 2));
            var b12 = new Entity(new SKPoint(3, 4));
            var b13 = new Entity(new SKPoint(5, 6));
            var basis1 = np.array(new Entity[] { b11, b12, b13 });

            var b21 = new Entity(new SKPoint(11, 12));
            var b22 = new Entity(new SKPoint(13, 14));
            var b23 = new Entity(new SKPoint(15, 16));
            var basis2 = np.array(new Entity[] { b21, b22, b23 });


            var layers = new List<Layer>();
            var shape = layers.Select(l => l.Entities.Count).ToArray();
            var wTensor = np.array(shape);

            var lambda1T = np.array(new float[] { 1, 0, 0 }).transpose();
            var lambda2T = np.array(new float[] { 0, 1, 0 }).transpose();

            var lambdasList = new List<NDarray>();
            NDarray kronProd = null;

            for (var idx = 0; idx < lambdas.Length; ++idx) {
                if (idx == 0) {
                    kronProd = np.array(lambdas[idx]);
                }
                else {
                    kronProd = np.outer(kronProd, np.array(lambdas[idx]));
                }
            }

            kronProd = kronProd.flatten();

            var result = np.dot(kronProd, wTensor);
        }
    }

    public class ComplexBary {
        public bool IsSet { get; set; } = false;
        public List<Entity> Basis { get; private set; }
        public List<Simplex> Complex { get; private set; }
        public Exterior Exterior { get; set; }

        private SimplexBary[] Barys => Complex.Select(s => s.Bary).ToArray();
        private int[] _shape;
        private NDarray _wTensor;
        private int _cursor = 0;

        public ComplexBary(List<Entity> basis, List<Simplex> complex, Exterior exterior) {
            Basis = basis;
            Complex = complex;
            Exterior = exterior;

            _shape = new int[] { Basis.Count };
            _wTensor = np.array(_shape);
        }

        private bool HasNext() {
            return _cursor < _wTensor.shape[1] - 1;
        }

        public void BeginSetting(int dim) {
            _shape = new int[] { dim, Basis.Count };
            _wTensor = np.array(_shape);
        }

        public int GetCurrentCursor() => _cursor;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <returns>Next index if this tensor is not fully set.</returns>
        public int SetTensor2D(float[] element) {
            if (HasNext()) {
                _wTensor[$":,{_cursor}"] = element;
                Basis[_cursor].IsSet = true;

                IsSet = false;
                _cursor++;

                return _cursor;
            }
            else {
                _wTensor[$":,{_cursor}"] = element;
                Basis[_cursor].IsSet = true;

                // Auto reset
                IsSet = true;
                _cursor = 0;
                
                return -1;
            }
        }

        public NDarray Calculate(float[] lambda) {
            if (IsSet) {
                var kronProd = np.array(lambda).flatten();
                var elements = new List<double>();

                for(var idx = 0; idx < _wTensor.shape[0]; ++idx) {
                    elements.Add(np.dot(kronProd, _wTensor[$"{idx},:"]).GetData<double>()[0]);
                }

                return np.array(elements.ToArray());
            }

            return null;
        }



        public Dictionary<Entity, float> GetLambdas(SKPoint p) {
            var results = new Dictionary<Entity, float>();

            Basis.ForEach(b => results.Add(b, 0.0f));

            foreach(var bary in Barys) {
                var basis = bary.Basis;
                var lambdas = bary.GetLambdas(p);

                if (lambdas.Any(e => e < 0.0f)) {
                    lambdas = bary.GetZeroLambdas();
                }

                for(var idx = 0; idx < basis.Count; ++idx) {
                    results[basis[idx]] += lambdas[idx];
                }
            }

            //Console.WriteLine(string.Join(", ", results.Keys.Select(k => results[k])));
            if (!results.All(e => e.Value == 0))
                return results;

            foreach(var r in Exterior.Regions) {
                if (r.GetType() == typeof(VoronoiRegion_Rect)) {
                    var bary = r.GetBary();
                    var basis = bary.Basis;
                    var lambdas = bary.GetLambdas(p);

                    if (!r.Contains(p)) {
                        lambdas = bary.GetZeroLambdas();
                    }

                    for(var idx = 0; idx < basis.Count; ++idx) {
                        results[basis[idx]] += lambdas[idx];
                    }
                }
                else if (r.GetType() == typeof(VoronoiRegion_CircularSector)) {
                    if ((r as VoronoiRegion_CircularSector).IsSingleGovernor) {
                        var bary = r.GetBary();
                        var basis = bary.Basis;
                        var lambdas = bary.GetLambdas(p);

                        if (!r.Contains(p)) {
                            lambdas = bary.GetZeroLambdas();
                        }

                        for (var idx = 0; idx < basis.Count; ++idx) {
                            results[basis[idx]] += lambdas[idx];
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
            return results;
        }

        public float[] Interpolate(SKPoint p) {
            var dict = GetLambdas(p);
            var lambda = Basis.Select(b => dict[b]).ToArray();
            var result = Calculate(lambda);

            return result.GetData<float>();
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
