using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TuggingController {
    public class ChartScene {
        private ICanvasObject root;

        public WorldSpaceCoordinate WorldSpace { get; set; } = new WorldSpaceCoordinate();

        public ChartScene() : base() {
            this.root = new RootObject_v1();
        }

        public void Update(SKCanvas canvas) {
            this.root.Draw(canvas, this.WorldSpace);
        }

        public void Dispatch(Event @event) {
            var e = @event as MouseEvent;
            var sPointer = new SKPoint(e.PointerX, e.PointerY);
            e.Pointer = this.WorldSpace.TransformToWorld(sPointer);

            this.root.Dispatcher.DispatchEvent(e);
        }

    }

    public class RootObject_v1 : CanvasObject_v1 {
        private DragAndDropComponent dragAndDropComponent;
        private float scale;
        private SKPoint windowOrigin = new SKPoint();
        private SKRect window = new SKRect();
        private SKPoint translateVector = new SKPoint();

        private SKPoint anchor;

        private Grid_v2 grid = new Grid_v2();
        private DataZone_v1 dataZone = new DataZone_v1();
        private float zoomFactor = 1.5f;
        private bool isZoom = false;
        private bool isPan = false;

        public RootObject_v1() {
            this.Dispatcher.Root = this;

            this.grid.SetParent(this);
            this.Children.Add(this.grid);

            this.dataZone.SetParent(this);
            this.Children.Add(this.dataZone);

            this.MouseWheel += this.RootObject_v1_MouseWheel;
            this.MouseDoubleClick += this.RootObject_v1_MouseDoubleClick;
            this.MouseDown += this.RootObject_v1_MouseDown;
            this.MouseUp += this.RootObject_v1_MouseUp;
            this.MouseMove += this.RootObject_v1_MouseMove;
        }

        private void RootObject_v1_MouseMove(Event @event) {
            var castArgs = @event as MouseEvent;
            var target = this;

            if (target.Dispatcher.CapturedTarget != null) {
                var lPointer = target.Transform.TransformToLocalPoint(castArgs.Pointer);
                var lAnchor = target.Transform.TransformToLocalPoint(this.anchor);
                this.translateVector = lPointer - lAnchor;
            }
        }

        private void RootObject_v1_MouseUp(Event @event) {
            var castArgs = @event as MouseEvent;
            var target = this;

            if (castArgs.Button == MouseButtons.Right) {
                if (castArgs.CurrentTarget == target) {
                    target.Dispatcher.Release();

                    this.anchor = new SKPoint();
                    this.isPan = false;
                }
            }
        }

        private void RootObject_v1_MouseDown(Event @event) {
            var castArgs = @event as MouseEvent;
            var target = this;

            if (castArgs.Button == MouseButtons.Right) {
                if (castArgs.CurrentTarget == target) {
                    target.Dispatcher.Capture(target);

                    this.anchor = castArgs.Pointer;
                    this.isPan = true;
                }
            }
        }

        private void RootObject_v1_MouseDoubleClick(Event @event) {
            var e = @event as MouseEvent;

            this.dataZone.Add(e.Pointer);
        }

        private void RootObject_v1_MouseWheel(Event @event) {
            var e = @event as MouseEvent;

            if (e.ModifierKey == Keys.Control) {
                if (e.Delta < 0) {
                    this.scale = zoomFactor;
                } else {
                    this.scale = 1.0f / zoomFactor;
                }

                this.isZoom = true;
            }
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            

            if (this.isZoom) {
                SKMatrix scale2 = SKMatrix.MakeScale(this.scale, this.scale);

                var oldWindow = worldCoordinate.Window;
                var translation1 = SKMatrix.MakeTranslation(-oldWindow.MidX, -oldWindow.MidY);
                var translation2 = SKMatrix.MakeTranslation(oldWindow.MidX, oldWindow.MidY);
                var transform = SKMatrix.MakeIdentity();

                SKMatrix.PostConcat(ref transform, translation1);
                SKMatrix.PostConcat(ref transform, scale2);
                SKMatrix.PostConcat(ref transform, translation2);

                var nWindow = transform.MapRect(oldWindow);
                worldCoordinate.Window = new SKRect { Bottom = nWindow.Top, Top = nWindow.Bottom, Left = nWindow.Left, Right = nWindow.Right };
                this.isZoom = false;
            }

            if (this.isPan) {
                SKMatrix translation = SKMatrix.MakeTranslation(-this.translateVector.X, -this.translateVector.Y);
                var nWindow = translation.MapRect(worldCoordinate.Window);
                worldCoordinate.Window = new SKRect { Bottom = nWindow.Top, Top = nWindow.Bottom, Left = nWindow.Left, Right = nWindow.Right };
            }
        }

        protected override void DrawThis(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate) { }

        public override bool ContainsPoint(SKPoint point) {
            return true;
        }
    }

    public class Grid_v2 : CanvasObject_v1 {
        private SKRect window;

        private List<Line_v1> horizontalLines = new List<Line_v1>();
        private List<Line_v1> verticalLines = new List<Line_v1>();
        private int gridScale = 50;
        private SKPaint boarderPaint = new SKPaint() { Color = SKColors.DarkKhaki, IsStroke = true, StrokeWidth = 6.0f };

        public Grid_v2() : base() { }

        private void CalculateGridInWindow() {
            this.horizontalLines.Clear();
            this.verticalLines.Clear();
            this.Children.Clear();

            List<int> calculate(float min, float max, ref int interval) {
                var ret = new List<int>();
                int minInt = (int)Math.Truncate(min);
                int maxInt = (int)Math.Truncate(max);
                //int quotient = (maxInt - minInt) / interval;

                //while (quotient >= 50) {
                //    interval *= 2;
                //    quotient = (maxInt - minInt) / interval;
                //}

                Math.DivRem(minInt, interval, out int reminder);

                int value; 
                if (minInt < 0) {
                    value = minInt - reminder;
                } else {
                    value = minInt + interval - reminder;
                }

                while (true) {
                    if (value > maxInt) {
                        break;
                    }

                    ret.Add(value);

                    value += interval;
                }

                return ret;
            }

            var gridXCoordinates = calculate(this.window.Left, this.window.Right, ref this.gridScale);
            var gridYCoordinates = calculate(this.window.Bottom, this.window.Top, ref this.gridScale);

            foreach (var x in gridXCoordinates) {
                var vLine = new Line_v1() {
                    P0 = new SKPoint(x, this.window.Bottom),
                    P1 = new SKPoint(x, this.window.Top)
                };

                if (x == 0) {
                    vLine.Paint.StrokeWidth = 2.0f;
                }

                vLine.SetParent(this);
                this.verticalLines.Add(vLine);
                this.Children.Add(vLine);
            }

            foreach (var y in gridYCoordinates) {
                var hLine = new Line_v1() {
                    P0 = new SKPoint(this.window.Left, y),
                    P1 = new SKPoint(this.window.Right, y)
                };

                if (y == 0) {
                    hLine.Paint.StrokeWidth = 2.0f;
                }

                hLine.SetParent(this);
                this.horizontalLines.Add(hLine);
                this.Children.Add(hLine);
            }
        }

        protected override void DrawThis(SKCanvas canvas, WorldSpaceCoordinate worldCoordinate) {
            var screenRect = worldCoordinate.TransformToDeviceRect(this.window);

            canvas.DrawRect(screenRect, this.boarderPaint);
        }

        protected override void Invalidate() {
            this.CalculateGridInWindow();
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this.window = worldCoordinate.Window;
            this.CalculateGridInWindow();
        }

        public override bool ContainsPoint(SKPoint point) {
            return this.window.Contains(point);
        }
    }
}
