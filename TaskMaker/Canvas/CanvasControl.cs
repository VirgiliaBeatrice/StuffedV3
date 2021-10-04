using MathNet.Numerics.LinearAlgebra;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TaskMaker {
    public partial class CanvasControl : UserControl {
        public Modes SelectedMode {
            get => selectedMode;
            set {
                selectedMode = value;

                //ModeChanged?.Invoke(this, null);
            }
        }

        //public Layer SelectedLayer {
        //    get => canvas.SelectedLayer;
        //    set {
        //        canvas.SelectedLayer = value;

        //        LayerFocused?.Invoke(this, new LayerFocusedEventArgs { Layer = canvas.SelectedLayer });
        //    }
        //}

        //public Layer RootLayer => canvas.RootLayer;
        public Canvas Canvas => canvas;
        //public Services Services => (this.ParentForm as TaskMakerForm).Services;

        public Layer SelectedLayer => canvas.SelectedLayer;

        private SKGLControl skControl;
        private SKMatrix translate = SKMatrix.Empty;
        private SKPoint prevMid;
        private SKImageInfo imageInfo;
        private Canvas canvas;
        private Modes selectedMode;
        private Timer timer;

        //public event EventHandler<LayerFocusedEventArgs> LayerFocused;
        //public event EventHandler ModeChanged;
        //public event EventHandler<InterpolatingEventArgs> Interpolated;

        private EditPhase editPhase = EditPhase.None;

        public CanvasControl() {
            InitializeComponent();

            skControl = new SKGLControl();
            skControl.Location = new Point(0, 0);
            skControl.Dock = DockStyle.Fill;

            Controls.AddRange(new Control[] {
                skControl
            });

            skControl.PaintSurface += SkControl_PaintSurface;
            skControl.MouseClick += SkControl_MouseClick;
            skControl.MouseDown += SkControl_MouseDown;
            skControl.MouseUp += SkControl_MouseUp;
            skControl.MouseMove += SkControl_MouseMove;
            skControl.Resize += SkControl_Resize;
            //this.skControl.KeyDown += this.SkControl_KeyDown;
            //this.skControl.KeyPress += this.SkControl_KeyPress;
            //this.skControl.MouseEnter += this.SkControl_MouseEnter;

            prevMid = new SKPoint(skControl.ClientSize.Width / 2, skControl.ClientSize.Height / 2);

            canvas = new Canvas();

            timer = new Timer();
            timer.Enabled = true;
            timer.Interval = 1;
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e) {
            skControl.Invalidate();
        }

        private void SkControl_Resize(object sender, EventArgs e) {
            //var mid = new SKPoint(this.skControl.ClientSize.Width / 2, this.skControl.ClientSize.Width / 2);
            //this.translate.TransX = mid.X - prevMid.X;
            //this.translate.TransY = mid.Y - prevMid.Y;

            //prevMid = mid;
        }

        public void Reset() {
            canvas.Reset();
            selectedMode = Modes.None;
        }

        public bool Triangulate() {
            return canvas.Triangulate();
        }

        public void BeginAddNodeMode() {
            canvas.IsShownPointer = true;
            SelectedMode = Modes.AddNode;
        }

        public void EndAddNodeMode() {
            canvas.Reset();
            SelectedLayer.Invalidate();
        }

        public void BeginEditMode() {
            SelectedMode = Modes.EditNode;
            editPhase = EditPhase.Select;
        }

        public void EndEditMode() {
            canvas.Reset();
            SelectedLayer.Invalidate();
        }

        public void BeginNoneMode() {
            Reset();
            SelectedMode = Modes.None;
            editPhase = EditPhase.None;
        }

        public void BeginManipulateMode() {
            SelectedMode = Modes.Manipulate;
        }

        public void BeginSelectionMode() {
            SelectedMode = Modes.Selection;
        }

        public void BeginPointerTrace(Point position) {
            canvas.PointerTrace = new PointerTrace(position.ToSKPoint());
            canvas.IsShownPointerTrace = true;
        }

        public void EndPointerTrace() {
            canvas.Reset();
        }

        public void AddLayer(Layer layer) {
            canvas.Layers.Add(layer);
        }

        public void RemoveLayer(Layer layer) {
            canvas.Layers.Remove(layer);
            
            if (canvas.Layers.Count != 0)
                canvas.Layers[0].IsSelected = true;

            //if (SelectedLayer == canvas.Layers[0]) {
            //    MessageBox.Show("Root layer could not be deleted.");

            //    return;
            //}

            //var parentNode = SelectedLayer.Parent;
            //var idxInParentNode = parentNode.Nodes.IndexOf(SelectedLayer);
            //var childNodes = SelectedLayer.Nodes.Cast<Layer>().ToArray();
            //parentNode.Nodes.Remove(canvas.SelectedLayer);
            //parentNode.Nodes.AddRange(childNodes);
            //SelectedLayer = parentNode as Layer;
        }

        public void RemoveSelectedNodes() {
            var selectedEntities = SelectedLayer.Entities.Where(e => e.IsSelected);

            foreach (var entity in selectedEntities.ToArray()) {
                SelectedLayer.Entities.Remove(entity);
                SelectedLayer.Complex.Clear();
            }
        }

        public void ChooseLayer(Layer layer) {
            canvas.Layers.ForEach(l => l.IsSelected = false);
            layer.IsSelected = true;

            SelectedLayer.Invalidate();
        }

        public void Pair() {
            if (SelectedLayer.BindedTarget == null) {
                MessageBox.Show("Layer without target.",
                    "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selectedEntities = canvas.SelectedLayer.Entities.FindAll(e => e.IsSelected);

            if (selectedEntities.Count > 1) {
                MessageBox.Show("More than one entity are selected.",
                    "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (selectedEntities.Count == 1) {
                selectedEntities[0].TargetState = SelectedLayer.BindedTarget.CreateTargetState();

                SelectedLayer.Invalidate();
            }
            else { }
        }


        public void Unpair() {
            var selectedEnities = canvas.SelectedLayer.Entities.FindAll(e => e.IsSelected);

            selectedEnities.ForEach(e => e.TargetState = null);
            //selectedEnities.ForEach(e => e.Pair.RemovePair());
            SelectedLayer.Invalidate();
        }

        private void SkControl_MouseDown(object sender, MouseEventArgs e) {
            switch (SelectedMode) {
                case Modes.Selection:
                    ProcessSelectionMouseDownEvent(e);
                    break;
                case Modes.Manipulate:
                    ProcessManipulatMouseDownEvent(e);
                    break;
                case Modes.EditNode:
                    ProcessEditNodeMouseDownEvent(e);
                    break;
            }

            //this.Invalidate(true);
        }


        private void SkControl_MouseMove(object sender, MouseEventArgs e) {
            switch (SelectedMode) {
                case Modes.Selection:
                    ProcessSelectionMouseMoveEvent(e);
                    break;
                case Modes.Manipulate:
                    ProcessManipulateMouseMoveEvent(e);
                    break;
                case Modes.EditNode:
                    ProcessEditNodeMouseMoveEvent(e);
                    break;
            }

            canvas.Pointer.Location = e.Location.ToSKPoint();

            Invalidate(true);
        }

        private void SkControl_MouseClick(object sender, MouseEventArgs e) {
            switch (SelectedMode) {
                case Modes.None:
                    ProcessGeneralMouseClickEvent(e);
                    break;
                case Modes.AddNode:
                    ProcessAddNodeMouseClickEvent(e);
                    break;
                case Modes.EditNode:
                    //this.ProcessEditNodeMouseClickEvent(e);
                    break;
            }

            //this.Invalidate(true);
        }

        private void SkControl_MouseUp(object sender, MouseEventArgs e) {
            switch (SelectedMode) {
                case Modes.Selection:
                    ProcessSelectionMouseUpEvent(e);
                    break;
                case Modes.Manipulate:
                    ProcessManipulateMouseUpEvent(e);
                    break;
                case Modes.EditNode:
                    ProcessEditNodeMouseUpEvent(e);
                    break;
            }

            //this.Invalidate(true);
        }

        private void ProcessEditNodeMouseUpEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                if (editPhase == EditPhase.Select) {
                    var entities = SelectedLayer.Entities;

                    foreach (var entity in entities) {
                        if (entity.ContainsPoint(ev.Location.ToSKPoint())) {
                            entity.IsSelected = !entity.IsSelected;
                            break;
                        }
                    }

                    editPhase = EditPhase.Edit;
                }
                else if (editPhase == EditPhase.Edit) {
                    canvas.Reset();

                    editPhase = EditPhase.Select;
                }
            }
        }

        private void ProcessEditNodeMouseMoveEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                var selectedEntities = SelectedLayer.Entities.FindAll(e => e.IsSelected);

                if (selectedEntities.Count != 0) {
                    selectedEntities[0].Location = ev.Location.ToSKPoint();
                }
            }
        }


        private void ProcessEditNodeMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                if (editPhase == EditPhase.Select) {
                    var entities = SelectedLayer.Entities;

                    foreach (var entity in entities) {
                        if (entity.ContainsPoint(ev.Location.ToSKPoint())) {
                            entity.IsSelected = !entity.IsSelected;
                            break;
                        }
                    }

                    editPhase = EditPhase.Edit;
                }
                else if (editPhase == EditPhase.Edit) {
                    var selectedEntities = SelectedLayer.Entities.FindAll(e => e.IsSelected);

                    if (selectedEntities.Count != 0) {
                        if (!selectedEntities[0].ContainsPoint(ev.Location.ToSKPoint())) {
                            canvas.Reset();

                            editPhase = EditPhase.Select;
                        }
                    }
                }
            }

        }

        private void ProcessManipulatMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                BeginPointerTrace(ev.Location);
            }
        }

        private void ProcessManipulateMouseMoveEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                //this.canvas.SelectedLayer.Interpolate(e.Location.ToSKPoint());
                canvas.SelectedLayer.Interpolate(e.Location.ToSKPoint());
                canvas.PointerTrace.Update(e.Location.ToSKPoint());
            }
        }


        private void ProcessManipulateMouseUpEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                EndPointerTrace();
            }
        }


        private void SkControl_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e) {
            e.Surface.Canvas.GetDeviceClipBounds(out SKRectI bounds);

            imageInfo = new SKImageInfo(bounds.Width, bounds.Height);
            //e.Surface.Canvas.Translate(this.translate.TransX, this.translate.TransY);
            //this.translate = SKMatrix.Empty;
            Draw(e.Surface.Canvas);

            //var start = new SKPoint(-50, 10);
            //var direction = new SKPoint(600, 450) - start;
            //var ray = new Ray_v3(start, direction);
            //ray.Draw(e.Surface.Canvas);
        }

        public void SaveAsImage() {
            using (var bitmap = new SKBitmap(imageInfo.Width, imageInfo.Height)) {
                var canvas = new SKCanvas(bitmap);

                canvas.Clear(SKColors.White);

                Draw(canvas);

                var image = SKImage.FromBitmap(bitmap);

                var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)}\\TaskMaker\\";

                Directory.CreateDirectory(path);
                var fileName = $"Canvas_{SelectedLayer.Name.Replace(" ", "")}";
                fileName = File.Exists(path + fileName + ".png") ? fileName + "_d" : fileName;

                using (var stream = File.Create(path + fileName + ".png")) {
                    var data = image.Encode(SKEncodedImageFormat.Png, 100);

                    data.SaveTo(stream);
                }
            }
        }

        protected virtual void Draw(SKCanvas sKCanvas) {
            sKCanvas.Clear(SKColors.White);

            canvas.Draw(sKCanvas);
        }

        private void ProcessSelectionMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                canvas.Reset();

                canvas.SelectionTool = new LassoSelectionTool(ev.Location.ToSKPoint());
            }
            else if (ev.Button == MouseButtons.Right) {
                canvas.Reset();

                canvas.SelectionTool = new RectSelectionTool(ev.Location.ToSKPoint());
            }
        }

        private void ProcessSelectionMouseMoveEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                var newLocation = ev.Location.ToSKPoint();
                canvas.SelectionTool.Trace(newLocation);
            }
            else if (ev.Button == MouseButtons.Right) {
                var newLocation = ev.Location.ToSKPoint();
                canvas.SelectionTool.Trace(newLocation);
            }
        }

        private void ProcessSelectionMouseUpEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                foreach (var e in canvas.SelectedLayer.Entities) {
                    if (canvas.SelectionTool.Contains(e.Location)) {
                        e.IsSelected = true;
                    }
                }

                canvas.SelectionTool.End();
            }
            else if (ev.Button == MouseButtons.Right) {
                foreach (var e in canvas.SelectedLayer.Entities) {
                    if (canvas.SelectionTool.Contains(e.Location)) {
                        e.IsSelected = true;
                    }
                }

                canvas.SelectionTool.End();
            }
        }

        private void ProcessGeneralMouseClickEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                canvas.Reset();

                foreach (var e in canvas.SelectedLayer.Entities) {
                    if (e.ContainsPoint(ev.Location.ToSKPoint())) {
                        e.IsSelected = !e.IsSelected;
                        break;
                    }
                }
            }
        }

        private void ProcessAddNodeMouseClickEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                canvas.SelectedLayer.Entities.Add(new Entity(e.Location.ToSKPoint()) {
                    Index = canvas.SelectedLayer.Entities.Count,
                });

                Services.Caretaker.Do(canvas);
            }

            // Quit from current mode after adding one entity
            //this.SelectedMode = Modes.None;

            canvas.Reset();
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

namespace PCController {
    public partial class Motor {
        public int NewOffset { get; set; } = 0;
    }
}

