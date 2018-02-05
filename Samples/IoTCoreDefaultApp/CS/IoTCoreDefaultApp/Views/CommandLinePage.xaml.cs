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
using Windows.UI;
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
        private const uint MaxOutputLines = 10000;
        private readonly SolidColorBrush RedSolidColorBrush = new SolidColorBrush(Colors.Red);
        private readonly SolidColorBrush DefaultSolidColorBrush = new SolidColorBrush(Colors.WhiteSmoke);
        private readonly TimeSpan TimeOutAfterNoOutput = TimeSpan.FromSeconds(15);

        private string currentDirectory = "C:\\";
        private List<string> commandLineHistory = new List<string>();
        private int currentCommandLine = -1;
        private ResourceLoader resourceLoader = new ResourceLoader();
        private bool isProcessRunning = true;
        private CoreDispatcher coreDispatcher;
        private IAsyncOperation<ProcessLauncherResult> processLauncherOperation;
        private bool isProcessTimedOut = false;

        public CommandLinePage()
        {
            InitializeComponent();
            DataContext = LanguageManager.GetInstance();
            CommandLine.PlaceholderText = String.Format(resourceLoader.GetString("CommandLinePlaceholderText"), currentDirectory);
            NavigationCacheMode = NavigationCacheMode.Enabled;
            coreDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            EnableCommandLineTextBox(true);
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
                Foreground = DefaultSolidColorBrush,
                FontWeight = FontWeights.Bold,
                Text = currentDirectory + "> " + CommandLine.Text + "\n"
            };

            var stdErrRunText = string.Empty;
            var commandLineText = CommandLine.Text.Trim();

            EnableCommandLineTextBox(false);
            CommandLine.Text = string.Empty;
            MainParagraph.Inlines.Add(cmdLineRun);

            if (commandLineText.Equals("cls", StringComparison.CurrentCultureIgnoreCase) ||
                commandLineText.Equals("clear", StringComparison.CurrentCultureIgnoreCase))
            {
                MainParagraph.Inlines.Clear();
                EnableCommandLineTextBox(true);
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
                var args = "/C \"" + commandLineText + "\"";
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
                    isProcessRunning = true;
                    isProcessTimedOut = false;
                    processLauncherOperation = ProcessLauncher.RunToCompletionAsync(CommandLineProcesserExe, args, options);
                    processLauncherOperation.Completed = (operation, status) =>
                    {
                        isProcessRunning = false;
                        if (status == AsyncStatus.Canceled)
                        {
                            if (isProcessTimedOut)
                            {
                                stdErrRunText = String.Format(resourceLoader.GetString("CommandTimeoutText"), TimeOutAfterNoOutput.Seconds) + "\n";
                            }
                            else
                            {
                                stdErrRunText = resourceLoader.GetString("CommandCancelled") + "\n";
                            }
                        }
                    };

                    // First write std out
                    using (var outStreamRedirect = standardOutput.GetInputStreamAt(0))
                    {
                        using (var streamReader = new StreamReader(outStreamRedirect.AsStreamForRead()))
                        {
                            await ReadText(streamReader);
                        }
                    }

                    // Then write std err
                    using (var errStreamRedirect = standardError.GetInputStreamAt(0))
                    {

                        using (var streamReader = new StreamReader(errStreamRedirect.AsStreamForRead()))
                        {
                            await ReadText(streamReader, true);
                        }
                    }
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
                Run stdErrRun = new Run
                {
                    Text = stdErrRunText,
                    Foreground = RedSolidColorBrush,
                    FontWeight = FontWeights.Bold
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
            }

            EnableCommandLineTextBox(true);
        }

        private void AccessButtonClicked(object sender, RoutedEventArgs e)
        {
            CoreWindow currentWindow = Window.Current.CoreWindow;
            EnableCmdPopup.VerticalOffset = (currentWindow.Bounds.Height / 2) - (EnableCmdStackPanel.Height / 2);
            EnableCmdPopup.HorizontalOffset = (currentWindow.Bounds.Width / 2) - (EnableCmdStackPanel.Width / 2);
            EnableCmdPopup.IsOpen = true;
            Password.Focus(FocusState.Keyboard);
        }

        private async Task ReadText(StreamReader streamReader, bool isErrorRun = false)
        {
            DateTime lastOutputTime = DateTime.Now;
            while (true)
            {
                string line = await streamReader.ReadLineAsync();
                if (line == null)
                {
                    if (isProcessRunning)
                    {
                        if (DateTime.Now.Subtract(lastOutputTime) > TimeOutAfterNoOutput)
                        {
                            // Timeout
                            isProcessTimedOut = true;
                            processLauncherOperation.Cancel();
                        }
                        else
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(1));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    lastOutputTime = DateTime.Now;
                    await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MainParagraph.Inlines.Add(new Run
                        {
                            Text = line + "\n",
                            Foreground = isErrorRun ? RedSolidColorBrush : MainParagraph.Foreground
                        });

                        if (MainParagraph.Inlines.Count >= MaxOutputLines)
                        {
                            MainParagraph.Inlines.RemoveAt(0);
                        }
                    });
                }
            }
        }

        private async void CommandLine_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Enter:
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            await RunProcess();
                        });
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
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await RunProcess();
            });
        }

        private void EnableCommandLineTextBox(bool isEnaled)
        {
            RunButton.IsEnabled = isEnaled;
            CommandLine.IsEnabled = isEnaled;
            ClearButton.IsEnabled = isEnaled;
            CancelButton.IsEnabled = !isEnaled;

            if (isEnaled)
            {
                CommandLine.Focus(FocusState.Keyboard);
            }
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
            var Protocol = "http";
            var Port = "8080";
            var client = new HttpClient();
            var command = CryptographicBuffer.ConvertStringToBinary(runCommand, BinaryStringEncoding.Utf8);
            var runAsDefaultAccountFalse = CryptographicBuffer.ConvertStringToBinary("false", BinaryStringEncoding.Utf8);

            var urlContent = new HttpFormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("command", CryptographicBuffer.EncodeToBase64String(command)),
                new KeyValuePair<string,string>("runasdefaultaccount", CryptographicBuffer.EncodeToBase64String(runAsDefaultAccountFalse)),
            });

            var uri = new Uri(Protocol + "://" + ipAddress + ":" + Port + "/api/iot/processmanagement/runcommand?" + await urlContent.ReadAsStringAsync());

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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessRunning)
            {
                processLauncherOperation.Cancel();
            }
        }
    }
}
