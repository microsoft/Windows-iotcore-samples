using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SharedData
{
    public enum MessageEnum : Int16
    {
        None = 0,
        Heartbeat,
        Temperature,
        LED
    }

    public interface IMessage
    {
        MessageEnum MessageType { get; set; }
    }

    static public class MessageHelper
    {
        private static byte[] buffer = new byte[1024];
        private static byte[] sendBuffer = new byte[1024];

        static public async Task SendInt16(WebSocket socket, Int16 value)
        {
            await Task.Yield();
            await socket.SendAsync(new ArraySegment<byte>(BitConverter.GetBytes(value)), WebSocketMessageType.Binary, false, CancellationToken.None);
        }

        static public async Task SendMessage(WebSocket socket, IMessage message)
        {
            byte[] buffer;
            int length = 0;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(message.GetType());
                ser.WriteObject(ms, message);
                buffer = ms.GetBuffer();
                length = (int)ms.Position;
            }

            if (socket.State == WebSocketState.Open)
            {
                await SendInt16(socket, (Int16)message.MessageType);
                await SendInt16(socket, (Int16)length);
                await Task.Yield();
                await socket.SendAsync(new ArraySegment<byte>(buffer, 0, length), WebSocketMessageType.Binary, false, CancellationToken.None);
            }
        }


        static public async Task<Int16> ReceiveInt16(WebSocket socket)
        {
            await ReceiveBuffer(socket, buffer, sizeof(Int16));
            Int16 result = BitConverter.ToInt16(buffer, 0);
            return result;
        }

        static public async Task ReceiveBuffer(WebSocket socket, byte[] buffer, int maxLength)
        {
            await Task.Yield();
            WebSocketReceiveResult receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, 0, maxLength), CancellationToken.None);
            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                throw new SharedDataException("WebSocketMessageType.Close");
            }
            else if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                await socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Expected WebSocketMessageType.Binary", CancellationToken.None);
                throw new SharedDataException("Expected WebSocketMessageType.Binary");
            }
        }

        static public async Task<IMessage> ReceiveMessage(WebSocket socket)
        {
            IMessage result = null;
            MessageEnum type = (MessageEnum)await ReceiveInt16(socket);
            int messageLength = await ReceiveInt16(socket);
            if (messageLength > buffer.Length)
            {
                throw new SharedDataException($"message length[{messageLength} is greater than buffer.Length[{buffer.Length}]");
            }
            await ReceiveBuffer(socket, buffer, buffer.Length);

            using (MemoryStream ms = new MemoryStream(buffer, 0, messageLength))
            {
                switch (type)
                {
                    case MessageEnum.Heartbeat:
                        result = Deserialize<Heartbeat>(ms);
                        break;
                    case MessageEnum.LED:
                        result = Deserialize<LEDMessage>(ms);
                        break;
                    case MessageEnum.Temperature:
                        result = Deserialize<Temperature>(ms);
                        break;
                }
            }
            return result;
        }

        private static IMessage Deserialize<T>(MemoryStream ms)
            where T : class, IMessage
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            T message = ser.ReadObject(ms) as T;
            return message;
        }
    }
}
