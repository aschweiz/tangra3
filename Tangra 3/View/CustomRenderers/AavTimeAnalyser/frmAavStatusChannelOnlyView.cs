﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tangra.Controller;
using Tangra.Model.Helpers;
using Tangra.Video;

namespace Tangra.View.CustomRenderers.AavTimeAnalyser
{
    public partial class frmAavStatusChannelOnlyView : Form
    {
        internal enum GraphType
        {
            gtNone,
            gtTimeDeltasLines,
            gtTimeDeltasDots,
            gtSystemUtilisation,
            gtNtpUpdates,
            gtNtpUpdatesInclUnapplied
        }

        internal enum GridlineStyle
        {
            Line,
            Tick
        }

        internal class GraphConfig
        {
            public GridlineStyle GridlineStyle = GridlineStyle.Line;
        }

        private GraphType m_GrapthType = GraphType.gtNone;

        private VideoController m_VideoController;
        private AstroDigitalVideoStream m_Aav;
        private AavTimeAnalyser m_TimeAnalyser;

        private int m_LastGraphWidth;
        private int m_LastGraphHeight;
        private int? m_PlotFromIndex = null;
        private int? m_PlotToIndex = null;

        const float MIN_PIX_DIFF = 1f;

        private bool m_IsDataReady;

        private GraphConfig m_GraphConfig = new GraphConfig();

        private static Font m_TitleFont = new Font(DefaultFont, FontStyle.Bold);

        public frmAavStatusChannelOnlyView()
        {
            InitializeComponent();
        }

        public frmAavStatusChannelOnlyView(VideoController videoController, AstroDigitalVideoStream aav)
            : this()
        {
            m_Aav = aav;
            m_VideoController = videoController;
            m_TimeAnalyser = new AavTimeAnalyser(aav);
        }

        private void UpdateProgressBarImpl(int val, int max, ProgressBar pbar, Action onFinished)
        {
            if (val == 0 && max == 0)
            {
                pbar.Visible = false;
                onFinished();
            }
            else if (val == 0 && max > 0)
            {
                pbar.Maximum = max;
                pbar.Value = 0;
                pbar.Visible = true;
                tabControl.SelectedTab = tabOverview;
            }
            else
            {
                pbar.Value = Math.Min(val, max);
                pbar.Update();
            }
        }

        private void UpdateProgressBar(int val, int max)
        {
            UpdateProgressBarImpl(val, max, pbLoadData, OnTimeAnalysisDataReady);
        }

        private void UpdateExportProgressBar(int val, int max)
        {
            UpdateProgressBarImpl(val, max, pbLoadData, OnExportFinished);
        }

        private void frmAavStatusChannelOnlyView_Load(object sender, EventArgs e)
        {
            Text = string.Format("AAV Time Analysis: {0}", m_Aav.FileName);

            m_IsDataReady = false;
            pbLoadData.Visible = true;
            Height = Height + 10 + (Math.Max(480, m_Aav.Height) - pbOcrErrorFrame.Height);
            Width = 10 + Math.Max(800, m_Aav.Width);

            m_TimeAnalyser.Initialize((val, max) =>
            {
                this.Invoke(new Action<int, int>(UpdateProgressBar), val, max);
            });
        }

        private void OnTimeAnalysisDataReady()
        {
            nudOcrErrorFrame.Enabled = m_TimeAnalyser.DebugFrames.Count > 0;
            if (nudOcrErrorFrame.Enabled)
            {
                nudOcrErrorFrame.Maximum = m_TimeAnalyser.DebugFrames.Count - 1;
                nudOcrErrorFrame.Minimum = 0;
                nudOcrErrorFrame.Value = 0;
            }

            cbxGraphType.SelectedIndex = 0;
            m_IsDataReady = true;
            
            ShowOcrErrorFrame();

            tabControl.SelectedTab = tabGraphs;
            DrawGraph();

            AnalyseData();

            tbxAnalysisDetails.Visible = true;
        }

