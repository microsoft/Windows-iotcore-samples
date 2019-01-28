using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.Maker.Devices.TextDisplay
{
    public interface ITextDisplayConfigProvider
    {
        IAsyncOperation<IEnumerable<TextDisplayConfig>> GetConfiguredDisplaysAsync();
    }
}
