// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Identity;
using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.System.Threading;

namespace SmartDisplay.Identity
{
    public class AuthenticationManager : IAuthManager
    {
        public event TokenStatusChangedEventHandler TokenStatusChanged;

        private Dictionary<string, IAuthProvider> _providers = new Dictionary<string, IAuthProvider>
        {
            { ProviderNames.MsaProviderKey, new WamProvider(
                                    AuthConstants.MsaAuthority,
                                    AuthConstants.MsaScope,
                                    null,
                                    AuthConstants.MsaClientId,
                                    ProviderNames.MsaProviderKey,
                                    dialogTimeoutSeconds: 2 * 60)},
        };

        private Dictionary<string, bool> _tokenCache = new Dictionary<string, bool>();
        private object _tokenCacheLock = new object();
        private ThreadPoolTimer _authStatusTimer;

        #region IAuthManager

        public bool AddProvider(string key, IAuthProvider provider)
        {
            if (!string.IsNullOrEmpty(key) && !_providers.ContainsKey(key))
            {
                _providers.Add(key, provider);
                return true;
            }

            return false;
        }

        public bool RemoveProvider(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                return _providers.Remove(key);
            }

            return false;
        }

        public IAuthProvider GetProvider(string providerKey)
        {
            if (_providers.TryGetValue(providerKey, out IAuthProvider provider))
            {
                return provider;
            }
            else
            {
                App.LogService.Write($"No provider found for {providerKey}");
                return null;
            }
        }

        public IAuthProvider GetGraphProvider()
        {
            return GetProvider((App.Settings.AppUseMsal) ? ProviderNames.AadV2ProviderKey : ProviderNames.AadV1ProviderKey);
        }

        #endregion

        /// <summary>
        /// Updates status immediately and then performs additional checks periodically.
        /// </summary>
        /// <param name="periodSeconds"></param>
        public void StartStatusTimer(int periodSeconds = 3600)
        {
            StopStatusTimer();
            RefreshAuthStatus();
            _authStatusTimer = ThreadPoolTimer.CreatePeriodicTimer(RefreshAuthStatusTimerCallback, TimeSpan.FromSeconds(periodSeconds));
        }

        public void StopStatusTimer()
        {
            if (_authStatusTimer != null)
            {
                _authStatusTimer.Cancel();
                _authStatusTimer = null;
            }
        }

        /// <summary>
        /// Rechecks the status of the providers for changes
        /// </summary>
        public void RefreshAuthStatus()
        {
            lock (_tokenCacheLock)
            {
                foreach (var kvp in _providers)
                {
                    string key = kvp.Key;

                    if (!_tokenCache.ContainsKey(key))
                    {
                        _tokenCache[key] = false;
                    }

                    bool wasValid = _tokenCache[key];
                    bool isValid = _providers[key].IsTokenValid();

                    if (isValid != wasValid)
                    {
                        _tokenCache[key] = isValid;
                        TokenStatusChanged?.Invoke(this, new TokenStatusEventArgs()
                        {
                            ProviderKey = key,
                            OldValue = wasValid,
                            NewValue = isValid
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Method overload used as a ThreadPoolTimer callback.
        /// </summary>
        /// <param name="timer"></param>
        private void RefreshAuthStatusTimerCallback(ThreadPoolTimer timer)
        {
            RefreshAuthStatus();
        }

        public bool IsAadProviderAvailable()
        {
            return (_providers.ContainsKey(ProviderNames.AadV1ProviderKey) || _providers.ContainsKey(ProviderNames.AadV2ProviderKey));
        }

        public async Task<bool> ClearAllWamAccountsAsync()
        {
            // Clear tokens/sessions
            foreach (var kvp in _providers)
            {
                if (kvp.Value is WamProvider wam)
                {
                    await wam.SignOutAsync();
                }
            }

            // Clear account from Windows
            return await ClearAllWindowsAccountsAsync();
        }

        public static async Task<bool> ClearWindowsAccountAsync(string username)
        {
            try
            {
                var accounts = await GetWindowsAccountsListAsync();

                var output = await ProcessLauncherUtil.RunCommandLineAsync(@"c:\windows\system32\iotsettings.exe" + $" -del account {username}");
                if (output != null)
                {
                    ServiceUtil.LogService.Write(output.ToString());
                    return output.Result.ExitCode == 0;
                }
            }
            catch (Exception e)
            {
                ServiceUtil.LogService.Write(e.Message, LoggingLevel.Error);
            }

            return false;
        }

        public static async Task<bool> ClearAllWindowsAccountsAsync()
        {
            return await ClearWindowsAccountAsync("all");
        }

        public static async Task<string[]> GetWindowsAccountsListAsync()
        {
            try
            {
                var output = await ProcessLauncherUtil.RunCommandLineAsync(@"c:\windows\system32\iotsettings.exe -list account");
                if (output != null && output.Output != null)
                {
                    ServiceUtil.LogService.Write(output.ToString());
                    return output.Output?.Split(" ", StringSplitOptions.RemoveEmptyEntries).Where(x => x.Contains("@")).ToArray();
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.WriteException(ex);
            }

            return null;
        }
    }
}
