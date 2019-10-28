using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using NLog;

namespace TuggingController
{
    public partial class Form1 : Form
    {
        public PointChart chart;

        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public Form1()
        {
            InitializeComponent();

            var config = new NLog.Config.LoggingConfiguration();

            var logConsole = new NLog.Targets.ColoredConsoleTarget("Form1");

            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            NLog.LogManager.Configuration = config;

            Logger.Debug("Hello World");
            //RobotController ctrl = new RobotController();
            skControl1.Location = new Point(0, 0);
            skControl1.Size = this.Size;
            skControl1.PaintSurface += SkControl1_PaintSurface;
            this.SizeChanged += Form1_SizeChanged;
            this.chart = new PointChart();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            skControl1.Size = this.Size;
        }

        private void SkControl1_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            this.chart.Draw(e.Surface.Canvas, this.Size.Width, this.Size.Height);
            //SharedPage.OnPainting(sender, e);
        }

        private void skControl1_MouseClick(object sender, MouseEventArgs e)
        {
            // Add new point

            switch (e.Button)
            {
                case MouseButtons.Left:
                    Logger.Debug("Add new point");
                    Logger.Debug(e.Location);
                    this.chart.AddPoint(e.Location);
                    skControl1.Invalidate();
                    break;
                default:
                    break;
            }

        }
    }

    public abstract class Chart
    {
        #region Properties
        public float Margin { get; set; } = 20;
        public float LabelTextSize { get; set; } = 16;

        #endregion

        #region Methods
        public void Draw(SKCanvas canvas, int width, int height)
        {
            canvas.Clear(SKColor.Empty);

            this.DrawContent(canvas, width, height);
        }

        public abstract void DrawContent(SKCanvas canvas, int width, int height);
        #endregion
    }

    public class PointChart : Chart
    {
        #region Properties

        public float PointSize { get; set; } = 14;
        public List<SKPoint> Points = new List<SKPoint>();
        
        #endregion
        
        #region Methods
        public override void DrawContent(SKCanvas canvas, int width, int height)
        {
            //For test
            if (Points.Count > 0)
            {
                this.DrawPoints(canvas, Points.ToArray());
            }
            //SKPoint[] points = 
            //{
            //    new SKPoint(100, 200),
            //    new SKPoint(200, 200)
            //};

            //this.DrawPoints(canvas, points);
        }

        protected void DrawPoints(SKCanvas canvas, SKPoint[] points)
        {
            if (points.Length > 0)
            {
                foreach(var p in points)
                {
                    var paint = new SKPaint
                    {
                        IsAntialias = true,
                        Color = SKColors.Blue,
                        Style = SKPaintStyle.Fill                        
                    };
                    canvas.DrawCircle(p.X, p.Y, 14 / 2, paint);
                }
            }
        }

        public void AddPoint(Point point)
        {
            this.Points.Add(new SKPoint(point.X, point.Y));
        }

        #endregion
    }
}
