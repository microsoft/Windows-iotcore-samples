using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace InternetRadio
{
    /// <summary>
    /// HttpServer class that services the content for the Security System web interface
    /// </summary>
    internal class HttpInterfaceManager : IDisposable
    {
        private const uint BufferSize = 8192;
        private int port = 8000;
        private readonly StreamSocketListener listener;
        private WebHelper helper;
        private IPlaybackManager playbackManager;
        private IPlaylistManager playlistManager;
        private IDevicePowerManager powerManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverPort">Port to start server on</param>
        internal HttpInterfaceManager(int serverPort, IPlaybackManager playbackManager, IPlaylistManager playlistManager, IDevicePowerManager powerManager)
        {
            this.playbackManager = playbackManager;
            this.playlistManager = playlistManager;
            this.powerManager = powerManager;

            helper = new WebHelper();
            listener = new StreamSocketListener();
            port = serverPort;
            listener.ConnectionReceived += (s, e) =>
            {
                try
                {
                    // Process incoming request
                    processRequestAsync(e.Socket);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in StreamSocketListener.ConnectionReceived(): " + ex.Message);
                }
            };
        }

        public async void StartServer()
        {
            await helper.InitializeAsync();

#pragma warning disable CS4014
            listener.BindServiceNameAsync(port.ToString());
#pragma warning restore CS4014
        }

        public void Dispose()
        {
            listener.Dispose();
        }

        /// <summary>
        /// Process the incoming request
        /// </summary>
        /// <param name="socket"></param>
        private async void processRequestAsync(StreamSocket socket)
        {
            try
            {
                StringBuilder request = new StringBuilder();
                using (IInputStream input = socket.InputStream)
                {
                    // Convert the request bytes to a string that we understand
                    byte[] data = new byte[BufferSize];
                    IBuffer buffer = data.AsBuffer();
                    uint dataRead = BufferSize;
                    while (dataRead == BufferSize)
                    {
                        await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                        dataRead = buffer.Length;
                    }
                }

                using (IOutputStream output = socket.OutputStream)
                {
                    // Parse the request
                    string[] requestParts = request.ToString().Split('\n');
                    string requestMethod = requestParts[0];
                    string[] requestMethodParts = requestMethod.Split(' ');

                    // Process the request and write a response to send back to the browser
                    if (requestMethodParts[0].ToUpper() == "GET")
                    {
                        Debug.WriteLine("request for: {0}", requestMethodParts[1]);
                        await writeResponseAsync(requestMethodParts[1], output, socket.Information);
                    }
                    else if (requestMethodParts[0].ToUpper() == "POST")
                    {
                        string requestUri = string.Format("{0}?{1}", requestMethodParts[1], requestParts[requestParts.Length - 1]);
                        Debug.WriteLine("POST request for: {0} ", requestUri);
                        await writeResponseAsync(requestUri, output, socket.Information);
                    }
                    else
                    {
                        throw new InvalidDataException("HTTP method not supported: "
                                                       + requestMethodParts[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in processRequestAsync(): " + ex.Message);
            }
        }

        private async Task writeResponseAsync(string request, IOutputStream os, StreamSocketInformation socketInfo)
        {
            try
            {
                request = request.TrimEnd('\0'); //remove possible null from POST request

                string[] requestParts = request.Split('/');

                // Request for the root page, so redirect to home page
                if (request.Equals("/"))
                {
                    await redirectToPage(NavConstants.HOME_PAGE, os);
                }
                // Request for the home page
                else if (request.Contains(NavConstants.HOME_PAGE))
                {
                    // Generate the default config page
                    string html = await GeneratePageHtml(NavConstants.HOME_PAGE);
                    string onState = (this.playbackManager.PlaybackState == PlaybackState.Playing) ? "On" : "Off";

                    html = html.Replace("#onState#", onState);
                    html = html.Replace("#radioVolume#", (this.playbackManager.Volume * 100).ToString());
                    html = html.Replace("#currentTrack#", this.playlistManager.CurrentTrack.Name);

                    await WebHelper.WriteToStream(html, os);

                }
                // Request for the settings page
                else if (request.Contains(NavConstants.SETTINGS_PAGE))
                {
                    if (!string.IsNullOrEmpty(request))
                    {
                        string settingParam = "";
                        IDictionary<string, string> parameters = WebHelper.ParseGetParametersFromUrl(new Uri(string.Format("http://0.0.0.0/{0}", request)));
                        bool waitForPlaying = (this.playbackManager.PlaybackState == PlaybackState.Playing);
                        bool waitForTrackChange = false;
                        string trackName = this.playlistManager.CurrentTrack.Name;

                        settingParam = "onStateVal";
                        if (parameters.ContainsKey(settingParam) && !string.IsNullOrWhiteSpace(parameters[settingParam]))
                        {
                            switch (parameters[settingParam])
                            {
                                case "On":
                                    if (!waitForPlaying)
                                        this.playbackManager.Play(new Uri(this.playlistManager.CurrentTrack.Address));
                                    waitForPlaying = true;
                                    break;
                                case "Off":
                                    this.playbackManager.Pause();
                                    waitForPlaying = false;
                                    break;
                            }
                        }
                        settingParam = "volumeSlide";
                        if (parameters.ContainsKey(settingParam) && !string.IsNullOrWhiteSpace(parameters[settingParam]))
                        {
                            double newVolume = this.playbackManager.Volume;
                            if (double.TryParse(parameters[settingParam], out newVolume))
                            {
                                newVolume = Math.Round(newVolume / 100, 2);
                                if (newVolume >= 0 && newVolume <= 1 && newVolume != this.playbackManager.Volume)
                                {
                                    this.playbackManager.Volume = newVolume;
                                }
                            }
                        }
                        settingParam = "trackAction";
                        if (parameters.ContainsKey(settingParam) && !string.IsNullOrWhiteSpace(parameters[settingParam]))
                        {
                            waitForTrackChange = true;
                            switch (parameters[settingParam])
                            {
                                case "prev":
                                    this.playbackManager.Pause();
                                    this.playlistManager.PreviousTrack();
                                    break;
                                case "next":
                                    this.playbackManager.Pause();
                                    this.playlistManager.NextTrack();
                                    break;
                                case "track":
                                    if (parameters.ContainsKey("trackName") && !string.IsNullOrWhiteSpace(parameters["trackName"]))
                                    {
                                        if (trackName != parameters["trackName"])
                                        {
                                            this.playbackManager.Pause();
                                            this.playlistManager.PlayTrack(parameters["trackName"]);
                                        }
                                        else
                                            waitForTrackChange = false;
                                    }
                                    break;
                            }
                            if (waitForTrackChange) { waitForPlaying = true; }
                        }

                        DateTime timeOut = DateTime.Now.AddSeconds(30);
                        Debug.WriteLine("Waiting on State: playback={0}; trackname={1}", (waitForPlaying)?"Playing":"NotPlaying", trackName);
                        while (DateTime.Now.CompareTo(timeOut) < 0 && (
                            (this.playbackManager.PlaybackState == PlaybackState.Playing) != waitForPlaying
                            || (this.playlistManager.CurrentTrack.Name != trackName) != waitForTrackChange
                            ));

                        if (DateTime.Now.CompareTo(timeOut) >= 0)
                        {
                            Debug.WriteLine("track did not start playing in time limit");
                        }
                    }
                    //handle UI interaction
                    await redirectToPage(NavConstants.HOME_PAGE, os);
                }
                else if (request.Contains(NavConstants.ADDSTATION_PAGE))
                {
                    string html = await GeneratePageHtml(NavConstants.ADDSTATION_PAGE);
                    string trackName = "";
                    string trackUrl = "";
                    if (!string.IsNullOrEmpty(request))
                    {
                        string settingParam = "trackName";
                        IDictionary<string, string> parameters = WebHelper.ParseGetParametersFromUrl(new Uri(string.Format("http://0.0.0.0/{0}", request)));
                        if (parameters.ContainsKey(settingParam) && !string.IsNullOrWhiteSpace(parameters[settingParam]))
                        {
                            Track trackToUpdate = playlistManager.CurrentPlaylist.Tracks.First(t => t.Name == parameters[settingParam]);
                            if (null != trackToUpdate)
                            {
                                trackName = trackToUpdate.Name;
                                trackUrl = trackToUpdate.Address;
                            }
                        }
                    }
                    html = html.Replace("var stationName = '';", string.Format("var stationName = '{0}';", trackName));
                    html = html.Replace("var stationUrl = '';", string.Format("var stationUrl = '{0}';", trackUrl));
                    await WebHelper.WriteToStream(html, os);
                }
                else if (request.Contains(NavConstants.ADDSTATIONSET_PAGE))
                {
                    if (!string.IsNullOrEmpty(request))
                    {
                        Track origTrack = null;
                        Track newTrack = null;
                        string trackAction = "";
                        IDictionary<string, string> parameters = WebHelper.ParseGetParametersFromUrl(new Uri(string.Format("http://0.0.0.0/{0}", request)));
                        if (parameters.ContainsKey("name") && !string.IsNullOrWhiteSpace(parameters["name"]))
                        {
                            if (parameters.ContainsKey("url") && !string.IsNullOrWhiteSpace(parameters["url"]))
                            {
                                newTrack = new Track() { Name = parameters["name"], Address = parameters["url"] };
                            }
                        }
                        if (parameters.ContainsKey("nameOrig") && !string.IsNullOrWhiteSpace(parameters["nameOrig"]))
                        {
                            origTrack = this.playlistManager.CurrentPlaylist.Tracks.First(t => t.Name == parameters["nameOrig"]);
                        }
                        if (parameters.ContainsKey("trackAction") && !string.IsNullOrWhiteSpace(parameters["trackAction"]))
                        {
                            trackAction = parameters["trackAction"];
                        }

                        if (null != newTrack)
                        {
                            switch (trackAction)
                            {
                                case "Update":
                                    if (null != origTrack)
                                    {
                                        this.playlistManager.CurrentPlaylist.Tracks[this.playlistManager.CurrentPlaylist.Tracks.IndexOf(origTrack)] = newTrack;
                                    }
                                    else
                                    { 
                                        this.playlistManager.CurrentPlaylist.Tracks.Add(newTrack);
                                    }
                                    break;
                                case "Remove":
                                    if (null != origTrack)
                                        this.playlistManager.CurrentPlaylist.Tracks.Remove(origTrack);
                                    break;
                                case "":
                                case "Add":
                                    this.playlistManager.CurrentPlaylist.Tracks.Add(newTrack);
                                    break;
                            }
                        }
                    }
                    await redirectToPage(NavConstants.HOME_PAGE, os);
                }
                // Request for a file that is in the Assets\Web folder (e.g. logo, css file)
                else
                {
                    using (Stream resp = os.AsStreamForWrite())
                    {
                        bool exists = true;
                        try
                        {
                            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

                            // Map the requested path to Assets\Web folder
                            string filePath = NavConstants.ASSETSWEB + request.Replace('/', '\\');

                            // Open the file and write it to the stream
                            using (Stream fs = await folder.OpenStreamForReadAsync(filePath))
                            {
                                string contentType = "";
                                if (request.Contains("css"))
                                {
                                    contentType = "Content-Type: text/css\r\n";
                                }
                                if (request.Contains("htm"))
                                {
                                    contentType = "Content-Type: text/html\r\n";
                                }
                                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                                "Content-Length: {0}\r\n{1}" +
                                                "Connection: close\r\n\r\n",
                                                fs.Length,
                                                contentType);
                                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                                await fs.CopyToAsync(resp);
                            }
                        }
                        catch (FileNotFoundException ex)
                        {
                            exists = false;

                            // Log telemetry event about this exception
                            var events = new Dictionary<string, string> { { "WebServer", ex.Message } };
                            TelemetryManager.WriteTelemetryEvent("FailedToOpenStream", events);
                        }

                        // Send 404 not found if can't find file
                        if (!exists)
                        {
                            byte[] headerArray = Encoding.UTF8.GetBytes(
                                                  "HTTP/1.1 404 Not Found\r\n" +
                                                  "Content-Length:0\r\n" +
                                                  "Connection: close\r\n\r\n");
                            await resp.WriteAsync(headerArray, 0, headerArray.Length);
                        }

                        await resp.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in writeResponseAsync(): " + ex.Message);
                Debug.WriteLine(ex.StackTrace);

                // Log telemetry event about this exception
                var events = new Dictionary<string, string> { { "WebServer", ex.Message } };
                TelemetryManager.WriteTelemetryEvent("FailedToWriteResponse", events);

                try
                {
                    // Try to send an error page back if there was a problem servicing the request
                    string html = helper.GenerateErrorPage("There's been an error: " + ex.Message + "<br><br>" + ex.StackTrace);
                    await WebHelper.WriteToStream(html, os);
                }
                catch (Exception e)
                {
                    TelemetryManager.WriteTelemetryException(e);
                }
            }
        }

        /// <summary>
        /// Get basic html for requested page, with list of stations populated
        /// </summary>
        /// <param name="requestedPage">nav enum ex: home.htm</param>
        /// <returns>string with full HTML, ready to have items replaced. ex: #onState#</returns>
        private async Task<string> GeneratePageHtml(string requestedPage)
        {
            string html = await helper.GeneratePage(requestedPage);
            StringBuilder stationList = new StringBuilder(@"[");
            string trackFormat = "{{ \"name\":\"{0}\" , \"uri\":\"{1}\" }}";
            foreach (Track track in this.playlistManager.CurrentPlaylist.Tracks)
            {
                if (stationList.Length > 10)
                {
                    stationList.Append(",");
                }
                stationList.Append(string.Format(trackFormat, track.Name, track.Address));
            }

            stationList.Append(" ]");

            html = html.Replace("#stationListJSON#", stationList.ToString());
            return html;
        }

        /// <summary>
        /// Redirect to a page
        /// </summary>
        /// <param name="path">Relative path to page</param>
        /// <param name="os"></param>
        /// <returns></returns>
        private async Task redirectToPage(string path, IOutputStream os)
        {
            using (Stream resp = os.AsStreamForWrite())
            {
                byte[] headerArray = Encoding.UTF8.GetBytes(
                                  "HTTP/1.1 302 Found\r\n" +
                                  "Content-Length:0\r\n" +
                                  "Location: /" + path + "\r\n" +
                                  "Connection: close\r\n\r\n");
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await resp.FlushAsync();
            }
        }
    }
}
