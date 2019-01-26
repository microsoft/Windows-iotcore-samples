#pragma once

using namespace Windows::Storage::Streams;

namespace Microsoft {
	namespace Maker {
		namespace Media {
			namespace UniversalMediaEngine
			{
				partial ref class MediaEngine;
			}
		}
	}
}

class MediaEngineNotify;

class MediaEngineManager : public IMFMediaEngineNotify
{
public:
	MediaEngineManager(Microsoft::Maker::Media::UniversalMediaEngine::MediaEngine^ mediaEngine);

	HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid,
		LPVOID * ppvObj);

	ULONG STDMETHODCALLTYPE AddRef();

	ULONG STDMETHODCALLTYPE Release();

	HRESULT Initialize();
	HRESULT TearDown();

	HRESULT PlayUrl(BSTR url);

	HRESULT PlayMfByteStream(IMFByteStream* mfByteStream);

	HRESULT Pause();

	HRESULT Stop();

	HRESULT GetVolume(double* pVolume);
	HRESULT SetVolume(double volume);

	HRESULT STDMETHODCALLTYPE EventNotify(
		DWORD     event,
		DWORD_PTR param1,
		DWORD     param2
		);

private:
	LONG m_cRef;
	bool isInitialized;
	ComPtr<IMFMediaEngine> spMediaEngine;
	Microsoft::Maker::Media::UniversalMediaEngine::MediaEngine^ mediaEngineComponent;

	HRESULT checkInitialized();
};


