﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Tangra.Controller;
using Tangra.Helpers;
using Tangra.Model.Astro;
using Tangra.Model.Config;
using Tangra.Model.Context;
using Tangra.Model.Helpers;
using Tangra.Model.Image;
using Tangra.Model.ImageTools;
using Tangra.Model.Video;
using Tangra.Model.VideoOperations;
using Tangra.OCR;
using Tangra.Video;
using Tangra.VideoOperations.LightCurves;
using Tangra.Config;
using Tangra.ImageTools;
using Tangra.VideoOperations.LightCurves.Measurements;
using Tangra.VideoOperations.LightCurves.Tracking;
using Tangra.Resources;


namespace Tangra.VideoOperations.LightCurves
{
    public class ReduceLightCurveOperation : VideoOperationBase, IVideoOperation
    {
        private VideoController m_VideoController;
        internal AstroImage m_AstroImage;

        private ucLightCurves m_ControlPanel = null;
        internal AstroImage m_StackedAstroImage;

        private object m_SyncRoot = new object();

        private LCStateMachine m_StateMachine;
        private Tracker m_Tracker;
        private AveragedFrame m_AveragedFrame;

        private MeasurementsHelper m_Measurer;
        private GroupMeasurer m_GroupMeasurer;

        private int m_ProcessedFrames = 0;
        private int m_UnsuccessfulFrames = 0;
        private Stopwatch m_StopWatch = new Stopwatch();

        private readonly Pen[] m_AllPens = new Pen[4];
        private readonly Brush[] m_AllBrushes = new Brush[4];

        internal int m_CurrFrameNo = -1;
        private int m_FirstMeasuredFrame;
        internal uint m_MinFrame;
        internal uint m_MaxFrame;
        internal uint m_TotalFrames;
        internal int m_MeasurementInterval;
        internal float m_AverageFWHM;

        private ITimestampOcr m_TimestampOCR;
        private DateTime m_OCRedTimeStamp;

        private LCState m_BackedUpSelectMeasuringStarsState = null;

        private MeasuringZoomImageType MeasuringZoomImageType = MeasuringZoomImageType.Stripe;
	    private LightCurveController m_LightCurveController;

		public ReduceLightCurveOperation()
		{
			Debug.Assert(false, "This constructor should not be called.");
		}

		public ReduceLightCurveOperation(LightCurveController lightCurveController)
		{
			m_LightCurveController = lightCurveController;

            m_AllPens[0] = new Pen(TangraConfig.Settings.Color.Target1);
            m_AllPens[1] = new Pen(TangraConfig.Settings.Color.Target2);
            m_AllPens[2] = new Pen(TangraConfig.Settings.Color.Target3);
            m_AllPens[3] = new Pen(TangraConfig.Settings.Color.Target4);

            m_AllBrushes[0] = new SolidBrush(TangraConfig.Settings.Color.Target1);
            m_AllBrushes[1] = new SolidBrush(TangraConfig.Settings.Color.Target2);
            m_AllBrushes[2] = new SolidBrush(TangraConfig.Settings.Color.Target3);
            m_AllBrushes[3] = new SolidBrush(TangraConfig.Settings.Color.Target4); 
        }

        #region IVideoOperation Members

        private void EnsureControlPanel(Panel controlPanel)
        {
            if (m_ControlPanel == null)
            {
                lock(m_SyncRoot)
                {
                    if (m_ControlPanel == null)
                    {
                        m_ControlPanel = new ucLightCurves();

                        controlPanel.Controls.Clear();
                        controlPanel.Controls.Add(m_ControlPanel);
                        m_ControlPanel.Dock = DockStyle.Fill;
                    }
                }
            }            
        }

        public bool InitializeOperation(IVideoController videoContoller, Panel controlPanel, IFramePlayer framePlayer, Form topForm)
        {
            m_VideoController = (VideoController)videoContoller;

			var configForm = new frmSelectReductionType(m_VideoController, framePlayer);
            if (configForm.ShowDialog(topForm) == DialogResult.OK)
            {
	            EnsureControlPanel(controlPanel);

	            BeginConfiguration();

	            TangraContext.Current.CanChangeTool = true;

	            return true;
            }

			TangraContext.Current.CanLoadDarkFrame = true;
			TangraContext.Current.CanLoadFlatFrame = true;

            return false;
        }

        public void PlayerStarted()
        { }

        public void MeasuringStarted()
        {
			if (m_StateMachine.m_CurrentState == LightCurvesState.Running &&
				m_StateMachine.MeasuringStars.Count > 0 &&
				!m_StateMachine.m_HasBeenPaused)
			{
				m_Measurer = new MeasurementsHelper(
					m_VideoController.VideoBitPix,
					LightCurveReductionContext.Instance.NoiseMethod,
					true,
					TangraConfig.Settings.Photometry.Saturation.GetSaturationForBpp(m_VideoController.VideoBitPix));

				m_Measurer.SetCoreProperties(
					TangraConfig.Settings.Photometry.AnulusInnerRadius,
					TangraConfig.Settings.Photometry.AnulusMinPixels,
					TangraConfig.PhotometrySettings.REJECTION_BACKGROUND_PIXELS_STD_DEV,
					(float)m_Tracker.PositionTolerance);

				m_Measurer.GetImagePixelsCallback +=
					new MeasurementsHelper.GetImagePixelsDelegate(m_Measurer_GetImagePixelsCallback);

				m_CustomZoomBitmap = null;

				if (TangraConfig.Settings.Photometry.PsfFittingMethod == TangraConfig.PsfFittingMethod.LinearFitOfAveragedModel)
				{
					float averageFWHM =
						(float)
						m_StateMachine.MeasuringStars.Average(ms => ms.Gaussian != null ? ms.Gaussian.FWHM : 3.5);
					m_GroupMeasurer = new GroupMeasurer(averageFWHM, m_StateMachine.MeasuringStars, m_Measurer);
				}
				else
					m_GroupMeasurer = null;
			}
        }

		uint[,] m_Measurer_GetImagePixelsCallback(int x, int y, int matrixSize)
		{
			AstroImage currImage = m_VideoController.GetCurrentAstroImage(false);
			
			return currImage.GetMeasurableAreaPixels(x, y, matrixSize);
		}

        public void FinalizeOperation()
        {
            SaveSessionFile();
        }

		public void SaveSessionFile()
		{
			// NOTE: Tracking session are not supported yet
		}

        internal byte FrameBackgroundMode = 0;
        private int m_PrevMeasuredFrame = -1;

