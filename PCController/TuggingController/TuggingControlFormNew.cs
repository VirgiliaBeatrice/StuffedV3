using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TuggingController {
    public partial class TuggingControlForm : Form {
        private ChartControl chartControl;

        public TuggingControlForm() {
            InitializeComponent();

            this.ClientSize = new Size(400, 400);
            this.SizeChanged += this.TuggingControlForm_SizeChanged;
            this.chartControl = new ChartControl();

            this.chartControl.Location = new Point(0, 0);
            this.chartControl.Size = this.ClientSize;

            this.Controls.AddRange(new Control[] { this.chartControl });

        }

        private void TuggingControlForm_SizeChanged(object sender, EventArgs e) {
            this.chartControl.Size = this.ClientSize;

            this.Invalidate(true);
        }
    }
}
