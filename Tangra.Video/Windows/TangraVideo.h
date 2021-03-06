#pragma once

#define VERSION_MAJOR 3
#define VERSION_MINOR 0
#define VERSION_REVISION 29

struct VideoFileInfo
{
	long Width;
	long Height;
	float FrameRate;
	long CountFrames;
	long FirstFrame;
	long BitmapImageSize;
	char EngineBuffer[16];
	char VideoFileTypeBuffer[32];
};



HRESULT TangraVideoEnumVideoEngines(char* videoEngines, int len);

HRESULT TangraVideoSetVideoEngine(int videoEngine);

HRESULT TangraVideoOpenFile(const char* fileName, VideoFileInfo* fileInfo);

HRESULT TangraVideoCloseFile();

HRESULT TangraVideoGetFrame(long frameNo, unsigned long* pixels, unsigned char* bitmapPixels, unsigned char* bitmapBytes);

HRESULT TangraVideoGetFramePixels(long frameNo, unsigned long* pixels);

HRESULT TangraVideoGetIntegratedFrame(long startFrameNo, long framesToIntegrate, bool isSlidingIntegration, bool isMedianAveraging, unsigned long* pixels, unsigned char* bitmapPixels, unsigned char* bitmapBytes);

HRESULT TangraCreateNewAviFile(LPCTSTR szFileName, long width, long height, int bpp, double fps);

HRESULT TangraAviFileAddFrame(long* pixels);

HRESULT TangraGetLastAviFileError(char* szErrorMessage, int len);

HRESULT TangraAviFileClose();