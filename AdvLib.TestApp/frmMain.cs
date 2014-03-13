﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AdvLibTestApp
{
	public partial class frmMain : Form
	{
		public frmMain()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			AdvRecorder recorder = new AdvRecorder();

			// First set the values of the standard file metadata
			recorder.FileMetaData.RecorderName = "Genika";
			recorder.FileMetaData.RecorderVersion = "x.y.z";
			recorder.FileMetaData.RecorderTimerFirmwareVersion = "a.b.c";

			recorder.FileMetaData.CameraModel = "Flea3 FL3-FW-03S3M";
			recorder.FileMetaData.CameraSerialNumber = "10210906";
			recorder.FileMetaData.CameraVendorNumber = "Point Grey Research";
			recorder.FileMetaData.CameraSensorInfo = "Sony ICX414AL (1/2\" 648x488 CCD)";
			recorder.FileMetaData.CameraSensorResolution = "648x488";
			recorder.FileMetaData.CameraFirmwareVersion = "1.22.3.0";
			recorder.FileMetaData.CameraFirmwareBuildTime = "Mon Dec 28 20:15:45 2009";
			recorder.FileMetaData.CameraDriverVersion = "2.2.1.6";

			// Then define additional metadata, if required
			recorder.FileMetaData.AddUserTag("TELESCOPE-NAME", "Large Telescope");
			recorder.FileMetaData.AddUserTag("TELESCOPE-FL", "8300");
			recorder.FileMetaData.AddUserTag("TELESCOPE-FD", "6.5");
			recorder.FileMetaData.AddUserTag("CAMERA-DIGITAL-SAMPLIG", "xxx");
			recorder.FileMetaData.AddUserTag("CAMERA-HDR-RESPONSE", "yyy");
			recorder.FileMetaData.AddUserTag("CAMERA-OPTICAL-RESOLUTION", "zzz");

			// Define the image size and bit depth
			recorder.ImageConfig.SetImageParameters(640, 480, 12);

			// By default no status section values will be recorded. The user must enable the ones they need recorded and 
			// can also define additional status parameters to be recorded with each video frame
			recorder.StatusSectionConfig.RecordGain = true;
			recorder.StatusSectionConfig.RecordGamma = true;
			int customTagId = recorder.StatusSectionConfig.AddDefineTag("EXAMPLE-MESSAGES", AdvTagType.List16OfAnsiString255);

			recorder.StartRecordingNewFile(@"C:\Filename.adv");

			AdvStatusEntry status = new AdvStatusEntry();
			status.AdditionalStatusTags = new object[1];

			int imagesCount = GetTotalImages();

			for (int i = 0; i < imagesCount; i++)
			{
				// NOTE: Moking up some test data
				uint exposure = GetCurrentImageExposure(i);
				DateTime timestamp = GetCurrentImageTimeStamp(i);
				status.Gain = GetCurrentImageGain(i);
				status.Gamma = GetCurrentImageGamma(i);
				status.AdditionalStatusTags[customTagId] = GetCurrentExampleMassages(i);

				byte[] imageBytes = GetCurrentImageBytes(i);

				recorder.AddVideoFrame(
					imageBytes,

					// NOTE: Use with caution! Using compression is slower and may not work at high frame rates 
					// i.e. it may take longer to compress that data than for the next image to arrive on the buffer
					true, 

					AdvTimeStamp.FromDateTime(timestamp),
					exposure, 
					status);
			}

			recorder.StopRecording();
		}

		private int GetTotalImages()
		{
			// TODO: In this file conversion example, return the number of images to be recorded
			return 1;
		}

		private uint GetCurrentImageExposure(int frameId)
		{
			// TODO: Get the image exposure in 1/10-th of milliseconds
			return 400;
		}

		private DateTime GetCurrentImageTimeStamp(int frameId)
		{
			// TODO: Get the image timestamp. Alternatevly return windows Ticks or year/month/day/hour/min/sec/milliseconds
			return DateTime.Now;
		}

		private float GetCurrentImageGamma(int frameId)
		{
			// TODO: Get the image gamma
			return 1.0f;
		}

		private float GetCurrentImageGain(int frameId)
		{
			// TODO: Get the image gain in dB
			return 36.0f;
		}

		private string[] GetCurrentExampleMassages(int frameId)
		{
			// TODO: Get the image custom defined "EXAMPLE-MESSAGES" value.
			return new string[] { "Message 1", "Message 2", "Message 3" }; ;
		}
		

		private byte[] GetCurrentImageBytes(int frameId)
		{
			// NOTE: In this TEST example we mock up 12 bit pixels (640, 480), where 
			
			byte[] pixels = new byte[640 * 480 * 2];

			// Background values are all half way 0x0FFF / 2 = 0x07FF
			for (int i = 0; i < pixels.Length / 2; i++)
			{
				pixels[2 * i] = 0xFF;
				pixels[2 * i + 1] = 0x07;
			}

			// There is a pixel wide line from top left - down and right with full intensity (0x0FFF)
			for (int x = 0; x < 480; x++)
			{
				pixels[2 * (x * 640 + x)] = 0xFF;
				pixels[2 * (x * 640 + x) + 1] = 0x0F;				
			}

			// There is a pixel wide line from top right - down and left with zero intensity (0x0000)
			for (int x = 0; x < 480; x++)
			{
				pixels[2* ((x + 1) * 640 - x - 1)] = 0x00;
				pixels[2 * ((x + 1) * 640 - x - 1) + 1] = 0x00;
			}

			return pixels;
		}
	}
}