        private void AnalyseData()
        {
            tbxAnalysisDetails.Clear();

            var nonOutliers = m_TimeAnalyser.Entries.Where(x => !x.IsOutlier).ToList();
            var medianSystemFileTime = nonOutliers.Select(x => x.DeltaSystemFileTimeMs).Median();
            double medianSystemFileTimeVariance = nonOutliers.Sum(x => (medianSystemFileTime - x.DeltaSystemFileTimeMs) * (medianSystemFileTime - x.DeltaSystemFileTimeMs));
            medianSystemFileTimeVariance = Math.Sqrt(medianSystemFileTimeVariance / nonOutliers.Count);

            var medianSystemTime = nonOutliers.Select(x => x.DeltaSystemTimeMs).Median();
            double medianSystemTimeVariance = nonOutliers.Sum(x => (medianSystemTime - x.DeltaSystemTimeMs) * (medianSystemTime - x.DeltaSystemTimeMs));
            medianSystemTimeVariance = Math.Sqrt(medianSystemTimeVariance / nonOutliers.Count);

            var medianNtpDiff = nonOutliers.Select(x => Math.Abs(x.DeltaSystemTimeMs - x.DeltaNTPTimeMs)).Median();

            tbxAnalysisDetails.AppendText(string.Format("SystemTimeAsFileTime: {0:0.0} ms +/- {1:0.00} ms    SystemTime: {2:0.0} ms +/- {3:0.00} ms    MedianNTPDiff: {4:0.0} ms\r\n\r\n", medianSystemFileTime, medianSystemFileTimeVariance, medianSystemTime, medianSystemTimeVariance, medianNtpDiff));
            tbxAnalysisDetails.AppendText(string.Format("Acqusition Delay: {0:0.0} ms    RecordingDelay: {1:0.0} ms\r\n\r\n", medianSystemFileTime, medianSystemTime - medianSystemFileTime));

            for (int i = 0; i < m_TimeAnalyser.Entries.Count; i++)
            {
                var ol = m_TimeAnalyser.Entries[i];

                var delta = ol.DeltaSystemFileTimeMs - medianSystemFileTime;
                if (Math.Abs(delta) > 6 * medianSystemFileTimeVariance)
                {
                    var ol2 = i == 0 ? m_TimeAnalyser.Entries[1] : m_TimeAnalyser.Entries[i - 1];
                    var utilEntry = ol.UtilisationEntry;
                    if (utilEntry == null) utilEntry = m_TimeAnalyser.Entries.Last(x => x.UtilisationEntry != null).UtilisationEntry;
                    tbxAnalysisDetails.AppendText(string.Format("Outlier at {0}, FrameNo: {1} Delta: {2:0.0} ms; Non-Acquisition Delay: {3:0.0} ms; CPU: {4:0.0}% Disks: {5:0.0}%  {6}\r\n", ol.SystemTimeFileTime.ToString("HH:mm:ss.fff"), ol.FrameNo, delta, (ol2.DeltaNTPTimeMs - ol2.DeltaSystemFileTimeMs) - (ol.DeltaNTPTimeMs - ol.DeltaSystemFileTimeMs), utilEntry.CpuUtilisation, utilEntry.DiskUtilisation, ol.DebugImage != null ? "OCR-ERR:" + ol.OrcField1 + "  " + ol.OrcField2 : null));
                }
            }
        }

        private void DrawGraph()
        {
            if (!m_IsDataReady) return;

            switch (m_GrapthType)
            {
                case GraphType.gtTimeDeltasLines:
                case GraphType.gtTimeDeltasDots:
                    DrawTimeDeltasGraph();
                    break;
                case GraphType.gtSystemUtilisation:
                    DrawSystemUtilisationGraph();
                    break;
                case GraphType.gtNtpUpdates:
                case GraphType.gtNtpUpdatesInclUnapplied:
                    DrawNtpUpdatesGraph();
                    break;
            }
        }

