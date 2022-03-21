using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaker.View.Widgets {
    public interface IState {

    }

    public struct NodeWidgetState : IState, ICloneable {
        public SKRect bound;
        public SKPoint location;
        public float radius;

        public bool isClicked;

        public object Clone() {
            return new NodeWidgetState {
                bound = this.bound,
                location = this.location,
                radius = this.radius,
                isClicked = this.isClicked
            };
        }
    }

    public class NodeWidget : RenderWidget<NodeWidgetState> {
        public NodeWidget(string name, NodeWidgetState initState) : base(name, initState) { }

        public override bool Contains(SKPoint p) {
            return (p - State.location).LengthSquared <= Math.Pow(State.radius, 2);
        }

        public override void OnClick() {
            var state = State;
            state.isClicked = !state.isClicked;
            State = state;
            (RenderObject as NodeRenderObject).Render(State);
        }

        public override void Build() {
            RenderObject = new NodeRenderObject(State);
        }
    }

    public class NodeRenderObject : RenderObject {
        public NodeRenderObject(NodeWidgetState initState) {
            Render(initState);
        }

        protected void OnRender(ref SKCanvas canvas, IState state) {
            var castState = (NodeWidgetState)state;

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

            if (castState.isClicked)
                fill.Color = SKColors.AliceBlue;

            canvas.DrawCircle(castState.location, castState.radius, stroke);
            canvas.DrawCircle(castState.location, castState.radius, fill);

            stroke.Dispose();
            fill.Dispose();
        }

        public void Render(NodeWidgetState state) {
            var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(state.bound);

            var shrinkedRect = state.bound;
            //shrinkedRect.Inflate(-10.0f, -10.0f);

            OnRender(ref canvas, state);

            _cachedPicture = recorder.EndRecording();

            canvas.Dispose();
        }
    }
}
