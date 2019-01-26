// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Features.Utils;
using SmartDisplay.Utils;
using SmartDisplay.ViewModels;
using System;
using System.Linq;
using System.Windows.Input;
using Windows.AI.MachineLearning;
using Windows.Media;

namespace SmartDisplay.Features.WinML.Views
{
    public class MnistPageVM : BaseViewModel
    {
        #region UI Properties

        public string NumberText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public bool IsEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        #endregion

        #region Commands

        private RelayCommand _clearCommand;
        public ICommand ClearCommand
        {
            get
            {
                return _clearCommand ??
                    (_clearCommand = new RelayCommand(unused =>
                    {
                        NumberText = string.Empty;
                        InvokeOnUIThread(() => _controller.Reset());
                    }));
            }
        }
        #endregion

        private ILogService LogService => AppService.LogService;

        private const string ModelFileName = "mnist.onnx";

        private mnistModel _model = null;
        private IVideoFrameInputController _controller;

        public async void SetUpVM(IVideoFrameInputController controller)
        {
            // WinML requires Windows 10 build 17728 or higher to run: https://docs.microsoft.com/en-us/windows/ai/
            if (!MLHelper.IsMLAvailable())
            {
                IsEnabled = false;
                AppService.DisplayDialog(FeatureUtil.GetLocalizedText("WinMLNotAvailableTitle"), FeatureUtil.GetLocalizedText("WinMLNotAvailableDescription"));
                return;
            }

            LogService.Write($"Loading model {ModelFileName}...");
            var modelFile = await FileUtil.GetFileFromInstalledLocationAsync(MLHelper.ModelBasePath + ModelFileName);
            _model = await mnistModel.CreateFromStreamAsync(modelFile);

            _controller = controller;
            _controller.InputReady += Controller_InputReady;
            
            IsEnabled = true;
        }

        public void TearDownVM()
        {
            if (_controller != null)
            {
                _controller.InputReady -= Controller_InputReady;
            }
        }

        private async void Controller_InputReady(object sender, VideoFrame args)
        {
            try
            {
                LogService.Write($"Evaluating model...");
                using (args)
                {
                    var output = await _model.EvaluateAsync(new mnistInput
                    {
                        Input3 = ImageFeatureValue.CreateFromVideoFrame(args)
                    });

                    var outputVector = output.Plus214_Output_0.GetAsVectorView().ToList();
                    var maxIndex = outputVector.IndexOf(outputVector.Max());

                    NumberText = maxIndex.ToString();

                    var topIndices = MLHelper.GetTopLabelIndices(outputVector);
                    string topIndicesString = Environment.NewLine;
                    foreach (var topIndex in topIndices)
                    {
                        topIndicesString += $"{topIndex.LabelIndex}, Confidence: {topIndex.Confidence}" + Environment.NewLine;
                    }
                    LogService.Write(topIndicesString);
                }
            }
            catch (Exception ex)
            {
                // The WinML APIs are still in preview, so throw a visible exception so users can file a bug
                AppService.DisplayDialog(ex.GetType().Name, ex.Message);
                LogService.WriteException(ex);
            }
        }
    }
}
