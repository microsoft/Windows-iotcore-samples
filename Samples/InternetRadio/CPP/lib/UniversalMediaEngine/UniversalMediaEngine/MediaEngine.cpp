#include "pch.h"
#include "MediaEngineManager.h"
#include "MediaEngine.h"

using namespace Microsoft::Maker::Media::UniversalMediaEngine;
using namespace Platform;
using namespace concurrency;

MediaEngine::~MediaEngine()
{
	if (nullptr != spMediaEngineManager.Get())
	{
		spMediaEngineManager->TearDown();
	}
}

IAsyncOperation<MediaEngineInitializationResult>^ MediaEngine::InitializeAsync()
{
	return create_async([this]
	{
		spMediaEngineManager = new MediaEngineManager(this);
		if (spMediaEngineManager.Get() != nullptr)
		{
			HRESULT hr = spMediaEngineManager->Initialize();
			if (SUCCEEDED(hr))
			{
				return MediaEngineInitializationResult::Success;
			}
		}

		return MediaEngineInitializationResult::Fail;
	});
}

void MediaEngine::PlayStream(IRandomAccessStream^ stream)
{
	HRESULT hr = E_FAIL;
	ComPtr<IMFByteStream> spMFByteStream = nullptr;
	ComPtr<IUnknown> pStreamUnk = reinterpret_cast<IUnknown*>(stream);

	CHR(MFCreateMFByteStreamOnStreamEx(pStreamUnk.Get(), &spMFByteStream));
	CHR(spMediaEngineManager->PlayMfByteStream(spMFByteStream.Get()));

End:

	if (S_OK != hr)
		throw ref new Exception(hr, "Invalid Stream");

	return;
}

void MediaEngine::Play(Platform::String^ url)
{
	HRESULT hr = E_FAIL;
	BSTR bstrUrl;

	bstrUrl = SysAllocString(url->Data());

	hr = spMediaEngineManager->PlayUrl(bstrUrl);

	SysFreeString(bstrUrl);

	// Check the HR at the end so we know the string is freed before throw
	CHECK_INIT(hr);
}

void MediaEngine::Pause()
{
	CHECK_INIT(spMediaEngineManager->Pause());
}

void MediaEngine::Stop()
{
	CHECK_INIT(spMediaEngineManager->Stop());
}

double MediaEngine::Volume::get()
{
	double volume;
	CHECK_INIT(spMediaEngineManager->GetVolume(&volume));
	return volume;
}

void MediaEngine::Volume::set(double value)
{
	CHECK_INIT(spMediaEngineManager->SetVolume(value));
}

void MediaEngine::TriggerMediaStateChanged(MediaState state)
{
	MediaStateChanged(state);
}