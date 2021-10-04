using SkiaSharp;
using System;
using System.Collections.Generic;
//using Reparameterization;
using System.Linq;
using MathNetExtension;
using TaskMaker.SimplicialMapping;
using MathNet.Numerics.LinearAlgebra;

namespace TaskMaker {
    public abstract class VoronoiRegion {
        public abstract SKPoint this[int index] { get; }
        public abstract void Draw(SKCanvas sKCanvas);
        //public abstract void Interpolate(SKPoint p);
        public abstract Vector<float> Interpolate(SKPoint p);
        public abstract bool Contains(SKPoint p);
        public abstract Vector<float> GetZeroTargetVector();
        public abstract Vector<float> GetInterpolatedTargetVector(SKPoint p);
    }

    public class VoronoiRegion_Rect : VoronoiRegion {
        public Simplex Governor { get; set; }
        public Entity E0 { get; set; }
        public Entity E1 { get; set; }

        private SKPath rect;
        private SKPaint stroke = new SKPaint() {
            StrokeWidth = 1.0f,
            Color = SKColors.DarkGray,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };
        private SKPaint fill = new SKPaint() {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        public override SKPoint this[int index] {
            get { return this.rect[index]; }
        }
        public override string ToString() {
            return $"VRR - {Governor}";
        }

        public override bool Contains(SKPoint p) {
            return this.rect.Contains(p.X, p.Y);
        }

        public VoronoiRegion_Rect(Entity it, Entity next, Simplex governor) {
            rect = new SKPath();
            Governor = governor;
            E0 = it;
            E1 = next;

            rect.AddPoly(this.GetVerticesFromEdge(E0.Location, E1.Location));

            fill.Shader = SKShader.CreateLinearGradient(
                rect[1],
                rect[2],
                new SKColor[] {
                    SKColors.ForestGreen.WithAlpha(0.2f),
                    SKColors.White.WithAlpha(0.5f)
                },
                SKShaderTileMode.Clamp);

        }

        private SKPoint[] GetVerticesFromEdge(SKPoint a, SKPoint b) {
            var factor = 120.0f;

            var dir = b - a;
            var unitDir = dir.DivideBy(dir.Length);
            // ccw
            var perp = SKMatrix.CreateRotationDegrees(90).MapPoint(unitDir);
            var aP = a + perp.Multiply(factor);
            var bP = b + perp.Multiply(factor);

            return new SKPoint[] { a, b, bP, aP };
        }

        public override Vector<float> Interpolate(SKPoint p) {
            if (Contains(p))
                return Governor.GetInterpolatedTargetVector(p);
            else
                return Governor.GetZeroTargetVector();
        }


        // 2-------------------3
        // |                   |
        // |                   |
        // 1-------------------0

        public override void Draw(SKCanvas sKCanvas) {
            //fill.Shader = SKShader.CreateLinearGradient(
            //            rect[1],
            //            rect[2],
            //            new SKColor[] {
            //                SKColors.ForestGreen.WithAlpha(0.2f),
            //                SKColors.White.WithAlpha(0.5f)
            //            },
            //            SKShaderTileMode.Clamp);

            sKCanvas.DrawPath(rect, fill);
            sKCanvas.DrawPath(rect, stroke);
        }

        public override Vector<float> GetZeroTargetVector() => Governor.GetZeroTargetVector();

        public override Vector<float> GetInterpolatedTargetVector(SKPoint p) => Governor.GetInterpolatedTargetVector(p);
    }

    public class VoronoiRegion_CircularSector : VoronoiRegion {
        public Simplex Governor0 { get; set; }
        public Simplex Governor1 { get; set; }
        public SKPoint Intersection { get; set; }
        public Entity E0 { get; set; }

