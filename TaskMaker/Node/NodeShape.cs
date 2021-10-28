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
        //PortShape Connector0 { get; set; }
        //PortShape Connector1 { get; set; }
        Node Parent { get; }
        SKPoint Location { get; set; }
        string Label { get; set; }

        object HitTest(SKPoint p);
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


    public class LinkShape {
        public Link Parent { get; set; }
        //public SKPoint InLocation { get; set; }
        //public SKPoint OutLocation { get; set; }
        public SKRect Bounds { get; set; }

        private bool IsDummy => Parent.IsDummy;

        private SKPoint _src;
        private SKPoint _dst;

        public LinkShape(Link parent) {
            Parent = parent;
        }

        public void Invalidate() {
            var src = Parent.Source.Shape.Src;
            var dst = Parent.IsDummy ? Parent.Dummy.Shape.Dst : Parent.Destination.Shape.Dst;

            Bounds = new SKRect(
                src.Location.X, src.Location.Y,
                dst.Location.X, dst.Location.Y);
            Bounds = Bounds.Standardized;

            _src = src.Location;
            _dst = dst.Location;
        }

        public SKPicture DrawThis() {
            Invalidate(); 

            var boxPaint = new SKPaint() {
                IsAntialias = true,
                Color = SKColors.Gray,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
            };
            var linkPaint = new SKPaint() {
                IsAntialias = true,
                Color = SKColors.DarkGray,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
            };
            var path = new SKPath();

            path.MoveTo(_src);
            path.CubicTo(
                _src + new SKPoint(50, 0), 
                _dst + new SKPoint(-50, 0), 
                _dst);

            var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bounds);

            if (IsDummy) {
                linkPaint.PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0);
            }

            canvas.DrawPath(path, linkPaint);
            canvas.DrawRect(Bounds, boxPaint);

            var pic = recorder.EndRecording();

            boxPaint.Dispose();
            linkPaint.Dispose();
            path.Dispose();
            recorder.Dispose();

            return pic;
        }

        public void Draw(SKCanvas sKCanvas) {
            var pic = DrawThis();

            sKCanvas.Save();
            //sKCanvas.Translate(Location);
            sKCanvas.DrawPicture(pic);
            sKCanvas.Restore();
        }
    }

    public class PortShape {
        public NodeBaseShape Parent { get; set; }
        public SKPoint Location { get; set; }
        public SKRect Bounds { get; set; } = new SKRect() { Size = new SKSize(12, 12) };
        //public AnchorTypes Anchor { get; set; } = AnchorTypes.Center;
        public AnchorX AnchorX { get; set; } = AnchorX.Mid;
        public AnchorY AnchorY { get; set; } = AnchorY.Mid;
        public bool IsVisible { get; set; } = true;

        private SKPoint _anchor;
        private SKMatrix _transform;

        public PortShape(NodeBaseShape parent) {
            Parent = parent;

            Invalidate();
        }

        public void RegisterEvents(EditorEventManager manager) {
            manager.DragStart += Manager_DragStart;
            manager.DragOver += Manager_DragOver;
            manager.DragEnd += Manager_DragEnd;
        }

        private void Manager_DragEnd(object sender, EditorDragEventArgs e) {
            if (sender != this)
                return;

            //var location = e.Start + e.Delta;

            //if (e.Target is PortShape) {
            //    e.Link.InLocation = location;
            //    e.Link.IsDummy = false;
            //}
        }

        private void Manager_DragOver(object sender, EditorDragEventArgs e) {
            if (sender != this)
                return;

            var location = e.Start + e.Delta;

            e.Link.Dummy.Shape.Location = location;
        }

        private void Manager_DragStart(object sender, EditorDragEventArgs e) {
            if (sender != this)
                return;
        }

        public object HitTest(SKPoint p) {
            return Contains(p) ? this : null;
        }

        public bool Contains(SKPoint p) {
            var mat = SKMatrix.CreateTranslation(Location.X, Location.Y);
            var toLocal = mat.Invert();

            return Bounds.Contains(toLocal.MapPoint(p));
        }

        private void Invalidate() {
            _anchor = new SKPoint(
                Bounds.Left + 0.5f * (int)AnchorX * Bounds.Width,
                Bounds.Top + 0.5f * (int)AnchorY * Bounds.Height);

            var mat = SKMatrix.CreateTranslation(-_anchor.X, -_anchor.Y);
            Bounds = mat.MapRect(Bounds);

            var translate = SKMatrix.CreateTranslation(Location.X, Location.Y);

            _transform = translate.Invert();
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
            //Connector1.IsVisible = false;
        }
    }

    public class MapNodeShape : NodeBaseShape {
        public override string Label { get; set; } = "Map";

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

    public class Connector {
        public NodeBaseShape Parent { get; set; }
        public SKRect Bounds { get; set; }
        public SKPoint Location => Parent._transform.Invert().MapPoint(Bounds.Location);
    }

    public class NodeBaseShape : INodeShape {
        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; } = false;
        public bool IsDragOver { get; set; } = false;
        public bool IsConnecting { get; set; } = false;
        public SKPoint Location { get; set; } = new SKPoint();
        public SKPoint Anchor { get; set; } = new SKPoint();
        public virtual string Label { get; set; } = "Node";
        public SKRect Bounds { get; set; }
        public virtual SKColor[] Colors { get; set; } = new SKColor[] {
            SKColor.Parse("#656765").FlattenWithAlpha(0.8f),
            SKColor.Parse("#BDC0BA").FlattenWithAlpha(0.8f)
        };

        //public PortShape Connector0 { get; set; }
        //public PortShape Connector1 { get; set; }

        public Connector Src;
        public Connector Dst;

        public SKTypeface Font { get; set; }

        public Node Parent { get; private set; }

        private SKPoint _dragStart;
        private SKRect _localBounds;
        public SKMatrix _transform;

        //public NodeBaseShape(Node parent) => Parent = parent;

        public NodeBaseShape(Node parent) {
            Parent = parent;

            //Connector0 = new PortShape(this) {
            //    AnchorX = AnchorX.Left,
            //    AnchorY = AnchorY.Mid
            //};

            //Connector1 = new PortShape(this) {
            //    AnchorX = AnchorX.Right,
            //    AnchorY = AnchorY.Mid
            //};

            Invalidate();
        }

        public void RegisterEvents(EditorEventManager manager) {
            manager.Click += Manager_Click;
            manager.DragStart += Manager_DragStart;
            manager.DragOver += Manager_DragOver;
            manager.DragEnd += Manager_DragEnd;

            //Connector0.RegisterEvents(manager);
            //Connector1.RegisterEvents(manager);
        }

        private void Manager_DragEnd(object sender, EditorDragEventArgs e) {
            if (sender != this)
                return;

            if (IsDragOver) {
                Location = e.Delta + _dragStart;
                _dragStart = SKPoint.Empty;

                IsSelected = false;
                IsDragOver = false;

                Invalidate();
            }
        }

        private void Manager_DragOver(object sender, EditorDragEventArgs e) {
            if (sender != this)
                return;

            if (IsDragOver)
                Location = e.Delta + _dragStart;

            if (e.IsConnector) {
                e.Link.Dummy.Shape.Location = e.Start + e.Delta;
            }
        }

        private void Manager_DragStart(object sender, EditorDragEventArgs e) {
            if (sender != this)
                return;

            if (e.IsConnector) {

            }
            else {
                IsDragOver = true;
                IsSelected = true;
                _dragStart = Location;
            }
        }

        private void Manager_Click(object sender, EditorMouseEventArgs e) {
            if (e.Handled)
                return;

            var local = _transform.MapPoint(e.Location);

            if (Contains(e.Location)) {
                if (Src.Bounds.Contains(local)) {
                    Console.WriteLine("Inside Src Port.");
                }
                else if (Dst.Bounds.Contains(local)) {
                    Console.WriteLine("Inside Dst Port.");
                }
                else {
                    e.Handled = true;
                    e.Target = Parent;

                    IsSelected = !IsSelected;
                }
            }
        }

        public object HitTest(SKPoint p) {
            var local = _transform.MapPoint(p);

            if (Src.Bounds.Contains(local))
                return Src;

            if (Dst.Bounds.Contains(local))
                return Dst;

            return Contains(p) ? this : null;
        }

        public bool Contains(SKPoint p) {
            return Bounds.Contains(p);
        }

        private SKRect _container;
        private SKRect _labelBox;
        private SKPoint _textLocation;

        public void Invalidate() {
            var translate = SKMatrix.CreateTranslation(Location.X, Location.Y);
            _transform = translate.Invert();

            var labelRect = new SKRect();
            var paint = new SKPaint() {
                TextSize = 20,
                TextAlign = SKTextAlign.Left,
                Color = SKColors.Black,
                IsAntialias = true,
                //Typeface = Font,
                //FakeBoldText = true,
            };

            paint.MeasureText(Label, ref labelRect);

            var anchor = labelRect.GetMid();
            var anchorT = SKMatrix.CreateTranslation(-anchor.X, -anchor.Y);
            var labelBox = labelRect.ScaleAt(1.5f, 2.0f, anchor);
            var container = labelRect.ScaleAt(2.5f, 2.0f, anchor);
            var textPosition = SKPoint.Empty;

            _container = anchorT.MapRect(container);
            _labelBox = anchorT.MapRect(labelBox);
            _textLocation = anchorT.MapPoint(textPosition);

            var portBox = new SKRect() {
                Size = new SKSize(12, 12)
            };
            var portAnchor = portBox.GetMid();
            var portAnchorT = SKMatrix.CreateTranslation(-portAnchor.X, -portAnchor.Y);

            Src = new Connector() {
                Parent = this,
                Bounds = SKRect.Create(new SKPoint(_container.Left, _container.MidY), portBox.Size),
            };

            Dst = new Connector() {
                Parent = this,
                Bounds = SKRect.Create(new SKPoint(_container.Right, _container.MidY), portBox.Size),
            };

            Src.Bounds = portAnchorT.MapRect(Src.Bounds);
            Dst.Bounds = portAnchorT.MapRect(Dst.Bounds);


            _localBounds = _container;
            _localBounds.Union(Src.Bounds);
            _localBounds.Union(Dst.Bounds);

            paint.Dispose();
        }

        public SKPicture DrawThis() {
            Invalidate();

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
            var portPaint = new SKPaint() {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.StrokeAndFill,
            };



            // Prepare canvas
            var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(_localBounds);


            // Draw objects on canvas
            var round = new SKRoundRect(_container, 10.0F);

            // Container
            boxPaint.Color = SKColors.Gray;
            canvas.DrawRoundRect(round, iconPaint);
            canvas.DrawRoundRect(round, boxPaint);

            // Textbox
            iconPaint.Color = Colors[1];
            canvas.DrawRect(_labelBox, iconPaint);

            // Draw ports
            canvas.DrawRect(Src.Bounds, portPaint);
            canvas.DrawRect(Dst.Bounds, portPaint);

            // Draw label
            canvas.DrawText(Label, _textLocation, paint);

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
            
            sKCanvas.Translate(Location.X, Location.Y);

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

            if (IsVisible) {
                sKCanvas.DrawPicture(pic, picPaint);
                sKCanvas.Restore();
            }

            pic.Dispose();
            picPaint.Dispose();
            return;
        }
    }
}
