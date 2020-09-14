﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Extended;
using SkiaSharp.Views.Desktop;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace TuggingController {
    [Flags]
    public enum MouseStates {
        NONE = 0b_0000_0000,
        LEFTDOWN = 0b_0000_0001,
        MOVE = 0b_1000_0000,
        LEFTDRAGGING = LEFTDOWN | MOVE,
        MOVEMASK = ~MOVE,
    }
    public partial class ChartControl : UserControl {
        private MouseStates _mouseState = MouseStates.NONE;
        private SKControl skControl;
        private Timer timer = new Timer();
        private int timerCount = 0;
        private float prevScale = 0.0f;
        private bool _IsScaleUp = true;

        //public Chart_v1 Chart { get; set; }
        public ChartScene ChartScene { get; set; }

        public ChartControl() {
            InitializeComponent();

            this.skControl = new SKControl();
            this.skControl.Location = new Point(0, 0);
            this.skControl.Size = new Size(this.Size.Width, this.Size.Height);

            this.timer.Interval = 10;
            this.timer.Tick += this.Timer_Tick;

            this.Controls.AddRange(new Control[] {
                this.skControl,
            });

            this.ChartScene = new ChartScene();
            this.ChartScene.WorldSpace.Device = new SKRect() {
                Left = -this.Size.Width / 2.0f,
                Right = this.Size.Width / 2.0f,
                Top = this.Size.Height / 2.0f,
                Bottom = -this.Size.Height / 2.0f
            };
            this.ChartScene.WorldSpace.Window = new SKRect() {
                Left = -this.Size.Width / 2.0f,
                Right = this.Size.Width / 2.0f,
                Top = this.Size.Height / 2.0f,
                Bottom = -this.Size.Height / 2.0f
            };

            this.skControl.KeyPress += this.SkControl_KeyPress;
            this.skControl.PaintSurface += this.SKControl_PaintSurface;
            this.skControl.MouseDown += this.SkControl_MouseDown;
            this.skControl.MouseUp += this.SkControl_MouseUp;
            this.skControl.MouseClick += this.SkControl_MouseClick;
            this.skControl.MouseDoubleClick += this.SkControl_MouseDoubleClick;
            this.skControl.MouseMove += this.SkControl_MouseMove;

            this.skControl.MouseWheel += this.SkControl_MouseWheel;

            this.SizeChanged += this.ChartControl_SizeChanged;
            this.KeyPress += this.ChartControl_KeyPress;

            this.Invalidate(true);
        }

        private void SkControl_MouseWheel(object sender, MouseEventArgs e) {
            if (ModifierKeys == Keys.Control) {
                var mouseEvent = new MouseEvent("MouseWheel") {
                    PointerX = e.X,
                    PointerY = e.Y,
                    Button = e.Button,
                    Delta = e.Delta,
                    ModifierKey = Keys.Control
                };

                this.ChartScene.Dispatch(mouseEvent);
            }

            this.Invalidate(true);
        }

        private void SkControl_MouseClick(object sender, MouseEventArgs e) {
            var mouseEvent = new MouseEvent("MouseClick") {
                PointerX = e.X,
                PointerY = e.Y,
                Button = e.Button,
            };

            //this.Chart.Dispatcher.DispatchEvent(mouseEvent);
        }

        private void SkControl_MouseUp(object sender, MouseEventArgs e) {
            var mouseEvent = new MouseEvent("MouseUp") {
                PointerX = e.X,
                PointerY = e.Y,
                Button = e.Button,
            };

            this.ChartScene.Dispatch(mouseEvent);
            this.Invalidate(true);
        }

        private void SkControl_MouseMove(object sender, MouseEventArgs e) {
            var mouseEvent = new MouseEvent("MouseMove") {
                PointerX = e.X,
                PointerY = e.Y,
                Button = e.Button,
            };

            this.ChartScene.Dispatch(mouseEvent);
            this.Invalidate(true);
        }

        private void SkControl_MouseDown(object sender, MouseEventArgs e) {
            var mouseEvent = new MouseEvent("MouseDown") {
                PointerX = e.X,
                PointerY = e.Y,
                Button = e.Button,
            };

            this.ChartScene.Dispatch(mouseEvent);
            this.Invalidate(true);
        }

        private void SkControl_MouseDoubleClick(object sender, MouseEventArgs e) {
            var mouseEvent = new MouseEvent("MouseDoubleClick") {
                PointerX = e.X,
                PointerY = e.Y,
                Button = e.Button,
            };

            this.ChartScene.Dispatch(mouseEvent);
            this.Invalidate(true);
        }

        private void Timer_Tick(object sender, EventArgs e) {
            decimal d = (decimal)((this._IsScaleUp ? 1.0f : -1.0f)
                * this.SmoothStep(0.0f, 10.0f, this.timerCount) * 0.1f);
            var scale = (decimal)this.prevScale + d;

            if (this.timerCount < 10) {
                ++this.timerCount;
            } else {
                this.timerCount = 0;
                this.timer.Stop();
            }

            //this.Chart.SetScale((float)scale);
            this.Invalidate(true);
        }

        // https://en.wikipedia.org/wiki/Smoothstep
        private float SmoothStep(float edge0, float edge1, float x) {
            // Scale, bias and saturate x to 0..1 range
            x = (x - edge0) / (edge1 - edge0);

            if (x <= 0.0f) {
                x = 0.0f;
            } else if (x >= 1.0f) {
                x = 1.0f;
            }

            // Evaluate polynomial
            return x * x * (3 - 2 * x);
        }


        private void SkControl_KeyPress(object sender, KeyPressEventArgs e) {
            this.OnKeyPress(e);
        }

        private void ChartControl_KeyPress(object sender, KeyPressEventArgs e) {
            Console.WriteLine(e.KeyChar);

            switch(e.KeyChar) {
                case '+':
                    this.OnKeyPlusPressed();
                    break;
                case '-':
                    this.OnKeyMinusPressed();
                    break;
            }
        }

        private void OnKeyPlusPressed() {
            //this.Chart.Scale += 0.1f;
            //this.prevScale = this.Chart.GetScale();

            if (this.prevScale < 10.0f) {
                this._IsScaleUp = true;
                this.timer.Start();
            }
        }

        private void OnKeyMinusPressed() {
            //this.Chart.Scale -= 0.1f;
            //this.prevScale = this.Chart.GetScale();

            if (this.prevScale >= 0.6f) {
                this._IsScaleUp = false;
                this.timer.Start();
            }
        }

        private void ChartControl_SizeChanged(object sender, EventArgs e) {
            //this.Chart.Transform.Scale = SKMatrix.MakeScale(1.0f, -1.0f);
            //this.Chart.Transform.Translation = SKMatrix.MakeTranslation(0, this.Size.Height);
            //this.Chart.Transform.Translation =
            //    SKMatrix.MakeTranslation(this.Size.Width / 2, this.Size.Height / 2);

            //this.Chart.SetSize(new SKSize(this.Size.Width, this.Size.Height));
            this.skControl.Size = new Size(this.Size.Width, this.Size.Height);
            this.SetDeviceViewport();

            this.Invalidate(true);
        }

        private void SKControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e) {
            this.Draw(e.Surface.Canvas);
        }

        private void Draw(SKCanvas canvas) {
            canvas.Clear();

            //this.Chart.Draw(canvas);
            this.ChartScene.Update(canvas);
            //canvas.DrawText("Hello World!", new SKPoint(100, 100), new SKPaint() { Color = SKColors.Black });

        }

        private void SetDeviceViewport() {
            this.ChartScene.WorldSpace.Device = new SKRect() {
                Left = -this.Size.Width / 2.0f,
                Right = this.Size.Width / 2.0f,
                Top = this.Size.Height / 2.0f,
                Bottom = -this.Size.Height / 2.0f
            };

            //var deviceWindow = new SKRect() {
            //    Left = -this.Size.Width / 2.0f,
            //    Right = this.Size.Width / 2.0f,
            //    Top = this.Size.Height / 2.0f,
            //    Bottom = -this.Size.Height / 2.0f
            //};

            //this.ChartScene.WorldSpace.Window = deviceWindow;
        }
    }
}
