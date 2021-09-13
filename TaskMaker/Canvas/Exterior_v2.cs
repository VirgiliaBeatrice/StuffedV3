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
            this.direction = direction;
            this.invDirection = new SKPoint(1 / direction.X, 1 / direction.Y);
        }

        // https://tavianator.com/2011/ray_box.html
        public (float tmin, float tmax)? Intersect(SKRect rect) {
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
            } else {
                return null;
            }
        }

        private SKPoint[] Clip(SKRect rect) {
            //var t = this.Intersect(rect);
            var t = this.Intersect(rect);
            var rets = new List<SKPoint>();

            if (t != null) {
                if (t.Value.tmin >= 0) {
                    var intersectionPoint = origin + this.direction.Multiply(t.Value.tmin);

                    rets.Add(intersectionPoint);
                }

                if (t.Value.tmax >= 0) {
                    var intersectionPoint = origin + this.direction.Multiply(t.Value.tmax);

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

    public class Ray_v2 : CanvasObject_v2 {
        public GeometryLine LineProperty { get; set; }
        public Entity_v2 BindedEntity { get; set; }
        private SKPoint p0;
        private SKPoint p1;
        private SKPaint rayPaint = new SKPaint() {
            Color = SKColors.Green,
            StrokeWidth = 2,
            IsAntialias = true,
        };

        static public Ray_v2 CreateRay(SKPoint start, SKPoint direction) {
            return new Ray_v2(start, start + direction);
        }

        public Ray_v2(SKPoint p0, SKPoint p1) {
            this.Location = p0;
            this.p0 = p0;
            this.p1 = p1;

            this.LineProperty = new GeometryLine() { V0 = this.p0.ToVector(), V1 = this.p1.ToVector() };
        }

        public GeometryLine ToGeometryLine() {
            return new GeometryLine { V0 = this.p0.ToVector(), V1 = this.p1.ToVector() };
        }

        private void ClipRay(SKRect clipBound) {
            //uint outcodeOut = worldCoordinate.GetPointViewportCode(this.Origin);
            var p0 = this.p0;
            var p1 = this.p1;
            var ray = new GeometryLine() {
                V0 = Vector<float>.Build.Dense(new float[] { p0.X, p0.Y }),
                V1 = Vector<float>.Build.Dense(new float[] { p1.X, p1.Y })
            };
            var ymax = clipBound.Bottom;
            var ymin = clipBound.Top;
            var xmax = clipBound.Right;
            var xmin = clipBound.Left;

            var top = new GeometryLine() {
                V0 = Vector<float>.Build.Dense(new float[] { xmin, ymax }),
                V1 = Vector<float>.Build.Dense(new float[] { xmax, ymax })
            };
            var bottom = new GeometryLine() {
                V0 = Vector<float>.Build.Dense(new float[] { xmin, ymin }),
                V1 = Vector<float>.Build.Dense(new float[] { xmax, ymin })
            };
            var left = new GeometryLine() {
                V0 = Vector<float>.Build.Dense(new float[] { xmin, ymin }),
                V1 = Vector<float>.Build.Dense(new float[] { xmin, ymax })
            };
            var right = new GeometryLine() {
                V0 = Vector<float>.Build.Dense(new float[] { xmax, ymin }),
                V1 = Vector<float>.Build.Dense(new float[] { xmax, ymax })
            };
            var collection = new List<GeometryLine>() {
                top, bottom, left, right
            };

            var factorPairs = new List<Matrix<float>>();

            foreach (var edge in collection) {
                factorPairs.Add(GeometryLine.CheckIsIntersected(ray, edge));
            }

            var intersections = new List<SKPoint>();

            foreach (var pair in factorPairs) {
                var result = pair[0, 0] > 0.0f & pair[1, 0] >= 0.0f & pair[1, 0] <= 1.0f;

                if (result) {
                    var newIntersection = p0.ToVector() + pair[0, 0] * ray.Direction;

                    // TODO: Extension
                    intersections.Add(new SKPoint(newIntersection[0], newIntersection[1]));
                }
            }

            this.p1 = intersections[1];

            //if (intersections.Count == 2) {
            //    this.p1 = intersections[1];

            //    //this._gP0 = worldCoordinate.TransformToDevice(intersections[0]);
            //    //this._gP1 = worldCoordinate.TransformToDevice(intersections[1]);
            //    //this.Logger.Debug("Ray - Origin is outside, but intersected.");
            //}
            //else if (intersections.Count == 1) {
            //    this.p1 = InterpolatingEventArgs
            //    this._gP0 = worldCoordinate.TransformToDevice(this.P0);
            //    this._gP1 = worldCoordinate.TransformToDevice(intersections[0]);
            //    //this.Logger.Debug("Ray - Origin is inside.");
            //}
            //else if (intersections.Count == 0) {
            //    this._gP0 = new SKPoint();
            //    this._gP1 = new SKPoint();
            //    //this.Logger.Debug("Ray - Origin is outside, but not intersected.");
            //}
        }

        public override void Invalidate() {
            this.ClipRay(new SKRect() { Size = new SKSize(600, 600) });
        }

        public override void Draw(SKCanvas sKCanvas) {
            sKCanvas.DrawLine(
                this.p0,
                this.p1,
                this.rayPaint
            );
        }
    }

    public class ExteriorRegion_v2 : CanvasObject_v2 {
        public ExteriorRay Ray0 { get; set; }
        public ExteriorRay Ray1 { get; set; }
        public Simplex_v2 Governor { get; set; }
        public Simplex_v2 ExcludedSimplex { get; set; }

        private Ray_v2 ray0;
        private Ray_v2 ray1;
        private SKPath path;
        private SKPaint fillPaint = new SKPaint {
            IsAntialias = true,
            Color = SkiaHelper.ConvertColorWithAlpha(SKColors.DarkOliveGreen, 0.3f),
            Style = SKPaintStyle.Fill
        };

        private void Clip(SKRect bound) {
            var lt = new SKPoint() {
                X = bound.Left,
                Y = bound.Top
            };
            var rt = new SKPoint() {
                X = bound.Right,
                Y = bound.Top
            };
            var rb = new SKPoint() {
                X = bound.Right,
                Y = bound.Bottom
            };
            var lb = new SKPoint() {
                X = bound.Left,
                Y = bound.Bottom
            };
            var left = new GeometryLine { V0 = lt.ToVector(), V1 = lb.ToVector() };
            var right = new GeometryLine { V0 = rb.ToVector(), V1 = rt.ToVector() };
            var bottom = new GeometryLine { V0 = lb.ToVector(), V1 = rb.ToVector() };
            var top = new GeometryLine { V0 = rt.ToVector(), V1 = lt.ToVector() };
            var cornerSites = new CircularList<SKPoint> {
                lt, rt, rb, lb
            };
            var boxEdges = new CircularList<GeometryLine> {
                left, right, bottom, top
            };

            var intersectionsOfEdge0 = new SortedDictionary<float, SKPoint>();
            var intersectionsOfEdge1 = new SortedDictionary<float, SKPoint>();

            var edge0 = ray0.ToGeometryLine();
            var edge1 = ray1.ToGeometryLine();

            foreach (var node in boxEdges) {
                var result = GeometryLine.CheckIsIntersected(edge0, node.Value);

                if (result[0, 0] >= 0.0f & result[1, 0] <= 1.0f & result[1, 0] >= 0.0f) {
                    intersectionsOfEdge0[result[0, 0]] = new SKPoint {
                        X = result[0, 0] * edge0.Direction[0] + edge0.V0[0],
                        Y = result[0, 0] * edge0.Direction[1] + edge0.V0[1]
                    };
                }
            }

            foreach (var node in boxEdges) {
                var result = GeometryLine.CheckIsIntersected(edge1, node.Value);

                if (result[0, 0] >= 0.0f & result[1, 0] <= 1.0f & result[1, 0] >= 0.0f) {
                    intersectionsOfEdge1[result[0, 0]] = new SKPoint {
                        X = result[0, 0] * edge1.Direction[0] + edge1.V0[0],
                        Y = result[0, 0] * edge1.Direction[1] + edge1.V0[1]
                    };
                }
            }

            // Path
            var pathNodes = new CircularList<SKPoint>();

            // Intersections that edge1 intersects with window box. (Reverse)
            pathNodes.AddRange(intersectionsOfEdge1.Values.ToArray().Reverse());

            // P or P0, P1
            if (edge0.V0 == edge1.V0) {
                pathNodes.Add(edge0.V0.ToSKPoint());
            }
            else {
                pathNodes.Add(edge1.V0.ToSKPoint());
                pathNodes.Add(edge0.V0.ToSKPoint());
            }

            // Intersections that edge0 intersects with window box.
            pathNodes.AddRange(intersectionsOfEdge0.Values.ToArray());

            // Inner sites
            var innerSites = cornerSites.Where(
                site => {
                    var sideOfE0 = GeometryLine.GetSide(edge0, site.Value);
                    var sideOfE1 = GeometryLine.GetSide(edge1, site.Value);

                    return sideOfE0 >= 0 & sideOfE1 <= 0;
                }).ToList().Select(e => e.Value).ToList();

            innerSites.ForEach(site => pathNodes.Add(site));

            var centroid = new SKPoint {
                X = pathNodes.Select(node => node.Value.X).Sum() / pathNodes.Count(),
                Y = pathNodes.Select(node => node.Value.Y).Sum() / pathNodes.Count()
            };

            pathNodes = new CircularList<SKPoint>(pathNodes.OrderBy(
                node => SkiaHelper.GetIncludedAngle(
                    centroid + new SKPoint(1.0f, 0.0f),
                    node.Value,
                    centroid)
                ).Select(node => node.Value));

            // Exclude triangle's vertex
            //if (this.ExcludedTri != null) {
            //    var vertices = this.ExcludedTri.GetVertices();
            //    var targetVertex = vertices.Where(v => v.Point != edge0.P0 & v.Point != edge1.P0).ElementAt(0);
            //    var targetIdx = path.IndexOf(edge0.P0);

            //    path.Insert(targetIdx, targetVertex.Point);
            //}

            // lt -> lb
            pathNodes = this.IteratePath(pathNodes, left);
            // rt -> rb
            pathNodes = this.IteratePath(pathNodes, right);
            // lb -> rb
            pathNodes = this.IteratePath(pathNodes, bottom);
            // lt -> rt
            pathNodes = this.IteratePath(pathNodes, top);

            var nodes = new List<SKPoint>();

            for (int i = 0; i < pathNodes.Count(); ++i) {
                if (i == 0)
                    this.path.MoveTo(pathNodes[i].Value);
                else
                    this.path.LineTo(pathNodes[i].Value);
            }

            this.path.Close();
        }

        private CircularList<SKPoint> IteratePath(CircularList<SKPoint> path, GeometryLine targetLine) {
            var newPath = new CircularList<SKPoint>();

            foreach (var node in path) {
                var s = node.Value;
                var e = node.Next.Value;

                var sideOfS = GeometryLine.GetSide(targetLine, s);
                var sideOfE = GeometryLine.GetSide(targetLine, e);

                var l0 = new GeometryLine {
                    V0 = s.ToVector(),
                    V1 = e.ToVector()
                };
                var result = GeometryLine.CheckIsIntersected(l0, targetLine);
                var i = new SKPoint {
                    X = result[0, 0] * l0.Direction[0] + s.X,
                    Y = result[0, 0] * l0.Direction[1] + s.Y
                };

                if (sideOfS >= 0 & sideOfE >= 0) {
                    // Keep E
                    newPath.Add(e);
                }
                else if (sideOfS > 0 & sideOfE < 0) {
                    // Keep I
                    newPath.Add(i);
                }
                else if (sideOfS < 0 & sideOfE >= 0) {
                    // Keep I and E
                    newPath.Add(i);
                    newPath.Add(e);
                }
            }

            return newPath;
        }

        public override void Draw(SKCanvas sKCanvas) {
            sKCanvas.DrawPath(this.path, this.fillPaint);
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

