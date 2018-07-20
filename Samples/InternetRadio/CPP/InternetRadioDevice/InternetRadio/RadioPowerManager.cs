using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetRadio
{
    class RadioPowerManager : IDevicePowerManager
    {
        private PowerState powerState;
        public PowerState PowerState
        {
            get
            {
                return this.powerState;
            }

            set
            {
                if (value != this.powerState)
                {
                    this.powerState = value;
                    this.PowerStateChanged(this, new PowerStateChangedEventArgs() { PowerState = this.powerState });
                }
            }
        }

        public event PowerStateChangedEventHandler PowerStateChanged;

        public bool CanPerformActions() => this.PowerState == PowerState.Powered;
    }
}
