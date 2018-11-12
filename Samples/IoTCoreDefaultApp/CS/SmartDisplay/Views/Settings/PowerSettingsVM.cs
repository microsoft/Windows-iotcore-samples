// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using System;
using System.Windows.Input;
using Windows.Foundation.Diagnostics;
using Windows.System;

namespace SmartDisplay.ViewModels.Settings
{
    public class PowerSettingsVM : BaseViewModel
    {
        #region UI properties and commands

        public double Width
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        private RelayCommand _shutdownButtonCommand;
        public ICommand ShutdownButtonCommand
        {
            get
            {
                return _shutdownButtonCommand ??
                    (_shutdownButtonCommand = new RelayCommand(unused =>
                    {
                        try
                        {
                            Shutdown();
                        }
                        catch (Exception ex)
                        {
                            LogService.Write(ex.ToString(), LoggingLevel.Error);
                        }
                    }));
            }
        }

        private RelayCommand _restartButtonCommand;
        public ICommand RestartButtonCommand
        {
            get
            {
                return _restartButtonCommand ??
                    (_restartButtonCommand = new RelayCommand(unused =>
                    {
                        try
                        {
                            Shutdown(true);
                        }
                        catch (Exception ex)
                        {
                            LogService.Write(ex.ToString(), LoggingLevel.Error);
                        }
                    }));
            }
        }

        #endregion

        #region Page services

        private ILogService LogService => AppService?.LogService;

        #endregion

        public PowerSettingsVM() : base()
        {
            // Set default width for settings panel
            Width = Constants.SettingsWidth;
        }

        /// <summary>
        /// Shutdown or restart the current device OS
        /// </summary>
        /// <param name="restart">Default is false to shutdown the device, otherwise set to true for restart</param>
        private void Shutdown(bool restart = false)
        {
            ShutdownManager.BeginShutdown(restart ? ShutdownKind.Restart : ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));
        }
    }
}
