// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.Foundation;
using Windows.Media;

namespace SmartDisplay.Features.WinML
{
    public interface IVideoFrameInputController
    {
        event TypedEventHandler<object, VideoFrame> InputReady;

        void Reset();
    }
}
