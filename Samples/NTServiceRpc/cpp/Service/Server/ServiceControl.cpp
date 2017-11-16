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
#include "ServiceControl.h"

#include <assert.h>
#include <codecvt>
#include <iostream>
#include <locale>
#include <string>

#include "RpcInterface_h.h" 
#include "RpcServer.h"

using namespace RpcServer;
using namespace std;

DWORD ServiceControl::GetServiceStatus(const wchar_t *serviceName)
{
    SC_HANDLE service = GetService(serviceName, 0, SERVICE_QUERY_STATUS);
    SERVICE_STATUS_PROCESS stat;
    DWORD needed = 0;
    bool success = QueryServiceStatusEx(service, SC_STATUS_PROCESS_INFO, (BYTE*)&stat, sizeof stat, &needed);
    CloseServiceHandle(service);
    if (!success)
    {
        ThrowLastError("QueryServiceStatusEx");
    }
    return stat.dwCurrentState;
}

void ServiceControl::RunService(const wchar_t *serviceName)
{
    SC_HANDLE service = GetService(serviceName, 0, SERVICE_START);
    bool success = StartService(service, 0, NULL);
    CloseServiceHandle(service);
    if (!success)
    {
        ThrowLastError("StartService");
    }
}

void ServiceControl::StopService(const wchar_t *serviceName)
{
    SC_HANDLE service = GetService(serviceName, 0, SERVICE_STOP);

    SERVICE_STATUS serviceStatus;
    bool success = ControlService(service, SERVICE_CONTROL_STOP, &serviceStatus);
    CloseServiceHandle(service);
    if (!success)
    {
        ThrowLastError("ControlService");
    }
}

/*
 * Gets a service with given service control manager and service permissions.
 * For a list of possible permissions, see https://msdn.microsoft.com/en-us/library/windows/desktop/ms685981(v=vs.85).aspx .
 */
SC_HANDLE ServiceControl::GetService(const wchar_t *serviceName, DWORD serviceControlManagerPermissions, DWORD servicePermission)
{
    SC_HANDLE service = NULL;
    SC_HANDLE serviceControlManager = OpenSCManager(NULL, NULL, serviceControlManagerPermissions);
    if (serviceControlManager == NULL)
    {
        ThrowLastError("OpenSCManager");
    }

    service = OpenService(serviceControlManager, serviceName, servicePermission);
    CloseServiceHandle(serviceControlManager);
    if (service == NULL)
    {
        ThrowLastError("OpenService");
    }
    return service;
}