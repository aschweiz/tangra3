using Tangra.VideoTools;

namespace Tangra.VideoOperations.LightCurves
{
    partial class frmSelectReductionType
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSelectReductionType));
            this.rbAsteroidal = new System.Windows.Forms.RadioButton();
            this.rbMutualEvent = new System.Windows.Forms.RadioButton();
            this.cbxReductionType = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cbxBackgroundMethod = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbxDigitalFilter = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnMoreOrLess = new System.Windows.Forms.Button();
            this.tabsOptions = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.pnlLunarType = new System.Windows.Forms.Panel();
            this.rbLunarR = new System.Windows.Forms.RadioButton();
            this.rbLunarGraze = new System.Windows.Forms.RadioButton();
            this.rbLunarD = new System.Windows.Forms.RadioButton();
            this.rbLunarEvent = new System.Windows.Forms.RadioButton();
            this.pnlMutualType = new System.Windows.Forms.Panel();
            this.rbMutualEcl = new System.Windows.Forms.RadioButton();
            this.rbMutualOcc = new System.Windows.Forms.RadioButton();
            this.rbVariableOrTransit = new System.Windows.Forms.RadioButton();
            this.rbUntrackedMeasurement = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.cbxDriftTrough = new System.Windows.Forms.CheckBox();
            this.cbxFullDisappearance = new System.Windows.Forms.CheckBox();
            this.cbxStopOnLostTracking = new System.Windows.Forms.CheckBox();
            this.rbFastTracking = new System.Windows.Forms.RadioButton();
            this.rbTrackingWithRecovery = new System.Windows.Forms.RadioButton();
            this.lblTrackingDescr = new System.Windows.Forms.Label();
            this.tabIntegration = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.gbxPixelIntegration = new System.Windows.Forms.GroupBox();
            this.rbMean = new System.Windows.Forms.RadioButton();
            this.rbMedian = new System.Windows.Forms.RadioButton();
            this.pnlFrameCount = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.nudIntegrateFrames = new System.Windows.Forms.NumericUpDown();
            this.rbNoIntegration = new System.Windows.Forms.RadioButton();
            this.rbBinning = new System.Windows.Forms.RadioButton();
            this.rbRunningAverage = new System.Windows.Forms.RadioButton();
            this.tabStretching = new System.Windows.Forms.TabPage();
            this.ucStretching = new Tangra.VideoTools.ucPreProcessing();
            this.tabReduction = new System.Windows.Forms.TabPage();
            this.pnlBackground = new System.Windows.Forms.Panel();
            this.tabsOptions.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.pnlLunarType.SuspendLayout();
            this.pnlMutualType.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tabIntegration.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.gbxPixelIntegration.SuspendLayout();
            this.pnlFrameCount.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudIntegrateFrames)).BeginInit();
            this.tabStretching.SuspendLayout();
            this.tabReduction.SuspendLayout();
            this.pnlBackground.SuspendLayout();
            this.SuspendLayout();
            // 
            // rbAsteroidal
            // 
            this.rbAsteroidal.AutoSize = true;
            this.rbAsteroidal.Location = new System.Drawing.Point(13, 19);
            this.rbAsteroidal.Name = "rbAsteroidal";
            this.rbAsteroidal.Size = new System.Drawing.Size(171, 17);
            this.rbAsteroidal.TabIndex = 0;
            this.rbAsteroidal.Text = "Tracked Asteroidal Occultation";
            this.rbAsteroidal.UseVisualStyleBackColor = true;
            // 
            // rbMutualEvent
            // 
            this.rbMutualEvent.AutoSize = true;
            this.rbMutualEvent.Checked = true;
            this.rbMutualEvent.Location = new System.Drawing.Point(13, 41);
            this.rbMutualEvent.Name = "rbMutualEvent";
            this.rbMutualEvent.Size = new System.Drawing.Size(171, 17);
            this.rbMutualEvent.TabIndex = 1;
            this.rbMutualEvent.TabStop = true;
            this.rbMutualEvent.Text = "Tracked Mutual Satellite Event";
            this.rbMutualEvent.UseVisualStyleBackColor = true;
            this.rbMutualEvent.CheckedChanged += new System.EventHandler(this.rbMutualEvent_CheckedChanged);
            // 
            // cbxReductionType
            // 
            this.cbxReductionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxReductionType.FormattingEnabled = true;
            this.cbxReductionType.Items.AddRange(new object[] {
            "Aperture Photometry",
            "PSF Photometry",
            "Optimal Extraction Photometry"});
            this.cbxReductionType.Location = new System.Drawing.Point(15, 31);
            this.cbxReductionType.Name = "cbxReductionType";
            this.cbxReductionType.Size = new System.Drawing.Size(205, 21);
            this.cbxReductionType.TabIndex = 5;
            this.cbxReductionType.SelectedIndexChanged += new System.EventHandler(this.cbxReductionType_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 15);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Reduction method:";
            // 
            // cbxBackgroundMethod
            // 
            this.cbxBackgroundMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxBackgroundMethod.FormattingEnabled = true;
            this.cbxBackgroundMethod.Items.AddRange(new object[] {
            "Average Background",
            "Background Mode",
            "3D Polynomial Fit",
            "PSF-Fitting Background",
            "Median Background"});
            this.cbxBackgroundMethod.Location = new System.Drawing.Point(9, 16);
            this.cbxBackgroundMethod.Name = "cbxBackgroundMethod";
            this.cbxBackgroundMethod.Size = new System.Drawing.Size(205, 21);
            this.cbxBackgroundMethod.TabIndex = 3;
            this.cbxBackgroundMethod.SelectedIndexChanged += new System.EventHandler(this.cbxBackgroundMethod_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(91, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Background from:";
            // 
            // cbxDigitalFilter
            // 
            this.cbxDigitalFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxDigitalFilter.FormattingEnabled = true;
            this.cbxDigitalFilter.Items.AddRange(new object[] {
            "No Filter",
            "Low Pass Filter (LP)",
            "Low Pass Difference Filter (LPD)"});
            this.cbxDigitalFilter.Location = new System.Drawing.Point(15, 86);
            this.cbxDigitalFilter.Name = "cbxDigitalFilter";
            this.cbxDigitalFilter.Size = new System.Drawing.Size(205, 21);
            this.cbxDigitalFilter.TabIndex = 1;
            this.cbxDigitalFilter.SelectedIndexChanged += new System.EventHandler(this.cbxDigitalFilter_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 70);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Digital filter:";
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(315, 282);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(234, 282);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnMoreOrLess
            // 
            this.btnMoreOrLess.Location = new System.Drawing.Point(12, 282);
            this.btnMoreOrLess.Name = "btnMoreOrLess";
            this.btnMoreOrLess.Size = new System.Drawing.Size(98, 23);
            this.btnMoreOrLess.TabIndex = 6;
            this.btnMoreOrLess.Text = "More Options";
            this.btnMoreOrLess.UseVisualStyleBackColor = true;
            this.btnMoreOrLess.Click += new System.EventHandler(this.btnMoreOrLess_Click);
            // 
            // tabsOptions
            // 
            this.tabsOptions.Controls.Add(this.tabGeneral);
            this.tabsOptions.Controls.Add(this.tabIntegration);
            this.tabsOptions.Controls.Add(this.tabStretching);
            this.tabsOptions.Controls.Add(this.tabReduction);
            this.tabsOptions.Location = new System.Drawing.Point(12, 9);
            this.tabsOptions.Name = "tabsOptions";
            this.tabsOptions.SelectedIndex = 0;
            this.tabsOptions.Size = new System.Drawing.Size(378, 267);
            this.tabsOptions.TabIndex = 7;
            // 
            // tabGeneral
            // 
            this.tabGeneral.BackColor = System.Drawing.Color.Transparent;
            this.tabGeneral.Controls.Add(this.groupBox2);
            this.tabGeneral.Controls.Add(this.groupBox3);
            this.tabGeneral.Location = new System.Drawing.Point(4, 22);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabGeneral.Size = new System.Drawing.Size(370, 241);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            this.tabGeneral.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.pnlLunarType);
            this.groupBox2.Controls.Add(this.rbLunarEvent);
            this.groupBox2.Controls.Add(this.pnlMutualType);
            this.groupBox2.Controls.Add(this.rbVariableOrTransit);
            this.groupBox2.Controls.Add(this.rbAsteroidal);
            this.groupBox2.Controls.Add(this.rbUntrackedMeasurement);
            this.groupBox2.Controls.Add(this.rbMutualEvent);
            this.groupBox2.Location = new System.Drawing.Point(6, 6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(352, 136);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Measurement Type";
            // 
            // pnlLunarType
            // 
            this.pnlLunarType.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlLunarType.Controls.Add(this.rbLunarR);
            this.pnlLunarType.Controls.Add(this.rbLunarGraze);
            this.pnlLunarType.Controls.Add(this.rbLunarD);
            this.pnlLunarType.Location = new System.Drawing.Point(165, 84);
            this.pnlLunarType.Name = "pnlLunarType";
            this.pnlLunarType.Size = new System.Drawing.Size(181, 26);
            this.pnlLunarType.TabIndex = 19;
            this.pnlLunarType.Visible = false;
            // 
            // rbLunarR
            // 
            this.rbLunarR.AutoSize = true;
            this.rbLunarR.Location = new System.Drawing.Point(66, 5);
            this.rbLunarR.Name = "rbLunarR";
            this.rbLunarR.Size = new System.Drawing.Size(60, 17);
            this.rbLunarR.TabIndex = 17;
            this.rbLunarR.TabStop = true;
            this.rbLunarR.Text = "Total R";
            this.rbLunarR.UseVisualStyleBackColor = true;
            // 
            // rbLunarGraze
            // 
            this.rbLunarGraze.AutoSize = true;
            this.rbLunarGraze.Location = new System.Drawing.Point(128, 5);
            this.rbLunarGraze.Name = "rbLunarGraze";
            this.rbLunarGraze.Size = new System.Drawing.Size(53, 17);
            this.rbLunarGraze.TabIndex = 16;
            this.rbLunarGraze.TabStop = true;
            this.rbLunarGraze.Text = "Graze";
            this.rbLunarGraze.UseVisualStyleBackColor = true;
            // 
            // rbLunarD
            // 
            this.rbLunarD.AutoSize = true;
            this.rbLunarD.Location = new System.Drawing.Point(6, 5);
            this.rbLunarD.Name = "rbLunarD";
            this.rbLunarD.Size = new System.Drawing.Size(60, 17);
            this.rbLunarD.TabIndex = 0;
            this.rbLunarD.TabStop = true;
            this.rbLunarD.Text = "Total D";
            this.rbLunarD.UseVisualStyleBackColor = true;
            // 
            // rbLunarEvent
            // 
            this.rbLunarEvent.AutoSize = true;
            this.rbLunarEvent.Location = new System.Drawing.Point(13, 88);
            this.rbLunarEvent.Name = "rbLunarEvent";
            this.rbLunarEvent.Size = new System.Drawing.Size(152, 17);
            this.rbLunarEvent.TabIndex = 18;
            this.rbLunarEvent.Text = "Tracked Lunar Occultation";
            this.rbLunarEvent.UseVisualStyleBackColor = true;
            this.rbLunarEvent.CheckedChanged += new System.EventHandler(this.rbLunarEvent_CheckedChanged);
            // 
            // pnlMutualType
            // 
            this.pnlMutualType.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlMutualType.Controls.Add(this.rbMutualEcl);
            this.pnlMutualType.Controls.Add(this.rbMutualOcc);
            this.pnlMutualType.Location = new System.Drawing.Point(191, 35);
            this.pnlMutualType.Name = "pnlMutualType";
            this.pnlMutualType.Size = new System.Drawing.Size(155, 26);
            this.pnlMutualType.TabIndex = 15;
            this.pnlMutualType.Visible = false;
            // 
            // rbMutualEcl
            // 
            this.rbMutualEcl.AutoSize = true;
            this.rbMutualEcl.Location = new System.Drawing.Point(88, 5);
            this.rbMutualEcl.Name = "rbMutualEcl";
            this.rbMutualEcl.Size = new System.Drawing.Size(59, 17);
            this.rbMutualEcl.TabIndex = 16;
            this.rbMutualEcl.TabStop = true;
            this.rbMutualEcl.Text = "Eclipse";
            this.rbMutualEcl.UseVisualStyleBackColor = true;
            // 
            // rbMutualOcc
            // 
            this.rbMutualOcc.AutoSize = true;
            this.rbMutualOcc.Location = new System.Drawing.Point(6, 5);
            this.rbMutualOcc.Name = "rbMutualOcc";
            this.rbMutualOcc.Size = new System.Drawing.Size(79, 17);
            this.rbMutualOcc.TabIndex = 0;
            this.rbMutualOcc.TabStop = true;
            this.rbMutualOcc.Text = "Occultation";
            this.rbMutualOcc.UseVisualStyleBackColor = true;
            // 
            // rbVariableOrTransit
            // 
            this.rbVariableOrTransit.AutoSize = true;
            this.rbVariableOrTransit.Location = new System.Drawing.Point(13, 64);
            this.rbVariableOrTransit.Name = "rbVariableOrTransit";
            this.rbVariableOrTransit.Size = new System.Drawing.Size(206, 17);
            this.rbVariableOrTransit.TabIndex = 14;
            this.rbVariableOrTransit.Text = "Tracked Variable Star or Transit Event";
            this.rbVariableOrTransit.UseVisualStyleBackColor = true;
            this.rbVariableOrTransit.CheckedChanged += new System.EventHandler(this.rbVariableOrTransit_CheckedChanged);
            // 
            // rbUntrackedMeasurement
            // 
            this.rbUntrackedMeasurement.AutoSize = true;
            this.rbUntrackedMeasurement.Location = new System.Drawing.Point(13, 111);
            this.rbUntrackedMeasurement.Name = "rbUntrackedMeasurement";
            this.rbUntrackedMeasurement.Size = new System.Drawing.Size(142, 17);
            this.rbUntrackedMeasurement.TabIndex = 13;
            this.rbUntrackedMeasurement.Text = "Untracked Measurement";
            this.rbUntrackedMeasurement.UseVisualStyleBackColor = true;
            this.rbUntrackedMeasurement.CheckedChanged += new System.EventHandler(this.rbUntrackedMeasurement_CheckedChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.cbxDriftTrough);
            this.groupBox3.Controls.Add(this.cbxFullDisappearance);
            this.groupBox3.Controls.Add(this.cbxStopOnLostTracking);
            this.groupBox3.Controls.Add(this.rbFastTracking);
            this.groupBox3.Controls.Add(this.rbTrackingWithRecovery);
            this.groupBox3.Controls.Add(this.lblTrackingDescr);
            this.groupBox3.Location = new System.Drawing.Point(6, 145);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(352, 94);
            this.groupBox3.TabIndex = 17;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Tracking";
            // 
            // cbxDriftTrough
            // 
            this.cbxDriftTrough.AutoSize = true;
            this.cbxDriftTrough.Location = new System.Drawing.Point(136, 74);
            this.cbxDriftTrough.Name = "cbxDriftTrough";
            this.cbxDriftTrough.Size = new System.Drawing.Size(88, 17);
            this.cbxDriftTrough.TabIndex = 15;
            this.cbxDriftTrough.Text = "Drift Through";
            this.cbxDriftTrough.UseVisualStyleBackColor = true;
            this.cbxDriftTrough.Visible = false;
            // 
            // cbxFullDisappearance
            // 
            this.cbxFullDisappearance.AutoSize = true;
            this.cbxFullDisappearance.Location = new System.Drawing.Point(13, 73);
            this.cbxFullDisappearance.Name = "cbxFullDisappearance";
            this.cbxFullDisappearance.Size = new System.Drawing.Size(117, 17);
            this.cbxFullDisappearance.TabIndex = 14;
            this.cbxFullDisappearance.Text = "Full Disappearance";
            this.cbxFullDisappearance.UseVisualStyleBackColor = true;
            this.cbxFullDisappearance.Visible = false;
            this.cbxFullDisappearance.CheckedChanged += new System.EventHandler(this.cbxFullDisappearance_CheckedChanged);
            // 
            // cbxStopOnLostTracking
            // 
            this.cbxStopOnLostTracking.AutoSize = true;
            this.cbxStopOnLostTracking.Location = new System.Drawing.Point(224, 74);
            this.cbxStopOnLostTracking.Name = "cbxStopOnLostTracking";
            this.cbxStopOnLostTracking.Size = new System.Drawing.Size(123, 17);
            this.cbxStopOnLostTracking.TabIndex = 12;
            this.cbxStopOnLostTracking.Text = "Stop on lost tracking";
            this.cbxStopOnLostTracking.UseVisualStyleBackColor = true;
            this.cbxStopOnLostTracking.Visible = false;
            // 
            // rbFastTracking
            // 
            this.rbFastTracking.AutoSize = true;
            this.rbFastTracking.Location = new System.Drawing.Point(13, 19);
            this.rbFastTracking.Name = "rbFastTracking";
            this.rbFastTracking.Size = new System.Drawing.Size(54, 17);
            this.rbFastTracking.TabIndex = 0;
            this.rbFastTracking.Text = "Faster";
            this.rbFastTracking.UseVisualStyleBackColor = true;
            this.rbFastTracking.CheckedChanged += new System.EventHandler(this.TrackingModeChanged);
            // 
            // rbTrackingWithRecovery
            // 
            this.rbTrackingWithRecovery.AutoSize = true;
            this.rbTrackingWithRecovery.Checked = true;
            this.rbTrackingWithRecovery.Location = new System.Drawing.Point(73, 19);
            this.rbTrackingWithRecovery.Name = "rbTrackingWithRecovery";
            this.rbTrackingWithRecovery.Size = new System.Drawing.Size(95, 17);
            this.rbTrackingWithRecovery.TabIndex = 1;
            this.rbTrackingWithRecovery.TabStop = true;
            this.rbTrackingWithRecovery.Text = "More Accurate";
            this.rbTrackingWithRecovery.UseVisualStyleBackColor = true;
            this.rbTrackingWithRecovery.CheckedChanged += new System.EventHandler(this.TrackingModeChanged);
            // 
            // lblTrackingDescr
            // 
            this.lblTrackingDescr.ForeColor = System.Drawing.Color.Navy;
            this.lblTrackingDescr.Location = new System.Drawing.Point(11, 43);
            this.lblTrackingDescr.Name = "lblTrackingDescr";
            this.lblTrackingDescr.Size = new System.Drawing.Size(333, 36);
            this.lblTrackingDescr.TabIndex = 16;
            // 
            // tabIntegration
            // 
            this.tabIntegration.BackColor = System.Drawing.Color.Transparent;
            this.tabIntegration.Controls.Add(this.groupBox1);
            this.tabIntegration.Location = new System.Drawing.Point(4, 22);
            this.tabIntegration.Name = "tabIntegration";
            this.tabIntegration.Padding = new System.Windows.Forms.Padding(3);
            this.tabIntegration.Size = new System.Drawing.Size(370, 241);
            this.tabIntegration.TabIndex = 2;
            this.tabIntegration.Text = "Integration";
            this.tabIntegration.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.gbxPixelIntegration);
            this.groupBox1.Controls.Add(this.pnlFrameCount);
            this.groupBox1.Controls.Add(this.rbNoIntegration);
            this.groupBox1.Controls.Add(this.rbBinning);
            this.groupBox1.Controls.Add(this.rbRunningAverage);
            this.groupBox1.Location = new System.Drawing.Point(6, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(358, 210);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Software Integration";
            // 
            // gbxPixelIntegration
            // 
            this.gbxPixelIntegration.Controls.Add(this.rbMean);
            this.gbxPixelIntegration.Controls.Add(this.rbMedian);
            this.gbxPixelIntegration.Enabled = false;
            this.gbxPixelIntegration.Location = new System.Drawing.Point(13, 121);
            this.gbxPixelIntegration.Name = "gbxPixelIntegration";
            this.gbxPixelIntegration.Size = new System.Drawing.Size(330, 83);
            this.gbxPixelIntegration.TabIndex = 2;
            this.gbxPixelIntegration.TabStop = false;
            this.gbxPixelIntegration.Text = "Pixel Averaging";
            // 
            // rbMean
            // 
            this.rbMean.AutoSize = true;
            this.rbMean.Checked = true;
            this.rbMean.Location = new System.Drawing.Point(86, 32);
            this.rbMean.Name = "rbMean";
            this.rbMean.Size = new System.Drawing.Size(52, 17);
            this.rbMean.TabIndex = 2;
            this.rbMean.TabStop = true;
            this.rbMean.Text = "Mean";
            this.rbMean.UseVisualStyleBackColor = true;
            this.rbMean.CheckedChanged += new System.EventHandler(this.rbMean_CheckedChanged);
            // 
            // rbMedian
            // 
            this.rbMedian.AutoSize = true;
            this.rbMedian.Location = new System.Drawing.Point(180, 32);
            this.rbMedian.Name = "rbMedian";
            this.rbMedian.Size = new System.Drawing.Size(60, 17);
            this.rbMedian.TabIndex = 0;
            this.rbMedian.Text = "Median";
            this.rbMedian.UseVisualStyleBackColor = true;
            // 
            // pnlFrameCount
            // 
            this.pnlFrameCount.Controls.Add(this.label5);
            this.pnlFrameCount.Controls.Add(this.nudIntegrateFrames);
            this.pnlFrameCount.Enabled = false;
            this.pnlFrameCount.Location = new System.Drawing.Point(177, 52);
            this.pnlFrameCount.Name = "pnlFrameCount";
            this.pnlFrameCount.Size = new System.Drawing.Size(156, 41);
            this.pnlFrameCount.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 2);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(149, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Number of frames to integrate:";
            // 
            // nudIntegrateFrames
            // 
            this.nudIntegrateFrames.Location = new System.Drawing.Point(6, 18);
            this.nudIntegrateFrames.Maximum = new decimal(new int[] {
            32,
            0,
            0,
            0});
            this.nudIntegrateFrames.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.nudIntegrateFrames.Name = "nudIntegrateFrames";
            this.nudIntegrateFrames.Size = new System.Drawing.Size(56, 20);
            this.nudIntegrateFrames.TabIndex = 3;
            this.nudIntegrateFrames.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.nudIntegrateFrames.ValueChanged += new System.EventHandler(this.nudIntegrateFrames_ValueChanged);
            // 
            // rbNoIntegration
            // 
            this.rbNoIntegration.AutoSize = true;
            this.rbNoIntegration.Checked = true;
            this.rbNoIntegration.Location = new System.Drawing.Point(13, 27);
            this.rbNoIntegration.Name = "rbNoIntegration";
            this.rbNoIntegration.Size = new System.Drawing.Size(92, 17);
            this.rbNoIntegration.TabIndex = 2;
            this.rbNoIntegration.TabStop = true;
            this.rbNoIntegration.Text = "No Integration";
            this.rbNoIntegration.UseVisualStyleBackColor = true;
            this.rbNoIntegration.CheckedChanged += new System.EventHandler(this.rbNoIntegration_CheckedChanged);
            // 
            // rbBinning
            // 
            this.rbBinning.AutoSize = true;
            this.rbBinning.Location = new System.Drawing.Point(13, 50);
            this.rbBinning.Name = "rbBinning";
            this.rbBinning.Size = new System.Drawing.Size(116, 17);
            this.rbBinning.TabIndex = 3;
            this.rbBinning.Text = "Stepped Averaging";
            this.rbBinning.UseVisualStyleBackColor = true;
            this.rbBinning.CheckedChanged += new System.EventHandler(this.rbNoIntegration_CheckedChanged);
            // 
            // rbRunningAverage
            // 
            this.rbRunningAverage.AutoSize = true;
            this.rbRunningAverage.Location = new System.Drawing.Point(13, 73);
            this.rbRunningAverage.Name = "rbRunningAverage";
            this.rbRunningAverage.Size = new System.Drawing.Size(107, 17);
            this.rbRunningAverage.TabIndex = 4;
            this.rbRunningAverage.Text = "Sliding Averaging";
            this.rbRunningAverage.UseVisualStyleBackColor = true;
            this.rbRunningAverage.CheckedChanged += new System.EventHandler(this.rbNoIntegration_CheckedChanged);
            // 
            // tabStretching
            // 
            this.tabStretching.BackColor = System.Drawing.Color.Transparent;
            this.tabStretching.Controls.Add(this.ucStretching);
            this.tabStretching.Location = new System.Drawing.Point(4, 22);
            this.tabStretching.Name = "tabStretching";
            this.tabStretching.Padding = new System.Windows.Forms.Padding(3);
            this.tabStretching.Size = new System.Drawing.Size(370, 241);
            this.tabStretching.TabIndex = 3;
            this.tabStretching.Text = "Pre-Processing";
            this.tabStretching.UseVisualStyleBackColor = true;
            // 
            // ucStretching
            // 
            this.ucStretching.Location = new System.Drawing.Point(0, 0);
            this.ucStretching.Name = "ucStretching";
            this.ucStretching.Size = new System.Drawing.Size(370, 221);
            this.ucStretching.TabIndex = 0;
            // 
            // tabReduction
            // 
            this.tabReduction.BackColor = System.Drawing.Color.Transparent;
            this.tabReduction.Controls.Add(this.pnlBackground);
            this.tabReduction.Controls.Add(this.label3);
            this.tabReduction.Controls.Add(this.cbxReductionType);
            this.tabReduction.Controls.Add(this.label1);
            this.tabReduction.Controls.Add(this.cbxDigitalFilter);
            this.tabReduction.Location = new System.Drawing.Point(4, 22);
            this.tabReduction.Name = "tabReduction";
            this.tabReduction.Padding = new System.Windows.Forms.Padding(3);
            this.tabReduction.Size = new System.Drawing.Size(370, 241);
            this.tabReduction.TabIndex = 1;
            this.tabReduction.Text = "Reduction Settings";
            this.tabReduction.UseVisualStyleBackColor = true;
            // 
            // pnlBackground
            // 
            this.pnlBackground.Controls.Add(this.label2);
            this.pnlBackground.Controls.Add(this.cbxBackgroundMethod);
            this.pnlBackground.Location = new System.Drawing.Point(6, 124);
            this.pnlBackground.Name = "pnlBackground";
            this.pnlBackground.Size = new System.Drawing.Size(291, 40);
            this.pnlBackground.TabIndex = 6;
            // 
            // frmSelectReductionType
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 315);
            this.Controls.Add(this.tabsOptions);
            this.Controls.Add(this.btnMoreOrLess);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmSelectReductionType";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Light Curve Reduction";
            this.Load += new System.EventHandler(this.frmSelectReductionType_Load);
            this.Move += new System.EventHandler(this.frmSelectReductionType_Move);
            this.tabsOptions.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.pnlLunarType.ResumeLayout(false);
            this.pnlLunarType.PerformLayout();
            this.pnlMutualType.ResumeLayout(false);
            this.pnlMutualType.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tabIntegration.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.gbxPixelIntegration.ResumeLayout(false);
            this.gbxPixelIntegration.PerformLayout();
            this.pnlFrameCount.ResumeLayout(false);
            this.pnlFrameCount.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudIntegrateFrames)).EndInit();
            this.tabStretching.ResumeLayout(false);
            this.tabReduction.ResumeLayout(false);
            this.tabReduction.PerformLayout();
            this.pnlBackground.ResumeLayout(false);
            this.pnlBackground.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RadioButton rbAsteroidal;
        private System.Windows.Forms.RadioButton rbMutualEvent;
        private System.Windows.Forms.ComboBox cbxBackgroundMethod;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbxDigitalFilter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.ComboBox cbxReductionType;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnMoreOrLess;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.TabPage tabReduction;
        private System.Windows.Forms.TabPage tabIntegration;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbNoIntegration;
        private System.Windows.Forms.RadioButton rbBinning;
        private System.Windows.Forms.RadioButton rbRunningAverage;
        private System.Windows.Forms.GroupBox gbxPixelIntegration;
        private System.Windows.Forms.RadioButton rbMean;
        private System.Windows.Forms.RadioButton rbMedian;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown nudIntegrateFrames;
        private System.Windows.Forms.TabControl tabsOptions;
        private System.Windows.Forms.Panel pnlFrameCount;
		private System.Windows.Forms.TabPage tabStretching;
        private System.Windows.Forms.CheckBox cbxStopOnLostTracking;
        private System.Windows.Forms.RadioButton rbUntrackedMeasurement;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Panel pnlBackground;
        private ucPreProcessing ucStretching;
		private System.Windows.Forms.RadioButton rbVariableOrTransit;
		private System.Windows.Forms.Panel pnlMutualType;
		private System.Windows.Forms.RadioButton rbMutualEcl;
        private System.Windows.Forms.RadioButton rbMutualOcc;
		private System.Windows.Forms.Panel pnlLunarType;
		private System.Windows.Forms.RadioButton rbLunarR;
		private System.Windows.Forms.RadioButton rbLunarGraze;
		private System.Windows.Forms.RadioButton rbLunarD;
		private System.Windows.Forms.RadioButton rbLunarEvent;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RadioButton rbFastTracking;
        private System.Windows.Forms.RadioButton rbTrackingWithRecovery;
        private System.Windows.Forms.CheckBox cbxFullDisappearance;
        private System.Windows.Forms.CheckBox cbxDriftTrough;
        private System.Windows.Forms.Label lblTrackingDescr;
    }
}