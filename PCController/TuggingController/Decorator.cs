using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using SkiaSharp;

namespace TuggingController {

    /// <summary>
    /// Transform Component for CanvasObject
    /// </summary>
    public class Transform : IComponent {
        public static Transform WithScale(SKMatrix scale) {
            return new Transform() { Scale = scale };
        }

        public static Transform WithRotation(SKMatrix rotation) {
            return new Transform() { Rotation = rotation };
        }

        public static Transform WithTranslation(SKMatrix translation) {
            return new Transform() { Translation = translation };
        }

        // Nullable Property
        public Transform Parent { get; set; }
        public ICanvasObject CanvasObject { get; set; }
        public event EventHandler TransformChanged;

        // Initialized Property
        private SKMatrix _scale = SKMatrix.MakeIdentity();
        private SKMatrix _rotation = SKMatrix.MakeIdentity();
        private SKMatrix _translation = SKMatrix.MakeIdentity();
        public SKMatrix Scale {
            get => this._scale;
            set {
                this._scale = value;
                TransformChanged.Invoke(this, null);
            }
        }
        public SKMatrix Rotation {
            get => this._rotation;
            set {
                this._rotation = value;
                TransformChanged.Invoke(this, null);
            }
        }
        public SKMatrix Translation {
            get => this._translation;
            set {
                this._translation = value;
                TransformChanged.Invoke(this, null);
            }
        }

        // Order: T<-R<-S, Global(TRS)<-Local(TRS)
        public SKMatrix LocalTransformation {
            get {
                var mat = SKMatrix.MakeIdentity();

                SKMatrix.PostConcat(ref mat, this.Scale);
                SKMatrix.PostConcat(ref mat, this.Rotation);
                SKMatrix.PostConcat(ref mat, this.Translation);

                return mat;
            }
        }

        public SKMatrix LocalToWorldMatrix {
            get => this.GlobalTransformation;
        }

        public SKMatrix WorldToLocalMatrix {
            get => this.InvGlobalTransformation;
        }

        public SKMatrix GlobalTransformation {
            get {
                var transform = this.LocalTransformation;

                if (this.Parent != null) {
                    SKMatrix.PostConcat(ref transform, this.Parent.GlobalTransformation);
                }

                return transform;
            }
        }
        public SKMatrix InvLocalTransformation {
            get {
                this.LocalTransformation.TryInvert(out SKMatrix inv);

                return inv;
            }
        }
        public SKMatrix InvGlobalTransformation {
            get {
                this.GlobalTransformation.TryInvert(out SKMatrix inv);

                return inv;
            }
        }

        public string Tag => "Transform";

        public Transform() : this(SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity()) { }

        public Transform(SKMatrix scale, SKMatrix rotation, SKMatrix translation) {
            this.TransformChanged += this.Transform_TransformChanged;

            this.Scale = scale;
            this.Translation = translation;
            this.Rotation = rotation;

        }

        private void Transform_TransformChanged(object sender, EventArgs e) { }

        /// <summary>
        /// Transform a local coordinate to global coordinate.
        /// Local --> Global
        /// </summary>
        /// <param name="point">Local Coordinate</param>
        /// <returns>Global Coordinate</returns>
        public SKPoint MapPoint(SKPoint point) {
            //var tranform = this.Transformation;

            //if (this.Parent != null) {
            //    SKMatrix.PostConcat(ref tranform, this.Parent.Transformation);
            //}

            //return tranform.MapPoint(point);
            return this.GlobalTransformation.MapPoint(point);
        }
        /// <summary>
        /// Transform a global coordinate to a local point according to its
        /// parent transform matrix.
        /// Global --> Local
        /// </summary>
        /// <param name="point">Global Coordinate</param>
        /// <returns>Local Coordinate</returns>
        public SKPoint InverseMapPoint(SKPoint point) {
            var invTransform = this.InvLocalTransformation;

            if (this.Parent != null) {
                SKMatrix.PreConcat(ref invTransform, this.Parent.InvLocalTransformation);
            }

            return invTransform.MapPoint(point);
        }

