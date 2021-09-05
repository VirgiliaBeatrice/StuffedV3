using SkiaSharp;

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

        private SKPaint fillPaint = new SKPaint {
            IsAntialias = true,
            Color = SkiaHelper.ConvertColorWithAlpha(SKColors.ForestGreen, 0.8f),
            Style = SKPaintStyle.Fill
        };
        private SKPaint strokePaint = new SKPaint {
            IsAntialias = true,
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
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
            if (!this.IsClosed)
                skCanvas.DrawRect(this._rect, this.strokePaint);
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
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
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
            if (!this.IsClosed)
                skCanvas.DrawPath(this._path, this.strokePaint);
        }

        public void Trace(SKPoint point) {
            this._path.LineTo(point);
        }

        public void End() {
            this._path.Close();
            this.IsClosed = true;
        }
    }
}
