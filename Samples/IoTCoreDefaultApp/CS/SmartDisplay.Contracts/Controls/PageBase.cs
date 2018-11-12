// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Controls
{
    public class PageBase : Page
    {
        private IAppService _appService;
        protected IAppService AppService
        {
            get
            {
                if (_appService == null)
                {
                    if (Application.Current is IAppServiceProvider appServiceProvider)
                    {
                        _appService = appServiceProvider.FindOrCreate(Dispatcher);
                    }
                }
                return _appService;
            }
        }

        private IPageService _pageService;
        protected IPageService PageService
        {
            get
            {
                if (_pageService == null)
                {
                    _pageService = AppService?.PageService;
                }
                return _pageService;
            }
        }

        // Override this in your derived class.
        protected virtual BaseViewModel ViewModelImpl => null;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // If this page uses a view model then set the model's AppService as well.
            ViewModelImpl?.SetAppService(AppService);
            ViewModelImpl?.SetActive(true);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModelImpl?.SetActive(false);
            PageService?.HideLoadingPanel();
        }

        /// <summary>
        /// Finds the first parent of a control of type T.
        /// </summary>
        protected static T FindParent<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);

            if (parent == null)
            {
                return null;
            }

            var parentT = parent as T;
            return parentT ?? FindParent<T>(parent);
        }
    }
}
