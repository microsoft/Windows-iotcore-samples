// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Utils;
using SmartDisplay.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SmartDisplay.ViewModels
{
    public class OOBEPermissionsPageVM : BaseViewModel
    {
        #region UI Properties

        public ObservableCollection<LocationDisplayListViewItem> OOBELocationListViewCollection { get; } = new ObservableCollection<LocationDisplayListViewItem>();

        public LocationDisplayListViewItem SelectedItem
        {
            get { return GetStoredProperty<LocationDisplayListViewItem>(); }
            set { SetStoredProperty(value); }
        }

        public string NextButtonText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        #endregion


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

        private SettingsProvider Settings => App.Settings;

        private NetworkPresenter _networkPresenter = new NetworkPresenter();
        private IOOBEWindowService _windowService;

        public OOBEPermissionsPageVM()
        {
            PopulateListView();
            NextButtonText = Common.GetLocalizedText("AcceptButton/Content");
        }

        public void SetUpVM(IOOBEWindowService windowService)
        {
            _windowService = windowService;
        }

        private void PopulateListView()
        {
            InvokeOnUIThread(() =>
            {
                OOBELocationListViewCollection.Clear();

                OOBELocationListViewCollection.Add(new LocationDisplayListViewItem
                {
                    IsAllowed = true,
                    Icon = "\uECAF",
                    Title = Common.GetLocalizedText("YesText"),
                    Description = Common.GetLocalizedText("LocationOOBEDescriptionOnText"),
                });

                OOBELocationListViewCollection.Add(new LocationDisplayListViewItem
                {
                    IsAllowed = false,
                    Icon = "\uF4DB",
                    Title = Common.GetLocalizedText("NoText"),
                    Description = Common.GetLocalizedText("LocationOOBEDescriptionOffText"),
                });
            });
        }

        private void SaveSettings()
        {
            if (SelectedItem != null)
            {
                Settings.IsLocationEnabled = SelectedItem.IsAllowed;
            }
        }

        /// <summary>
        /// Advances to the next OOBE page depending on which device capabilities are available
        /// </summary>
        private async void NavigateNext()
        {
            SaveSettings();

            var wiFiAvailable = _networkPresenter.WiFiIsAvailable();
            Type nextScreen = (await wiFiAvailable) ? typeof(OOBENetworkPage) : typeof(MainPage);

            _windowService.Navigate(nextScreen);
        }
    }
}
