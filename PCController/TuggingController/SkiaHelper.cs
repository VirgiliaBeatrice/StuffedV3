using MathNet.Numerics.LinearAlgebra;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TuggingController {
    public partial class SkiaHelper {

        /// <summary>
        ///  return a signed included angle of v0 and v1
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <returns></returns>
        public static double GetIncludedAngle(SKPoint v0, SKPoint v1) {
            return Math.Atan2(CrossProduct(v0, v1), DotProduct(v0, v1));
        }
        public static double GetIncludedAngle(SKPoint v0, SKPoint v1, SKPoint origin) {
            return Math.Atan2(CrossProduct(v0 - origin, v1 - origin), DotProduct(v0 - origin, v1 - origin));
        }

        public static int GetSide(ShapeElements.ILine line, SKPoint target) {
            var result = GetIncludedAngle(ToSKPoint(line.Direction), target - ToSKPoint(line.V0));

            if (result == 0) {
                return 0;
            } else if (result > 0.0f) {
                return 1;
            } else {
                return -1;
            }
        }

        // v0 x v1 = |v0||v1|sin(theta)
        public static double CrossProduct(SKPoint v0, SKPoint v1) {
            return v0.X * v1.Y - v0.Y * v1.X;
        }

        // v0 x v1 = |v0||v1|cos(theta)
        public static double DotProduct(SKPoint v0, SKPoint v1) {
            return v0.X * v1.X + v0.Y * v1.Y;
        }

        public struct Line2D : ShapeElements.ILine {

            public SKPoint P0 {
                get => ToSKPoint(this.V0);
                set {
                    this.V0 = ToVector(value);
                }
            }
            public SKPoint P1 {
                get => ToSKPoint(this.V1);
                set {
                    this.V1 = ToVector(value);
                }
            }
            public Vector<float> V0 { get; set; }
            public Vector<float> V1 { get; set; }

            public Vector<float> Direction => V1 - V0;

            public Vector<float> UnitDirection {
                get {
                    var unitDir = new float[] {
                        this.Direction[0] / (float)this.Direction.L2Norm(),
                        this.Direction[1] / (float)this.Direction.L2Norm()
                    };

                    return Vector<float>.Build.Dense(unitDir);
                }
            }

            public Line2D(SKPoint p0, SKPoint p1) {
                this.V0 = ToVector(p0);
                this.V1 = ToVector(p1);
            }
        }
    }
}
