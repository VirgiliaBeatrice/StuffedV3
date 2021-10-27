using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using MathNetExtension;
using System.Windows.Forms;

namespace TaskMaker.Node {
    public interface INodeShape {
        PortShape Connector0 { get; set; }
        PortShape Connector1 { get; set; }
        Node Parent { get; }
        SKPoint Location { get; set; }
        string Label { get; set; }

        bool Contains(SKPoint p);
        void Draw(SKCanvas sKCanvas);
    }

    public enum AnchorTypes {
        LeftTop,
        LeftMid,
        LeftBottom,
        MidTop,
        Center,
        MidBottom,
        RightTop,
        RightMid,
        RightBottom
    }

    public enum AnchorX {
        Left = 0,
        Mid = 1,
        Right = 2,
    }

    public enum AnchorY {
        Top = 0,
        Mid = 1,
        Bottom = 2,
    }

    //public struct Transform {
    //    public SKMatrix Scale;
    //    public SKMatrix Rotate;
    //    public SKMatrix Translate;
    //    public SKPoint Anchor;
    //    public SKPoint Pivot;

    //    public Transform(SKMatrix s, SKMatrix r, SKMatrix t, SKPoint anchor, SKPoint pivot) {
    //        Scale = s;
    //        Rotate = r;
    //        Translate = t;
    //        Anchor = anchor;
    //        Pivot = pivot;
    //    }

    //    private SKMatrix T => Scale.PostConcat(Rotate).PostConcat(Translate);

    //    private SKMatrix TInv => T.Invert();

    //    public SKPoint LocalToParent(SKPoint point) => T.MapPoint(point);

    //    public SKPoint ParentToLocal(SKPoint point) => TInv.MapPoint(point);

    //    public static Transform Identity => new Transform(SKMatrix.Identity, SKMatrix.Identity, SKMatrix.Identity, anchor);
    //}

    public class PortShape {
        public SKPoint Location { get; set; }
        public SKRect Bounds { get; set; } = new SKRect() { Size = new SKSize(10, 10) };
        //public AnchorTypes Anchor { get; set; } = AnchorTypes.Center;
        public AnchorX AnchorX { get; set; } = AnchorX.Mid;
        public AnchorY AnchorY { get; set; } = AnchorY.Mid;
        public bool IsVisible { get; set; } = true;

        private SKPoint _anchor;
        //public Transform Transform { get; set; } = Transform.Identity;

        public PortShape() {
            Invalidate();
        }

        public bool Contains(SKPoint p) {
            //var anchor = SKMatrix.CreateTranslation(-_anchor.X, -_anchor.Y);
            var mat = SKMatrix.CreateTranslation(Location.X, Location.Y);
            //var mat = anchor.PostConcat(translate);
            var toLocal = mat.Invert();

            return Bounds.Contains(toLocal.MapPoint(p));
        }

        private void Invalidate() {
            _anchor = new SKPoint(
                Bounds.Left + 0.5f * (int)AnchorX * Bounds.Width,
                Bounds.Top + 0.5f * (int)AnchorY * Bounds.Height);

            var mat = SKMatrix.CreateTranslation(-_anchor.X, -_anchor.Y);
            Bounds = mat.MapRect(Bounds);
        }

        public SKPicture DrawThis() {
            var iconPaint = new SKPaint() {
                Color = SKColor.Parse("#434343"),
                IsAntialias = true,
                Style = SKPaintStyle.StrokeAndFill
            };

            //_anchor = new SKPoint(
            //    Bounds.Left + 0.5f * (int)AnchorX * Bounds.Width,
            //    Bounds.Top + 0.5f * (int)AnchorY * Bounds.Height);
            //var mat = SKMatrix.CreateTranslation(-_anchor.X, -_anchor.Y);
            //Bounds = mat.MapRect(Bounds);

            // Prepare canvas
            var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bounds);

            canvas.DrawRect(Bounds, iconPaint);

