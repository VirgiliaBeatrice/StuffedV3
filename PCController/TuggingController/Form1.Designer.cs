namespace TuggingController
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.TuggingController = new SkiaSharp.Views.Desktop.SKControl();
            this.ConfigurationSpace = new SkiaSharp.Views.Desktop.SKControl();
            this.SuspendLayout();
            // 
            // TuggingController
            // 
            this.TuggingController.Location = new System.Drawing.Point(12, 12);
            this.TuggingController.Name = "TuggingController";
            this.TuggingController.Size = new System.Drawing.Size(550, 659);
            this.TuggingController.TabIndex = 0;
            this.TuggingController.Text = "skControl1";
            // 
            // ConfigurationSpace
            // 
            this.ConfigurationSpace.Location = new System.Drawing.Point(701, 23);
            this.ConfigurationSpace.Name = "ConfigurationSpace";
            this.ConfigurationSpace.Size = new System.Drawing.Size(565, 622);
            this.ConfigurationSpace.TabIndex = 1;
            this.ConfigurationSpace.Text = "skControl2";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1288, 683);
            this.Controls.Add(this.ConfigurationSpace);
            this.Controls.Add(this.TuggingController);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private SkiaSharp.Views.Desktop.SKControl TuggingController;
        private SkiaSharp.Views.Desktop.SKControl ConfigurationSpace;
    }
}

