using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaker.MementoPattern {
    public class Memento {
        private object _states;

        public Memento(object states) {
            _states = states;
        }

        public object GetStates() { return _states; }
    }

    public class Originator {
        private object _state;

        public Memento Save() => new Memento(_state);
        public void Restore(Memento m) { }
    }

    public interface IOriginator {
        Memento Save();
        void Restore(Memento m);
    }

    public class Caretaker {
        private IOriginator _originator;
        private Stack<(Memento, IOriginator)> _history = new Stack<(Memento, IOriginator)>(10);

        public Caretaker(IOriginator originator) {
            _originator = originator;
        }

        public Caretaker() { }

        //public void Do() {
        //    var m = _originator.Save();

        //    _history.Push(m);
        //}

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
    }
}
