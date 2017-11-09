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
#include <stdlib.h>
#include <stdio.h>
#include <iostream>
#include "RpcInterface_h.h"
#include <windows.h>
#include <Userenv.h>

#include <sddl.h>
#include <securitybaseapi.h>
#include <AclAPI.h>
#include "RpcServer.h"

using namespace RpcServer;
using namespace std;

namespace
{
    const PWSTR AllowedPackageFamilyName = L"Microsoft.SDKSamples.NTServiceRpc.CPP_8wekyb3d8bbwe";
    RPC_BINDING_VECTOR* BindingVector = nullptr;

    void PrintLastError(const string& functionName)
    {
        cerr << functionName.c_str() << " failed: " << GetLastError() << "\n";
    }

    void PrintLastError(const string& functionName, HRESULT result)
    {
        cerr << functionName.c_str() << " failed: " << result << "\n";
    }

    PACL BuildAcl()
    {
        SID_IDENTIFIER_AUTHORITY SIDAuthWorld = SECURITY_WORLD_SID_AUTHORITY;
        PSID everyoneSid = nullptr;
        PSID pfnSid = nullptr;
        EXPLICIT_ACCESS ea[2] = {};
        PACL acl = nullptr;

        // Get the SID that represents 'everyone' (this doesn't include AppContainers)
        if (!AllocateAndInitializeSid(
            &SIDAuthWorld, 1,
            SECURITY_WORLD_RID,
            0, 0, 0, 0, 0, 0, 0,
            &everyoneSid))
        {
            PrintLastError("AllocateAndInitializeSid");
            return NULL;
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
        HRESULT hResult = DeriveAppContainerSidFromAppContainerName(AllowedPackageFamilyName, &pfnSid);
        if (hResult != S_OK)
        {
            PrintLastError("DeriveAppContainerSidFromAppContainerName", hResult);
            goto cleanup;
        }

        ea[1].grfAccessMode = SET_ACCESS;
        ea[1].grfAccessPermissions = GENERIC_ALL;
        ea[1].grfInheritance = NO_INHERITANCE;
        ea[1].Trustee.TrusteeForm = TRUSTEE_IS_SID;
        ea[1].Trustee.TrusteeType = TRUSTEE_IS_UNKNOWN;
        ea[1].Trustee.ptstrName = static_cast<LPWSTR>(pfnSid);
        hResult = SetEntriesInAcl(ARRAYSIZE(ea), ea, nullptr, &acl);
        if (hResult != ERROR_SUCCESS)
        {
            PrintLastError("DeriveAppContainerSidFromAppContainerName", hResult);
        }

    cleanup:
        FreeSid(everyoneSid);
        if (pfnSid)
        {
            FreeSid(pfnSid);
        }
        return acl;
    }
}

//
// Routine to create RPC server and listen to incoming RPC calls
//
DWORD RpcServerStart()
{
    DWORD hResult = S_OK;
    WCHAR* protocolSequence = L"ncalrpc";

    PACL acl = BuildAcl();
    if (!acl)
    {
        return -1;
    }

    // Initialize an empty security descriptor
    SECURITY_DESCRIPTOR rpcSecurityDescriptor = {};
    if (!InitializeSecurityDescriptor(&rpcSecurityDescriptor, SECURITY_DESCRIPTOR_REVISION))
    {
        hResult = GetLastError();
        goto end;
    }

    // Assign the ACL to the security descriptor
    if (!SetSecurityDescriptorDacl(&rpcSecurityDescriptor, TRUE, acl, FALSE))
    {
        hResult = GetLastError();
        goto end;
    }

    //
    // Bind to LRPC using dynamic endpoints
    //
    hResult = RpcServerUseProtseqEp(
        reinterpret_cast<RPC_WSTR>(protocolSequence),
        RPC_C_PROTSEQ_MAX_REQS_DEFAULT,
        reinterpret_cast<RPC_WSTR>(RPC_STATIC_ENDPOINT),
        &rpcSecurityDescriptor);

    if (hResult != S_OK)
    {
        goto end;
    }

    hResult = RpcServerRegisterIf3(
        RpcInterface_v1_0_s_ifspec,
        nullptr,
        nullptr,
        RPC_IF_AUTOLISTEN | RPC_IF_ALLOW_LOCAL_ONLY,
        RPC_C_LISTEN_MAX_CALLS_DEFAULT,
        0,
        nullptr,
        &rpcSecurityDescriptor);

    if (hResult != S_OK)
    {
        goto end;
    }

    hResult = RpcServerInqBindings(&BindingVector);

    if (hResult != S_OK)
    {
        goto end;
    }

    hResult = RpcEpRegister(
        RpcInterface_v1_0_s_ifspec,
        BindingVector,
        nullptr,
        nullptr);

    if (hResult != S_OK)
    {
        goto end;
    }

    hResult = RpcServerListen(
        1,
        RPC_C_LISTEN_MAX_CALLS_DEFAULT,
        false);

    if (hResult == RPC_S_ALREADY_LISTENING)
    {
        hResult = RPC_S_OK;
    }

end:

    // cleanup acl
    if (acl != nullptr)
    {
        LocalFree(acl);
    }

    return hResult;
}

//
// Notify rpc server to stop listening to incoming rpc calls
//
void RpcServerDisconnect()
{
    RpcServerUnregisterIf(RpcInterface_v1_0_s_ifspec, nullptr, 0);
    RpcEpUnregister(RpcInterface_v1_0_s_ifspec, BindingVector, nullptr);
    if (BindingVector != nullptr)
    {
        RpcBindingVectorFree(&BindingVector);
        BindingVector = nullptr;
    }
}

//
// Rpc method to retrieve client context handle
//
void RemoteOpen(
    _In_ handle_t hBinding,
    _Out_ PPCONTEXT_HANDLE_TYPE pphContext)
{
    *pphContext = static_cast<PCONTEXT_HANDLE_TYPE *>(midl_user_allocate(sizeof(SERVICE_CONTROL_CONTEXT)));
    SERVICE_CONTROL_CONTEXT* serviceControlContext = static_cast<SERVICE_CONTROL_CONTEXT *>(*pphContext);
    serviceControlContext->serviceControl = new ServiceControl();
}

//
// Rpc method to close the client context handle
//
void RemoteClose(_Inout_ PPCONTEXT_HANDLE_TYPE pphContext)
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
    _In_ PCONTEXT_HANDLE_TYPE phContext)
{
    RemoteClose(&phContext);
}