        private SKPath circularSector;
        private SKPaint stroke = new SKPaint() {
            StrokeWidth = 1.0f,
            Color = SKColors.DarkGray,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };
        private SKPaint fill = new SKPaint() {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public override SKPoint this[int index] {
            get { return this.circularSector[index]; }
        }

        public override string ToString() {
            return $"VRC - {Governor0}, {Governor1}";
        }

        public override bool Contains(SKPoint p) {
            return this.circularSector.Contains(p.X, p.Y);
        }

        public VoronoiRegion_CircularSector(Entity o, SKPoint a, SKPoint i, SKPoint b) {
            circularSector = new SKPath();
            Intersection = i;
            E0 = o;

            circularSector.MoveTo(o.Location);
            circularSector.LineTo(a);
            circularSector.CubicTo(a, i, b);
            circularSector.Close();

            fill.Shader = SKShader.CreateRadialGradient(
                E0.Location,
                (a - E0.Location).Length,
                new SKColor[] {
                    SKColors.ForestGreen.WithAlpha(0.2f),
                    SKColors.White.WithAlpha(0.5f)},
                SKShaderTileMode.Clamp);
        }

        public override Vector<float> Interpolate(SKPoint p) {
            if (Contains(p)) {
                if (Governor1 == null) {
                    return Governor0.GetLambdas_v1(p);
                    //Console.WriteLine(this);
                    //Console.WriteLine(this.Governor0.GetLambdasExterior(p));
                    //Console.WriteLine("---");
                }
                else {
                    var o = this[0];
                    var a = this[1];
                    var b = this[2];

                    var theta0 = Math.Abs(Math.Asin((a - o).Cross(p - o) / ((a - o).Length * (p - o).Length)));
                    var theta1 = Math.Abs(Math.Asin((b - o).Cross(p - o) / ((b - o).Length * (p - o).Length)));
                    var theta = theta0 + theta1;
                    var lambdas0 = Governor0.GetLambdas_v1(p);
                    var lambdas1 = Governor1.GetLambdas_v1(p);

                    //Console.WriteLine(this);
                    //Console.WriteLine(lambdas0);
                    //Console.WriteLine(lambdas1);
                    //Console.WriteLine(lambdas0.Multiply((float)(theta1 / theta)) + lambdas1.Multiply((float)(theta0 / theta)));
                    //Console.WriteLine("---");
                    return lambdas0.Multiply((float)(theta1 / theta)) + lambdas1.Multiply((float)(theta0 / theta));
                }
            }
            else {
                return Governor0.Map.MapToZero();
            }

        }

        public override void Draw(SKCanvas sKCanvas) {
            sKCanvas.DrawPath(circularSector, fill);
            sKCanvas.DrawPath(circularSector, stroke);
        }

        public override Vector<float> GetZeroTargetVector() => Governor0.GetZeroTargetVector();

        public override Vector<float> GetInterpolatedTargetVector(SKPoint p) {
            if (Governor1 == null) {
                return Governor0.GetInterpolatedTargetVector(p);
            }
            else {
                var o = this[0];
                var a = this[1];
                var b = this[2];

                var theta0 = Math.Abs(Math.Asin((a - o).Cross(p - o) / ((a - o).Length * (p - o).Length)));
                var theta1 = Math.Abs(Math.Asin((b - o).Cross(p - o) / ((b - o).Length * (p - o).Length)));
                var theta = theta0 + theta1;
                var target0 = Governor0.GetInterpolatedTargetVector(p);
                var target1 = Governor1.GetInterpolatedTargetVector(p);

                return target0 * (float)(theta1 / theta) + target1 * (float)(theta0 / theta);
            }
        }
    }

    public class VoronoiRegions : List<IVoronoiRegion> { }

    public struct LineSegment {
        public SKPoint P0 { get; set; }
        public SKPoint P1 { get; set; }

        public SKPoint Direction => P1 - P0;
    }

    public interface IVoronoiRegion {
        void Draw(SKCanvas sKCanvas);
    }

    /// <summary>
    /// VoronoiRegion Type0 : two different vertices, no excluded simplex.
    /// </summary>
    public class VoronoiRegion_Type0 : CanvasObject_v2, IVoronoiRegion {
        public ExteriorRay_v3 ExRay0 { get; set; }
        public ExteriorRay_v3 ExRay1 { get; set; }
        public Simplex Triangle { get; set; }

        private SKPaint stroke = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DeepSkyBlue,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public VoronoiRegion_Type0(ExteriorRay_v3 ray0, ExteriorRay_v3 ray1, Simplex triangle) {
            this.ExRay0 = ray0;
            this.ExRay1 = ray1;
            this.Triangle = triangle;
        }

        public override bool ContainsPoint(SKPoint p) {
            var ret0 = Geometry.LineSegment.GetSide(ExRay0.Location, ExRay0.Location + ExRay0.Direction, p) <= 0;
            var ret1 = Geometry.LineSegment.GetSide(ExRay0.Location, ExRay1.Location, p) >= 0;
            var ret2 = Geometry.LineSegment.GetSide(ExRay1.Location, ExRay1.Location + ExRay1.Direction, p) >= 0;

            return ret0 & ret1 & ret2;
        }

