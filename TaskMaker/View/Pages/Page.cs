using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaker.Utilities;
using TaskMaker.View.Widgets;

namespace TaskMaker.View.Pages {
    public class MainPage {
        public TreeElement WidgetTree { get; set; }

        public MainPage() {
            WidgetTree = new TreeElement { Name = "Root" };

            var grid_0 = WidgetTree.AddChild<GridWidget>("Grid_0");
            var grid0_item0 = grid_0.AddChild<GridItemWidget>("Item_0");
            var grid0_item1 = grid_0.AddChild<GridItemWidget>("Item_1");

        }


    }
}
