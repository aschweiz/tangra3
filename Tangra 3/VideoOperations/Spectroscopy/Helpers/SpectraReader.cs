﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using Tangra.Helpers;
using Tangra.Model.Astro;
using Tangra.Model.Config;

namespace Tangra.VideoOperations.Spectroscopy.Helpers
{
	public class SpectraPoint
	{
		public int PixelNo;
		public float Wavelength;
        public float RawSignal;
        public float RawSignalPixelCount;
		public float RawBackgroundPerPixel;

	    private float m_RawValue;
        public float RawValue 
        {
            get { return m_RawValue; }
            set
            {
                m_RawValue = value;
                ProcessedValue = m_RawValue;
            }
        }
	    public float SmoothedValue;

        public float ProcessedValue;

		private static int SERIALIZATION_VERSION = 1;

		internal SpectraPoint()
		{ }

        internal SpectraPoint(SpectraPoint cloneFrom)
        {
            PixelNo = cloneFrom.PixelNo;
            Wavelength = cloneFrom.Wavelength;
            RawSignal = cloneFrom.RawSignal;
            RawSignalPixelCount = cloneFrom.RawSignalPixelCount;
            RawBackgroundPerPixel = cloneFrom.RawBackgroundPerPixel;
            RawValue = cloneFrom.RawValue;
            SmoothedValue = cloneFrom.SmoothedValue;
        }

		public SpectraPoint(BinaryReader reader)
		{
			int version = reader.ReadInt32();

			PixelNo = reader.ReadInt32();
			Wavelength = reader.ReadSingle();
			RawSignal = reader.ReadSingle();
			RawSignalPixelCount = reader.ReadSingle();
			RawBackgroundPerPixel = reader.ReadSingle();
			RawValue = reader.ReadSingle();
			SmoothedValue = reader.ReadSingle();
		}

		public void WriteTo(BinaryWriter writer)
		{
			writer.Write(SERIALIZATION_VERSION);

			writer.Write(PixelNo);
			writer.Write(Wavelength);
			writer.Write(RawSignal);
			writer.Write(RawSignalPixelCount);
			writer.Write(RawBackgroundPerPixel);
			writer.Write(RawValue);
			writer.Write(SmoothedValue);
		}
	}
			

    public class MasterSpectra : Spectra
    {
        public int CombinedMeasurements;

		public SpectraCalibration Calibration;
        public MeasurementInfo MeasurementInfo;
		public ObservationInfo ObservationInfo;
        public ProcessingInfo ProcessingInfo = new ProcessingInfo();

        public List<Spectra> RawMeasurements = new List<Spectra>();

		public bool IsCalibrated()
		{
			return Calibration != null;
		}

		private static int SERIALIZATION_VERSION = 2;

	    internal MasterSpectra()
	    { }

	    public MasterSpectra(BinaryReader reader)
	    {
		    int version = reader.ReadInt32();

			CombinedMeasurements = reader.ReadInt32();
			SignalAreaWidth = reader.ReadInt32();
			MaxPixelValue = reader.ReadUInt32();
			MaxSpectraValue = reader.ReadUInt32();
			ZeroOrderPixelNo = reader.ReadInt32();

			int pixelsCount = reader.ReadInt32();
		    Points = new List<SpectraPoint>();
			for (int i = 0; i < pixelsCount; i++)
			{
				var point = new SpectraPoint(reader);
				Points.Add(point);
			}

            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
	        Pixels = new float[width,height];
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Pixels[x, y] = reader.ReadSingle();
            }

	        MeasurementInfo = new MeasurementInfo(reader);
	        if (reader.ReadBoolean())
	            Calibration = new SpectraCalibration(reader);

