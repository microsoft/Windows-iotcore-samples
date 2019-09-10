// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using System;
using Windows.Foundation;
using Windows.Media;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Features.WinML.Views
{
    public sealed partial class MnistPage : PageBase, IVideoFrameInputController
    {
        public MnistPageVM ViewModel { get; } = new MnistPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public event TypedEventHandler<object, VideoFrame> InputReady;

        private const int InkSize = 10;

        public MnistPage()
        {
            InitializeComponent();

            // Set supported inking device types.
            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(
                new InkDrawingAttributes()
                {
                    Color = Colors.White,
                    Size = new Size(InkSize, InkSize),
                    IgnorePressure = false,
                    IgnoreTilt = true,
                }
            );
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            inkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            ViewModel.SetUpVM(this);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            inkCanvas.InkPresenter.StrokesCollected -= InkPresenter_StrokesCollected;

            ViewModel.TearDownVM();
        }
        
        private async void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            try
            {
                InputReady?.Invoke(this, await MnistHelper.GetHandWrittenImageAsync(inkGrid));
            }
            catch (Exception ex)
            {
                PageService?.ShowNotification(ex.Message);
            }
        }

        public void Reset()
        {
            inkCanvas.InkPresenter.StrokeContainer.Clear();
        }
    }
}
