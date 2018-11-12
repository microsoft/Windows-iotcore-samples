// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Security.Authentication.Web;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI.ApplicationSettings;

namespace SmartDisplay.Identity
{
    public class WamProvider : IAuthProvider
    {
        public int DialogTimeoutSeconds { get; }

        public Dictionary<string, string> CachedProperties { get; private set; } = new Dictionary<string, string>();

        public event EventHandler TokenUpdate;

        private const string ProviderId = "https://login.microsoft.com";
        private const string DefaultResource = "DEFAULT_RESOURCE";

        private class TokenInfo
        {
            public DateTime ExpiresOn;
            public string Token;
        }

        private static readonly ApplicationDataContainer _appSettings = ApplicationData.Current.RoamingSettings;
        private Dictionary<string, TokenInfo> _tokens = new Dictionary<string, TokenInfo>();
        private string _authority;
        public string _scope;
        public string _resource;
        private string _clientId;
        private string _userIdKey;
        private bool _isSignedIn;

        public WamProvider(
            string authority,
            string scope,
            string resource,
            string clientId,
            string authName,
            int dialogTimeoutSeconds
            )
        {
            _authority = authority;
            _scope = scope;
            _resource = resource;
            _clientId = clientId;
            _userIdKey = authName + "_UserId";
            DialogTimeoutSeconds = dialogTimeoutSeconds;

            Debug.WriteLine("ms-appx-web://Microsoft.AAD.BrokerPlugIn/" + WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper());
        }

        #region IAuthProvider

        public async Task<string> GetTokenAsync(string resource = null)
        {
            if (resource == null)
            {
                resource = _resource;
            }

            var wtrr = await SignInAsync(resource);
            if (wtrr != null && wtrr.ResponseStatus == WebTokenRequestStatus.Success)
            {
                return GetTokenFromResult(wtrr);
            }

            return null;
        }

        public async Task<string> GetTokenSilentAsync(string resource = null)
        {
            if (resource == null)
            {
                resource = _resource;
            }

            var userID = GetSavedAccountId();

            if (userID != null)
            {
                var provider = await GetProvider();
                var userAccount = await WebAuthenticationCoreManager.FindAccountAsync(provider, (string)userID);
                if (userAccount != null)
                {
                    try
                    {
                        WebTokenRequest wtr = new WebTokenRequest(provider, _scope, _clientId);
                        if (resource != null)
                        {
                            wtr.Properties.Add("resource", resource);
                        }

                        var wtrr = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(wtr, userAccount);

                        SaveToken(wtrr, resource);

                        return GetTokenFromResult(wtrr);
                    }
                    catch (Exception ex)
                    {
                        ServiceUtil.LogService.Write("SignInSilent: " + ex.Message, LoggingLevel.Error);
                    }
                }
            }

            return null;
        }

        public bool IsTokenValid(string resource = null)
        {
            if (resource == null)
            {
                resource = _resource ?? DefaultResource;
            }

            // MSA doesn't provide an expiration date/time, so we 
            // need to keep track of "signed out" state
            if (!_isSignedIn)
            {
                return false;
            }

            if (_tokens.TryGetValue(resource, out var tokenInfo))
            {
                if (tokenInfo.Token == null || tokenInfo.ExpiresOn < DateTime.Now)
                {
                    // MSA doesn't provide expiration date/time so we need to actually
                    // check if we can get the token
                    return Task.Run(() => GetTokenSilentAsync(resource)).Result != null;
                }
                return true;
            }

            return false;
        }

        public async Task SignOutAsync()
        {
            _isSignedIn = false;

            ClearTokens();

            var webAccountId = GetSavedAccountId();
            if (webAccountId != null)
            {
                ServiceUtil.LogService.Write("Signing out of account: " + webAccountId);

                var provider = await GetProvider();
                var userAccount = await WebAuthenticationCoreManager.FindAccountAsync(provider, webAccountId);
                if (userAccount != null)
                {
                    await userAccount.SignOutAsync(_clientId);
                }

                ClearSavedAccountId();
            }
        }

        #endregion

        private async Task<WebTokenRequestResult> SignInAsync(string resource)
        {
            var taskCompletionSource = new TaskCompletionSource<WebTokenRequestResult>();

            TypedEventHandler<AccountsSettingsPane, AccountsSettingsPaneCommandsRequestedEventArgs> AccountCommandsRequestedHandler = null;
            AccountCommandsRequestedHandler = async (s, e) =>
            {
                Debug.WriteLine("AccountCommandsRequestedHandler");

                AccountsSettingsPane.GetForCurrentView().AccountCommandsRequested -= AccountCommandsRequestedHandler;

                // In order to make async calls within this callback, the deferral object is needed
                AccountsSettingsPaneEventDeferral deferral = e.GetDeferral();
                if (deferral != null)
                {
                    // The Microsoft account provider is always present in Windows 10 devices, even IoT Core, as is the Azure AD plugin.
                    var providerCommand = new WebAccountProviderCommand(await GetProvider(), async (command) =>
                    {
                        Debug.WriteLine("WebAccountProviderCommandInvokedHandler");

                        try
                        {
                            WebTokenRequest wtr = new WebTokenRequest(command.WebAccountProvider, _scope, _clientId);
                            if (resource != null)
                            {
                                wtr.Properties.Add("resource", resource);
                            }

                            var wtrr = await RequestTokenWithTimeout(wtr);
                            SaveToken(wtrr, resource);

                            taskCompletionSource.SetResult(wtrr);
                        }
                        catch (Exception ex)
                        {
                            ServiceUtil.LogService.Write("Web Token Request Error: " + ex.Message, LoggingLevel.Error);
                            taskCompletionSource.SetResult(null);
                        }
                    });

                    e.WebAccountProviderCommands.Add(providerCommand);

                    deferral.Complete();
                }
            };

            AccountsSettingsPane.GetForCurrentView().AccountCommandsRequested += AccountCommandsRequestedHandler;
            await AccountsSettingsPane.ShowAddAccountAsync();

            return await taskCompletionSource.Task;
        }