            var pic = recorder.EndRecording();

            iconPaint.Dispose();
            recorder.Dispose();

            return pic;
        } 


        public void Draw(SKCanvas sKCanvas) {
            if (!IsVisible) {
                return;
            }

            var pic = DrawThis();

            sKCanvas.Save();
            //sKCanvas.Translate(-_anchor.X, -_anchor.Y);
            sKCanvas.Translate(Location);
            sKCanvas.DrawPicture(pic);
            sKCanvas.Restore();
        }
    }

    public class MotorNodeShape : NodeBaseShape {
        public override string Label { get; set; } = "Motor";

        public override SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#006284").FlattenWithAlpha(0.8f),
            SKColor.Parse("#81C7D4").FlattenWithAlpha(0.8f)
        };

        public MotorNodeShape(Node parent) : base(parent) {
            Connector1.IsVisible = false;
        }
    }

    public class MapNodeShape : NodeBaseShape {
        public MapNodeShape(Node parent) : base(parent) { }

        public override SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#77428D").FlattenWithAlpha(0.8f),
            SKColor.Parse("#B481BB").FlattenWithAlpha(0.8f)
        };
    }

    //public class LayerNodeShape : NodeBaseShape {
    //    public LayerNodeShape(INode parent) : base(parent) { }

    //    public override SKColor[] Colors { get; set; } = new SKColor[] {
    //        SKColor.Parse("#F75C2F").FlattenWithAlpha(0.8f),
    //        SKColor.Parse("#FB966E").FlattenWithAlpha(0.8f)
    //    };
    //}

    //public class SplitNodeShape : NodeBaseShape {
    //    public override string Label { get; set; } = "Split";
    //    public override SKColor[] Colors { get; set; } = new SKColor[] {
    //        SKColor.Parse("#1B813E").FlattenWithAlpha(0.8f),
    //        SKColor.Parse("#5DAC81").FlattenWithAlpha(0.8f)
    //    };

    //    public SplitNodeShape(INode parent) : base(parent) { }
    //}

    //public class JoinNodeShape : NodeBaseShape {
    //    public override string Label { get; set; } = "Join";

    //    public JoinNodeShape(INode parent) : base(parent) { }

    //    public override SKColor[] Colors { get; set; } = new SKColor[] {
    //        SKColor.Parse("#1B813E").FlattenWithAlpha(0.8f),
    //        SKColor.Parse("#5DAC81").FlattenWithAlpha(0.8f)
    //    };

    //}


    public class NodeBaseShape : INodeShape {
        public bool IsSelected { get; set; } = false;
        public bool IsDragOver { get; set; } = false;
        public SKPoint Location { get; set; } = new SKPoint();
        public SKPoint Anchor { get; set; } = new SKPoint();
        public virtual string Label { get; set; } = "Node";
        public SKRect Bounds { get; set; }
        public virtual SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#656765").FlattenWithAlpha(0.8f),
            SKColor.Parse("#BDC0BA").FlattenWithAlpha(0.8f)
        };

        public PortShape Connector0 { get; set; } = new PortShape();
        public PortShape Connector1 { get; set; } = new PortShape();
        public SKTypeface Font { get; set; }

        public Node Parent { get; private set; }

        private SKPoint _dragStart;
        private SKRect _localBounds;

        public NodeBaseShape(Node parent) => Parent = parent;

        //public NodeBaseShape(Node parent) {
        //    Parent = parent;
        //}

        public void RegisterEvents(EditorEventManager manager) {
            manager.Click += Manager_Click;
            manager.DragStart += Manager_DragStart;
            manager.DragOver += Manager_DragOver;
            manager.DragEnd += Manager_DragEnd;
        }

        private void Manager_DragEnd(object sender, EditorDragEventArgs e) {
            if (Parent == e.Target) {
                Location = e.Delta + _dragStart;
                _dragStart = SKPoint.Empty;

                IsSelected = false;
                IsDragOver = false;
            }
        }

        private void Manager_DragOver(object sender, EditorDragEventArgs e) {
            if (Parent == e.Target) {
                Location = e.Delta + _dragStart;
            }
        }

        private void Manager_DragStart(object sender, EditorDragEventArgs e) {
            if (e.Target != null & e.Target != Parent)
                return;

            if (Contains(e.Start)) {
                IsDragOver = true;
                e.Target = Parent;
                _dragStart = Location;

                IsSelected = true;
            }
        }

        private void Manager_Click(object sender, EditorMouseEventArgs e) {
            if (e.Handled)
                return;

            if (Contains(e.Location)) {
                e.Handled = true;
                e.Target = Parent;

                IsSelected = !IsSelected;
            }
        }

        public bool Contains(SKPoint p) {
            var mat = SKMatrix.CreateTranslation(Location.X, Location.Y);
            if (Connector0.Contains(mat.Invert().MapPoint(p))) {

            }
            return Bounds.Contains(p);
        }

        public SKPicture DrawThis() {

            var boxPaint = new SKPaint() {
                Color = Colors[1],
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
            };
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

            Anchor = labelRect.GetMid();

            var labelBox = labelRect.ScaleAt(1.5f, 2f, labelRect.GetMid());
            var container = labelRect.ScaleAt(2.5f, 2f, labelRect.GetMid());

            _localBounds = container;

            var round = new SKRoundRect(container, 10.0F);

            var connectorBox0 = new SKRect();
            connectorBox0.Size = new SKSize(10, 10);
            connectorBox0.Location = new SKPoint(container.Left, container.MidY);

            var connectorBox1 = new SKRect();
            connectorBox1.Size = new SKSize(10, 10);
            connectorBox1.Location = new SKPoint(container.Right, container.MidY);

            Connector0.Location = connectorBox0.Location;
            Connector1.Location = connectorBox1.Location;

            // Prepare canvas
            var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(_localBounds);


            // Draw objects on canvas
            canvas.DrawRoundRect(round, iconPaint);
            iconPaint.Color = Colors[1];

            //if (IsSelected) {
            //    canvas.DrawRoundRect(round, boxPaint);
            //} else {
                boxPaint.Color = SKColors.Gray;
                canvas.DrawRoundRect(round, boxPaint);
            //}

            canvas.DrawRect(labelBox, iconPaint);

            Connector0.Draw(canvas);
            Connector1.Draw(canvas);

            canvas.DrawText(Label, SKPoint.Empty, paint);

            paint.Dispose();
            iconPaint.Dispose();
            boxPaint.Dispose();

            var pic = recorder.EndRecording();

            recorder.Dispose();
            return pic;
        }

        public void Draw(SKCanvas sKCanvas) {
            var picPaint = new SKPaint() {
                IsAntialias = true,
                ImageFilter = SKImageFilter.CreateDropShadow(
                    0.0f,
                    0.0f,
                    10.0f,
                    10.0f,
                    SKColors.Gray)};
            var pic = DrawThis();
            sKCanvas.Save();
            
            //if (IsDragOver) {
            //    sKCanvas.RotateDegrees(10);
            //}
            // Translate to location
            sKCanvas.Translate(Location.X, Location.Y);

            // Translate to anchor
            sKCanvas.Translate(-Anchor.X, -Anchor.Y);

            var mat = sKCanvas.TotalMatrix;
            Bounds = mat.MapRect(_localBounds);

            if (IsSelected) {
                picPaint.ImageFilter = SKImageFilter.CreateDropShadow(
                    0.0f,
                    0.0f,
                    5.0f,
                    5.0f,
                    SKColors.Cyan);
            }
            sKCanvas.DrawPicture(pic, picPaint);
            sKCanvas.Restore();

            pic.Dispose();
            picPaint.Dispose();
            return;
        }
    }
}