        private SKPoint[] GetCorners(SKRect bounds, SKPoint start, SKPoint end) {
            var lt = new SKPoint(bounds.Left, bounds.Top);
            var lb = new SKPoint(bounds.Left, bounds.Bottom);
            var rt = new SKPoint(bounds.Right, bounds.Top);
            var rb = new SKPoint(bounds.Right, bounds.Bottom);

            var ccw = new CircularList<LineSegment> {
                new LineSegment() { P0 = lt, P1 = lb },
                new LineSegment() { P0 = lb, P1 = rb },
                new LineSegment() { P0 = rb, P1 = rt },
                new LineSegment() { P0 = lb, P1 = lt },
            };
            var cw = new CircularList<LineSegment> {
                new LineSegment() {P0 = lt, P1 = rt },
                new LineSegment() {P0 = rt, P1 = rb },
                new LineSegment() {P0 = rb, P1 = lb },
                new LineSegment() {P0 = lb, P1 = lt },
            };

            var it = cw.First;
            var points = new List<SKPoint>();

            var init = it;
            do {
                if (Geometry.LineSegment.IsPointOnLine(start, it.Value.P0, it.Value.P1))
                    break;
                else
                    it = it.Next;
            } while (it != init);

            init = it;
            do {
                if (Geometry.LineSegment.IsPointOnLine(end, it.Value.P0, it.Value.P1))
                    break;
                else {
                    points.Add(it.Value.P1);
                    it = it.Next;
                }
            } while (it != init);

            return points.ToArray();
        }

