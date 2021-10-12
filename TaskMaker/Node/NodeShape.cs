using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using MathNetExtension;

namespace TaskMaker.Node {
    public interface INodeShape {
        PortShape Connector0 { get; set; }
        PortShape Connector1 { get; set; }
        INode Parent { get; }
        SKPoint Location { get; set; }
        string Label { get; set; }

        bool Contains(SKPoint p);
        void Draw(SKCanvas sKCanvas);
    }

    public class Link {
        public INode In { get; private set; }
        public INode Out { get; private set; }

        public LinkShape Shape { get; set; }

        public Link() {
            Shape = new LinkShape();
        }

        public void SetOut(INode output) {
            Out = output;
            Shape.P0Ref = output.Shape.Connector1;

            Shape.P1Dummy = new PortShape();
        }

        public void Update(SKPoint p) {
            Shape.P1Dummy.Location = p;
        }

        public void SetIn(INode input) {
            Shape.P1Dummy = null;

            if (Out.GetType() == typeof(LayerNode)) {
                if (input.GetType() == typeof(JoinNode)) {
                    In = input;
                    Shape.P1Ref = In.Shape.Connector0;

                    Services.Flow.AddSource(Out as LayerNode);
                    Services.Flow.Links.Add(this);
                }
            }
            else if (Out.GetType() == typeof(JoinNode)) {
                if (input.GetType() == typeof(NLinearMapNode)) {
                    In = input;
                    Shape.P1Ref = In.Shape.Connector0;

                    Services.Flow.Links.Add(this);
                }
            }
            else if (Out.GetType() == typeof(SplitNode)) {
                if (input.GetType() == typeof(LayerNode)) {
                    In = input;
                    Shape.P1Ref = In.Shape.Connector0;

                    Services.Flow.AddSink(Out as LayerNode);
                    Services.Flow.Links.Add(this);
                }
                else if (input.GetType() == typeof(MotorNode)) {
                    In = input;
                    Shape.P1Ref = In.Shape.Connector0;

                    Services.Flow.AddSink(Out as MotorNode);
                    Services.Flow.Links.Add(this);
                }
            }
            else if (Out.GetType() == typeof(NLinearMapNode)) {
                if (input.GetType() == typeof(SplitNode)) {
                    In = input;
                    Shape.P1Ref = In.Shape.Connector0;

                    Services.Flow.Links.Add(this);
                }
            }
        }
    }

    public class LinkShape {
        public PortShape P0Ref { get; set; }
        public PortShape P1Ref { get; set; }
        public PortShape P1Dummy { get; set; }

        public void Draw(SKCanvas sKCanvas) {
            var stroke = new SKPaint() {
                Color = SKColor.Parse("#0F2540").FlattenWithAlpha(0.5f),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 4.0f,
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
            else if (P1Dummy == null & P1Ref != null) {
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
            SKColor.Parse("#006284").FlattenWithAlpha(0.8f),
            SKColor.Parse("#81C7D4").FlattenWithAlpha(0.8f)
        };

        public MotorNodeShape(INode parent) : base(parent) {
            Connector1.IsVisible = false;
        }
    }

    public class MapNodeShape : NodeBaseShape {
        public MapNodeShape(INode parent) : base(parent) { }

        public override SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#77428D").FlattenWithAlpha(0.8f),
            SKColor.Parse("#B481BB").FlattenWithAlpha(0.8f)
        };
    }

    public class LayerNodeShape : NodeBaseShape {
        public LayerNodeShape(INode parent) : base(parent) { }

        public override SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#F75C2F").FlattenWithAlpha(0.8f),
            SKColor.Parse("#FB966E").FlattenWithAlpha(0.8f)
        };
    }

    public class SplitNodeShape : NodeBaseShape {
        public override string Label { get; set; } = "Split";
        public override SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#1B813E").FlattenWithAlpha(0.8f),
            SKColor.Parse("#5DAC81").FlattenWithAlpha(0.8f)
        };

        public SplitNodeShape(INode parent) : base(parent) { }
    }

    public class JoinNodeShape : NodeBaseShape {
        public override string Label { get; set; } = "Join";

        public JoinNodeShape(INode parent) : base(parent) { }

        public override SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#1B813E").FlattenWithAlpha(0.8f),
            SKColor.Parse("#5DAC81").FlattenWithAlpha(0.8f)
        };

    }


    public class NodeBaseShape : INodeShape {
        public SKPoint Location { get; set; } = new SKPoint();
        public virtual string Label { get; set; } = "Node";
        public SKRect Bounds { get; set; }
        public virtual SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#656765").FlattenWithAlpha(0.8f),
            SKColor.Parse("#BDC0BA").FlattenWithAlpha(0.8f)
        };

        public PortShape Connector0 { get; set; } = new PortShape();
        public PortShape Connector1 { get; set; } = new PortShape();
        public SKTypeface Font { get; set; }

        public INode Parent { get; private set; }

        public NodeBaseShape(INode parent) => Parent = parent;

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
            };

            var labelRect = new SKRect();
            paint.MeasureText(Label, ref labelRect);


            labelRect.Location += Location;

            var box = labelRect.ScaleAt(1.2f, 2.0f, labelRect.GetMid());
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
