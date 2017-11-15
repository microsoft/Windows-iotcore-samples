using System;
using System.Runtime.Serialization;

namespace SharedData
{
    public enum LEDState : Int16
    {
        Off = 0,
        On
    }

    [DataContract]
    public class LEDMessage : IMessage
    {
        public LEDMessage(Int16 pin, LEDState state)
        {
            Pin = pin;
            State = state;
        }

        [DataMember]
        MessageEnum IMessage.MessageType { get; set;  } = MessageEnum.LED;

        [DataMember]
        public Int16 Pin { get; set; }

        [DataMember]
        public LEDState State { get; set; }
    }
}
