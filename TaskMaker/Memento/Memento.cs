using System.Collections.Generic;
using System.Text.Json;


namespace TaskMaker.MementoPattern {
    public interface IMemento {
        object GetState();
    }

    public abstract class BaseState : IMemento {
        public abstract object GetState();

        public virtual byte[] ToJsonUtf8Bytes() {
            var options = new JsonSerializerOptions { WriteIndented = true };

            return JsonSerializer.SerializeToUtf8Bytes(this, GetType(), options);
        }

        public virtual string ToJsonString() {
            var options = new JsonSerializerOptions { WriteIndented = true };

            return JsonSerializer.Serialize(this, GetType(), options);
        }

    }

    public class Memento {
        private object _state;

        public Memento(object state) {
            _state = state;
        }

        public object GetState() { return _state; }
    }

    public class Originator {
        private object _state;

        public Memento Save() => new Memento(_state);
        public void Restore(Memento m) { }
    }

    public interface IOriginator {
        IMemento Save();
        void Restore(IMemento m, object info = null);
    }

    public class Caretaker {
        private IOriginator _originator;
        private Stack<(IMemento, IOriginator)> _history = new Stack<(IMemento, IOriginator)>(10);
        private Stack<(IMemento, IOriginator)> _redoHistory = new Stack<(IMemento, IOriginator)>(10);

        public Caretaker(IOriginator originator) {
            _originator = originator;
        }

        public Caretaker() { }

        public void Do(IOriginator o) {
            var m = o.Save();

            _history.Push((m, o));
        }

        public void Undo() {
            if (_history.Count != 0) {
                var (m, o) = _history.Pop();

                o.Restore(m);
            }
        }

        public void Redo() {
            if (_redoHistory.Count != 0) {
                var (m, o) = _redoHistory.Pop();
            }
        }
    }
}
