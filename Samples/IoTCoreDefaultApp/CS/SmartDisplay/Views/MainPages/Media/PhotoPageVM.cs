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
        public BitmapImage SlideshowImage1
        {
            get { return GetStoredProperty<BitmapImage>(); }
            set { SetStoredProperty(value); }
        }

        public BitmapImage SlideshowImage2
        {
            get { return GetStoredProperty<BitmapImage>(); }
            set { SetStoredProperty(value); }
        }

        public bool SlideshowImage1Visible
        {
            get { return GetStoredProperty<bool>(); }
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

        private bool _suspended;
        private int _currentFileIndex;
        private IReadOnlyList<StorageFile> _files;
        private TimeSpan _interval;
        private ThreadPoolTimer _slideshowTimer;
        // 1 if an image change is currently in progress, otherwise 0
        private int _imageChanging;

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
                _suspended = false;
                _imageChanging = 0;

                SlideshowImage1 = new BitmapImage();
                SlideshowImage2 = new BitmapImage();
                SlideshowImage1Visible = true;
                NoImagesGridIsVisible = false;
                ShowLoadingPanel(Common.GetLocalizedText("PhotoPageLoading"));

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
                    _slideshowTimer = ThreadPoolTimer.CreateTimer(SlideshowTimer_Elapsed, _interval);
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
            _suspended = true;

            Settings.SettingsUpdated -= Settings_SettingsUpdated;

            LogService.Write("Stopping slideshow timer...");

            _slideshowTimer?.Cancel();
            _slideshowTimer = null;
        }

        private void Settings_SettingsUpdated(object sender, SettingsUpdatedEventArgs args)
        {
            if (sender is SettingsProvider settings)
            {
                switch (args.Key)
                {
                    case "SlideshowIntervalSeconds":
                        _interval = TimeSpan.FromSeconds(settings.SlideshowIntervalSeconds);
                        StartSlideshowTimer();
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

        private void StartSlideshowTimer()
        {
            if (!_suspended)
            {
                _slideshowTimer?.Cancel();
                _slideshowTimer = ThreadPoolTimer.CreateTimer(SlideshowTimer_Elapsed, _interval);
            }
        }

        private bool BeginImageChange()
        {
            // Don't allow a new image change to start if any of the following conditions are true:
            // - The page is being shut down
            // - There aren't any pictures to display
            // - Another thread is in the middle of changing the image
            return !_suspended &&
                    _files.Count > 0 &&
                    Interlocked.CompareExchange(ref _imageChanging, 1, 0) == 0;
        }

        private void EndImageChange()
        {
            _imageChanging = 0;
        }

        private void NormalizeFileIndex()
        {
            _currentFileIndex %= _files.Count;
            if (_currentFileIndex < 0)
            {
                _currentFileIndex += _files.Count;
            }
        }

        private void DisplayNextPicture()
        {
            _currentFileIndex++;

            // Don't try to change the picture if another change is in progress.

            if (BeginImageChange())
            {
                try
                {
                    NormalizeFileIndex();
                    var result = SetImage(_files[_currentFileIndex]);
                }
                catch (Exception ex)
                {
                    LogService.Write(ex.Message, LoggingLevel.Error);
                }
                finally
                {
                    EndImageChange();
                }
            }
        }

        private void DisplayPreviousPicture()
        {
            _currentFileIndex--;

            // Don't try to change the picture if another change is in progress.
            if (BeginImageChange())
            {
                try
                {
                    NormalizeFileIndex();
                    var result = SetImage(_files[_currentFileIndex]);
                }
                catch (Exception ex)
                {
                    LogService.Write(ex.Message, LoggingLevel.Error);
                }
                finally
                {
                    EndImageChange();
                }
            }
        }

        private async Task SetImage(StorageFile file)
        {
            try
            {
                using (var stream = await ImageUtil.GetBitmapStreamAsync(file))
                {
                    // Set the source of the non-visible Image element.
                    // This allows the Image element to resize itself before we make it visible.
                    var newImage = SlideshowImage1Visible ? SlideshowImage2 : SlideshowImage1;
                    await newImage.SetSourceAsync(stream);

                    // Swap the images
                    SlideshowImage1Visible = !SlideshowImage1Visible;

                    // Clear the old Image element to free its image
                    if (SlideshowImage1Visible)
                    {
                        SlideshowImage2 = new BitmapImage();
                    }
                    else
                    {
                        SlideshowImage1 = new BitmapImage();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Write(ex.Message, LoggingLevel.Error);
            }
        }

        private void SlideshowTimer_Elapsed(ThreadPoolTimer timer)
        {
            // Only attempt to update the picture if there is not another update taking place
            if (_imageChanging == 0)
            {
                InvokeOnUIThread(() =>
                {
                    DisplayNextPicture();
                });
            }

            StartSlideshowTimer();
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

            StartSlideshowTimer();
        }
    }
}
