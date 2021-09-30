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
        public static Vector<float> ToVector(this Point point) {
            return Vector<float>.Build.Dense(new float[] { point.X, point.Y });
        }

        public static Vector<float> ToVector(this SKPoint point) {
            return Vector<float>.Build.Dense(new float[] { point.X, point.Y });
        }

        public static SKPoint ToSKPoint(this Vector<float> vector) {
            return new SKPoint { X = vector[0], Y = vector[1] };
        }

        public static Vector<float> Concatenate(this Vector<float> top, Vector<float> bottom) {
            var resultList = new List<float>();

            resultList.AddRange(top);
            resultList.AddRange(bottom);

            return Vector<float>.Build.Dense(resultList.ToArray());
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
    }
}
