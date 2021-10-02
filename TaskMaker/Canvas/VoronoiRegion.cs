using SkiaSharp;
using System;
using System.Collections.Generic;
//using Reparameterization;
using System.Linq;
using MathNetExtension;

namespace TaskMaker {
    public class VoronoiRegion : CanvasObject_v2 {
        public ExteriorRay_v3 ExteriorRay0 { get; set; }
        public ExteriorRay_v3 ExteriorRay1 { get; set; }
        public Entity_v2 ExcludedEntity { get; set; }

        private SKPaint stroke = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DeepSkyBlue,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public VoronoiRegion(ExteriorRay_v3 ray0, ExteriorRay_v3 ray1, Entity_v2 exEntity) {
            this.ExteriorRay0 = ray0;
            this.ExteriorRay1 = ray1;
            this.ExcludedEntity = exEntity;
        }

        public override bool ContainsPoint(SKPoint point) {
            var lines = new List<LineSegment>();

            lines.Add(new LineSegment() {
                P0 = ExteriorRay0.E0.Location + ExteriorRay0.Direction,
                P1 = ExteriorRay0.E0.Location
            });

            if (ExcludedEntity != null) {
                lines.Add(new LineSegment {
                    P0 = ExteriorRay0.E0.Location,
                    P1 = ExcludedEntity.Location
                });
                lines.Add(new LineSegment {
                    P0 = ExcludedEntity.Location,
                    P1 = ExteriorRay1.E0.Location,
                });
            }
            else {
                if (ExteriorRay0.E0 != ExteriorRay1.E0) {
                    lines.Add(new LineSegment {
                        P0 = ExteriorRay0.E0.Location,
                        P1 = ExteriorRay1.E0.Location
                    });
                }
            }

            lines.Add(new LineSegment() {
                P0 = ExteriorRay1.E0.Location,
                P1 = ExteriorRay1.E0.Location + ExteriorRay1.Direction
            });

            var results = new List<int>();
            foreach (var line in lines) {
                var a = line.P1 - line.P0;
                var b = point - line.P0;
                var crossProduct = a.X * b.Y - a.Y * b.X;
                var theta = Math.Asin(crossProduct / (a.Length * b.Length));

                results.Add(Math.Sign(theta));
            }

            // left < 0, right > 0 , in = 0
            if (results.Any(res => res > 0)) {
                return false; // out
            }
            else {
                return true; // on or in
            }
        }

        private int GetSide(SKPoint target) {
            var lines = new List<LineSegment>();

            lines.Add(new LineSegment() {
                P0 = ExteriorRay0.E0.Location + ExteriorRay0.Direction,
                P1 = ExteriorRay0.E0.Location
            });

            if (ExcludedEntity != null) {
                lines.Add(new LineSegment {
                    P0 = ExteriorRay0.E0.Location,
                    P1 = ExcludedEntity.Location
                });
                lines.Add(new LineSegment {
                    P0 = ExcludedEntity.Location,
                    P1 = ExteriorRay1.E0.Location,
                });
            } else {
                if (ExteriorRay0.E0 != ExteriorRay1.E0) {
                    lines.Add(new LineSegment {
                        P0 = ExteriorRay0.E0.Location,
                        P1 = ExteriorRay1.E0.Location
                    });
                }
            }

            lines.Add(new LineSegment() {
                P0 = ExteriorRay1.E0.Location,
                P1 = ExteriorRay1.E0.Location + ExteriorRay1.Direction
            });

            var results = new List<int>();
            foreach(var line in lines) {
                var a = line.P1 - line.P0;
                var b = target - line.P0;
                var crossProduct = a.X * b.Y - a.Y * b.X;
                var theta = Math.Asin(crossProduct / (a.Length * b.Length));

                results.Add(Math.Sign(theta));
            }

            // left < 0, right > 0 , in = 0
            if (results.Any(res => res > 0)) {
                return -1; // out
            } else if (results.All(res => res < 0)) {
                return 1; // in
            } else {
                return 0; // on
            }
        }
        
