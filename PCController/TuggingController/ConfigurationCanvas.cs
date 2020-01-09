using System;
using System.Numerics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using NLog;


namespace TuggingController {
    public class ConfigurationCanvas {
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public int Mode { get; set; } = 0;
        public SKSize CanvasSize { get; set; }
        // Transformation: Global ==> Local
        public SKMatrix Transform { get; set; }
        public SKMatrix InverseTransform { get; set; }

        public SKPoint[] ControlPoints { get; set; }
        public float Radius { get; set; } = 16;
        public float Threshold { get; set; } = 16;
        public bool IsDragging { get; set; } = false;
        public bool IsDown { get; set; } = false;
        // Scale: Local ==> Value
        public SKMatrix Scale { get; set; }
        public SKMatrix InverseScale { get; set; }

        public ConfigurationCanvas() { }
        public ConfigurationCanvas(float width, float height) {
            this.CanvasSize = new SKSize {
                Width = width,
                Height = height
            };

            this.ControlPoints = new SKPoint[] {
                new SKPoint(this.CanvasSize.Width / 2, 80),
                new SKPoint(this.CanvasSize.Width / 2 +100, 80 + 100),
                new SKPoint(this.CanvasSize.Width / 2, 80 + 400 - 100),
                new SKPoint(this.CanvasSize.Width / 2, 80 + 400)
            };
        }


        public int? IsInControlArea(SKPoint location) {
            double leastDistance = Math.Pow(this.Threshold, 2);
            int? ret = null;

            for (int i = 0; i < this.ControlPoints.Length; i ++) {
                float dist = SKPoint.Distance(location, this.ControlPoints[i]);
                if (dist <= leastDistance) {
                    ret = i;
                    leastDistance = dist;
                }
            }

            return ret;
        }
        public bool CheckInControlArea(SKPoint location, out int idx) {
            var leastDistance = this.Threshold;
            var ret = false;
            idx = -1;

            for (int i = 0; i < this.ControlPoints.Length; i++) {
                var dist = SKPoint.DistanceSquared(location, this.ControlPoints[i]);
                if (dist <= Math.Pow(this.Threshold, 2)) {
                    ret = true;
                    idx = i;
                }

                //if (ret) {
                //    if (leastDistance < dist) {
                //        idx = i;
                //        leastDistance = dist;
                //    }
                //}
            }

            return ret;
        }

        public void Draw(SKCanvas canvas) {
            var path = new SKPath();

            //var start = new SKPoint(this.CanvasSize.Width / 2, 80);
            //var pivots = new SKPoint[] {
            //    new SKPoint(this.CanvasSize.Width / 2, 80 + 100),
            //    new SKPoint(this.CanvasSize.Width / 2, 80 + 400 - 100)
            //};
            //var end = new SKPoint(this.CanvasSize.Width / 2, 80 + 400);

            path.MoveTo(this.ControlPoints[0]);
            path.CubicTo(this.ControlPoints[1], this.ControlPoints[2], this.ControlPoints[3]);

            var strokePaint = new SKPaint() {
                IsAntialias = true,
                StrokeWidth = 4,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke
            };
            var circlePaint = new SKPaint() {
                IsAntialias = true,
                Color = SKColors.BlueViolet.WithAlpha((byte)(0xFF * 0.8)),
                Style = SKPaintStyle.Fill
            };
            var tanStrokePaint = new SKPaint() {
                IsAntialias = true,
                StrokeWidth = 2,
                Color = SKColors.SlateGray,
                Style = SKPaintStyle.Stroke,
                PathEffect = SKPathEffect.CreateDash(new float[] { 10, 10 }, 10)
            };

            canvas.Clear();

            canvas.DrawPath(path, strokePaint);
            for (int i = 0; i < this.ControlPoints.Length; i ++) {
                canvas.DrawCircle(ControlPoints[i], this.Radius, circlePaint);
            }
            canvas.DrawLine(this.ControlPoints[0], this.ControlPoints[1], tanStrokePaint);
            canvas.DrawLine(this.ControlPoints[3], this.ControlPoints[2], tanStrokePaint);
        }

    }
}
