using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Media;
using Windows.Media.Capture.Frames;
using Windows.Storage;

namespace ConsoleDotNetCoreWinML
{
    public sealed class ModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class ModelOutput
    {
        public string classLabel {
            get
            {
                var temp = classLabelTensor.GetAsVectorView();
                if (temp.Count != 1)
                {
                    throw new ApplicationException(string.Format("class label tensor count = {0}", temp.Count));
                }
                string r = null;
                foreach (var c in temp)
                {
                    r = c;
                }
                return r;
            }
        }
        public IList<IDictionary<string, float>> loss { get; private set; }
        public TensorString classLabelTensor { get; private set; }
        public ModelOutput(LearningModel model)
        {
            // assume coreml converted sequence<map<>> followed by class label and validate
            var lossMap = new Dictionary<string, float>()
            {
                {"apple",  0f },
                {"grapes", 0f },
                {"pear", 0f },
                {"pen", 0f }
            };
            this.loss = new List<IDictionary<string, float>>();
            this.loss.Add(lossMap);
            List<long> oshape = new List<long>();
            foreach (var of in model.OutputFeatures)
            {
                if (of.Name == "classLabel" && of.Kind == LearningModelFeatureKind.Tensor)
                {
                    foreach (var i in ((TensorFeatureDescriptor)of).Shape)
                    {
                        if (i == -1)
                        {
                            oshape.Add(1);
                        }
                        else
                        {
                            oshape.Add(i);
                        }
                    }
                }
            }
            if (oshape.Count != 2)
            {
                throw new ApplicationException("missing expected output feature classLabel");
            }
            if (oshape[0] != 1 || oshape[1] != 1)
            {
                throw new ApplicationException(string.Format("unexpected dimension in output feature classLabel [{0}, {1}]", oshape[0], oshape[1]));
            }
            //classLabel = new string[(oshape[1])];
            //classLabel[0] = "Unevaluated";
            var temp = new string[(oshape[1])];
            temp[0] = "Unevaluated";
            classLabelTensor = TensorString.CreateFromArray(oshape, temp);
        }
    }

    public sealed class ModelResult {
        public LearningModelBinding _binding { get;  }
        public ModelOutput _output { get;  }
        public LearningModelEvaluationResult _result { get; set; }
        public string _correlationId { get; }
        public ModelResult(LearningModelSession s, string correlationId) {
            _binding = new LearningModelBinding(s);
            _output = new ModelOutput(s.Model);
            _correlationId = correlationId;
        }
        public string ClassLabel
        {
            get
            {
                if (_result == null || !_result.Succeeded)
                {
                    return null;
                }
                return _output.classLabel;
            }
        }
        public IList<IDictionary<string, float>> Probabilities
        {
            get
            {
                return _output.loss;
            }
        }
        public KeyValuePair<string, float> MostProbable
        {
            get
            {
                KeyValuePair<string, float> v = new KeyValuePair<string, float>(null, float.MinValue);
                foreach (var o in _output.loss)
                {
                    foreach (var l in o)
                    {
                        if (v.Key == null || l.Value > v.Value)
                        {
                            v = l;
                        }
                    }
                }
                return v;
            }
        }
    }
    public sealed class Model
    {
        private LearningModelSession _session { get; set; }
        private LearningModel _model { get; set; }
        private LearningModelDevice _device { get; set; }

        static private Dictionary<string, ModelResult> _results = new Dictionary<string, ModelResult>();
        public static async Task<Model> CreateModelAsync(string filename)
        {
            Log.WriteLine("creating model");
            var file = await AsyncHelper.SyncFromAsync(StorageFile.GetFileFromPathAsync(filename), "file");
            Log.WriteLine("have file");
            var learningModel = await AsyncHelper.SyncFromAsync(LearningModel.LoadFromStorageFileAsync(file), "learningModel");
            Log.WriteLine("loaded model");
            Model model = new Model();
            model._model = learningModel;
            //LearningModelDeviceKind kind = LearningModelDeviceKind.DirectXHighPerformance;
            LearningModelDeviceKind kind = LearningModelDeviceKind.Cpu;
            model._device = new LearningModelDevice(kind);
            model._session = new LearningModelSession(model._model, model._device);
            Log.WriteLine("returning model");
            return model;
        }
        public void Clear(string correlationId)
        {
            lock (_results)
            {
                _results.Remove(correlationId);
                --depth;
            }

        }
        private const int MAX_DEPTH = 3;
        private static int depth = 0;
        public static bool Full
        {
            get
            {
                bool rc = false;
                lock (_results)
                {
                    if (depth >= MAX_DEPTH)
                    {
                        rc = true;
                    }
                }
                return rc;
            }
        }
        public void UpdateSession(LearningModelDeviceKind kind)
        {
            _device = new LearningModelDevice(kind);
            _session = new LearningModelSession(_model, _device);
        }
        public async Task<ModelResult> EvaluateAsync(MediaFrameReference input, string correlationId)
        {
            var r = new ModelResult(_session, correlationId);
            lock (_results) {
                _results.Add(correlationId, r);
                ++depth;
            }
            var v = ImageFeatureValue.CreateFromVideoFrame(input.VideoMediaFrame.GetVideoFrame());

            // NOTE: following bind strings are specific to azure custom vision coreml output.
            r._binding.Bind("data", v);
            r._binding.Bind("classLabel", r._output.classLabelTensor);
            r._binding.Bind("loss", r._output.loss);

            r._result = await AsyncHelper.SyncFromAsync(_session.EvaluateAsync(r._binding, correlationId), "r._result");
            return r;
        }
    }
}
