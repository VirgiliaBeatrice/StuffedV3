using System;
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
    public partial class ChartControl : UserControl {
        private Timer mouseDownTimer = new Timer();
        private int clicks = 0;
        private bool isDragging = false;
        private MouseEventArgs mouseDownEventArgs;

        protected SKControl skControl;
        protected Timer timer = new Timer();
        protected int timerCount = 0;
        protected float prevScale = 0.0f;
        protected bool _IsScaleUp = true;

        public event EventHandler<CanvasTargetChangedEventArgs> CanvasTargetChanged;
        public event EventHandler<EventArgs> CanvasObjectChanged;
        public event EventHandler<DataValidatedEventArgs> DataValidated {
            add {
                this.ChartScene.DataValidated += value;
            }
            remove {
                this.ChartScene.DataValidated -= value;
            }
        }

        public IScene ChartScene { get; set; }

        public ChartControl() {
            InitializeComponent();

            this.skControl = new SKControl();
            this.skControl.Location = new Point(0, 0);
            this.skControl.Size = new Size(this.Size.Width, this.Size.Height);

            this.timer.Interval = 10;
            this.timer.Tick += this.Timer_Tick;

            this.mouseDownTimer.Interval = 400;
            this.mouseDownTimer.Tick += this.mouseDownTimer_Tick;

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
            this.ChartScene.CanvasTargetChanged += this.ChartScene_CanvasTargetChanged;
            this.ChartScene.CanvasObjectChanged += this.ChartScene_CanvasObjectChanged;

            this.skControl.PaintSurface += this.SKControl_PaintSurface;
            this.skControl.MouseUp += this.SkControl_MouseUp;
            this.skControl.MouseDown += this.SkControl_MouseDown;
            this.skControl.MouseMove += this.SkControl_MouseMove;
            this.skControl.MouseWheel += this.SkControl_MouseWheel;
            this.skControl.KeyDown += this.SkControl_KeyDown;

            this.skControl.MouseEnter += this.SkControl_MouseEnter;

            this.SizeChanged += this.ChartControl_SizeChanged;
            this.Invalidate(true);
        }

        private void SkControl_MouseEnter(object sender, EventArgs e) {
            this.skControl.Focus();
        }

        protected void ChartScene_CanvasObjectChanged(object sender, EventArgs e) {
            this.OnCanvasObjectChanged(e);
        }

        protected void ChartScene_CanvasTargetChanged(object sender, CanvasTargetChangedEventArgs e) {
            this.OnCanvasTargetChanged(e);
        }

        public virtual void OnCanvasTargetChanged(CanvasTargetChangedEventArgs e) {
            this.CanvasTargetChanged?.Invoke(this, e);
        }

        public virtual void OnCanvasObjectChanged(EventArgs e) {
            this.CanvasObjectChanged?.Invoke(this, e);
        }


        protected void SkControl_MouseWheel(object sender, MouseEventArgs e) {
            if (ModifierKeys == Keys.Control) {
                var mouseEvent = new MouseEvent("MouseWheel") {
                    X = e.X,
                    Y = e.Y,
                    Button = e.Button,
                    Delta = e.Delta,
                    ModifierKey = Keys.Control,
                    Sender = this,
                    OriginalEventArgs = e,
                };

                this.ChartScene.Dispatch(mouseEvent);
            }

            this.Invalidate(true);
        }

        protected void SkControl_MouseDown(object sender, MouseEventArgs e) {
            if (!this.mouseDownTimer.Enabled) {
                this.mouseDownEventArgs = e;
                this.mouseDownTimer.Start();
            }

            var dragStartEvent = new MouseEvent("DragStart") {
                X = this.mouseDownEventArgs.X,
                Y = this.mouseDownEventArgs.Y,
                Button = this.mouseDownEventArgs.Button,
                Sender = this,
                OriginalEventArgs = this.mouseDownEventArgs,
            };
            this.isDragging = true;

            this.ChartScene.Dispatch(dragStartEvent);

            var mouseDownEvent = new MouseEvent("MouseDown") {
                X = e.X,
                Y = e.Y,
                Button = e.Button,
                Sender = this,
                OriginalEventArgs = e,
            };

            this.ChartScene.Dispatch(mouseDownEvent);
            this.Invalidate(true);
        }

        protected void SkControl_MouseUp(object sender, MouseEventArgs e) {
            this.clicks++;

            //if (this.isDragging) {
            var dragEndEvent = new MouseEvent("DragEnd") {
                X = e.X,
                Y = e.Y,
                Button = e.Button,
                Sender = this,
                OriginalEventArgs = e,
            };

            this.ChartScene.Dispatch(dragEndEvent);
            this.isDragging = false;
            //this.clicks = 0;
        //}
            //else {
            var mouseClickEvent = new MouseEvent("MouseClick") {
                X = e.X,
                Y = e.Y,
                Button = e.Button,
                Sender = this,
                OriginalEventArgs = e,
            };

            this.ChartScene.Dispatch(mouseClickEvent);
            //}

            var mouseUpEvent = new MouseEvent("MouseUp") {
                X = e.X,
                Y = e.Y,
                Button = e.Button,
                Sender = this,
                OriginalEventArgs = e,
            };

            this.ChartScene.Dispatch(mouseUpEvent);

            this.Invalidate(true);
        }

        protected void SkControl_MouseMove(object sender, MouseEventArgs e) {
            if (this.isDragging) {
                var draggingEvent = new MouseEvent("Dragging") {
                    X = e.X,
                    Y = e.Y,
                    Button = e.Button,
                    Delta = e.Delta,
                    Sender = this,
                    OriginalEventArgs = e,
                };

                this.ChartScene.Dispatch(draggingEvent);
            }
            else {
                var mouseEvent = new MouseEvent("MouseMove") {
                    X = e.X,
                    Y = e.Y,
                    Button = e.Button,
                    Delta = e.Delta,
                    Sender = this,
                    OriginalEventArgs = e,
                };

                this.ChartScene.Dispatch(mouseEvent);
            }

            //var objects = this.ChartScene.Root.


            this.Invalidate(true);
        }

        protected void SkControl_KeyDown(object sender, KeyEventArgs e) {


            //var keyEvent = new KeyEvent("KeyDown") {
            //    KeyCode = e.KeyCode,
            //};

            //Console.WriteLine($"KeyCode: {keyEvent.KeyCode}");

            //this.ChartScene.Dispatch(keyEvent);
            //this.Invalidate(true);
        }

        private void mouseDownTimer_Tick(object sender, EventArgs e) {
            this.mouseDownTimer.Stop();

            //if (this.clicks == 0) {
            //    var mouseEvent = new MouseEvent("DragStart") {
            //        X = this.mouseDownEventArgs.X,
            //        Y = this.mouseDownEventArgs.Y,
            //        Button = this.mouseDownEventArgs.Button,
            //        Sender = this,
            //        OriginalEventArgs = this.mouseDownEventArgs,
            //    };
            //    this.isDragging = true;

            //    this.ChartScene.Dispatch(mouseEvent);
            //}
            if (this.clicks == 2) {
                var mouseEvent = new MouseEvent("MouseDoubleClick") {
                    X = this.mouseDownEventArgs.X,
                    Y = this.mouseDownEventArgs.Y,
                    Button = this.mouseDownEventArgs.Button,
                    Sender = this,
                    OriginalEventArgs = this.mouseDownEventArgs,
                };

                this.ChartScene.Dispatch(mouseEvent);
                this.clicks = 0;
                this.mouseDownEventArgs = null;
            }
            else {
                this.clicks = 0;
                this.mouseDownEventArgs = null;
            }

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

        protected void ChartControl_SizeChanged(object sender, EventArgs e) {
            this.skControl.Size = new Size(this.Size.Width, this.Size.Height);
            this.SetDeviceViewport();

            this.Invalidate(true);
        }

        protected void SKControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e) {
            this.Draw(e.Surface.Canvas);
        }

        protected virtual void Draw(SKCanvas canvas) {
            canvas.Clear();

            this.ChartScene.Update(canvas);
            //canvas.DrawText("Hello World!", new SKPoint(100, 100), new SKPaint() { Color = SKColors.Black });
        }

        protected void SetDeviceViewport() {
            this.ChartScene.WorldSpace.Device = new SKRect() {
                Left = -this.Size.Width / 2.0f,
                Right = this.Size.Width / 2.0f,
                Top = this.Size.Height / 2.0f,
                Bottom = -this.Size.Height / 2.0f
            };

            var deviceWindow = new SKRect() {
                Left = -this.Size.Width / 2.0f,
                Right = this.Size.Width / 2.0f,
                Top = this.Size.Height / 2.0f,
                Bottom = -this.Size.Height / 2.0f
            };

            this.ChartScene.WorldSpace.Window = deviceWindow;
        }
    }
}
