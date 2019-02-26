using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.System.Threading;

namespace Microsoft.Maker.Devices.TextDisplay
{
    internal partial class TextDisplayBase : ITextDisplay
    {
        private TextDisplayConfig config;
        private string currentMessage;
        private string tempMessage;
        private ThreadPoolTimer tempTimer;

        protected TextDisplayBase(TextDisplayConfig config)
        {
            this.config = config;
            this.Height = this.config.Height;
            this.Width = this.config.Width;
        }

        public uint Height
        {
            get;
        }

        public uint Width
        {
            get;
        }

        public IAsyncAction DisposeAsync()
        {
            return Task.Run(async () =>
            {
                if (tempTimer != null)
                {
                    tempTimer.Cancel();
                    tempTimer = null;
                }

                await this.DisposeInternal();
            }).AsAsyncAction();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected virtual async Task DisposeInternal() { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public IAsyncAction InitializeAsync()
        {
            return Task.Run(async () =>
            {
                await this.InitializeInternal(this.config.DriverConfigurationValues);
            }).AsAsyncAction();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected virtual async Task InitializeInternal(IDictionary<string, string> driverConfigurationValues) { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public IAsyncAction WriteMessageAsync(string message, uint timeout)
        {
            return Task.Run(async () =>
            {
                if (timeout == 0)
                {
                    currentMessage = message;
                    await this.WriteMessageInternal(currentMessage);
                }
                else
                {
                    if (tempTimer != null)
                    {
                        tempTimer.Cancel();
                        tempTimer = null;
                    }

                    tempMessage = message;
                    tempTimer = ThreadPoolTimer.CreateTimer(Timer_Tick, new TimeSpan(0, 0, (int)timeout));
                    await this.WriteMessageInternal(tempMessage);
                }

            }).AsAsyncAction();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected virtual async Task WriteMessageInternal(string message) { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        private async void Timer_Tick(ThreadPoolTimer timer)
        {
            tempTimer = null;
            await WriteMessageInternal(currentMessage);
        }
    }
}
