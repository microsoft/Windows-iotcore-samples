// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Metadata;

namespace SmartDisplay.Features.WinML
{
    public static class MLHelper
    {
        public const string ModelBasePath = @"SmartDisplay.Features\WinML\Models\";

        public static bool IsMLAvailable()
        {
            return ApiInformation.IsTypePresent("Windows.AI.MachineLearning.LearningModel");
        }

        public static List<MLLabelIndex> GetTopLabelIndices(List<float> resultOutputs)
        {
            // Create a list of tuples containing the value and the index
            var transform = resultOutputs.Select((Confidence, LabelIndex) => new MLLabelIndex(LabelIndex, Confidence)).ToList();

            // Sort by the confidence value (descending)
            transform.Sort((f1, f2) => f2.Confidence.CompareTo(f1.Confidence));

            // Return the top 3
            return transform.Take(3).ToList();
        }
    }

    public struct MLLabelIndex
    {
        public int LabelIndex;
        public float Confidence;

        public MLLabelIndex(int labelIndex, float confidence)
        {
            LabelIndex = labelIndex;
            Confidence = confidence;
        }
    }
}
