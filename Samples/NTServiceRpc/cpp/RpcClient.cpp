//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

// There is an error in the system header files that incorrectly
// places RpcStringBindingCompose in the app partition.
// Work around it by changing the WINAPI_FAMILY to desktop temporarily.
#pragma push_macro("WINAPI_FAMILY")
#undef WINAPI_FAMILY
#define WINAPI_FAMILY WINAPI_FAMILY_DESKTOP_APP
#include "RpcClient.h"
#pragma pop_macro("WINAPI_FAMILY")

#include <string>
#include <stdexcept>

using namespace NTServiceRpc;
using namespace std;

namespace
{
    void ThrowIfRpcError(RPC_STATUS code)
    {
        if (code != RPC_S_OK)
        {
            throw RpcCallException(code);
        }
    }

    void ThrowIfRemoteError(DWORD code)
    {
        if (code)
        {
            throw RpcRemoteException(code);
        }
    }
}

RpcCallException::RpcCallException(RPC_STATUS error) : runtime_error("RPC call failed: " + to_string(error))
{
}

RpcRemoteException::RpcRemoteException(DWORD error) : runtime_error("RPC server returned error " + to_string(error))
{
}

void RpcClient::Initialize()
{
    RPC_WSTR stringBinding = nullptr;

    ThrowIfRpcError(RpcStringBindingCompose(
        NULL,
        reinterpret_cast<RPC_WSTR>(L"ncalrpc"),
        NULL,
        reinterpret_cast<RPC_WSTR>(RPC_STATIC_ENDPOINT),
        NULL,
        &stringBinding));

    RPC_STATUS status = RpcBindingFromStringBinding(
        stringBinding, 
        &_rpcBinding);
    RpcStringFree(&stringBinding);
    ThrowIfRpcError(status);

    DWORD rpcResult;
    RpcTryExcept
    {
        rpcResult = ::RemoteOpen(_rpcBinding, &phContext);
    }
    RpcExcept(1)
    {
        throw RpcCallException(RpcExceptionCode());
    }
    RpcEndExcept
    ThrowIfRemoteError(rpcResult);
}

DWORD RpcClient::GetServiceStatus(const wchar_t *serviceName)
{
    DWORD rpcResult;
    RpcTryExcept
    {
        DWORD status;
        rpcResult = ::GetServiceStatus(phContext, &status, serviceName);
        if (!rpcResult)
        {
            return status;
        }
    }
    RpcExcept(1)
    {
        throw RpcCallException(RpcExceptionCode());
    }
    RpcEndExcept
    throw RpcRemoteException(rpcResult);
}

void RpcClient::RunService(const wchar_t *serviceName)
{
    DWORD rpcResult;
    RpcTryExcept
    {
        rpcResult = ::RunService(phContext, serviceName);
    }
    RpcExcept(1)
    {
        throw RpcCallException(RpcExceptionCode());
    }
    RpcEndExcept
    ThrowIfRemoteError(rpcResult);
}

void RpcClient::StopService(const wchar_t *serviceName)
{
    DWORD rpcResult;
    RpcTryExcept
    {
        rpcResult = ::StopService(phContext, serviceName);
    }
    RpcExcept(1)
    {
        throw RpcCallException(RpcExceptionCode());
    }
    RpcEndExcept
    ThrowIfRemoteError(rpcResult);
}

RpcClient::~RpcClient()
{
    RPC_STATUS status;

    if (_rpcBinding != NULL)
    {
        RpcTryExcept
        {
            ::RemoteClose(&phContext);
        }
        RpcExcept(1)
        {
            // Ignoring the result of RemoteClose as nothing can be
            // done on the client side with this return code
            status = RpcExceptionCode();
        }
        RpcEndExcept

        status = RpcBindingFree(&_rpcBinding);
        _rpcBinding = NULL;
    }
}

///******************************************************/
///*         MIDL allocate and free                     */
///******************************************************/

void __RPC_FAR * __RPC_USER midl_user_allocate(size_t len)
{
    return(malloc(len));
}

void __RPC_USER midl_user_free(void __RPC_FAR * ptr)
{
    free(ptr);
}
