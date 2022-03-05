using MathNet.Numerics.LinearAlgebra;
using Numpy;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TaskMaker.SimplicialMapping;
//using TaskMaker.SkiaExtension;
//using MathNetExtension;

namespace TaskMaker {
    public partial class CanvasControl : UserControl {
        public Modes SelectedMode {
            get => selectedMode;
            set {
                selectedMode = value;
            }
        }
        public ControlUIWidget SelectedControlUI => _view.SelectedControlUI;
        public event EventHandler<MessageEventArgs> Interpolated;

        private SKGLControl skControl;
        private SKImageInfo imageInfo;
        private Modes selectedMode;
        private Timer _refreshTimer;
        private ViewWidget _view => Services.ViewWidget;
        private EditPhase editPhase = EditPhase.None;

        private SKRect _viewport;
        private SKRect _window;
        private SKPoint _panCenterInView;
        private SKPoint _panStartInWorld;
        private Stopwatch _watch = new Stopwatch();
        private int _count = 100;
        private BackgroundWorker worker;

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

            //prevMid = new SKPoint(skControl.ClientSize.Width / 2, skControl.ClientSize.Height / 2);

            _refreshTimer = new Timer();
            _refreshTimer.Enabled = true;
            _refreshTimer.Interval = 1;
            _refreshTimer.Tick += Timer_Tick;

            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            ResetViewport();
            //Services.Canvas.Bounds = _viewport;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            //var (result, layer) = ((double[], Layer))e.Result;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e) {
            //if (!(sender as BackgroundWorker).IsBusy) {
                var (layer, location) = ((ControlUIWidget, SKPoint))e.Argument;

                e.Result = (layer.Interpolate(location), layer);
            //}
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
            _view.Reset();
            selectedMode = Modes.None;
        }

        private void ResetViewport() {
            _window = new SKRect() { Location = new SKPoint(), Size = ClientSize.ToSKSize() };
            _viewport = new SKRect() { Location = new SKPoint(), Size = ClientSize.ToSKSize() };
            Services.ViewWidget.Bounds = _viewport;
        }

        public void BeginAddNodeMode() {
            _view.IsShownPointer = true;
            SelectedMode = Modes.AddNode;

            SelectedControlUI.Complex.Clear();
            SelectedControlUI.Exterior = null;
        }

        public void EndAddNodeMode() {
            _view.Reset();
            SelectedControlUI.Invalidate();
        }

        public void BeginEditMode() {
            SelectedMode = Modes.EditNode;
            editPhase = EditPhase.Select;
        }

        public void EndEditMode() {
            _view.Reset();
            SelectedControlUI.Invalidate();
        }

        public void BeginNoneMode() {
            Reset();
            SelectedMode = Modes.None;
            editPhase = EditPhase.None;
        }

        public void BeginManipulateMode() {
            // Set mode = manipulate
            SelectedMode = Modes.Manipulate;

            // Show controllers in complexes
            SelectedControlUI.ShowController();
        }

        public void BeginSelectionMode() {
            SelectedMode = Modes.Selection;

            SelectedControlUI.HideController();
        }

        public void BeginPointerTrace(Point position) {
            _view.PointerTrace = new PointerTrace(position.ToSKPoint());
            _view.IsShownPointerTrace = true;
        }

        public void EndPointerTrace() {
            _view.Reset();
        }

        public void AddLayer(ControlUIWidget layer) {
            _view.Layers.Add(layer);
        }

        public void RemoveLayer(ControlUIWidget layer) {
            _view.Layers.Remove(layer);
            
            if (_view.Layers.Count != 0)
                _view.Layers[0].IsSelected = true;

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
            var selectedEntities = SelectedControlUI.Entities.Where(e => e.IsSelected);

            foreach (var entity in selectedEntities.ToArray()) {
                SelectedControlUI.Entities.Remove(entity);
                SelectedControlUI.Complex.Clear();
            }
        }

        public void ChooseLayer(ControlUIWidget layer) {
            _view.Layers.ForEach(l => l.IsSelected = false);
            layer.IsSelected = true;

            SelectedControlUI.Invalidate();
        }

        public bool PairingStart = true;

        //public void PairWithNLinearMap(NLinearMap map) {
        //    if (SelectedLayer.BindedTarget == null) {
        //        MessageBox.Show("Layer without target.",
        //            "Error",
        //            MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        return;
        //    }

        //    if (PairingStart) {
        //        PairingStart = false;

