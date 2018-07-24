﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Adv;
using DirectShowLib.DES;
using Tangra.Helpers;
using Tangra.Model.VideoOperations;

namespace Tangra.Video.AstroDigitalVideo
{
    public static class AavFileHelper
    {
        public static bool CheckAndCorrectBadMaxPixelValue(string fileName, IVideoController videoController)
        {
            // FixOccuRecEveryFrameSavedInPeriod(fileName);

            try
            {
                byte aavVer;

                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                using (BinaryReader rdr = new BinaryReader(fs))
                {
                    int magic = rdr.ReadInt32();
                    if (magic != 0x46545346)
                        return false;

                    aavVer = rdr.ReadByte();
                }

                if (aavVer == 1)
                    return ProcessAAVv1File(fileName, videoController);
                else if (aavVer == 2)
                    return ProcessAAVv2File(fileName, videoController);
                else
                    return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(Model.Helpers.Extensions.GetFullStackTrace(ex));
                return false;
            }
        }

        private static bool ProcessAAVv1File(string fileName, IVideoController videoController)
        {
            Tuple<long, string> aavNormValTag;
            int expectedMaxPixelVal;

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (BinaryReader rdr = new BinaryReader(fs))
            {
                int magic = rdr.ReadInt32();
                byte aavVer = rdr.ReadByte();
                if (magic != 0x46545346 || aavVer != 1)
                {
                    // Not an AAVv1 File
                    return false;
                }

                fs.Seek(12, SeekOrigin.Current);

                long systemTableOffs = rdr.ReadInt64();
                long userTableOffs = rdr.ReadInt64();

                fs.Seek(systemTableOffs, SeekOrigin.Begin);
                var sysTags = ReadTagsVer1(rdr);

                fs.Seek(userTableOffs, SeekOrigin.Begin);
                var userTags = ReadTagsVer1(rdr);

                if (!sysTags.ContainsKey("BITPIX") ||
                    !sysTags.ContainsKey("EFFECTIVE-FRAME-RATE") ||
                    !sysTags.ContainsKey("NATIVE-FRAME-RATE") ||
                    !sysTags.ContainsKey("AAV16-NORMVAL"))
                {
                    return false;
                }

                int bitPix = int.Parse(sysTags["BITPIX"].Item2);
                if (bitPix == 8)
                {
                    // This is an 8-bit file and it is not affected by a MaxPixelValue issue.
                    return false;
                }

                double effectiveFrameRate = double.Parse(sysTags["EFFECTIVE-FRAME-RATE"].Item2, CultureInfo.InvariantCulture);
                double nativeFrameRate = double.Parse(sysTags["NATIVE-FRAME-RATE"].Item2, CultureInfo.InvariantCulture);

                expectedMaxPixelVal = (int)Math.Round(256 * nativeFrameRate / effectiveFrameRate);

                if (sysTags.ContainsKey("AAV16-NORMVAL"))
                {
                    aavNormValTag = sysTags["AAV16-NORMVAL"];
                }
                else if (userTags.ContainsKey("AAV16-NORMVAL"))
                {
                    aavNormValTag = userTags["AAV16-NORMVAL"];
                }
                else
                {
                    return false;
                }

                int maxPixelValue = int.Parse(aavNormValTag.Item2);

                if (expectedMaxPixelVal >= maxPixelValue)
                {
                    // The MaxPixelValue s correct for this file. Nothing to do
                    return false;
                }

                if (videoController.ShowMessageBox(
                    string.Format("This AAV file appears to have an incorrect MaxPixelValue which can result in incorrectly displayed object brightness. Press 'Yes' to fix the MaxPixelValue to the correct value of {0}.", expectedMaxPixelVal),
                    "Tangra",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.No)
                {
                    return false;
                }
            }

            string expectedStringValue = expectedMaxPixelVal.ToString();

            if (expectedStringValue.Length > aavNormValTag.Item2.Length)
            {
                videoController.ShowMessageBox(string.Format("Error changing MaxPixelValue from {0} to {1}", aavNormValTag.Item2, expectedMaxPixelVal), "Tangra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            expectedStringValue = expectedStringValue.PadLeft(aavNormValTag.Item2.Length, '0');

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Write))
            using (BinaryWriter wrt = new BinaryWriter(fs))
            {
                fs.Seek(aavNormValTag.Item1 + 1, SeekOrigin.Begin);
                wrt.Write(Encoding.UTF8.GetBytes(expectedStringValue));
            }

            return true;
        }

        private static bool ProcessAAVv2File(string fileName, IVideoController videoController)
        {
            Tuple<long, string> currMaxPixelValue;
            Tuple<long, string> currMaxPixelValueImgSec;
            int expectedMaxPixelVal;

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (BinaryReader rdr = new BinaryReader(fs))
            {
                int magic = rdr.ReadInt32();
                byte aavVer = rdr.ReadByte();
                if (magic != 0x46545346 || aavVer != 2)
                {
                    // Not an AAVv2 File
                    return false;
                }

                fs.Seek(12, SeekOrigin.Current);

                long sysMetaOffs = rdr.ReadInt64();

                long userMetaOffs = rdr.ReadInt64();

                fs.Seek(1, SeekOrigin.Current);

                string stream1 = ReadString(rdr);

                fs.Seek(24, SeekOrigin.Current);

                string stream2 = ReadString(rdr);
                fs.Seek(24, SeekOrigin.Current);

                rdr.ReadByte();
                string section1 = ReadString(rdr);
                long imgSecOffs = rdr.ReadInt64();

                if (stream1 != "MAIN" || stream2 != "CALIBRATION" || section1 != "IMAGE")
                {
                    // Not a standard AAVv2 File
                    return false;
                }

                fs.Seek(sysMetaOffs, SeekOrigin.Begin);
                var sysTags = ReadTags(rdr);

                fs.Seek(userMetaOffs, SeekOrigin.Begin);
                var userTags = ReadTags(rdr);
                foreach (var kvp in userTags)
                {
                    sysTags[kvp.Key] = kvp.Value;
                }

                fs.Seek(imgSecOffs + 10, SeekOrigin.Begin);
                byte numLayouts = rdr.ReadByte();
                for (int i = 0; i < numLayouts; i++)
                {
                    fs.Seek(3, SeekOrigin.Current);
                    int tagsCount = rdr.ReadByte();
                    ReadTags(rdr, tagsCount);
                }
                int imgTagsCnt = rdr.ReadByte();
                var imgTags = ReadTags(rdr, imgTagsCnt);

                if (!sysTags.ContainsKey("BITPIX") ||
                    !sysTags.ContainsKey("EFFECTIVE-FRAME-RATE") ||
                    !sysTags.ContainsKey("NATIVE-FRAME-RATE") ||
                    !sysTags.ContainsKey("AAV16-NORMVAL") ||
                    !imgTags.ContainsKey("IMAGE-MAX-PIXEL-VALUE"))
                {
                    return false;
                }

                int bitPix = int.Parse(sysTags["BITPIX"].Item2);
                if (bitPix == 8)
                {
                    // This is an 8-bit file and it is not affected by a MaxPixelValue issue.
                    return false;
                }

                double effectiveFrameRate = double.Parse(sysTags["EFFECTIVE-FRAME-RATE"].Item2, CultureInfo.InvariantCulture);
                double nativeFrameRate = double.Parse(sysTags["NATIVE-FRAME-RATE"].Item2, CultureInfo.InvariantCulture);

                expectedMaxPixelVal = (int)Math.Round(256 * nativeFrameRate / effectiveFrameRate);
                int maxPixelValue = int.Parse(sysTags["AAV16-NORMVAL"].Item2);
                int maxPixelValueImgSec = int.Parse(imgTags["IMAGE-MAX-PIXEL-VALUE"].Item2);

                if (expectedMaxPixelVal == maxPixelValue && expectedMaxPixelVal == maxPixelValueImgSec)
                {
                    // The MaxPixelValue s correct for this file. Nothing to do
                    return false;
                }

                if (videoController.ShowMessageBox(
                    string.Format("This AAV file appears to have an incorrect MaxPixelValue which can result in incorrectly displayed object brightness. Press 'Yes' to fix the MaxPixelValue to the correct value of {0}.", expectedMaxPixelVal),
                    "Tangra",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.No)
                {
                    return false;
                }

                currMaxPixelValue = sysTags["AAV16-NORMVAL"];
                currMaxPixelValueImgSec = imgTags["IMAGE-MAX-PIXEL-VALUE"];
            }

            string expectedStringValue = expectedMaxPixelVal.ToString();

            if (expectedStringValue.Length > currMaxPixelValue.Item2.Length || expectedStringValue.Length > currMaxPixelValueImgSec.Item2.Length)
            {
                videoController.ShowMessageBox(string.Format("Error changing MaxPixelValue from {0}|{1} to {2}", currMaxPixelValue.Item2, currMaxPixelValueImgSec.Item2, expectedMaxPixelVal), "Tangra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            expectedStringValue = expectedStringValue.PadLeft(currMaxPixelValue.Item2.Length, '0');

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Write))
            using (BinaryWriter wrt = new BinaryWriter(fs))
            {
                fs.Seek(currMaxPixelValue.Item1 + 2, SeekOrigin.Begin);
                wrt.Write(Encoding.UTF8.GetBytes(expectedStringValue));

                fs.Seek(currMaxPixelValueImgSec.Item1 + 2, SeekOrigin.Begin);
                wrt.Write(Encoding.UTF8.GetBytes(expectedStringValue));
            }

            return true;
        }

        private static Dictionary<string, Tuple<long, string>> ReadTags(BinaryReader rdr)
        {
            int numTags = rdr.ReadInt32();
            return ReadTags(rdr, numTags);
        }

        private static  Dictionary<string, Tuple<long, string>> ReadTags(BinaryReader rdr, int numTags)
        {
            var tags = new Dictionary<string, Tuple<long, string>>();
            for (int i = 0; i < numTags; i++)
            {
                string tagName = ReadString(rdr);

                long valPos = rdr.BaseStream.Position;
                string tagValue = ReadString(rdr);

                tags.Add(tagName, Tuple.Create(valPos, tagValue));
            }

            return tags;
        }

        private static string ReadString(BinaryReader rdr)
        {
            short len = rdr.ReadInt16();
            byte[] strBytes = rdr.ReadBytes(len);
            return Encoding.UTF8.GetString(strBytes);
        }

        private static Dictionary<string, Tuple<long, string>> ReadTagsVer1(BinaryReader rdr)
        {
            int numTags = rdr.ReadInt32();

            var tags = new Dictionary<string, Tuple<long, string>>();
            for (int i = 0; i < numTags; i++)
            {
                string tagName = ReadStringVer1(rdr);

                long valPos = rdr.BaseStream.Position;
                string tagValue = ReadStringVer1(rdr);

                tags.Add(tagName, Tuple.Create(valPos, tagValue));
            }

            return tags;
        }

        private static string ReadStringVer1(BinaryReader rdr)
        {
            byte len = rdr.ReadByte();
            byte[] strBytes = rdr.ReadBytes(len);
            return Encoding.UTF8.GetString(strBytes);
        }

        private static void FixOccuRecEveryFrameSavedInPeriod(string fileLocation)
        {
            string fileLocationOut = Path.GetFullPath(Path.GetDirectoryName(fileLocation) + @"\" + Path.GetFileName(fileLocation) + "-fixed.aav");

            int FRAME_WIDTH = 720;
            int FRAME_HEIGHT = 576;
            int FRAME_PIXELS = FRAME_WIDTH * FRAME_HEIGHT;
            int INTEGRATION = 2;
            int FIRST_FRAME_IN_PERIOD = 1;
            int FIRST_OSD_LINE = 536;
            int LAST_OSD_LINE = 617;
            List<uint> BAD_FRAMES = new List<uint>();

            var aaFile = new AdvFile2(fileLocation);
            var frame = new ushort[FRAME_PIXELS];
            List<ushort[]> calFrames = new List<ushort[]>();
            
            for (uint i = 0; i < aaFile.CalibrationSteamInfo.FrameCount; i++)
            {
                var calPix = aaFile.GetCalibrationFramePixels(i);
                for (int j = 0; j < FRAME_PIXELS; j++)
                    frame[j] = (ushort)calPix[j];
                calFrames.Add(frame.ToArray());
            }

            for (int j = 0; j < FRAME_PIXELS; j++)
            {
                frame[j] = 0;
            }

            int totalIntegratedFrames = 0;
            string tmpBuffFileName = Path.GetTempFileName();
            using (var tmpFile = File.Create(tmpBuffFileName))
            using (var bw = new BinaryWriter(tmpFile))
            {
                uint[] pixels = null;
                for (uint i = 1; i < aaFile.MainSteamInfo.FrameCount; i++)
                {
                    if (!BAD_FRAMES.Contains(i))
                    {
                        pixels = aaFile.GetMainFramePixels(i);
                    }

                    for (int j = 0; j < FRAME_PIXELS; j++)
                    {
                        if (j < FIRST_OSD_LINE * FRAME_WIDTH)
                        {
                            frame[j] += (ushort)pixels[j];
                        }
                        else
                        {
                            frame[j] = (ushort)pixels[j];
                        }

                    }
                    if ((i - (FIRST_FRAME_IN_PERIOD + INTEGRATION - 1)) % INTEGRATION == 0)
                    {
                        for (int j = 0; j < frame.Length; j++)
                        {
                            bw.Write(frame[j]);
                        }

                        totalIntegratedFrames++;
                        for (int j = 0; j < FRAME_PIXELS; j++)
                        {
                            frame[j] = 0;
                        }
                    }
                }
            }


            var recorder = new AdvRecorder();
            recorder.ImageConfig.SetImageParameters(
                (ushort)aaFile.Width,
                (ushort)aaFile.Height,
                16,
                aaFile.MaxPixelValue);

            recorder.FileMetaData.RecorderSoftwareName = "Tangra";
            recorder.FileMetaData.RecorderSoftwareVersion = VersionHelper.AssemblyVersion;
            recorder.FileMetaData.CameraModel = "Unknown";
            recorder.FileMetaData.CameraSensorInfo = "Unknown";

            var frameRate = 25.00;
            recorder.FileMetaData.NativeFrameRate = frameRate;
            recorder.FileMetaData.EffectiveFrameRate = 25.0 / INTEGRATION;

            var nativeStandards = string.Empty;
            if (Math.Abs(frameRate - 25.0) < 0.1)
                nativeStandards = "PAL";
            else if (Math.Abs(frameRate - 29.97) < 0.1)
                nativeStandards = "NTSC";
            recorder.FileMetaData.AddUserTag("NATIVE-VIDEO-STANDARD", nativeStandards);
            recorder.FileMetaData.AddUserTag("FRAME-COMBINING", "Binning");
            recorder.FileMetaData.AddUserTag("OSD-FIRST-LINE", FIRST_OSD_LINE.ToString());
            recorder.FileMetaData.AddUserTag("OSD-LAST-LINE", LAST_OSD_LINE.ToString());
            recorder.FileMetaData.AddUserTag("AAV-VERSION", "2");
            recorder.FileMetaData.AddUserTag("AAV16-NORMVAL", (256 * INTEGRATION).ToString());

            recorder.FileMetaData.AddCalibrationStreamTag("TYPE", "VTI-OSD-CALIBRATION");
            recorder.FileMetaData.AddUserTag("OSD-FIRST-LINE", FIRST_OSD_LINE.ToString());
            recorder.FileMetaData.AddUserTag("OSD-LAST-LINE", LAST_OSD_LINE.ToString());

            recorder.StatusSectionConfig.RecordSystemErrors = true;
            recorder.StatusSectionConfig.AddDefineTag("FRAME-TYPE", Adv2TagType.UTF8String);
            recorder.StatusSectionConfig.AddDefineTag("FRAMES-IN-INTERVAL", Adv2TagType.Int8);
            recorder.StatusSectionConfig.AddDefineTag("NOISE-SIGNATURES", Adv2TagType.UTF8String);
            recorder.StatusSectionConfig.AddDefineTag("ORIGINAL-FRAME-ID", Adv2TagType.Int32);
            recorder.StatusSectionConfig.AddDefineTag("IntegratedFrames", Adv2TagType.Int8);

            recorder.StartRecordingNewFile(fileLocationOut, 0, true);

            int calFrameId = 0;
            foreach (var calFrame in calFrames)
            {
                recorder.AddCalibrationFrame(calFrame, true,
                    PreferredCompression.Lagarith16,
                    new AdvRecorder.AdvStatusEntry() { AdditionalStatusTags = new[] { "VTI-OSD-CALIBRATION", (object)(byte)0, string.Empty, (object)calFrameId, (object)(byte)0 } },
                    Adv.AdvImageData.PixelDepth16Bit);
                calFrameId++;
            }

            using (var tmpFile = File.OpenRead(tmpBuffFileName))
            using (var rdr = new BinaryReader(tmpFile))
            {
                for (int i = 0; i < totalIntegratedFrames; i++)
                {
                    for (int j = 0; j < frame.Length; j++)
                    {
                        frame[j] = rdr.ReadUInt16();
                    }

                    recorder.AddVideoFrame(frame, true,
                        PreferredCompression.Lagarith16,
                        new AdvRecorder.AdvStatusEntry()
                        {
                            SystemErrors = "",
                            AdditionalStatusTags = new[] { "DATA", (object)(byte)8, string.Empty, (object)(int)0, (object)(byte)8 }
                        },
                        Adv.AdvImageData.PixelDepth16Bit);
                }
            }

            File.Delete(tmpBuffFileName);

            recorder.FinishRecording();
        }
    }
}
