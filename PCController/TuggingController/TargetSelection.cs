using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TuggingController {
    public partial class TargetSelection : UserControl {
        public TargetSelection() {
            InitializeComponent();
        }

        private void AddTestData() {
            //var node1 = 
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) {
            if (this.radioButton1.Checked) {
                this.radioButton2.Checked = false;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e) {
            if (this.radioButton2.Checked) {
                this.radioButton1.Checked = false;
            }
        }
    }
}
