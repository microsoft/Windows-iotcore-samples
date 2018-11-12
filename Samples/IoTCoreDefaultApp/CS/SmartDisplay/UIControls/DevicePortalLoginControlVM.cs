// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using SmartDisplay.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml.Input;

namespace SmartDisplay.Controls
{
    public class DevicePortalLoginControlVM : BaseViewModel, IDisposable
    {
        public event TypedEventHandler<object, SignInCompletedArgs> SignInCompleted;

        #region UI properties

        public string Title
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public string Description
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public string Username
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public string Password
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsSignInVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsTransitionVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsInputEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsCancelVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        #endregion

        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private ILogService LogService = ServiceUtil.LogService;
        private static bool _isSignedIn = false;
        private bool _finalSignInEventFired = false;

        public DevicePortalLoginControlVM() : base()
        {
            IsVisible = true;
            IsTransitionVisible = true;
            IsSignInVisible = false;
            IsInputEnabled = true;

            if (string.IsNullOrEmpty(Title))
            {
                Title = Common.GetLocalizedText("WDPTitle/Text");
            }

            if (string.IsNullOrEmpty(Description))
            {
                Description = Common.GetLocalizedText("DevicePortalSignInDescription/Text");
            }

            Username = DevicePortalUtil.DefaultUserName;

            _isSignedIn = false;
        }

        /// <summary>
        /// Reset the state of the control
        /// </summary>
        public void Reset()
        {
            IsVisible = true;
            IsTransitionVisible = true;
            IsSignInVisible = false;
            IsInputEnabled = true;

            _isSignedIn = false;
        }

        public async void KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Enter:
                    await SignInAsync(true);
                    break;
            }
        }

        public async Task<bool> SignInAsync(bool isUserInitiated = false)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                // Don't try signing in again if we're already signed in
                if (_isSignedIn)
                {
                    return true;
                }

                IsInputEnabled = false;

                _isSignedIn = await DevicePortalUtil.IsSignedInAsync();

                if (!_isSignedIn)
                {
                    _isSignedIn = await DevicePortalUtil.SignInAsync(Username, Password);
                }

                _finalSignInEventFired = _isSignedIn;

                SignInCompleted?.Invoke(this, new SignInCompletedArgs
                {
                    IsSuccessful = _isSignedIn,
                    IsUserInitiated = isUserInitiated,
                });

                IsTransitionVisible = false;
                IsVisible = IsSignInVisible = IsInputEnabled = !_isSignedIn;

                return _isSignedIn;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public void TearDownVM()
        {
            if (!_finalSignInEventFired)
            {
                _finalSignInEventFired = true;
                SignInCompleted?.Invoke(this, new SignInCompletedArgs
                {
                    IsSuccessful = false,
                    IsUserInitiated = true,
                });
            }
        }

        #region IDisposable
        bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here.
                    if (_semaphoreSlim != null)
                    {
                        _semaphoreSlim.Dispose();
                        _semaphoreSlim = null;
                    }
                }

                // Dispose unmanaged resources here.
                _disposed = true;
            }
        }

        ~DevicePortalLoginControlVM()
        {
            Dispose(false);
        }
        #endregion

        private RelayCommand _signInCommand;
        public ICommand SignInCommand
        {
            get
            {
                return _signInCommand ??
                    (_signInCommand = new RelayCommand(async unused => await SignInAsync(true)));
            }
        }

        private RelayCommand _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                return _cancelCommand ??
                    (_cancelCommand = new RelayCommand(unused =>
                    {
                        IsInputEnabled = false;
                        IsVisible = false;

                        _finalSignInEventFired = true;

                        SignInCompleted?.Invoke(this, new SignInCompletedArgs
                        {
                            IsSuccessful = false,
                            IsUserInitiated = true,
                        });
                    }));
            }
        }
    }
}
