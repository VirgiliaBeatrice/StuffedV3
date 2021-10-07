using MathNet.Numerics.LinearAlgebra;
using PCController;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using TaskMaker.SimplicialMapping;
using MathNetExtension;
using System;
using TaskMaker.MementoPattern;

namespace TaskMaker {
    public abstract class Target : IVectorizable, IReflectable {
        public abstract Vector<float> ToVector();
        public abstract void FromVector(Vector<float> vector);
        public TargetState CreateTargetState() => new TargetState(this);

        public abstract IMemento Save();
        public abstract void Restore();
    }

    public class MotorTarget : Target {
        public List<Motor> Motors { get; set; } = new List<Motor>();

        public MotorTarget() { }

        public MotorTarget(IEnumerable<Motor> motors) {
            Motors.AddRange(motors);
        }

        public void AddMotor(Motor motor) {
            Motors.Add(motor);
        }

        public override Vector<float> ToVector() => Vector<float>.Build.DenseOfArray(Motors.Select(m => (float)m.position.Value).ToArray());

        public override void FromVector(Vector<float> vector) {
            for (int i = 0; i < vector.Count; ++i) {
                Motors[i].position.Value = (int)vector[i];
            }
        }

        public override IMemento Save() {
            //return new TargetState()
            throw new NotImplementedException();
        }

        public override void Restore() {
            throw new NotImplementedException();
        }
    }

    public class LayerTarget : Target {
        public List<Layer> Layers { get; set; } = new List<Layer>();
        public LayerTarget() { }

        public LayerTarget(IEnumerable<Layer> layers) {
            Layers.AddRange(layers);
        }

        public void Add(Layer layer) {
            Layers.Add(layer);
        }

        public override Vector<float> ToVector() {
            var columns = Layers.Select(l => l.Controller.Location.ToVector());
            var mat = Matrix<float>.Build.DenseOfColumnVectors(columns);

            return Vector<float>.Build.DenseOfArray(mat.AsColumnMajorArray());
        }

        public override void FromVector(Vector<float> vector) {
            //var mat = Matrix<float>.Build.DenseOfColumnVectors(vector);
            //mat = mat.Resize(2, vector.Count / 2);

            //for (int i = 0; i < vector.Count / 2; ++i) {
            //    var colVector = mat.Column(i);

            //    foreach (var c in Layers[i].Complexes) {
            //        c.Controller.Location = mat.Column()
            //    }
            //}

            //for (int i = 0; i < vector.Count / 2; ++i) {
            //    //Layers[i].Pointer.Location = new SKPoint(vector[i * 2], vector[i * 2 + 1]);
            //    Layers[i].Controller.Location = new SKPoint(vector[i * 2], vector[i * 2 + 1]);
            //    //Layers[i].Interpolate(Layers[i].Pointer.Location);
            //    Layers[i].Interpolate(Layers[i].Controller.Location);
            //}
        }

        public override IMemento Save() {
            throw new NotImplementedException();
        }

        public override void Restore() {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Immutable target state
    /// </summary>
    public class TargetState : IVectorizable, IMemento {
        private Vector<float> _state;
        private Target _parent;

        public TargetState(Target target) {
            _parent = target;
            _state = _parent.ToVector();
        }

        public Vector<float> ToVector() => _state;

        public object GetState() {
            return (_state, _parent);
        }
    }

    public interface IInput {
        Vector<float> ToVector();
    }

    public interface IOutput { }

    public class Node<T> {
        public T Tag { get; set; }

    }

    public class Link {
        public object Input { get; set; }
        public object Output { get; set; }

        public void Process() {
            if (Input != null & Output != null) {

            }
        }
    }

}
