// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Threading.Tasks;

namespace SmartDisplay.Contracts
{
    public interface IIoTDmService : ISmartDisplayService
    {
        bool IsDmClientConnected { get; }

        Task SetMethodHandlerAsync(string methodName, Func<string, Task<string>> methodHandler);
    }
}
