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
        #endregion

        public WorldSpaceCoordinate WorldSpace { get; set; } = new WorldSpaceCoordinate();
        public DrawingMode Mode { get; set; } = DrawingMode.Circle;

        public ConfigScene() {
            this.Root = new ConfigSceneRootObject(this);
            this.Dispatcher = new EventDispatcher<ICanvasObject>() {
                Root = this.Root,
            };

            // Register event handlers of dispatcher
            this.Dispatcher.TargetUnlocked += this.Dispatcher_TargetUnlocked;

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

            if (e.Type == "KeyDown") {
                switch (e.KeyCode) {
                    case Keys.L:
                        this.Mode = DrawingMode.Line;
                        break;
                    case Keys.C:
                        this.Mode = DrawingMode.Circle;
                        this.AddCircleObject();
                        break;
                    case Keys.Escape:
                        this.Mode = DrawingMode.None;
                        break;
                }
            }
        }

        private void AddCircleObject() {
            var circle = new CircleObject_v1() {
                Center = this.Dispatcher.Pointer,
            };

            circle.SetParent(this.Root as CanvasObject_v1);
            circle.Scene = this;
            this.Root.Children.Add(circle);

            circle.ChangeState("AddToBehaviors");
            this.Dispatcher.Lock(circle);
        }
    }

    public class ConfigSceneRootObject : RootObject_v1 {

        public ConfigSceneRootObject(IScene scene) : base(scene) {
        }

        protected override void RootObject_v1_MouseDoubleClick(Event @event) {
            var e = @event as MouseEvent;

            switch (e.AddtionalParameters as DrawingMode?) {
                case DrawingMode.Circle:
                    var circle = new CircleObject_v1() {
                        Center = e.Pointer,
                    };

                    circle.SetParent(this);
                    circle.Scene = this.Scene;
                    this.Children.Add(circle);

                    circle.ChangeState("AddToBehaviors");
                    this.Dispatcher.Lock(circle);
                    //circle.StartAddToBehavior();
                    break;
                case DrawingMode.Line:
                    var lineSeg = new LineSegmentObject_v1();

                    lineSeg.SetParent(this);
                    lineSeg.Scene = this.Scene;
                    lineSeg.P0.Point = e.Pointer;
                    this.Children.Add(lineSeg);

                    lineSeg.StartAddToBehavior();
                    break;
                case DrawingMode.None:
                    break;
            }

            //base.RootObject_v1_MouseDoubleClick(@event);
        }
    }
}
