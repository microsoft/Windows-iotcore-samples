using System;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml.Controls;

namespace FacialRecognitionDoor.Helpers
{
    /// <summary>
    /// Utilizes SpeechSynthesizer to convert text to an audio message played through a XAML MediaElement
    /// </summary>
    class SpeechHelper : IDisposable
    {
        private MediaElement mediaElement;
        private SpeechSynthesizer synthesizer;

        /// <summary>
        /// Accepts a MediaElement that should be placed on whichever page user is on when text is read by SpeechHelper.
        /// Initializes SpeechSynthesizer.
        /// </summary>
        public SpeechHelper(MediaElement media)
        {
            mediaElement = media;
            synthesizer = new SpeechSynthesizer();
        }

        /// <summary>
        /// Synthesizes passed through text as audio and plays speech through the MediaElement first sent through.
        /// </summary>
        public async Task Read(string text)
        {
            if (mediaElement != null && synthesizer != null)
            {
                var stream = await synthesizer.SynthesizeTextToStreamAsync(text);
                mediaElement.AutoPlay = true;
                mediaElement.SetSource(stream, stream.ContentType);
                mediaElement.Play();
            }
        }

        /// <summary>
        /// Disposes of IDisposable type SpeechSynthesizer
        /// </summary>
        public void Dispose()
        {
            synthesizer.Dispose();
        }
    }
}
