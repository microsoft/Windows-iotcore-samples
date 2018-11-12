// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Threading.Tasks;

namespace SmartDisplay.Contracts
{
    public interface IAuthManager
    {
        event TokenStatusChangedEventHandler TokenStatusChanged;

        IAuthProvider GetProvider(string providerKey);

        bool AddProvider(string key, IAuthProvider provider);
        
        bool RemoveProvider(string key);

        /// <summary>
        /// Gets an IAuthProvider for graph.microsoft.com based on the app settings (AADv1 or AADv2).
        /// </summary>
        IAuthProvider GetGraphProvider();

        /// <summary>
        /// Fires TokenStatusChanged if the auth status changes.
        /// </summary>
        void RefreshAuthStatus();

        bool IsAadProviderAvailable();

        Task<bool> ClearAllWamAccountsAsync();
    }

    public enum AuthMethod
    {
        Msal,
        Wam
    }

    public enum ProviderType
    {
        Msa = 0,
        AadV1,
        AadV2,
    }

    public static class ProviderNames
    {
        public const string MsaProviderKey = "MSA";
        public const string AadV1ProviderKey = "AADv1";
        public const string AadV2ProviderKey = "AADv2";
    }

    public enum AuthStatus
    {
        SignedOut = 0,
        SignedIn,
        UserInteractionRequired
    }

    public class TokenStatusEventArgs : EventArgs
    {
        public string ProviderKey;
        public bool OldValue;
        public bool NewValue;
    }

    public delegate void TokenStatusChangedEventHandler(object sender, TokenStatusEventArgs e);
}
