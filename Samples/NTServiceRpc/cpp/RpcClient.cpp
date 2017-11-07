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

using namespace NTServiceRpc;

namespace
{
    RPC_STATUS DoRunService(PCONTEXT_HANDLE_TYPE context, const wchar_t *serviceName)
    {
        RpcTryExcept
        {
            return !::RunService(context, serviceName);
        }
        RpcExcept(1)
        {
            return RpcExceptionCode();
        }
        RpcEndExcept
        return 0;
    }

    RPC_STATUS DoStopService(PCONTEXT_HANDLE_TYPE context, const wchar_t *serviceName)
    {
        RpcTryExcept
        {
            return !::StopService(context, serviceName);
        }
        RpcExcept(1)
        {
            return RpcExceptionCode();
        }
        RpcEndExcept
        return 0;
    }
}

__int64 RpcClient::Initialize()
{
    RPC_STATUS status;
    RPC_WSTR pszStringBinding = nullptr;

    status = RpcStringBindingCompose(
        NULL,
        reinterpret_cast<RPC_WSTR>(L"ncalrpc"),
        NULL,
        reinterpret_cast<RPC_WSTR>(RPC_STATIC_ENDPOINT),
        NULL,
        &pszStringBinding);

    if (status)
    {
        goto cleanup;
    }

    status = RpcBindingFromStringBinding(
        pszStringBinding, 
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
    if (pszStringBinding)
    {
        RPC_STATUS freeStatus;
        freeStatus = RpcStringFree(&pszStringBinding);
        if (!status)
        {
            status = freeStatus;
        }
    }

    return status;
}

DWORD RpcClient::GetServiceStatus(const wchar_t *serviceName)
{
    RPC_STATUS status = 0;
    RpcTryExcept
    {
        return ::GetServiceStatus(phContext, serviceName);
    }
    RpcExcept(1)
    {
        status = RpcExceptionCode();
    }
    RpcEndExcept
    return -status;
}

boolean RpcClient::RunService(const wchar_t *serviceName)
{
    RPC_STATUS status = DoRunService(phContext, serviceName);
    if (status)
    {
        OutputDebugString((L"RPC call failed with " + std::to_wstring(status)).c_str());
        return false;
    }
    return true;
}

boolean RpcClient::StopService(const wchar_t *serviceName)
{
    RPC_STATUS status = DoStopService(phContext, serviceName);
    if (status)
    {
        OutputDebugString((L"RPC call failed with " + std::to_wstring(status)).c_str());
        return false;
    }
    return true;
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