using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace TaskMaker {
    //public partial class Triangle_v1 : ShapeElements.ITriangle {
    //    public Vector<float> V0 { get => this.P0.PointVector; }
    //    public Vector<float> V1 { get => this.P1.PointVector; }
    //    public Vector<float> V2 { get => this.P2.PointVector; }
    //}

    //public partial class Entity_v1 : ShapeElements.IPoint {
    //    public Vector<float> PointVector { get; set; }
    //}

    // L = V(V0) + T * V(V1 - V0)
    public partial class Line_v1 : ShapeElements.ILine {
        public Vector<float> V0 { get; set; }
        public Vector<float> V1 { get; set; }
        public Vector<float> Direction {
            get {
                return this.V1 - this.V0;
            }
        }
        public float L2Norm {
            get {
                return (float)this.Direction.L2Norm();
            }
        }
        public Vector<float> UnitDirection {
            get {
                return this.Direction.Normalize(2.0f);
            }
        }

        public Line_v1(Vector<float> v0, Vector<float> v1) {
            this.V0 = v0;
            this.V1 = v1;
        }

        public Vector<float> GetNewPointOnLine(float magnitude) {
            return this.V0 + magnitude * this.UnitDirection;
        }

        public static Line_v1 CreateLineFromDirection(Vector<float> v0, Vector<float> direction) {
            return new Line_v1(v0, v0 + direction);
        }
    }

    namespace SkiaExtension {
        public static class SkiaHelper {
            static public SKPoint ToSKPoint(Vector<float> vector) => new SKPoint(vector[0], vector[1]);
            static public Vector<float> ToVector(SKPoint point) => Vector<float>.Build.DenseOfArray(new float[] { point.X, point.Y });
        }
    }

    namespace ShapeElements {
        public interface ILine {
            Vector<float> V0 { get; set; }
            Vector<float> V1 { get; set; }
            Vector<float> Direction { get; }
            Vector<float> UnitDirection { get; }
            //float L2Norm { get; }
        }

        public interface IPoint {
            Vector<float> PointVector { get; set; }
        }

        public interface ITriangle {
            Vector<float> V0 { get; }
            Vector<float> V1 { get; }
            Vector<float> V2 { get;  }
        }
    }
}
