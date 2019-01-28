#pragma once

#include <collection.h>
#include <ppltasks.h>
#include <Winstring.h>
#include <Unknwn.h>

#include <Mfapi.h>
#include <Mfmediaengine.h>
#include <Audioclient.h>
#include <mfidl.h>

#include "MediaState.h"

using namespace Microsoft::WRL;

#define CHECK_INIT(hr) if (E_NOT_VALID_STATE == hr) { throw ref new Exception(hr, "Media Engine initialization must have completed before subsequent operations can be performed");}
#define CHR(chr) hr = chr; if (FAILED(hr)) { goto End;}
#define CP(pointer) if ((void*)pointer == nullptr) {hr = E_INVALIDARG; goto End;}