// Copyright (c) Microsoft Corporation. All rights reserved.

using Newtonsoft.Json;
using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace SmartDisplay.Utils
{
    public class DevicePortalUtil
    {
        public const int InvalidTelemetryValue = -1;
        public const int DefaultTelemetryValue = 0;
        public const int BasicTelemetryValue = 1;
        public const int FullTelemetryValue = 3;

        private static PasswordVault _vault = new PasswordVault();
        private static string _resource = "WDP";
        public const string DefaultUserName = "administrator";
        private const string FlightRingRegKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsSelfHost\Applicability";
        public const string TelemetryRegKey = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\DataCollection";
        public const string TelemetryRegValue = "AllowTelemetry";
        private static readonly string RegAddAllowTelemetryFormat = "reg add " + TelemetryRegKey + " /v " + TelemetryRegValue + " /t REG_DWORD /d {0} /f";
        private const string BaseUri = "http://localhost:8080";

        #region PasswordVault

        public static void StoreCredential(string username, string password)
        {
            ClearCredentials();

            ServiceUtil.LogService.Write($"Storing credentials for {username}");
            _vault.Add(new PasswordCredential(_resource, username, password));
        }

        public static void ClearCredentials()
        {
            ServiceUtil.LogService.Write($"Clearing all credentials");

            try
            {
                foreach (var credential in _vault.RetrieveAll())
                {
                    _vault.Remove(credential);
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.WriteException(ex);
            }
        }

        public static PasswordCredential GetCredential()
        {
            try
            {
                var credential = _vault.FindAllByResource(_resource).FirstOrDefault();
                if (credential != null)
                {
                    ServiceUtil.LogService.Write($"Found credentials for {credential.UserName}");
                    credential.RetrievePassword();
                    return credential;
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.WriteException(ex);
            }

            ServiceUtil.LogService.Write("No credentials found");
            return null;
        }

        public static async Task<bool> SignInAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrEmpty(password))
            {
                return false;
            }
            // Verify credentials by attempting to get OS info
            var info = await GetOsInfoAsync(username, password);
            if (info != null)
            {
                StoreCredential(username, password);
                return true;
            }
            return false;
        }

        public static async Task<bool> IsSignedInAsync()
        {
            var cred = GetCredential();
            if (cred != null)
            {
                // Verify credentials by attempting to get OS info
                var info = await GetOsInfoAsync(cred.UserName, cred.Password);
                return (info != null);
            }
            return false;
        }

        #endregion

        // Rather than making the check each time, cache it
        private static bool? _isDevicePortalEnabled = null;
        public static bool IsDevicePortalEnabled()
        {
            if (_isDevicePortalEnabled == null)
            {
                var response = Task.Run(() => WebUtil.GetAsync(BaseUri, null)).Result;
                _isDevicePortalEnabled = (response != null) && (response.StatusCode != System.Net.HttpStatusCode.NotFound);
            }

            return _isDevicePortalEnabled == true;
        }

        public static async Task<OsInfo> GetOsInfoAsync(string username, string password)
        {
            try
            {
                string uri = BaseUri + "/api/os/info";

                ServiceUtil.LogService.Write("GET URI: " + uri);

                var result = await WebUtil.SendRequestAsync(uri, username, password, HttpMethod.Get);
                if (result == null)
                {
                    ServiceUtil.LogService.Write("SendRequestAsync failed");
                    return null;
                }

                return JsonConvert.DeserializeObject<OsInfo>(result);
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.WriteException(ex);
            }

            return null;
        }

        public static async Task<Packages> GetInstalledPackagesAsync(string username, string password)
        {
            try
            {
                string uri = BaseUri + "/api/app/packagemanager/packages";

                ServiceUtil.LogService.Write("GET URI: " + uri);

                var result = await WebUtil.SendRequestAsync(uri, username, password, HttpMethod.Get);
                if (result == null)
                {
                    ServiceUtil.LogService.Write("SendRequestAsync failed");
                    return null;
                }

                return JsonConvert.DeserializeObject<Packages>(result);
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.WriteException(ex);
            }

            return null;
        }

        public static async Task<RunCommandWithOutput> RunCommandWithOutputAsync(string username, string password, string command)
        {
            try
            {
                string cmd = $"?command={Base64Encode(command)}&runasdefaultaccount={Base64Encode("false")}&timeout={Base64Encode("2000")}";
                string uri = BaseUri + WdpConstants.RunCommandWithOutputApi + cmd;

                ServiceUtil.LogService.Write("POST URI: " + uri);
                ServiceUtil.LogService.Write("Cmd: " + cmd);

                var result = await WebUtil.SendRequestAsync(uri, username, password, HttpMethod.Post);
                if (result == null)
                {
                    ServiceUtil.LogService.Write("SendRequestAsync failed");
                    return null;
                }

                return JsonConvert.DeserializeObject<RunCommandWithOutput>(result);
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.WriteException(ex);
            }

            return null;
        }

        #region Time Zone

        public static async Task<RunCommandWithOutput> GetTimeZoneAsync(string username, string password)
        {
            return await RunCommandWithOutputAsync(username, password, "tzutil /g");
        }

        public static async Task<RunCommandWithOutput> SetTimeZoneAsync(string username, string password, string timezone)
        {
            string command = "tzutil /s \"" + timezone + "\"";
            return await RunCommandWithOutputAsync(username, password, command);
        }
        #endregion

        #region Flight Rings

        // Some rings are commented out because we never flight to them
        public static readonly Dictionary<string, string> RingIdDict = new Dictionary<string, string>()
        {
            {"Canary", "-setCanary"},
            {"Self-Host (OSG)", "-setSelfhost" },
            {"Windows Insider Fast (WIF)", "-setWIPFast" },
            {"Retail", "-setRetail" },
            {"Canary WSD Servicing", "-setPVP" },
        };

        public static async Task<string> GetFlightRingAsync(string username, string password)
        {
            var output = await RunCommandWithOutputAsync(username, password, "reg query " + FlightRingRegKey + " /v Ring");
            if (output != null)
            {
                // Output is regkey, followed by line of format "Ring    REG_SZ    value"
                // Split the line to get only the value, then split at Z to get only the data
                var lineSplit = output.Output.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lineSplit.Length > 1)
                {
                    if (lineSplit[1].Contains("Ring"))
                    {
                        var subStrings = lineSplit[1].Split('Z');
                        if (subStrings.Length > 0)
                        {
                            return RingIdDict.FirstOrDefault(x => x.Value == subStrings[1].Trim()).Key;
                        }
                    }
                }
                else
                {
                    ServiceUtil.LogService.Write("Could not get flight ring. Output: " + lineSplit[0]);
                }
            }
            return null;
        }

        public static int ParseTelemetryCommandOutput(string output)
        {
            if (string.IsNullOrEmpty(output))
            {
                return InvalidTelemetryValue;
            }

            // Output is regkey, followed by line of format "AllowTelemetry    REG_DWORD    value"
            // Split the line to get only the value, then split at Z to get only the data
            var lineSplit = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lineSplit.Length > 1 && lineSplit[1].Contains(TelemetryRegValue))
            {
                string levelChar = lineSplit[1].Substring((lineSplit[1].Length - 1));
                if (int.TryParse(levelChar, out int levelInt))
                {
                    return levelInt;
                }
            }
            else
            {
                ServiceUtil.LogService.Write("Could not get telemetry level. Output: " + output);
            }

            return DefaultTelemetryValue;
        }

        public static async Task<int> GetTelemetryLevelAsync(string username, string password)
        {
            var output = await RunCommandWithOutputAsync(username, password, "reg query " + TelemetryRegKey);
            if (output != null)
            {
                return ParseTelemetryCommandOutput(output.Output);
            }
            return InvalidTelemetryValue;
        }

        public static async Task<bool> SetTelemetryLevelAsync(string username, string password, int newLevel)
        {
            if (newLevel < 0 || newLevel > 3)
            {
                throw new ArgumentException("newLevel");
            }

            try
            {
                var command = string.Format(RegAddAllowTelemetryFormat, newLevel);
                var output = await RunCommandWithOutputAsync(username, password, command);
                if (output != null)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.WriteException(ex);
            }

            return false;
        }

        public static async Task<RunCommandWithOutput> SetFlightRingAsync(string username, string password, string ring)
        {
            ServiceUtil.LogService.Write($"Set FlightRing to: {ring}");
            if (ring == "Canary WSD Servicing")
            {
                await RunCommandWithOutputAsync(username, password, "Reg add " + FlightRingRegKey + " /v IsBuildFlightingEnabled /t REG_DWORD /d 1 /f");
            }
            if (RingIdDict.ContainsKey(ring))
            {
                return await RunCommandWithOutputAsync(username, password, "Reg add " + FlightRingRegKey + " /v Ring /t REG_SZ /d " + RingIdDict[ring] + " /f");
            }
            return null;
        }


        public static string TelemetryLevelToFriendlyName(int level)
        {
            switch (level)
            {
                case 0:
                    return $"{Common.GetLocalizedText("TelemetryLevel0Text")} (0)";
                case 1:
                    return $"{Common.GetLocalizedText("TelemetryLevel1Text")} (1)";
                case 2:
                    return $"{Common.GetLocalizedText("TelemetryLevel2Text")} (2)";
                case 3:
                    return $"{Common.GetLocalizedText("TelemetryLevel3Text")} (3)";
                default:
                    return string.Format(Common.GetLocalizedText("TelemetryLevelUnknownFormat"), level.ToString());
            }
        }

        public static string[] GetRings()
        {
            return RingIdDict.Keys.ToArray();
        }

        #endregion

        #region Windows Update

        public static async Task<WindowsUpdateStatus> GetWindowsUpdateStatusAsync(string username, string password)
        {
            try
            {
                string uri = BaseUri + "/api/iot/windowsupdate/status";

                ServiceUtil.LogService.Write("GET URI: " + uri);

                var result = await WebUtil.SendRequestAsync(uri, username, password, HttpMethod.Get);
                if (result == null)
                {
                    ServiceUtil.LogService.Write("SendRequestAsync failed");
                    return null;
                }

                return JsonConvert.DeserializeObject<WindowsUpdateStatus>(result);
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.WriteException(ex);
            }

            return null;
        }

        public static string GetUpdateState(int id)
        {
            switch (id)
            {
                case 1:
                    return Common.GetLocalizedText("WindowsUpdateState1");
                case 2:
                    return Common.GetLocalizedText("WindowsUpdateState2");
                case 3:
                    return Common.GetLocalizedText("WindowsUpdateState3");
                default:
                    return Common.GetLocalizedText("WindowsUpdateStateDefault");
            }
        }

        #endregion

        public static string Base64Encode(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        public static string Base64Decode(string data)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(data));
        }
    }

    #region WDP Classes

    public class OsInfo
    {
        public string ComputerName, Language, OsEdition, OsEditionId, OsVersion, Platform;
    }

    public class RunCommandWithOutput
    {
        public string Output;
        public int ExitCode;
    }

    public class WindowsUpdateStatus
    {
        public string LastCheckTime, LastUpdateTime, UpdateStatusMessage;
        public int UpdateState;
    }

    public class Packages
    {
        public bool HolographicAvailable;
        public InstalledPackage[] InstalledPackages;
    }

    public class InstalledPackage
    {
        public int AppListEntry;
        public bool CanUninstall;
        public string Name, PackageFamilyName, PackageFullName, PackageRelativeId, Publisher;
        public int PackageOrigin;
        public AppVersion Version;
    }

    public class AppVersion
    {
        public int Build, Major, Minor, Revision;
        public override string ToString()
        {
            return $"{Build}.{Major}.{Minor}.{Revision}";
        }
    }

    #endregion
}