        public void NextFrame(int frameNo, MovementType movementType, bool isLastFrame, AstroImage astroImage, int firstFrameInIntegrationPeriod)
        {
            m_AstroImage = astroImage;

            if (m_Correcting)
                // Do not track or process the frame while correcting the tracking
                return;

            if (m_Configuring)
            {
                if (m_CurrFrameNo != frameNo) m_StackedAstroImage = null;
                if (frameNo != m_StateMachine.SelectedObjectFrameNo) m_StateMachine.SelectedObject = null;
            }

            m_CurrFrameNo = frameNo;
            uint[] osdPixels = null;
            if (m_Refining || m_Measuring)
            {
                //if (m_TimestampOCR != null)
                //{
                //    osdPixels = VideoContext.Current.AstroImage.GetOSDBytes();

                //    if (m_TimestampOCR.RequiresConfiguring)
                //        m_TimestampOCR.TryToAutoConfigure(osdPixels);

                //    if (m_TimestampOCR.RequiresCalibration)
                //    {
                //        frmOCRCalibrating frmCalibrating = new frmOCRCalibrating();
                //        frmCalibrating.ConfigureCalibration(m_Host.FramePlayer, m_TimestampOCR, frameNo);
                //        if (frmCalibrating.ShowDialog(m_Host.MainFormWindow) == DialogResult.Abort)
                //        {
                //            // Couldn't do it. Cancel the OCR-ing
                //            m_TimestampOCR = null;
                //        }
                //        OCRConfigEntry calibratedConfig = frmCalibrating.GetCalibConfig();
                //        m_TimestampOCR.AddConfiguration(osdPixels, calibratedConfig);
                //    }

                //    if (!m_TimestampOCR.RequiresConfiguring)
                //    {
                //        if (m_Refining)
                //            m_TimestampOCR.RefiningFrame(osdPixels, m_Tracker.RefiningFramesRemaining);
                //        else
                //            if (!m_TimestampOCR.ExtractTime(osdPixels, out m_OCRedTimeStamp, out m_OCRedTimestampPixels))
                //                m_OCRedTimeStamp = DateTime.MinValue;
                //    }
                //}

                m_Tracker.NextFrame(frameNo, astroImage);

                if (m_Measuring)
                {
                    m_ProcessedFrames++;
                    if (!m_Tracker.IsTrackedSuccessfully) m_UnsuccessfulFrames++;

                    SaveEmbeddedOrORCedTimeStamp();

                    MeasureObjects();

                    m_PrevMeasuredFrame = m_CurrFrameNo;

                    m_StopWatch.Stop();
                    m_ControlPanel.UpdateProcessedFrames(m_ProcessedFrames, m_UnsuccessfulFrames, (int)(m_StopWatch.ElapsedMilliseconds / 1000));
                    m_StopWatch.Start();

                    if (!m_Tracker.IsTrackedSuccessfully &&
                         LightCurveReductionContext.Instance.StopOnLostTracking)
                    {
                        m_ControlPanel.StopMeasurements();

                        if (TangraConfig.Settings.Tracking.PlaySound)
                            Console.Beep(800, 750);

                        m_VideoController.ShowMessageBox(
                            "Use the mouse to pan the object apertures back to the object location and press 'Continue' to continue with the measurements.",
                            "Tracking has been lost",
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Exclamation);

                        return;
                    }
                }
                else if (m_Refining)
                {
                    if (m_Tracker.RefiningPercentageWorkLeft <= 0)
                    {
                        bool canSwitchFromRefiningToMeasuringNow = true;

                        if (m_VideoController.IsUsingSteppedAveraging)
                        {
                            // When using stepped averaging the measurements should start at the first frame of an integration 'step'
                            // which means we need to flip the switch from 'Refining' to 'Measuring' one frame before that
                            if (firstFrameInIntegrationPeriod + m_VideoController.FramesToIntegrate - 1 > frameNo)
                            {
                                Trace.WriteLine(string.Format("Skipping frame {0}, waiting for the first frame in the next integration period to start measurments.", frameNo));
                                canSwitchFromRefiningToMeasuringNow = false;
                            }
                        }

                        if (canSwitchFromRefiningToMeasuringNow)
                        {
                            // Begin measurements
                            m_Measuring = true;
                            m_Refining = false;

                            m_ProcessedFrames = 0;
                            m_UnsuccessfulFrames = 0;
                            m_StopWatch.Reset();
                            m_StopWatch.Start();

                            m_Tracker.BeginMeasurements(astroImage);

                            // IMPORTANT: The finalHeader must be changed as well if changing this
                            LCFile.NewOnTheFlyOutputFile(
                                m_VideoController.CurrentVideoFileName,
                                string.Format("Video ({0})", m_VideoController.CurrentVideoFileType),
                                (byte)m_Tracker.TrackedObjects.Count, (float)m_Tracker.PositionTolerance);

                            m_MinFrame = uint.MaxValue;
                            m_MaxFrame = uint.MinValue;
                            m_TotalFrames = 0;
                            m_FirstMeasuredFrame = frameNo;

                            m_AverageFWHM = astroImage.GetAverageFWHM();

                            if (m_TimestampOCR != null && osdPixels != null)
                                m_TimestampOCR.PrepareForMeasurements(osdPixels);

                            m_VideoController.StatusChanged("Measuring");                            
                        }
                    }
                }				
            }
			else if (m_ViewingLightCurve && m_lcFile != null)
			{
				var currentSelection = new LCMeasurement[m_lcFile.Header.ObjectCount];

				if (m_lcFile.Header.MinFrame <= m_CurrFrameNo &&
					m_lcFile.Header.MaxFrame >= m_CurrFrameNo)
				{
					for (int i = 0; i < m_lcFile.Header.ObjectCount; i++)
					{
						List<LCMeasurement> measurements = m_lcFile.Data[i];
						currentSelection[i] = measurements[(int)(m_CurrFrameNo - m_lcFile.Header.MinFrame)];
					}

					m_LightCurveController.OnNewSelectedMeasurements(currentSelection.ToArray());
				}
			}

            if (m_ControlPanel != null)
                m_ControlPanel.UpdateState();
            if (isLastFrame)
            {
                if (m_Refining)
                {
                    m_ControlPanel.StopRefining();
                    if (TangraConfig.Settings.Tracking.PlaySound)
                        Console.Beep();
                }
                else if (m_Measuring)
                {
                    m_ControlPanel.StopMeasurements();

                    if (TangraConfig.Settings.Tracking.PlaySound)
                        Console.Beep();

                    m_ControlPanel.StoppedAtLastFrame();
                }
            }
        }

        public void ImageToolChanged(ImageTool newTool, ImageTool oldTool)
        { }

        public void PreDraw(System.Drawing.Graphics g)
        { }

        private int m_ManualTrackingDeltaX = 0;
        private int m_ManualTrackingDeltaY = 0;

        internal void SetManualTrackingCorrection(int deltaX, int deltaY)
        {
            m_ManualTrackingDeltaX = deltaX;
            m_ManualTrackingDeltaY = deltaY;
        }

        private bool m_Measuring = false;
        private bool m_Configuring = false;
        private bool m_Refining = false;
        private bool m_Correcting = false;
        private bool m_ViewingLightCurve = false;

        private static Font s_FONT = new Font(FontFamily.GenericMonospace, 8);

