#include "pch.h"
#include "MediaEngine.h"
#include "MediaEngineManager.h"

using namespace Platform;
using namespace Microsoft::Maker::Media::UniversalMediaEngine;

MediaEngineManager::MediaEngineManager(MediaEngine^ mediaEngine)
{
	mediaEngineComponent = mediaEngine;
}

HRESULT MediaEngineManager::QueryInterface(REFIID riid,
	LPVOID * ppvObj)
{
	// Always set out parameter to NULL, validating it first.
	if (!ppvObj)
		return E_INVALIDARG;
	*ppvObj = NULL;
	if (riid == IID_IUnknown || riid == __uuidof(IMFMediaEngineNotify))
	{
		// Increment the reference count and return the pointer.
		*ppvObj = (LPVOID)this;
		AddRef();
		return NOERROR;
	}
	return E_NOINTERFACE;
}

ULONG MediaEngineManager::AddRef()
{
	InterlockedIncrement(&m_cRef);
	return m_cRef;
}
ULONG MediaEngineManager::Release()
{
	// Decrement the object's internal counter.
	ULONG ulRefCount = InterlockedDecrement(&m_cRef);
	if (0 == m_cRef)
	{
		delete this;
	}
	return ulRefCount;
}

HRESULT MediaEngineManager::Initialize()
{
	HRESULT hr = E_FAIL;

	IMFMediaEngineClassFactory* mediaEngineFactory;
	IMFAttributes* mediaEngineAttributes;

	// If we are already initialized 
	// then don't initialize again
	if (isInitialized)
		return S_FALSE;

	// Startup the media foundation subsystem
	CHR(MFStartup(MF_VERSION));

	CHR(CoCreateInstance(CLSID_MFMediaEngineClassFactory, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&mediaEngineFactory)));

	CHR(MFCreateAttributes(&mediaEngineAttributes, 1));

	CHR(mediaEngineAttributes->SetUnknown(MF_MEDIA_ENGINE_CALLBACK, this));

	// We only support audio since this component is designed for use in headless applications
	CHR(mediaEngineAttributes->SetUINT32(MF_MEDIA_ENGINE_AUDIO_CATEGORY, AUDIO_STREAM_CATEGORY::AudioCategory_Media));

	auto flags = MF_MEDIA_ENGINE_AUDIOONLY | MF_MEDIA_ENGINE_REAL_TIME_MODE;

	CHR(mediaEngineFactory->CreateInstance(flags, mediaEngineAttributes, &spMediaEngine));

	CHR(spMediaEngine->SetAutoPlay(true));

	isInitialized = true;

End:
	return hr;
}

HRESULT MediaEngineManager::TearDown()
{
	HRESULT hr = E_FAIL;

	// If we are already initialized 
	// then don't initialize again
	if (!isInitialized)
		return S_FALSE;

	// release the managers reference to the parent object 
	mediaEngineComponent = nullptr;

	// Shutdown the media foundation subsystem
	CHR(MFShutdown());

	isInitialized = false;

End:
	return hr;
}

HRESULT MediaEngineManager::PlayUrl(BSTR url)
{
	HRESULT hr = E_FAIL;
	CHR(checkInitialized());

	CHR(spMediaEngine->SetSource(url));
	CHR(spMediaEngine->Load());

End:
	return hr;
}

HRESULT MediaEngineManager::PlayMfByteStream(IMFByteStream* mfByteStream)
{
	HRESULT hr = E_FAIL;
	BSTR url = nullptr;
	ComPtr<IMFMediaEngineEx> spMediaEngineEx = nullptr;

	CHR(spMediaEngine.As<IMFMediaEngineEx>(&spMediaEngineEx));
	url = SysAllocString(L"file://");
	CHR(spMediaEngineEx->SetSourceFromByteStream(mfByteStream, url));
	CHR(spMediaEngineEx->Play());

End:
	if (nullptr != url)
		SysFreeString(url);

	return hr;
}

HRESULT MediaEngineManager::Pause()
{
	HRESULT hr = E_FAIL;
	CHR(checkInitialized());

	CHR(spMediaEngine->Pause());

End:
	return hr;
}

HRESULT MediaEngineManager::Stop()
{
	HRESULT hr = E_FAIL;
	CHR(checkInitialized());

	CHR(spMediaEngine->Shutdown());

End:
	return hr;
}

HRESULT MediaEngineManager::GetVolume(double* pVolume)
{
	HRESULT hr = E_FAIL;
	CHR(checkInitialized());

	CP(pVolume);

	*pVolume = spMediaEngine->GetVolume();

End:
	return hr;
}

HRESULT MediaEngineManager::SetVolume(double volume)
{
	HRESULT hr = E_FAIL;
	CHR(checkInitialized());

	CHR(spMediaEngine->SetVolume(volume));

End:
	return hr;
}

HRESULT MediaEngineManager::EventNotify(DWORD event, DWORD_PTR param1, DWORD param2)
{
	switch (event)
	{
	case MF_MEDIA_ENGINE_EVENT_LOADSTART:
		mediaEngineComponent->TriggerMediaStateChanged(MediaState::Loading);
		break;
	case MF_MEDIA_ENGINE_EVENT_PLAYING:
		mediaEngineComponent->TriggerMediaStateChanged(MediaState::Playing);
		break;
	case MF_MEDIA_ENGINE_EVENT_ENDED:
		mediaEngineComponent->TriggerMediaStateChanged(MediaState::Ended);
		break;
	case MF_MEDIA_ENGINE_EVENT_ERROR:
		mediaEngineComponent->TriggerMediaStateChanged(MediaState::Error);
		break;
	}

	return S_OK;
}

HRESULT MediaEngineManager::checkInitialized()
{
	if (isInitialized)
	{
		return S_OK;
	}
	else
	{
		return E_NOT_VALID_STATE;
	}
}