        //        map.SetComponent();

        //        var content = $"Pairing start from:\r\n" +
        //            $"Entities - ({string.Join(",", map.CurrentCursor)})";
        //        //var content = $"Pairing start from: {SelectedLayer.Entities[0]}";

        //        MessageBox.Show($"Next: {content}",
        //        "Information",
        //        MessageBoxButtons.OK, MessageBoxIcon.Information);
        //        return;
        //    }
        //    else {
        //        var target = SelectedLayer.BindedTarget;
        //        var lastCursor = map.CurrentCursor;
        //        var isSet = map.SetComponent(target.CreateTargetState().ToVector().ToArray());

        //        if (!isSet) {
        //            var currCursor = map.CurrentCursor;
        //            SelectedLayer.Reset();

        //            var content = $"Entities - ({string.Join(",", lastCursor)}) are set.\r\n" +
        //                $"Next: Entities - ({string.Join(",", currCursor)})";

        //            MessageBox.Show($"{content}",
        //            "Information",
        //            MessageBoxButtons.OK, MessageBoxIcon.Information);
        //        }
        //        else {
        //            SelectedLayer.Reset();
        //            PairingStart = true;

        //            MessageBox.Show("All entites are paired.",
        //            "Information",
        //            MessageBoxButtons.OK, MessageBoxIcon.Information);
        //        }
        //    }
        //}

