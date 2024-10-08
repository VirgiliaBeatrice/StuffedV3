﻿using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TuggingController {
    public class ChartScene : IScene {
        public ICanvasObject Root { get; set; }
        public bool IsForDataValidation { get; set; } = false;
        // TODO: https://stackoverflow.com/questions/943171/expose-and-raise-event-of-a-child-control-in-a-usercontrol-in-c-sharp
        public event EventHandler<CanvasTargetChangedEventArgs> CanvasTargetChanged;
        public event EventHandler<EventArgs> CanvasObjectChanged;
        public event EventHandler<DataValidatedEventArgs> DataValidated {
            add {
                this.Dispatcher.DataValidated += value;
            }
            remove {
                this.Dispatcher.DataValidated -= value;
            }
        }

        public EventDispatcher<ICanvasObject> Dispatcher { get; set; }

        public WorldSpaceCoordinate WorldSpace { get; set; } = new WorldSpaceCoordinate();

        private float scale = 1.0f;
        private float zoomFactor = 1.5f;
        private bool isZooming = false;

        public ChartScene() : base() {
            this.Root = new RootObject_v1(this);
            this.Dispatcher = new EventDispatcher<ICanvasObject>() {
                Root = this.Root,
                Scene = this,
            };

            this.Root.Dispatcher.CanvasTargetChanged += this.Dispatcher_CanvasTargetChanged;
            this.Root.Dispatcher.CanvasObjectChanged += this.Dispatcher_CanvasObjectChanged;
        }

        private void Dispatcher_CanvasObjectChanged(object sender, EventArgs e) {
            this.OnCanvasObjectChanged(e);
        }

        private void Dispatcher_CanvasTargetChanged(object sender, CanvasTargetChangedEventArgs e) {
            this.OnCanvasTargetChanged(e);
        }
        
        public void Invoke(string eventName, EventArgs args) {
            var eventRaiseMethod = typeof(EventDispatcher<ICanvasObject>).GetEvent(eventName)?.GetRaiseMethod();

            eventRaiseMethod?.Invoke(this.Dispatcher, new object[] { args });
        }


        public void OnCanvasTargetChanged(CanvasTargetChangedEventArgs e) {
            this.CanvasTargetChanged?.Invoke(this, e);
        }

        public void OnCanvasObjectChanged(EventArgs e) {
            this.CanvasObjectChanged?.Invoke(this, e);
        }

        public void Update(SKCanvas canvas) {
            this.Root.Draw(canvas, this.WorldSpace);
        }
        
        protected void UpdateSceneScale() {
            SKMatrix scale2 = SKMatrix.MakeScale(this.scale, this.scale);

            var oldWindow = this.WorldSpace.Window;
            var translation1 = SKMatrix.MakeTranslation(-oldWindow.MidX, -oldWindow.MidY);
            var translation2 = SKMatrix.MakeTranslation(oldWindow.MidX, oldWindow.MidY);
            var transform = SKMatrix.MakeIdentity();

            SKMatrix.PostConcat(ref transform, translation1);
            SKMatrix.PostConcat(ref transform, scale2);
            SKMatrix.PostConcat(ref transform, translation2);

            var nWindow = transform.MapRect(oldWindow);
            this.WorldSpace.Window = new SKRect { Bottom = nWindow.Top, Top = nWindow.Bottom, Left = nWindow.Left, Right = nWindow.Right };
        }

        public void Dispatch(Event @event) {
            if (@event as MouseEvent != null) {
                var e = @event as MouseEvent;
                var sPointer = new SKPoint(e.X, e.Y);
                e.Pointer = this.WorldSpace.TransformToWorld(sPointer);
                e.ForDataValidation = this.IsForDataValidation;

                this.Dispatcher.DispatchMouseEvent(e);
            }
            else if (@event as ZoomEvent != null) {
                var e = @event as ZoomEvent;
                var delta = e.Delta;

                if (delta < 0) {
                    this.scale = zoomFactor;
                }
                else {
                    this.scale = 1.0f / zoomFactor;
                }

                this.UpdateSceneScale();
            }
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
            var entity = new MenuItem("Entity");

            entity.Click += (sender, e) => {
                this.AddEntity();
            };

            contextMenu.MenuItems.Add(entity);

            return contextMenu;
        }

        private void AddEntity() {
            var entity = new Entity_v1() {
                Point = this.Dispatcher.Pointer,
            };
            entity.Scene = this;

            entity.InitStateVector();

            (this.Root as RootObject_v1).AddEntity(entity);
        }
    }
}
