// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using System;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SmartDisplay.Utils;

namespace SmartDisplay.ViewModels
{
    public class DrawingPageVM : BaseViewModel
    {
        #region UI properties

        private InkCanvas _viewInkCanvas;

        #endregion

        public DrawingPageVM() : base()
        {
        }

        public void SetUpVM(InkCanvas inkCanvas)
        {
            _viewInkCanvas = inkCanvas;
            _viewInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;
            PopulateCommandBar();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filename = DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png";
                StorageFile file = await KnownFolders.PicturesLibrary.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);
                if (null != file)
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await _viewInkCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);
                        PageService.ShowNotification(string.Format(Common.GetLocalizedText("FileSavedText"), filename));
                        App.TelemetryService.WriteEvent("InkCanvasSave");
                    }
                }
            }
            catch (Exception ex)
            {
                App.LogService.Write(ex.Message, LoggingLevel.Error);
            }
        }

        private void PopulateCommandBar()
        {
            PageService.AddCommandBarButton(CommandBarButton.Separator);
            
            PageService.AddCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Save),
                Label = Common.GetLocalizedText("SaveButtonText"),
                Handler = SaveButton_Click,
            });
        }
    }
}
