//
// RpcClient.h
//

#pragma once

#define RPC_STATIC_ENDPOINT L"NTServiceSampleRpcEndpoint"

#include "RpcInterface_h.h"

namespace NTServiceRpc
{
    /// <summary>
    /// Client side RPC implementation
    /// </summary>
    private class RpcClient sealed
    {
    public:
        ~RpcClient();
        __int64 Initialize();
        DWORD GetServiceStatus(const wchar_t *serviceName);
        boolean RunService(const wchar_t *serviceName);
        boolean StopService(const wchar_t *serviceName);
        int CallbackCount;
        __int64 MeteringData;
    private:
        handle_t hRpcBinding;
        PCONTEXT_HANDLE_TYPE phContext;
    };
}
