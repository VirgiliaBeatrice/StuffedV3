using System;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace TuggingController
{
    public class RobotController
    {
        public RobotController()
        {
            string title = "Simple Circle";

            using (var surface = SKSurface.Create(SKImageInfo.Empty))
            {
                SKCanvas canvas = surface.Canvas;

                // clear the canvas / fill with white
                canvas.Clear(SKColors.White);

                // set up drawing tools
                using (var paint = new SKPaint())
                {
                    paint.IsAntialias = true;
                    paint.Color = new SKColor(0x2c, 0x3e, 0x50);
                    paint.StrokeCap = SKStrokeCap.Round;

                    // create the Xamagon path
                    using (var path = new SKPath())
                    {
                        path.MoveTo(71.4311121f, 56f);
                        path.CubicTo(68.6763107f, 56.0058575f, 65.9796704f, 57.5737917f, 64.5928855f, 59.965729f);
                        path.LineTo(43.0238921f, 97.5342563f);
                        path.CubicTo(41.6587026f, 99.9325978f, 41.6587026f, 103.067402f, 43.0238921f, 105.465744f);
                        path.LineTo(64.5928855f, 143.034271f);
                        path.CubicTo(65.9798162f, 145.426228f, 68.6763107f, 146.994582f, 71.4311121f, 147f);
                        path.LineTo(114.568946f, 147f);
                        path.CubicTo(117.323748f, 146.994143f, 120.020241f, 145.426228f, 121.407172f, 143.034271f);
                        path.LineTo(142.976161f, 105.465744f);
                        path.CubicTo(144.34135f, 103.067402f, 144.341209f, 99.9325978f, 142.976161f, 97.5342563f);
                        path.LineTo(121.407172f, 59.965729f);
                        path.CubicTo(120.020241f, 57.5737917f, 117.323748f, 56.0054182f, 114.568946f, 56f);
                        path.LineTo(71.4311121f, 56f);
                        path.Close();

                        // draw the Xamagon path
                        canvas.DrawPath(path, paint);
                    }
                }
            }


        }
    }

    internal static class SharedPage
    {
        public static void OnPainting(object sender, SKPaintSurfaceEventArgs e)
        {
            // CLEARING THE SURFACE

            // we get the current surface from the event args
            var surface = e.Surface;
            // then we get the canvas that we can draw on
            var canvas = surface.Canvas;
            // clear the canvas / view
            canvas.Clear(SKColors.White);


            // DRAWING SHAPES

            // create the paint for the filled circle
            var circleFill = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColors.Blue
            };
            // draw the circle fill
            canvas.DrawCircle(100, 100, 40, circleFill);

            // create the paint for the circle border
            var circleBorder = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Red,
                StrokeWidth = 5
            };
            // draw the circle border
            canvas.DrawCircle(100, 100, 40, circleBorder);


            // DRAWING PATHS

            // create the paint for the path
            var pathStroke = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Green,
                StrokeWidth = 5
            };

            // create a path
            var path = new SKPath();
            path.MoveTo(160, 60);
            path.LineTo(240, 140);
            path.MoveTo(240, 60);
            path.LineTo(160, 140);

            // draw the path
            canvas.DrawPath(path, pathStroke);


            // DRAWING TEXT

            // create the paint for the text
            var textPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SKColors.Orange,
                TextSize = 80
            };
            // draw the text (from the baseline)
            canvas.DrawText("SkiaSharp", 60, 160 + 80, textPaint);
        }
    }
}





