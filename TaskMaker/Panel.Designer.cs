
namespace TaskMaker {
    partial class ControlPanel {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.skglControl1 = new SkiaSharp.Views.Desktop.SKGLControl();
            this.skglControl2 = new SkiaSharp.Views.Desktop.SKGLControl();
            this.skglControl3 = new SkiaSharp.Views.Desktop.SKGLControl();
            this.skglControl4 = new SkiaSharp.Views.Desktop.SKGLControl();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.skglControl4, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.skglControl3, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.skglControl2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.skglControl1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 450);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // skglControl1
            // 
            this.skglControl1.BackColor = System.Drawing.Color.Black;
            this.skglControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.skglControl1.Location = new System.Drawing.Point(3, 3);
            this.skglControl1.Name = "skglControl1";
            this.skglControl1.Size = new System.Drawing.Size(394, 219);
            this.skglControl1.TabIndex = 0;
            this.skglControl1.VSync = false;
            // 
            // skglControl2
            // 
            this.skglControl2.BackColor = System.Drawing.Color.Black;
            this.skglControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.skglControl2.Location = new System.Drawing.Point(403, 3);
            this.skglControl2.Name = "skglControl2";
            this.skglControl2.Size = new System.Drawing.Size(394, 219);
            this.skglControl2.TabIndex = 1;
            this.skglControl2.VSync = false;
            // 
            // skglControl3
            // 
            this.skglControl3.BackColor = System.Drawing.Color.Black;
            this.skglControl3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.skglControl3.Location = new System.Drawing.Point(3, 228);
            this.skglControl3.Name = "skglControl3";
            this.skglControl3.Size = new System.Drawing.Size(394, 219);
            this.skglControl3.TabIndex = 2;
            this.skglControl3.VSync = false;
            // 
            // skglControl4
            // 
            this.skglControl4.BackColor = System.Drawing.Color.Black;
            this.skglControl4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.skglControl4.Location = new System.Drawing.Point(403, 228);
            this.skglControl4.Name = "skglControl4";
            this.skglControl4.Size = new System.Drawing.Size(394, 219);
            this.skglControl4.TabIndex = 3;
            this.skglControl4.VSync = false;
            // 
            // Panel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "Panel";
            this.Text = "Panel";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private SkiaSharp.Views.Desktop.SKGLControl skglControl1;
        private SkiaSharp.Views.Desktop.SKGLControl skglControl4;
        private SkiaSharp.Views.Desktop.SKGLControl skglControl3;
        private SkiaSharp.Views.Desktop.SKGLControl skglControl2;
    }
}