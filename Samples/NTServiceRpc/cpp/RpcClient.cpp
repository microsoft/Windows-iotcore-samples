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

RpcCallException::RpcCallException(RPC_STATUS status) : runtime_error("")
{
    message = "RPC call failed: " + to_string(status);
}

const char* RpcCallException::what() const
{
    return message.c_str();
}

void RpcClient::Initialize()
{
    RPC_WSTR stringBinding = nullptr;

    RPC_STATUS status = RpcStringBindingCompose(
        NULL,
        reinterpret_cast<RPC_WSTR>(L"ncalrpc"),
        NULL,
        reinterpret_cast<RPC_WSTR>(RPC_STATIC_ENDPOINT),
        NULL,
        &stringBinding);

    if (status == RPC_S_OK)
    {
        goto cleanup;
    }

    status = RpcBindingFromStringBinding(
        stringBinding, 
        &hRpcBinding);

    if (status)
    {
        goto cleanup;
    }

    RpcTryExcept
    {
        ::RemoteOpen(hRpcBinding, &phContext);
    }
    RpcExcept(1)
    {
        status = RpcExceptionCode();
    }
    RpcEndExcept

cleanup:
    if (stringBinding)
    {
        RPC_STATUS freeStatus;
        freeStatus = RpcStringFree(&stringBinding);
        if (!status)
        {
            status = freeStatus;
        }
    }

    if (status)
    {
        throw RpcCallException(status);
    }
}

DWORD RpcClient::GetServiceStatus(const wchar_t *serviceName)
{
    RpcTryExcept
    {
        return ::GetServiceStatus(phContext, serviceName);
    }
    RpcExcept(1)
    {
        throw RpcCallException(RpcExceptionCode());
    }
    RpcEndExcept
}

bool RpcClient::RunService(const wchar_t *serviceName)
{
    RpcTryExcept
    {
        return ::RunService(phContext, serviceName);
    }
        RpcExcept(1)
    {
        throw RpcCallException(RpcExceptionCode());
    }
    RpcEndExcept
}

bool RpcClient::StopService(const wchar_t *serviceName)
{
    RpcTryExcept
    {
        return ::StopService(phContext, serviceName);
    }
        RpcExcept(1)
    {
        throw RpcCallException(RpcExceptionCode());
    }
    RpcEndExcept
}

RpcClient::~RpcClient()
{
    RPC_STATUS status;

    if (hRpcBinding != NULL) 
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

        status = RpcBindingFree(&hRpcBinding);
        hRpcBinding = NULL;
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
