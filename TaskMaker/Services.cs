using PCController;
using System.Windows.Forms;
using TaskMaker.MementoPattern;
using TaskMaker.Mapping;
using TaskMaker.Node;
using TaskMaker.SimplicialMapping;
using System.Collections.Generic;

namespace TaskMaker {
    static public class Services {
        static public Caretaker Caretaker { get; set; }
        static public Boards Boards { get; set; } = new Boards();
        static public Motors Motors { get; set; } = new Motors();
        static public Timer MotorTimer { get; set; } = new Timer() { Interval = 100 };
        static public TreeNode LayerTree { get; set; }
        static public Triangulation TriHandler { get; set; } = new Triangulation();
        static public Canvas Canvas { get; set; } = new Canvas();
        static public NLinearMap Map { get; set; }
        static public Dictionary<string, NLinearMap> Maps { get; set; } = new Dictionary<string, NLinearMap>();
        static public Graph Graph { get; set; }
        //static public Flow Flow { get; set; }
    }
}
