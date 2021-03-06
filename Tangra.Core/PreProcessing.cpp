/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#include "cross_platform.h"
#include "PreProcessing.h"
#include "PixelMapUtils.h"
#include "HotPixelRemover.h"
#include "PolygonMask.h"

PreProcessingType s_PreProcessingType;
PreProcessingFilter s_PreProcessingFilter;
RotateFlipType g_RotateFlipType;
unsigned int  g_PreProcessingFromValue;
unsigned int  g_PreProcessingToValue;
int g_PreProcessingBrigtness;
int g_PreProcessingContrast;
float g_EncodingGamma;
int g_KnownCameraResponse;
int g_KnownCameraResponseParams[16];
bool g_UsesPreProcessing = false;

unsigned int g_HotPixelModel[49];
unsigned int g_HotPixelsXPos[200];
unsigned int g_HotPixelsYPos[200];
int g_HotPixelsPosCount;
unsigned int g_HotPixelsImageMedian;
unsigned int g_HotPixelsMaxPixelValue;

unsigned int g_MaskCornerXPos[200];
unsigned int g_MaskCornerYPos[200];
int g_MaskCornersCount;
unsigned int g_MaskCornerImageMedian;

float* g_DarkFramePixelsCopy = NULL;
float* g_FlatFramePixelsCopy = NULL;
float* g_BiasFramePixelsCopy = NULL;
float g_FlatFrameMedian = 0;
float g_DarkFrameExposure = 0;
bool g_DarkFrameIsBiasCorrected = false;
bool g_IsSameExposureDarkFrame = false;
unsigned int g_DarkFramePixelsCount = 0;
unsigned int g_FlatFramePixelsCount = 0;
unsigned int g_BiasFramePixelsCount = 0;

float ABS(float x)
{
	if (x < 0)
		return -x;
	return x;
}

bool UsesPreProcessing()
{
	return g_UsesPreProcessing;
}

int PreProcessingClearAll()
{
	s_PreProcessingType = pptpNone;
	g_RotateFlipType = RotateNoneFlipNone;
	g_PreProcessingFromValue = 0;
	g_PreProcessingToValue = 0;
	g_PreProcessingBrigtness = 0;
	g_PreProcessingContrast = 0;
	g_DarkFramePixelsCount = 0;
	g_FlatFramePixelsCount = 0;
	g_BiasFramePixelsCount = 0;
	g_HotPixelsPosCount = 0;
	g_UsesPreProcessing = false;

	s_PreProcessingFilter = ppfNoFilter;
	g_EncodingGamma = 1;
	g_KnownCameraResponse = 0;

	if (NULL != g_BiasFramePixelsCopy) {
		delete g_BiasFramePixelsCopy;
		g_BiasFramePixelsCopy = NULL;
	}

	if (NULL != g_DarkFramePixelsCopy) {
		delete g_DarkFramePixelsCopy;
		g_DarkFramePixelsCopy = NULL;
	}

	if (NULL != g_FlatFramePixelsCopy) {
		delete g_FlatFramePixelsCopy;
		g_FlatFramePixelsCopy = NULL;
	}

	g_FlatFrameMedian = 0;
	g_DarkFrameExposure = 0;
	g_DarkFrameIsBiasCorrected = false;
	g_IsSameExposureDarkFrame = false;

	return S_OK;
}

int PreProcessingUsesPreProcessing(bool* usesPreProcessing)
{
	*usesPreProcessing = g_UsesPreProcessing;

	return S_OK;
}

int PreProcessingGetConfig(PreProcessingType* preProcessingType, unsigned int* fromValue, unsigned int* toValue,int* brigtness,int* contrast, PreProcessingFilter* filter, float* gamma, int* reversedCameraResponse, unsigned int* darkPixelsCount, unsigned int* flatPixelsCount, unsigned int* biasPixelsCount, RotateFlipType* rotateFlipType, unsigned int* hotPixelsPosCount)
{
	if (g_UsesPreProcessing) {
		*preProcessingType = s_PreProcessingType;
		*rotateFlipType = g_RotateFlipType;
		*fromValue = g_PreProcessingFromValue;
		*toValue = g_PreProcessingToValue;
		*brigtness = g_PreProcessingBrigtness;
		*contrast = g_PreProcessingContrast;
		*darkPixelsCount = g_DarkFramePixelsCount;
		*flatPixelsCount = g_FlatFramePixelsCount;
		*biasPixelsCount = g_BiasFramePixelsCount;

		*filter = s_PreProcessingFilter;
		*gamma = g_EncodingGamma;
		*reversedCameraResponse = ABS(g_EncodingGamma - 1.0f) < 0.01 ? g_KnownCameraResponse : 0;
		
		*hotPixelsPosCount = g_HotPixelsPosCount;
	}

	return S_OK;
}