            int rawFramesCount = reader.ReadInt32();
            for (int i = 0; i < rawFramesCount; i++)
            {
                var frameMeasurement = new Spectra();
                RawMeasurements.Add(frameMeasurement);

                frameMeasurement.SignalAreaWidth = reader.ReadInt32();
                frameMeasurement.MaxPixelValue = reader.ReadUInt32();
                frameMeasurement.MaxSpectraValue = reader.ReadUInt32();
                frameMeasurement.ZeroOrderPixelNo = reader.ReadInt32();

                frameMeasurement.ZeroOrderFWHM = float.NaN;
                if (version > 1)
                {
                    frameMeasurement.ZeroOrderFWHM = reader.ReadSingle();    
                }

                int frameMeaCount = reader.ReadInt32();

                for (int j = 0; j < frameMeaCount; j++)
                {
                    var point = new SpectraPoint(reader);
                    frameMeasurement.Points.Add(point);
                }
            }

	        ProcessingInfo = new ProcessingInfo(reader);
		    ObservationInfo = new ObservationInfo(reader);

	        ZeroOrderFWHM = float.NaN;
            if (version > 1)
            {
                ZeroOrderFWHM = reader.ReadSingle();
            }
	    }

	    public void WriteTo(BinaryWriter writer)
	    {
			writer.Write(SERIALIZATION_VERSION);

			writer.Write(CombinedMeasurements);
			writer.Write(SignalAreaWidth);
			writer.Write(MaxPixelValue);
		    writer.Write(MaxSpectraValue);
			writer.Write(ZeroOrderPixelNo);

			writer.Write(Points.Count);
		    foreach (var spectraPoint in Points)
		    {
				spectraPoint.WriteTo(writer);
		    }

	        int width = Pixels.GetLength(0);
            int height = Pixels.GetLength(1);
            writer.Write(width);
            writer.Write(height);
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                writer.Write(Pixels[x, y]);
            }

            MeasurementInfo.WriteTo(writer);

            if (Calibration == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                Calibration.WriteTo(writer);    
            }

            writer.Write(RawMeasurements.Count);
            for (int i = 0; i < RawMeasurements.Count; i++)
            {
                Spectra frameSpectra = RawMeasurements[i];

                writer.Write(frameSpectra.SignalAreaWidth);
                writer.Write(frameSpectra.MaxPixelValue);
                writer.Write(frameSpectra.MaxSpectraValue);
                writer.Write(frameSpectra.ZeroOrderPixelNo);
                writer.Write(frameSpectra.ZeroOrderFWHM); // Version 2 Property

                writer.Write(frameSpectra.Points.Count);
                foreach (var spectraPoint in frameSpectra.Points)
                {
                    spectraPoint.WriteTo(writer);
                }
	        }

	        ProcessingInfo.WriteTo(writer);
			ObservationInfo.WriteTo(writer);