DWORD GetServiceStatus(
    _In_ PCONTEXT_HANDLE_TYPE phContext,
    _In_ const wchar_t *serviceName)
{
    SERVICE_CONTROL_CONTEXT* serviceControlContext = static_cast<SERVICE_CONTROL_CONTEXT *>(phContext);
    return serviceControlContext->serviceControl->GetServiceStatus(serviceName);
}

boolean RunService(
    _In_ PCONTEXT_HANDLE_TYPE phContext,
    _In_ const wchar_t *serviceName)
{
    SERVICE_CONTROL_CONTEXT* serviceControlContext = static_cast<SERVICE_CONTROL_CONTEXT *>(phContext);
    return serviceControlContext->serviceControl->RunService(serviceName);
}

boolean StopService(
    _In_ PCONTEXT_HANDLE_TYPE phContext,
    _In_ const wchar_t *serviceName)
{
    SERVICE_CONTROL_CONTEXT* serviceControlContext = static_cast<SERVICE_CONTROL_CONTEXT *>(phContext);
    return serviceControlContext->serviceControl->StopService(serviceName);
}

/******************************************************/
/*         MIDL allocate and free                     */
/******************************************************/

void __RPC_FAR * __RPC_USER midl_user_allocate(_In_ size_t len)
{
    return(malloc(len));
}

void __RPC_USER midl_user_free(_In_ void __RPC_FAR* ptr)
{
    free(ptr);
}
