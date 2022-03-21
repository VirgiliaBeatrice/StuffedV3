using SkiaSharp;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;
using SkiaSharp.Views.Desktop;
using TaskMaker.View.Widgets;
using TaskMaker.View.Widgets.Units;
using System.Collections.Generic;

namespace TaskMaker.View.Pages {
    public class MainPage {
        static public NodeWidgetState DefaultNodeState { get; set; } = new NodeWidgetState {
            bound = new SKRect(0, 0, 10, 10),
            location = new SKPoint(0, 0),
            isClicked = false,
            radius = 5
        };

        public Widget WidgetTree { get; set; }

        public MainPage() {
            //InitializeWidgets();
            InitializeWidgets_v1();

            //Console.Write(WidgetTree.PrintAllChild());
        }

        public void InitializeWidgets() {
            WidgetTree = new RootContainerWidget("Root");

            var grid0 = new GridWidget(
                "Grid",
                new GridWidgetState {
                    row = 1,
                    column = 2,
                    width = 800,
                    height = 600 });
            var grid0_item0 = new GridItemWidget(
                "Item0",
                grid0.GetItemState(0, 0));
            var grid0_item1 = new GridItemWidget(
                "Item1",
                grid0.GetItemState(0, 1));
            var content = new TestWidget(
                "Content",
                new TestWidgetState { bound = new SKRect(0, 0, 100, 50) });

            WidgetTree.AddChild(grid0);
            grid0.AddChild(grid0_item0);
            grid0.AddChild(grid0_item1);
            grid0_item0.AddChild(content);
        }

        public void InitializeWidgets_v1() {
            WidgetTree = new RootContainerWidget("Root");

            var e0 = new NodeWidget("E0",
                new NodeWidgetState {
                    bound = new SKRect(0, 0, 5, 5),
                    isClicked = false,
                    location = new SKPoint(200, 200),
                    radius = 5
                });

            WidgetTree.AddChild(e0);
        }

        public void AddNode(SKPoint p) {
            var state = (NodeWidgetState)DefaultNodeState.Clone();
            state.location = p;
            var e1 = new NodeWidget("E1", state);
            WidgetTree.AddChild(e1);

            e1.Build();
        }
    }
}
