// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts.Interfaces;
using SmartDisplay.Utils;
using SmartDisplay.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    /// <summary>
    /// Manages notification requests to ensure we only show that only one notification at a time.
    /// </summary>
    public class UiManager : IDisposable
    {
        public ObservableCollection<Notification> NotificationHistory { get; private set; } = new ObservableCollection<Notification>();

        private const int HistoryLimit = 20;

        private CoreDispatcher _dispatcher;
        private INotificationControl _notification;

        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        private int _handlerCount = 0;

        private static Dictionary<CoreDispatcher, UiManager> s_uiManagers = new Dictionary<CoreDispatcher, UiManager>();
        private static object s_uiManagersLock = new object();

        // This should only be called from the UI control that owns the InAppNotification
        public static UiManager Create(CoreDispatcher dispatcher, INotificationControl notification)
        {
            lock (s_uiManagersLock)
            {
                if (s_uiManagers.ContainsKey(dispatcher))
                {
                    throw new InvalidOperationException("UiManager.Create should only be called once per dispatcher");
                }

                var manager = new UiManager(dispatcher, notification);
                s_uiManagers[dispatcher] = manager;
                return manager;
            }
        }

        public static UiManager Find(CoreDispatcher dispatcher)
        {
            if (dispatcher == null)
            {
                return null;
            }

            lock (s_uiManagersLock)
            {
                s_uiManagers.TryGetValue(dispatcher, out var manager);
                return manager;
            }
        }

        private UiManager(CoreDispatcher dispatcher, INotificationControl notification)
        {
            _dispatcher = dispatcher;
            _notification = notification;
            _notification.NotificationPressed += _notification_NotificationPressed;
        }

        private Action _clickHandler = null;

        private void _notification_NotificationPressed(object sender, EventArgs e)
        {
            if (_clickHandler != null)
            {
                _clickHandler.Invoke();
            }
            else
            {
                AppService.GetForCurrentContext().PageService?.NavigateTo(typeof(NotificationsPage));
            }

            var asyncDismiss = DismissNotificationAsync();
        }

        private async Task ExclusiveTaskSetup()
        {
            // Interrupt any open notifications
            await ClearNotificationAsync();

            // Wait for any other calls to RunExclusiveTaskAsync to finish.
            await _semaphoreSlim.WaitAsync();
        }

        private void ExclusiveTaskCleanup()
        {
            _semaphoreSlim.Release();
        }

        /// <summary>
        /// Ensures that the task is run synchronously with notifications so there is no overlap.
        /// Use this on dialogs and other UI elements that cannot be run at the same time.
        /// </summary>
        /// <param name="task"></param>
        public async Task RunExclusiveTaskAsync(Action task)
        {
            await ExclusiveTaskSetup();

            try
            {
                task();
            }
            finally
            {
                ExclusiveTaskCleanup();
            }
        }

        /// <summary>
        /// Ensures that the task is run synchronously with notifications so there is no overlap.
        /// Use this on dialogs and other UI elements that cannot be run at the same time.
        /// </summary>
        /// <param name="task"></param>
        public async Task RunExclusiveTaskAsync(Func<Task> task)
        {
            await ExclusiveTaskSetup();

            try
            {
                await task();
            }
            finally
            {
                ExclusiveTaskCleanup();
            }
        }

        /// <summary>
        /// Ensures that the task is run synchronously with notifications so there is no overlap.
        /// Use this on dialogs and other UI elements that cannot be run at the same time.
        /// </summary>
        /// <param name="task"></param>
        /// <returns>The return value from the task.</returns>
        public async Task<T> RunExclusiveTaskAsync<T>(Func<Task<T>> task)
        {
            await ExclusiveTaskSetup();

            try
            {
                return await task();
            }
            finally
            {
                ExclusiveTaskCleanup();
            }
        }

        /// <summary>
        /// Displays notification. Notifications can be interrupted by other tasks.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="symbol"></param>
        public async Task ShowNotificationAsync(string text, int timeoutMilliseconds = 3000, string symbol = "🔵", Action clickHandler = null)
        {
            await RunExclusiveTaskAsync(() =>
            {
                App.LogService.Write($"Showing notification: {text}");

                // Record notification
                AddNotificationToHistory(new Notification()
                {
                    Text = text,
                    Timestamp = DateTime.Now,
                    Symbol = symbol
                });

                var noWait = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    _clickHandler = clickHandler;
                    _notification.Show(text, timeoutMilliseconds, symbol);
                });
            });
        }

        /// <summary>
        /// Dismisses any open notifications
        /// </summary>
        public async Task ClearNotificationAsync()
        {
            if (_handlerCount > 0)
            {
                await DismissNotificationAsync();
            }
        }

        private async Task DismissNotificationAsync()
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _notification.Hide();
            });
        }

        public void AddNotificationToHistory(Notification notification)
        {
            NotificationHistory.Insert(0, notification);

            if (NotificationHistory.Count > HistoryLimit)
            {
                NotificationHistory.RemoveAt(NotificationHistory.Count - 1);
            }
        }

        /// <summary>
        /// Displays a ContentDialog with 1 - 3 customizable buttons
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="primaryButton">Will show up as the leftmost button. Set to null if not needed.</param>
        /// <param name="secondaryButton">Will show up as the center button if primary is set. Set to null if not needed.</param>
        /// <param name="closeButton">Always enabled and "Close" is the default text</param>
        public async Task DisplayDialogAsync(
            string title,
            object content,
            DialogButton? primaryButton,
            DialogButton? secondaryButton,
            DialogButton? closeButton
            )
        {
            var tcs = new TaskCompletionSource<bool>();
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var currentDialog = new ContentDialog()
                {
                    Title = title,
                    Content = content,
                };

                if (primaryButton != null)
                {
                    currentDialog.PrimaryButtonText = primaryButton.Value.Name;
                    currentDialog.PrimaryButtonClick += primaryButton.Value.ClickEventHandler;
                }

                if (secondaryButton != null)
                {
                    currentDialog.SecondaryButtonText = secondaryButton.Value.Name;
                    currentDialog.SecondaryButtonClick += secondaryButton.Value.ClickEventHandler;
                }

                if (closeButton != null)
                {
                    currentDialog.CloseButtonText = closeButton.Value.Name;
                    currentDialog.CloseButtonClick += closeButton.Value.ClickEventHandler;
                }
                else
                {
                    currentDialog.CloseButtonText = Common.GetLocalizedText("CloseButton");
                }

                await RunExclusiveTaskAsync(() => currentDialog.ShowAsync().AsTask());

                tcs.SetResult(true);
            });

            await tcs.Task;
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
                    _semaphoreSlim.Dispose();
                    _notification.NotificationPressed -= _notification_NotificationPressed;
                }

                // Dispose unmanaged resources here.
                _disposed = true;
            }
        }

        ~UiManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
