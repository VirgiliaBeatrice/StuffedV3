using SkiaSharp;
using System.Collections.Generic;
//using Reparameterization;
using MathNetExtension;
using System.Linq;

namespace TaskMaker {
    public class Exterior {
        public List<VoronoiRegion> Regions { get; set; } = new List<VoronoiRegion>();

        public Exterior() { }

        public void Draw(SKCanvas sKCanvas) {
            this.Regions.ForEach(r => r.Draw(sKCanvas));
        }

        public static Exterior CreateExterior(Entity_v2[] extremes, Simplex_v2[] simplices) {
            var ccw = new CircularList<Entity_v2>(extremes);
            var exterior = new Exterior();
            var rects = new List<VoronoiRegion_Rect>();

            foreach (var node in ccw) {
                var it = node.Value;
                var next = node.Next.Value;
                var governor = simplices.Where(s => s.IsVertex(it) & s.IsVertex(next)).FirstOrDefault();

                rects.Add(new VoronoiRegion_Rect(it, next, governor));
            }

            for (var idx = 0; idx < rects.Count; ++idx) {
                var it = rects[idx];
                var next = rects[(idx + 1) == rects.Count ? 0 : idx + 1];

                exterior.Regions.Add(it);

                if (it[2] != next[3]) {
                    var exLine0 = new LineSegment { P0 = it[3], P1 = it[2] };
                    var exLine1 = new LineSegment { P0 = next[2], P1 = next[3] };
                    var (t0, _) = Geometry.LineSegment.Intersect(exLine0.P0, exLine0.P1, exLine1.P0, exLine1.P1);
                    var i = exLine0.P0 + exLine0.Direction.Multiply(t0);
                    var governor0 = simplices.Where(s => s.IsVertex(it.E1) & s.IsVertex(it.E0)).FirstOrDefault();
                    var governor1 = simplices.Where(s => s.IsVertex(next.E1) & s.IsVertex(next.E0)).FirstOrDefault();
                    var cone = new VoronoiRegion_CircularSector(it.E1, it[2], i, next[3]);
                    if (governor0 != governor1) {
                        cone.Governor0 = governor0;
                        cone.Governor1 = governor1;
                    }
                    else {
                        cone.Governor0 = governor0;
                    }

                    exterior.Regions.Add(cone);
                }
            }

            return exterior;
        }
    }


    public class Bend {
        private SKPath[] zones;
        public List<SKPoint> intersections = new List<SKPoint>();

        private static float Factor = 100.0f;
        private SKPaint stroke = new SKPaint() {
            StrokeWidth = 1.0f,
            Color = SKColors.DarkGray,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        private SKPaint fill = new SKPaint() {
            Style = SKPaintStyle.Fill,
        };
       

        // ccw
        public Bend(SKPath[] zones) {
            this.zones = zones;
        }

        public void Draw(SKCanvas sKCanvas) {
            foreach(var zone in zones) {

                sKCanvas.DrawPath(zone, stroke);

                if (zone.SegmentMasks == SKPathSegmentMask.Line) {
                    fill.Shader = SKShader.CreateLinearGradient(
                        zone[1],
                        zone[2],
                        new SKColor[] {
                            SKColors.ForestGreen.WithAlpha(0.2f),
                            SKColors.White.WithAlpha(0.5f)
                        },
                        SKShaderTileMode.Clamp);
                    
                    sKCanvas.DrawPath(zone, fill);
                }
                else if (zone.SegmentMasks == (SKPathSegmentMask.Line | SKPathSegmentMask.Cubic)) {
                    fill.Shader = SKShader.CreateRadialGradient(
                        zone[0],
                        (zone[1] - zone[0]).Length,
                        new SKColor[] {
                            SKColors.ForestGreen.WithAlpha(0.2f),
                            SKColors.White.WithAlpha(0.5f)
                        },
                        SKShaderTileMode.Clamp);

                    sKCanvas.DrawPath(zone, fill);
                }
            }

            intersections.ForEach(i => sKCanvas.DrawCircle(i, 2.5f, stroke));
        }

        public static Bend GenerateBend(SKPoint[] extremes) {
            var ccw = new CircularList<SKPoint>(extremes);
            var zones = new List<SKPath>();

            foreach(var node in ccw) {
                var it = node.Value;
                var next = node.Next.Value;
                var edge = next - it;
                var perp = SKMatrix.CreateRotationDegrees(90).MapPoint(edge);
                var unitPerp = perp.DivideBy(perp.Length);
                var itPerp = it + unitPerp.Multiply(Factor);
                var nextPerp = next + unitPerp.Multiply(Factor);

                var poly = new SKPath();

                poly.AddPoly(new SKPoint[] { it, next, nextPerp, itPerp }, true);
                zones.Add(poly);
            }

            var modifiedZones = new List<SKPath>();
            var inters = new List<SKPoint>();

            for (var idx = 0; idx < zones.Count; ++idx) {
                var it = zones[idx];
                var next = zones[(idx + 1) == zones.Count ? 0 : idx + 1];

                modifiedZones.Add(it);

                if (it[2] != next[3]) {
                    var cone = new SKPath();
                    var exLine0 = new LineSegment { P0 = it[3], P1 = it[2] };
                    var exLine1 = new LineSegment { P0 = next[2], P1 = next[3] };
                    var (t0, t1) = Geometry.LineSegment.Intersect(exLine0.P0, exLine0.P1, exLine1.P0, exLine1.P1);
                    var i = exLine0.P0 + exLine0.Direction.Multiply(t0);

                    cone.MoveTo(it[1]);
                    cone.LineTo(it[2]);
                    cone.CubicTo(i, i, next[3]);
                    cone.Close();

                    inters.Add(i);
                    
                    modifiedZones.Add(cone);
                }
            }

            return new Bend(modifiedZones.ToArray()) { intersections = inters };
        }
    }
}

