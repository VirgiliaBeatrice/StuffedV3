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
using SkiaSharp.Views.Desktop;

namespace TuggingController {
    public partial class CanvasControl : UserControl {
        public Layer RootLayer { get; set; } = new Layer("RootLayer");
        public Layer SelectedLayer { get; set; }
        public Modes SelectedMode { get; set; } = Modes.None;

        protected SKControl skControl;
        private Canvas _canvas;
        

        public CanvasControl() {
            InitializeComponent();

            this.SelectedLayer = this.RootLayer;

            this.skControl = new SKControl();
            this.skControl.Location = new Point(0, 0);
            this.skControl.Dock = DockStyle.Fill;

            this.Controls.AddRange(new Control[] {
                this.skControl
            });

            this.skControl.PaintSurface += this.SkControl_PaintSurface;
            this.skControl.MouseClick += this.SkControl_MouseClick;
            this.skControl.MouseDown += this.SkControl_MouseDown;
            this.skControl.MouseUp += this.SkControl_MouseUp;
            this.skControl.MouseMove += this.SkControl_MouseMove;
            this.skControl.KeyDown += this.SkControl_KeyDown;
            //this.skControl.KeyPress += this.SkControl_KeyPress;
            this.skControl.MouseEnter += this.SkControl_MouseEnter;

            this._canvas = new Canvas();
        }

        private void SkControl_MouseUp(object sender, MouseEventArgs e) {
            switch (this.SelectedMode) {
                case Modes.Selection:
                    this.ProcessSelectionMouseUpEvent(e);
                    break;
            }

            this.Invalidate(true);
        }

        private void SkControl_MouseEnter(object sender, EventArgs e) {
            this.skControl.Focus();
        }

        //private void SkControl_KeyPress(object sender, KeyPressEventArgs e) {
        //    throw new NotImplementedException();
        //}

        private void SkControl_MouseDown(object sender, MouseEventArgs e) {
            switch (this.SelectedMode) {
                case Modes.Selection:
                    this.ProcessSelectionMouseDownEvent(e);
                    break;

            }

            this.Invalidate(true);
        }

        private void SkControl_KeyDown(object sender, KeyEventArgs ev) {
            switch(ev.KeyCode) {
                case Keys.A:
                    this.SelectedMode = Modes.AddNode;
                    break;
                case Keys.Escape:
                    this.SelectedMode = Modes.None;
                    // Reset states of all entities
                    foreach(var e in this._canvas.Entities) {
                        e.IsSelected = false;
                    }
                    break;
                case Keys.T:
                    if (!this._canvas.Triangulate()) {
                        MessageBox.Show("Amount of nodes is less than 3. Abort.");
                    }
                    break;
                case Keys.S:
                    this.SelectedMode = Modes.Selection;
                    break;
            }

            this.Invalidate(true);
        }

        private void SkControl_MouseMove(object sender, MouseEventArgs e) {
            switch(this.SelectedMode) {
                case Modes.Selection:
                    this.ProcessSelectionMouseMoveEvent(e);
                    break;
            }

            this.Invalidate(true);
        }

        private void SkControl_MouseClick(object sender, MouseEventArgs e) {
            switch(this.SelectedMode) {
                case Modes.None:
                    this.ProcessGeneralMouseClickEvent(e);
                    break;
                case Modes.AddNode:
                    this.ProcessAddNodeMouseClickEvent(e);
                    break;
            }

            this.Invalidate(true);
        }

        private void SkControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e) {
            this.Draw(e.Surface.Canvas);
        }

        protected virtual void Draw(SKCanvas sKCanvas) {
            sKCanvas.Clear();

            this._canvas.Draw(sKCanvas);
        }

        private void ProcessSelectionMouseMoveEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                var newLocation = ev.Location.ToSKPoint();
                this._canvas.SelectionTool.Trace(newLocation);
            }
            else if (ev.Button == MouseButtons.Right) {
                var newLocation = ev.Location.ToSKPoint();
                this._canvas.SelectionTool.Trace(newLocation);
            }
        }

        private void ProcessSelectionMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                this._canvas.Reset();
                
                this._canvas.SelectionTool = new LassoSelectionTool(ev.Location.ToSKPoint());
            }
            else if (ev.Button == MouseButtons.Right) {
                this._canvas.Reset();

                this._canvas.SelectionTool = new RectSelectionTool(ev.Location.ToSKPoint());
            }
        }

        private void ProcessSelectionMouseUpEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                foreach(var e in this._canvas.Entities) {
                    if (this._canvas.SelectionTool.Contains(e.Location)) {
                        e.IsSelected = true;
                    }
                }

                this._canvas.SelectionTool.End();
            }
            else if (ev.Button == MouseButtons.Right) {
                foreach (var e in this._canvas.Entities) {
                    if (this._canvas.SelectionTool.Contains(e.Location)) {
                        e.IsSelected = true;
                    }
                }

                this._canvas.SelectionTool.End();
            }
        }

        private void ProcessGeneralMouseClickEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                foreach(var e in this._canvas.Entities) {
                    if (e.ContainsPoint(ev.Location.ToSKPoint())) {
                        e.IsSelected = !e.IsSelected;
                    }
                }
            }
        }

        private void ProcessAddNodeMouseClickEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this._canvas.Entities.Add(new Entity_v2() {
                    Index = this._canvas.Entities.Count,
                    Point = e.Location.ToSKPoint(),
                });
            }

            // Quit from current mode after adding one entity
            this.SelectedMode = Modes.None;
        }
    }
}
