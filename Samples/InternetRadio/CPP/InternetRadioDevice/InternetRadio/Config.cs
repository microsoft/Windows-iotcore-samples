using System.Collections.Generic;

namespace InternetRadio
{
    public enum InputAction
    {
        NextChannel,
        PreviousChannel,
        VolumeUp,
        VolumeDown,
        Sleep,
        AddChannel,
        DeleteChannel
    }

    public sealed class InternetRadioConfig
    {
        InternetRadioConfig()
        {
            this.Buttons_Pins = new Dictionary<int, InputAction>();
        }

        public int Messages_StartupMessageDelay
        {
            get;
            set;
        }

        public int Buttons_Debounce
        {
            get;
            set;
        }

        public IDictionary<int, InputAction> Buttons_Pins
        {
            get;
            private set;
        }

        public Track Playlist_BuiltInStation
        {
            get;
            set;
        }

        public static InternetRadioConfig GetDefault()
        {
            return new InternetRadioConfig()
            {
                Messages_StartupMessageDelay = 3000,
                Buttons_Debounce = 1000,
                Buttons_Pins = new Dictionary<int, InputAction>()
                {
                    { 4, InputAction.Sleep },
                    { 16, InputAction.NextChannel },
                    { 27, InputAction.PreviousChannel },
                    { 12, InputAction.VolumeDown },
                    { 22, InputAction.VolumeUp }
                },
                Playlist_BuiltInStation = new Track()
                     {
                         Address = @"http://video.ch9.ms/ch9/debd/54ebcbdf-d688-43fc-97ef-cb83162bdebd/2-724.mp3",
                         Name = "CH 9: Build 2015 Presentation"
                     }
        };
        }
    }

}
