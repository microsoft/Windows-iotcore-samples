// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartDisplay.ViewModels
{
    public class PhotoPageVM : BaseViewModel
    {
        #region UI properties

        public BitmapImage SlideshowImage
        {
            get { return GetStoredProperty<BitmapImage>(); }
            set { SetStoredProperty(value); }
        }

        public bool NoImagesGridIsVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public string NoImagesText
        {
            get { return GetStoredProperty<string>() ?? Common.GetLocalizedText("PhotoPageNoImagesText"); }
            set { SetStoredProperty(value); }
        }
        #endregion

        private ILogService LogService => AppService?.LogService;
        private SettingsProvider Settings => AppService?.Settings as SettingsProvider;
        private ITelemetryService TelemetryService => AppService?.TelemetryService;

        private int _currentFileIndex;
        private IReadOnlyList<StorageFile> _files;
        private TimeSpan _interval;
        private ThreadPoolTimer _slideshowTimer;
        private SemaphoreSlim _semaphoreSlim;

        public PhotoPageVM() : base()
        {
            _files = new List<StorageFile>();
            _slideshowTimer = null;
        }

        public async Task<bool> SetUpVM()
        {
            PopulateCommandBar();
            try
            {
                _currentFileIndex = 0;

                SlideshowImage = new BitmapImage();

                NoImagesGridIsVisible = false;
                ShowLoadingPanel(Common.GetLocalizedText("PhotoPageLoading"));

                _semaphoreSlim = new SemaphoreSlim(1, 1);

                var query = CommonFileQuery.DefaultQuery;
                var queryOptions = new QueryOptions(query, new[] { ".png", ".jpg" })
                {
                    FolderDepth = FolderDepth.Shallow
                };
                var queryResult = KnownFolders.PicturesLibrary.CreateFileQueryWithOptions(queryOptions);

                _files = await queryResult.GetFilesAsync();

                if (_files.Count > 0)
                {
                    TelemetryService.WriteEvent("SlideshowFilesLoaded");

                    DisplayNextPicture();

                    _interval = TimeSpan.FromSeconds(Settings.SlideshowIntervalSeconds);
                    _slideshowTimer = ThreadPoolTimer.CreatePeriodicTimer(SlideshowTimer_Elapsed, _interval);
                }
                else
                {
                    NoImagesGridIsVisible = true;
                    AppService.DisplayDialog(NoImagesText, Common.GetLocalizedText("PhotoPageNoImagesHint"));
                }

                Settings.SettingsUpdated += Settings_SettingsUpdated;
            }
            catch (Exception ex)
            {
                LogService.Write(ex.Message, LoggingLevel.Error);
                return false;
            }
            finally
            {
                HideLoadingPanel();
            }
            return true;
        }

        public void TearDownVM()
        {
            Settings.SettingsUpdated -= Settings_SettingsUpdated;

            LogService.Write("Stopping slideshow timer...");

            _slideshowTimer?.Cancel();
            _slideshowTimer = null;

            _semaphoreSlim.Dispose();
            _semaphoreSlim = null;
        }

        private void Settings_SettingsUpdated(object sender, SettingsUpdatedEventArgs args)
        {
            if (sender is SettingsProvider settings)
            {
                switch (args.Key)
                {
                    case "SlideshowIntervalSeconds":
                        _slideshowTimer?.Cancel();
                        _interval = TimeSpan.FromSeconds(settings.SlideshowIntervalSeconds);
                        _slideshowTimer = ThreadPoolTimer.CreatePeriodicTimer(SlideshowTimer_Elapsed, _interval);                        
                        break;
                }
            }
        }

        private void PopulateCommandBar()
        {
            PageService?.AddCommandBarButton(CommandBarButton.Separator);
            PageService?.AddCommandBarButton(PageUtil.CreatePageSettingCommandBarButton(
                PageService,
                new SlideshowSettingsControl
                {
                    Width = Constants.DefaultSidePaneContentWidth,
                    Background = new SolidColorBrush(Colors.Transparent),
                },
                Common.GetLocalizedText("SlideshowSettingHeader/Text")));
        }

        private void DisplayNextPicture()
        {
            try
            {
                if (_files.Count > 0)
                {
                    _currentFileIndex++;

                    if (_currentFileIndex >= _files.Count)
                    {
                        _currentFileIndex = 0;
                    }

                    var result = SetImage(_files[_currentFileIndex]);
                }
            }
            catch (Exception ex)
            {
                LogService.Write(ex.Message, LoggingLevel.Error);
            }
        }

        private void DisplayPreviousPicture()
        {
            try
            {
                if (_files.Count > 0)
                {
                    _currentFileIndex--;

                    if (_currentFileIndex < 0)
                    {
                        _currentFileIndex = _files.Count + _currentFileIndex;
                    }

                    var result = SetImage(_files[_currentFileIndex]);
                }
            }
            catch (Exception ex)
            {
                LogService.Write(ex.Message, LoggingLevel.Error);
            }
        }

        private async Task SetImage(StorageFile file)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                using (var stream = await ImageUtil.GetBitmapStreamAsync(file))
                {
                    SlideshowImage = new BitmapImage();
                    await SlideshowImage.SetSourceAsync(stream);
                }
            }
            catch (Exception ex)
            {
                LogService.Write(ex.Message, LoggingLevel.Error);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private void SlideshowTimer_Elapsed(ThreadPoolTimer timer)
        {
            _slideshowTimer?.Cancel();

            InvokeOnUIThread(() =>
            {
                DisplayNextPicture();
            });

            _slideshowTimer = ThreadPoolTimer.CreatePeriodicTimer(SlideshowTimer_Elapsed, _interval);
        }

        public void UpdateSlideshowImage(bool displayPrevious)
        {
            if (displayPrevious)
            {
                DisplayPreviousPicture();
            }
            else
            {
                DisplayNextPicture();
            }

            // Reset slideshow timer
            _slideshowTimer?.Cancel();
            _slideshowTimer = ThreadPoolTimer.CreatePeriodicTimer(SlideshowTimer_Elapsed, _interval);
        }
    }
}