        public SKRect MapRectFromParent(SKRect rect) {
            return this.LocalTransformation.MapRect(rect);
        }

        public SKRect MapRectFromGlobal(SKRect rect) {
            return this.GlobalTransformation.MapRect(rect);
        }

        public SKRect InverseMapRectToGlobal(SKRect rect) {
            return this.InvGlobalTransformation.MapRect(rect);
        }

        public SKRect InverseMapRectToParent(SKRect rect) {
            return this.InvLocalTransformation.MapRect(rect);
        }

        public SKPoint TransformToWorldPoint(SKPoint point) {
            return this.LocalToWorldMatrix.MapPoint(point);
        }

        public SKRect TransformToWorldRect(SKRect rect) {
            return this.LocalToWorldMatrix.MapRect(rect);
        }

        public SKPoint TransformToLocalPoint(SKPoint point) {
            return this.WorldToLocalMatrix.MapPoint(point);
        }

        public void DefaultBehavior(BehaviorArgs e) {
            throw new NotImplementedException();
        }

        public void PreventDefault(BehaviorHandler behavior) {
            throw new NotImplementedException();
        }

        public void AddBehavior(BehaviorHandler behavior) {
            throw new NotImplementedException();
        }
    }

    public class WorldSpaceCoordinate {
        private SKRect _window;
        private SKRect _viewport = new SKRect() {
            Left = -1.0f,
            Right = 1.0f,
            Top = 1.0f,
            Bottom = -1.0f
        };
        private SKRect _device;

        private ViewTranform _viewT = new ViewTranform();
        private ClipTransform _clipT = new ClipTransform();
        private DeviceTransform _deviceT = new DeviceTransform();

        public float AspectRatio => Math.Abs(this._device.Width / this._device.Height);

        public SKRect Window {
            get => this._window;
            set {
                this._window = value;

                this.UpdateClipTransform();
            }
        }
        public SKRect Device {
            get => this._device;
            set {
                this._device = value;

                //var aspectRatio = Math.Abs(value.Width / value.Height);

                //this._viewport.Left = this._viewport.Left * aspectRatio;
                //this._viewport.Right = this._viewport.Right * aspectRatio;

                //this._window.Left = this._window.Left * this.AspectRatio;
                //this._window.Right = this.Window.Right * this.AspectRatio;

                this.UpdateDeviceTransform();
            }
        }

        public SKMatrix WorldToDeviceTransform {
            get {
                var transform = SKMatrix.MakeIdentity();

                SKMatrix.PostConcat(ref transform, this._viewT.WorldToViewSpaceTransform);
                SKMatrix.PostConcat(ref transform, this._clipT.ViewToNormalizedClipSpaceTransform);
                SKMatrix.PostConcat(ref transform, this._deviceT.ClipToDeviceSpaceTransform);

                return transform;
            }
        }

        public SKMatrix DeviceToWorldTransform {
            get {
                this.WorldToDeviceTransform.TryInvert(out var matrix);

                return matrix;
            }
        }

        public SKMatrix WorldToClipTransform {
            get {
                var transform = SKMatrix.MakeIdentity();

                SKMatrix.PostConcat(ref transform, this._viewT.WorldToViewSpaceTransform);
                SKMatrix.PostConcat(ref transform, this._clipT.ViewToNormalizedClipSpaceTransform);

                return transform;
            }
        }

        public SKMatrix ClipToWorldTransform {
            get {
                this.WorldToClipTransform.TryInvert(out var matrix);

                return matrix;
            }
        }

        public WorldSpaceCoordinate() { }

        public WorldSpaceCoordinate(SKRect window, SKRect device) {
            this._window = window;
            this._device = device;

            this.UpdateClipTransform();
            this.UpdateDeviceTransform();
        }

        public void SetViewTranslation(SKMatrix translation) {
            this._viewT.Translation = translation;
        }

