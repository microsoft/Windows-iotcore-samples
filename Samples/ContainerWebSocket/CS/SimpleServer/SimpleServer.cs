using SharedData;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;


namespace Microsoft.Windows.Iot.Core
{
    public class SimpleServer
    {
        private int requestCount = 0;
        private WebSocket webSocket;
        private ConcurrentQueue<IMessage> queue = new ConcurrentQueue<IMessage>();
        Action<string> Trace;
        Action<string> TraceLine;

        public SimpleServer(Action<string> trace, Action<string> traceLine)
        {
            Trace = trace;
            TraceLine = traceLine;
        }

        public void Start(string listenerPrefix)
        {
            TraceLine($"Starting server: {listenerPrefix}");
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add(listenerPrefix);
            httpListener.Start();
            TraceLine("Listening...");

            while (true)
            {
                httpListener.BeginGetContext((result) =>
                {
                    var listener = result.AsyncState as HttpListener;
                    var context = listener.EndGetContext(result);

                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }, httpListener).AsyncWaitHandle.WaitOne();
            }
        }

        private async void ProcessRequest(HttpListenerContext listenerContext)
        {

            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
                Interlocked.Increment(ref requestCount);
                TraceLine($"Requests processed: {requestCount}");
            }
            catch (Exception e)
            {
                // The upgrade process failed somehow. For simplicity lets assume it was a failure on the part of the server and indicate this using 500.
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                TraceLine($"Exception: {e.Message}");
                return;
            }

            try
            {
                webSocket = webSocketContext.WebSocket;
                await Task.WhenAll(SendReceiveLoop(webSocket), SimulateWeatherStation(webSocket));
            }
            catch (Exception e)
            {
                TraceLine($"Exception: {e.Message}");
            }
            finally
            {
                // Clean up by disposing the WebSocket once it is closed/aborted.
                if (webSocket != null)
                    webSocket.Dispose();
            }
        }

        private async Task SendReceiveLoop(WebSocket webSocket)
        {
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    IMessage receiveMessage = await MessageHelper.ReceiveMessage(webSocket);
                    if (receiveMessage != null)
                    {
                        switch (receiveMessage.MessageType)
                        {
                            case MessageEnum.Heartbeat:
                                queue.Enqueue(receiveMessage);
                                TraceLine(DateTime.Now.ToString("HH:mm:ss.ffff") + " Heartbeat");
                                break;
                            case MessageEnum.LED:
                                LEDMessage led = receiveMessage as LEDMessage;
                                if (led != null)
                                {
                                    if (currentTemperature > 90.0)
                                    {
                                        queue.Enqueue(led);
                                        currentTemperature = 70.0;
                                        TraceLine(DateTime.Now.ToString("HH:mm:ss.ffff") + $" LEDMessage: [{led.Pin} {led.State}]");
                                    }
                                }
                                break;
                        }
                    }

                    IMessage sendMessage;
                    while (queue.TryDequeue(out sendMessage))
                    {
                        await MessageHelper.SendMessage(webSocket, sendMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLine(ex.Message);
            }
        }

        private double currentTemperature = 70.0;

        private async Task SimulateWeatherStation(WebSocket socket)
        {
            try
            {
                Random r = new Random();
                while (socket.State == WebSocketState.Open)
                {
                    currentTemperature += (r.NextDouble() * 5.0);
                    queue.Enqueue(new Temperature(currentTemperature));
                    TraceLine(DateTime.Now.ToString("HH:mm:ss.ffff") + $" Current Temperature: {currentTemperature}");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            catch (Exception ex)
            {
                TraceLine(ex.Message);
            }
        }
    }
}
