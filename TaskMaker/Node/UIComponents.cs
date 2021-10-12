using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNetExtension;
using TaskMaker;

namespace TaskMaker.Node {
    public class IconButton {
        public SKRect Bounds { get; set; }
        public string Text { get; set; } = "Button";
        public SKPoint Location { get; set; }

        public SKColor Color { get; set; }

        public void Draw(SKCanvas sKCanvas) {
            using (var textP = new SKPaint())
            using (var stroke = new SKPaint())
            using (var fill = new SKPaint()) {
                textP.IsAntialias = true;
                textP.Color = SKColors.Black;
                textP.FakeBoldText = true;
                textP.TextSize = 12;
                textP.TextAlign = SKTextAlign.Center;

                stroke.IsAntialias = true;
                stroke.StrokeWidth = 2.0f;
                stroke.Style = SKPaintStyle.Stroke;
                stroke.Color = SKColors.Gray;

                fill.IsAntialias = true;
                fill.Style = SKPaintStyle.Fill;
                fill.Color = SKColors.LightGray;

                var textBox = new SKRect();
                textP.MeasureText(Text, ref textBox);

                textBox.Location += Location;

                var mat = Extensions.CreateScaleAt(2.0f, 2.0f, textBox.GetMid());

                Bounds = mat.MapRect(textBox);

                sKCanvas.DrawRect(Bounds, fill);
                sKCanvas.DrawRect(Bounds, stroke);
                sKCanvas.DrawText(Text, Location, textP);
            }
        }
    }

}
