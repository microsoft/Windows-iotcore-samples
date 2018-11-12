// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SmartDisplay.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Make our BaseViewModel thread safe by assuring we post our property updates to the correct context
        /// </summary>
        private SynchronizationContext _syncContext;

        protected IAppService AppService { get; private set; }
        protected IPageService PageService { get; private set; }

        protected bool IsActive { get; private set; }

        public void SetAppService(IAppService appService)
        {
            AppService = appService;
            PageService = appService.PageService;
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
        }

        public void ShowLoadingPanel(string text = null)
        {
            if (IsActive)
            {
                PageService?.ShowLoadingPanel(text);
            }
        }

        public void HideLoadingPanel()
        {
            if (IsActive)
            {
                PageService?.HideLoadingPanel();
            }
        }

        public BaseViewModel()
        {
            // Will be null during unit tests, otherwise should indicate UI thread sync context
            _syncContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Allows updating a view model property from a non-UI thread. An example would be running
        /// a task in a thread-pool thread that downloads data the view model needs. Once downloaded
        /// the UI property needs to be updated; however, you cannot access UI elements from a non UI
        /// thread so you marshal it over using this function which calls the Window Dispatcher to run
        /// the given task.
        /// 
        /// Use example:
        /// InvokeOnUIThread(() => { DelayedAchievementCount = result; });
        /// 
        /// </summary>
        /// <param name="task">
        /// The task to update the desired property
        /// </param>
        protected void InvokeOnUIThread(Windows.UI.Core.DispatchedHandler task)
        {
            if (_syncContext != null && _syncContext != SynchronizationContext.Current)
            {
                _syncContext.Post(delegate { task.Invoke(); }, null);
            }
            else
            {
                task.Invoke();
            }
        }

        /// <summary>
        /// Notifies listeners that a property of the view-model has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Temporarily suspends PropertyChanged events from being fired.
        /// This allows properties to be set without listeners being notified right away.
        /// </summary>
        protected bool SuspendPropertyChanged { get; set; }

        /// <summary>
        /// Fires a PropertyChanged event for the provided property name.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Default value required for CallerMemberNameAttribute")]
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (!SuspendPropertyChanged && PropertyChanged != null)
            {
                InvokeOnUIThread(() => 
                {
                    try
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        switch (ex.HResult)
                        {
                            // Operation aborted
                            case -2147467260:
                                break;
                            default:
                                throw;
                        }
                    }
                });
            }
        }

        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Default value required for CallerMemberNameAttribute")]
        protected T GetStoredProperty<T>([CallerMemberName] string name = null)
        {
            if (!_properties.ContainsKey(name))
            {
                return default(T);
            }

            return (T)_properties[name];
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Default value required for CallerMemberNameAttribute")]
        protected bool SetStoredProperty(object value, [CallerMemberName] string name = null)
        {
            if (_properties.ContainsKey(name) && value != null && value.Equals(_properties[name]))
            {
                return false;
            }
            else
            {
                _properties[name] = value;
                NotifyPropertyChanged(name);

                return true;
            }
        }
    }
}