        private void UpdateClipTransform() {
            this._clipT.Translation = SKMatrix.MakeTranslation(-this._window.MidX, -this._window.MidY);
            this._clipT.Scale = SKMatrix.MakeScale(this._viewport.Width / this._window.Width, this._viewport.Height / this._window.Height);
        }

        private void UpdateDeviceTransform() {
            this._deviceT.Scale = SKMatrix.MakeScale(this._device.Width / 2.0f, this._device.Height / 2.0f);
            this._deviceT.Translation = SKMatrix.MakeTranslation(this._device.Width / 2.0f, -this._device.Height / 2.0f);
        }

        public SKPoint TransformToDevice(SKPoint point) {
            return this.WorldToDeviceTransform.MapPoint(point);
        }

        public SKRect TransformToDeviceRect(SKRect rect) {
            return this.WorldToDeviceTransform.MapRect(rect);
        }

        public SKPoint TransformToWorld(SKPoint point) {
            return this.DeviceToWorldTransform.MapPoint(point);
        }

        public uint GetPointViewportCode(SKPoint point) {
            var cPoint = this.WorldToClipTransform.MapPoint(point);
            uint result = 0b_0000;

            if (cPoint.X < this._viewport.Left) {
                result |= 0b_0001;
            } else if (cPoint.X > this._viewport.Right) {
                result |= 0b_0010;
            } else if (cPoint.Y > this._viewport.Top) {
                result |= 0b_0100;
            } else if (cPoint.Y < this._viewport.Bottom) {
                result |= 0b_1000;
            }

            return result;
        }
    }

    public class ViewTranform {
        private SKMatrix _translation = SKMatrix.MakeIdentity();
        private SKMatrix _rotation = SKMatrix.MakeIdentity();

        public SKMatrix WorldToViewSpaceTransform {
            get {
                var matrix = SKMatrix.MakeIdentity();

                SKMatrix.PostConcat(ref matrix, this._rotation);
                SKMatrix.PostConcat(ref matrix, this.Translation);

                return matrix;
            }
        }
        public SKMatrix ViewToWorldSpaceTransform {
            get {
                this.WorldToViewSpaceTransform.TryInvert(out var matrix);

                return matrix;
            }
        }

        public SKMatrix Translation { get => this._translation; set => this._translation = value; }

        public ViewTranform() { }
    }

    public class ClipTransform {
        private SKMatrix _translation = SKMatrix.MakeIdentity();
        private SKMatrix _scale = SKMatrix.MakeIdentity();

        public SKMatrix ViewToNormalizedClipSpaceTransform {
            get {
                var matrix = SKMatrix.MakeIdentity();

                SKMatrix.PostConcat(ref matrix, this.Translation);
                SKMatrix.PostConcat(ref matrix, this.Scale);

                return matrix;
            }
        }
        public SKMatrix NormalizedClipToViewSpaceTransform {
            get {
                this.ViewToNormalizedClipSpaceTransform.TryInvert(out var matrix);

                return matrix;
            }
        }

        public SKMatrix Translation { get => this._translation; set => this._translation = value; }
        public SKMatrix Scale { get => this._scale; set => this._scale = value; }

        public ClipTransform() { }
    }

    public class DeviceTransform {
        private SKMatrix _scale = SKMatrix.MakeIdentity();
        private SKMatrix _translation = SKMatrix.MakeIdentity();

        public SKMatrix ClipToDeviceSpaceTransform {
            get {
                var matrix = SKMatrix.MakeIdentity();

                SKMatrix.PostConcat(ref matrix, this.Scale);
                SKMatrix.PostConcat(ref matrix, this.Translation);

                return matrix;
            }
        }
        public SKMatrix DeviceToClipSpaceTransform {
            get {
                this.ClipToDeviceSpaceTransform.TryInvert(out var matrix);

                return matrix;
            }
        }

        public SKMatrix Scale { get => this._scale; set => this._scale = value; }
        public SKMatrix Translation { get => this._translation; set => this._translation = value; }
    }
}
