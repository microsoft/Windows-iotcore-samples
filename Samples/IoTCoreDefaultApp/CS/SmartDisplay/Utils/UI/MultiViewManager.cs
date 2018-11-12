// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Utils.UI
{
    public class MultiViewManager
    {
        public event TypedEventHandler<object, ViewLifetimeControlEventArgs> ViewAdded;

        public CoreDispatcher MainDispatcher { get; private set; }

        public int MainViewId { get; private set; }

        // Keeps track of all the views that the MultiViewManager creates
        public List<ViewLifetimeControl> SecondaryViews { get; private set; }

        private object _collectionLock;

        public MultiViewManager(CoreDispatcher mainDispatcher, int mainViewId)
        {
            _collectionLock = new object();

            MainDispatcher = mainDispatcher;
            MainViewId = mainViewId;
            SecondaryViews = new List<ViewLifetimeControl>();
        }

        public bool IsMultiViewAvailable()
        {
            bool available = ProjectionManager.ProjectionDisplayAvailable;
#if DEBUG
            // If debugging, show the Clone View button even if a second display is not available.
            if (!available)
            {
                available = System.Diagnostics.Debugger.IsAttached;
            }
#endif
            return available;
        }

        public async Task<ViewLifetimeControl> CreateAndShowViewAsync(Type pageType, object param = null)
        {
            try
            {
                var viewControl = await CreateViewAsync(pageType, param);
                if (viewControl != null)
                {
                    await ShowViewAsync(viewControl);
                    return viewControl;
                }
            }
            catch (Exception ex)
            {
                App.LogService.Write(ex.ToString(), LoggingLevel.Error);
            }

            return null;
        }

        public async Task<ViewLifetimeControl> CreateViewAsync(Type pageType, object param)
        {
            // Set up the secondary view, but don't show it yet
            ViewLifetimeControl viewControl = null;
            await CoreApplication.CreateNewView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // This object is used to keep track of the views and important
                // details about the contents of those views across threads
                viewControl = ViewLifetimeControl.CreateForCurrentView();
                viewControl.Title = Common.GetLocalizedText("MultiViewWindowTitle");

                // Increment the ref count because we just created the view and we have a reference to it       
                // The corresponding StopViewInUse() is in the Consolidated handler
                viewControl.StartViewInUse();

                var frame = new Frame();

                // Set window content before navigation so that the child pages can get the MainPage
                Window.Current.Content = frame;
                frame.Navigate(pageType, Tuple.Create(viewControl, param));

                // This is a change from 8.1: In order for the view to be displayed later it needs to be activated.
                Window.Current.Activate();
                ApplicationView.GetForCurrentView().Title = viewControl.Title;
            });

            // Attach a released handler so we can update the list when it's released
            viewControl.Released += ViewControl_Released;

            // Add view to list
            AddView(viewControl);

            return viewControl;
        }

        public async Task ShowViewAsync(ViewLifetimeControl viewControl)
        {
            try
            {
                // Prevent the view from closing while
                // switching to it
                viewControl.StartViewInUse();

                // Show the view
                var viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
                    viewControl.Id,
                    ViewSizePreference.Default,
                    ApplicationView.GetForCurrentView().Id,
                    ViewSizePreference.Default);

                if (!viewShown)
                {
                    App.LogService.Write("The view was not shown. Make sure it has focus");
                }

                // Signal that switching has completed and let the view close
                viewControl.StopViewInUse();
            }
            catch (InvalidOperationException ex)
            {
                // The view could be in the process of closing, and
                // this thread just hasn't updated.
                App.LogService.Write(ex.ToString());
            }
        }

        private void AddView(ViewLifetimeControl viewControl)
        {
            lock (_collectionLock)
            {
                App.LogService.Write($"Adding view: {viewControl.Id}");
                SecondaryViews.Add(viewControl);
            }
            ViewAdded?.Invoke(this, new ViewLifetimeControlEventArgs() { ViewControl = viewControl });
        }

        private void RemoveView(ViewLifetimeControl viewControl)
        {
            lock (_collectionLock)
            {
                if (SecondaryViews.Contains(viewControl))
                {
                    App.LogService.Write($"Removing view: {viewControl.Id}");
                    SecondaryViews.Remove(viewControl);
                }
            }
        }

        private void ViewControl_Released(object sender, EventArgs e)
        {
            if (sender is ViewLifetimeControl viewControl)
            {
                viewControl.Released -= ViewControl_Released;
                RemoveView(viewControl);
            }
        }
    }

    public class ViewLifetimeControlEventArgs : EventArgs
    {
        public ViewLifetimeControl ViewControl;
    }
}
