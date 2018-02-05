// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace IoTCoreDefaultApp
{
    /// <summary>
    /// Command Line page. 
    /// Allow executing processes and simple command lines using Windows Command Processor, cmd.exe, through a familiar interface.
    /// </summary>
    public sealed partial class CommandLinePage : Page
    {
        private const string CommandLineProcesserExe = "c:\\windows\\system32\\cmd.exe";
        private const string EnableCommandLineProcesserRegCommand = "reg ADD \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\EmbeddedMode\\ProcessLauncher\" /f /v AllowedExecutableFilesList /t REG_MULTI_SZ /d \"c:\\windows\\system32\\cmd.exe\\0\"";
        private const uint MaxLines = 10000;
        private const int MaxReaderTries = 10;
        private readonly SolidColorBrush RedSolidColorBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private readonly SolidColorBrush DefaultSolidColorBrush = new SolidColorBrush(Windows.UI.Colors.WhiteSmoke);
        private readonly TimeSpan CommandTimeOut = TimeSpan.FromSeconds(10);

        private string currentDirectory = "C:\\";
        private List<string> commandLineHistory = new List<string>();
        private int currentCommandLine = -1;
        private ResourceLoader resourceLoader = new ResourceLoader();
        private bool IsProcessRunning = true;
        private CoreDispatcher coreDispatcher;
        private DateTime commandStartTime;
        private IAsyncOperation<ProcessLauncherResult> processLauncherOperation;

        public CommandLinePage()
        {
            this.InitializeComponent();
            this.DataContext = LanguageManager.GetInstance();
            CommandLine.PlaceholderText = String.Format(resourceLoader.GetString("CommandLinePlaceholderText"), currentDirectory);
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            coreDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            CommandLine.Focus(FocusState.Pointer);
        }

        private async Task RunProcess()
        {
            if (string.IsNullOrWhiteSpace(CommandLine.Text))
            {
                return;
            }

            commandLineHistory.Add(CommandLine.Text);
            currentCommandLine = commandLineHistory.Count;

            bool isCmdAuthorized = true;
            Run cmdLineRun = new Run
            {
                Foreground = new SolidColorBrush(Windows.UI.Colors.LightGray),
                FontWeight = FontWeights.Bold,
                Text = currentDirectory + "> " + CommandLine.Text + "\n"
            };

            var stdErrRunText = string.Empty;

            var commandLineText = CommandLine.Text.Trim();

            if (commandLineText.Equals("cls", StringComparison.CurrentCultureIgnoreCase))
            {
                MainParagraph.Inlines.Clear();
                return;
            }
            else if (commandLineText.StartsWith("cd ", StringComparison.CurrentCultureIgnoreCase) || commandLineText.StartsWith("chdir ", StringComparison.CurrentCultureIgnoreCase))
            {
                stdErrRunText = resourceLoader.GetString("CdNotSupported") + "\n";
            }
            else if (commandLineText.Equals("exit", StringComparison.CurrentCultureIgnoreCase))
            {
                NavigationUtils.GoBack();
            }
            else
            {
                var standardOutput = new InMemoryRandomAccessStream();
                var standardError = new InMemoryRandomAccessStream();
                var options = new ProcessLauncherOptions
                {
                    StandardOutput = standardOutput,
                    StandardError = standardError,
                    WorkingDirectory = currentDirectory
                };

                try
                {
                    var args = "/C \"" + commandLineText + "\"";
                    IsProcessRunning = true;
                    commandStartTime = DateTime.Now;

                    processLauncherOperation = ProcessLauncher.RunToCompletionAsync(CommandLineProcesserExe, args, options);
                    processLauncherOperation.Completed = (operation, status) =>
                    {
                        IsProcessRunning = false;
                        if (status == AsyncStatus.Canceled)
                        {
                            stdErrRunText = resourceLoader.GetString("CommandTimeoutText") + "\n";
                        }
                    };

                    await coreDispatcher.RunAsync(
                        CoreDispatcherPriority.Normal, () =>
                    {

                        MainParagraph.Inlines.Add(cmdLineRun);

                        // First write std out
                        using (var outStreamRedirect = standardOutput.GetInputStreamAt(0))
                        {
                            using (var streamReader = new StreamReader(outStreamRedirect.AsStreamForRead()))
                            {
                                ReadText(streamReader);
                            }
                        }

                        // Then write std err
                        using (var errStreamRedirect = standardError.GetInputStreamAt(0))
                        {

                            using (var streamReader = new StreamReader(errStreamRedirect.AsStreamForRead()))
                            {
                                ReadText(streamReader, true);
                            }
                        }
                    });
                }
                catch (UnauthorizedAccessException uax)
                {
                    isCmdAuthorized = false;
                    stdErrRunText = uax.Message + "\n\n" + resourceLoader.GetString("CmdNotEnabled") + "\n";
                }
                catch (Exception ex)
                {
                    stdErrRunText = ex.Message + "\n";
                }
            }

            if (!string.IsNullOrEmpty(stdErrRunText))
            {
                await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Run stdErrRun = new Run
                    {
                        Text = stdErrRunText,
                        Foreground = new SolidColorBrush(Windows.UI.Colors.Red)
                    };

                    MainParagraph.Inlines.Add(stdErrRun);

                    if (!isCmdAuthorized)
                    {
                        InlineUIContainer uiContainer = new InlineUIContainer();
                        Button cmdEnableButton = new Button
                        {
                            Content = resourceLoader.GetString("EnableCmdText")
                        };
                        cmdEnableButton.Click += AccessButtonClicked;
                        uiContainer.Child = cmdEnableButton;
                        MainParagraph.Inlines.Add(uiContainer);
                    }
                });
            }
        }

        private void AccessButtonClicked(object sender, RoutedEventArgs e)
        {
            CoreWindow currentWindow = Window.Current.CoreWindow;
            EnableCmdPopup.VerticalOffset = (currentWindow.Bounds.Height / 2) - (EnableCmdStackPanel.Height / 2);
            EnableCmdPopup.HorizontalOffset = (currentWindow.Bounds.Width / 2) - (EnableCmdStackPanel.Width / 2);
            EnableCmdPopup.IsOpen = true;
            Password.Focus(FocusState.Keyboard);
        }

        private void ReadText(StreamReader streamReader, bool isErrorRun = false)
        {
            int numTries = MaxReaderTries;
            while (numTries > 0)
            {
                string line;
                try
                {
                    line = streamReader.ReadLine();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                if (line == null)
                {
                    if (IsProcessRunning)
                    {
                        if (DateTime.Now.Subtract(commandStartTime) > CommandTimeOut)
                        {
                            processLauncherOperation.Cancel();
                        }
                    }
                    else
                    {
                        numTries--;
                    }
                }
                else
                {
                    if (isErrorRun)
                    {
                        MainParagraph.Inlines.Add(new Run
                            {
                                Text = line + "\n",
                                Foreground = RedSolidColorBrush
                            });
                    }
                    else
                    {
                        MainParagraph.Inlines.Add(new Run
                            {
                                Text = line + "\n"
                            });
                    }

                    if (MainParagraph.Inlines.Count  >= MaxLines)
                    {
                        MainParagraph.Inlines.RemoveAt(0);
                    }
                }
            }
        }

        private async void CommandLine_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Enter:
                    await DoRunCommand(true);
                    break;
                case VirtualKey.Up:
                    currentCommandLine = Math.Max(0, currentCommandLine - 1);
                    if (currentCommandLine < commandLineHistory.Count)
                    {
                        UpdateCommandLineFromHistory();
                    }
                    break;
                case VirtualKey.Down:
                    currentCommandLine = Math.Min(commandLineHistory.Count, currentCommandLine + 1);
                    if (currentCommandLine < commandLineHistory.Count && currentCommandLine >= 0)
                    {
                        UpdateCommandLineFromHistory();
                    }
                    else
                    {
                        CommandLine.Text = string.Empty;
                    }
                    break;
                case VirtualKey.Escape:
                    CommandLine.Text = string.Empty;
                    break;
            }
        }

        private void UpdateCommandLineFromHistory()
        {
            CommandLine.Text = commandLineHistory[currentCommandLine];
            if (CommandLine.Text.Length > 0)
            {
                CommandLine.SelectionStart = CommandLine.Text.Length;
                CommandLine.SelectionLength = 0;
            }
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            await DoRunCommand(false);
        }

        private async Task DoRunCommand(bool isFocus)
        {
            await Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                async () =>
                {
                    RunButton.IsEnabled = false;
                    CommandLine.IsEnabled = false;
                    ClearButton.IsEnabled = false;

                    await RunProcess();
                    CommandLine.Text = string.Empty;

                    RunButton.IsEnabled = true;
                    CommandLine.IsEnabled = true;
                    ClearButton.IsEnabled = true;
                    if (isFocus)
                    {
                        CommandLine.Focus(FocusState.Keyboard);
                    }
                });
        }

        private void StdOutputText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            StdOutputScroller.ChangeView(null, StdOutputScroller.ScrollableHeight, null);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            MainParagraph.Inlines.Clear();
        }

        private async void EnableCmdLineButton_Click(object sender, RoutedEventArgs e)
        {
            if (Password.Password.Trim().Equals(string.Empty))
            {
                // Empty password not accepted
                return;
            }

            try
            {
                var response = await EnableCmdExe("127.0.0.1", Username.Text, Password.Password, EnableCommandLineProcesserRegCommand);
                if (response.IsSuccessStatusCode)
                {
                    CmdEnabledStatus.Text = resourceLoader.GetString("CmdTextEnabledSuccess");
                }
                else
                {
                    CmdEnabledStatus.Text = string.Format(resourceLoader.GetString("CmdTextEnabledFailure"), response.StatusCode);
                }
            }
            catch (Exception cmdEnabledException)
            {
                CmdEnabledStatus.Text = string.Format(resourceLoader.GetString("CmdTextEnabledFailure"), cmdEnabledException.HResult);
            }

            EnableCmdPopup.IsOpen = false;

            CoreWindow currentWindow = Window.Current.CoreWindow;
            CmdEnabledStatusPopup.VerticalOffset = (currentWindow.Bounds.Height / 2) - (StatusStackPanel.Height / 2);
            CmdEnabledStatusPopup.HorizontalOffset = (currentWindow.Bounds.Width / 2) - (StatusStackPanel.Width / 2);

            CmdEnabledStatusPopup.IsOpen = true;
        }

        private static async Task<HttpResponseMessage> EnableCmdExe(string ipAddress, string username, string password, string runCommand)
        {
            HttpClient client = new HttpClient();
            var command = CryptographicBuffer.ConvertStringToBinary(runCommand, BinaryStringEncoding.Utf8);
            var runAsdefault = CryptographicBuffer.ConvertStringToBinary("false", BinaryStringEncoding.Utf8);

            var urlContent = new HttpFormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("command", CryptographicBuffer.EncodeToBase64String(command)),
                new KeyValuePair<string,string>("runasdefaultaccount", CryptographicBuffer.EncodeToBase64String(runAsdefault)),
            });

            Uri uri = new Uri("http://" + ipAddress + ":8080/api/iot/processmanagement/runcommand?" + await urlContent.ReadAsStringAsync());

            var authBuffer = CryptographicBuffer.ConvertStringToBinary(username + ":" + password, BinaryStringEncoding.Utf8);
            client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Basic", CryptographicBuffer.EncodeToBase64String(authBuffer));

            HttpResponseMessage response = await client.PostAsync(uri, null);
            return response;
        }

        private void CloseStatusButton_Click(object sender, RoutedEventArgs e)
        {
            CmdEnabledStatusPopup.IsOpen = false;
            CommandLine.Focus(FocusState.Keyboard);
        }
    }
}
