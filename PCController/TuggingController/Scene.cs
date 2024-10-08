﻿using SkiaSharp;
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

    public interface IScene {
        ICanvasObject Root { get; set; }
        WorldSpaceCoordinate WorldSpace { get; set; }
        EventDispatcher<ICanvasObject> Dispatcher { get; set; }
        event EventHandler<CanvasTargetChangedEventArgs> CanvasTargetChanged;
        event EventHandler<EventArgs> CanvasObjectChanged;
        event EventHandler<DataValidatedEventArgs> DataValidated;

        void Dispatch(Event @event);
        void Update(SKCanvas canvas);

        ContextMenu GenerateClickContextMenu(IEnumerable<object> targets, Event @event);
        ContextMenu GenerateDbClickContextMenu();
    }

    public class RootObject_v1 : CanvasObject_v1 {
        private float scale;
        private SKPoint translateVector = new SKPoint();

        private SKPoint anchor;

        private Grid_v2 grid = new Grid_v2() { IsNodeVisible = false };
        private DataZone_v1 dataZone = new DataZone_v1() { IsNodeVisible = false };
        private float zoomFactor = 1.5f;
        private bool isZoom = false;
        private bool isPan = false;

        public override bool Selectable { get; set; } = false;

        public RootObject_v1(IScene scene) {
            // TODO!
            this.Scene = scene;

            this.grid.SetParent(this);
            this.grid.Scene = this.Scene;
            this.Children.Add(this.grid);

            this.dataZone.SetParent(this);
            this.dataZone.Scene = this.Scene;
            this.Children.Add(this.dataZone);

            this.MouseWheel += this.RootObject_v1_MouseWheel;
            this.MouseDoubleClick += this.RootObject_v1_MouseDoubleClick;
            this.MouseDown += this.RootObject_v1_MouseDown;
            this.MouseUp += this.RootObject_v1_MouseUp;
            this.MouseMove += this.RootObject_v1_MouseMove;
        }

        public void AddEntity(Entity_v1 entity) {
            this.dataZone.Add(entity);
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

        protected virtual void RootObject_v1_MouseDoubleClick(Event @event) {
            var e = @event as MouseEvent;

            if (e.Button == MouseButtons.Left) {
                this.dataZone.Add(e.Pointer);
            }
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
                //SKMatrix scale2 = SKMatrix.MakeScale(this.scale, this.scale);
                SKMatrix scale2 = SKMatrix.CreateScale(this.scale, this.scale);

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

        protected override void DrawThis(SKCanvas canvas) { }

        public override bool ContainsPoint(SKPoint point) {
            return true;
        }
    }

    public class Grid_v2 : CanvasObject_v1 {
        private SKRect window;
        private SKRect gWindow;

        private List<Line_v1> horizontalLines = new List<Line_v1>();
        private List<Line_v1> verticalLines = new List<Line_v1>();
        private int gridScale = 50;
        private SKPaint boarderPaint = new SKPaint() { Color = SKColors.DarkKhaki, IsStroke = true, StrokeWidth = 6.0f };

        public new bool Selectable { get; set; } = false;

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
                    P1 = new SKPoint(x, this.window.Top),
                    IsNodeVisible = false,
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
                    P1 = new SKPoint(this.window.Right, y),
                    IsNodeVisible = false,
                };

                if (y == 0) {
                    hLine.Paint.StrokeWidth = 2.0f;
                }

                hLine.SetParent(this);
                this.horizontalLines.Add(hLine);
                this.Children.Add(hLine);
            }
        }

        protected override void DrawThis(SKCanvas canvas) {
            canvas.DrawRect(this.gWindow, this.boarderPaint);
        }

        protected override void Invalidate(WorldSpaceCoordinate worldCoordinate) {
            this.window = worldCoordinate.Window;
            this.gWindow = worldCoordinate.TransformToDeviceRect(this.window);

            this.CalculateGridInWindow();
        }

        public override bool ContainsPoint(SKPoint point) {
            return this.window.Contains(point);
        }
    }
}
