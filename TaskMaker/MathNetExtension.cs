using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using SkiaSharp;

namespace MathNetExtension {
    public static class MyExtension {
        public static Vector<float> ToVector(this System.Drawing.Point point) {
            return Vector<float>.Build.Dense(new float[] { point.X, point.Y });
        }

        public static Vector<float> ToVector(this SKPoint point) {
            return Vector<float>.Build.Dense(new float[] { point.X, point.Y });
        }

        public static Vector<float> Concatenate(this Vector<float> top, Vector<float> bottom) {
            var resultList = new List<float>();

            resultList.AddRange(top);
            resultList.AddRange(bottom);

            return Vector<float>.Build.Dense(resultList.ToArray());
        }
    }
}
