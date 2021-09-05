using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MathNet.Numerics.LinearAlgebra;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace TaskMaker {
    public partial class CanvasControl : UserControl {
        public Modes SelectedMode {
            get => this._selectedMode;
            set {
                this._selectedMode = value;

                this.ModeChanged?.Invoke(this, null);
            }
        }

        protected SKControl skControl;
        private SKImageInfo imageInfo;
        private Canvas _canvas;
        private Modes _selectedMode;

        public event EventHandler LayerUpdated;
        public event EventHandler ModeChanged;
        public event EventHandler<InterpolatingEventArgs> Interpolated;

        public CanvasControl() {
            InitializeComponent();

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

        public void StartPointerTrace(Point position) {
            this._canvas.PointerTrace = new PointerTrace(position.ToSKPoint());
            this._canvas.IsShownPointerTrace = true;
        }

        public void EndPointerTrace() {
            this._canvas.Reset();
        }

        public Layer GetCurrentSelectedLayer() {
            return this._canvas.SelectedLayer;
        }
        public void AddLayer() {
            this._canvas.SelectedLayer.Nodes.Add(new Layer());
            this.LayerUpdated?.Invoke(this, null);
        }

        public void RemoveLayer() {
            this._canvas.RootLayer.Nodes.Remove(this._canvas.SelectedLayer);
            this.LayerUpdated?.Invoke(this, null);
        }

        public TreeNode GetRootLayer() {
            return this._canvas.RootLayer;
        }
        public void ChangeLayer(TreeNode node) {
            this._canvas.SelectedLayer = (Layer)node;
        }

        private void SkControl_MouseUp(object sender, MouseEventArgs e) {
            switch (this.SelectedMode) {
                case Modes.Selection:
                    this.ProcessSelectionMouseUpEvent(e);
                    break;
                case Modes.Manipulate:
                    this.ProcessManipulateMouseUpEvent(e);
                    break;
            }

            this.Invalidate(true);
        }


        private void SkControl_MouseEnter(object sender, EventArgs e) {
            this.skControl.Focus();
        }


        private void SkControl_MouseDown(object sender, MouseEventArgs e) {
            switch (this.SelectedMode) {
                case Modes.Selection:
                    this.ProcessSelectionMouseDownEvent(e);
                    break;
                case Modes.Manipulate:
                    this.ProcessManipulatMouseDownEvent(e);
                    break;

            }

            this.Invalidate(true);
        }

        private void ProcessManipulatMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                this.StartPointerTrace(ev.Location);
            }
        }

        private void ProcessManipulateMouseMoveEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this._canvas.PointerTrace.Update(e.Location.ToSKPoint());
                var lambdas = this._canvas.SelectedLayer.Complex.GetLambdas(e.Location.ToSKPoint());

                this.Interpolated?.Invoke(
                    this,
                    new InterpolatingEventArgs() {
                        Values = lambdas
                    });

                //this._canvas.Simplices.ForEach(sim => Console.WriteLine(sim.GetLambdas(e.Location.ToSKPoint())));
            }
        }

        private void ProcessManipulateMouseUpEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this.EndPointerTrace();
            }
        }


        private void SkControl_KeyDown(object sender, KeyEventArgs ev) {
            switch (ev.KeyCode) {
                case Keys.A:
                    this.SelectedMode = Modes.AddNode;
                    this._canvas.IsShownPointer = true;
                    break;
                case Keys.Escape:
                    this.SelectedMode = Modes.None;
                    this._canvas.Reset();
                    break;
                case Keys.T:
                    if (!this._canvas.Triangulate()) {
                        MessageBox.Show("Amount of nodes is less than 3. Abort.");
                    }
                    break;
                case Keys.S:
                    this.SelectedMode = Modes.Selection;
                    break;
                case Keys.P:
                    Form form = new Form();
                    //TargetSelection control = new TargetSelection();
                    //control.Dock = DockStyle.Fill;
                    //form.Size = new Size(600, 600);

                    //form.Controls.Add(control);
                    //form.Show();
                    break;
                case Keys.M:
                    this.SelectedMode = Modes.Manipulate;
                    break;
            }

            this.Invalidate(true);
        }

        private void SkControl_MouseMove(object sender, MouseEventArgs e) {
            switch (this.SelectedMode) {
                case Modes.Selection:
                    this.ProcessSelectionMouseMoveEvent(e);
                    break;
                case Modes.Manipulate:
                    this.ProcessManipulateMouseMoveEvent(e);
                    break;
            }

            this._canvas.Pointer.Location = e.Location.ToSKPoint();

            this.Invalidate(true);
        }



        private void SkControl_MouseClick(object sender, MouseEventArgs e) {
            switch (this.SelectedMode) {
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
            this.imageInfo = e.Info;
            this.Draw(e.Surface.Canvas);
        }

        public void SaveAsImage(string path) {
            using (var bitmap = new SKBitmap(imageInfo.Width, imageInfo.Height)) {
                var canvas = new SKCanvas(bitmap);

                canvas.Clear();

                this.Draw(canvas);

                var image = SKImage.FromBitmap(bitmap);

                using (var stream = File.Create(path)) {
                    var data = image.Encode(SKEncodedImageFormat.Png, 100);

                    data.SaveTo(stream);
                }
            }
        }

        protected virtual void Draw(SKCanvas sKCanvas) {
            sKCanvas.Clear();

            this._canvas.Draw(sKCanvas);
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

        private void ProcessSelectionMouseUpEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                foreach (var e in this._canvas.SelectedLayer.Entities) {
                    if (this._canvas.SelectionTool.Contains(e.Location)) {
                        e.IsSelected = true;
                    }
                }

                this._canvas.SelectionTool.End();
            }
            else if (ev.Button == MouseButtons.Right) {
                foreach (var e in this._canvas.SelectedLayer.Entities) {
                    if (this._canvas.SelectionTool.Contains(e.Location)) {
                        e.IsSelected = true;
                    }
                }

                this._canvas.SelectionTool.End();
            }
        }

        private void ProcessGeneralMouseClickEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                foreach (var e in this._canvas.SelectedLayer.Entities) {
                    if (e.ContainsPoint(ev.Location.ToSKPoint())) {
                        e.IsSelected = !e.IsSelected;
                    }
                }
            }
        }

        private void ProcessAddNodeMouseClickEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this._canvas.SelectedLayer.Entities.Add(new Entity_v2() {
                    Index = this._canvas.SelectedLayer.Entities.Count,
                    Location = e.Location.ToSKPoint(),
                });
            }

            // Quit from current mode after adding one entity
            this.SelectedMode = Modes.None;

            this._canvas.Reset();
        }
    }

    public class InterpolatingEventArgs : EventArgs {
        public Vector<float> Values { get; set; }
    }
}
