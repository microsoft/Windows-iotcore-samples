// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Identity;
using SmartDisplay.Utils;
using SmartDisplay.Utils.Converters;
using SmartDisplay.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartDisplay.ViewModels
{
    public class AuthenticationPageVM : BaseViewModel
    {
        #region UI Properties

        public string PageDescription
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }

        public bool IsMsalChecked
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (SetStoredProperty(value) &&
                    AppService.Settings is SettingsProvider settings)
                {
                    settings.AppUseMsal = value;
                }
            }
        }

        public bool IsMsalCheckboxVisible
        {
            get { return GetStoredProperty<bool>(); }
            private set { SetStoredProperty(value); }
        }

        public string MsaSignInButtonText
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }

        public string AadSignInButtonText
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }

        public string MsaDisplayName
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }

        public string AadDisplayName
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }

        public BitmapImage MsaBitmap
        {
            get { return GetStoredProperty<BitmapImage>(); }
            private set { SetStoredProperty(value); }
        }

        public BitmapImage AadBitmap
        {
            get { return GetStoredProperty<BitmapImage>(); }
            private set { SetStoredProperty(value); }
        }

        public string MsaInfoText
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }

        public bool IsMsaInfoPanelVisible
        {
            get { return GetStoredProperty<bool>(); }
            private set { SetStoredProperty(value); }
        }

        public string AadInfoText
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }

        public bool IsAadInfoPanelVisible
        {
            get { return GetStoredProperty<bool>(); }
            private set { SetStoredProperty(value); }
        }

        public string AadTitleText
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }

        public string AadDescriptionText
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }

        public bool IsAadPanelVisible
        {
            get { return GetStoredProperty<bool>(); }
            private set { SetStoredProperty(value); }
        }

        public bool IsClearAccountsButtonVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        #endregion

        #region Commands

        private RelayCommand _msaSignInCommand;
        public ICommand MsaSignInCommand
        {
            get
            {
                return _msaSignInCommand ??
                    (_msaSignInCommand = new RelayCommand(async unused =>
                    {
                        var msaProvider = AppService.AuthManager.GetProvider(ProviderNames.MsaProviderKey);

                        if (!msaProvider.IsTokenValid())
                        {
                            string token = null;
                            if (msaProvider is WamProvider)
                            {
                                var wamProvider = msaProvider as WamProvider;

                                string title = MsaSignInTitle;
                                string message = string.Format(DialogFormat, wamProvider.DialogTimeoutSeconds);

                                token = await DoSignInWithDialogAsync(msaProvider, MsaString, title, message);
                            }
                            else
                            {
                                token = await DoSignInAsync(msaProvider, MsaString);
                            }
                        }
                        else if (msaProvider.IsTokenValid() &&
                            MsaSignInButtonText == SignOutText)
                        {
                            await DoSignOutAsync(msaProvider, MsaString);
                        }

                        AppService.AuthManager.RefreshAuthStatus();
                    }));
            }
        }

        private RelayCommand _aadSignInCommand;
        public ICommand AadSignInCommand
        {
            get
            {
                return _aadSignInCommand ??
                    (_aadSignInCommand = new RelayCommand(async unused =>
                    {
                        // Return immediately if the panel isn't visible
                        if (!IsAadPanelVisible)
                        {
                            return;
                        }

                        var aadProvider = GetAadProvider(IsMsalChecked);
                        string token = null;

                        if (!aadProvider.IsTokenValid())
                        {
                            if (aadProvider is WamProvider)
                            {
                                var wamProvider = aadProvider as WamProvider;

                                string title = AadSignInTitle;
                                string message = string.Format(DialogFormat, wamProvider.DialogTimeoutSeconds);

                                token = await DoSignInWithDialogAsync(aadProvider, AadString, title, message);
                            }
                            else
                            {
                                token = await DoSignInAsync(aadProvider, AadString);
                            }
                        }
                        else if (aadProvider.IsTokenValid() &&
                            AadSignInButtonText == SignOutText)
                        {
                            await DoSignOutAsync(aadProvider, AadString);
                        }

                        AppService.AuthManager.RefreshAuthStatus();
                    }));
            }
        }

        #endregion

        #region Localized Strings

        private string SignInText { get; } = Common.GetLocalizedText("AuthSignInText");
        private string SignOutText { get; } = Common.GetLocalizedText("AuthSignOutText");
        private string CancelText { get; } = Common.GetLocalizedText("AuthCancelText");
        private string MsaString { get; } = Common.GetLocalizedText("AuthMsaString");
        private string AadString { get; } = Common.GetLocalizedText("AuthAadString");
        private string DefaultAadDescription { get; } = Common.GetLocalizedText("AuthDefaultAadDescription");
        private string DialogFormat { get; } = Common.GetLocalizedText("AuthDialogFormat");
        private string AadPageDescription { get; } = Common.GetLocalizedText("AuthAadPageDescription");
        private string MsaOnlyPageDescription { get; } = Common.GetLocalizedText("AuthMsaOnlyPageDescription");
        private string AadV1Description { get; } = Common.GetLocalizedText("AuthAadV1Description");
        private string AadV2Description { get; } = Common.GetLocalizedText("AuthAadV2Description");
        private string AadTitle { get; } = Common.GetLocalizedText("AuthAadTitle");
        private string AadTitleVersionedFormat { get; } = Common.GetLocalizedText("AuthAadTitleVersionedFormat");
        private string MsaSignInTitle { get; } = Common.GetLocalizedText("AuthMsaSignInTitle");
        private string MsaSignInMessage { get; } = Common.GetLocalizedText("AuthMsaSignInTitle");
        private string AadSignInTitle { get; } = Common.GetLocalizedText("AuthAadSignInTitle");
        private string AadSignInMessage { get; } = Common.GetLocalizedText("AuthAadSignInMessage");
        private string LoadingSigningInText { get; } = Common.GetLocalizedText("AuthLoadingSigningInText");
        private string SignInSuccessfulFormatText { get; } = Common.GetLocalizedText("AuthSignInSuccessfulFormatText");
        private string SignInFailedFormatText { get; } = Common.GetLocalizedText("AuthSignInFailedFormatText");
        private string LoadingSigningOutText { get; } = Common.GetLocalizedText("AuthLoadingSigningOutText");
        private string SignOutSuccessfulFormatText { get; } = Common.GetLocalizedText("AuthSignOutSuccessfulFormatText");
        private string ClearAccountsPromptFormat { get; } = Common.GetLocalizedText("ClearAccountsPromptFormat");
        private string ClearAccountsButtonContent { get; } = Common.GetLocalizedText("ClearAccountsButton/Content");
        private string ClearAccountsSuccessText { get; } = Common.GetLocalizedText("ClearAccountsSuccessText");
        private string ClearAccountsFailText { get; } = Common.GetLocalizedText("ClearAccountsFailText");
        private string NoAccountsText { get; } = Common.GetLocalizedText("NoAccountsText");
        private string ClearingProgressText { get; } = Common.GetLocalizedText("ClearingProgressText");

        #endregion

        private AuthenticationManager AuthManager => AppService?.AuthManager as AuthenticationManager;
        private SettingsProvider Settings => AppService?.Settings as SettingsProvider;

        public void SetUpVM()
        {
            // This page requires internet
            if (!AppService.IsConnectedInternet())
            {
                AppService.DisplayNoInternetDialog(typeof(TilePage));
                return;
            }

            PopulateCommandBar();

            IsMsalChecked = Settings?.AppUseMsal ?? false;

            IsAadPanelVisible = AppService.AuthManager.IsAadProviderAvailable();
            if (IsAadPanelVisible)
            {
                PageDescription = AadPageDescription;
            }
            else
            {
                PageDescription = MsaOnlyPageDescription;
            }

            AppService.AuthManager.TokenStatusChanged += AuthManager_TokenStatusChanged;

            // Stop the timer since we're doing auth related actions
            // on this page
            AuthManager?.StopStatusTimer();

            var updateUiTask = UpdateUiAsync();
        }

        public void TearDownVM()
        {
            AuthManager?.StartStatusTimer();
        }

        public async Task UpdateUiAsync()
        {
            ShowLoadingPanel();
            try
            {
                var msaProvider = AppService.AuthManager.GetProvider(ProviderNames.MsaProviderKey);
                var aadProvider = GetAadProvider(IsMsalChecked);

                bool isMsaValid = msaProvider.IsTokenValid();
                bool isAadValid = (aadProvider != null) ? aadProvider.IsTokenValid() : false;

                PageService.SetSignInStatus(isMsaValid, isAadValid);

                await UpdateMsaUiAsync(isMsaValid);
                await UpdateAadUiAsync(isAadValid);

                IsMsalCheckboxVisible = !isAadValid;

                var accounts = await AuthenticationManager.GetWindowsAccountsListAsync();
                IsClearAccountsButtonVisible = (accounts != null && accounts.Length > 0);
            }
            finally
            {
                HideLoadingPanel();
            }
        }

        private IAuthProvider GetAadProvider(bool useV2)
        {
            return AuthManager.GetProvider((useV2) ? ProviderNames.AadV2ProviderKey : ProviderNames.AadV1ProviderKey);
        }

        private async void AuthManager_TokenStatusChanged(object sender, TokenStatusEventArgs e)
        {
            await UpdateUiAsync();
        }

        private async Task UpdateMsaUiAsync(bool isMsaValid)
        {
            try
            {
                string nl = Environment.NewLine;

                if (isMsaValid)
                {
                    MsaSignInButtonText = SignOutText;

                    var provider = AppService.AuthManager.GetProvider(ProviderNames.MsaProviderKey);
                    var token = await provider.GetTokenSilentAsync();

                    if (token != null)
                    {
                        InvokeOnUIThread(async () =>
                        {
                            var bitmap = await LiveApiUtil.GetPictureAsync(token);
                            if (bitmap != null)
                            {
                                MsaBitmap = bitmap;
                            }
                        });

                        var userInfo = await LiveApiUtil.GetUserInfoAsync(token);
                        if (userInfo != null)
                        {
                            MsaInfoText = userInfo.name;
                            MsaDisplayName = userInfo.name;
                        }

                        IsMsaInfoPanelVisible = true;
                    }
                }
                else
                {
                    MsaSignInButtonText = SignInText;
                    IsMsaInfoPanelVisible = false;
                }
            }
            catch (Exception ex)
            {
                AppService.LogService.WriteException(ex);
            }
        }

        private async Task UpdateAadUiAsync(bool isAadValid)
        {
            try
            {
                // Return immediately if the panel isn't visible
                if (!IsAadPanelVisible)
                {
                    return;
                }

                string nl = Environment.NewLine;

                if (isAadValid)
                {
                    AadSignInButtonText = SignOutText;

                    var provider = GetAadProvider(IsMsalChecked);
                    using (var graphHelper = new GraphHelper(provider))
                    {
                        var userInfo = await graphHelper.GetUserAsync();
                        if (userInfo != null)
                        {
                            AadInfoText = userInfo.DisplayName + nl +
                                userInfo.Mail + nl +
                                userInfo.JobTitle + nl +
                                userInfo.OfficeLocation + nl +
                                userInfo.BusinessPhones.FirstOrDefault();
                            AadDisplayName = userInfo.DisplayName;
                        }
                        else
                        {
                            AadInfoText = string.Empty;
                        }
                    }

                    InvokeOnUIThread(async () =>
                    {
                        using (var graphHelper = new GraphHelper(provider))
                        {
                            var bitmap = await graphHelper.GetPhotoAsync();
                            if (bitmap != null)
                            {
                                AadBitmap = bitmap;
                            }
                        }
                    });

                    string version = (provider is WamProvider) ? "v1" : "v2";

                    AadTitleText = string.Format(AadTitleVersionedFormat, version);
                    AadDescriptionText = (provider is WamProvider) ? AadV1Description : AadV2Description;
                    IsAadInfoPanelVisible = true;
                }
                else
                {
                    AadTitleText = AadTitle;
                    AadDescriptionText = DefaultAadDescription;
                    AadSignInButtonText = SignInText;
                    IsAadInfoPanelVisible = false;
                }
            }
            catch (Exception ex)
            {
                AppService.LogService.WriteException(ex);
            }
        }

        private void PopulateCommandBar()
        {
            if (PageService.AddCommandBarButton(CommandBarButton.Separator) is AppBarSeparator separator)
            {
                separator.SetBinding(UIElement.VisibilityProperty, new Binding
                {
                    Converter = new BooleanToVisibilityConverter(),
                    Path = new PropertyPath("IsClearAccountsButtonVisible"),
                    Source = this,
                    Mode = BindingMode.OneWay
                });
            }

            if (PageService.AddCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Clear),
                Label = ClearAccountsButtonContent,
                Handler = ClearAccountsButton_Click,
            }) is AppBarButton appBarButton)
            {
                // Bind button to property so that it's hidden when there are no accounts to clear, or 
                // ProcessLauncher is not enabled
                appBarButton.SetBinding(UIElement.VisibilityProperty, new Binding
                {
                    Converter = new BooleanToVisibilityConverter(),
                    Path = new PropertyPath("IsClearAccountsButtonVisible"),
                    Source = this,
                    Mode = BindingMode.OneWay
                });
            }
        }

        private async void ClearAccountsButton_Click(object sender, RoutedEventArgs args)
        {
            var accounts = await AuthenticationManager.GetWindowsAccountsListAsync();
            if (accounts != null && accounts.Length > 0)
            {
                var accountList = string.Empty;
                foreach (var account in accounts)
                {
                    accountList += account + Environment.NewLine;
                }

                if (await AppService?.YesNoAsync(ClearAccountsButtonContent,
                    string.Format(ClearAccountsPromptFormat, accountList.Trim())))
                {
                    try
                    {
                        PageService?.ShowLoadingPanel(ClearingProgressText);
                        PageService?.ShowNotification((await AppService.AuthManager.ClearAllWamAccountsAsync()) ?
                            ClearAccountsSuccessText :
                            ClearAccountsFailText);

                        AppService.AuthManager.RefreshAuthStatus();
                    }
                    finally
                    {
                        PageService?.HideLoadingPanel();
                    }
                }
            }
            else
            {
                PageService?.ShowNotification(NoAccountsText);
            }
        }

        #region Sign In/Out Methods

        /// <summary>
        /// Creates a dialog before launching into the Sign In sequence
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="providerName"></param>
        /// <param name="dialogTitle"></param>
        /// <param name="dialogMessage"></param>
        private async Task<string> DoSignInWithDialogAsync(IAuthProvider provider, string providerName, string dialogTitle, string dialogMessage)
        {
            var taskCompletionSource = new TaskCompletionSource<string>();

            DialogButton primaryButton = new DialogButton(SignInText, async (Sender, ClickEventsArgs) =>
            {
                var result = await DoSignInAsync(provider, providerName);
                taskCompletionSource.SetResult(result);
            });

            DialogButton closeButton = new DialogButton(CancelText, (Sender, ClickEventsArgs) =>
            {
                taskCompletionSource.SetResult(null);
            });

            await AppService.DisplayDialogAsync(dialogTitle, dialogMessage, primaryButton, null, closeButton);

            return await taskCompletionSource.Task;
        }

        private async Task<string> DoSignInAsync(IAuthProvider provider, string providerName = "")
        {
            if (provider == null)
            {
                AppService.LogService.Write("No provider specified", Windows.Foundation.Diagnostics.LoggingLevel.Warning);
                return null;
            }

            ShowLoadingPanel(LoadingSigningInText);

            try
            {
                var token = await AppService.RunExclusiveTaskAsync(() => provider.GetTokenAsync());
                if (token != null)
                {
                    PageService.ShowNotification(string.Format(SignInSuccessfulFormatText, providerName));
                    return token;
                }
                else
                {
                    PageService.ShowNotification(string.Format(SignInFailedFormatText, providerName));
                }
            }
            finally
            {
                HideLoadingPanel();
            }

            return null;
        }

        private async Task<bool> DoSignOutAsync(IAuthProvider provider, string providerName = "")
        {
            if (provider == null)
            {
                AppService.LogService.Write("No provider specified", Windows.Foundation.Diagnostics.LoggingLevel.Warning);
                return false;
            }

            ShowLoadingPanel(LoadingSigningOutText);

            try
            {
                await provider.SignOutAsync();

                PageService.ShowNotification(string.Format(SignOutSuccessfulFormatText, providerName));

                return true;
            }
            finally
            {
                HideLoadingPanel();
            }
        }

        #endregion
    }
}
