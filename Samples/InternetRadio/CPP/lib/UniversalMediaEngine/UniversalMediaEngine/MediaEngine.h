#pragma once

using namespace Windows::Foundation;
using namespace Windows::Storage::Streams;

// Prototype
class MediaEngineManager;

namespace Microsoft {
	namespace Maker {
		namespace Media {
			namespace UniversalMediaEngine
			{
				public enum class MediaEngineInitializationResult
				{
					Success,
					Fail
				};

				public delegate void MediaStateChangedHandler(MediaState state);

				public ref class MediaEngine sealed
				{
				private:
					~MediaEngine();
				public:

					/// <summary>
					/// Asynchronously initializes the media engine
					/// </summary>
					/// <returns> A task with a value that indicates whether Initialization was
					/// successful</returns>
					IAsyncOperation<MediaEngineInitializationResult>^ InitializeAsync();

					/// <summary>
					/// Plays the media stream at the given URL
					/// </summary>
					void Play(Platform::String^ url);

					void PlayStream(IRandomAccessStream^ stream);

					/// <summary>
					/// Pauses playback
					/// </summary>
					void Pause();

					/// <summary>
					/// Stops playback
					/// </summary>
					void Stop();

					property double Volume
					{
						/// <summary>
						/// Gets the current volume
						/// </summary>
						/// <returns>The current volume</returns>
						double get();

						/// <summary>
						/// Sets the current volume
						/// </summary>
						/// <param name="value">The new volume</param>
						void set(double value);
					}

					/// <summary>
					/// Gets the current volume
					/// </summary>
					event MediaStateChangedHandler^ MediaStateChanged;

				internal:

					/// <summary>
					/// Used by internal components to trigger the MediaStateChanged event
					/// </summary>
					void TriggerMediaStateChanged(MediaState state);

				private:

					/// <summary>
					/// Used by internal components to trigger the MediaStateChanged event
					/// </summary>
					ComPtr<MediaEngineManager> spMediaEngineManager;
				};
			}
		}
	}
}