        public void PostDraw(Graphics g)
        {
            try
            {
				if (m_ViewingLightCurve && m_lcFile != null)
				{
					// Display the objects for the current frame
					if (m_lcFile.Header.MinFrame <= m_CurrFrameNo &&
						m_lcFile.Header.MaxFrame >= m_CurrFrameNo)
					{
						int frameToDisplay;
						if (m_VideoController.CurrentVideoFileEngine == SingleBitmapFileFrameStream.SINGLE_BMP_FILE_ENGINE)
						{
							// The original file cannot be found and we are displaying the embedded image in the .lc file.
							frameToDisplay = (int)m_lcFile.Header.MinFrame;
						}
						else
						{
							frameToDisplay = m_CurrFrameNo;
						}

						for (int i = 0; i < m_lcFile.Header.ObjectCount; i++)
						{
							List<LCMeasurement> measurements = m_lcFile.Data[i];
							LCMeasurement data = measurements[(int)(frameToDisplay - m_lcFile.Header.MinFrame)];

							float delta = m_lcFile.Header.MeasurementApertures[i];

							if (!float.IsNaN(delta))
							{
								g.DrawEllipse(m_AllPens[i], data.X0 - delta, data.Y0 - delta, 2 * delta, 2 * delta);
								g.DrawString(string.Format("x={0}; y={1}", data.X0.ToString("0.0"), data.Y0.ToString("0.0")),
											 s_FONT, m_AllBrushes[i], data.X0, data.Y0 + 2 * delta);

							}
						}

					}
				}

                if (m_Measuring)
                {
                    int iTo = Math.Min(4, m_Tracker.TrackedObjects.Count);

                    if (!m_Correcting)
                    {
                        // This is where we draw the stars while measuring
                        if (m_Tracker.IsTrackedSuccessfully ||
                            LightCurveReductionContext.Instance.IsDriftThrough)
                        {
                            for (int i = 0; i < iTo; i++)
                            {
                                ImagePixel center = m_Tracker.TrackedObjects[i].Center;
                                float app = m_Tracker.TrackedObjects[i].Aperture;

                                if (!float.IsNaN(app))
                                {
                                    g.DrawEllipse(m_AllPens[i], (float)center.XDouble + m_ManualTrackingDeltaX - app, (float)center.YDouble + m_ManualTrackingDeltaY - app, 2 * app, 2 * app);
                                }
                            }
                        }
                    }
                    else
                    {
                        // This is where we draw the stars while we are correcting lost tracking. In this case we use the last good known
                        // position as a reference rather than the latest position
                        for (int i = 0; i < iTo; i++)
                        {
                            ImagePixel center = m_Tracker.TrackedObjects[i].LastKnownGoodPosition;
                            float app = m_Tracker.TrackedObjects[i].Aperture;
                            if (float.IsNaN(app)) app = 4.0f;

                            if (center != null)
                            {
                                g.DrawEllipse(m_AllPens[i], (float)center.XDouble + m_ManualTrackingDeltaX - app, (float)center.YDouble + m_ManualTrackingDeltaY - app, 2 * app, 2 * app);
                            }
                        }

                        if (m_Tracker.AutoDiscoveredStars != null &&
                            m_Tracker.AutoDiscoveredStars.Count > 0)
                        {
                            float app = 4;
                            foreach (PSFFit fit in m_Tracker.AutoDiscoveredStars)
                            {
                                g.DrawEllipse(Pens.Yellow, (float)fit.XCenter - app, (float)fit.YCenter + m_ManualTrackingDeltaY - app, 2 * app, 2 * app);
                            }
                        }
                    }
                }
                else if (m_Refining)
                {
                    int iTo = Math.Min(4, m_Tracker.TrackedObjects.Count);
                    for (int i = 0; i < iTo; i++)
                    {
                        ImagePixel center = m_Tracker.TrackedObjects[i].Center;
                        float delta = ((float)Math.Min(1, m_Tracker.RefiningPercentageWorkLeft) * 3 + 1) * m_StateMachine.MeasuringApertures[i];

                        if (m_Tracker.TrackedObjects[i].IsLocated)
                        {
                            if (!float.IsNaN(delta))
                                g.DrawArc(
                                    m_AllPens[i], (float)center.XDouble - delta,
                                    (float)center.YDouble - delta, 2 * delta, 2 * delta,
                                    0, (float)Math.Min(360 * (1 - m_Tracker.RefiningPercentageWorkLeft), 360));
                        }
                    }
                }
                else if (m_Configuring)
                {
                    for (int i = 0; i < m_StateMachine.MeasuringStars.Count; i++)
                    {
                        TrackedObjectConfig star = m_StateMachine.MeasuringStars[i];
                        float aperture = m_StateMachine.MeasuringApertures[i] + 1;
                        Pen pen = m_AllPens[i];

                        float centerX = star.ApertureStartingX + m_ManualTrackingDeltaX;
                        float centerY = star.ApertureStartingY + m_ManualTrackingDeltaY;

                        g.DrawEllipse(pen, centerX - aperture, centerY - aperture, 2 * aperture, 2 * aperture);

                        if (m_StateMachine.SelectedMeasuringStar == i)
                        {
                            g.DrawLine(pen, centerX - 12, centerY, centerX - 6, centerY);
                            g.DrawLine(pen, centerX + 12, centerY, centerX + 6, centerY);
                            g.DrawLine(pen, centerX, centerY - 12, centerX, centerY - 6);
                            g.DrawLine(pen, centerX, centerY + 12, centerX, centerY + 6);
                        }

                        if (star.TrackingType == TrackingType.GuidingStar)
                        {
                            g.DrawEllipse(pen, centerX - aperture - 1, centerY - aperture - 1, 2 * aperture + 2, 2 * aperture + 2);
                            g.DrawEllipse(pen, centerX - aperture - 2, centerY - aperture - 2, 2 * aperture + 4, 2 * aperture + 4);
                        }
                    }

                    if (m_StateMachine.SelectedMeasuringStar == -1 &&
                        m_StateMachine.SelectedObject != null)
                    {
                        ImagePixel selection = m_StateMachine.SelectedObject;
                        g.DrawLine(Pens.WhiteSmoke, (float)selection.XDouble - 12, (float)selection.YDouble, (float)selection.XDouble - 6, (float)selection.YDouble);
                        g.DrawLine(Pens.WhiteSmoke, (float)selection.XDouble + 12, (float)selection.YDouble, (float)selection.XDouble + 6, (float)selection.YDouble);
                        g.DrawLine(Pens.WhiteSmoke, (float)selection.XDouble, (float)selection.YDouble - 12, (float)selection.XDouble, (float)selection.YDouble - 6);
                        g.DrawLine(Pens.WhiteSmoke, (float)selection.XDouble, (float)selection.YDouble + 12, (float)selection.XDouble, (float)selection.YDouble + 6);
                    }

                    #region Draw the zoomed image of the selected star
                    bool isMeasuringStar = m_StateMachine.SelectedMeasuringStar != -1;
                    if (isMeasuringStar || m_StateMachine.SelectedObject != null)
                    {

                        ImagePixel star =
                            isMeasuringStar
                                ? new ImagePixel(m_StateMachine.MeasuringStars[m_StateMachine.SelectedMeasuringStar].ApertureStartingX, m_StateMachine.MeasuringStars[m_StateMachine.SelectedMeasuringStar].ApertureStartingY)
                                : m_StateMachine.SelectedObject;

                        float dx = 0;
                        float dy = 0;

                        if (isMeasuringStar)
                        {
                            dx = m_StateMachine.MeasuringStars[m_StateMachine.SelectedMeasuringStar].ApertureDX;
                            dy = m_StateMachine.MeasuringStars[m_StateMachine.SelectedMeasuringStar].ApertureDX;
                        }


                        float aperture =
                            isMeasuringStar
                                ? m_StateMachine.MeasuringApertures[m_StateMachine.SelectedMeasuringStar]
                                : 5;

                        Pen pen =
                            isMeasuringStar
                            ? m_AllPens[m_StateMachine.SelectedMeasuringStar]
                            : Pens.WhiteSmoke;

                        Bitmap zoomedBmp = m_AstroImage.GetZoomImagePixels(star.X, star.Y, TangraConfig.Settings.Color.Saturation, TangraConfig.Settings.Photometry.Saturation);
                        using (Graphics gz = Graphics.FromImage(zoomedBmp))
                        {
                            float x = 8.0f * (float)(star.XDouble - star.X - 0.5f) + 16 * 8;
                            float y = 8.0f * (float)(star.YDouble - star.Y - 0.5f) + 16 * 8;
                            float ap = aperture * 8;
                            gz.DrawEllipse(pen, x - ap, y - ap, 2 * ap, 2 * ap);

                            if (isMeasuringStar)
                            {
                                // Draw background annulus
                                float innerRadius = aperture * TangraConfig.Settings.Photometry.AnulusInnerRadius;
                                float outerRadius = (float)Math.Sqrt(TangraConfig.Settings.Photometry.AnulusMinPixels / Math.PI + innerRadius * innerRadius);

                                innerRadius *= 8.0f;
                                outerRadius *= 8.0f;

                                gz.DrawEllipse(pen, x - innerRadius, y - innerRadius, 2 * innerRadius, 2 * innerRadius);
                                gz.DrawEllipse(pen, x - outerRadius, y - outerRadius, 2 * outerRadius, 2 * outerRadius);
                            }

                            gz.Save();
                        }

                        m_VideoController.UpdateZoomedImage(zoomedBmp);
                    }
                    else
                    {
                        m_VideoController.ClearZoomedImage();
                    }
                    #endregion
                }

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        public override void MouseClick(ObjectClickEventArgs e)
        {
            if (e.Pixel != null &&
                m_StateMachine.CurrentState < LightCurvesState.ReadyToRun)
            {
                // Keep track on the currently selected object in the state
                if (e.Gausian != null)
                    m_StateMachine.SelectedObject = new ImagePixel(e.Gausian.Brightness, e.Gausian.XCenter, e.Gausian.YCenter);
                else
                {
                    m_StateMachine.SelectedObject = e.Pixel;
                }

                m_StateMachine.SelectedObjectGaussian = e.Gausian;
                m_StateMachine.SelectedObjectFrameNo = m_CurrFrameNo;

                int existingIndexId = -1;
                if (!m_StateMachine.IsNewObject(e.Pixel, e.Shift, e.Control, ref existingIndexId))
                {
                    m_StateMachine.SelectedMeasuringStar = existingIndexId;
                }
                else
                    m_StateMachine.SelectedMeasuringStar = -1;

                SelectedTargetChanged(m_StateMachine.SelectedMeasuringStar);

                // This is how we tell the VideoOperation that something changed.
                m_VideoController.RefreshCurrentFrame();
            }
        }

        public bool HasCustomZoomImage
        {
            get
            {
				return m_Measuring || m_Refining || m_ViewingLightCurve || m_Configuring;
            }
        }

        public bool AvoidImageOverlays
        {
            get
            {
                // When configuring - dont show overlays
                return m_Configuring;
            }
        }

        private Bitmap m_CustomZoomBitmap = null;
        private Color m_MedianBackgroundColor = Color.Black;
        private Pen m_MedianBackgroundPen = null;
        private Brush m_MedianBackgroundBrush = null;


		private byte PixelValueToDisplayBitmapByte(uint pixel, int bpp)
		{
			if (bpp == 8)
				return (byte)(pixel & 0xFF);
			else if (bpp == 12)
				return (byte)((pixel >> 4) & 0xFF);
			else if (bpp == 16)
				return (byte)((pixel >> 8) & 0xFF);

			return (byte)(pixel & 0xFF);
		}

        private void InitCustomZoomBitmap(int width, int height)
        {
			m_CustomZoomBitmap = new Bitmap(width, height);
			byte color = PixelValueToDisplayBitmapByte(m_Tracker.MedianValue, m_VideoController.VideoBitPix);
			m_MedianBackgroundColor = Color.FromArgb(color, color, color);

			if (m_MedianBackgroundPen != null) m_MedianBackgroundPen.Dispose();
			m_MedianBackgroundPen = new Pen(m_MedianBackgroundColor);

			if (m_MedianBackgroundBrush != null) m_MedianBackgroundBrush.Dispose();
			m_MedianBackgroundBrush = new SolidBrush(m_MedianBackgroundColor);

			using (Graphics g = Graphics.FromImage(m_CustomZoomBitmap))
			{
				g.Clear(m_MedianBackgroundColor);

				int objHeight = (height / 4) - 20;

				foreach (TrackedObject obj in m_Tracker.TrackedObjects)
				{
					int beg = obj.TargetNo * (objHeight) + obj.TargetNo * 20 + 10;
					g.FillRectangle(m_AllBrushes[obj.TargetNo], 1, beg, 4, objHeight);
					g.FillRectangle(m_MedianBackgroundBrush, 5, beg, width - 9, objHeight);
				}

				g.Save();
			}
            
        }


        public bool DrawCustomZoomImage(Graphics gMain, int width, int height)
        {
			if (m_Correcting)
				// The frame was redrawn because of manual corrections
				return false;

			if (m_Tracker != null)
			{
				lock (m_SyncRoot)
				{
					if (m_Measuring)
					{
						if (this.MeasuringZoomImageType == MeasuringZoomImageType.Stripe)
						{
							DrawZoomStripes(gMain, width, height);
							return true;
						}
						else if (this.MeasuringZoomImageType == MeasuringZoomImageType.Pixel)
						{
							DrawZoomPixels(gMain, width, height);
							return true;
						}
						else
						{
							gMain.Clear(Color.Gray);
							return true;
						}
					}
					else if (m_Refining &&
						LightCurveReductionContext.Instance.DebugTracking)
					{
						DrawZoomArea(gMain);
						return true;
					}
					else if (m_ViewingLightCurve)
					{
						DrawZoomArea(gMain);
						return true;
					}				
				}
			}

	        if (m_Configuring)
				// Don't overwrite the configuration image
		        return true;

	        return false;
        }

        private void DrawZoomArea(Graphics gMain)
        {
	        AstroImage currentImage = m_VideoController.GetCurrentAstroImage(false);
			 
			if (currentImage != null)
			{
				Rectangle zommedArea = new Rectangle(m_VideoController.ZoomedCenter.X - 15,
													 m_VideoController.ZoomedCenter.Y - 16, 32, 32);

				using (Bitmap bmpZoom = currentImage.GetZoomImagePixels(m_VideoController.ZoomedCenter.X, m_VideoController.ZoomedCenter.Y, TangraConfig.Settings.Color.Saturation, TangraConfig.Settings.Photometry.Saturation))
				{
                    m_VideoController.ApplyDisplayModeAdjustments(bmpZoom);

					gMain.DrawImage(bmpZoom, new PointF(0, 0));

					if (m_Refining)
					{
						foreach (TrackedObject obj in m_Tracker.TrackedObjects)
						{
							if (zommedArea.Contains((int)obj.ThisFrameX, (int)obj.ThisFrameY))
							{
								float xx = (int)obj.ThisFrameX - (m_VideoController.ZoomedCenter.X - 15);
								float yy = (int)obj.ThisFrameY - (m_VideoController.ZoomedCenter.Y - 16);

								xx = xx * 8 + 0.5f;
								yy = yy * 8 + 0.5f;

								gMain.DrawLine(m_AllPens[obj.TargetNo], xx - 16, yy, xx, yy);
								gMain.DrawLine(m_AllPens[obj.TargetNo], xx - 16, yy + 1, xx, yy + 1);
								gMain.DrawLine(m_AllPens[obj.TargetNo], xx, yy - 16, xx, yy);
								gMain.DrawLine(m_AllPens[obj.TargetNo], xx + 1, yy - 16, xx + 1, yy);
								gMain.DrawLine(m_AllPens[obj.TargetNo], xx + 16, yy, xx, yy);
								gMain.DrawLine(m_AllPens[obj.TargetNo], xx + 16, yy + 1, xx, yy + 1);
								gMain.DrawLine(m_AllPens[obj.TargetNo], xx, yy + 16, xx, yy);
								gMain.DrawLine(m_AllPens[obj.TargetNo], xx + 1, yy + 16, xx + 1, yy);
							}
						}

						gMain.Save();
					}
				}				
			}
        }

        private Rectangle[] m_ZoomPixelRects = new Rectangle[]
                                                   {
                                                       new Rectangle(1, 1, 122, 122), 
                                                       new Rectangle(125, 1, 122, 122), 
                                                       new Rectangle(1, 125, 122, 122), 
                                                       new Rectangle(125, 125, 122, 122)
                                                   };

        private Rectangle[] m_BorderRects = new Rectangle[]
                                                   {
                                                       new Rectangle(0, 0, 124, 124), 
                                                       new Rectangle(124, 0, 124, 124), 
                                                       new Rectangle(0, 124, 124, 124), 
                                                       new Rectangle(124, 124, 124, 124)
                                                   };

		private void DrawZoomPixels(Graphics g, int width, int height)
		{
			g.Clear(Color.Gray);

			// IDs are 0, 1, 2 and 3
			foreach (TrackedObject obj in m_Tracker.TrackedObjects)
			{
				if (obj.ThisFrameFit != null)
				{
					obj.ThisFrameFit.DrawDataPixels(g, m_ZoomPixelRects[obj.TargetNo], obj.Aperture, m_AllPens[obj.TargetNo], m_VideoController.VideoBitPix);
				}
				else if (obj.IsLocated)
				{
					AstroImage currentImage = m_VideoController.GetCurrentAstroImage(false);
					if (currentImage != null)
					{
						int x0 = (int)Math.Round(obj.ThisFrameX);
						int y0 = (int)Math.Round(obj.ThisFrameY);
						uint[,] pix = currentImage.GetMeasurableAreaPixels(x0, y0);
						pix.DrawDataPixels(g,
							m_ZoomPixelRects[obj.TargetNo],
							new DisplayBitmapConverter.DefaultDisplayBitmapConverter(),
							obj.ThisFrameX - x0 + 8, obj.ThisFrameY - y0 + 8,
							obj.Aperture, m_AllPens[obj.TargetNo]);						
					}
				}
			}

			foreach (TrackedObject obj in m_Tracker.TrackedObjects)
				g.DrawRectangle(Pens.WhiteSmoke, m_BorderRects[obj.TargetNo]);
		}

		private void DrawZoomStripes(Graphics gMain, int width, int height)
		{
			int objHeight = (height / 4) - 20;

			if (m_CustomZoomBitmap == null) InitCustomZoomBitmap(width, height);

			using (Graphics g = Graphics.FromImage(m_CustomZoomBitmap))
			{
				bool isAperturePhotometry = LightCurveReductionContext.Instance.ReductionMethod == TangraConfig.PhotometryReductionMethod.AperturePhotometry;
				AstroImage currentImage = m_VideoController.GetCurrentAstroImage(false);

				foreach (TrackedObject obj in m_Tracker.TrackedObjects)
				{
					int beg = obj.TargetNo * (objHeight) + obj.TargetNo * 20 + 10;

					int w = m_CurrFrameNo - m_FirstMeasuredFrame + 4;
					Pen pen = Pens.Black;
					int h = objHeight - 1;

					if (!m_Tracker.IsTrackedSuccessfully)
					{
						pen = m_AllPens[obj.TargetNo];
					}
					else if (isAperturePhotometry &&
						!float.IsNaN(obj.ThisFrameX) &&
						!float.IsNaN(obj.ThisFrameY))
					{
						h = (int)Math.Min(objHeight - 1, objHeight * obj.Aperture / 7);
						if (h % 2 == 0) h++;
						byte z0 = currentImage.GetDisplayPixel((int)Math.Round(obj.ThisFrameX), (int)Math.Round(obj.ThisFrameY));
						pen = AllGrayPens.GrayPen(z0);
					}
					else if (obj.ThisFrameFit != null)
					{
						h = (int)Math.Min(objHeight - 1, objHeight * obj.ThisFrameFit.FWHM / 7);
						if (h % 2 == 0) h++;
						byte z = (byte)Math.Max(0, Math.Min(255, obj.ThisFrameFit.IMax));

						pen = AllGrayPens.GrayPen(z);
					}
					else
					{
						pen = m_AllPens[obj.TargetNo];
					}

					if (w > width - 4)
					{
						using (Bitmap clone = m_CustomZoomBitmap.Clone(
												 new Rectangle(6, beg, width - 11, objHeight),
												 m_CustomZoomBitmap.PixelFormat))
						{
							g.DrawImage(clone, new Point(5, beg));
						}

						g.DrawLine(m_MedianBackgroundPen, width - 6, beg, width - 6, beg + objHeight);
						g.DrawLine(pen, width - 6, beg + (objHeight - h) / 2, width - 6, beg + (objHeight + h) / 2);
					}
					else
					{
						g.DrawLine(pen, w, beg + (objHeight - h) / 2, w, beg + (objHeight + h) / 2);
					}
				}

				g.Save();
			}

			gMain.DrawImage(m_CustomZoomBitmap, 0, 0);
		}

		internal void SetMeasuringZoomImageType(MeasuringZoomImageType type)
		{
			this.MeasuringZoomImageType = type;
			lock(m_SyncRoot)
			{
				m_CustomZoomBitmap = null;
				m_FirstMeasuredFrame = m_CurrFrameNo;
			}
		}
        #endregion

        #region Event Handlers
        public void SelectedTargetChanged(int newSelectedIndex)
        {
        }

        public void MaxStarsReached(bool maximumReached)
        {
            m_ControlPanel.UpdateState();
        }
        #endregion

        internal bool IsMeasuring
        {
            get { return m_Measuring; }
        }

        internal bool IsRefining
        {
            get { return m_Refining; }
        }

        public static MeasurementsHelper DoConfiguredMeasurement(uint[,] matrix, PSFFit matrixFit, int bitPixCamera, double bestFindTolerance, ref float aperture, ref int matrixSize)
        {
            if (matrixFit != null && TangraConfig.Settings.Photometry.DefaultSignalApertureUnit == TangraConfig.SignalApertureUnit.FWHM)
                aperture = (float)(matrixFit.FWHM * TangraConfig.Settings.Photometry.DefaultSignalAperture);
            else
                aperture = (float)(TangraConfig.Settings.Photometry.DefaultSignalAperture);

            var measurer = new MeasurementsHelper(
                            bitPixCamera,
                            TangraConfig.Settings.Photometry.DefaultBackgroundMethod,
                            true,
                            TangraConfig.Settings.Photometry.Saturation.GetSaturationForBpp(bitPixCamera));

            measurer.SetCoreProperties(
                    TangraConfig.Settings.Photometry.AnulusInnerRadius,
                    TangraConfig.Settings.Photometry.AnulusMinPixels,
                    TangraConfig.Settings.Special.RejectionBackgroundPixelsStdDev,
                    4.0f);

            measurer.BestMatrixSizeDistanceDifferenceTolerance = bestFindTolerance;

            MeasurementsHelper.Filter measurementFilter = MeasurementsHelper.Filter.None;

            switch (LightCurveReductionContext.Instance.DigitalFilter)
            {
                case TangraConfig.PreProcessingFilter.NoFilter:
                    measurementFilter = MeasurementsHelper.Filter.None;
                    break;

                case TangraConfig.PreProcessingFilter.LowPassFilter:
                    measurementFilter = MeasurementsHelper.Filter.LP;
                    break;

                case TangraConfig.PreProcessingFilter.LowPassDifferenceFilter:
                    measurementFilter = MeasurementsHelper.Filter.LPD;
                    break;
            }

            // This will also find the best PSFFit
			measurer.Measure(9, 9, aperture, measurementFilter, matrix, bitPixCamera, double.NaN, ref matrixSize, false);

            return measurer;
        }

        public void BeginConfiguration()
        {
            m_MeasurementInterval = 1;
            m_CurrFrameNo = -1;

            m_StateMachine = new LCStateMachine(this, m_VideoController);

            m_Measuring = false;
            m_Refining = false;
            m_ViewingLightCurve = false;
            m_Configuring = true;

            m_StateMachine.SelectedMeasuringStar = -1;
            m_StateMachine.SelectedObject = null;

            m_ControlPanel.BeginConfiguration(m_StateMachine, m_VideoController);

            m_VideoController.StatusChanged("Configuring");
        }

        public void StartOver()
        {
            BeginConfiguration();

            TangraContext.Current.HasVideoLoaded = true;
            m_VideoController.UpdateViews();

            m_VideoController.RefreshCurrentFrame();
        }

        public void BeginMeasurements()
        {
			bool isColourBitmap = false;
			AstroImage currentImage = m_VideoController.GetCurrentAstroImage(false);
			//using (Bitmap nonIntegratedBmp = m_Host.FramePlayer.GetFrame(m_CurrFrameNo, true))
			//{
			//    isColourBitmap = BitmapFilter.IsColourBitmap(nonIntegratedBmp);
			//}

			LightCurveReductionContext.Instance.IsColourVideo = isColourBitmap;

			if (isColourBitmap &&
				TangraConfig.Settings.Photometry.ColourChannel != TangraConfig.ColourChannel.GrayScale)
			{
				string channel = TangraConfig.Settings.Photometry.ColourChannel.ToString();
				DialogResult dlgRes = m_VideoController.ShowMessageBox(
					"Would you like to use the GrayScale band for this measurement only?\r\n\r\n" +
					"This appears to be a colour video but the current band to measure is not set to GrayScale. It is recommended to use the GrayScale band for colour videos. \r\n\r\n" +
					"To use the GrayScale band for this reduction only - press 'Yes', to use the currently set [" + channel + "] " +
					"band press 'No'. To manually set a different band for this and other reductions press 'Cancel' and configure the band from the Tangra settings form before you continue.",
					"Warning",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Warning);

				if (dlgRes == DialogResult.Cancel)
					return;

				if (dlgRes == DialogResult.Yes)
					LightCurveReductionContext.Instance.ColourChannel = TangraConfig.ColourChannel.GrayScale;
				else
					LightCurveReductionContext.Instance.ColourChannel = TangraConfig.Settings.Photometry.ColourChannel;
			}

			LightCurveReductionContext.Instance.ColourChannel = TangraConfig.Settings.Photometry.ColourChannel;
            
            //LightCurveReductionContext.Instance.OSDFrame =
            //    RegistryConfig.Instance.OSDSizes.GetOSDRectangleForFrameSize(
            //        VideoContext.Current.VideoStream.Width, VideoContext.Current.VideoStream.Height);

			m_BackedUpSelectMeasuringStarsState = m_StateMachine.m_CurrentStateObject;
			m_Measuring = false;
			m_Refining = true;
			m_ViewingLightCurve = false;
			m_Configuring = false;
			m_Correcting = false;

			m_Tracker = TrackerFactory.CreateTracker(
				LightCurveReductionContext.Instance.LightCurveReductionType,
				m_StateMachine.MeasuringStars);

            if (m_StackedAstroImage == null)
            {
                EnsureStackedAstroImage();
                m_AveragedFrame = new AveragedFrame(m_StackedAstroImage);
            }

			m_Tracker.InitializeNewTracking();

			m_ManualTrackingDeltaX = 0;
			m_ManualTrackingDeltaY = 0;

			InitializeTimestampOCR();

			if (m_TimestampOCR != null || 
				m_VideoController.IsAstroDigitalVideo ||
				(m_VideoController.IsAstroAnalogueVideo && m_VideoController.AstroAnalogueVideoHasOcrData))
				// We have embedded timestamps for OCR-ed analogue video timestamps or for ADV videos
				LightCurveReductionContext.Instance.HasEmbeddedTimeStamps = true;
			else
				LightCurveReductionContext.Instance.HasEmbeddedTimeStamps = false;

			m_VideoController.StatusChanged("Refining");

			m_StateMachine.ChangeState(LightCurvesState.Running);

			MeasuringStarted();

			if (LightCurveReductionContext.Instance.DebugTracking)
			{
				TangraContext.Current.CanPlayVideo = true;
				m_VideoController.UpdateViews();
			}
			else
				m_VideoController.PlayVideo();
        }

		private void InitializeTimestampOCR()
		{
			m_TimestampOCR = null;

			// NOTE: Timestamp OCR not supported yet
			//m_TimestampOCR = OcrExtensionManager.GetCurrentOCR();

			//if (m_TimestampOCR != null)
			//{
			//    TimestampOCRData data = new TimestampOCRData();
			//    data.FrameWidth = m_Host.FramePlayer.Video.Width;
			//    data.FrameHeight = m_Host.FramePlayer.Video.Height;
			//    data.OSDFrame = LightCurveReductionContext.Instance.OSDFrame;
			//    data.VideoFrameRate = (float)m_Host.FramePlayer.Video.FrameRate;
			//    // NOTE: This is taking too long to calculate and is not used for OCR
			//    //data.MedianBrightness = VideoContext.Current.AstroImage.MedianNoise;
			//    data.SourceInfo = m_Host.FramePlayer.Video.SourceInfo;

			//    m_TimestampOCR.Initialize(data);
			//}
		}

        public void EnsureStackedAstroImage()
        {
            if (m_StackedAstroImage == null)
            {
                m_ControlPanel.Cursor = Cursors.WaitCursor;
                try
                {
                    m_StackedAstroImage = m_VideoController.GetCurrentAstroImage(!LightCurveReductionContext.Instance.WindOrShaking);
                }
                finally
                {
                    m_ControlPanel.Cursor = Cursors.Default;
                }
            }   
        }

        public void StopRefining()
        {
			m_Measuring = false;
			m_Refining = false;
			m_ViewingLightCurve = false;
			m_Configuring = true;

			m_Tracker = null;
			m_VideoController.StatusChanged("Configuring");

			m_VideoController.StopVideo();
			m_VideoController.MoveToFrame(m_StateMachine.m_ConfiguringFrame);

			TangraContext.Current.CanPlayVideo = false;
			TangraContext.Current.CanScrollFrames = false;
	        m_VideoController.UpdateViews();

			m_StateMachine.m_CurrentState = LightCurvesState.SelectMeasuringStars;
			m_StateMachine.m_CurrentStateObject = m_BackedUpSelectMeasuringStarsState;
			m_ControlPanel.UpdateState();
        }

        public void ContinueMeasurements(int continueAtFrame)
        {
			m_Measuring = true;
			m_Correcting = false;
			m_Refining = false;
			m_ViewingLightCurve = false;
			m_Configuring = false;

			if (m_ManualTrackingDeltaX != 0 || m_ManualTrackingDeltaY != 0)
				m_Tracker.DoManualFrameCorrection(m_ManualTrackingDeltaX, m_ManualTrackingDeltaY);

			m_ManualTrackingDeltaX = 0;
			m_ManualTrackingDeltaY = 0;

			m_VideoController.PlayVideo();

			m_VideoController.StatusChanged("Measuring");

			// Change back to arrow tool
			m_VideoController.SelectImageTool<ArrowTool>();
        }

        public void StopMeasurements()
        {
			m_Measuring = true;
			m_Correcting = true;
			m_Refining = false;
			m_ViewingLightCurve = false;
			m_Configuring = false;

			m_VideoController.StopVideo();
			m_VideoController.StatusChanged("Stopped");

			TangraContext.Current.CanPlayVideo = false;
			TangraContext.Current.CanScrollFrames = false;
			m_VideoController.UpdateViews();

			// Allow correction of the tracking
			CorrectTrackingTool correctTrackingTool = m_VideoController.SelectImageTool<CorrectTrackingTool>() as CorrectTrackingTool;
			if (correctTrackingTool != null)
				correctTrackingTool.Initialize(this, m_Tracker, m_VideoController);

			m_StateMachine.m_HasBeenPaused = true;
        }

        public void FinishedWithMeasurements()
        {
			m_Measuring = false;
			m_Refining = false;
			m_ViewingLightCurve = false;
			m_Configuring = false;
			m_Correcting = false;

			m_VideoController.StatusChanged("Ready");

			SaveSessionFile();
        }

        public void ShowLightCurve()
        {
			FlushLightCurveFile();

			m_Measuring = false;
			m_Refining = false;
			m_ViewingLightCurve = true;
			m_Configuring = false;

			m_StateMachine.ChangeState(LightCurvesState.Viewing);

			DoShowLightCurve();

			m_ControlPanel.SetupLCFileInfo(m_lcFile);
			m_ControlPanel.UpdateState();
        }

        public void InitGetStartTime()
        {
			m_StateMachine.ChangeState(LightCurvesState.SelectingFrameTimes);

			m_StartFrameTime = DateTime.MaxValue;
			m_EndFrameTime = DateTime.MinValue;

			var avoider = new DuplicateFrameAvoider(m_VideoController, (int)m_MinFrame);
			int firstGoodFrame = avoider.GetFirstGoodFrameId();

			m_VideoController.MoveToFrame(firstGoodFrame);

			TangraContext.Current.CanPlayVideo = false;
			TangraContext.Current.CanScrollFrames = false;
			m_VideoController.UpdateViews();
        }

        public void InitGetEndTime()
        {
			var avoider = new DuplicateFrameAvoider(m_VideoController, (int)m_MaxFrame);
			int lastGoodFrame = avoider.GetLastGoodFrameId();

            m_VideoController.MoveToFrame(lastGoodFrame);
        }

        private DateTime m_StartFrameTime;
        private DateTime m_EndFrameTime;
        private int m_StartTimeFrame;
        private int m_EndTimeFrame;

        public void SetStartTime(DateTime startTime)
        {
            m_StartFrameTime = startTime;
            m_StartTimeFrame = m_CurrFrameNo;
        }

        public void SetEndTime(DateTime endTime)
        {
            m_EndFrameTime = endTime;
            m_EndTimeFrame = m_CurrFrameNo;

            //TODO: Fix the time computations when the entered times are not for the very first and very last frames
        }

        public DialogResult EnteredTimeIntervalLooksOkay()
        {
            if ((m_VideoController.VideoFrameRate < 24.0 && m_VideoController.VideoFrameRate > 26.0) ||
                (m_VideoController.VideoFrameRate < 29.0 && m_VideoController.VideoFrameRate > 31.0))
            {
                MessageBox.Show(
                    string.Format("This video has an unusual frame rate of {0}. Tangra cannot run internal checks for the correctness of the entered frame times.", m_VideoController.VideoFrameRate.ToString("0.00")),
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return DialogResult.Ignore;
            }

            double acceptedVideoFrameRate = m_VideoController.VideoFrameRate > 24.0 && m_VideoController.VideoFrameRate < 26.0
                                                ? 25.0 /* PAL */
                                                : 29.97 /* NTSC */;
            string videoType = m_VideoController.VideoFrameRate > 24.0 && m_VideoController.VideoFrameRate < 26.0
                                   ? "PAL"
                                   : "NTSC";

            TimeSpan ts = new TimeSpan(m_EndFrameTime.Ticks - m_StartFrameTime.Ticks);
            double videoTimeInSec = (m_EndTimeFrame - m_StartTimeFrame) / acceptedVideoFrameRate;

            if (videoTimeInSec < 0 || Math.Abs((videoTimeInSec - ts.TotalSeconds) * 1000) > TangraConfig.Settings.Special.MaxAllowedTimestampShiftInMs)
            {
                if (MessageBox.Show(
                    string.Format("The time computed from the measured number of frames in this {1} video is off by more than {0} ms from the entered time. This may indicate " +
                    "incorrectly entered start or end time or an almanac update or a leap second event. Do you want to enter the start and end times again?",
                    (TangraConfig.Settings.Special.MaxAllowedTimestampShiftInMs).ToString("0.00"),
                    videoType),
                    "Warning",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                {
                    return DialogResult.Retry;
                }

                return DialogResult.Ignore;
            }

            double derivedFrameRate = (m_EndTimeFrame - m_StartTimeFrame) / ts.TotalSeconds;

            // 1) compute 1 ms plus 1ms for each 30 sec up to the max of 4ms. e.g. if this is a PAL video and we have measured 780 frames, this makes 780 / 25fps (PAL) = 31.2 sec. So we take excess = 1 + 1 sec.
            // 2) max allowed difference = 1.33 * Module[video frame rate - (video frame rate * num frames + excess) / num frames]
            int allowedExcess = 1 + Math.Min(4, (int)((m_EndTimeFrame - m_StartTimeFrame) / acceptedVideoFrameRate) / 30);
            double maxAllowedFRDiff = 1.33 * Math.Abs(acceptedVideoFrameRate - ((acceptedVideoFrameRate * (m_EndTimeFrame - m_StartTimeFrame) + allowedExcess) / (m_EndTimeFrame - m_StartTimeFrame)));

            if (Math.Abs(derivedFrameRate - acceptedVideoFrameRate) > maxAllowedFRDiff)
            {
                if (MessageBox.Show(
                    "Based on your entered frame times it appears that there may be dropped frames, incorrectly entered start " +
                    "or end time, an almanac update or a leap second event. Do you want to enter the start and end times again?",
                    "Warning",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                {
                    return DialogResult.Retry;
                }

                return DialogResult.Ignore;
            }

            return DialogResult.OK;
        }

		internal void EnterViewLightCurveMode(LCFile lcFile, IVideoController videoController, Panel controlPanel)
		{
			// TODO: This method needs to be called from somewhere else

			m_VideoController = (VideoController)videoController;
			EnsureControlPanel(controlPanel);

			m_lcFile = lcFile;

			m_Measuring = false;
			m_Refining = false;
			m_ViewingLightCurve = true;
			m_Configuring = false;

			m_StateMachine = new LCStateMachine(this, m_VideoController);
			m_StateMachine.ChangeState(LightCurvesState.Viewing);

			m_StateMachine.SelectedMeasuringStar = -1;
			m_StateMachine.SelectedObject = null;

			m_MeasurementInterval = 1;
			m_CurrFrameNo = -1;

			m_ControlPanel.BeginConfiguration(m_StateMachine, m_VideoController);
			m_ControlPanel.SetupLCFileInfo(m_lcFile);
			m_ControlPanel.UpdateState();

		}

        internal void ToggleShowFields(bool showFields)
        {
			m_VideoController.RedrawCurrentFrame(showFields);
        }

        private long prevFrameId;
        public void SaveEmbeddedOrORCedTimeStamp()
        {
			if (m_VideoController.HasAstroImageState)
			{
                if (m_VideoController.IsAstroDigitalVideo ||
                    (m_VideoController.IsAstroAnalogueVideo && m_VideoController.AstroAnalogueVideoHasOcrData))
                {
                    FrameStateData frameState = m_VideoController.GetCurrentFrameState();
                    Trace.Assert(prevFrameId != frameState.VideoCameraFrameId || frameState.VideoCameraFrameId == 0 /* When VideoCameraFrameId is not supported */);
                    prevFrameId = frameState.VideoCameraFrameId;

                    int frameDuration = (int)Math.Round(frameState.ExposureInMilliseconds);

					//TODO: If Instrumental Delay has been selected then apply if for this frameDuration and camera model
                    LCFile.SaveOnTheFlyFrameTiming(new LCFrameTiming(frameState.CentralExposureTime, frameDuration));                    
                }
                else
                {
                    // Nothing to save
                }
			}
			else if (m_TimestampOCR != null)
			{
				int frameDuration = (int)Math.Round(1000.0/m_VideoController.VideoFrameRate /*MillisecondsPerFrame*/);
				LCFile.SaveOnTheFlyFrameTiming(new LCFrameTiming(m_OCRedTimeStamp, frameDuration));
			}
        }

        public void MeasureObjects()
        {
            if (m_MinFrame > m_CurrFrameNo) m_MinFrame = (uint)m_CurrFrameNo;
            if (m_MaxFrame < m_CurrFrameNo) m_MaxFrame = (uint)m_CurrFrameNo;
            m_TotalFrames++;

			foreach (TrackedObject trackedObject in m_Tracker.TrackedObjects)
			{
				ImagePixel center = trackedObject.Center;

				if (center != ImagePixel.Unspecified)
				{
					MeasureTrackedObject2(trackedObject,
											m_Measurer,
											LightCurveReductionContext.Instance.DigitalFilter,
											false);
				}
				else
				{
					uint[,] data = m_VideoController.GetCurrentAstroImage(false).GetMeasurableAreaPixels(
						(int)Math.Round(center.XDouble), (int)Math.Round(center.YDouble), 35);

					uint flags = trackedObject.GetLCMeasurementFlags();
					// Add unsuccessfull measurement for this object and this frame
					LCFile.SaveOnTheFlyMeasurement(new LCMeasurement(
													(uint)m_CurrFrameNo,
													trackedObject.TargetNo,
													0,
													0,
													flags,
													0, 0,
						/* We want to use the real image (X,Y) coordinates here */
						//(float) aperture,
													null,
						/* but we want to use the actual measurement fit. This only matters for reviewing the light curve when not opened from a file. */
													data /* save the original non filtered data */, 0, 0, m_OCRedTimeStamp));
				}
            }
        }

        private SpinLock m_WriterLock;

		private void MeasureTrackedObject2(
			TrackedObject trackedObject,
			MeasurementsHelper measurer,
			TangraConfig.PreProcessingFilter filter,
			bool synchronise)
		{
			ImagePixel center = trackedObject.Center;
			int areaSize = LightCurveReductionContext.Instance.DigitalFilter == TangraConfig.PreProcessingFilter.NoFilter
							   ? 17
							   : 19;

			int centerX = (int)Math.Round(center.XDouble);
			int centerY = (int)Math.Round(center.YDouble);

			uint[,] data = m_VideoController.GetCurrentAstroImage(false).GetMeasurableAreaPixels(centerX, centerY, areaSize);
			uint[,] backgroundPixels = m_VideoController.GetCurrentAstroImage(false).GetMeasurableAreaPixels(centerX, centerY, 35);

			float msrX0 = trackedObject.ThisFrameX;
			float msrY0 = trackedObject.ThisFrameY;

			MeasureObject(
				center,
				data,				
				backgroundPixels,
				m_VideoController.VideoBitPix,
				measurer,
				filter,
				synchronise,
				LightCurveReductionContext.Instance.ReductionMethod,
				trackedObject.Aperture,
				m_Tracker.RefinedFWHM[trackedObject.TargetNo],
				m_Tracker.RefinedAverageFWHM,
				trackedObject,
				LightCurveReductionContext.Instance.FullDisappearance);

			uint[,] pixelsToSave = trackedObject.IsOffScreen
									   ? new uint[35, 35]
									   // As the background may have been pre-processed for measuring, we need to take another copy for saving in the file
									   : m_VideoController.GetCurrentAstroImage(false).GetMeasurableAreaPixels(centerX, centerY, 35);

			bool lockTaken = false;

			if (synchronise)
				m_WriterLock.TryEnter(ref lockTaken);

			try
			{
				uint flags = trackedObject.GetLCMeasurementFlags();

				LCFile.SaveOnTheFlyMeasurement(new LCMeasurement(
												   (uint)m_CurrFrameNo,
												   trackedObject.TargetNo,
												   (uint)Math.Round(measurer.TotalReading),
												   (uint)Math.Round(measurer.TotalBackground),
												   flags,
												   msrX0, msrY0,
					/* We want to use the real image (X,Y) coordinates here */
					//(float) aperture,
												   trackedObject.ThisFrameFit,
					/* but we want to use the actual measurement fit. This only matters for reviewing the light curve when not opened from a file. */
												   pixelsToSave /* save the original non filtered data */, centerX, centerY, m_OCRedTimeStamp));
			}
			finally
			{
				if (synchronise && lockTaken)
					m_WriterLock.Exit();
			}
		}

		internal static void MeasureObject(
			ImagePixel center,
			uint[,] data,
			uint[,] backgroundPixels,
			int bpp,
			MeasurementsHelper measurer,
			TangraConfig.PreProcessingFilter filter,
			bool synchronise,
			TangraConfig.PhotometryReductionMethod reductionMethod,
			float aperture,
			double refinedFWHM,
			float refinedAverageFWHM,
			IMeasuredObject measuredObject,
			bool fullDisappearance
			)
		{
			if (reductionMethod == TangraConfig.PhotometryReductionMethod.PsfPhotometryAnalytical || reductionMethod == TangraConfig.PhotometryReductionMethod.PsfPhotometryNumerical)
			{
				if (TangraConfig.Settings.Photometry.PsfQuadrature == TangraConfig.PsfQuadrature.NumericalInAperture)
					reductionMethod = TangraConfig.PhotometryReductionMethod.PsfPhotometryNumerical;
				else if (TangraConfig.Settings.Photometry.PsfQuadrature == TangraConfig.PsfQuadrature.Analytical)
					reductionMethod = TangraConfig.PhotometryReductionMethod.PsfPhotometryAnalytical;
			}

			measurer.MeasureObject(center, data, backgroundPixels, bpp, filter, synchronise, reductionMethod,
								   aperture, refinedFWHM, refinedAverageFWHM, measuredObject, fullDisappearance);
	}

		private LCFile m_lcFile = null;

		internal void FlushLightCurveFile()
		{
			List<int> matrixSizes = new List<int>();
			List<float> apertures = new List<float>();
			List<bool> fixedFlags = new List<bool>();

			m_Tracker.TrackedObjects.ForEach(
				delegate(TrackedObject o)
				{
					matrixSizes.Add(o.PsfFitMatrixSize);
					apertures.Add(o.Aperture);
					fixedFlags.Add(o.OriginalObject.IsWeakSignalObject);
				}
			);

			MeasurementTimingType measurementTimingType = MeasurementTimingType.UserEnteredFrameReferences;
			if (m_VideoController.IsAstroDigitalVideo || 
                (m_VideoController.IsAstroAnalogueVideo && m_VideoController.AstroAnalogueVideoHasOcrData))
				measurementTimingType = MeasurementTimingType.EmbeddedTimeForEachFrame;
			else if (m_TimestampOCR != null)
				measurementTimingType = MeasurementTimingType.OCRedTimeForEachFrame;

			LCMeasurementHeader finalHeader = new LCMeasurementHeader(
				m_VideoController.CurrentVideoFileName,
				string.Format("Video ({0})", m_VideoController.CurrentVideoFileType),
				m_VideoController.VideoFirstFrame,
				m_VideoController.VideoCountFrames,
				m_VideoController.VideoFrameRate,
				m_MinFrame,
				m_MaxFrame,
				(uint)m_TotalFrames,
				(uint)m_MeasurementInterval,
				(byte)m_Tracker.TrackedObjects.Count,
				LightCurveReductionContext.Instance.LightCurveReductionType,
				measurementTimingType,
				(int)LightCurveReductionContext.Instance.NoiseMethod,
				(int)LightCurveReductionContext.Instance.DigitalFilter,
				matrixSizes.ToArray(), apertures.ToArray(), fixedFlags.ToArray(), (float)m_Tracker.PositionTolerance);

			finalHeader.FirstTimedFrameTime = m_StartFrameTime;
			finalHeader.SecondTimedFrameTime = m_EndFrameTime;

			finalHeader.FirstTimedFrameNo = m_StartTimeFrame;
			finalHeader.LastTimedFrameNo = m_EndTimeFrame;

			if (m_AveragedFrame == null)
			{
				if (m_StackedAstroImage == null) EnsureStackedAstroImage();
				m_AveragedFrame = new AveragedFrame(m_StackedAstroImage);
			}

			LCMeasurementFooter footer = new LCMeasurementFooter(
				m_AveragedFrame.Pixelmap,
				TangraConfig.Settings,
				LightCurveReductionContext.Instance,
				m_StateMachine.MeasuringStars,
				m_Tracker,
				m_TimestampOCR,
				null);

			m_lcFile = LCFile.FlushOnTheFlyOutputFile(finalHeader, footer);
		}

		internal void DoShowLightCurve()
		{   
			m_LightCurveController.EnsureLightCurveFormClosed();
			m_VideoController.EnsureLightCurveForm();

			TangraContext.Current.HasVideoLoaded = true;
			TangraContext.Current.CanPlayVideo = false;
			m_VideoController.UpdateViews();

			m_LightCurveController.SetLcFile(m_lcFile);
			m_VideoController.MoveToFrame((int)m_lcFile.Header.MinFrame);			
		}
    }

    internal enum MeasuringZoomImageType
    {
        Stripe,
        Pixel,
        None
    }

}