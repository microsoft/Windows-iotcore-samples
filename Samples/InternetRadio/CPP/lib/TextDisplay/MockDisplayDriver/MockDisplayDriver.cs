using Microsoft.Maker.Devices.TextDisplay;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace MockDisplayDriver
{
    public class MockDisplayDriver : ITextDisplay
    {
        public TextDisplayConfig Config;
        public string LastMessage;
        private uint height;
        public uint width;

        public MockDisplayDriver(TextDisplayConfig config)
        {
            this.Config = config;
            this.height = config.Height;
            this.width = config.Width;      
        }

        public uint Height
        {
            get
            {
                return this.height;
            }
        }

        public uint Width
        {
            get
            {
                return this.width;
            }
        }

        public IAsyncAction DisposeAsync()
        {
            return Task.Run(() =>
            {
            }).AsAsyncAction();
        }

        public IAsyncAction InitializeAsync()
        {
            return Task.Run(() =>
            {
            }).AsAsyncAction();
        }

        public IAsyncAction WriteMessageAsync(string message, uint timeout)
        {
            return Task.Run(() =>
            {
                this.LastMessage = message;
            }).AsAsyncAction();
        }
    }
}