            // Version 2 Properties
	        writer.Write(ZeroOrderFWHM);
	    }
    }

	public class SpectraCalibration
	{
		public float Pixel1 { get; set; }
		public float Pixel2 { get; set; }
		public float Pixel3 { get; set; }
		public float Pixel4 { get; set; }
		public float Pixel5 { get; set; }
		public float Pixel6 { get; set; }
		public float Pixel7 { get; set; }
		public float Pixel8 { get; set; }
		public float Wavelength1 { get; set; }
		public float Wavelength2 { get; set; }
		public float Wavelength3 { get; set; }
		public float Wavelength4 { get; set; }
		public float Wavelength5 { get; set; }
		public float Wavelength6 { get; set; }
		public float Wavelength7 { get; set; }
		public float Wavelength8 { get; set; }
		public float Dispersion { get; set; }
		public float ZeroPixel { get; set; }
		public float A { get; set; }
		public float B { get; set; }
		public float C { get; set; }
		public float D { get; set; }
		public float E { get; set; }
		public int PolynomialOrder { get; set; }
		public float RMS { get; set; }
		public string FitType { get; set; }

		private static int SERIALIZATION_VERSION = 1;

		internal SpectraCalibration()
		{
			Wavelength1 = float.NaN;
			Wavelength2 = float.NaN;
			Wavelength3 = float.NaN;
			Wavelength4 = float.NaN;
			Wavelength5 = float.NaN;
			Wavelength6 = float.NaN;
			Wavelength7 = float.NaN;
			Wavelength8 = float.NaN;
			Pixel1 = float.NaN;
			Pixel2 = float.NaN;
			Pixel3 = float.NaN;
			Pixel4 = float.NaN;
			Pixel5 = float.NaN;
			Pixel6 = float.NaN;
			Pixel7 = float.NaN;
			Pixel8 = float.NaN;
		}

		public SpectraCalibration(BinaryReader reader)
		{
			int version = reader.ReadInt32();

			Pixel1 = reader.ReadSingle();
			Pixel2 = reader.ReadSingle();
			Pixel3 = reader.ReadSingle();
			Pixel4 = reader.ReadSingle();
			Pixel5 = reader.ReadSingle();
			Pixel6 = reader.ReadSingle();
			Pixel7 = reader.ReadSingle();
			Pixel8 = reader.ReadSingle();
			Wavelength1 = reader.ReadSingle();
			Wavelength2 = reader.ReadSingle();
			Wavelength3 = reader.ReadSingle();
			Wavelength4 = reader.ReadSingle();
			Wavelength5 = reader.ReadSingle();
			Wavelength6 = reader.ReadSingle();
			Wavelength7 = reader.ReadSingle();
			Wavelength8 = reader.ReadSingle();
			Dispersion = reader.ReadSingle();
			ZeroPixel = reader.ReadSingle();
			PolynomialOrder = reader.ReadInt32();
			A = reader.ReadSingle();
			B = reader.ReadSingle();
			C = reader.ReadSingle();
			D = reader.ReadSingle();
			E = reader.ReadSingle();
			RMS = reader.ReadSingle();
			FitType = reader.ReadString();
		}

		public void WriteTo(BinaryWriter writer)
		{
			writer.Write(SERIALIZATION_VERSION);

			writer.Write(Pixel1);
			writer.Write(Pixel2);
			writer.Write(Pixel3);
			writer.Write(Pixel4);
			writer.Write(Pixel5);
			writer.Write(Pixel6);
			writer.Write(Pixel7);
			writer.Write(Pixel8);
			writer.Write(Wavelength1);
			writer.Write(Wavelength2);
			writer.Write(Wavelength3);
			writer.Write(Wavelength4);
			writer.Write(Wavelength5);
			writer.Write(Wavelength6);
			writer.Write(Wavelength7);
			writer.Write(Wavelength8);
			writer.Write(Dispersion);
			writer.Write(ZeroPixel);
			writer.Write(PolynomialOrder);
			writer.Write(A);
			writer.Write(B);
			writer.Write(C);
			writer.Write(D);
			writer.Write(E);
			writer.Write(RMS);
			writer.Write(FitType);
		}
	}

    public class ProcessingInfo
    {
        public int? GaussianBlur10Fwhm;

        private static int SERIALIZATION_VERSION = 1;

		internal ProcessingInfo()
		{ }

        public ProcessingInfo(BinaryReader reader)
        {
            int version = reader.ReadInt32();

            if (reader.ReadBoolean())
                GaussianBlur10Fwhm = reader.ReadInt32();
            else
                GaussianBlur10Fwhm = null;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(SERIALIZATION_VERSION);

            writer.Write(GaussianBlur10Fwhm.HasValue);
            if (GaussianBlur10Fwhm.HasValue) writer.Write(GaussianBlur10Fwhm.Value);
        }

    }

	public class ObservationInfo
	{
		private static int SERIALIZATION_VERSION = 1;

		private Dictionary<string, string> m_Properties = new Dictionary<string, string>();
		private Dictionary<string, string> m_PropertyComments = new Dictionary<string, string>();

		public void AddProperty(string name, string value, string comment = null)
		{
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("name");

            if (comment == null) comment = string.Empty;
            if (value == null) value = string.Empty;

            m_Properties[name] = value;		    
		    m_PropertyComments[name] = comment;
		}

		public void Reset()
		{
			m_Properties.Clear();
			m_PropertyComments.Clear();
		}

		public bool IsEmpty
		{
			get { return m_Properties.Count == 0; }
		}

		public string[] GetPropertyNames()
		{
			return m_Properties.Keys.ToArray();
		}


		public string GetProperty(string name)
		{
			string value;
			if (m_Properties.TryGetValue(name, out value))
				return value;

			return null;
		}

		public string GetPropertyComment(string name)
		{
			string value;
			if (m_PropertyComments.TryGetValue(name, out value))
				return value;

			return null;
		}

		internal ObservationInfo()
		{ }

		public ObservationInfo(BinaryReader reader)
		{
			int version = reader.ReadInt32();

			int numProps = reader.ReadInt32();

			for (int i = 0; i < numProps; i++)
			{
				string name = reader.ReadString();
				string value = reader.ReadString();
				string comment = reader.ReadString();

				AddProperty(name, value, comment);
			}
		}

		public void WriteTo(BinaryWriter writer)
		{
			writer.Write(SERIALIZATION_VERSION);

			int numPros = m_Properties.Count;
			writer.Write(numPros);

			foreach (string key in m_Properties.Keys)
			{
				writer.Write(key);
                writer.Write(m_Properties[key]);
				writer.Write(m_PropertyComments[key]);
			}
		}
	}

    public class MeasurementInfo
    {
        // SpectraReductionContext items
        public int FramesToMeasure { get; set; }
        public int MeasurementAreaWing { get; set; }
        public int BackgroundAreaWing { get; set; }
		public int BackgroundAreaGap { get; set; }
        public PixelCombineMethod BackgroundMethod { get; set; }
        public PixelCombineMethod FrameCombineMethod { get; set; }
        public bool UseFineAdjustments { get; set; }
        public int? AlignmentAbsorptionLinePos { get; set; }

        // Other items
        public int FirstMeasuredFrame;
        public int LastMeasuredFrame;
        public DateTime? FirstFrameTimeStamp;
        public DateTime? LastFrameTimeStamp;
	    public byte[] FrameBitmapPixels;
        public float Gain;
        public float ExposureSeconds;

        private static int SERIALIZATION_VERSION = 1;

		internal MeasurementInfo()
		{ }

        public MeasurementInfo(BinaryReader reader)
		{
			int version = reader.ReadInt32();

            FramesToMeasure = reader.ReadInt32();
            MeasurementAreaWing = reader.ReadInt32();
            BackgroundAreaWing = reader.ReadInt32();
			BackgroundAreaGap = reader.ReadInt32();
            BackgroundMethod = (PixelCombineMethod)reader.ReadInt32();
            FrameCombineMethod = (PixelCombineMethod)reader.ReadInt32();
            UseFineAdjustments = reader.ReadBoolean();
            reader.ReadBoolean(); /* Deprecated */
            FirstMeasuredFrame = reader.ReadInt32();
            LastMeasuredFrame = reader.ReadInt32();
            bool hasAlignmentAbsorptionLinePos = reader.ReadBoolean();
            if (hasAlignmentAbsorptionLinePos) AlignmentAbsorptionLinePos = reader.ReadInt32();
            bool hasFirstTimestamp = reader.ReadBoolean();
            if (hasFirstTimestamp) FirstFrameTimeStamp = new DateTime(reader.ReadInt64());
            bool hasLastTimestamp = reader.ReadBoolean();
            if (hasLastTimestamp) LastFrameTimeStamp = new DateTime(reader.ReadInt64());

	        int bytesToRead = reader.ReadInt32();
			if (bytesToRead > 0)
				FrameBitmapPixels = reader.ReadBytes(bytesToRead);

            Gain = reader.ReadSingle();
            ExposureSeconds = reader.ReadSingle();
		}

		public void WriteTo(BinaryWriter writer)
		{
			writer.Write(SERIALIZATION_VERSION);

            writer.Write(FramesToMeasure);
            writer.Write(MeasurementAreaWing);
            writer.Write(BackgroundAreaWing);
			writer.Write(BackgroundAreaGap);
            writer.Write((int)BackgroundMethod);
            writer.Write((int)FrameCombineMethod);
            writer.Write(UseFineAdjustments);
            writer.Write(false /* Deprecated */);
            writer.Write(FirstMeasuredFrame);
            writer.Write(LastMeasuredFrame);
            writer.Write(AlignmentAbsorptionLinePos.HasValue);
            if (AlignmentAbsorptionLinePos.HasValue) writer.Write(AlignmentAbsorptionLinePos.Value);
		    writer.Write(FirstFrameTimeStamp.HasValue);
            if (FirstFrameTimeStamp.HasValue) writer.Write(FirstFrameTimeStamp.Value.Ticks);
            writer.Write(LastFrameTimeStamp.HasValue);
            if (LastFrameTimeStamp.HasValue) writer.Write(LastFrameTimeStamp.Value.Ticks);

			writer.Write(FrameBitmapPixels.Length);
			writer.Write(FrameBitmapPixels);
		    writer.Write(Gain);
            writer.Write(ExposureSeconds);
		}
    }

	public class Spectra
	{
		public int SignalAreaWidth;
		public int BackgroundAreaHalfWidth;
        public int BackgroundAreaGap;
		public uint MaxPixelValue;
		public uint MaxSpectraValue;
	    public int ZeroOrderPixelNo;
        public float ZeroOrderFWHM;

		public List<SpectraPoint> Points = new List<SpectraPoint>();

		public float[,] Pixels;

		public void InitialisePixelArray(int width)
		{
			Pixels = new float[width, SignalAreaWidth + 2 * BackgroundAreaHalfWidth + 2 * BackgroundAreaGap];
		}
	}

	public enum PixelCombineMethod
	{
		Average,
		Median
	}

	public class SpectraReader
	{
		private AstroImage m_Image;
		private RotationMapper m_Mapper;
		private RectangleF m_SourceVideoFrame;

        private float[] m_BgValues;
        private uint[] m_BgPixelCount;
		private List<float>[] m_BgValuesList;
        private float m_PixelValueCoeff = 1;

		public SpectraReader(AstroImage image, float angleDegrees, float pixelValueCoefficient)
		{
			m_Image = image;
			m_Mapper = new RotationMapper(image.Width, image.Height, angleDegrees);
			m_SourceVideoFrame = new RectangleF(0, 0, image.Width, image.Height);
            m_PixelValueCoeff = pixelValueCoefficient;
		}

        public Spectra ReadSpectra(float x0, float y0, int halfWidth, int bgHalfWidth, int bgGap, PixelCombineMethod bgMethod)
		{
			var rv = new Spectra()
			{
				SignalAreaWidth = 2 * halfWidth,
				BackgroundAreaHalfWidth = bgHalfWidth,
                BackgroundAreaGap = bgGap,
                MaxPixelValue = m_Image.Pixelmap.MaxSignalValue
			};

			int xFrom = int.MaxValue;
			int xTo = int.MinValue;

			// Find the destination pixel range at the destination horizontal
			PointF p1 = m_Mapper.GetDestCoords(x0, y0);
		    rv.ZeroOrderPixelNo = (int)Math.Round(p1.X);
            
			for (float x = p1.X - m_Mapper.MaxDestDiagonal; x < p1.X + m_Mapper.MaxDestDiagonal; x++)
			{
				PointF p = m_Mapper.GetSourceCoords(x, p1.Y);
				if (m_SourceVideoFrame.Contains(p))
				{
				    int xx = (int) x;

					if (xx < xFrom) xFrom = xx;
					if (xx > xTo) xTo = xx;
				}
			}

			m_BgValues = new float[xTo - xFrom + 1];
            m_BgPixelCount = new uint[xTo - xFrom + 1];
            m_BgValuesList = new List<float>[xTo - xFrom + 1];
			rv.InitialisePixelArray(xTo - xFrom + 1);

			// Get all readings in the range
			for (int x = xFrom; x <= xTo; x++)
			{
				var point = new SpectraPoint();
				point.PixelNo = x;
			    point.RawSignalPixelCount = 0;

				for (int z = -halfWidth; z <= halfWidth; z++)
				{
					PointF p = m_Mapper.GetSourceCoords(x, p1.Y +z);
					int xx = (int)Math.Round(p.X);
					int yy = (int)Math.Round(p.Y);

				    if (m_SourceVideoFrame.Contains(xx, yy))
				    {
				        float sum = 0;
				        int numPoints = 0;
				        for (float kx = -0.4f; kx < 0.5f; kx+=0.2f)
                        for (float ky = -0.4f; ky < 0.5f; ky += 0.2f)
                        {
                            p = m_Mapper.GetSourceCoords(x + kx, p1.Y + ky + z);
                            int xxx = (int)Math.Round(p.X);
                            int yyy = (int)Math.Round(p.Y);
                            if (m_SourceVideoFrame.Contains(xxx, yyy))
                            {
                                sum += (m_Image.Pixelmap[xxx, yyy] * m_PixelValueCoeff);
                                numPoints++;
                            }
                        }
					    float destPixVal = (sum/numPoints); 
                        point.RawValue += destPixVal;
				        point.RawSignalPixelCount++;
						rv.Pixels[x - xFrom, z + bgHalfWidth + bgGap + halfWidth - 1] = destPixVal;
				    }
				}

			    point.RawSignal = point.RawValue;
				rv.Points.Add(point);

				#region Reads background 
                if (bgMethod == PixelCombineMethod.Average)
				{
                    ReadAverageBackgroundForPixelIndex(halfWidth, bgHalfWidth, bgGap, x, p1.Y, x - xFrom);
				}
				else if (bgMethod == PixelCombineMethod.Median)
				{
                    ReadMedianBackgroundForPixelIndex(halfWidth, bgHalfWidth, bgGap, x, p1.Y, x - xFrom);
				}
				#endregion
			}

			// Apply background
            for (int i = 0; i < rv.Points.Count; i++)
            {
                SpectraPoint point = rv.Points[i];

                if (bgMethod == PixelCombineMethod.Average)
				{
					point.RawBackgroundPerPixel = GetAverageBackgroundValue(point.PixelNo, xFrom, xTo, bgHalfWidth);
				}
				else if (bgMethod == PixelCombineMethod.Median)
				{
					point.RawBackgroundPerPixel = GetMedianBackgroundValue(point.PixelNo, xFrom, xTo, bgHalfWidth);
				}

                for (int z = -halfWidth - bgGap - bgHalfWidth + 1; z < -halfWidth - bgGap; z++)
                    rv.Pixels[i, z + bgHalfWidth + bgGap + halfWidth - 1] = point.RawBackgroundPerPixel;

                for (int z = halfWidth + bgGap + 1; z < halfWidth + bgGap + bgHalfWidth + 1; z++)
                    rv.Pixels[i, z + bgHalfWidth + bgGap + halfWidth - 1] = point.RawBackgroundPerPixel;

				point.RawValue -= point.RawBackgroundPerPixel * point.RawSignalPixelCount;

                if (point.RawValue < 0 && !TangraConfig.Settings.Spectroscopy.AllowNegativeValues)
				    point.RawValue = 0;
			}

			rv.MaxSpectraValue = (uint)Math.Ceiling(rv.Points.Where(x => x.PixelNo > rv.ZeroOrderPixelNo + 20).Select(x => x.RawValue).Max());

			return rv;
		}

		private float GetMedianBackgroundValue(int pixelNo, int xFrom, int xTo, int horizontalSpan)
		{
			var allAreaBgPixels = new List<float>();
			int idxFrom = Math.Max(xFrom, pixelNo - horizontalSpan);
			int idxTo = Math.Min(xTo, pixelNo + horizontalSpan);

			for (int i = idxFrom; i <= idxTo; i++)
			{
				allAreaBgPixels.AddRange(m_BgValuesList[i - xFrom]);
			}

			allAreaBgPixels.Sort();

			return allAreaBgPixels.Count == 0 
				? 0 
				: allAreaBgPixels[allAreaBgPixels.Count / 2];
		}

		private float GetAverageBackgroundValue(int pixelNo, int xFrom, int xTo, int horizontalSpan)
		{
			int idxFrom = Math.Max(xFrom, pixelNo - horizontalSpan);
			int idxTo = Math.Min(xTo, pixelNo + horizontalSpan);
			float bgSum = 0;
			uint pixCount = 0;
			for (int i = idxFrom; i <= idxTo; i++)
			{
				bgSum += m_BgValues[i - xFrom];
				pixCount += m_BgPixelCount[i - xFrom];
			}
			return pixCount == 0 
				? 0 
				: bgSum / pixCount;
		}

        private void ReadMedianBackgroundForPixelIndex(int halfWidth, int bgHalfWidth, int bgGap, float x1, float y1, int index)
		{
			var allBgPixels = new List<float>();

            int x1int = (int) x1;

            for (int x = x1int - bgHalfWidth; x <= x1int + bgHalfWidth; x++)
            {
                for (int z = -bgHalfWidth - bgGap - halfWidth; z < -halfWidth - bgGap; z++)
                {
                    PointF p = m_Mapper.GetSourceCoords(x, y1 + z);
                    int xx = (int) Math.Round(p.X);
                    int yy = (int) Math.Round(p.Y);

                    if (m_SourceVideoFrame.Contains(xx, yy))
                    {
                        allBgPixels.Add((m_Image.Pixelmap[xx, yy]*m_PixelValueCoeff));
                    }
                }

                for (int z = halfWidth + bgGap + 1; z <= halfWidth + bgGap + bgHalfWidth; z++)
                {
                    PointF p = m_Mapper.GetSourceCoords(x, y1 + z);
                    int xx = (int) Math.Round(p.X);
                    int yy = (int) Math.Round(p.Y);

                    if (m_SourceVideoFrame.Contains(xx, yy))
                    {
                        allBgPixels.Add((m_Image.Pixelmap[xx, yy]*m_PixelValueCoeff));
                    }
                }
            }

            m_BgValuesList[index] = allBgPixels;
			m_BgPixelCount[index] = 1;
		}

        private void ReadAverageBackgroundForPixelIndex(int halfWidth, int bgHalfWidth, int bgGap, float x1, float y1, int index)
		{
			float bgValue = 0;
            uint bgPixelCount = 0;
            int x1int = (int) x1;

            for (int x = x1int - bgHalfWidth; x <= x1int + bgHalfWidth; x++)
            {
                for (int z = -bgHalfWidth - halfWidth - bgGap; z < -halfWidth - bgGap; z++)
                {
                    PointF p = m_Mapper.GetSourceCoords(x, y1 + z);
                    int xx = (int)Math.Round(p.X);
                    int yy = (int)Math.Round(p.Y);

                    if (m_SourceVideoFrame.Contains(xx, yy))
                    {
                        bgValue += (m_Image.Pixelmap[xx, yy] * m_PixelValueCoeff);
                        bgPixelCount++;
                    }
                }

                for (int z = halfWidth + bgGap + 1; z <= halfWidth + bgGap + bgHalfWidth; z++)
                {
                    PointF p = m_Mapper.GetSourceCoords(x, y1 + z);
                    int xx = (int)Math.Round(p.X);
                    int yy = (int)Math.Round(p.Y);

                    if (m_SourceVideoFrame.Contains(xx, yy))
                    {
                        bgValue += (m_Image.Pixelmap[xx, yy] * m_PixelValueCoeff);
                        bgPixelCount++;
                    }
                }
            }

			m_BgValues[index] = bgValue;
			m_BgPixelCount[index] = bgPixelCount;
		}
	}
}
