﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

public class AdvFileMetaData
{
	internal Dictionary<string, string> UserMetaData = new Dictionary<string, string>(); 

	public string RecorderName { get; set; }
	public string RecorderVersion { get; set; }
	public string RecorderTimerFirmwareVersion { get; set; }

	public string CameraModel { get; set; }
	public string CameraSerialNumber { get; set; }
	public string CameraVendorNumber { get; set; }
	public string CameraSensorInfo { get; set; }
	public string CameraSensorResolution { get; set; }
	public string CameraFirmwareVersion { get; set; }
	public string CameraFirmwareBuildTime { get; set; }
	public string CameraDriverVersion { get; set; }

	public void AddUserTag(string tagName, string tagValue)
	{
		UserMetaData[tagName] = tagValue;
	}
}

public class AdvLocationData
{
	public string LongitudeWgs84 { get; set; }
	public string LatitudeWgs84 { get; set; }
	public string AltitudeMsl { get; set; }
	public string MslWgs84Offset { get; set; }
	public string GpsHdop { get; set; }
}

public class AdvImageConfig
{
	public ushort ImageWidth { get; private set; }
	public ushort ImageHeight { get; private set; }
	public byte CameraBitsPerPixel { get; private set; }
    public byte ImageBitsPerPixel { get; private set; }
	public bool ImageBigEndian { get; set; }

    /// <summary>
    /// Sets the image configuration
    /// </summary>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="cameraBitDepth">The native camera bith depth.</param>
    /// <param name="imageDynamicBitDepth">The bit depth of the dynamic range of the saved images.</param>
    public void SetImageParameters(ushort width, ushort height, byte cameraBitDepth, byte imageDynamicBitDepth)
	{
		ImageWidth = width;
		ImageHeight = height;
        CameraBitsPerPixel = cameraBitDepth;
        ImageBitsPerPixel = imageDynamicBitDepth;
	}	
}

public class AdvRecorder
{
	private uint m_TAGID_SystemTime;
	private uint m_TAGID_TrackedGPSSatellites;
	private uint m_TAGID_GPSAlmanacStatus;
	private uint m_TAGID_GPSAlmanacOffset;
	private uint m_TAGID_GPSFixStatus;
	private uint m_TAGID_Gain;
	private uint m_TAGID_Shutter;
	private uint m_TAGID_Offset;
	private uint m_TAGID_VideoCameraFrameId;
	private uint m_TAGID_Gamma;
	private uint m_TAGID_UserCommand;
	private uint m_TAGID_SystemError;

	private int m_NumberRecordedFrames = 0;
	private int m_NumberDroppedFrames = 0;
	private long m_FirstRecordedFrameTimestamp = 0;

	private const byte CFG_ADV_LAYOUT_1_UNCOMPRESSED = 1;
	private const byte CFG_ADV_LAYOUT_2_COMPRESSED = 2;
	private const byte CFG_ADV_LAYOUT_3_COMPRESSED = 3;

	private Dictionary<string, uint> m_AdditionalStatusSectionTagIds = new Dictionary<string, uint>();


	public AdvStatusSectionConfig StatusSectionConfig = new AdvStatusSectionConfig();
 
	public AdvRecorder()
	{
			
	}

	public AdvFileMetaData FileMetaData = new AdvFileMetaData();

	public AdvImageConfig ImageConfig = new AdvImageConfig();

	public AdvLocationData LocationData = new AdvLocationData();

	private string EnsureStringLength(string input)
	{
		if (input == null)
			return string.Empty;
		else if (input.Length > 255)
			return input.Substring(0, 255);
		else
			return input;
	}