int PreProcessingAddStretching(unsigned int fromValue, unsigned int toValue)
{
	s_PreProcessingType = pptpStretching;
	g_PreProcessingFromValue = fromValue;
	g_PreProcessingToValue = toValue;
	g_UsesPreProcessing = true;

	return S_OK;
}

int PreProcessingAddClipping(unsigned int  fromValue, unsigned int  toValue)
{
	s_PreProcessingType = pptpClipping;
	g_PreProcessingFromValue = fromValue;
	g_PreProcessingToValue = toValue;
	g_UsesPreProcessing = true;

	return S_OK;
}

int PreProcessingAddBrightnessContrast(int brigtness, int contrast)
{
	s_PreProcessingType = pptpBrightnessContrast;
	g_PreProcessingBrigtness = brigtness;
	g_PreProcessingContrast = contrast;
	g_UsesPreProcessing = true;

	return S_OK;
}

int PreProcessingAddDigitalFilter(enum PreProcessingFilter filter)
{
	s_PreProcessingFilter = filter;
	g_UsesPreProcessing = true;

	return S_OK;
}

int PreProcessingAddGammaCorrection(float gamma)
{
	g_EncodingGamma = gamma;
	g_UsesPreProcessing = g_UsesPreProcessing || ABS(g_EncodingGamma - 1.0f) > 0.01;

	return S_OK;
}

int PreProcessingAddCameraResponseCorrection(int knownCameraResponse, int* responseParams)
{
	g_KnownCameraResponse = knownCameraResponse;
	g_UsesPreProcessing = g_UsesPreProcessing || g_KnownCameraResponse > 0;

	if (knownCameraResponse == 1)
	{
		g_KnownCameraResponseParams[0] = rand(); // Random number to correspond to the current param configuration being set (as)
		
		// WAT-910BD dual knee mode - 9 parameter
		g_KnownCameraResponseParams[1] = *responseParams;
		g_KnownCameraResponseParams[2] = *(responseParams + 1);
		g_KnownCameraResponseParams[3] = *(responseParams + 2);
		g_KnownCameraResponseParams[4] = *(responseParams + 3);
		g_KnownCameraResponseParams[5] = *(responseParams + 4);
		g_KnownCameraResponseParams[6] = *(responseParams + 5);
		g_KnownCameraResponseParams[7] = *(responseParams + 6);
		g_KnownCameraResponseParams[8] = *(responseParams + 7);
		g_KnownCameraResponseParams[9] = *(responseParams + 8);
	}
	return S_OK;
}

int PreProcessingAddFlipAndRotation(enum RotateFlipType rotateFlipType)
{
	g_RotateFlipType = rotateFlipType;
	g_UsesPreProcessing = true;

	return S_OK;
}

int PreProcessingAddDarkFrame(float* darkFramePixels, unsigned int pixelsCount, float exposureSeconds, bool isBiasCorrected, bool isSameExposure)
{
	if (NULL != g_DarkFramePixelsCopy) {
		delete g_DarkFramePixelsCopy;
		g_DarkFramePixelsCopy = NULL;
	}

	int bytesCount = pixelsCount * sizeof(float);

	g_DarkFramePixelsCopy = (float*)malloc(bytesCount);
	memcpy(g_DarkFramePixelsCopy, darkFramePixels, bytesCount);
	g_DarkFramePixelsCount = pixelsCount;
	g_DarkFrameExposure = exposureSeconds;
	g_DarkFrameIsBiasCorrected = isBiasCorrected;
	g_IsSameExposureDarkFrame = isSameExposure;

	if (!isBiasCorrected && isSameExposure) {
		// If we are loading a same exposure Dark frame that already has bias in it, then remove the currently loaded bias frame

		g_BiasFramePixelsCount = 0;

		if (NULL != g_BiasFramePixelsCopy) {
			delete g_BiasFramePixelsCopy;
			g_BiasFramePixelsCopy = NULL;
		}
	}

	g_UsesPreProcessing = true;

	return S_OK;
}

int PreProcessingAddBiasFrame(float* biasFramePixels, unsigned int pixelsCount)
{
	if (NULL != g_BiasFramePixelsCopy) {
		delete g_BiasFramePixelsCopy;
		g_BiasFramePixelsCopy = NULL;
	}

	int bytesCount = pixelsCount * sizeof(float);

	g_BiasFramePixelsCopy = (float*)malloc(bytesCount);
	memcpy(g_BiasFramePixelsCopy, biasFramePixels, bytesCount);
	g_BiasFramePixelsCount = pixelsCount;

	g_UsesPreProcessing = true;

	return S_OK;
}

