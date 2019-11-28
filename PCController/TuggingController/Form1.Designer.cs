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
            this.groupbox1 = new System.Windows.Forms.GroupBox();
            this.button3 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.groupbox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TuggingController
            // 
            this.TuggingController.Location = new System.Drawing.Point(12, 12);
            this.TuggingController.Name = "TuggingController";
            this.TuggingController.Size = new System.Drawing.Size(352, 659);
            this.TuggingController.TabIndex = 0;
            this.TuggingController.Text = "skControl1";
            // 
            // ConfigurationSpace
            // 
            this.ConfigurationSpace.Location = new System.Drawing.Point(559, 12);
            this.ConfigurationSpace.Name = "ConfigurationSpace";
            this.ConfigurationSpace.Size = new System.Drawing.Size(457, 622);
            this.ConfigurationSpace.TabIndex = 1;
            this.ConfigurationSpace.Text = "skControl2";
            // 
            // groupbox1
            // 
            this.groupbox1.Controls.Add(this.button3);
            this.groupbox1.Controls.Add(this.button2);
            this.groupbox1.Controls.Add(this.button1);
            this.groupbox1.Location = new System.Drawing.Point(1081, 36);
            this.groupbox1.Name = "groupbox1";
            this.groupbox1.Size = new System.Drawing.Size(278, 589);
            this.groupbox1.TabIndex = 2;
            this.groupbox1.TabStop = false;
            this.groupbox1.Text = "Parameters";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(110, 120);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 2;
            this.button3.Text = "Screenshot";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(110, 62);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "Intialization";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(110, 91);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Create Pair";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1371, 683);
            this.Controls.Add(this.groupbox1);
            this.Controls.Add(this.ConfigurationSpace);
            this.Controls.Add(this.TuggingController);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupbox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private SkiaSharp.Views.Desktop.SKControl TuggingController;
        private SkiaSharp.Views.Desktop.SKControl ConfigurationSpace;
        private System.Windows.Forms.GroupBox groupbox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
    }
}

