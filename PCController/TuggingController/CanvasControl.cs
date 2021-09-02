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
        private SelectionTool _selectionTool;
        private Triangulation _triangulation;
        

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
            this._triangulation = new Triangulation();

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
                    var entities = this._canvas.Entities.Where(e => e.IsSelected).Select(e => new double[] { e.Location.X, e.Location.Y });
                    var flattern = new List<double>();

                    foreach(var e in entities) {
                        flattern.AddRange(e);
                    }

                    var input = flattern.ToArray();
                    var output = this._triangulation.RunDelaunay_v1(2, input.Length / 2, ref input);

                    foreach(var triIndices in output) {
                        var tri = new Entity_v2[] {
                            this._canvas.Entities[triIndices[0]],
                            this._canvas.Entities[triIndices[1]],
                            this._canvas.Entities[triIndices[2]]
                        };
                        this._canvas.Simplices.Add(new Simplex_v2(tri));
                    }
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
            this._selectionTool?.DrawThis(sKCanvas);
            this._canvas.Draw(sKCanvas);
        }

        private void ProcessSelectionMouseMoveEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                var newLocation = ev.Location.ToSKPoint();
                this._selectionTool.AddNode(newLocation);
                //this._selectionTool.Size = new SKSize(newLocation - this._selectionTool.Location);
            }
            else if (ev.Button == MouseButtons.Right) {
                //var 
            }
           
        }

        private void ProcessSelectionMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                this._selectionTool = new SelectionTool(ev.Location.ToSKPoint());
            }
        }

        private void ProcessSelectionMouseUpEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                foreach(var e in this._canvas.Entities) {
                    if (this._selectionTool.Contains(e.Location)) {
                        e.IsSelected = true;
                    }
                }

                this._selectionTool = null;
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
