using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace TaskMaker.View.Widgets {
    public class GridWidget : ContainerWidget {
        public SKRect ClipBound { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }

        private float _itemWidth;
        private float _itemHeight;

        public GridWidget() { }

        public GridWidget(int row, int column, float width, float height) {
            Row = row;
            Col = column;

            Margin = 0;
            Padding = 0;

            Width = width;
            Height = height;
            ClipBound = new SKRect(0, 0, width, height);
        }

        public void Validate() {
            _itemWidth = Width / Col;
            _itemHeight = Height / Row;
        }

        public object CalculateItemLocation(int row, int col) {
            return new float[] {
                row * _itemHeight, col * _itemWidth
            };
        }

        public override void SetTransform(float x, float y) {
            _transform = SKMatrix.CreateTranslation(x, y);
        }
    }

    public class GridItemWidget : ContainerWidget {
        public SKRect ClipBound { get; set; }

        private int _paintLayer;

        public GridItemWidget() : this(0, 0) { }

        public GridItemWidget(float width, float height) {
            Width = width;
            Height = height;
            ClipBound = new SKRect(0, 0, Width, Height);
        }

        public override void SetTransform(float x, float y) {
            _transform = SKMatrix.CreateTranslation(x, y);
        }

        public override void OnPainting(SKCanvas canvas) {
            _paintLayer = canvas.Save();

            canvas.ClipRect(ClipBound);
            canvas.SetMatrix(_transform);

            // if it has any render object
            if (RenderObject != null)
                canvas.DrawPicture(RenderObject.Picture);
        }

        public override void OnPainted(SKCanvas canvas) {
            canvas.RestoreToCount(_paintLayer);
        }
    }
}