        public void PairAll() {
            if (SelectedControlUI.BindedTarget == null) {
                MessageBox.Show("Layer without target.",
                    "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //if (PairingStart) {
            //    SelectedLayer.Entities[0].IsSelected = true;
            //    PairingStart = false;
            //    //SelectedLayer.Bary.BeginSetting(SelectedLayer.BindedTarget.Dimension);
            //    //SelectedLayer.Bary.BeginSetting(2);
            //    SelectedLayer.MultiBary.SetComponent();

            //    var content = $"Pairing start from: {SelectedLayer.Entities[0]}";

            //    MessageBox.Show($"Next: {content}",
            //    "Information",
            //    MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}
            //else {
            //    var bary = SelectedLayer.MultiBary;
            //    var target = SelectedLayer.BindedTarget;
            //    var cursor = bary.CurrentCursor;
            //    var isSet = bary.SetComponent(target.CreateTargetState().ToVector().ToArray());

            //    if (!isSet) {
            //        SelectedLayer.Reset();

            //        var content = $"{SelectedLayer.Entities[cursor[0]]} is set. \r\nNext: {SelectedLayer.Entities[cursor[0] + 1]}";
            //        MessageBox.Show($"{content}",
            //        "Information",
            //        MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    }
            //    else {
            //        SelectedLayer.Reset();
            //        PairingStart = true;

            //        MessageBox.Show("All entites are paired.",
            //        "Information",
            //        MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    }
            //}
        }

        public void Pair() {
            if (SelectedControlUI.BindedTarget == null) {
                MessageBox.Show("Layer without target.",
                    "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selectedEntities = SelectedControlUI.Entities.FindAll(e => e.IsSelected);

            if (selectedEntities.Count > 1) {
                MessageBox.Show("More than one entity are selected.",
                    "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (selectedEntities.Count == 1) {
                selectedEntities[0].TargetState = SelectedControlUI.BindedTarget.CreateTargetState();

                SelectedControlUI.Invalidate();
            }
            else { }
        }


        public void Unpair() {
            var selectedEnities = _view.SelectedControlUI.Entities.FindAll(e => e.IsSelected);

            selectedEnities.ForEach(e => e.TargetState = null);
            //selectedEnities.ForEach(e => e.Pair.RemovePair());
            SelectedControlUI.Invalidate();
        }

        private void SkControl_MouseDown(object sender, MouseEventArgs e) {
            if (ModifierKeys == Keys.Control) {
                if (e.Button == MouseButtons.Left) {
                    _panCenterInView = e.Location.ToSKPoint();
                    _panStartInWorld = _window.Location;
                }
            }
            else {
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
            }
        }


        private void SkControl_MouseMove(object sender, MouseEventArgs e) {
            if (ModifierKeys == Keys.Control) {
                if (e.Button == MouseButtons.Left) {
                    Pan(e.Location.ToSKPoint() - _panCenterInView);
                }
            } 
            else {
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

                _view.Pointer.Location = ViewportToWorld().MapPoint(e.Location.ToSKPoint());
            }
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
            if (ModifierKeys == Keys.Control) {
                if (e.Button == MouseButtons.Right) {
                    ResetViewport();
                }
            }
            else {
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
            }
        }

        private void ProcessEditNodeMouseUpEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                if (editPhase == EditPhase.Select) {
                    var entities = SelectedControlUI.Entities;
                    var wLocation = ViewportToWorld().MapPoint(ev.Location.ToSKPoint());

                    foreach (var entity in entities) {
                        if (entity.ContainsPoint(wLocation)) {
                            entity.IsSelected = !entity.IsSelected;
                            break;
                        }
                    }

                    editPhase = EditPhase.Edit;
                }
                else if (editPhase == EditPhase.Edit) {
                    _view.Reset();

                    editPhase = EditPhase.Select;
                }
            }
        }

        private void ProcessEditNodeMouseMoveEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                var selectedEntities = SelectedControlUI.Entities.FindAll(e => e.IsSelected);

                if (selectedEntities.Count != 0) {
                    selectedEntities[0].Location = ViewportToWorld().MapPoint(ev.Location.ToSKPoint());
                }
            }
        }


        private void ProcessEditNodeMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                if (editPhase == EditPhase.Select) {
                    var entities = SelectedControlUI.Entities;
                    var wLocation = ViewportToWorld().MapPoint(ev.Location.ToSKPoint());

                    foreach (var entity in entities) {
                        if (entity.ContainsPoint(wLocation)) {
                            entity.IsSelected = !entity.IsSelected;
                            break;
                        }
                    }

                    editPhase = EditPhase.Edit;
                }
                else if (editPhase == EditPhase.Edit) {
                    var selectedEntities = SelectedControlUI.Entities.FindAll(e => e.IsSelected);
                    var wLocation = ViewportToWorld().MapPoint(ev.Location.ToSKPoint());

                    if (selectedEntities.Count != 0) {
                        if (!selectedEntities[0].ContainsPoint(wLocation)) {
                            _view.Reset();

                            editPhase = EditPhase.Select;
                        }
                    }
                }
            }

        }

        private void ProcessManipulatMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                var wLocation = ViewportToWorld().MapPoint(ev.Location.ToSKPoint());

                //foreach(var l in SelectedLayer.TargetMap.Layers) {
                //    var sum = SKPoint.Empty;

                //    l.Entities.ForEach(e => sum = sum + e.Location);

                //    var centroid = new SKPoint(sum.X / l.Entities.Count, sum.Y / l.Entities.Count);

                //    l.Controller.Location = centroid;
                //}
                //BeginPointerTrace(new Point((int)wLocation.X, (int)wLocation.Y));
            }
        }

        private void ProcessManipulateMouseMoveEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var wLocation = ViewportToWorld().MapPoint(e.Location.ToSKPoint());

                if (worker.IsBusy)
                    return;
    
                worker.RunWorkerAsync((SelectedControlUI, wLocation));

                //var result = SelectedLayer.Interpolate(wLocation);

                //if (result == null)
                //    return;

                //Interpolated?.Invoke(null, new MessageEventArgs() { Message = np.array(result).repr });
            }
        }


        private void ProcessManipulateMouseUpEvent(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                //EndPointerTrace();
            }
        }


        private void SkControl_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e) {
            _watch.Stop();

            var time = _watch.ElapsedMilliseconds;
            //fps.Enqueue(_watch.ElapsedMilliseconds);
            var canvas = e.Surface.Canvas;
            var mat = WorldToViewport();

            e.Surface.Canvas.GetDeviceClipBounds(out SKRectI bounds);

            imageInfo = new SKImageInfo(bounds.Width, bounds.Height);

            canvas.Clear(SKColors.White);
            canvas.Concat(ref mat);
            Draw(e.Surface.Canvas);

            using (var text = new SKPaint()) {
                text.Color = SKColors.Black;
                text.TextSize = 12;

                canvas.DrawText($"FPS: {Math.Round(1000.0 / time, 2)}", new SKPoint(0, 12), text);
            }

            _watch.Reset();
            _watch.Start();
        }

        public void SaveAsImage() {
            using (var bitmap = new SKBitmap(imageInfo.Width, imageInfo.Height))
            using (var canvas = new SKCanvas(bitmap))
                {
                canvas.Clear(SKColors.White);

                Draw(canvas);
                canvas.Translate(SelectedControlUI.Bounds.MidX, SelectedControlUI.Bounds.MidY);
                //canvas.ClipRect(SelectedLayer.Bounds);

                using (var image = SKImage.FromBitmap(bitmap)) {
                    var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)}\\TaskMaker\\";

                    Directory.CreateDirectory(path);
                    var fileName = $"Canvas_{SelectedControlUI.Name.Replace(" ", "")}";
                    fileName = File.Exists(path + fileName + ".png") ? fileName + "_d" : fileName;

                    using (var stream = File.Create(path + fileName + ".png")) {
                        var data = image.Encode(SKEncodedImageFormat.Png, 100);

                        data.SaveTo(stream);
                    }
                }

            }
        }

        protected virtual void Draw(SKCanvas sKCanvas) {
            sKCanvas.Clear(SKColors.White);

            _view.Draw(sKCanvas);
        }

        private void ProcessSelectionMouseDownEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                _view.Reset();

                var wP = ViewportToWorld().MapPoint(ev.Location.ToSKPoint());
                _view.SelectionTool = new LassoSelectionTool(wP);
            }
            else if (ev.Button == MouseButtons.Right) {
                _view.Reset();

                var wP = ViewportToWorld().MapPoint(ev.Location.ToSKPoint());
                _view.SelectionTool = new RectSelectionTool(wP);
            }

            // Disable Context Menu
        }

        private void ProcessSelectionMouseMoveEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                var wLocation = ViewportToWorld().MapPoint(ev.Location.ToSKPoint());
                _view.SelectionTool.Trace(wLocation);
            }
            else if (ev.Button == MouseButtons.Right) {
                var wLocation = ViewportToWorld().MapPoint(ev.Location.ToSKPoint());
                _view.SelectionTool.Trace(wLocation);
            }
        }

        private void ProcessSelectionMouseUpEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                foreach (var e in _view.SelectedControlUI.Entities) {
                    if (_view.SelectionTool.Contains(e.Location)) {
                        e.IsSelected = true;
                    }
                }

                _view.SelectionTool.End();
            }
            else if (ev.Button == MouseButtons.Right) {
                foreach (var e in _view.SelectedControlUI.Entities) {
                    if (_view.SelectionTool.Contains(e.Location)) {
                        e.IsSelected = true;
                    }
                }

                _view.SelectionTool.End();
            }
        }

        private void ProcessGeneralMouseClickEvent(MouseEventArgs ev) {
            if (ev.Button == MouseButtons.Left) {
                _view.Reset();

                foreach (var e in _view.SelectedControlUI.Entities) {
                    var wP = ViewportToWorld().MapPoint(ev.Location.ToSKPoint());
                    if (e.ContainsPoint(wP)) {
                        e.IsSelected = !e.IsSelected;
                        break;
                    }
                }
            }
        }

        private void ProcessAddNodeMouseClickEvent(MouseEventArgs e) {

            if (e.Button == MouseButtons.Left) {
                Services.Caretaker.Do(SelectedControlUI);

                var wP = ViewportToWorld().MapPoint(e.Location.ToSKPoint());
                SelectedControlUI.Entities.Add(new Entity(wP) {
                    Index = SelectedControlUI.Entities.Count,
                });
            }

            // Quit from current mode after adding one entity
            //this.SelectedMode = Modes.None;

            _view.Reset();
        }

        private SKMatrix WorldToViewport() {
            var translate = SKMatrix.CreateTranslation(-_window.Left, -_window.Top);
            var scaleMat = SKMatrix.CreateScale(_viewport.Width / _window.Width, _viewport.Height / _window.Height);
            var translateInv = SKMatrix.CreateTranslation(_viewport.Left, _viewport.Top);

            // T_i * S * T
            return translate.PostConcat(scaleMat).PostConcat(translateInv);
        }

        private SKMatrix ViewportToWorld() {
            return WorldToViewport().Invert();
        }

        private void Pan(SKPoint offset) {
            _window.Location = _panStartInWorld - ViewportToWorld().MapVector(offset);
        }
    }

    public enum EditPhase {
        None,
        Select,
        Edit,
    }

    public class MessageEventArgs :  EventArgs {
        public object Message { get; set; } 
    }

    public class InterpolationWorker {
        public SKPoint Location { get; set; }
        public ControlUIWidget Layer { get; set; }
    }
}

namespace PCController {
    public partial class Motor {
        public int NewOffset { get; set; } = 0;
    }
}