        private SKPoint[] Measure(SKRect bounds) {
            var (t0min, t0max) = ExRay0.Intersect(bounds);
            var (t1min, t1max) = ExRay1.Intersect(bounds);
            var (tlmin, tlmax) = Geometry.LineSegment.IntersectWithBox(ExRay0.Location, ExRay1.Location, bounds);

            var iPointsExRay0 = new List<SKPoint>();
            var iPointsExRay1 = new List<SKPoint>();
            var iPointsL = new List<SKPoint>();
            var iPoints = new List<SKPoint>();
            var dirL = ExRay1.Location - ExRay0.Location;

            if (!float.IsNaN(t0min) & !float.IsNaN(t0max)) {
                if (t0min >= 0 & t0max >= 0) {
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0max));
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0min));
                }
                else if (t0min < 0 & t0max >= 0) {
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0max));
                    //iPointsExRay0.Add(ExRay0.Location);
                }
                else if (t0min < 0 & t0max < 0) {
                    // Outside
                    Console.WriteLine("ExRay0 Outside");
                    ;
                }
                else {
                    throw new Exception("Unhandled condition.");
                }
            }

            if (!float.IsNaN(tlmin) & !float.IsNaN(tlmax)) {
                if (tlmin < 0.0f & tlmax >= 1.0f) {
                    iPointsL.Add(ExRay0.Location);
                    iPointsL.Add(ExRay1.Location);
                    //iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmin));
                    //iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmax));
                }
                else if (tlmin < 0 & tlmax >= 0 & tlmax <= 1.0f) {
                    iPointsL.Add(ExRay0.Location);
                    iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmax));
                }
                else if (tlmin >= 0 & tlmin <= 1.0f & tlmax >= 0 & tlmax <= 1.0f) {
                    iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmin));
                    iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmax));
                }
                else if (tlmin >= 0 & tlmin <= 1.0f & tlmax > 1.0f) {
                    iPointsL.Add(ExRay0.Location + dirL.Multiply(tlmin));
                    iPointsL.Add(ExRay1.Location);
                }
                else if (tlmin < 0 & tlmax < 0) {
                    Console.WriteLine("Line Segment Outside");
                }
                else if (tlmin > 1.0f & tlmax > 1.0f) {
                    Console.WriteLine("Line Segment Outside");
                }
                else {
                    throw new Exception("Unhandled condition.");
                }
            }

            if (!float.IsNaN(t1min) & !float.IsNaN(t1max)) {
                if (t1min >= 0 & t1max >= 0) {
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1min));
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1max));
                }
                else if (t1min < 0 & t1max >= 0) {
                    //iPointsExRay1.Add(ExRay1.Location);
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1max));
                }
                else if (t1min < 0 & t1max < 0) {
                    // Outside
                    Console.WriteLine("ExRay1 Outside");
                    ;
                }
                else {
                    throw new Exception("Unhandled condition.");
                }
            }

            iPoints.AddRange(iPointsExRay0);
            iPoints.AddRange(iPointsL);
            iPoints.AddRange(iPointsExRay1);


            var lt = new SKPoint(bounds.Left, bounds.Top);
            var lb = new SKPoint(bounds.Left, bounds.Bottom);
            var rt = new SKPoint(bounds.Right, bounds.Top);
            var rb = new SKPoint(bounds.Right, bounds.Bottom);
            // Special case 1: no intersection at all
            if (iPoints.Count == 0) {
                var ret0 = this.ContainsPoint(lt);
                var ret1 = this.ContainsPoint(lb);
                var ret2 = this.ContainsPoint(rb);
                var ret3 = this.ContainsPoint(rt);

                if (ret0 & ret1 & ret2 & ret3)
                    iPoints.AddRange(new SKPoint[] { lt, lb, rb, rt });
            }
            else {
                SKPoint first, last;
                SKPoint[] contains;

                if (iPointsExRay0.Count == 2 & iPointsExRay1.Count == 2 & iPointsL.Count == 0) {
                    first = iPoints[1];
                    last = iPoints[2];
                    contains = this.GetCorners(bounds, first, last);

                    iPoints.InsertRange(2, contains);
                }

                first = iPoints.First();
                last = iPoints.Last();
                contains = this.GetCorners(bounds, last, first);

                iPoints.AddRange(contains);
            }

            return iPoints.ToArray();
        }

        public override void Draw(SKCanvas sKCanvas) {
            var stroke = new SKPaint() { Color = SKColors.DeepSkyBlue, IsAntialias = true, StrokeWidth = 2.0f, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Bevel };
            var fill = new SKPaint() { Color = SKColors.BlueViolet.WithAlpha(0.5f), IsAntialias = true, Style = SKPaintStyle.Fill, };
            var text = new SKPaint() { Color = SKColors.IndianRed, IsAntialias = true, StrokeWidth = 1.0f, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Bevel, TextSize = 18.0f };
            var path = new SKPath();

            SKRectI bounds;
            sKCanvas.GetDeviceClipBounds(out bounds);

            try {
                SKPoint[] points;

                points = this.Measure(bounds);


                var idx = 0;
                foreach (var p in points) {
                    sKCanvas.DrawCircle(p, 10.0f, stroke);
                    sKCanvas.DrawText($"{idx + 1}", p, text);

                    if (idx == 0) {
                        path.MoveTo(p);
                    }
                    else {
                        path.LineTo(p);
                    }

                    ++idx;
                }

                path.Close();

                //if (ExRay0.Location != ExRay1.Location) {
                //    var colors = new SKColor[] {
                //        SKColors.Blue.WithAlpha(0.5f),
                //        SKColors.White.WithAlpha(0.5f)
                //    };
                //    var dir = ExRay1.Location - ExRay0.Location;
                //    var uDir = dir.DivideBy(dir.Length).Multiply(100.0f);
                //    var perp = SKMatrix.CreateRotationDegrees(90).MapPoint(uDir);
                //    var shader = SKShader.CreateLinearGradient(
                //        ExRay0.Location,
                //        ExRay0.Location + perp,
                //        colors,
                //        null,
                //        SKShaderTileMode.Clamp);

                //    fill.Shader = shader;
                //}

                //sKCanvas.DrawPath(path, fill);
            }
            catch {
                ;
            }
            finally {
                //ExRay0.Draw(sKCanvas);
                //sKCanvas.DrawText("Ray0", ExRay0.Location, text);
                //ExRay1.Draw(sKCanvas);
                //sKCanvas.DrawText("Ray1", ExRay1.Location, text);
                stroke.StrokeWidth = 3.0f;
                sKCanvas.DrawLine(ExRay0.Location, ExRay1.Location, stroke);
            }
        }
    }

    /// <summary>
    /// VoronoiRegion Type1 : same vertex, no excluded simplex.
    /// </summary>
    public class VoronoiRegion_Type1 : CanvasObject_v2, IVoronoiRegion {
        public ExteriorRay_v3 ExRay0 { get; set; }
        public ExteriorRay_v3 ExRay1 { get; set; }
        public Simplex Triangle0 { get; set; }
        public Simplex Triangle1 { get; set; }

        private SKPaint stroke = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DeepSkyBlue,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public VoronoiRegion_Type1(ExteriorRay_v3 ray0, ExteriorRay_v3 ray1, Simplex tri0, Simplex tri1) {
            this.ExRay0 = ray0;
            this.ExRay1 = ray1;
            this.Triangle0 = tri0;
            this.Triangle1 = tri1;
        }

        public override bool ContainsPoint(SKPoint p) {
            var ret0 = Geometry.LineSegment.GetSide(ExRay0.Location, ExRay0.Location + ExRay0.Direction, p) <= 0;
            //var ret1 = Geometry.LineSegment.GetSide(ExRay0.Location, ExRay1.Location, p) >= 0;
            var ret2 = Geometry.LineSegment.GetSide(ExRay1.Location, ExRay1.Location + ExRay1.Direction, p) >= 0;

            return ret0 & ret2;
        }

        private SKPoint[] GetCorners(SKRect bounds, SKPoint start, SKPoint end) {
            var lt = new SKPoint(bounds.Left, bounds.Top);
            var lb = new SKPoint(bounds.Left, bounds.Bottom);
            var rt = new SKPoint(bounds.Right, bounds.Top);
            var rb = new SKPoint(bounds.Right, bounds.Bottom);

            var ccw = new CircularList<LineSegment> {
                new LineSegment() { P0 = lt, P1 = lb },
                new LineSegment() { P0 = lb, P1 = rb },
                new LineSegment() { P0 = rb, P1 = rt },
                new LineSegment() { P0 = lb, P1 = lt },
            };
            var cw = new CircularList<LineSegment> {
                new LineSegment() {P0 = lt, P1 = rt },
                new LineSegment() {P0 = rt, P1 = rb },
                new LineSegment() {P0 = rb, P1 = lb },
                new LineSegment() {P0 = lb, P1 = lt },
            };

            var it = cw.First;
            var points = new List<SKPoint>();

            var init = it;
            do {
                if (Geometry.LineSegment.IsPointOnLine(start, it.Value.P0, it.Value.P1))
                    break;
                else
                    it = it.Next;
            } while (it != init);

            init = it;
            do {
                if (Geometry.LineSegment.IsPointOnLine(end, it.Value.P0, it.Value.P1))
                    break;
                else {
                    points.Add(it.Value.P1);
                    it = it.Next;
                }
            } while (it != init);

            return points.ToArray();
        }

        private SKPoint[] Measure(SKRect bounds) {
            var (t0min, t0max) = ExRay0.Intersect(bounds);
            var (t1min, t1max) = ExRay1.Intersect(bounds);

            var iPointsExRay0 = new List<SKPoint>();
            var iPointsExRay1 = new List<SKPoint>();
            var iPoints = new List<SKPoint>();

            if (!float.IsNaN(t0min) & !float.IsNaN(t0max)) {
                if (t0min >= 0 & t0max >= 0) {
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0max));
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0min));
                }
                else if (t0min < 0 & t0max >= 0) {
                    iPointsExRay0.Add(ExRay0.Location + ExRay0.Direction.Multiply(t0max));
                    //iPointsExRay0.Add(ExRay0.Location);
                }
                else if (t0min < 0 & t0max < 0) {
                    // Outside
                    Console.WriteLine("ExRay0 Outside");
                    ;
                }
                else {
                    throw new Exception("Unhandled condition.");
                }
            }

            if (!float.IsNaN(t1min) & !float.IsNaN(t1max)) {
                if (t1min >= 0 & t1max >= 0) {
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1min));
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1max));
                }
                else if (t1min < 0 & t1max >= 0) {
                    //iPointsExRay1.Add(ExRay1.Location);
                    iPointsExRay1.Add(ExRay1.Location + ExRay1.Direction.Multiply(t1max));
                }
                else if (t1min < 0 & t1max < 0) {
                    // Outside
                    Console.WriteLine("ExRay1 Outside");
                    ;
                }
                else {
                    throw new Exception("Unhandled condition.");
                }
            }

            iPoints.AddRange(iPointsExRay0);
            //iPoints.Add(ExRay0.Location);
            iPoints.AddRange(iPointsExRay1);

            var lt = new SKPoint(bounds.Left, bounds.Top);
            var lb = new SKPoint(bounds.Left, bounds.Bottom);
            var rt = new SKPoint(bounds.Right, bounds.Top);
            var rb = new SKPoint(bounds.Right, bounds.Bottom);
            // Special case 1: no intersection at all
            if (iPoints.Count == 0) {
                var ret0 = this.ContainsPoint(lt);
                var ret1 = this.ContainsPoint(lb);
                var ret2 = this.ContainsPoint(rt);
                var ret3 = this.ContainsPoint(rb);

                if (ret0 & ret1 & ret2 & ret3)
                    iPoints.AddRange(new SKPoint[] { lt, lb, rt, rb });
            }
            else {
                SKPoint first, last;
                SKPoint[] contains;

                if (iPointsExRay0.Count == 2 & iPointsExRay1.Count == 2) {
                    first = iPoints[1];
                    last = iPoints[2];
                    contains = this.GetCorners(bounds, first, last);

                    iPoints.InsertRange(2, contains);
                }

                first = iPoints.First();
                last = iPoints.Last();
                contains = this.GetCorners(bounds, last, first);

                iPoints.AddRange(contains);
            }

            if (iPointsExRay0.Count == 1) {
                iPoints.Insert(1, ExRay0.Location);
            }

            return iPoints.ToArray();
        }

        public override void Draw(SKCanvas sKCanvas) {
            var stroke = new SKPaint() { Color = SKColors.DeepSkyBlue, IsAntialias = true, StrokeWidth = 2.0f, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Bevel };
            var fill = new SKPaint() { Color = SKColors.BlueViolet.WithAlpha(0.5f), IsAntialias = true, Style = SKPaintStyle.Fill, };
            var text = new SKPaint() { Color = SKColors.IndianRed, IsAntialias = true, StrokeWidth = 1.0f, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Bevel, TextSize = 18.0f };
            var path = new SKPath();

            SKRectI bounds;
            sKCanvas.GetDeviceClipBounds(out bounds);

            try {
                SKPoint[] points;

                points = this.Measure(bounds);

                var idx = 0;
                foreach (var p in points) {
                    sKCanvas.DrawCircle(p, 10.0f, stroke);
                    sKCanvas.DrawText($"{idx + 1}", p, text);

                    if (idx == 0) {
                        path.MoveTo(p);
                    }
                    else {
                        path.LineTo(p);
                    }

                    ++idx;
                }

                path.Close();

                //if (ExRay0.Location != ExRay1.Location) {
                //    var colors = new SKColor[] {
                //        SKColors.Blue.WithAlpha(0.5f),
                //        SKColors.White.WithAlpha(0.5f)
                //    };
                //    var dir = ExRay1.Location - ExRay0.Location;
                //    var uDir = dir.DivideBy(dir.Length).Multiply(20.0f);
                //    var perp = SKMatrix.CreateRotationDegrees(90).MapPoint(uDir);
                //    var shader = SKShader.CreateLinearGradient(
                //        ExRay0.Location,
                //        ExRay0.Location + perp,
                //        colors,
                //        null,
                //        SKShaderTileMode.Clamp);

                //    fill.Shader = shader;
                //}

                //sKCanvas.DrawPath(path, fill);
            }
            catch {
                ;
            }
            finally {
                //ExRay0.Draw(sKCanvas);
                //sKCanvas.DrawText("Ray0", ExRay0.Location, text);
                //ExRay1.Draw(sKCanvas);
                //sKCanvas.DrawText("Ray1", ExRay1.Location, text);
                stroke.StrokeWidth = 3.0f;
                sKCanvas.DrawLine(ExRay0.Location, ExRay1.Location, stroke);
            }
        }
    }

    /// <summary>
    /// VoronoiRegion Type1 : same vertex, no excluded simplex.
    /// </summary>
    public class VoronoiRegion_Type2 : VoronoiRegion_Type0 {

        public VoronoiRegion_Type2(ExteriorRay_v3 ray0, ExteriorRay_v3 ray1, Simplex triangle) : base(ray0, ray1, triangle) { }

        public override bool ContainsPoint(SKPoint p) {
            return base.ContainsPoint(p) & !this.Triangle.ContainsPoint(p);
        }
    }

}