	public void StartRecordingNewFile(string fileName)
	{
		AdvLib.AdvNewFile(fileName);

		if (string.IsNullOrEmpty(FileMetaData.RecorderName)) throw new ArgumentException("FileMetaData.RecorderName must be specified.");
		if (string.IsNullOrEmpty(FileMetaData.RecorderVersion)) throw new ArgumentException("FileMetaData.RecorderVersion must be specified.");
		if (string.IsNullOrEmpty(FileMetaData.CameraModel)) throw new ArgumentException("FileMetaData.CameraModel must be specified.");
		if (string.IsNullOrEmpty(FileMetaData.CameraSensorInfo)) throw new ArgumentException("FileMetaData.CameraSensorInfo must be specified.");

		AdvLib.AdvAddFileTag("RECORDER-SOFTWARE-VERSION", EnsureStringLength(FileMetaData.RecorderVersion));
		AdvLib.AdvAddFileTag("TIMER-FIRMWARE-VERSION", EnsureStringLength(FileMetaData.RecorderTimerFirmwareVersion));
		AdvLib.AdvAddFileTag("ADVLIB-VERSION", "1.0");

		AdvLib.AdvAddFileTag("RECORDER", EnsureStringLength(FileMetaData.RecorderName));
		AdvLib.AdvAddFileTag("FSTF-TYPE", "ADV");
		AdvLib.AdvAddFileTag("ADV-VERSION", "1");

		if (!string.IsNullOrEmpty(LocationData.LongitudeWgs84)) AdvLib.AdvAddFileTag("LONGITUDE-WGS84", LocationData.LongitudeWgs84);
		if (!string.IsNullOrEmpty(LocationData.LatitudeWgs84)) AdvLib.AdvAddFileTag("LATITUDE-WGS84", LocationData.LatitudeWgs84);
		if (!string.IsNullOrEmpty(LocationData.AltitudeMsl)) AdvLib.AdvAddFileTag("ALTITUDE-MSL", LocationData.AltitudeMsl);
		if (!string.IsNullOrEmpty(LocationData.MslWgs84Offset)) AdvLib.AdvAddFileTag("MSL-WGS84-OFFSET", LocationData.MslWgs84Offset);
		if (!string.IsNullOrEmpty(LocationData.GpsHdop)) AdvLib.AdvAddFileTag("GPS-HDOP", LocationData.GpsHdop);

		AdvLib.AdvAddFileTag("CAMERA-MODEL", EnsureStringLength(FileMetaData.CameraModel));
		AdvLib.AdvAddFileTag("CAMERA-SERIAL-NO", EnsureStringLength(FileMetaData.CameraSerialNumber));
		AdvLib.AdvAddFileTag("CAMERA-VENDOR-NAME", EnsureStringLength(FileMetaData.CameraVendorNumber));
		AdvLib.AdvAddFileTag("CAMERA-SENSOR-INFO", EnsureStringLength(FileMetaData.CameraSensorInfo));
		AdvLib.AdvAddFileTag("CAMERA-SENSOR-RESOLUTION", EnsureStringLength(FileMetaData.CameraSensorResolution));
		AdvLib.AdvAddFileTag("CAMERA-FIRMWARE-VERSION", EnsureStringLength(FileMetaData.CameraFirmwareVersion));
		AdvLib.AdvAddFileTag("CAMERA-FIRMWARE-BUILD-TIME", EnsureStringLength(FileMetaData.CameraFirmwareBuildTime));
		AdvLib.AdvAddFileTag("CAMERA-DRIVER-VERSION", EnsureStringLength(FileMetaData.CameraDriverVersion));

		foreach (string key in FileMetaData.UserMetaData.Keys)
		{
			AdvLib.AdvAddFileTag(EnsureStringLength(key), EnsureStringLength(FileMetaData.UserMetaData[key]));				
		}

        AdvLib.AdvDefineImageSection(ImageConfig.ImageWidth, ImageConfig.ImageHeight, ImageConfig.CameraBitsPerPixel);
		AdvLib.AdvAddOrUpdateImageSectionTag("IMAGE-BYTE-ORDER", ImageConfig.ImageBigEndian ? "BIG-ENDIAN" : "LITTLE-ENDIAN");
	    AdvLib.AdvAddOrUpdateImageSectionTag("IMAGE-DYNABITS", ImageConfig.ImageBitsPerPixel.ToString(CultureInfo.InvariantCulture));

		AdvLib.AdvDefineImageLayout(CFG_ADV_LAYOUT_1_UNCOMPRESSED, "FULL-IMAGE-RAW", "UNCOMPRESSED", 16, 0, null);
		AdvLib.AdvDefineImageLayout(CFG_ADV_LAYOUT_2_COMPRESSED, "FULL-IMAGE-DIFFERENTIAL-CODING", "QUICKLZ", 12, 32, "PREV-FRAME");
		AdvLib.AdvDefineImageLayout(CFG_ADV_LAYOUT_3_COMPRESSED, "FULL-IMAGE-RAW", "QUICKLZ", 16, 0, null);

		if (StatusSectionConfig.RecordSystemTime) m_TAGID_SystemTime = AdvLib.AdvDefineStatusSectionTag("SystemTime", AdvTagType.ULong64);

		if (StatusSectionConfig.RecordGPSTrackedSatellites) m_TAGID_TrackedGPSSatellites = AdvLib.AdvDefineStatusSectionTag("GPSTrackedSatellites", AdvTagType.UInt8);
		if (StatusSectionConfig.RecordGPSAlmanacStatus) m_TAGID_GPSAlmanacStatus = AdvLib.AdvDefineStatusSectionTag("GPSAlmanacStatus", AdvTagType.UInt8);
		if (StatusSectionConfig.RecordGPSAlmanacOffset) m_TAGID_GPSAlmanacOffset = AdvLib.AdvDefineStatusSectionTag("GPSAlmanacOffset", AdvTagType.UInt8);
		if (StatusSectionConfig.RecordGPSFixStatus) m_TAGID_GPSFixStatus = AdvLib.AdvDefineStatusSectionTag("GPSFixStatus", AdvTagType.UInt8);
		if (StatusSectionConfig.RecordGain) m_TAGID_Gain = AdvLib.AdvDefineStatusSectionTag("Gain", AdvTagType.Real);
		if (StatusSectionConfig.RecordShutter) m_TAGID_Shutter = AdvLib.AdvDefineStatusSectionTag("Shutter", AdvTagType.Real);
		if (StatusSectionConfig.RecordCameraOffset) m_TAGID_Offset = AdvLib.AdvDefineStatusSectionTag("Offset", AdvTagType.Real);
		if (StatusSectionConfig.RecordVideoCameraFrameId) m_TAGID_VideoCameraFrameId = AdvLib.AdvDefineStatusSectionTag("VideoCameraFrameId", AdvTagType.ULong64);
		if (StatusSectionConfig.RecordGamma) m_TAGID_Gamma = AdvLib.AdvDefineStatusSectionTag("Gamma", AdvTagType.Real);
		if (StatusSectionConfig.RecordUserCommands) m_TAGID_UserCommand = AdvLib.AdvDefineStatusSectionTag("UserCommand", AdvTagType.List16OfAnsiString255);
		if (StatusSectionConfig.RecordSystemErrors) m_TAGID_SystemError = AdvLib.AdvDefineStatusSectionTag("SystemError", AdvTagType.List16OfAnsiString255);

		m_AdditionalStatusSectionTagIds.Clear();

		if (StatusSectionConfig.AdditionalStatusTags.Count > 0)
		{
			foreach (string tagName in StatusSectionConfig.AdditionalStatusTags.Keys)
			{
				uint tagId = AdvLib.AdvDefineStatusSectionTag(tagName, StatusSectionConfig.AdditionalStatusTags[tagName]);
				m_AdditionalStatusSectionTagIds.Add(tagName, tagId);
			}
		}

		m_NumberRecordedFrames = 0;
		m_NumberDroppedFrames = 0;
		m_FirstRecordedFrameTimestamp = 0;
	}

