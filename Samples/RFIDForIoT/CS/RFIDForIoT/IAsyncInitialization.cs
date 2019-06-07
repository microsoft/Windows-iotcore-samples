using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFIDForIoT
{
    public interface IAsyncInitialization
    {
        Task Initialization { get; }
    }
}
