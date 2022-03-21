using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaker.View.Widgets;
using TaskMaker.Model.Core;
using TaskMaker.Model.ControlUI;
using SkiaSharp;
using Numpy;

namespace TaskMaker.ViewModel {
    public class BaseViewModel {
        // View
        //public BaseWidget Widget { get; set; }
        // Model
        public BaseModel Model { get; set; }

        public void DoOperation() { }
    }

    public class ControlUIViewModel : BaseViewModel {
        public List<BaseModel> Models { get; set; } = new List<BaseModel> { };

        public ControlUIViewModel() { }

        public void AddNode(ControlUIUnit unit, SKPoint p) {
            // Convert skpoint to ndarray
            var node = np.array(new float[] { p.X, p.Y });

            // Update model - add node
            unit.AddNode(new Model.ControlUI.Node { Data0 = node });

            // TODO
            // Update view - 
        }

    }
}
