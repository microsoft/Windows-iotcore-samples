// Copyright (c) Microsoft Corporation. All rights reserved.

// The objects defined here demonstrate how to make sure each of the views created remains alive as long as 
// the app needs them, but only when they're being used by the app or the user. 
//
// The Consolidated event is fired when a view stops being visible separately from other views. Common cases where this will occur
// is when the view falls out of the list of recently used apps, or when the user performs the close gesture on the view.
// This is a good time to close the view, provided the app isn't trying to show the view at the same time. This event
// is fired on the thread of the view that becomes consolidated.
//
// Each view lives on its own thread, so concurrency control is necessary. Certain objects may be bound to UI on given threads. 
// Properties of those objects should only be updated on that UI thread.

using System;
using System.ComponentModel;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

/// <summary>
/// Based on the Multi-View Universal Windows Platform (UWP) sample
/// </summary>
namespace SmartDisplay.Utils.UI
{
    /// <summary>
    /// A custom event that fires whenever the secondary view is ready to be closed. You should
    /// clean up any state (including deregistering for events) then close the window in this handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ViewReleasedHandler(object sender, EventArgs e);

    /// <summary>
    /// A ViewLifetimeControl is instantiated for every secondary view. ViewLifetimeControl's reference count
    /// keeps track of when the secondary view thinks it's in usem and when the main view is interacting with the secondary view (about to show
    /// it to the user, etc.) When the reference count drops to zero, the secondary view is closed.
    /// </summary>
    public sealed class ViewLifetimeControl : INotifyPropertyChanged
    {
        // Dispatcher for this view. Kept here for sending messages between this view and the main view.
        public CoreDispatcher Dispatcher { get; private set; }

        // Each view has a unique Id, found using the ApplicationView.Id property or
        // ApplicationView.GetApplicationViewIdForCoreWindow method. This id is used in all of the ApplicationViewSwitcher
        // and ProjectionManager APIs. 
        public int Id { get; private set; }

        // Window for this particular view. Used to register and unregister for events
        private CoreWindow _window;

        // The title for the view shown in the list of recently used apps (by setting the title on 
        // ApplicationView)
        private string _title;

        // This class uses references counts to make sure the secondary views isn't closed prematurely.
        // Whenever the main view is about to interact with the secondary view, it should take a reference
        // by calling "StartViewInUse" on this object. When finished interacting, it should release the reference
        // by calling "StopViewInUse". 
        private int _refCount = 0;

        // Tracks if this ViewLifetimeControl object is still valid. If this is true, then the view is in the process
        // of closing itself down
        private bool _released = false;

        // Used to store pubicly registered events under the protection of a lock
        private event ViewReleasedHandler _internalReleased;

        // Used to prevent threads from making reference count modifications at the same time
        private object _referenceLock;

        // Instantiate views using "CreateForCurrentView"
        private ViewLifetimeControl(CoreWindow newWindow)
        {
            _window = newWindow;
            _referenceLock = new object();

            Dispatcher = newWindow.Dispatcher;
            Id = ApplicationView.GetApplicationViewIdForWindow(_window);

            // This class will automatically tell the view when its time to close
            // or stay alive in a few cases
            RegisterForEvents();
        }

        // Register for events on the current view
        private void RegisterForEvents()
        {
            // A view is consolidated with other views when there's no way for the user to get to it (it's not in the list of recently used apps, cannot be
            // launched from Start, etc.) A view stops being consolidated when it's visible--at that point the user can interact with it, move it on or off screen, etc.
            // It's generally a good idea to close a view after it has been consolidated, but keep it open while it's visible.
            ApplicationView.GetForCurrentView().Consolidated += ViewConsolidated;
        }

        // Unregister for events. Call this method before closing the view to prevent leaks.
        private void UnregisterForEvents()
        {
            ApplicationView.GetForCurrentView().Consolidated -= ViewConsolidated;
        }

