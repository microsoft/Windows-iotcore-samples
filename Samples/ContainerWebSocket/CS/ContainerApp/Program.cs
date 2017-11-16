using SharedData;
using System;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        private static object consoleLock = new object();
        private static ConcurrentQueue<IMessage> queue = new ConcurrentQueue<IMessage>();

        static void Main(string[] args)
        {
            try
            {
                string gateway = GetGateWayAddress();
                Console.WriteLine($"Gateway Address: {gateway}");
                Connect(uri: $"ws://{gateway}:22122/wsDemo").Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static string GetGateWayAddress()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

            if (Environment.UserName.Equals("ContainerUser", StringComparison.InvariantCultureIgnoreCase))
            {
                return NetworkHelper.GetGateWayAddress();
            }
            else
            {
                return NetworkHelper.GetLocalHost();
            }
        }

        public static async Task Connect(string uri)
        {
            ClientWebSocket webSocket = null;

            while (true)
            {
                try
                {
                    Console.WriteLine($"Connect({uri})");
                    webSocket = new ClientWebSocket();
                    await webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
                    Console.WriteLine("Connected");
                    await Task.WhenAll(Receive(webSocket), Send(webSocket), HeartbeatTask(webSocket));
                }
                catch(WebSocketException wse)
                {
                    Console.WriteLine(wse.Message);
                }
                catch (Exception ex)
                {
                    lock (consoleLock)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine($"Exception: {ex.Message}");
                        Console.ResetColor();
                        throw;
                    }
                }
                finally
                {
                    if (webSocket != null)
                        webSocket.Dispose();

                    lock (consoleLock)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("WebSocket closed.");
                        Console.ResetColor();
                    }
                }

                Console.WriteLine("Waiting 1 second and connecting again");
                await Task.Delay(1000);
            }
        }

        private static async Task Send(ClientWebSocket webSocket)
        {
            try
            {
                IMessage message;
                while (webSocket.State == WebSocketState.Open)
                {
                    if (queue.TryDequeue(out message))
                    {
                        await MessageHelper.SendMessage(webSocket, message);
                    }
                    await Task.Yield();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                webSocket.Dispose();
            }
        }

        private static async Task Receive(ClientWebSocket webSocket)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    IMessage message = await MessageHelper.ReceiveMessage(webSocket);
                    if (message != null)
                    {
                        switch (message.MessageType)
                        {
                            case MessageEnum.Heartbeat:
                                Heartbeat heartbeat = message as Heartbeat;
                                if (heartbeat != null)
                                {
                                    Console.WriteLine("Receive Heartbeat");
                                }
                                break;
                            case MessageEnum.Temperature:
                                Temperature temp = message as Temperature;
                                if (temp != null)
                                {
                                    Console.WriteLine($"Receive Temperature: {temp.Value}");
                                    if (temp.Value > 90.0)
                                    {
                                        queue.Enqueue(new LEDMessage(12, LEDState.On));
                                    }
                                }
                                break;
                            case MessageEnum.LED:
                                LEDMessage led = message as LEDMessage;
                                if (led != null)
                                {
                                    Console.WriteLine($"Receive LED [Pin: {led.Pin}  State: {led.State}]");
                                }
                                break;
                        }
                    }
                    else
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "expected binary", CancellationToken.None);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private static async Task HeartbeatTask(ClientWebSocket webSocket)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                queue.Enqueue(new Heartbeat());
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
