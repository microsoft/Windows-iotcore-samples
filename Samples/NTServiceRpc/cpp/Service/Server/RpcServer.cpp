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

#include "stdafx.h"
#include "RpcServer.h"

#include <AclAPI.h>
#include <codecvt>
#include <iostream>
#include <locale>
#include <sddl.h>
#include <securitybaseapi.h>
#include <stdio.h>
#include <stdlib.h>
#include <Userenv.h>
#include <windows.h>

#include "RpcInterface_h.h"

using namespace RpcServer;
using namespace std;

namespace RpcServer
{
    WindowsCodeError::WindowsCodeError(const string& function, HRESULT code) : std::runtime_error(function + ": " + to_string(code))
    {
        this->code = code;
    }

    void ThrowLastError(const string& functionName, HRESULT errorCode)
    {
        if (!errorCode)
        {
            errorCode = GetLastError();
        }
        string errorMessage = functionName + " failed: " + to_string(errorCode) + "\n";
        wstring_convert<codecvt_utf8_utf16<wchar_t>> converter;
        OutputDebugString(converter.from_bytes(errorMessage).c_str());
        cerr << errorMessage;
        throw WindowsCodeError(functionName, errorCode);
    }
}

namespace
{
    const PWSTR AllowedPackageFamilyName = L"Microsoft.SDKSamples.NTServiceRpc.CPP_8wekyb3d8bbwe";
    RPC_BINDING_VECTOR* BindingVector = nullptr;

    PACL BuildAcl()
    {
        SID_IDENTIFIER_AUTHORITY SIDAuthWorld = SECURITY_WORLD_SID_AUTHORITY;
        PSID everyoneSid = nullptr;
        EXPLICIT_ACCESS ea[2] = {};

        // Get the SID that represents 'everyone' (this doesn't include AppContainers)
        if (!AllocateAndInitializeSid(
            &SIDAuthWorld, 1,
            SECURITY_WORLD_RID,
            0, 0, 0, 0, 0, 0, 0,
            &everyoneSid))
        {
            ThrowLastError("AllocateAndInitializeSid");
        }
        // Everyone GENERIC_ALL access
        ea[0].grfAccessMode = SET_ACCESS;
        ea[0].grfAccessPermissions = GENERIC_ALL;
        ea[0].grfInheritance = NO_INHERITANCE;
        ea[0].Trustee.TrusteeForm = TRUSTEE_IS_SID;
        ea[0].Trustee.TrusteeType = TRUSTEE_IS_WELL_KNOWN_GROUP;
        ea[0].Trustee.ptstrName = static_cast<LPWSTR>(everyoneSid);

        // When creating the RPC endpoint we want it to allow connections from the UWA with a
        // specific package family name. By default, RPC endpoints don't allow UWAs (AppContainer processes)
        // to connect to them, so we need to set the security on the endpoint to allow access to our specific
        // UWA.
        //
        // To do this we'll perform the following steps:
        // 1) Create a SID for the allowed package family name
        // 2) Create a security descriptor using that SID
        // 3) Create the RPC endpoint using that security descriptor
        PSID pfnSid = nullptr;
        HRESULT hResult = DeriveAppContainerSidFromAppContainerName(AllowedPackageFamilyName, &pfnSid);
        FreeSid(everyoneSid);
        if (hResult != S_OK)
        {
            ThrowLastError("DeriveAppContainerSidFromAppContainerName", hResult);
        }

        ea[1].grfAccessMode = SET_ACCESS;
        ea[1].grfAccessPermissions = GENERIC_ALL;
        ea[1].grfInheritance = NO_INHERITANCE;
        ea[1].Trustee.TrusteeForm = TRUSTEE_IS_SID;
        ea[1].Trustee.TrusteeType = TRUSTEE_IS_UNKNOWN;
        ea[1].Trustee.ptstrName = static_cast<LPWSTR>(pfnSid);
        PACL acl = nullptr;
        hResult = SetEntriesInAcl(ARRAYSIZE(ea), ea, nullptr, &acl);
        FreeSid(pfnSid);
        if (hResult != ERROR_SUCCESS)
        {
            ThrowLastError("DeriveAppContainerSidFromAppContainerName", hResult);
        }
        return acl;
    }

    void RegisterAndListen(PACL acl)
    {
        // Initialize an empty security descriptor
        SECURITY_DESCRIPTOR rpcSecurityDescriptor = {};
        if (!InitializeSecurityDescriptor(&rpcSecurityDescriptor, SECURITY_DESCRIPTOR_REVISION))
        {
            ThrowLastError("InitializeSecurityDescriptor");
        }

        // Assign the ACL to the security descriptor
        if (!SetSecurityDescriptorDacl(&rpcSecurityDescriptor, TRUE, acl, FALSE))
        {
            ThrowLastError("SetSecurityDescriptorDacl");
        }

        //
        // Bind to LRPC using dynamic endpoints
        //
        RPC_STATUS rpcStatus = RpcServerUseProtseqEp(
            reinterpret_cast<RPC_WSTR>(L"ncalrpc"),
            RPC_C_PROTSEQ_MAX_REQS_DEFAULT,
            reinterpret_cast<RPC_WSTR>(RPC_STATIC_ENDPOINT),
            &rpcSecurityDescriptor);
        if (rpcStatus != S_OK)
        {
            ThrowLastError("RpcServerUseProtseqEp", rpcStatus);
        }

        rpcStatus = RpcServerRegisterIf3(
            RpcInterface_v1_0_s_ifspec,
            nullptr,
            nullptr,
            RPC_IF_AUTOLISTEN | RPC_IF_ALLOW_LOCAL_ONLY,
            RPC_C_LISTEN_MAX_CALLS_DEFAULT,
            0,
            nullptr,
            &rpcSecurityDescriptor);
        if (rpcStatus != S_OK)
        {
            ThrowLastError("RpcServerRegisterIf3", rpcStatus);
        }

        rpcStatus = RpcServerInqBindings(&BindingVector);
        if (rpcStatus != RPC_S_OK)
        {
            ThrowLastError("RpcServerRegisterIf3", rpcStatus);
        }

        rpcStatus = RpcEpRegister(
            RpcInterface_v1_0_s_ifspec,
            BindingVector,
            nullptr,
            nullptr);
        if (rpcStatus != RPC_S_OK)
        {
            ThrowLastError("RpcEpRegister", rpcStatus);
        }

        rpcStatus = RpcServerListen(
            1,
            RPC_C_LISTEN_MAX_CALLS_DEFAULT,
            false);
        if (rpcStatus != RPC_S_OK && rpcStatus != RPC_S_ALREADY_LISTENING)
        {
            ThrowLastError("RpcServerListen", rpcStatus);
        }
    }
}

