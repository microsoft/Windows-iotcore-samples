// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Identity.Client;
using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;

namespace SmartDisplay.Identity
{
    public class MsalProvider : IAuthProvider
    {
        public int DialogTimeoutSeconds { get; } = 0;
        public Dictionary<string, string> CachedProperties { get; } = new Dictionary<string, string>();
        public event EventHandler TokenUpdate;

        private string[] _scopes;
        private PublicClientApplication _identityClientApp;
        private DateTimeOffset _expiration;

        private string _tokenForUser;
        private string TokenForUser
        {
            get => _tokenForUser;
            set
            {
                if (value != _tokenForUser)
                {
                    _tokenForUser = value;
                    TokenUpdate?.Invoke(this, new EventArgs());
                }
                else
                {
                    // Assign in case string pointer changes but the contents are the same
                    _tokenForUser = value;
                }
            }
        }

        public MsalProvider(string clientId, string[] scopes)
        {
            _identityClientApp = new PublicClientApplication(clientId);
            _scopes = scopes;
        }

        #region IAuthProvider

        public async Task<string> GetTokenAsync(string unused = null)
        {
            ServiceUtil.LogService.Write("Getting token with UI...");

            try
            {
                var authResult = await _identityClientApp.AcquireTokenAsync(_scopes);
                TokenForUser = authResult.AccessToken;
                _expiration = authResult.ExpiresOn;

                return TokenForUser;
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.Write(ex.ToString(), LoggingLevel.Error);
            }

            return null;
        }

        public async Task<string> GetTokenSilentAsync(string unused = null)
        {
            var user = _identityClientApp.Users.FirstOrDefault();
            if (user == null)
            {
                ServiceUtil.LogService.Write("No user found");
                return null;
            }

            ServiceUtil.LogService.Write("Getting token silently...");

            try
            {
                var authResult = await _identityClientApp.AcquireTokenSilentAsync(_scopes, user);
                TokenForUser = authResult.AccessToken;
                _expiration = authResult.ExpiresOn;

                return TokenForUser;
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.Write(ex.ToString(), LoggingLevel.Warning);
            }

            return null;
        }

        public bool IsTokenValid(string unused = null)
        {
            if (TokenForUser == null || _expiration < DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return Task.Run(() => GetTokenSilentAsync()).Result != null;
            }

            return true;
        }

        /// <summary>
        /// Signs the user out of the service.
        /// </summary>
        public Task SignOutAsync()
        {
            return Task.Run(() =>
            {
                ServiceUtil.LogService.Write("Signing out all users");
                foreach (var user in _identityClientApp.Users)
                {
                    _identityClientApp.Remove(user);
                }
                TokenForUser = null;
            });
        }

        #endregion
    }

}