	public void StopRecording()
	{
		AdvLib.AdvEndFile();
	}

	public void AddVideoFrame(byte[] pixels, bool compress, AdvTimeStamp timeStamp, uint exposureIn10thMilliseconds, AdvStatusEntry metadata)
	{
		BeginVideoFrame(timeStamp, exposureIn10thMilliseconds, metadata);

		byte layoutIdForCurrentFramerate = compress ? CFG_ADV_LAYOUT_3_COMPRESSED : CFG_ADV_LAYOUT_1_UNCOMPRESSED;

		AdvLib.AdvFrameAddImageBytes(layoutIdForCurrentFramerate, pixels, 16);

		AdvLib.AdvEndFrame();	
	}

	public void AddVideoFrame(ushort[] pixels, bool compress, AdvTimeStamp timeStamp, uint exposureIn10thMilliseconds, AdvStatusEntry metadata)
	{
		BeginVideoFrame(timeStamp, exposureIn10thMilliseconds, metadata);

		byte layoutIdForCurrentFramerate = compress ? CFG_ADV_LAYOUT_3_COMPRESSED : CFG_ADV_LAYOUT_1_UNCOMPRESSED;

		AdvLib.AdvFrameAddImage(layoutIdForCurrentFramerate, pixels, 16);

		AdvLib.AdvEndFrame();	
	}

