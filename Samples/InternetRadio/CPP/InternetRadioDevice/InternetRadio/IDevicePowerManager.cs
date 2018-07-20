using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetRadio
{
    internal enum PowerState
    {
        Standby,
        Powered
    }

    internal struct PowerStateChangedEventArgs
    {
        public PowerState PowerState;
    }

    delegate void PowerStateChangedEventHandler(object sender, PowerStateChangedEventArgs e);

    interface IDevicePowerManager
    {
        event PowerStateChangedEventHandler PowerStateChanged;

        PowerState PowerState
        {
            get;
            set;
        }

        bool CanPerformActions();
    }
}
