using MathNetExtension;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaskMaker.View;
using TaskMaker.View.Pages;
using TaskMaker.View.Widgets;

namespace TaskMaker {
    public partial class TestForm : Form {
        public MainPage MainPage { get; set; }

        public TestForm() {
            InitializeComponent();

            MainPage = new MainPage();
            Engine.Build(MainPage.WidgetTree);

            var mouseDown = Observable.FromEventPattern<MouseEventArgs>(skglControl1, nameof(skglControl1.MouseDown));
            var mouseMove = Observable.FromEventPattern<MouseEventArgs>(skglControl1, nameof(skglControl1.MouseMove));
            var mouseUp = Observable.FromEventPattern<MouseEventArgs>(skglControl1, nameof(skglControl1.MouseUp));

            var mouseClick = mouseDown
                .Take(1)
                .Sample(mouseUp.Take(1));

            var mouseDrag = mouseDown
                .SelectMany(
                    e0 => mouseDown
                        .Take(1)
                        .Timeout(TimeSpan.FromMilliseconds(400), Observable.Empty<EventPattern<MouseEventArgs>>()))
                .Select(
                    e1 => mouseMove
                        .TakeUntil(mouseUp));

            mouseDrag
                .SelectMany(x => Observable.Defer(() => {
                    Console.WriteLine("start");

                    return x.Do(y => Console.WriteLine("move")).Finally(() => Console.WriteLine("stop"));
                }))
                .Subscribe();

            mouseClick
                .Timestamp()
                .Do(x => Console.WriteLine($"{x.Value.EventArgs.Location} - {x.Timestamp}"))
                .Repeat();

            Widget GetClickedDeepestWidget(Widget next, SKPoint point) {
                if (next.Contains(point)) {
                    Widget result = null;

                    foreach (var child in next.GetAllChild()) {
                        if (child is ContainerWidget castChild) {
                            result = GetClickedDeepestWidget(castChild, castChild.T.MapPoint(point));
                        }
                        else {
                            result = GetClickedDeepestWidget(child, point);
                        }

                        if (result != null)
                            break;
                    }

                    return result ?? next;
                }
                else {
                    return null;
                }
            }

            mouseClick
                .Select(x => x.EventArgs.Location)
                .Repeat()
                .Subscribe(x => MainPage.AddNode(x.ToSKPoint()));

            //mouseClick
            //    .Select(e => {
            //        var point = e.EventArgs.Location;
            //        var result = GetClickedDeepestWidget(MainPage.WidgetTree, point.ToSKPoint());

            //        return result;
            //    })
            //    .Where(e => e != null)
            //    .Repeat()
            //    .Subscribe(e => e.OnClick());

            var update = Observable.Interval(TimeSpan.FromSeconds(1.0 / 60.0))
                .Timestamp()
                //.Do(e => Console.WriteLine(e.Timestamp.ToUnixTimeMilliseconds()))
                .Subscribe(e => skglControl1.Invalidate());
            //var fps = Observable.FromEventPattern(skglControl1, nameof(skglControl1.PaintSurface));

            //fps.Window(TimeSpan.FromSeconds(1))
            //    .SelectMany(e => e.Count())
            //    .Subscribe(e => Console.WriteLine(e));
        }

        private void skglControl1_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintGLSurfaceEventArgs e) {
            var canvas = e.Surface.Canvas;

            canvas.Clear(SKColors.AntiqueWhite);

            Engine.Paint(MainPage.WidgetTree, canvas);
        }
    }
}