	private void BeginVideoFrame( AdvTimeStamp timeStamp, uint exposureIn10thMilliseconds, AdvStatusEntry metadata)
	{
		long elapsedTimeMilliseconds = 0; // since the first recorded frame was taken
		if (m_NumberRecordedFrames > 0 && m_FirstRecordedFrameTimestamp != 0)
		{
			elapsedTimeMilliseconds = timeStamp.MillisecondsAfterAdvZeroEpoch - m_FirstRecordedFrameTimestamp;
		}
		else if (m_NumberRecordedFrames == 0)
		{
			m_FirstRecordedFrameTimestamp = timeStamp.MillisecondsAfterAdvZeroEpoch;
		}

		bool frameStartedOk = AdvLib.AdvBeginFrame(timeStamp.MillisecondsAfterAdvZeroEpoch, elapsedTimeMilliseconds > 0 ? (uint)elapsedTimeMilliseconds : 0, exposureIn10thMilliseconds);
		if (!frameStartedOk)
		{
			// If we can't add the first frame, this may be a file creation issue; otherwise increase the dropped frames counter
			if (m_NumberRecordedFrames > 0)
				m_NumberDroppedFrames++;
			return;
		}
	
		if (StatusSectionConfig.RecordSystemTime) AdvLib.AdvFrameAddStatusTag64(m_TAGID_SystemTime, metadata.SystemTime.MillisecondsAfterAdvZeroEpoch > 0 ? (ulong)metadata.SystemTime.MillisecondsAfterAdvZeroEpoch : 0);
	
		if (StatusSectionConfig.RecordGPSTrackedSatellites) AdvLib.AdvFrameAddStatusTagUInt8(m_TAGID_TrackedGPSSatellites, metadata.GPSTrackedSatellites);
		if (StatusSectionConfig.RecordGPSAlmanacStatus) AdvLib.AdvFrameAddStatusTagUInt8(m_TAGID_GPSAlmanacStatus, (byte)metadata.GPSAlmanacStatus);
		if (StatusSectionConfig.RecordGPSAlmanacOffset) AdvLib.AdvFrameAddStatusTagUInt8(m_TAGID_GPSAlmanacOffset, metadata.GPSAlmanacOffset);	
		if (StatusSectionConfig.RecordGPSFixStatus) AdvLib.AdvFrameAddStatusTagUInt8(m_TAGID_GPSFixStatus, (byte)metadata.GPSFixStatus);
		if (StatusSectionConfig.RecordGain) AdvLib.AdvFrameAddStatusTagReal(m_TAGID_Gain, metadata.Gain);
		if (StatusSectionConfig.RecordGamma) AdvLib.AdvFrameAddStatusTagReal(m_TAGID_Gamma, metadata.Gamma);
		if (StatusSectionConfig.RecordShutter) AdvLib.AdvFrameAddStatusTagReal(m_TAGID_Shutter, metadata.Shutter);
		if (StatusSectionConfig.RecordCameraOffset) AdvLib.AdvFrameAddStatusTagReal(m_TAGID_Offset, metadata.CameraOffset);
		if (StatusSectionConfig.RecordVideoCameraFrameId) AdvLib.AdvFrameAddStatusTag64(m_TAGID_VideoCameraFrameId, metadata.VideoCameraFrameId);

		if (StatusSectionConfig.RecordUserCommands && metadata.UserCommands != null)
		{
			for (int i = 0; i < Math.Min(16, metadata.UserCommands.Count()); i++)
			{
				if (metadata.UserCommands[i] != null)
				{
					if (metadata.UserCommands[i].Length > 255)
						AdvLib.AdvFrameAddStatusTagMessage(m_TAGID_UserCommand, metadata.UserCommands[i].Substring(0, 255));
					else
						AdvLib.AdvFrameAddStatusTagMessage(m_TAGID_UserCommand, metadata.UserCommands[i]);
				}
			}
		}

		if (StatusSectionConfig.RecordSystemErrors && metadata.SystemErrors != null)
		{
			for (int i = 0; i < Math.Min(16, metadata.SystemErrors.Count()); i++)
			{
				if (metadata.SystemErrors[i] != null)
				{
					if (metadata.SystemErrors[i].Length > 255)
						AdvLib.AdvFrameAddStatusTagMessage(m_TAGID_SystemError, metadata.SystemErrors[i].Substring(0, 255));
					else
						AdvLib.AdvFrameAddStatusTagMessage(m_TAGID_SystemError, metadata.SystemErrors[i]);
				}
			} 
		}

		int additionalStatusTagId = -1;
		foreach (string tagName in StatusSectionConfig.AdditionalStatusTags.Keys)
		{
			uint tagId = m_AdditionalStatusSectionTagIds[tagName];
			additionalStatusTagId++;
			object statusTagValue = metadata.AdditionalStatusTags[additionalStatusTagId];

			switch (StatusSectionConfig.AdditionalStatusTags[tagName])
			{
				case AdvTagType.UInt8:
					AdvLib.AdvFrameAddStatusTagUInt8(tagId, (byte)statusTagValue);
					break;

				case AdvTagType.UInt16:
					AdvLib.AdvFrameAddStatusTag16(tagId, (ushort)statusTagValue);
					break;

				case AdvTagType.UInt32:
					AdvLib.AdvFrameAddStatusTag32(tagId, (uint)statusTagValue);
					break;

				case AdvTagType.ULong64:
					AdvLib.AdvFrameAddStatusTag64(tagId, (ulong)statusTagValue);
					break;

				case AdvTagType.Real:
					AdvLib.AdvFrameAddStatusTagReal(tagId, (float)statusTagValue);
					break;

				case AdvTagType.AnsiString255:
					AdvLib.AdvFrameAddStatusTag(tagId, (string)statusTagValue);
					break;

				case AdvTagType.List16OfAnsiString255:
					string[] lines = (string[]) statusTagValue;
					for (int i = 0; i < Math.Min(16, lines.Count()); i++)
					{
						if (lines[i] != null)
						{
							if (lines[i].Length > 255)
								AdvLib.AdvFrameAddStatusTagMessage(tagId, lines[i].Substring(0, 255));
							else
								AdvLib.AdvFrameAddStatusTagMessage(tagId, lines[i]);
						}
					} 
					break;
			}
		}
	}
}