using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace TaskMaker.View.Widgets.Units {
    public class UnitWidget {
        public SKRect Bound { get; set; }

        private SKPicture _cachedPicture;

        public void Render() {
            var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bound);
            var roundSize = new SKSize(5, 5);
            var stroke = new SKPaint {
                IsAntialias = true,
                StrokeWidth = 2,
                Color = SKColors.Black
            };
            var fill = new SKPaint {
                IsAntialias = true,
                Color = SKColors.Aquamarine
            };

            canvas.DrawRoundRect(Bound, roundSize, stroke);
            canvas.DrawRoundRect(Bound, roundSize, fill);

            _cachedPicture = recorder.EndRecording();

            stroke.Dispose();
            fill.Dispose();
            canvas.Dispose();
        }
    }
}
