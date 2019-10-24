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

namespace TuggingController
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            //RobotController ctrl = new RobotController();
            skControl1.PaintSurface += SkControl1_PaintSurface;
        }

        private void SkControl1_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SharedPage.OnPainting(sender, e);
            //throw new NotImplementedException();
        }
    }
}
