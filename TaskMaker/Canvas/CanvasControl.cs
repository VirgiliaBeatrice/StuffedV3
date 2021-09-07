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
            get => this.selectedMode;
            set {
                this.selectedMode = value;

                this.ModeChanged?.Invoke(this, null);
            }
        }

        public Layer SelectedLayer {
            get => this.canvas.SelectedLayer;
            set {
                this.canvas.SelectedLayer = value;

                this.LayerFocused?.Invoke(this, new LayerFocusedEventArgs { Layer = this.canvas.SelectedLayer });
            }
        }

        public Layer RootLayer => this.canvas.RootLayer;
        public Canvas Canvas => this.canvas;


        protected SKControl skControl;
        private SKImageInfo imageInfo;
        private Canvas canvas;
        private Modes selectedMode;

        public event EventHandler LayerUpdated;
        public event EventHandler LayerConfigured;
        public event EventHandler<LayerFocusedEventArgs> LayerFocused;
        public event EventHandler ModeChanged;
        public event EventHandler<InterpolatingEventArgs> Interpolated;

        private EditPhase editPhase = EditPhase.None;

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
            //this.skControl.KeyDown += this.SkControl_KeyDown;
            //this.skControl.KeyPress += this.SkControl_KeyPress;
            //this.skControl.MouseEnter += this.SkControl_MouseEnter;

            this.canvas = new Canvas();
        }

        public void Reset() {
            this.canvas.Reset();
            this.selectedMode = Modes.None;
        }

        public bool Triangulate() {
            return this.canvas.Triangulate();
        }

        public void BeginAddNodeMode() {
            this.canvas.IsShownPointer = true;
            this.SelectedMode = Modes.AddNode;
        }

        public void EndAddNodeMode() {
            this.canvas.Reset();
        }

        public void BeginEditMode() {
            this.SelectedMode = Modes.EditNode;
            this.editPhase = EditPhase.Select;
        }

        public void EndEditMode() {
            this.canvas.Reset();
        }

        public void BeginNoneMode() {
            this.Reset();
            this.SelectedMode = Modes.None;
            this.editPhase = EditPhase.None;
        }

        public void BeginManipulateMode() {
            this.SelectedMode = Modes.Manipulate;
        }

        public void BeginSelectionMode() {
            this.SelectedMode = Modes.Selection;
        }

        public void BeginPointerTrace(Point position) {
            this.canvas.PointerTrace = new PointerTrace(position.ToSKPoint());
            this.canvas.IsShownPointerTrace = true;
        }

        public void EndPointerTrace() {
            this.canvas.Reset();
        }

        public void AddLayer() {
            var name = this.SelectedLayer == this.RootLayer ?
                $"New Layer {this.SelectedLayer.Nodes.Count + 1}" :
                $"{this.SelectedLayer.Text} {this.SelectedLayer.Nodes.Count + 1}";

            this.SelectedLayer.Nodes.Add(new Layer(name));
            this.LayerUpdated?.Invoke(this, null);
        }

        public void RemoveLayer() {
            if (this.SelectedLayer == this.RootLayer) {
                MessageBox.Show("Root layer could not be deleted.");

                return;
            }

            var parentNode = this.SelectedLayer.Parent;
            var idxInParentNode = parentNode.Nodes.IndexOf(this.SelectedLayer);
            var childNodes = this.SelectedLayer.Nodes.Cast<Layer>().ToArray();
            parentNode.Nodes.Remove(this.canvas.SelectedLayer);
            parentNode.Nodes.AddRange(childNodes);
            this.SelectedLayer = parentNode as Layer;

            this.LayerUpdated?.Invoke(this, null);

            this.Invalidate(true);
        }

        public void RemoveSelectedNodes() {
            var selectedEntities = this.SelectedLayer.Entities.Where(e => e.IsSelected);

            foreach(var entity in selectedEntities.ToArray()) {
                this.SelectedLayer.Entities.Remove(entity);
                this.SelectedLayer.Complex.Clear();
            }
        }

        public void ChooseLayer(Layer layer) {
            this.SelectedLayer = layer;
        }

        public void Pair() {
            var selectedEnities = this.canvas.SelectedLayer.Entities.FindAll(e => e.IsSelected);

            if (this.canvas.SelectedLayer.MotorConfigs != null) {
                if (selectedEnities.Count == 1) {
                    selectedEnities[0].Pair.AddPair(this.canvas.SelectedLayer.MotorConfigs.ToVector(this.canvas.SelectedLayer.MotorConfigs));
                }

                return;
            }

            if (this.canvas.SelectedLayer.LayerConfigs != null) {
                if (selectedEnities.Count == 1) {
                    selectedEnities[0].Pair.AddPair(this.canvas.SelectedLayer.LayerConfigs.ToVector(this.canvas.SelectedLayer.LayerConfigs));
                }

                return;
            }

            MessageBox.Show("This layer did not bind with any configuration.");
        }

        private void SkControl_MouseDown(object sender, MouseEventArgs e) {
            switch (this.SelectedMode) {
                case Modes.Selection:
                    this.ProcessSelectionMouseDownEvent(e);
                    break;
                case Modes.Manipulate:
                    this.ProcessManipulatMouseDownEvent(e);
                    break;
                case Modes.EditNode:
                    this.ProcessEditNodeMouseDownEvent(e);
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
                case Modes.EditNode:
                    this.ProcessEditNodeMouseMoveEvent(e);
                    break;
            }

            this.canvas.Pointer.Location = e.Location.ToSKPoint();

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
                case Modes.EditNode:
                    //this.ProcessEditNodeMouseClickEvent(e);
                    break;
            }

            this.Invalidate(true);
        }

        private void SkControl_MouseUp(object sender, MouseEventArgs e) {
            switch (this.SelectedMode) {
                case Modes.Selection:
                    this.ProcessSelectionMouseUpEvent(e);
                    break;
                case Modes.Manipulate:
                    this.ProcessManipulateMouseUpEvent(e);
                    break;
                case Modes.EditNode:
                    this.ProcessEditNodeMouseUpEvent(e);
                    break;
            }

            this.Invalidate(true);
        }

        private void ProcessEditNodeMouseUpEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                if (this.editPhase == EditPhase.Select) {
                    var entities = this.SelectedLayer.Entities;

                    foreach (var entity in entities) {
                        if (entity.ContainsPoint(ev.Location.ToSKPoint())) {
                            entity.IsSelected = !entity.IsSelected;
                            break;
                        }
                    }

                    this.editPhase = EditPhase.Edit;
                }
                else if (this.editPhase == EditPhase.Edit) {
                    this.canvas.Reset();

                    this.editPhase = EditPhase.Select;
                }
            }
        }

        private void ProcessEditNodeMouseMoveEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                var selectedEntities = this.SelectedLayer.Entities.FindAll(e => e.IsSelected);

                if (selectedEntities.Count != 0) {
                    selectedEntities[0].Location = ev.Location.ToSKPoint();
                }
            }
        }


        private void ProcessEditNodeMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                if (this.editPhase == EditPhase.Select) {
                    var entities = this.SelectedLayer.Entities;

                    foreach (var entity in entities) {
                        if (entity.ContainsPoint(ev.Location.ToSKPoint())) {
                            entity.IsSelected = !entity.IsSelected;
                            break;
                        }
                    }

                    this.editPhase = EditPhase.Edit;
                }
                else if (this.editPhase == EditPhase.Edit) {
                    var selectedEntities = this.SelectedLayer.Entities.FindAll(e => e.IsSelected);

                    if (selectedEntities.Count != 0) {
                        if (!selectedEntities[0].ContainsPoint(ev.Location.ToSKPoint())) {
                            this.canvas.Reset();

                            this.editPhase = EditPhase.Select;
                        }
                    }
                }
            }

        }

        private void ProcessManipulatMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                this.BeginPointerTrace(ev.Location);
            }
        }

        private void ProcessManipulateMouseMoveEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this.canvas.SelectedLayer.Interpolate(e.Location.ToSKPoint());
                this.canvas.PointerTrace.Update(e.Location.ToSKPoint());

                //var pointer = this.canvas.SelectedLayer.Pointer;
                //var layer = this.canvas.SelectedLayer;

                //pointer.Location = e.Location.ToSKPoint();

                //// Get lambdas from current simplicial complex[Task => Bary.]
                //var lambdas = this.canvas.SelectedLayer.Complex.GetLambdas(pointer.Location);

                //// Get interpolated result from input on task space [Task => Config]
                //var configVector = this.canvas.SelectedLayer.Complex.GetConfigVectors(pointer.Location);

                //if (layer.LayerStatus == LayerStatus.WithMotor) {
                //    var configs = layer.MotorConfigs;

                //    configs.FromVector(configs, configVector);
                //}

                //if (layer.LayerStatus == LayerStatus.WithLayer) {
                //    var configs = layer.LayerConfigs;

                //    configs.FromVector(configs, configVector);
                //}

                //if (.LayerStatus == LayerStatus.WithMotor) {
                //    this.canvas.SelectedLayer.MotorConfigs.FromVector(this.canvas.SelectedLayer.MotorConfigs, configVector);
                //} else if (this.canvas.SelectedLayer.LayerStatus == LayerStatus.WithLayer) {
                //    this.canvas.SelectedLayer.LayerConfigs.FromVector(this.canvas.SelectedLayer.LayerConfigs, configVector);

                //    foreach(var layer in this.canvas.SelectedLayer.LayerConfigs) {
                //        if (layer.LayerStatus == LayerStatus.WithMotor) {
                //            var newVector = layer.Complex.GetConfigVectors(layer.Pointer.Location);
                //            layer.MotorConfigs.FromVector(layer.MotorConfigs, newVector);
                //        }
                //    }
                //}


                //this.Interpolated?.Invoke(
                //    this,
                //    new InterpolatingEventArgs() {
                //        Values = lambdas
                //    });

                //this._canvas.Simplices.ForEach(sim => Console.WriteLine(sim.GetLambdas(e.Location.ToSKPoint())));
            }
        }

        private void ProcessManipulateMouseUpEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this.EndPointerTrace();
            }
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

            this.canvas.Draw(sKCanvas);
        }

        private void ProcessSelectionMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                this.canvas.Reset();

                this.canvas.SelectionTool = new LassoSelectionTool(ev.Location.ToSKPoint());
            }
            else if (ev.Button == MouseButtons.Right) {
                this.canvas.Reset();

                this.canvas.SelectionTool = new RectSelectionTool(ev.Location.ToSKPoint());
            }
        }

        private void ProcessSelectionMouseMoveEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                var newLocation = ev.Location.ToSKPoint();
                this.canvas.SelectionTool.Trace(newLocation);
            }
            else if (ev.Button == MouseButtons.Right) {
                var newLocation = ev.Location.ToSKPoint();
                this.canvas.SelectionTool.Trace(newLocation);
            }
        }

        private void ProcessSelectionMouseUpEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                foreach (var e in this.canvas.SelectedLayer.Entities) {
                    if (this.canvas.SelectionTool.Contains(e.Location)) {
                        e.IsSelected = true;
                    }
                }

                this.canvas.SelectionTool.End();
            }
            else if (ev.Button == MouseButtons.Right) {
                foreach (var e in this.canvas.SelectedLayer.Entities) {
                    if (this.canvas.SelectionTool.Contains(e.Location)) {
                        e.IsSelected = true;
                    }
                }

                this.canvas.SelectionTool.End();
            }
        }

        private void ProcessGeneralMouseClickEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                this.canvas.Reset();
                
                foreach (var e in this.canvas.SelectedLayer.Entities) {
                    if (e.ContainsPoint(ev.Location.ToSKPoint())) {
                        e.IsSelected = !e.IsSelected;
                        break;
                    }
                }
            }
        }

        private void ProcessAddNodeMouseClickEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this.canvas.SelectedLayer.Entities.Add(new Entity_v2(e.Location.ToSKPoint()) {
                    Index = this.canvas.SelectedLayer.Entities.Count,
                });
            }

            // Quit from current mode after adding one entity
            //this.SelectedMode = Modes.None;

            this.canvas.Reset();
        }
    }

    public class LayerFocusedEventArgs : EventArgs {
        public Layer Layer { get; set; }
    }

    public class InterpolatingEventArgs : EventArgs {
        public Vector<float> Values { get; set; }
    }

    public enum EditPhase {
        None,
        Select,
        Edit,
    }
}
