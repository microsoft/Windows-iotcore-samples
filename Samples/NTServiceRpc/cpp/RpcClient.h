//
// RpcClient.h
//

#pragma once

#define RPC_STATIC_ENDPOINT L"NTServiceSampleRpcEndpoint"

#include "RpcInterface_h.h"

#include <stdexcept>

namespace NTServiceRpc
{
    class RpcCallException : public std::runtime_error
    {
    public:
        RpcCallException(RPC_STATUS error);
    };

    class RpcRemoteException : public std::runtime_error
    {
    public:
        RpcRemoteException(DWORD error);
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
        void RunService(const wchar_t *serviceName);
        void StopService(const wchar_t *serviceName);
    private:
        handle_t _rpcBinding;
        PCONTEXT_HANDLE_TYPE phContext;
    };
}
