// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    internal enum CommandError
    {
        None,
        Cancelled,
        CdNotSupported,
        InvalidDirectory,
        NotAuthorized,
        TimedOut,
        GenericError
    }
    /// <summary>
    /// Command Line page. 
    /// Allow executing processes and simple command lines using Windows Command Processor, cmd.exe, through a familiar interface.
    /// </summary>
    public sealed partial class CommandLinePage : Page
    {
        private const string CommandLineProcesserExe = "c:\\windows\\system32\\cmd.exe";
        private const string EnableCommandLineProcesserRegCommand = "reg ADD \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\EmbeddedMode\\ProcessLauncher\" /f /v AllowedExecutableFilesList /t REG_MULTI_SZ /d \"c:\\windows\\system32\\cmd.exe\\0\"";
        private const uint PageSize = 25;
        private const uint PagingThreshold = 500;
        private const uint MaxCommandOutputLines = 2000;
        private const uint MaxTotalOutputRuns = 2000;
        private const int MaxRetriesAfterProcessTerminates = 2;
        private const int HRESULT_AccessDenied = -2147024891;
        private const int HRESULT_InvalidDirectory = -2147024629;
        private readonly SolidColorBrush RedSolidColorBrush = new SolidColorBrush(Colors.Red);
        private readonly SolidColorBrush GraySolidColorBrush = new SolidColorBrush(Colors.Gray);
        private readonly SolidColorBrush YellowSolidColorBrush = new SolidColorBrush(Colors.Yellow);
        private readonly TimeSpan TimeOutAfterNoOutput = TimeSpan.FromSeconds(15);

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
            NavigationCacheMode = NavigationCacheMode.Enabled;
            coreDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            EnableCommandLineTextBox(true, CommandLine);
        }

        private async Task RunProcess()
        {
            if (string.IsNullOrWhiteSpace(CommandLine.Text))
            {
                return;
            }

            commandLineHistory.Add(CommandLine.Text);
            currentCommandLine = commandLineHistory.Count;

            Run cmdLineRun = new Run
            {
                Foreground = GraySolidColorBrush,
                FontWeight = FontWeights.Bold,
                Text = "\n" + WorkingDirectory.Text + " " + CommandLine.Text + "\n"
            };

            var stdErrRunText = string.Empty;
            var commandLineText = CommandLine.Text.Trim();
            CommandError commandError = CommandError.None;

            EnableCommandLineTextBox(false);
            CommandLine.Text = string.Empty;
            MainParagraph.Inlines.Add(cmdLineRun);

            if (commandLineText.Equals("cls", StringComparison.CurrentCultureIgnoreCase) ||
                commandLineText.Equals("clear", StringComparison.CurrentCultureIgnoreCase))
            {
                MainParagraph.Inlines.Clear();
                EnableCommandLineTextBox(true, CommandLine);
                return;
            }
            else if (commandLineText.StartsWith("cd ", StringComparison.CurrentCultureIgnoreCase) || commandLineText.StartsWith("chdir ", StringComparison.CurrentCultureIgnoreCase))
            {
                commandError = CommandError.CdNotSupported;
                stdErrRunText = resourceLoader.GetString("CdNotSupported");
            }
            else if (commandLineText.Equals("exit", StringComparison.CurrentCultureIgnoreCase))
            {
                NavigationUtils.GoBack();
            }
            else
            {
                var args = String.Format("/C \"{0}\"", commandLineText); ;
                var standardOutput = new InMemoryRandomAccessStream();
                var standardError = new InMemoryRandomAccessStream();
                var options = new ProcessLauncherOptions
                {
                    StandardOutput = standardOutput,
                    StandardError = standardError,
                    WorkingDirectory = GetWorkingDirectory()
                };

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
                            commandError = CommandError.TimedOut;
                            stdErrRunText = String.Format(resourceLoader.GetString("CommandTimeoutText"), TimeOutAfterNoOutput.Seconds);
                        }
                        else
                        {
                            commandError = CommandError.Cancelled;
                            stdErrRunText = resourceLoader.GetString("CommandCancelled");
                        }
                    }
                    else if (status == AsyncStatus.Error)
                    {
                        if (operation.ErrorCode.HResult == HRESULT_AccessDenied)
                        {
                            commandError = CommandError.NotAuthorized;
                            stdErrRunText = String.Format(resourceLoader.GetString("CmdNotEnabled"), EnableCommandLineProcesserRegCommand);

                        }
                        else
                        if (operation.ErrorCode.HResult == HRESULT_InvalidDirectory)
                        {
                            commandError = CommandError.InvalidDirectory;
                            stdErrRunText = String.Format(resourceLoader.GetString("WorkingDirectoryInvalid"), options.WorkingDirectory);

                        }
                        else
                        {
                            commandError = CommandError.GenericError;
                            stdErrRunText = String.Format(resourceLoader.GetString("CommandLineError"), operation.ErrorCode.Message);
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

            if (commandError != CommandError.None)
            {
                Run stdErrRun = new Run
                {
                    Text = stdErrRunText + "\n",
                    Foreground = RedSolidColorBrush,
                    FontWeight = FontWeights.Bold
                };

                MainParagraph.Inlines.Add(stdErrRun);

                if (commandError == CommandError.NotAuthorized)
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

            if (commandError == CommandError.InvalidDirectory || commandError == CommandError.CdNotSupported)
            {
                EnableCommandLineTextBox(true, WorkingDirectory);
            }
            else
            {
                EnableCommandLineTextBox(true, CommandLine);

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

        private async Task ReadText(StreamReader streamReader, bool isErrorRun = false)
        {
            DateTime lastOutputTime = DateTime.Now;
            uint numTriesAfterProcessCompletes = 0;
            uint numLinesReadFromStream = 0;
            uint numLinesInCurrentPage = 0;
            StringBuilder currentPage = null;
            bool isPageOutput = false;
            bool isCmdOutputWarningDisplayed = false;
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

                            if (isPageOutput && numLinesInCurrentPage > 0 && !isCmdOutputWarningDisplayed)
                            {
                                await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    AddLineToParagraph(isErrorRun, currentPage.ToString());
                                });
                            }

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
                        if (numTriesAfterProcessCompletes >= MaxRetriesAfterProcessTerminates)
                        {
                            if (isPageOutput && numLinesInCurrentPage > 0 && !isCmdOutputWarningDisplayed)
                            {
                                await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    AddLineToParagraph(isErrorRun, currentPage.ToString());
                                });
                            }

                            break;
                        }
                        else
                        {
                            numTriesAfterProcessCompletes++;
                            await Task.Delay(TimeSpan.FromMilliseconds(1));
                        }
                    }
                }
                else
                {
                    lastOutputTime = DateTime.Now;
                    numLinesReadFromStream++;
                    if (numLinesReadFromStream >= PagingThreshold && !isPageOutput)
                    {
                        isPageOutput = true;
                        currentPage = new StringBuilder();
                        numLinesInCurrentPage = 0;
                    }

                    if (numLinesReadFromStream > MaxCommandOutputLines)
                    {
                        if (!isCmdOutputWarningDisplayed)
                        {
                            isCmdOutputWarningDisplayed = true;
                            await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                AddLineToParagraph(isErrorRun, "...\n");
                                AddLineToParagraph(true, resourceLoader.GetString("CommandOutputTooLongeWarning") + "\n");
                            });
                        }
                        continue;
                    }

                    if (isPageOutput)
                    {
                        currentPage.AppendLine(line);

                        if (numLinesInCurrentPage < PageSize)
                        {
                            numLinesInCurrentPage++;
                        }
                        else
                        {
                            await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                AddLineToParagraph(isErrorRun, currentPage.ToString());
                            });
                            numLinesInCurrentPage = 0;
                            currentPage.Clear();
                        }
                    }
                    else
                    {
                        await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            AddLineToParagraph(isErrorRun, line + "\n");
                        });
                    }
                }
            }
        }

        private void AddLineToParagraph(bool isErrorRun, string text)
        {
            if (MainParagraph.Inlines.Count >= MaxTotalOutputRuns)
            {
                MainParagraph.Inlines.RemoveAt(0);
            }

            MainParagraph.Inlines.Add(new Run
            {
                Text = text,
                Foreground = isErrorRun ? RedSolidColorBrush : MainParagraph.Foreground
            });
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

        private void EnableCommandLineTextBox(bool isEnabled, TextBox textBoxToFocus = null)
        {
            RunButton.IsEnabled = isEnabled;
            CommandLine.IsEnabled = isEnabled;
            WorkingDirectory.IsEnabled = isEnabled;
            ClearButton.IsEnabled = isEnabled;

            CancelButton.IsEnabled = !isEnabled;
            CancelButton.Foreground = isEnabled ? GraySolidColorBrush : YellowSolidColorBrush;
            CancelButton.FontWeight = isEnabled ? FontWeights.Normal : FontWeights.Bold;

            if (isEnabled && textBoxToFocus != null)
            {
                textBoxToFocus.Focus(FocusState.Keyboard);
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
                    CmdEnabledStatus.Text = String.Format(resourceLoader.GetString("CmdTextEnabledFailure"), response.StatusCode);
                }
            }
            catch (Exception cmdEnabledException)
            {
                CmdEnabledStatus.Text = String.Format(resourceLoader.GetString("CmdTextEnabledFailure"), cmdEnabledException.HResult);
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

        private void WorkingDirectory_LostFocus(object sender, RoutedEventArgs e)
        {
            string workingDirectory = GetWorkingDirectory();

            WorkingDirectory.Text = workingDirectory + ">";
        }

        private bool IsDrive(string workingDirectory)
        {
            return workingDirectory.Length == 2 && workingDirectory[1] == ':';
        }

        private string GetWorkingDirectory()
        {
            var workingDirectory = WorkingDirectory.Text.Trim();
            if (workingDirectory.Length == 0)
            {
                return "C:\\";
            }

            while (workingDirectory.EndsWith(">") || workingDirectory.EndsWith("\\"))
            {
                workingDirectory = workingDirectory.Remove(workingDirectory.Length - 1);
            }

            if (IsDrive(workingDirectory))
            {
                workingDirectory = workingDirectory + "\\";
            }

            return workingDirectory;
        }

        private void WorkingDirectory_GotFocus(object sender, RoutedEventArgs e)
        {
            WorkingDirectory.Text = GetWorkingDirectory();
            WorkingDirectory.SelectAll();
        }

        private void WorkingDirectory_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                CommandLine.Focus(FocusState.Keyboard);
            }
        }
    }
}
