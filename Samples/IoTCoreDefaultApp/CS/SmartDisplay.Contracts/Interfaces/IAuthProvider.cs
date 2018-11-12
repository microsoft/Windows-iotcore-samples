// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDisplay.Contracts
{
    public interface IAuthProvider
    {
        /// <summary>
        /// Gets auth token with prompt
        /// </summary>
        /// <returns>Token if successful, null otherwise</returns>
        Task<string> GetTokenAsync(string resource = null);

        /// <summary>
        /// Gets auth token silently
        /// </summary>
        /// <returns>Token if successful, null otherwise</returns>
        Task<string> GetTokenSilentAsync(string resource = null);

        /// <summary>
        /// Checks if token is valid
        /// </summary>
        bool IsTokenValid(string resource = null);

        /// <summary>
        /// Sign out of account
        /// </summary>
        Task SignOutAsync();

        /// <summary>
        /// If the provider displays a dialog to prompt for credentials, this is the timeout before the dialog automatically closes.
        /// </summary>
        int DialogTimeoutSeconds { get; }

        /// <summary>
        /// Returns properties cached from the last valid token.
        /// Currently this is only supported by WamProvider.
        /// </summary>
        Dictionary<string, string> CachedProperties { get; }

        /// <summary>
        /// Event that is fired when the provider gets a new valid token.
        /// </summary>
        event EventHandler TokenUpdate;
    }
}
