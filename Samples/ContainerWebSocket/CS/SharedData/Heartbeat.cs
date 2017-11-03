using System.Runtime.Serialization;

namespace SharedData
{
    [DataContract]
    public class Heartbeat : IMessage
    {
        public Heartbeat()
        {
            // nothing to do
        }

        [DataMember]
        MessageEnum IMessage.MessageType { get; set; } = MessageEnum.Heartbeat;
    }
}
