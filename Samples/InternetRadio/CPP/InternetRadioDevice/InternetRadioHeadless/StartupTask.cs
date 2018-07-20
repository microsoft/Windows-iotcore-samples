using Windows.ApplicationModel.Background;
using System;
using InternetRadio;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace InternetRadioHeadless
{
    public sealed class StartupTask : IBackgroundTask
    {
        internal static RadioManager s_radioManager;
        private BackgroundTaskDeferral deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            if (null == s_radioManager)
            {
                s_radioManager = new RadioManager();
                await s_radioManager.Initialize(InternetRadioConfig.GetDefault());
            }
        }

        private async void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            await s_radioManager.Dispose();

            deferral.Complete();
        }
    }
}
