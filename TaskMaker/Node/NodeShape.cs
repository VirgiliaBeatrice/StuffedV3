using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using MathNetExtension;

namespace TaskMaker.Node {
    public class LinkShape {
        public PortShape P0Ref { get; set; }
        public PortShape P1Ref { get; set; }
        public PortShape P1Dummy { get; set; }

        public void Draw(SKCanvas sKCanvas) {
            var stroke = new SKPaint() {
                Color = SKColor.Parse("#0F2540").WithAlpha(0.3f),
                BlendMode= SKBlendMode.SrcOver,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2.0f,
            };

            var path = new SKPath();

            path.MoveTo(P0Ref.Location);

            if (P1Dummy != null) {
                if (SKPoint.Distance(P0Ref.Location, P1Dummy.Location) < 10) {
                    path.LineTo(P1Dummy.Location);
                }
                else {
                    path.CubicTo(P0Ref.Location + new SKPoint(40, 0), P1Dummy.Location + new SKPoint(-40, 0), P1Dummy.Location);
                }
            }
            else {
                if (SKPoint.Distance(P0Ref.Location, P1Ref.Location) < 10) {
                    path.LineTo(P1Ref.Location);
                }
                else {
                    path.CubicTo(P0Ref.Location + new SKPoint(40, 0), P1Ref.Location + new SKPoint(-40, 0), P1Ref.Location);
                }

            }


            sKCanvas.DrawPath(path, stroke);
        }
    }

    public class PortShape {
        public SKPoint Location { get; set; }
        public LinkShape Binding { get; set; }
        public bool IsVisible { get; set; } = true;

        public void Draw(SKCanvas sKCanvas) {
            if (!IsVisible) {
                return;
            }

            var iconPaint = new SKPaint() {
                Color = SKColor.Parse("#434343"),
                IsAntialias = true,
                Style = SKPaintStyle.StrokeAndFill
            };

            var anchor = SKMatrix.CreateTranslation(-10 / 2, -10 / 2);
            var box = new SKRect() {
                Size = new SKSize(10, 10),
                Location = Location,
            };

            box = anchor.MapRect(box);

            sKCanvas.DrawRect(box, iconPaint);

            iconPaint.Dispose();
        }
    }

    public class MotorNodeShape : NodeBaseShape {
        public override SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#006284"),
            SKColor.Parse("#81C7D4")
        };

        public MotorNodeShape() : base() {
            Connector1.IsVisible = false;
        }
    }

    public class MapNodeShape : NodeBaseShape {
        public override SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#77428D"),
            SKColor.Parse("#B481BB")
        };
    }

    public class LayerNodeShape : NodeBaseShape {
        public override SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#F75C2F").WithAlpha(0.5f),
            SKColor.Parse("#FB966E").WithAlpha(0.5f)
        };
    }

    public class ExcuteNodeShape : NodeBaseShape {
        public override string Label { get; set; } = "Excute";
        public override SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#1B813E"),
            SKColor.Parse("#5DAC81")
        };

        public ExcuteNodeShape() : base() {
            Connector0.IsVisible = false;
        }
    }


    public class NodeBaseShape {
        public SKPoint Location { get; set; } = new SKPoint();
        public virtual string Label { get; set; } = "Node";
        public SKRect Bounds { get; set; }
        public virtual SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#656765"),
            SKColor.Parse("#BDC0BA")
        };

        public PortShape Connector0 { get; set; } = new PortShape();
        public PortShape Connector1 { get; set; } = new PortShape();
        public SKTypeface Font { get; set; }

        public NodeBaseShape() { }

        public bool Contains(SKPoint p) {
            return Bounds.Contains(p);
        }

        public void Draw(SKCanvas sKCanvas) {
            var paint = new SKPaint() {
                TextSize = 20,
                TextAlign = SKTextAlign.Left,
                Color = SKColors.Black,
                IsAntialias = true,
                //Typeface = Font,
                FakeBoldText = true,
            };
            var iconPaint = new SKPaint() {
                Color = Colors[0],
                IsAntialias = true,
                Style = SKPaintStyle.StrokeAndFill,
                BlendMode = SKBlendMode.SrcOver
            };

            var labelRect = new SKRect();
            paint.MeasureText(Label, ref labelRect);


            labelRect.Location += Location;
            //var t = SKMatrix.CreateScaleTranslation(4.0f, 4.0f, -labelRect.MidX, -labelRect.MidY);

            var t = SKMatrix.CreateTranslation(-labelRect.MidX, -labelRect.MidY);
            var s = SKMatrix.CreateScale(1.2f, 2.0f);
            var t_inv = SKMatrix.CreateTranslation(labelRect.MidX, labelRect.MidY);

            var box = t.PostConcat(s).PostConcat(t_inv).MapRect(labelRect);
            var box1 = box;

            box1.Size = box.Size + new SKSize(box.Height * 2, 0);
            var t1 = SKMatrix.CreateTranslation(-box.Height, 0);
            box1 = t1.MapRect(box1);

            Bounds = box1;

            var round = new SKRoundRect(box1, 10.0F);

            var connectorBox0 = new SKRect();
            connectorBox0.Size = new SKSize(10, 10);
            connectorBox0.Location = new SKPoint(box1.Left, box1.MidY);

            var connectorBox1 = new SKRect();
            connectorBox1.Size = new SKSize(10, 10);
            connectorBox1.Location = new SKPoint(box1.Right, box1.MidY);

            Connector0.Location = connectorBox0.Location;
            Connector1.Location = connectorBox1.Location;

            sKCanvas.DrawRoundRect(round, iconPaint);
            iconPaint.Color = Colors[1];

            sKCanvas.DrawRect(box, iconPaint);

            Connector0.Draw(sKCanvas);
            Connector1.Draw(sKCanvas);

            sKCanvas.DrawText(Label, Location, paint);

            paint.Dispose();
            iconPaint.Dispose();
        }
    }
}
