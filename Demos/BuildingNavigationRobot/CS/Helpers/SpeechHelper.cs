// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml.Controls;

namespace BuildingNavigationRobot
{
    class SpeechHelper
    {
        private static SpeechSynthesizer speechSynthesizer;
        private static MediaElement mediaElement;

        static SpeechHelper()
        {
            speechSynthesizer = new SpeechSynthesizer();
            mediaElement = new MediaElement();
        }

        public static async void Speak(string textToSpeech, VoiceInformation voice = null)
        {
            if (!string.IsNullOrEmpty(textToSpeech))
            {
                ConfigureVoice(voice);
                var speechStream = await speechSynthesizer.SynthesizeTextToStreamAsync(textToSpeech);
                await mediaElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        mediaElement.SetSource(speechStream, speechStream.ContentType);
                        mediaElement.Play();
                    });
            }
        }

        private static void ConfigureVoice(VoiceInformation voice)
        {
            if (voice != null)
            {
                speechSynthesizer.Voice = voice;
            }
            else
            {
                speechSynthesizer.Voice = SpeechSynthesizer.DefaultVoice;
                //speechSynthesizer.Voice = SpeechSynthesizer.AllVoices[1];
            }
        }
    }
}
