using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using MathNet.Numerics.LinearAlgebra;
using SkiaSharp;

namespace MathNetExtension {
    public static class MyExtension {
        public static float[] ToArray(this SKPoint p) {
            return new float[] { p.X, p.Y };
        }

        public static float[] ToArray(this Point p) {
            return new float[] { p.X, p.Y };
        }

        public static Vector<float> ToVector(this Point point) {
            return Vector<float>.Build.Dense(new float[] { point.X, point.Y });
        }

        public static Vector<float> ToVector(this SKPoint point) {
            return Vector<float>.Build.Dense(new float[] { point.X, point.Y });
        }

        public static SKPoint ToSKPoint(this Vector<float> vector) {
            return new SKPoint { X = vector[0], Y = vector[1] };
        }

        public static SKPoint ToSKPoint(this Point point) {
            return new SKPoint { X = point.X, Y = point.Y };
        }

        public static Vector<float> Concatenate(this Vector<float> top, Vector<float> bottom) {
            var resultList = new List<float>();

            resultList.AddRange(top);
            resultList.AddRange(bottom);

            return Vector<float>.Build.Dense(resultList.ToArray());
        }

        public static Vector<float> Concatenate(IEnumerable<Vector<float>> vs) {
            var ret = Vector<float>.Build.Dense(0);

            foreach(var v in vs) {
                ret = ret.Concatenate(v);
            }

            return ret;
        }

        public static SKPoint Multiply(this SKPoint point, float factor) {
            return new SKPoint(factor * point.X, factor * point.Y);
        }

        public static SKPoint DivideBy(this SKPoint point, float factor) {
            if (factor == 0)
                throw new DivideByZeroException();

            return new SKPoint(point.X / factor, point.Y / factor);
        }

        public static float Cross(this SKPoint a, SKPoint b) {
            return a.X * b.Y - a.Y * b.X;
        }
        public static float Dot(this SKPoint a, SKPoint b) {
            return a.X * b.X + a.Y * b.Y;
        }

        public static SKColor WithAlpha(this SKColor color, float percentage) {
            return color.WithAlpha((byte)(int)(percentage * 256));
        }

        public static SKPoint GetMid(this SKRect rect) => new SKPoint(rect.MidX, rect.MidY);

        public static Vector<float> Sum(this List<Vector<float>> values) {
            var ret = values[0] * 0;

            foreach (var v in values)
                ret += v;

            return ret;
        }
    }
}
