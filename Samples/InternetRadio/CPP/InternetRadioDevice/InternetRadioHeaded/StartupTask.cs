using InternetRadio;
using System;

namespace InternetRadioHeaded
{
    class StartupTask
    {
        internal static RadioManager s_radioManager;

        public static async void Start()
        {
            if (null == s_radioManager)
            {
                s_radioManager = new RadioManager();
                await s_radioManager.Initialize(InternetRadioConfig.GetDefault());
            }
        }

        public static async void stop()
        {
            await s_radioManager.Dispose();
        }
    }
}
