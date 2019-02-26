using Microsoft.Maker.Devices.TextDisplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace TextDisplay.Tests
{
    class TestConfigProvider : ITextDisplayConfigProvider
    {
        public TestConfigProvider()
        {
            this.Configs = new List<TextDisplayConfig>();
        }

        public IEnumerable<TextDisplayConfig> Configs;

        public IAsyncOperation<IEnumerable<TextDisplayConfig>> GetConfiguredDisplaysAsync()
        {
            return Task.Run(() =>
            {
                return this.Configs;
            }).AsAsyncOperation();
        }
    }
}