        private SKPath GetClipPath(SKCanvas sKCanvas) {
            var path = new SKPath();

            sKCanvas.GetDeviceClipBounds(out var bounds);

            // CCW
            var corners = new SKPoint[] {
                new SKPoint(bounds.Left, bounds.Top),
                new SKPoint(bounds.Left, bounds.Bottom),
                new SKPoint(bounds.Right, bounds.Bottom),
                new SKPoint(bounds.Right, bounds.Top),
            };

            path.MoveTo(ExteriorRay0.E0.Location + ExteriorRay0.Direction.Multiply(10));
            path.LineTo(ExteriorRay0.E0.Location);

            if (ExcludedEntity != null)
                path.LineTo(ExcludedEntity.Location);

            if (ExteriorRay0.E0 != ExteriorRay1.E0)
                path.LineTo(ExteriorRay1.E0.Location);

            path.LineTo(ExteriorRay1.E0.Location + ExteriorRay1.Direction.Multiply(10));

            foreach(var corner in corners) {
                if (this.ContainsPoint(corner))
                    path.LineTo(corner);
            }

            path.Close();


            return path;
        }

        public override void Draw(SKCanvas sKCanvas) {
            var path = this.GetClipPath(sKCanvas);

            //sKCanvas.DrawPath(path, stroke);
            sKCanvas.GetDeviceClipBounds(out var bounds);

            var region = new SKRegion(bounds);
            region.SetPath(path);
            sKCanvas.DrawRegion(region, stroke);
            //this.ExteriorRay0.Draw(sKCanvas);

            //if (this.ExcludedEntity != null) {
            //    sKCanvas.DrawLine(this.ExteriorRay0.E0.Location, this.ExcludedEntity.Location, stroke);
            //    sKCanvas.DrawLine(this.ExcludedEntity.Location, this.ExteriorRay1.E0.Location, stroke);
            //} else if (this.ExcludedEntity == null & this.ExteriorRay0.E0 != this.ExteriorRay1.E0) {
            //    sKCanvas.DrawLine(this.ExteriorRay0.E0.Location, this.ExteriorRay1.E0.Location, stroke);
            //}

            //this.ExteriorRay1.Draw(sKCanvas);
        }
    }

    public class VoronoiRegions : List<IVoronoiRegion> { }

    public struct LineSegment {
        public SKPoint P0 { get; set; }
        public SKPoint P1 { get; set; }
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
        public Simplex_v2 Triangle { get; set; }

        private SKPaint stroke = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DeepSkyBlue,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public VoronoiRegion_Type0(ExteriorRay_v3 ray0, ExteriorRay_v3 ray1, Simplex_v2 triangle) {
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

                if (ExRay0.Location != ExRay1.Location) {
                    var colors = new SKColor[] {
                        SKColors.Blue.WithAlpha(0.5f),
                        SKColors.White.WithAlpha(0.5f)
                    };
                    var dir = ExRay1.Location - ExRay0.Location;
                    var uDir = dir.DivideBy(dir.Length).Multiply(100.0f);
                    var perp = SKMatrix.CreateRotationDegrees(90).MapPoint(uDir);
                    var shader = SKShader.CreateLinearGradient(
                        ExRay0.Location,
                        ExRay0.Location + perp,
                        colors,
                        null,
                        SKShaderTileMode.Clamp);

                    fill.Shader = shader;
                }

                sKCanvas.DrawPath(path, fill);
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
        public Simplex_v2 Triangle0 { get; set; }
        public Simplex_v2 Triangle1 { get; set; }

        private SKPaint stroke = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DeepSkyBlue,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        public VoronoiRegion_Type1(ExteriorRay_v3 ray0, ExteriorRay_v3 ray1, Simplex_v2 tri0, Simplex_v2 tri1) {
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

                if (ExRay0.Location != ExRay1.Location) {
                    var colors = new SKColor[] {
                        SKColors.Blue.WithAlpha(0.5f),
                        SKColors.White.WithAlpha(0.5f)
                    };
                    var dir = ExRay1.Location - ExRay0.Location;
                    var uDir = dir.DivideBy(dir.Length).Multiply(20.0f);
                    var perp = SKMatrix.CreateRotationDegrees(90).MapPoint(uDir);
                    var shader = SKShader.CreateLinearGradient(
                        ExRay0.Location,
                        ExRay0.Location + perp,
                        colors,
                        null,
                        SKShaderTileMode.Clamp);

                    fill.Shader = shader;
                }

                sKCanvas.DrawPath(path, fill);
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

        public VoronoiRegion_Type2(ExteriorRay_v3 ray0, ExteriorRay_v3 ray1, Simplex_v2 triangle) : base(ray0, ray1, triangle) { }

        public override bool ContainsPoint(SKPoint p) {
            return base.ContainsPoint(p) & !this.Triangle.ContainsPoint(p);
        }
    }

}