        // A view is consolidated with other views when there's no way for the user to get to it (it's not in the list of recently used apps, cannot be
        // launched from Start, etc.) A view stops being consolidated when it's visible--at that point the user can interact with it, move it on or off screen, etc. 
        // It's generally a good idea to close a view after it has been consolidated, but keep it open while it's visible.
        private void ViewConsolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs e)
        {
            App.LogService.Write($"ViewControl has been consolidated: {Id}");

            StopViewInUse();
        }

        // Called when a view has been "consolidated" (no longer accessible to the user) 
        // and no other view is trying to interact with it. This should only be closed after the reference
        // count goes to 0 (including being consolidated). At the end of this, the view is closed. 
        private void FinalizeRelease()
        {
            bool justReleased = false;
            lock (_referenceLock)
            {
                if (_refCount == 0)
                {
                    justReleased = true;
                    _released = true;
                }
            }

            // This assumes that released will never be made false after it
            // it has been set to true
            if (justReleased)
            {
                UnregisterForEvents();
                
                App.LogService.Write($"ViewControl has been released: {Id}");

                if (_internalReleased == null)
                {
                    // There must be a released handler to perform clean up on the view before the 
                    // view control can be released
                    throw new Exception("There must be at least one Released handler!");
                }
                _internalReleased(this, null);
            }
        }

        // Creates ViewLifetimeControl for the particular view.
        // Only do this once per view.
        public static ViewLifetimeControl CreateForCurrentView()
        {
            return new ViewLifetimeControl(CoreWindow.GetForCurrentThread());
        }

        // For purposes of this sample, the collection of views
        // is bound to a UI collection. This property is available for binding
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                if (_title != value)
                {
                    _title = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Title"));
                }
            }
        }

        // Signals that the view is being interacted with by another view,
        // so it shouldn't be closed even if it becomes "consolidated"
        public int StartViewInUse()
        {
            bool releasedCopy = false;
            int refCountCopy = 0;

            // This method is called from several different threads
            // (each view lives on its own thread)
            lock (_referenceLock)
            {
                releasedCopy = _released;
                if (!_released)
                {
                    refCountCopy = ++_refCount;
                }
            }

            if (releasedCopy)
            {
                throw new InvalidOperationException("This view is being disposed");
            }

            return refCountCopy;
        }

        // Should come after any call to StartViewInUse
        // Signals that another view has finished interacting with the view tracked
        // by this object
        public int StopViewInUse()
        {
            int refCountCopy = 0;
            bool releasedCopy = false;

            lock (_referenceLock)
            {
                releasedCopy = _released;
                if (!_released)
                {
                    refCountCopy = --_refCount;
                    if (refCountCopy == 0)
                    {
                        // If no other view is interacting with this view, and
                        // the view isn't accessible to the user, it's appropriate
                        // to close it
                        //
                        // Before actually closing the view, make sure there are no
                        // other important events waiting in the queue (this low-priority item
                        // will run after other events)
                        var task = Dispatcher.RunAsync(CoreDispatcherPriority.Low, FinalizeRelease);
                    }
                }
            }

            if (releasedCopy)
            {
                throw new InvalidOperationException("This view is being disposed");
            }
            return refCountCopy;
        }

        // Signals to consumers that it's time to close the view so that
        // they can clean up (including calling Window.Close() when finished)
        public event PropertyChangedEventHandler PropertyChanged;
        public event ViewReleasedHandler Released
        {
            add
            {
                bool releasedCopy = false;
                lock (_referenceLock)
                {
                    releasedCopy = _released;
                    if (!_released)
                    {
                        _internalReleased += value;
                    }
                }

                if (releasedCopy)
                {
                    throw new InvalidOperationException("This view is being disposed");
                }
            }

            remove
            {
                lock (_referenceLock)
                {
                    _internalReleased -= value;
                }
            }
        }
    }
}
