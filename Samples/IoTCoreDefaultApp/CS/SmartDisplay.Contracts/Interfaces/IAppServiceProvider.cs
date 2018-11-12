// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.UI.Core;

namespace SmartDisplay.Contracts
{
    public interface IAppServiceProvider
    {
        /// <summary>
        /// Returns a new or existing IAppService for <paramref name="dispatcher"/>
        /// </summary>
        IAppService FindOrCreate(CoreDispatcher dispatcher);

        /// <summary>
        /// Returns a new or existing IAppService for the current context.
        /// This app service may not have UI services available if called from a background thread.
        /// </summary>
        IAppService GetForCurrentContext();
    }
}
