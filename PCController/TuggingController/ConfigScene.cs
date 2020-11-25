using Reparameterization;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TuggingController {

    public enum DrawingMode {
        Circle,
        Line,
        None
    }

    public class ConfigScene : IScene {

        #region PropertiesNoDefaultInitialization
        public ICanvasObject Root { get; set; }
        public EventDispatcher<ICanvasObject> Dispatcher { get; set; }
        public event EventHandler<CanvasTargetChangedEventArgs> CanvasTargetChanged;
        public event EventHandler<EventArgs> CanvasObjectChanged;
        public event EventHandler<DataValidatedEventArgs> DataValidated;
        #endregion

        public WorldSpaceCoordinate WorldSpace { get; set; } = new WorldSpaceCoordinate();
        public DrawingMode Mode { get; set; } = DrawingMode.Circle;

        public ConfigScene() {
            this.Root = new ConfigSceneRootObject(this);
            this.Dispatcher = new EventDispatcher<ICanvasObject>() {
                Root = this.Root,
                Scene = this,
            };

            // Register event handlers of dispatcher
            //this.Dispatcher.TargetUnlocked += this.Dispatcher_TargetUnlocked;

        }

        private void Dispatcher_TargetUnlocked(object sender, EventArgs e) {
            var target = (e as TargetUnlockedEventArgs).Target as CircleObject_v1;

            target.ChangeState("DefaultBehaviors");
        }

        public void Update(SKCanvas canvas) {
            this.Root.Draw(canvas, this.WorldSpace);
        }

        public void Dispatch(Event @event) {
            if (@event as MouseEvent != null) {
                var e = @event as MouseEvent;
                var sPointer = new SKPoint(e.X, e.Y);
                e.Pointer = this.WorldSpace.TransformToWorld(sPointer);
                e.AddtionalParameters = this.Mode;
                
                this.Dispatcher.DispatchMouseEvent(e);
            }

            if (@event as KeyEvent != null) {
                var e = @event as KeyEvent;
                e.AddtionalParameters = this.Mode;

                this.DispatchSceneEvent(e);
                //this.Dispatcher.DispatchKeyEvent(e);
            }
        }

        public void DispatchSceneEvent(Event @event) {
            var e = @event as KeyEvent;

            if (e.Type == "KeyDown") { }
        }

        public ContextMenu GenerateClickContextMenu(IEnumerable<object> targets, Event @event) {
            var contextMenu = new ContextMenu();

            foreach (var target in targets) {
                var item = new MenuItem(target.ToString());

                item.Click += (sender, e) => {
                    this.Dispatcher.Capture(target as CanvasObject_v1);

                    (target as CanvasObject_v1).OnMouseClick(@event);
                };

                contextMenu.MenuItems.Add(item);
            }

            return contextMenu;
        }

        public ContextMenu GenerateDbClickContextMenu() {
            var contextMenu = new ContextMenu();
            var circle = new MenuItem("Circle");
            var line = new MenuItem("Line");

            circle.Click += (sender, e) => {
                this.AddCircleObject();
            };
            line.Click += (sender, e) => {
                this.AddLineSegmentObject();
            };

            contextMenu.MenuItems.Add(circle);
            contextMenu.MenuItems.Add(line);

            return contextMenu;
        }

        private void AddCircleObject() {
            var circle = new CircleObject_v1() {
                Center = this.Dispatcher.Pointer,
            };

            circle.SetParent(this.Root as CanvasObject_v1);
            circle.Scene = this;
            this.Root.Children.Add(circle);

            circle.ChangeState("AddToBehaviors");
            this.Dispatcher.Capture(circle);
        }

        private void AddLineSegmentObject() {
            var lineSeg = new LineSegmentObject_v1();

            lineSeg.SetParent(this.Root as CanvasObject_v1);
            lineSeg.Scene = this;
            lineSeg.P0.Point = this.Dispatcher.Pointer;
            this.Root.Children.Add(lineSeg);

            lineSeg.ChangeState("AddToBehaviors");
            this.Dispatcher.Capture(lineSeg);
        }

        public ConfigurationVector PackData() {
            var children = this.Root.Children;
            var dataArray = new List<float>();
            
            foreach (var child in children) {
                if ((child as CircleObject_v1) != null) {
                    dataArray.Add((child as CircleObject_v1).Center.X);
                    dataArray.Add((child as CircleObject_v1).Center.Y);
                }
                if ((child as LineSegmentObject_v1) != null) {
                    dataArray.Add((child as LineSegmentObject_v1).P0.Point.X);
                    dataArray.Add((child as LineSegmentObject_v1).P0.Point.Y);
                    dataArray.Add((child as LineSegmentObject_v1).P1.Point.X);
                    dataArray.Add((child as LineSegmentObject_v1).P1.Point.Y);
                }
            }

            return new ConfigurationVector(dataArray);
        }

        public void UnpackData(ConfigurationVector config) {
            if (config == null) {
                return;
            }

            var children = this.Root.Children;
            var idx = 0;

            foreach(var child in children) {
                if ((child as CircleObject_v1) != null) {
                    (child as CircleObject_v1).Center = new SKPoint() {
                        X = config.Vector[idx],
                        Y = config.Vector[idx + 1],
                    };

                    idx += 2;
                }
                if ((child as LineSegmentObject_v1) != null) {
                    (child as LineSegmentObject_v1).P0.Point = new SKPoint() {
                        X = config.Vector[idx],
                        Y = config.Vector[idx + 1],
                    };

                    idx += 2;

                    (child as LineSegmentObject_v1).P1.Point = new SKPoint() {
                        X = config.Vector[idx],
                        Y = config.Vector[idx + 1],
                    };

                    idx += 2;
                }
            }
        }
    }

    public class ConfigSceneRootObject : RootObject_v1 {

        public ConfigSceneRootObject(IScene scene) : base(scene) {
        }
    }
}
