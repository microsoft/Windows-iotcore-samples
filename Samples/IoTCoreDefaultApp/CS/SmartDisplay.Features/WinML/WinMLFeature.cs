// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Features.WinML.Views;
using System;

namespace SmartDisplay.Features.WinML
{
    [System.Composition.Export(typeof(IFeature))]
    public class WinMLFeature : IFeature
    {
        #region IFeature

        public string FeatureName => WinMLString;

        public PageDescriptor[] Pages { get; } = new PageDescriptor[]
        {
            new PageDescriptor()
            {
                Icon = "✒",
                Title = "MNIST ML",
                Type = typeof(MnistPage),
                Tag = "\xE734",
                Category = WinMLString,
            },
        };

        public Type DefaultPage => null;

        public string DeviceInfo => null;

        public Type SettingsUserControl => null;

        #endregion

        #region IAppService

        public static IAppService AppService;
        public static ILogService LogService => AppService.LogService;

        #endregion

        private const string WinMLString = "Windows ML";

        public void OnLoaded(IAppServiceProvider provider)
        {
            AppService = provider.GetForCurrentContext();
        }

        public void OnShutdown()
        {
        }
    }
}
