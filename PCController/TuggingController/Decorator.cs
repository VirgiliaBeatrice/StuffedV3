using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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

        public SKRect MapRect(SKRect rect) {
            var transform = this.LocalTransformation;

            if(this.Parent != null) {
                SKMatrix.PostConcat(ref transform, this.Parent.LocalTransformation);
            }

            return transform.MapRect(rect);
        }

        public SKRect InverseMapRect(SKRect rect) {
            return this.InvGlobalTransformation.MapRect(rect);
        }

        public SKRect InverseLocalMapRect(SKRect rect) {
            return this.InvLocalTransformation.MapRect(rect);
        }

        public BehaviorResult Behavior(BehaviorArgs e) {
            throw new NotImplementedException();
        }
    }
}
