using MathNet.Numerics.LinearAlgebra;
using MathNetExtension;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskMaker {


    public class GeometryLine {
        public Vector<float> V0 { get; set; }
        public Vector<float> V1 { get; set; }
        public Vector<float> Direction {
            get {
                return this.V1 - this.V0;
            }
        }
        public Vector<float> UnitDirection {
            get {
                return this.Direction.Normalize(2.0f);
            }
        }

        public static Matrix<float> CheckIsIntersected(GeometryLine line1, GeometryLine line2) {

            // Line Representation: line = p + a * v
            // Solve p1 + a1 * v1 = p2 + a2 * v2

            Matrix<float> A = Matrix<float>.Build.DenseOfArray(
                new float[,] {
                    { line1.Direction[0], - line2.Direction[0] },
                    { line1.Direction[1], - line2.Direction[1] }});
            Matrix<float> b = Matrix<float>.Build.DenseOfArray(
                new float[,] {
                    { line2.V0[0] - line1.V0[0] },
                    { line2.V0[1] - line1.V0[1] }});
            Matrix<float> x = A.Solve(b);

            //Console.WriteLine(x.ToMatrixString());
            return x;
        }

        public static int GetSide(GeometryLine line, SKPoint target) {
            var result = GetIncludedAngle(line.Direction.ToSKPoint(), target - line.V0.ToSKPoint());

            if (result == 0) {
                return 0;
            }
            else if (result > 0.0f) {
                return 1;
            }
            else {
                return -1;
            }
        }

        public static double GetIncludedAngle(SKPoint v0, SKPoint v1) {
            return Math.Atan2(CrossProduct(v0, v1), DotProduct(v0, v1));
        }
        public static double GetIncludedAngle(SKPoint v0, SKPoint v1, SKPoint origin) {
            return Math.Atan2(CrossProduct(v0 - origin, v1 - origin), DotProduct(v0 - origin, v1 - origin));
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
                get => this.V0.ToSKPoint();
                set {
                    this.V0 = value.ToVector();
                }
            }
            public SKPoint P1 {
                get => this.V1.ToSKPoint();
                set {
                    this.V1 = value.ToVector();
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
                this.V0 = p0.ToVector();
                this.V1 = p1.ToVector();
            }
        }
    }

    public class Ray_v3 : CanvasObject_v2 {
        public SKPoint InvDirection => this.invDirection;
        public SKPoint Direction => this.direction;

        private SKPoint origin;
        private SKPoint direction;
        private SKPoint invDirection;
        private SKPaint rayPaint = new SKPaint() {
            Color = SKColors.Green,
            StrokeWidth = 2,
            IsAntialias = true,
        };

        public Ray_v3(SKPoint origin, SKPoint direction) {
            this.origin = origin;
            this.Location = origin;
            this.direction = direction;
            this.invDirection = new SKPoint(1 / direction.X, 1 / direction.Y);
        }

        // https://tavianator.com/2011/ray_box.html
        public (float tmin, float tmax) Intersect(SKRect rect) {
            float t0x, t1x, t0y, t1y;

            t0x = (rect.Left - origin.X) * invDirection.X;
            t1x = (rect.Right - origin.X) * invDirection.X;
            t0y = (rect.Top - origin.Y) * invDirection.Y;
            t1y = (rect.Bottom - origin.Y) * invDirection.Y;

            var txmax = Math.Max(t0x, t1x);
            var tymax = Math.Max(t0y, t1y);
            var txmin = Math.Min(t0x, t1x);
            var tymin = Math.Min(t0y, t1y);

            var tmin = Math.Max(txmin, tymin);
            var tmax = Math.Min(txmax, tymax);


            if (tmin <= tmax) {
                return (tmin, tmax);
            }
            else {
                return (float.NaN, float.NaN);
            }
        }

        public SKPoint[] Clip(SKRect rect) {
            //var t = this.Intersect(rect);
            var (tmin, tmax) = this.Intersect(rect);
            var rets = new List<SKPoint>();

            if (!float.IsNaN(tmin) & !float.IsNaN(tmax)) {
                if (tmin >= 0) {
                    var intersectionPoint = origin + this.direction.Multiply(tmin);

                    rets.Add(intersectionPoint);
                }

                if (tmax >= 0) {
                    var intersectionPoint = origin + this.direction.Multiply(tmax);

                    rets.Add(intersectionPoint);
                }
            }

            return rets.ToArray();
        }

        public override void Draw(SKCanvas sKCanvas) {
            SKRectI bounds;
            sKCanvas.GetDeviceClipBounds(out bounds);

            Console.WriteLine(this.Intersect(bounds));

            var intersectionPoints = this.Clip(bounds);

            if (intersectionPoints.Length == 1) {
                sKCanvas.DrawLine(origin, intersectionPoints[0], rayPaint);
                sKCanvas.DrawCircle(intersectionPoints[0], 5.0f, rayPaint);
            } else if (intersectionPoints.Length == 2) {
                sKCanvas.DrawLine(intersectionPoints[0], intersectionPoints[1], rayPaint);
                sKCanvas.DrawCircle(intersectionPoints[0], 5.0f, rayPaint);
                sKCanvas.DrawCircle(intersectionPoints[1], 5.0f, rayPaint);
            }

            sKCanvas.DrawCircle(this.origin, 5.0f, rayPaint);
        }
    }

    public class CircularList<T> : IEnumerable<CircularListNode<T>> {
        private List<CircularListNode<T>> collection = new List<CircularListNode<T>>();

        public CircularListNode<T> First => this.collection.First();
        public CircularListNode<T> Last => this.collection.Last();

        public CircularList() { }

        public CircularList(IEnumerable<T> ts) {
            this.AddRange(ts);
        }

        public CircularListNode<T> this[int index] {
            get { return this.collection[index]; }
        }

        public override string ToString() {
            return collection.ToString();
        }

        public int IndexOf(T item) {
            return this.collection.Select(e => e.Value).ToList().IndexOf(item);
        }

        public bool Contains(T item) {
            return this.collection.Select(e => e.Value).Contains(item);
        }

        public CircularList<T> Clone() {
            var clone = new CircularList<T>();

            foreach (var node in this.collection) {
                clone.Add(node.Value);
            }

            return clone;
        }

        public void Insert(int at, T item) {
            if (at < 0 | at > this.collection.Count - 1) {
                throw new Exception();
            }

            CircularListNode<T> newItem = new CircularListNode<T>() {
                Value = item,
            };

            var nextItem = this.collection[at];
            var prevItem = this.collection[at].Prev;

            newItem.Next = nextItem;
            newItem.Prev = prevItem;

            this.collection.Insert(at, newItem);

            nextItem.Prev = newItem;
            prevItem.Next = newItem;
        }

        public void RemoveAt(int at) {
            if (at < 0 | at > this.collection.Count - 1) {
                throw new Exception();
            }

            var targetItem = this.collection[at];
            var prevItem = targetItem.Prev;
            var nextItem = targetItem.Next;

            this.collection.RemoveAt(at);

            prevItem.Next = nextItem;
            nextItem.Prev = prevItem;
        }

        public void AddRange(IEnumerable<T> ts) {
            foreach (var value in ts) {
                this.Add(value);
            }
        }

        public void Add(T item) {
            CircularListNode<T> newItem;

            if (this.collection.Count == 0) {
                newItem = new CircularListNode<T>() {
                    Value = item,
                };

                newItem.Next = newItem;
                newItem.Prev = newItem;
            }
            else {
                newItem = new CircularListNode<T>() {
                    Value = item,
                    Prev = this.collection.Last(),
                    Next = this.collection.First()
                };
                this.collection.Last().Next = newItem;
                this.collection.First().Prev = newItem;
            }

            this.collection.Add(newItem);
        }

        public void Clear() {
            this.collection.Clear();
        }

        public IEnumerator<CircularListNode<T>> GetEnumerator() {
            return this.collection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }

    public class CircularListNode<T> {
        public T Value { get; set; }

        public CircularListNode<T> Prev { get; set; }
        public CircularListNode<T> Next { get; set; }

        public override string ToString() {
            return this.Value.ToString();
        }
    }


}