int PreProcessingAddFlatFrame(float* flatFramePixels, unsigned int pixelsCount, float flatFrameMedian)
{
	if (NULL != g_FlatFramePixelsCopy) {
		delete g_FlatFramePixelsCopy;
		g_FlatFramePixelsCopy = NULL;
	}

	int bytesCount = pixelsCount * sizeof(float);

	g_FlatFramePixelsCopy = (float*)malloc(bytesCount);
	memcpy(g_FlatFramePixelsCopy, flatFramePixels, bytesCount);
	g_FlatFramePixelsCount = pixelsCount;
	g_FlatFrameMedian = flatFrameMedian;

	g_UsesPreProcessing = true;

	return S_OK;
}

int PreProcessingAddRemoveHotPixels(unsigned int* model, unsigned int count, unsigned int* xVals, unsigned int* yVals, unsigned int imageMedian, unsigned int maxPixelValue)
{
	g_UsesPreProcessing = true;

	for(int i = 0; i < 49; i++)
		g_HotPixelModel[i] = model[i];
	
	g_HotPixelsPosCount = count;
	g_HotPixelsImageMedian = imageMedian;
	g_HotPixelsMaxPixelValue = maxPixelValue;
	for (int i = 0; i < g_HotPixelsPosCount; i++)
	{
		g_HotPixelsXPos[i] = xVals[i];
		g_HotPixelsYPos[i] = yVals[i];
	}
	
	return S_OK;
}

int PreProcessingDefineMaskArea(int numPoints, unsigned int* xVals, unsigned int* yVals, unsigned int imageMedian)
{
	g_UsesPreProcessing = true;	
	
	g_MaskCornersCount = numPoints;
	g_MaskCornerImageMedian = imageMedian;
	for (int i = 0; i < g_MaskCornersCount; i++)
	{
		g_MaskCornerXPos[i] = xVals[i];
		g_MaskCornerYPos[i] = yVals[i];
	}
	
	return S_OK;
}

int ApplyPreProcessingWithNormalValue(unsigned int* originalPixels, unsigned int* pixels, int width, int height, int bpp, float exposureSeconds, unsigned int normVal, BYTE* bitmapPixels, BYTE* bitmapBytes)
{
	int rv = ApplyPreProcessingPixelsOnly(originalPixels, pixels, width, height, bpp, normVal, exposureSeconds);
	if (!SUCCEEDED(rv)) return rv;

	return GetBitmapPixels(width, height, pixels, bitmapPixels, bitmapBytes, false, bpp, normVal);
}

int ApplyPreProcessing(unsigned int* originalPixels, unsigned int* pixels, int width, int height, int bpp, float exposureSeconds, BYTE* bitmapPixels, BYTE* bitmapBytes)
{
	int rv = ApplyPreProcessingPixelsOnly(originalPixels, pixels, width, height, bpp, 0, exposureSeconds);
	if (!SUCCEEDED(rv)) return rv;

	return GetBitmapPixels(width, height, pixels, bitmapPixels, bitmapBytes, false, bpp, 0);
}

