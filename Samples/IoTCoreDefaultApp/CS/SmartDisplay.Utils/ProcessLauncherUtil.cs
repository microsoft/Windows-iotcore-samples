// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.System;

namespace SmartDisplay.Utils
{
    /// <summary>
    /// Any executables run by ProcessLauncher need to be whitelisted in the Registry first.
    /// More information: https://docs.microsoft.com/en-us/uwp/api/windows.system.processlauncher.runtocompletionasync
    /// </summary>
    public static class ProcessLauncherUtil
    {
        private const string RegKeyQueryCmdArg = "/c \"reg query HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\IoT /v IsMakerImage /z\"";
        private const string CommandLineProcesserExe = "c:\\windows\\system32\\cmd.exe";
        private const string ExpectedResultPattern = @"\s*IsMakerImage\s*REG_DWORD\s*\(4\)\s*0x1";
        private const uint CmdLineBufSize = 8192;

        #region Device Info
        public static async Task<bool> GetIsMakerImageAsync()
        {
            var cmdOutput = string.Empty;
            var standardOutput = new InMemoryRandomAccessStream();
            var options = new ProcessLauncherOptions
            {
                StandardOutput = standardOutput
            };
            var output = await ProcessLauncher.RunToCompletionAsync(CommandLineProcesserExe, RegKeyQueryCmdArg, options);
            if (output != null && output.ExitCode == 0)
            {
                using (var outStreamRedirect = standardOutput.GetInputStreamAt(0))
                {
                    using (var dataReader = new DataReader(outStreamRedirect))
                    {
                        uint bytesLoaded = 0;
                        while ((bytesLoaded = await dataReader.LoadAsync(CmdLineBufSize)) > 0)
                        {
                            cmdOutput += dataReader.ReadString(bytesLoaded);
                        }
                    }
                }
                Match match = Regex.Match(cmdOutput, ExpectedResultPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return true;
                }
                else
                {
                    ServiceUtil.LogService.Write("Could not get IsMakerImage. Output: " + cmdOutput);
                }
            }
            return false;
        }
        #endregion


        public static async Task<ProcessLauncherOutput> RunCommandAsync(string fileName, string args)
        {
            var output = new ProcessLauncherOutput();
            try
            {
                using (var standardOutput = new InMemoryRandomAccessStream())
                using (var standardError = new InMemoryRandomAccessStream())
                {
                    var options = new ProcessLauncherOptions
                    {
                        StandardOutput = standardOutput,
                        StandardError = standardError
                    };

                    var result = await ProcessLauncher.RunToCompletionAsync(
                        fileName,
                        args,
                        options);

                    output.Result = result;

                    using (IInputStream inputStream = standardOutput.GetInputStreamAt(0))
                    {
                        ulong size = standardOutput.Size;

                        using (var dataReader = new DataReader(inputStream))
                        {
                            uint bytesLoaded = await dataReader.LoadAsync((uint)size);
                            output.Output = dataReader.ReadString(bytesLoaded);
                        }
                    }

                    using (IInputStream inputStream = standardError.GetInputStreamAt(0))
                    {
                        ulong size = standardError.Size;

                        using (var dataReader = new DataReader(inputStream))
                        {
                            uint bytesLoaded = await dataReader.LoadAsync((uint)size);
                            output.Error = dataReader.ReadString(bytesLoaded);
                        }
                    }
                }

                return output;
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.WriteException(ex);
            }

            return null;
        }

        /// <summary>
        /// Runs executables using cmd.exe, which is whitelisted on Windows IoT Core Maker images
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static async Task<ProcessLauncherOutput> RunCommandLineAsync(string command)
        {
            return await RunCommandAsync(CommandLineProcesserExe, string.Format("/C \"{0}\"", command));
        }

        public class ProcessLauncherOutput
        {
            public ProcessLauncherResult Result;
            public string Output;
            public string Error;

            public override string ToString()
            {
                return $"Exit Code: {Result.ExitCode}" + Environment.NewLine +
                    $"Output: {Output}" + Environment.NewLine +
                    $"Error: {Error}";
            }
        }
    }
}
