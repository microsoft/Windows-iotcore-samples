using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;
using static Helpers.AsyncHelper;

namespace SampleModule
{
    
    public sealed class ScoringInput
    {
        public ImageFeatureValue data_0; // shape(1,3,224,224)
    }
    
    public sealed class ScoringOutput
    {
        public TensorFloat softmaxout_1; // shape(1,1000,1,1)
    }
    
    public sealed class ScoringModel
    {
        private LearningModel model;
        private LearningModelSession session;
        private LearningModelBinding binding;
        public static async Task<ScoringModel> CreateFromStreamAsync(IRandomAccessStreamReference stream)
        {
            ScoringModel learningModel = new ScoringModel();
            learningModel.model = await AsAsync( LearningModel.LoadFromStreamAsync(stream));
            learningModel.session = new LearningModelSession(learningModel.model);
            learningModel.binding = new LearningModelBinding(learningModel.session);
            return learningModel;
        }
        public async Task<ScoringOutput> EvaluateAsync(ScoringInput
 input)
        {
            binding.Bind("data_0", input.data_0);
            var result = await AsAsync( session.EvaluateAsync(binding, "0") );
            var output = new ScoringOutput();
            output.softmaxout_1 = result.Outputs["softmaxout_1"] as TensorFloat;
            return output;
        }
    }
}