int ApplyPreProcessingPixelsOnly(unsigned int* originalPixels, unsigned int* pixels, int width, int height, int bpp, unsigned int normVal, float exposureSeconds)
{
	// To achieve correct photometry gamma needs to be applied before darks and flats. 
	// Use the following order when applying pre-processing
	// (1) Gamma
	// (2) Bias/Dark/Flat (Same gamma should have been applied when generating the Bias/Dark/Flat)
	// (3) Stretch/Clip/Brightness

	int rv = S_OK;

	if (ABS(g_EncodingGamma - 1.0f) > 0.01) {
		rv = PreProcessingGamma(pixels, width, height, bpp, normVal, g_EncodingGamma);
		if (rv != S_OK) return rv;
	}
	else if (g_KnownCameraResponse > 0)
	{
		rv = PreProcessingReverseCameraResponse(pixels, width, height, bpp, normVal, g_KnownCameraResponse, g_KnownCameraResponseParams);
		if (rv != S_OK) return rv;
	}
	
	if (NULL != g_BiasFramePixelsCopy || NULL != g_DarkFramePixelsCopy || NULL != g_FlatFramePixelsCopy) {
		rv = PreProcessingApplyBiasDarkFlatFrame(
		         pixels, width, height, bpp, normVal,
		         g_BiasFramePixelsCopy, g_DarkFramePixelsCopy, g_FlatFramePixelsCopy,
		         exposureSeconds, g_DarkFrameExposure, g_DarkFrameIsBiasCorrected, g_IsSameExposureDarkFrame, g_FlatFrameMedian);

		if (rv != S_OK) return rv;
	}

	if (g_RotateFlipType > RotateNoneFlipNone) {
		rv = PreProcessingFlipRotate(pixels, width, height, bpp, g_RotateFlipType);
		if (rv != S_OK) return rv;
	}

	if (s_PreProcessingType == pptpStretching) {
		rv = PreProcessingStretch(pixels, width, height, bpp, normVal, g_PreProcessingFromValue, g_PreProcessingToValue);
		if (rv != S_OK) return rv;
	} else if (s_PreProcessingType == pptpClipping) {
		rv = PreProcessingClip(pixels, width, height, bpp, normVal, g_PreProcessingFromValue, g_PreProcessingToValue);
		if (rv != S_OK) return rv;
	} else if (s_PreProcessingType == pptpBrightnessContrast) {
		rv = PreProcessingBrightnessContrast(pixels, width, height, bpp, normVal, g_PreProcessingBrigtness, g_PreProcessingContrast);
		if (rv != S_OK) return rv;
	}

	if (s_PreProcessingFilter == ppfLowPassFilter) {
		rv = PreProcessingLowPassFilter(pixels, width, height, bpp, normVal);
		if (rv != S_OK) return rv;
	} else if (s_PreProcessingFilter == ppfLowPassDifferenceFilter) {
		rv = PreProcessingLowPassDifferenceFilter(pixels, width, height, bpp, normVal);
		if (rv != S_OK) return rv;
	}
	
	if (g_MaskCornersCount > 0)
	{
		rv = PreProcessingMaskOutArea(pixels, width, height, g_MaskCornerImageMedian, g_MaskCornersCount, g_MaskCornerXPos, g_MaskCornerYPos);
		if (rv != S_OK) return rv;	

		// Area masking is applied to both original pixels and the pre-processed pixels copy
		rv = PreProcessingMaskOutArea(originalPixels, width, height, g_MaskCornerImageMedian, g_MaskCornersCount, g_MaskCornerXPos, g_MaskCornerYPos);
		if (rv != S_OK) return rv;
	}
	
	if (g_HotPixelsPosCount > 0)
	{
		rv = PreProcessingRemoveHotPixels(pixels, width, height, g_HotPixelModel, g_HotPixelsPosCount, g_HotPixelsXPos, g_HotPixelsYPos, g_HotPixelsImageMedian, g_HotPixelsMaxPixelValue);
		if (rv != S_OK) return rv;
	}
	
	return rv;
}

HRESULT SwapVideoFields(unsigned int* pixels, unsigned int* originalPixels, int width, int height, BYTE* bitmapPixels, BYTE* bitmapBytes)
{
	HRESULT rv = S_OK;

	unsigned int buffer[width];
	
	for(int k=0; k<height/2;k++) 
	{ 
		int y1 = 2 * k;
		int y2 = 2 * k + 1;
		memcpy(&buffer[0], &pixels[y1 * width], width * sizeof(unsigned int));
		memcpy(&pixels[y1 * width], &pixels[y2 * width], width * sizeof(unsigned int));
		memcpy(&pixels[y2 * width], &buffer[0], width * sizeof(unsigned int));
		
		memcpy(&buffer[0], &originalPixels[y1 * width], width * sizeof(unsigned int));
		memcpy(&originalPixels[y1 * width], &originalPixels[y2 * width], width * sizeof(unsigned int));
		memcpy(&originalPixels[y2 * width], &buffer[0], width * sizeof(unsigned int));		
	}
			
	return GetBitmapPixels(
			   width,
			   height,
			   pixels,
			   bitmapPixels,
			   bitmapBytes,
			   true, 8, 0);
}

HRESULT ShiftVideoFields(unsigned int* pixels, unsigned int* originalPixels, unsigned int* pixels2, unsigned int* originalPixels2, int width, int height, int fldIdx, BYTE* bitmapPixels, BYTE* bitmapBytes)
{
	HRESULT rv = S_OK;
	
	for(int k=0; k<height/2;k++) 
	{ 
		int yCpy = 2 * k + (fldIdx % 2);
		memcpy(&pixels[yCpy * width], &pixels2[yCpy * width], width * sizeof(unsigned int));	
		memcpy(&originalPixels[yCpy * width], &originalPixels2[yCpy * width], width * sizeof(unsigned int));
	}
	
	return GetBitmapPixels(
			   width,
			   height,
			   pixels,
			   bitmapPixels,
			   bitmapBytes,
			   true, 8, 0);	
}

