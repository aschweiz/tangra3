﻿namespace Tangra.VideoOperations.Spectroscopy.AbsFluxCalibration
{
	partial class frmAbsFlux
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmAbsFlux));
			this.pnlFiles = new System.Windows.Forms.Panel();
			this.btnBrowseFiles = new System.Windows.Forms.Button();
			this.lbAvailableFiles = new System.Windows.Forms.ListBox();
			this.lblAvailableSpectraTitle = new System.Windows.Forms.Label();
			this.lblUsedSpectraTitle = new System.Windows.Forms.Label();
			this.lbIncludedSpecta = new System.Windows.Forms.CheckedListBox();
			this.pnlDetail = new System.Windows.Forms.Panel();
			this.pnlClient = new System.Windows.Forms.Panel();
			this.picPlot = new System.Windows.Forms.PictureBox();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.pnlFiles.SuspendLayout();
			this.pnlClient.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.picPlot)).BeginInit();
			this.SuspendLayout();
			// 
			// pnlFiles
			// 
			this.pnlFiles.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlFiles.Controls.Add(this.btnBrowseFiles);
			this.pnlFiles.Controls.Add(this.lbAvailableFiles);
			this.pnlFiles.Controls.Add(this.lblAvailableSpectraTitle);
			this.pnlFiles.Controls.Add(this.lblUsedSpectraTitle);
			this.pnlFiles.Controls.Add(this.lbIncludedSpecta);
			this.pnlFiles.Dock = System.Windows.Forms.DockStyle.Right;
			this.pnlFiles.Location = new System.Drawing.Point(564, 0);
			this.pnlFiles.Name = "pnlFiles";
			this.pnlFiles.Size = new System.Drawing.Size(207, 499);
			this.pnlFiles.TabIndex = 0;
			// 
			// btnBrowseFiles
			// 
			this.btnBrowseFiles.Location = new System.Drawing.Point(13, 460);
			this.btnBrowseFiles.Name = "btnBrowseFiles";
			this.btnBrowseFiles.Size = new System.Drawing.Size(184, 27);
			this.btnBrowseFiles.TabIndex = 4;
			this.btnBrowseFiles.Text = "Change Spectra Files Location";
			this.btnBrowseFiles.UseVisualStyleBackColor = true;
			this.btnBrowseFiles.Click += new System.EventHandler(this.btnBrowseFiles_Click);
			// 
			// lbAvailableFiles
			// 
			this.lbAvailableFiles.FormattingEnabled = true;
			this.lbAvailableFiles.Location = new System.Drawing.Point(13, 224);
			this.lbAvailableFiles.Name = "lbAvailableFiles";
			this.lbAvailableFiles.Size = new System.Drawing.Size(184, 225);
			this.lbAvailableFiles.TabIndex = 3;
			this.lbAvailableFiles.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lbAvailableFiles_MouseDoubleClick);
			// 
			// lblAvailableSpectraTitle
			// 
			this.lblAvailableSpectraTitle.AutoSize = true;
			this.lblAvailableSpectraTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblAvailableSpectraTitle.Location = new System.Drawing.Point(10, 208);
			this.lblAvailableSpectraTitle.Name = "lblAvailableSpectraTitle";
			this.lblAvailableSpectraTitle.Size = new System.Drawing.Size(186, 13);
			this.lblAvailableSpectraTitle.TabIndex = 2;
			this.lblAvailableSpectraTitle.Text = "Available Files (doubleclick to include)";
			// 
			// lblUsedSpectraTitle
			// 
			this.lblUsedSpectraTitle.AutoSize = true;
			this.lblUsedSpectraTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblUsedSpectraTitle.Location = new System.Drawing.Point(10, 10);
			this.lblUsedSpectraTitle.Name = "lblUsedSpectraTitle";
			this.lblUsedSpectraTitle.Size = new System.Drawing.Size(188, 13);
			this.lblUsedSpectraTitle.TabIndex = 1;
			this.lblUsedSpectraTitle.Text = "Included Spectra (dbl-click to exclude)";
			// 
			// lbIncludedSpecta
			// 
			this.lbIncludedSpecta.FormattingEnabled = true;
			this.lbIncludedSpecta.Location = new System.Drawing.Point(10, 31);
			this.lbIncludedSpecta.Name = "lbIncludedSpecta";
			this.lbIncludedSpecta.Size = new System.Drawing.Size(187, 154);
			this.lbIncludedSpecta.TabIndex = 0;
			// 
			// pnlDetail
			// 
			this.pnlDetail.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.pnlDetail.Location = new System.Drawing.Point(0, 401);
			this.pnlDetail.Name = "pnlDetail";
			this.pnlDetail.Size = new System.Drawing.Size(564, 98);
			this.pnlDetail.TabIndex = 1;
			// 
			// pnlClient
			// 
			this.pnlClient.Controls.Add(this.picPlot);
			this.pnlClient.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pnlClient.Location = new System.Drawing.Point(0, 0);
			this.pnlClient.Name = "pnlClient";
			this.pnlClient.Size = new System.Drawing.Size(564, 401);
			this.pnlClient.TabIndex = 2;
			// 
			// picPlot
			// 
			this.picPlot.Dock = System.Windows.Forms.DockStyle.Fill;
			this.picPlot.Location = new System.Drawing.Point(0, 0);
			this.picPlot.Name = "picPlot";
			this.picPlot.Size = new System.Drawing.Size(564, 401);
			this.picPlot.TabIndex = 0;
			this.picPlot.TabStop = false;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(564, 24);
			this.menuStrip1.TabIndex = 3;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// openFileDialog
			// 
			this.openFileDialog.DefaultExt = "spectra";
			this.openFileDialog.Filter = "Tangra Exports (*.dat)|*.dat|All Files (*.*)|*.*";
			this.openFileDialog.Title = "Select Spectra File to Add All Files from Same Location";
			// 
			// frmAbsFlux
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(771, 499);
			this.Controls.Add(this.menuStrip1);
			this.Controls.Add(this.pnlClient);
			this.Controls.Add(this.pnlDetail);
			this.Controls.Add(this.pnlFiles);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "frmAbsFlux";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Spectroscopy - Absolute Flux Calibration (Based on HST/STIS CALSPEC Spectra)";
			this.Load += new System.EventHandler(this.frmAbsFlux_Load);
			this.pnlFiles.ResumeLayout(false);
			this.pnlFiles.PerformLayout();
			this.pnlClient.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.picPlot)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel pnlFiles;
		private System.Windows.Forms.Label lblUsedSpectraTitle;
		private System.Windows.Forms.CheckedListBox lbIncludedSpecta;
		private System.Windows.Forms.Panel pnlDetail;
		private System.Windows.Forms.Panel pnlClient;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.Button btnBrowseFiles;
		private System.Windows.Forms.ListBox lbAvailableFiles;
		private System.Windows.Forms.Label lblAvailableSpectraTitle;
		private System.Windows.Forms.PictureBox picPlot;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
	}
}