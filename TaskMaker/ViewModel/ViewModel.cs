using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaker.View.Widgets;
using TaskMaker.Model.Core;

namespace TaskMaker.ViewModel {
    public class BaseViewModel {
        // View
        public BaseWidget Widget { get; set; }
        // Model
        public BaseModel Model { get; set; }

        public void DoOperation() { }
    }

    public class ControlUIViewModel {
        
    }
}
