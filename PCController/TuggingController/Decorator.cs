using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace TuggingController {

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TransformAttribute : Attribute {
        public SKMatrix Translation { get; set; }
        public SKMatrix Scale { get; set; }
        public SKMatrix Rotation { get; set; }
        public SKMatrix Transform { get; set; }
        public TransformAttribute(SKMatrix scale, SKMatrix rotation, SKMatrix translation) {
            var mat = SKMatrix.MakeIdentity();

            this.Scale = scale;
            this.Translation = translation;
            this.Rotation = rotation;

            SKMatrix.PostConcat(ref mat, scale);
            SKMatrix.PostConcat(ref mat, rotation);
            SKMatrix.PostConcat(ref mat, translation);

            this.Transform = mat;
        }
    }

    // Transform Component for CanvasObject
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

        // Initialized Property
        public SKMatrix Scale { get; set; } = SKMatrix.MakeIdentity();
        public SKMatrix Rotation { get; set; } = SKMatrix.MakeIdentity();
        public SKMatrix Translation { get; set; } = SKMatrix.MakeIdentity();

        // Order: T<-R<-S, Global(TRS)<-Local(TRS)
        public SKMatrix Transformation {
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
                var transform = this.Transformation;

                if (this.Parent != null) {
                    SKMatrix.PostConcat(ref transform, this.Parent.GlobalTransformation);
                }
                return transform;
            }
        }
        public SKMatrix InvTransformation {
            get {
                this.Transformation.TryInvert(out SKMatrix inv);

                return inv;
            }
        }
        public SKMatrix GlobalInvTransformation {
            get {
                this.Transformation.TryInvert(out SKMatrix inv);

                return inv;
            }
        }


        public Transform() : this(SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity()) { }

        public Transform(SKMatrix scale, SKMatrix rotation, SKMatrix translation) {
            this.Scale = scale;
            this.Translation = translation;
            this.Rotation = rotation;
        }

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
            var invTransform = this.InvTransformation;

            if (this.Parent != null) {
                SKMatrix.PreConcat(ref invTransform, this.Parent.InvTransformation);
            }

            return invTransform.MapPoint(point);
        }

        public SKRect MapRect(SKRect rect) {
            var transform = this.Transformation;

            if(this.Parent != null) {
                SKMatrix.PostConcat(ref transform, this.Parent.Transformation);
            }

            return transform.MapRect(rect);
        }
    }
}
