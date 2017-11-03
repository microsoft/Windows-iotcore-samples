using System.Runtime.Serialization;

namespace SharedData
{
    [DataContract]
    public class Temperature : IMessage
    {
        public Temperature(double current)
        {
            Value = current;
        }

        [DataMember]
        MessageEnum IMessage.MessageType { get; set; } = MessageEnum.Temperature;

        [DataMember]
        public double Value { get; set; }
    }
}
