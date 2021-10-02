using MathNet.Numerics.LinearAlgebra;
using MathNetExtension;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskMaker.Geometry {
    /// <summary>
    /// Left-top Coordinates, right > 0, left < 0
    /// </summary>
    public class LineSegment {
        // right > 0, left < 0, on line == 0 
        public static int GetSide(SKPoint o, SKPoint a, SKPoint b) {
            var oa = a - o;
            var ob = b - o;
            var ret = Math.Asin(oa.Cross(ob) / (oa.Length * ob.Length));

            return Math.Sign(ret);
        }

        public static (float, float) Intersect(SKPoint a, SKPoint b, SKPoint c, SKPoint d) {
            var dir0 = b - a;
            var dir1 = d - c;

            // Parellel
            if (dir0.Cross(dir1) == 0) {
                return (float.NaN, float.NaN);
            }

            // p = a + dir0 * t1 = c + dir1 * t2
            // A * T = B
            var A = Matrix<float>.Build.DenseOfRowMajor(2, 2, new float[] { dir0.X, dir1.X, dir0.Y, dir1.Y });
            var B = (c - a).ToVector();
            var T = A.Solve(B);

            return (T[0], T[1]);
        }

        public static (float, float) IntersectWithBox(SKPoint a, SKPoint b, SKRect rect) {
            float t0x, t1x, t0y, t1y;

            var direction = b - a;
            var invDirection = new SKPoint(1 / direction.X, 1 / direction.Y);
            var hDir = new SKPoint(rect.Right - rect.Left, 0);
            var vDir = new SKPoint(0, rect.Bottom - rect.Top);

            //if (direction.Cross(hDir) == 0 | direction.Cross(vDir) == 0)
            //    return (float.PositiveInfinity, float.PositiveInfinity);

            t0x = (rect.Left - a.X) * invDirection.X;
            t1x = (rect.Right - a.X) * invDirection.X;
            t0y = (rect.Top - a.Y) * invDirection.Y;
            t1y = (rect.Bottom - a.Y) * invDirection.Y;

            var txmax = Math.Max(t0x, t1x);
            var tymax = Math.Max(t0y, t1y);
            var txmin = Math.Min(t0x, t1x);
            var tymin = Math.Min(t0y, t1y);

            var tmin = Math.Max(txmin, tymin);
            var tmax = Math.Min(txmax, tymax);


            // 0 <= tmin <= tmax <= 1 has 2 clips
            // tmin <= 0 <= tmax <= 1 has 1 clip

            if (tmin <= tmax) {
                return (tmin, tmax);
            }
            else {
                return (float.NaN, float.NaN);
            }
        }

        public static bool IsPointOnLine(SKPoint p, SKPoint a, SKPoint b, float e = 0.001f) {
            var dpa = (p - a).Length;
            var dpb = (p - b).Length;
            var dab = (b - a).Length;
            var value = dpa + dpb - dab;

            return value > -e & value < e;
        }

        public static double GetIncludedAngle(SKPoint a, SKPoint b) {
            return Math.Atan2(a.Cross(b), a.Dot(b));
        }
        public static double GetIncludedAngle(SKPoint o, SKPoint a, SKPoint b) {
            return GetIncludedAngle(a - o, b - o);
        }

        public SKPoint P0 { get; set; }
        public SKPoint P1 { get; set; }
        public SKPoint Direction => P1 - P0;
    }

    public class BezierCurve {
        public static SKPath GetBezierCurve(List<SKPoint> points, float factor = 0.2f) {
            var path = new SKPath();
            var controls = new List<(SKPoint, SKPoint)>();

            for (int i = 0; i < points.Count; ++i) {
                SKPoint c, c_inv;

                if (i == 0) {
                    c = points[i];
                    c_inv = c;

                    //path.MoveTo(points[i]);
                }
                else if (i == points.Count - 1) {
                    c = points[i];
                    c_inv = c;
                }
                else {
                    var prev = points[i - 1];
                    var next = points[i + 1];
                    var it = points[i];

                    c = it + (next - prev).DivideBy((next - prev).Length).Multiply(factor * (it - next).Length);
                    c_inv = it + (next - prev).DivideBy((next - prev).Length).Multiply(-factor * (it - prev).Length);
                }

                controls.Add((c, c_inv));
            }

            path.MoveTo(points[0]);

            for (int i = 0; i < points.Count - 1; ++i) {
                path.CubicTo(controls[i].Item1, controls[i + 1].Item2, points[i + 1]);
            }

            return path;
        }
    }

}