        private void DrawTimeDeltasGraph()
        {
            Cursor = Cursors.WaitCursor;

            bool plotSystemTime = rbSystemTime.Checked;
            bool plotOccuRecTime = cbxNtpTime.Checked;
            bool plotNtpError = cbxNtpError.Checked;
            int graphWidth = pbGraph.Width;
            int graphHeight = pbGraph.Height;
            var image = new Bitmap(graphWidth, graphHeight, PixelFormat.Format24bppRgb);

            bool ellipses = m_GrapthType == GraphType.gtTimeDeltasDots;
            bool tickGridlines = m_GraphConfig.GridlineStyle == GridlineStyle.Tick;

            Task.Run(() =>
            {
                Pen SysTimePen = Pens.Red;
                Brush SysTimeBrush = Brushes.Red;
                Pen NtpTimePen = Pens.LimeGreen;
                Brush NtpTimeBrush = Brushes.LimeGreen;
                Pen NtpErrorPen = Pens.DarkKhaki;
                Brush NtpErrorBrush = Brushes.DarkKhaki;

                float maxDelta = 0;
                float minDelta = 0;

                bool isSubset = m_PlotFromIndex.HasValue && m_PlotToIndex.HasValue;
                IEnumerable<TimeAnalyserEntry> subsetEnum = null;
                if (isSubset)
                {
                    subsetEnum = m_TimeAnalyser.Entries.Skip(m_PlotFromIndex.Value).Take(m_PlotToIndex.Value);
                }
                
                if (plotSystemTime)
                {
                    if (isSubset)
                    {
                        minDelta = subsetEnum.Min(x => x.DeltaSystemTimeMs);
                        maxDelta = subsetEnum.Max(x => x.DeltaSystemTimeMs);
                    }
                    else
                    {
                        minDelta = m_TimeAnalyser.MinDeltaSystemTimeMs;
                        maxDelta = m_TimeAnalyser.MaxDeltaSystemTimeMs;
                    }
                }
                else
                {
                    if (isSubset)
                    {
                        minDelta = subsetEnum.Min(x => x.DeltaSystemFileTimeMs);
                        maxDelta = subsetEnum.Max(x => x.DeltaSystemFileTimeMs);
                    }
                    else
                    {
                        minDelta = m_TimeAnalyser.MinDeltaSystemFileTimeMs;
                        maxDelta = m_TimeAnalyser.MaxDeltaSystemFileTimeMs;
                    }
                }

                if (plotOccuRecTime)
                {
                    if (isSubset)
                    {
                        var minNtpDelta = subsetEnum.Min(x => x.DeltaNTPTimeMs);
                        var maxNtpDelta = subsetEnum.Max(x => x.DeltaNTPTimeMs);
                        minDelta = Math.Min(minDelta, minNtpDelta);
                        maxDelta = Math.Max(maxDelta, maxNtpDelta);
                    }
                    else
                    {
                        minDelta = Math.Min(minDelta, m_TimeAnalyser.MinDeltaNTPMs);
                        maxDelta = Math.Max(maxDelta, m_TimeAnalyser.MaxDeltaNTPMs);
                    }
                }

                if (plotNtpError)
                {
                    if (isSubset)
                    {
                        var maxNtpErr = subsetEnum.Max(x => x.NTPErrorMs);
                        minDelta = Math.Min(minDelta, -maxNtpErr);
                        maxDelta = Math.Max(maxDelta, maxNtpErr);
                    }
                    else
                    {
                        minDelta = Math.Min(minDelta, m_TimeAnalyser.MinDeltaNTPErrorMs);
                        maxDelta = Math.Max(maxDelta, m_TimeAnalyser.MaxDeltaNTPErrorMs);
                    }
                }

                // Extend the Y range by 50ms for better display
                minDelta -= 50;
                maxDelta += 50;

                using (var g = Graphics.FromImage(image))
                {
                    float padding = 10;
                    float paddingX = 40;
                    float paddingY = 25;
                    float width = graphWidth - padding - paddingX;
                    float height = graphHeight - 2 * paddingY;
                    float yFactor = height / (maxDelta - minDelta);
                    float minX = m_PlotFromIndex.HasValue ? m_PlotFromIndex.Value : 0;
                    float maxX = m_PlotToIndex.HasValue ? m_PlotToIndex.Value : m_TimeAnalyser.Entries.Count - 1;
                    float xFactor = width / (maxX - minX + 1);

                    g.FillRectangle(Brushes.WhiteSmoke, 0, 0, graphWidth, graphHeight);

                    for (int ya = -2000; ya < 2000; ya += 100)
                    {
                        float y = graphHeight - paddingY - yFactor * (ya - minDelta);
                        if (y < paddingY || y > graphHeight - paddingY) continue;

                        if (tickGridlines && ya != 0 /* The zero grid line is fully drawn even in 'tick' mode */)
                        {
                            g.DrawLine(Pens.Gray, paddingX, y, paddingX + 5, y);
                            g.DrawLine(Pens.Gray, graphWidth - padding - 5, y, graphWidth - padding, y);
                        }
                        else
                        {
                            g.DrawLine(Pens.Gray, paddingX, y, graphWidth - padding, y);
                        }
                        
                        if (ya == 0)
                        {
                            if (tickGridlines)
                            {
                                g.DrawLine(Pens.Gray, paddingX, y - 1, paddingX + 5, y - 1);
                                g.DrawLine(Pens.Gray, graphWidth - padding - 5, y - 1, graphWidth - padding, y - 1);
                                g.DrawLine(Pens.Gray, paddingX, y + 1, paddingX + 5, y + 1);
                                g.DrawLine(Pens.Gray, graphWidth - padding - 5, y + 1, graphWidth - padding, y + 1);
                            }
                            else
                            {
                                g.DrawLine(Pens.Gray, paddingX, y - 1, graphWidth - padding, y - 1);
                                g.DrawLine(Pens.Gray, paddingX, y + 1, graphWidth - padding, y + 1);
                            }
                            var sizF = g.MeasureString("0", DefaultFont);
                            g.DrawString("0", DefaultFont, Brushes.Black, paddingX - sizF.Width - 5, y - sizF.Height / 2);
                        }
                        else if (ya % 100 == 0)
                        {
                            var label = string.Format("{0:0.0} s", ya / 1000.0);
                            var sizF = g.MeasureString(label, DefaultFont);
                            g.DrawString(label, DefaultFont, Brushes.Black, paddingX - sizF.Width - 5, y - sizF.Height / 2);
                        }
                    }

                    int idx = 0;
                    float y1p = 0;
                    float y2p = 0;
                    float y3pt = 0;
                    float y3pb = 0;
                    float xp1 = 0;
                    float xp2 = 0;
                    float xp3 = 0;

                    float y1 = 0;
                    float y2 = 0;
                    float y3t = 0;
                    float y3b = 0;

                    foreach (var entry in m_TimeAnalyser.Entries)
                    {
                        if (entry.IsOutlier) continue;
                        if (m_PlotFromIndex.HasValue && m_PlotFromIndex.Value > idx)
                        {
                            idx++;
                            continue;
                        }
                        if (m_PlotToIndex.HasValue && m_PlotToIndex.Value < idx)
                        {
                            idx++;
                            continue;
                        }

                        float x = paddingX + (idx - minX) * xFactor;
                        bool calcOnly = idx == 0;

                        if (plotNtpError)
                        {
                            y3t = graphHeight - paddingY - yFactor * (entry.NTPErrorMs - minDelta);
                            y3b = graphHeight - paddingY - yFactor * (-entry.NTPErrorMs - minDelta);
                            if (!calcOnly && (x - xp3 > MIN_PIX_DIFF || Math.Abs(y3t - y3pt) > MIN_PIX_DIFF || Math.Abs(y3b - y3pb) > MIN_PIX_DIFF))
                            {
                                if (ellipses)
                                {
                                    g.FillEllipse(NtpErrorBrush, x - 1, y3t - 1, 2, 2);
                                    g.FillEllipse(NtpErrorBrush, x - 1, y3b - 1, 2, 2);
                                }
                                else
                                {
                                    g.DrawLine(NtpErrorPen, xp3, y3pt, x, y3t);
                                    g.DrawLine(NtpErrorPen, xp3, y3pb, x, y3b); 
                                }
                                xp3 = x;
                                y3pt = y3t;
                                y3pb = y3b;
                            }
                        }

                        if (plotOccuRecTime)
                        {
                            y1 = graphHeight - paddingY - yFactor * (entry.DeltaNTPTimeMs - minDelta);
                            if (!calcOnly && (x - xp1 > MIN_PIX_DIFF || Math.Abs(y1 - y1p) > MIN_PIX_DIFF))
                            {
                                if (ellipses)
                                {
                                    g.FillEllipse(NtpTimeBrush, x - 1, y1 - 1, 2, 2);
                                }
                                else
                                {
                                    g.DrawLine(NtpTimePen, xp1, y1p, x, y1);
                                }
                                xp1 = x;
                                y1p = y1;
                            }
                        }

                        if (plotSystemTime)
                            y2 = graphHeight - paddingY - yFactor * (entry.DeltaSystemTimeMs - minDelta);
                        else
                            y2 = graphHeight - paddingY - yFactor * (entry.DeltaSystemFileTimeMs - minDelta);

                        if (!calcOnly && (x - xp2 > MIN_PIX_DIFF || Math.Abs(y2 - y2p) > MIN_PIX_DIFF))
                        {
                            if (ellipses)
                            {
                                g.FillEllipse(SysTimeBrush, x - 1, y2 - 1, 2, 2);
                            }
                            else
                            {
                                g.DrawLine(SysTimePen, xp2, y2p, x, y2);
                            }
                            xp2 = x;
                            y2p = y2;
                        }

                        if (idx == 0)
                        {
                            y1p = y1;
                            y2p = y2;
                            y3pt = y3t;
                            y3pb = y3b;
                            xp1 = x;
                            xp2 = x;
                            xp3 = x;
                        }

                        idx++;
                    }

                    g.DrawRectangle(Pens.Black, paddingX, paddingY, graphWidth - padding - paddingX, graphHeight - 2 * paddingY);

                    var title = string.Format("Time Delta analysis of {0:0.0} million data points recorded between {1} UT and {2} UT", m_TimeAnalyser.Entries.Count / 1000000.0, m_TimeAnalyser.FromDateTime.ToString("dd-MMM HH:mm"), m_TimeAnalyser.ToDateTime.ToString("dd-MMM HH:mm"));
                    var sizeF = g.MeasureString(title, m_TitleFont);
                    g.DrawString(title, m_TitleFont, Brushes.Black, (width - sizeF.Width) / 2 + paddingX, (paddingY - sizeF.Height) / 2);

                    var thirdW = width / 3;
                    int legPos = -1;
                    for (int i = 0; i < 3; i++)
                    {
                        string legend = "";
                        Pen legendPen = Pens.Black;
                        if (i == 0)
                        {
                            legend = plotSystemTime ? "GetSystemTime()" : "GetSystemTimePreciseAsFileTime()";
                            legendPen = SysTimePen;
                        }
                        else if (i == 1)
                        {
                            if (plotOccuRecTime)
                            {
                                legend = "OccuRec's Time Keeping (NTP reference)";
                                legendPen = NtpTimePen;
                            }
                            else continue;
                        }
                        else if (i == 2)
                        {
                            if (plotNtpError)
                            {
                                legend = "Max 3-Sigma NTP Error";
                                legendPen = NtpErrorPen;
                            }
                            else continue;
                        }

                        legPos++;

                        sizeF = g.MeasureString(legend, m_TitleFont);
                        var y = paddingY + height + sizeF.Height / 2;
                        var yl = paddingY + height + sizeF.Height;
                        g.DrawString(legend, m_TitleFont, Brushes.Black, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW + 15, y);
                        g.DrawLine(legendPen, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl - 1, 6 + (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl - 1);
                        g.DrawLine(legendPen, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl, 6 + (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl);
                        g.DrawLine(legendPen, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl + 1, 6 + (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl + 1);
                    }

                    g.Save();
                }
            }).ContinueWith((r) =>
            {
                if (r.IsCompleted)
                {
                    this.Invoke(new Action(() =>
                    {
                        if (pbGraph.Image != null)
                        {
                            pbGraph.Image.Dispose();
                        }

                        pbGraph.Image = image;
                        pbGraph.Update();

                        m_LastGraphWidth = pbGraph.Width;
                        m_LastGraphHeight = pbGraph.Height;

                        Cursor = Cursors.Default;
                    }));
                }
                else
                {
                    Cursor = Cursors.Default;
                }
            });
        }


        private void DrawSystemUtilisationGraph()
        {
            Cursor = Cursors.WaitCursor;

            int graphWidth = pbGraph.Width;
            int graphHeight = pbGraph.Height;

            bool tickGridlines = m_GraphConfig.GridlineStyle == GridlineStyle.Tick;

            var image = new Bitmap(graphWidth, graphHeight, PixelFormat.Format24bppRgb);

            Task.Run(() =>
            {
                Pen CpuPen = Pens.Blue;
                Pen DisksPen = Pens.Red;

                using (var g = Graphics.FromImage(image))
                {
                    float padding = 10;
                    float paddingX = 45;
                    float paddingY = 25;
                    float width = graphWidth - padding - paddingX;
                    float height = graphHeight - 2*paddingY;
                    float yFactor = height/100.0f; // 100% Max
                    float minX = 0;
                    float maxX = m_TimeAnalyser.SystemUtilisation.Count - 1;
                    float xFactor = width/(maxX - minX + 1);

                    g.FillRectangle(Brushes.WhiteSmoke, 0, 0, graphWidth, graphHeight);

                    for (int ya = 0; ya <= 100; ya += 10)
                    {
                        float y = graphHeight - paddingY - yFactor * ya;
                        if (y < paddingY || y > graphHeight - paddingY) continue;

                        if (tickGridlines && ya != 0 /* The zero grid line is fully drawn even in 'tick' mode */)
                        {
                            g.DrawLine(Pens.Gray, paddingX, y, paddingX + 5, y);
                            g.DrawLine(Pens.Gray, graphWidth - padding - 5, y, graphWidth - padding, y);
                        }
                        else
                        {
                            g.DrawLine(Pens.Gray, paddingX, y, graphWidth - padding, y);
                        }

                        if (ya % 20 == 0)
                        {
                            var label = string.Format("{0}%", ya);
                            var sizF = g.MeasureString(label, DefaultFont);
                            g.DrawString(label, DefaultFont, Brushes.Black, paddingX - sizF.Width - 5, y - sizF.Height / 2);
                        }
                    }

                    int idx = 0;
                    float xp = 0;
                    float y1p = 0;
                    float y2p = 0;

                    foreach (var utilEntry in m_TimeAnalyser.SystemUtilisation)
                    {
                        float x = paddingX + idx * xFactor;
                        float y1 = graphHeight - paddingY - yFactor * utilEntry.CpuUtilisation;
                        float y2 = graphHeight - paddingY - yFactor * Math.Min(100, utilEntry.DiskUtilisation); // NOTE: Disk utilisation of more than 100% is possible when there are more than 1 disks

                        if (idx > 0)
                        {
                            g.DrawLine(CpuPen, xp, y1p, x, y1);
                            g.DrawLine(DisksPen, xp, y2p, x, y2);
                        }

                        xp = x;
                        y1p = y1;
                        y2p = y2;

                        idx++;
                    }

                    g.DrawRectangle(Pens.Black, paddingX, paddingY, graphWidth - padding - paddingX, graphHeight - 2 * paddingY);

                    var title = string.Format("System utilisation between {0} UT and {1} UT", m_TimeAnalyser.FromDateTime.ToString("dd-MMM HH:mm"), m_TimeAnalyser.ToDateTime.ToString("dd-MMM HH:mm"));
                    var sizeF = g.MeasureString(title, m_TitleFont);
                    g.DrawString(title, m_TitleFont, Brushes.Black, (width - sizeF.Width) / 2 + paddingX, (paddingY - sizeF.Height) / 2);

                    var thirdW = width / 3;
                    int legPos = -1;
                    for (int i = 0; i < 2; i++)
                    {
                        string legend = "";
                        Pen legendPen = Pens.Black;
                        if (i == 0)
                        {
                            legend = "CPU Utilisation";
                            legendPen = CpuPen;
                        }
                        else if (i == 1)
                        {
                            legend = "All Disks Utilisation";
                            legendPen = DisksPen;
                        }

                        legPos++;

                        sizeF = g.MeasureString(legend, m_TitleFont);
                        var y = paddingY + height + sizeF.Height / 2;
                        var yl = paddingY + height + sizeF.Height;
                        g.DrawString(legend, m_TitleFont, Brushes.Black, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW + 15, y);
                        g.DrawLine(legendPen, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl - 1, 6 + (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl - 1);
                        g.DrawLine(legendPen, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl, 6 + (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl);
                        g.DrawLine(legendPen, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl + 1, 6 + (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl + 1);
                    }

                    g.Save();
                }

            }).ContinueWith((r) =>
            {
                if (r.IsCompleted)
                {
                    this.Invoke(new Action(() =>
                    {
                        if (pbGraph.Image != null)
                        {
                            pbGraph.Image.Dispose();
                        }

                        pbGraph.Image = image;
                        pbGraph.Update();

                        m_LastGraphWidth = pbGraph.Width;
                        m_LastGraphHeight = pbGraph.Height;

                        Cursor = Cursors.Default;
                    }));
                }
                else
                {
                    Cursor = Cursors.Default;
                }
            });
        }

        private void DrawNtpUpdatesGraph()
        {
            Cursor = Cursors.WaitCursor;

            int graphWidth = pbGraph.Width;
            int graphHeight = pbGraph.Height;

            bool tickGridlines = m_GraphConfig.GridlineStyle == GridlineStyle.Tick;
            bool includeUnapplied = m_GrapthType == GraphType.gtNtpUpdatesInclUnapplied;

            var image = new Bitmap(graphWidth, graphHeight, PixelFormat.Format24bppRgb);

            Task.Run(() =>
            {
                Pen DeltaPen = Pens.DarkOrchid;
                Pen LatencyPen = Pens.Red;

                var minDelta = m_TimeAnalyser.NtpUpdates.Where(x => x.Updated || includeUnapplied).Min(x => x.Delta);
                var maxDelta = m_TimeAnalyser.NtpUpdates.Where(x => x.Updated || includeUnapplied).Max(x => x.Delta);
                var minLatency = m_TimeAnalyser.NtpUpdates.Min(x => x.Latency);
                var maxLatency = m_TimeAnalyser.NtpUpdates.Max(x => x.Latency);

                var minY = Math.Min(minLatency, minDelta);
                var maxY = Math.Max(maxLatency, maxDelta);

                using (var g = Graphics.FromImage(image))
                {
                    float padding = 10;
                    float paddingX = 45;
                    float paddingY = 25;
                    float width = graphWidth - padding - paddingX;
                    float height = graphHeight - 2 * paddingY;
                    float yFactor = height / (1.2f * (maxY - minY));
                    float minX = 0;
                    float maxX = m_TimeAnalyser.NtpUpdates.Count - 1;
                    float xFactor = width / (maxX - minX + 1);

                    g.FillRectangle(Brushes.WhiteSmoke, 0, 0, graphWidth, graphHeight);

                    for (int ya = -2000; ya < 2000; ya += 100)
                    {
                        float y = graphHeight - paddingY - yFactor * (ya - minY);
                        if (y < paddingY || y > graphHeight - paddingY) continue;

                        if (tickGridlines && ya != 0 /* The zero grid line is fully drawn even in 'tick' mode */)
                        {
                            g.DrawLine(Pens.Gray, paddingX, y, paddingX + 5, y);
                            g.DrawLine(Pens.Gray, graphWidth - padding - 5, y, graphWidth - padding, y);
                        }
                        else
                        {
                            g.DrawLine(Pens.Gray, paddingX, y, graphWidth - padding, y);
                        }

                        if (ya == 0)
                        {
                            if (tickGridlines)
                            {
                                g.DrawLine(Pens.Gray, paddingX, y - 1, paddingX + 5, y - 1);
                                g.DrawLine(Pens.Gray, graphWidth - padding - 5, y - 1, graphWidth - padding, y - 1);
                                g.DrawLine(Pens.Gray, paddingX, y + 1, paddingX + 5, y + 1);
                                g.DrawLine(Pens.Gray, graphWidth - padding - 5, y + 1, graphWidth - padding, y + 1);
                            }
                            else
                            {
                                g.DrawLine(Pens.Gray, paddingX, y - 1, graphWidth - padding, y - 1);
                                g.DrawLine(Pens.Gray, paddingX, y + 1, graphWidth - padding, y + 1);
                            }
                            var sizF = g.MeasureString("0", DefaultFont);
                            g.DrawString("0", DefaultFont, Brushes.Black, paddingX - sizF.Width - 5, y - sizF.Height / 2);
                        }
                        else if (ya % 100 == 0)
                        {
                            var label = string.Format("{0:0.0} s", ya / 1000.0);
                            var sizF = g.MeasureString(label, DefaultFont);
                            g.DrawString(label, DefaultFont, Brushes.Black, paddingX - sizF.Width - 5, y - sizF.Height / 2);
                        }
                    }

                    int idx = 0;
                    float x1p = 0;
                    float x2p = 0;
                    float y1p = 0;
                    float y2p = 0;

                    foreach (var utilEntry in m_TimeAnalyser.NtpUpdates)
                    {
                        float x = paddingX + idx * xFactor;
                        float y1 = graphHeight - paddingY - yFactor * (utilEntry.Delta - minY);
                        float y2 = graphHeight - paddingY - yFactor * (utilEntry.Latency - minY);

                        if (idx > 0)
                        {
                            if (utilEntry.Updated || includeUnapplied)
                            {
                                g.DrawLine(DeltaPen, x1p, y1p, x, y1);
                            }

                            g.DrawLine(LatencyPen, x2p, y2p, x, y2);
                        }

                        if (utilEntry.Updated || includeUnapplied)
                        {
                            y1p = y1;
                            x1p = x;
                        }

                        x2p = x;
                        y2p = y2;

                        idx++;
                    }

                    g.DrawRectangle(Pens.Black, paddingX, paddingY, graphWidth - padding - paddingX, graphHeight - 2 * paddingY);

                    var title = string.Format("OccuRec NTP updates between {0} UT and {1} UT", m_TimeAnalyser.FromDateTime.ToString("dd-MMM HH:mm"), m_TimeAnalyser.ToDateTime.ToString("dd-MMM HH:mm"));
                    var sizeF = g.MeasureString(title, m_TitleFont);
                    g.DrawString(title, m_TitleFont, Brushes.Black, (width - sizeF.Width) / 2 + paddingX, (paddingY - sizeF.Height) / 2);

                    var thirdW = width / 3;
                    int legPos = -1;
                    for (int i = 0; i < 2; i++)
                    {
                        string legend = "";
                        Pen legendPen = Pens.Black;
                        if (i == 0)
                        {
                            legend = includeUnapplied ? "NTP Deltas, Including Unapplied (ms)" : "NTP Deltas (ms)";
                            legendPen = DeltaPen;
                        }
                        else if (i == 1)
                        {
                            legend = "Latency (ms)";
                            legendPen = LatencyPen;
                        }

                        legPos++;

                        sizeF = g.MeasureString(legend, m_TitleFont);
                        var y = paddingY + height + sizeF.Height / 2;
                        var yl = paddingY + height + sizeF.Height;
                        g.DrawString(legend, m_TitleFont, Brushes.Black, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW + 15, y);
                        g.DrawLine(legendPen, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl - 1, 6 + (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl - 1);
                        g.DrawLine(legendPen, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl, 6 + (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl);
                        g.DrawLine(legendPen, (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl + 1, 6 + (thirdW - sizeF.Width) / 2 + paddingX + legPos * thirdW, yl + 1);
                    }

                    g.Save();
                }

            }).ContinueWith((r) =>
            {
                if (r.IsCompleted)
                {
                    this.Invoke(new Action(() =>
                    {
                        if (pbGraph.Image != null)
                        {
                            pbGraph.Image.Dispose();
                        }

                        pbGraph.Image = image;
                        pbGraph.Update();

                        m_LastGraphWidth = pbGraph.Width;
                        m_LastGraphHeight = pbGraph.Height;

                        Cursor = Cursors.Default;
                    }));
                }
                else
                {
                    Cursor = Cursors.Default;
                }
            });
        }

        private void resizeUpdateTimer_Tick(object sender, EventArgs e)
        {
            resizeUpdateTimer.Enabled = false;

            if (m_LastGraphWidth != pbGraph.Width || m_LastGraphHeight != pbGraph.Height)
            {
                DrawGraph();
            }
        }

        private void frmAavStatusChannelOnlyView_ResizeEnd(object sender, EventArgs e)
        {
            resizeUpdateTimer.Enabled = true;
        }

        private void cbxGraphType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbxGraphType.SelectedIndex == 0)
            {
                m_GrapthType = GraphType.gtTimeDeltasLines;
                pnlTimeDeltaConfig.Visible = true;
            }
            else if (cbxGraphType.SelectedIndex == 1)
            {
                m_GrapthType = GraphType.gtTimeDeltasDots;
                pnlTimeDeltaConfig.Visible = true;
            }
            else if (cbxGraphType.SelectedIndex == 2)
            {
                m_GrapthType = GraphType.gtSystemUtilisation;
                pnlTimeDeltaConfig.Visible = false;
            }
            else if (cbxGraphType.SelectedIndex == 3)
            {
                m_GrapthType = GraphType.gtNtpUpdates;
                pnlTimeDeltaConfig.Visible = false;
            }
            else if (cbxGraphType.SelectedIndex == 4)
            {
                m_GrapthType = GraphType.gtNtpUpdatesInclUnapplied;
                pnlTimeDeltaConfig.Visible = false;
            }

            DrawGraph();
        }

        private void TimeDeltasTimeSourceChanged(object sender, EventArgs e)
        {
            if (m_GrapthType != GraphType.gtNone)
            {
                DrawGraph();
            }
        }

        private void GridlinesStyleChanged(object sender, EventArgs e)
        {
            if (sender == miCompleteGridlines)
            {
                m_GraphConfig.GridlineStyle = GridlineStyle.Line;
            }
            else if (sender == miTickGridlines)
            {
                m_GraphConfig.GridlineStyle = GridlineStyle.Tick;
            }

            foreach (ToolStripMenuItem item in ((sender as ToolStripMenuItem).OwnerItem as ToolStripMenuItem).DropDownItems)
            {
                item.Checked = (item == (sender as ToolStripMenuItem));
            }

            DrawGraph();
        }

        private void ShowOcrErrorFrame()
        {
            if (!m_IsDataReady || m_TimeAnalyser.DebugFrames.Count == 0) return;

            var frame = m_TimeAnalyser.DebugFrames[(int)nudOcrErrorFrame.Value];
            lblOcrText.Text = string.Format("[{0}]           [{1}]", frame.OrcField1, frame.OrcField2);
            pbOcrErrorFrame.Image = frame.DebugImage;
        }

        private void nudOcrErrorFrame_ValueChanged(object sender, EventArgs e)
        {
            ShowOcrErrorFrame();
        }

        private string m_ExportFileName;

        private void miExport_Click(object sender, EventArgs e)
        {
            saveFileDialog.FileName = Path.ChangeExtension(m_Aav.FileName, "csv");

            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                m_ExportFileName = saveFileDialog.FileName;
                m_TimeAnalyser.ExportData(
                    m_ExportFileName,
                    (val, max) =>
                    {
                        this.Invoke(new Action<int, int>(UpdateExportProgressBar), val, max);
                    }
                );
            }
        }

        private void OnExportFinished()
        {
            Process.Start(m_ExportFileName);
        }

        private void miSubset_Click(object sender, EventArgs e)
        {
            var frm = new frmChooseSubset(m_TimeAnalyser);
            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                if (frm.From == 0 && frm.To == m_TimeAnalyser.Entries.Count - 1)
                {
                    m_PlotFromIndex = null;
                    m_PlotToIndex = null;
                }
                else
                {
                    m_PlotFromIndex = frm.From;
                    m_PlotToIndex = frm.To;
                }
                DrawGraph();
            }
        }
    }
}