        private static string GetTokenFromResult(WebTokenRequestResult result)
        {
            try
            {
                foreach (var data in result.ResponseData)
                {
                    if (!string.IsNullOrWhiteSpace(data.Token))
                    {
                        return data.Token;
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.Write(ex.ToString(), LoggingLevel.Warning);
            }

            return null;
        }

        private WebAccountProvider _wap;
        private async Task<WebAccountProvider> GetProvider()
        {
            if (_wap == null)
            {
                _wap = await WebAuthenticationCoreManager.FindAccountProviderAsync(ProviderId, _authority);
            }
            return _wap;
        }

        private string GetSavedAccountId()
        {
            return _appSettings.Values[_userIdKey] as string;
        }

        private void ClearSavedAccountId()
        {
            _appSettings.Values[_userIdKey] = null;
        }

        private bool SaveToken(WebTokenRequestResult wtrr, string resource)
        {
            if (wtrr.ResponseStatus == WebTokenRequestStatus.Success)
            {
                if (resource == null)
                {
                    resource = DefaultResource;
                }

                if (!_tokens.ContainsKey(resource))
                {
                    _tokens.Add(resource, new TokenInfo());
                }

                _tokens[resource].Token = GetTokenFromResult(wtrr);
                CachedProperties.Clear();

                if (wtrr.ResponseData != null)
                {
                    foreach (var data in wtrr.ResponseData)
                    {
                        CachedProperties = new Dictionary<string, string>(data.Properties);

                        if (data.Properties.TryGetValue("exp", out string unixTimeString))
                        {
                            var dt = DateTimeOffset.FromUnixTimeSeconds(Convert.ToUInt32(unixTimeString));
                            _tokens[resource].ExpiresOn = dt.DateTime.ToLocalTime();
                            ServiceUtil.LogService.Write($"Resource: {resource}, Token Expiration: {_tokens[resource].ExpiresOn}");
                        }

                        _isSignedIn = true;
                        TokenUpdate?.Invoke(this, new EventArgs());
                        return true;
                    }
                }
            }

            _isSignedIn = false;
            return false;
        }

        private void ClearTokens()
        {
            _tokens.Clear();
            CachedProperties.Clear();
        }

        private async Task<WebTokenRequestResult> RequestTokenWithTimeout(WebTokenRequest request)
        {
            // The WebTokenRequest will time out if the user does not complete in time.
            // This is because there is currently no 'close' button on IoT Core, so it
            // will prevent users from getting stuck on the sign-in page forever.
            var requestOperation = WebAuthenticationCoreManager.RequestTokenAsync(request);
            var delay = Task.Delay(TimeSpan.FromSeconds(DialogTimeoutSeconds));

            ServiceUtil.LogService.Write("Calling WebAuthenticationCoreManager.RequestTokenAsync()...");
            var taskResult = await Task.WhenAny(delay, requestOperation.AsTask());

            if (requestOperation.Status == AsyncStatus.Started)
            {
                requestOperation.Cancel();
            }

            ServiceUtil.LogService.Write("WebTokenRequestAsync.Status: " + Enum.GetName(typeof(AsyncStatus), requestOperation.Status));

            if (taskResult == delay)
            {
                ServiceUtil.LogService.Write("MSA dialog timeout");
                return null;
            }

            var tokenResult = requestOperation.GetResults();

            ServiceUtil.LogService.Write("WebTokenRequestResult: " + Enum.GetName(typeof(WebTokenRequestStatus), tokenResult.ResponseStatus));
            if (tokenResult != null)
            {
                switch (tokenResult.ResponseStatus)
                {
                    case WebTokenRequestStatus.Success:
                        ServiceUtil.LogService.Write("Successfully signed in! " + tokenResult.ResponseData[0].WebAccount.UserName);
                        _appSettings.Values[_userIdKey] = tokenResult.ResponseData[0].WebAccount.Id;
                        break;

                    default:
                        ServiceUtil.LogService.Write("ResponseStatus: " + Enum.GetName(typeof(WebTokenRequestStatus), tokenResult.ResponseStatus), LoggingLevel.Error);
                        if (tokenResult.ResponseError != null)
                        {
                            ServiceUtil.LogService.Write("Error code: " + tokenResult.ResponseError.ErrorCode, LoggingLevel.Error);
                            ServiceUtil.LogService.Write("Error message: " + tokenResult.ResponseError.ErrorMessage, LoggingLevel.Error);
                        }
                        break;
                }
            }

            return tokenResult;
        }
    }
}