//
// Routine to create RPC server and listen to incoming RPC calls
//
DWORD RpcServerStart()
{
    PACL acl = BuildAcl();
    HRESULT error = 0;
    try
    {
        RegisterAndListen(acl);
    }
    catch (WindowsCodeError& e)
    {
        error = e.code;
    }
    LocalFree(acl);
    return error;
}

//
// Notify rpc server to stop listening to incoming rpc calls
//
void RpcServerDisconnect()
{
    RPC_STATUS rpcStatus = RpcServerUnregisterIf(RpcInterface_v1_0_s_ifspec, nullptr, 0);
    if (rpcStatus != RPC_S_OK)
    {
        OutputDebugString((L"RpcServerUnregisterIf: " + to_wstring(rpcStatus)).c_str());
    }
    if (BindingVector != nullptr)
    {
        rpcStatus = RpcEpUnregister(RpcInterface_v1_0_s_ifspec, BindingVector, nullptr);
        if (rpcStatus != RPC_S_OK)
        {
            OutputDebugString((L"RpcEpUnregister: " + to_wstring(rpcStatus)).c_str());
        }
        RpcBindingVectorFree(&BindingVector);
        BindingVector = nullptr;
    }
}

//
// Rpc method to retrieve client context handle
//
DWORD RemoteOpen(
    handle_t hBinding,
    PPCONTEXT_HANDLE_TYPE pphContext)
{
    *pphContext = static_cast<PCONTEXT_HANDLE_TYPE *>(midl_user_allocate(sizeof(SERVICE_CONTROL_CONTEXT)));
    if (!pphContext)
    {
        return -1;
    }
    SERVICE_CONTROL_CONTEXT* serviceControlContext = static_cast<SERVICE_CONTROL_CONTEXT *>(*pphContext);
    serviceControlContext->serviceControl = new ServiceControl();
    return 0;
}

//
// Rpc method to close the client context handle
//
void RemoteClose(PPCONTEXT_HANDLE_TYPE pphContext)
{
    if (*pphContext == nullptr)
    {
        //Log error, client tried to close a NULL handle.
        return;
    }

    SERVICE_CONTROL_CONTEXT* serviceControlContext = static_cast<SERVICE_CONTROL_CONTEXT *>(*pphContext);
    delete serviceControlContext->serviceControl;
    midl_user_free(serviceControlContext);

    // This tells the run-time, when it is marshalling the out 
    // parameters, that the context handle has been closed normally.
    *pphContext = nullptr;
}

//
// Routine to cleanup client context when client has died with active 
// connection with server
//
void __RPC_USER PCONTEXT_HANDLE_TYPE_rundown(
    PCONTEXT_HANDLE_TYPE phContext)
{
    RemoteClose(&phContext);
}

DWORD GetServiceStatus(
    PCONTEXT_HANDLE_TYPE phContext,
    DWORD *status,
    const wchar_t *serviceName)
{
    SERVICE_CONTROL_CONTEXT* serviceControlContext = static_cast<SERVICE_CONTROL_CONTEXT *>(phContext);
    try
    {
        *status = serviceControlContext->serviceControl->GetServiceStatus(serviceName);
        return 0;
    }
    catch (WindowsCodeError& e)
    {
        return e.code;
    }
}

DWORD RunService(
    PCONTEXT_HANDLE_TYPE phContext,
    const wchar_t *serviceName)
{
    SERVICE_CONTROL_CONTEXT* serviceControlContext = static_cast<SERVICE_CONTROL_CONTEXT *>(phContext);
    try
    {
        serviceControlContext->serviceControl->RunService(serviceName);
        return 0;
    }
    catch (WindowsCodeError& e)
    {
        return e.code;
    }
}

DWORD StopService(
    PCONTEXT_HANDLE_TYPE phContext,
    const wchar_t *serviceName)
{
    SERVICE_CONTROL_CONTEXT* serviceControlContext = static_cast<SERVICE_CONTROL_CONTEXT *>(phContext);
    try
    {
        serviceControlContext->serviceControl->StopService(serviceName);
        return 0;
    }
    catch (WindowsCodeError& e)
    {
        return e.code;
    }
}

/******************************************************/
/*         MIDL allocate and free                     */
/******************************************************/

void __RPC_FAR * __RPC_USER midl_user_allocate(size_t len)
{
    return(malloc(len));
}

void __RPC_USER midl_user_free(void __RPC_FAR* ptr)
{
    free(ptr);
}
