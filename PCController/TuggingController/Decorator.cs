using System;
using System.Collections.Generic;
using System.Linq;
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

    public class Transform {
        public static Transform WithScale(SKMatrix scale) {
            return new Transform(scale, SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity());
        }

        public static Transform WithRotation(SKMatrix rotation) {
            return new Transform(SKMatrix.MakeIdentity(), rotation, SKMatrix.MakeIdentity());
        }

        public static Transform WithTranslation(SKMatrix translation) {
            return new Transform(SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity(), translation);
        }

        public SKMatrix Scale { get; set; }
        public SKMatrix Rotation { get; set; }
        public SKMatrix Translation { get; set; }
        public SKMatrix Transformation {
            get {
                var mat = SKMatrix.MakeIdentity();

                SKMatrix.PostConcat(ref mat, this.Scale);
                SKMatrix.PostConcat(ref mat, this.Rotation);
                SKMatrix.PostConcat(ref mat, this.Translation);

                return mat;
            }
        }
        public SKMatrix InvTransformation {
            get {
                var inv = SKMatrix.MakeIdentity();

                this.Transformation.TryInvert(out inv);

                return inv;
            }
        }

        public Transform() : this(SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity(), SKMatrix.MakeIdentity()) { }

        public Transform(SKMatrix scale, SKMatrix rotation, SKMatrix translation) {
            this.Scale = scale;
            this.Translation = translation;
            this.Rotation = rotation;
        }

        public SKPoint MapPoint(SKPoint point) {
            return this.Transformation.MapPoint(point);
        }

        public SKPoint InverseMapPoint(SKPoint point) {
            return this.InvTransformation.MapPoint(point);
        }
    }
}
