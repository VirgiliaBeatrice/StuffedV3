using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace TaskMaker.View.Widgets.Container {

    public struct Node {
        public bool IsSelected { get; set; }
        public SKPoint Location { get; set; }
    }

    public class NodeCollectionRenderObject : RenderObject {
        public SKRect Bound { get; set; }
        public Node[] Nodes { get; set; }

        public NodeCollectionRenderObject(Node[] nodes) : base() {
            Nodes = nodes;
        }

        public NodeCollectionRenderObject() {
            Bound = new SKRect { Right = 10, Bottom = 10 };

            Render();
        }

        public void Render() {
            var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bound);
            var stroke = new SKPaint {
                IsAntialias = true,
                StrokeWidth = 2,
                IsStroke = true,
                Color = SKColors.Black,
            };
            var fill = new SKPaint {
                IsAntialias = true,
                Color = SKColors.YellowGreen
            };

            foreach (var node in Nodes) {
                canvas.DrawCircle(node.Location, 5.0f, stroke);
                canvas.DrawCircle(node.Location, 5.0f, fill);
            }

            _cachedPicture = recorder.EndRecording();

            stroke.Dispose();
            fill.Dispose();
            canvas.Dispose();
        }
    }
}
