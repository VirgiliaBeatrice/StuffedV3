﻿using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TuggingController {
    public partial class TuggingControlForm : Form {
        private ChartControl chartControl;
        private ToolStripStatusLabel statusLabel;
        public ChartControl configControl;
        private CanvasObjectPropertyForm propertyForm;

        public TuggingControlForm() {
            InitializeComponent();

            this.statusLabel = new ToolStripStatusLabel();

            this.configControl = new ChartControl();
            this.configControl.ChartScene = new ConfigScene();
            this.configControl.ChartScene.WorldSpace.Device = new SKRect() {
                Left = -this.configControl.Size.Width / 2.0f,
                Right = this.configControl.Size.Width / 2.0f,
                Top = this.configControl.Size.Height / 2.0f,
                Bottom = -this.configControl.Size.Height / 2.0f
            };
            this.configControl.ChartScene.WorldSpace.Window = new SKRect() {
                Left = -this.configControl.Size.Width / 2.0f,
                Right = this.configControl.Size.Width / 2.0f,
                Top = this.configControl.Size.Height / 2.0f,
                Bottom = -this.configControl.Size.Height / 2.0f
            };

            this.KeyPreview = true;
            //this.ClientSize = new Size(400, 400 + this.statusStrip1.Size.Height);
            this.SizeChanged += this.TuggingControlForm_SizeChanged;
            this.chartControl = new ChartControl();

            this.chartControl.Location = new Point(0, 0);
            this.chartControl.Dock = DockStyle.Fill;

            this.configControl.Location = new Point(0, 0);
            this.configControl.Dock = DockStyle.Fill;

            //this.statusStrip1.Location = new Point(0, 400 + this.statusStrip1.Size.Height);
            this.statusStrip1.Items.Add(this.statusLabel);

            this.tabPage1.Controls.Add(this.chartControl);
            this.tabPage5.Controls.Add(this.configControl);

            this.treeView1.NodeMouseDoubleClick += this.TreeView1_NodeMouseDoubleClick;

            //this.Controls.AddRange(new Control[] { this.chartControl });

            this.chartControl.CanvasTargetChanged += this.ChartControl_CanvasTargetChanged;
            this.chartControl.CanvasObjectChanged += this.ChartControl_CanvasObjectChanged;

            this.propertyForm = new CanvasObjectPropertyForm() {
                Parent = this,
            };
            this.KeyDown += this.TuggingControlForm_KeyDown;

        }

        private void TuggingControlForm_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Space) {
                this.propertyForm.Show();
                e.Handled = true;
            }
        }

        private void ChartControl_CanvasObjectChanged(object sender, EventArgs e) {
            this.UpdateTreeview();
        }

        private void TreeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e) {
            var prevState = (e.Node.Tag as ICanvasObject).IsSelected;
            (e.Node.Tag as ICanvasObject).IsSelected = !prevState;

            this.chartControl.Invalidate(true);
        }

        private void ChartControl_CanvasTargetChanged(object sender, CanvasTargetChangedEventArgs e) {
            this.statusLabel.Text = e.Target.ToString();

            //this.UpdateTreeview();
        }

        private void TuggingControlForm_SizeChanged(object sender, EventArgs e) {
            this.chartControl.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - this.statusStrip1.Size.Height);

            this.Invalidate(true);
        }

        private void UpdateTreeview() {
            this.treeView1.BeginUpdate();
            
            var root = this.chartControl.ChartScene.Root;

            this.treeView1.Nodes.Clear();
            this.treeView1.Nodes.Add(new TreeNode(root.ToString(), root.GetChildrenTreeNodes()) { Tag = root });

            this.treeView1.EndUpdate();
            this.treeView1.ExpandAll();
        }
    }
}
