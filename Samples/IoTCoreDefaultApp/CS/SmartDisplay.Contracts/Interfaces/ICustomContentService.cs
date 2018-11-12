// Copyright (c) Microsoft Corporation. All rights reserved.

namespace SmartDisplay.Contracts
{
    public interface ICustomContentService : ISmartDisplayService
    {
        T GetContent<T>(string key);
    }
}
