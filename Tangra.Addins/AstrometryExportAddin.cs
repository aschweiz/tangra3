﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Tangra.SDK;

namespace Tangra.Addins
{
    [Serializable]
    public class AstrometryExportAddin : MarshalByRefObject, ITangraAddinAction
    {
        private ITangraHost m_Host;
        private ITangraHost2 m_Host2;

        public void Initialise(ITangraHost host)
        {
            m_Host = host;
            m_Host2 = host as ITangraHost2;
        }

        public void Finalise()
        { }

        public string DisplayName
        {
            get { return "Astrometry CSV Export"; }
        }

        public AddinActionType ActionType
        {
            get { return AddinActionType.Astrometry; }
        }

        public IntPtr Icon
        {
            get { return IntPtr.Zero; }
        }

        public int IconTransparentColorARGB
        {
            get { return Color.Transparent.ToArgb(); }
        }

        private ITangraAstrometricSolution2 m_LastSolution;

        public void Execute()
        {
            ITangraAstrometricSolution2 solution = m_Host.GetAstrometryProvider().GetCurrentFrameAstrometricSolution() as ITangraAstrometricSolution2;
            if (solution != null)
                m_LastSolution = solution;
        }

        internal void OnBeginMultiFrameAstrometry()
        {
            m_LastSolution = null;
        }

        internal void OnEndMultiFrameAstrometry()
        {
            if (m_LastSolution != null)
            {
                string fileName = m_Host2 != null ? m_Host2.GetFileInfoProvider().FileName : null;
                var meaList = m_LastSolution.GetAllMeasurements();

				var output = new StringBuilder();
                output.AppendLine("Tangra Astrometry Export v1.0");
                output.AppendLine("FilePath, Date, InstrumentalDelay, DelayUnits, IntegratedFrames, IntegratedExposure(sec), FrameTimeType, NativeVideoFormat");
                output.AppendLine(string.Format("\"{0}\",{1},{2},{3},{4},{5},{6},{7}", 
                    fileName, 
                    meaList.Count > 0 && meaList[0].UncorrectedTimeStamp.HasValue ? meaList[0].UncorrectedTimeStamp.Value.ToString("yyyy-MM-dd") : null,
                    m_LastSolution.InstrumentalDelay,
                    m_LastSolution.InstrumentalDelayUnits,
                    m_LastSolution.IntegratedFramesCount,
                    m_LastSolution.IntegratedExposureSeconds,
                    m_LastSolution.FrameTimeType,
                    m_LastSolution.NativeVideoFormat));

                output.Append("FrameNo, TimeUTC(Uncorrected), RADeg, DEDeg, Mag, SolutionUncertaintyRA*Cos(DE)[arcsec], SolutionUncertaintyDE[arcsec], FWHM[arcsec], DetectionCertainty, SNR\r\n");
                
                foreach (var mea in meaList)
                {
                    output.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}\r\n", 
                        mea.FrameNo, mea.UncorrectedTimeStamp.HasValue ? (double?)mea.UncorrectedTimeStamp.Value.TimeOfDay.TotalDays : null,
                        mea.RADeg, mea.DEDeg, mea.Mag, mea.SolutionUncertaintyRACosDEArcSec, mea.SolutionUncertaintyDEArcSec, mea.FWHMArcSec, mea.Detection, mea.SNR);
				}

				var dialog = new SaveFileDialog();
				dialog.Filter = "Comma Separated Values (*.csv)|*.csv|All Files (*.*)|*.*";
				dialog.DefaultExt = "csv";
				dialog.Title = "Export Tangra Astrometry";

                if (fileName != null)
                    dialog.FileName = Path.ChangeExtension(fileName, ".csv");

				if (dialog.ShowDialog(m_Host.ParentWindow) == DialogResult.OK)
				{
					File.WriteAllText(dialog.FileName, output.ToString());
					Process.Start(dialog.FileName);
				}
            }
        }
    }
}
