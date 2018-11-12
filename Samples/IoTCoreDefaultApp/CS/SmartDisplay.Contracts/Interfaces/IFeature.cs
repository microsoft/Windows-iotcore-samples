// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace SmartDisplay.Contracts
{
    public interface IFeature
    {
        string FeatureName { get; }

        /// <summary>
        /// Called by the app during Application.OnLoaded
        /// </summary>
        /// <param name="provider"></param>
        void OnLoaded(IAppServiceProvider provider);

        void OnShutdown();

        /// <summary>
        /// A list of pages to show in the Tile list. These can be set as the default page.
        /// This property can be null.
        /// </summary>
        PageDescriptor[] Pages { get; }

        /// <summary>
        /// A default page to show if the user has not set a default page.
        /// This property can be null.
        /// </summary>
        Type DefaultPage { get; }

        /// <summary>
        /// A string to be displayed in the Device Info page.
        /// This property can be null.
        /// </summary>
        string DeviceInfo { get; }

        /// <summary>
        /// The user control for a custom setting for the feature.
        /// </summary>
        Type SettingsUserControl { get; }
    }

    public class PageDescriptor
    {
        public string Icon;
        public string Title;
        public Type Type;
        public string Tag;
        public string Category;
        public object PageParam;
    }
}
