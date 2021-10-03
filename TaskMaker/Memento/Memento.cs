using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaker.Memento {
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

    public class Caretaker {
        private Originator _originator;
        private Stack<Memento> _history = new Stack<Memento>(10);

        public Caretaker(Originator originator) {
            _originator = originator;
        }

        public void Do() {
            var m = _originator.Save();

            _history.Push(m);
        }
        public void Undo() {
            var m = _history.Pop();

            _originator.Restore(m);
        }
    }
}
