// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using SmartDisplay.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Windows.UI.Xaml;

namespace SmartDisplay.ViewModels
{
    public class OOBEWelcomePageVM : BaseViewModel
    {
        #region UI properties
        
        public bool IsChooseDefaultLanguageStackVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public IReadOnlyList<string> LanguagesListViewItems
        {
            get { return GetStoredProperty<IReadOnlyList<string>>(); }
            set { SetStoredProperty(value); }
        }

        public string LanguagesListViewSelectedItem
        {
            get { return GetStoredProperty<string>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    UpdateLanguage();
                }
            }
        }

        public string DeviceName
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public string IPv4Address
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public string OSVersion
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public double DefaultLanguageProgressValue
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public double DefaultLanguageProgressSmallChange
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public double DefaultLanguageProgressMaximum
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public LanguageManager LanguageManager { get; } = LanguageManager.GetInstance();
        #endregion

        private LanguageManager _languageManager;
        private NetworkPresenter _networkPresenter = new NetworkPresenter();
        private IOOBEWindowService _windowService;


        public OOBEWelcomePageVM() : base()
        {
            _languageManager = LanguageManager.GetInstance();
        }

        public void SetUpVM(IOOBEWindowService windowService)
        {
            _windowService = windowService;

            SetUpLanguages();
            UpdateBoardInfo();
            UpdateNetworkInfo();
        }

        #region Commands
        private RelayCommand _nextButtonCommand;
        public ICommand NextButtonCommand
        {
            get
            {
                return _nextButtonCommand ??
                    (_nextButtonCommand = new RelayCommand(unused =>
                    {
                        NavigateNext();
                    }));
            }
        }
        #endregion

        private void SetUpLanguages()
        {
            LanguagesListViewItems = _languageManager.LanguageDisplayNames;
            LanguagesListViewSelectedItem = LanguageManager.GetCurrentLanguageDisplayName();

            SetPreferences();
        }

        private void SetPreferences()
        {
            if (LanguagesListViewSelectedItem == null)
            {
                return;
            }
            else
            {
                string selectedLanguage = LanguagesListViewSelectedItem;
                                
                if (LanguageManager.GetCurrentLanguageDisplayName().Equals(selectedLanguage))
                {
                    // Do Nothing
                    return;
                }

                // Check if selected language is part of ffu
                var newLang = _languageManager.CheckUpdateLanguage(selectedLanguage);

                // Update
                var langReturned = _languageManager.UpdateLanguage(selectedLanguage);

                if (LanguageManager.GetDisplayNameFromLanguageTag(newLang.LanguageTag).Equals(selectedLanguage))
                {
                    // Reload the page
                    _windowService.ReloadCurrentPage();
                }
            }
        }

        private void UpdateLanguage()
        {
            if (LanguageManager.GetCurrentLanguageDisplayName().Equals(LanguagesListViewSelectedItem))
            {
                // Do Nothing
                return;
            }
            SetPreferences();
        }

        private void UpdateBoardInfo()
        {
            if (!ulong.TryParse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion, out ulong version))
            {
                OSVersion = _languageManager["OSVersionNotAvailable"];
            }
            else
            {
                OSVersion = Common.GetOSVersionString();
            }
        }

        private void UpdateNetworkInfo()
        {
            DeviceName = DeviceInfoPresenter.GetDeviceName();
            IPv4Address = NetworkPresenter.GetCurrentIpv4Address();
        }

        /// <summary>
        /// Advances to the next OOBE page
        /// </summary>
        public void NavigateNext()
        {
            SetPreferences();

            Type nextPage;
#if FORCE_OOBE_DIAGNOSTICS_SCREEN
            nextPage = typeof(OOBEPrivacyPage);
#else
            if (DevicePortalUtil.IsDevicePortalEnabled())
            {
                nextPage = typeof(OOBEPrivacyPage);
            }
            else
            {
                nextPage = typeof(OOBEPermissionsPage);
            }
#endif

            _windowService.Navigate(nextPage);
        }
    }
}
