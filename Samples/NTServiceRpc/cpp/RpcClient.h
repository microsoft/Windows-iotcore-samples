//
// RpcClient.h
//

#pragma once

#define RPC_STATIC_ENDPOINT L"NTServiceSampleRpcEndpoint"

#include "RpcInterface_h.h"

#include <stdexcept>

namespace NTServiceRpc
{
    class RpcCallException : std::runtime_error
    {
    public:
        RpcCallException(RPC_STATUS status);
        const char* what() const;
    private:
        std::string message;
    };

    /// <summary>
    /// Client side RPC implementation
    /// </summary>
    private class RpcClient sealed
    {
    public:
        ~RpcClient();
        void Initialize();
        DWORD GetServiceStatus(const wchar_t *serviceName);
        bool RunService(const wchar_t *serviceName);
        bool StopService(const wchar_t *serviceName);
    private:
        handle_t hRpcBinding;
        PCONTEXT_HANDLE_TYPE phContext;
    };
}
