using System;

namespace Keg.DAL
{
    public class FlowControlChangedEventArgs : EventArgs
    {
        public Boolean Flowing { get; set; }
        public FlowControlChangedEventArgs(Boolean flowing)
        {
            Flowing = flowing;
        }
    }
}