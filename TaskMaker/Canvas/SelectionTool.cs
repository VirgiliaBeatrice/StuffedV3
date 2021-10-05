using SkiaSharp;
using System.Linq;
using MathNetExtension;

namespace TaskMaker {
    public interface ISelectionTool {
        bool IsClosed { get; set; }
        bool Contains(SKPoint point);
        void Trace(SKPoint point);
        void End();
        void DrawThis(SKCanvas sKCanvas);
    }

    public class RectSelectionTool : ISelectionTool {
        public bool IsClosed { get; set; } = false;

        private SKPaint strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DarkGray.WithAlpha(0.9f),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            PathEffect = SKPathEffect.CreateDash(new float[] { 5.0f, 5.0f }, 0.0f),
        };
        private SKPaint _fill = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DarkGray.WithAlpha(0.3f),
            Style = SKPaintStyle.Fill,
        };
        private SKRect _rect = new SKRect();
        private SKSize _size = new SKSize();


        public RectSelectionTool(SKPoint start) {
            this._rect.Location = start;
        }

        public bool Contains(SKPoint point) {
            return this._rect.Contains(point.X, point.Y);
        }

        public void Invalidate() {

        }

        public void DrawThis(SKCanvas skCanvas) {
            if (!this.IsClosed) {
                skCanvas.DrawRect(this._rect, this.strokePaint);
                skCanvas.DrawRect(this._rect, _fill);
            }
        }

        public void Trace(SKPoint point) {
            this._size = new SKSize(point - this._rect.Location);
            this._rect.Size = this._size;
        }

        public void End() {
            this.IsClosed = true;
        }
    }

    public class LassoSelectionTool : ISelectionTool {
        public bool IsClosed { get; set; } = false;

        private SKPath _path = new SKPath();
        private SKPaint strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DarkGray.WithAlpha(0.9f),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            PathEffect = SKPathEffect.CreateDash(new float[] { 5.0f, 5.0f }, 0.0f),
        };
        private SKPaint _fill = new SKPaint {
            IsAntialias = true,
            Color = SKColors.DarkGray.WithAlpha(0.3f),
            Style = SKPaintStyle.Fill,
        };

        public LassoSelectionTool(SKPoint start) {
            this._path.MoveTo(start);
        }

        public bool Contains(SKPoint point) {
            return this._path.Contains(point.X, point.Y);
        }

        public void Invalidate() {

        }

        public void DrawThis(SKCanvas skCanvas) {
            var points = _path.Points.Where((p, idx) => idx % 4 == 0);
            var curve = Geometry.BezierCurve.GetBezierCurve(points, 0.4f);

            if (!this.IsClosed) {
                //skCanvas.DrawPath(this._path, this.strokePaint);
                skCanvas.DrawPath(curve, strokePaint);
                skCanvas.DrawPath(curve, _fill);
            }
        }

        public void Trace(SKPoint point) {
            if (point != _path.LastPoint) {
                this._path.LineTo(point);
            }
        }

        public void End() {
            this._path.Close();
            this.IsClosed = true;
        }
    }
}
