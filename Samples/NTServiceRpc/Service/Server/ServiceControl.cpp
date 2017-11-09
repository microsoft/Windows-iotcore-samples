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
#include <iostream>

#include "RpcInterface_h.h" 

using namespace RpcServer;
using namespace std;

namespace
{
    void PrintLastError(const string& functionName)
    {
        cerr << functionName.c_str() << " failed: " << GetLastError() << "\n";
    }

    /*
     * Gets a service with given service control manager and service permissions.
     * For a list of possible permissions, see https://msdn.microsoft.com/en-us/library/windows/desktop/ms685981(v=vs.85).aspx .
     */
    SC_HANDLE GetService(_In_ const wchar_t *serviceName, DWORD serviceControlManagerPermissions, DWORD servicePermission)
    {
        SC_HANDLE service = NULL;
        SC_HANDLE serviceControlManager = OpenSCManager(NULL, NULL, serviceControlManagerPermissions);
        if (serviceControlManager == NULL)
        {
            PrintLastError("OpenSCManager");
            return NULL;
        }

        service = OpenService(serviceControlManager, serviceName, servicePermission);
        if (service == NULL)
        {
            PrintLastError("OpenService");
        }
        CloseServiceHandle(serviceControlManager);
        return service;
    }
}

DWORD ServiceControl::GetServiceStatus(_In_ const wchar_t *serviceName)
{
    SC_HANDLE service = GetService(serviceName, 0, SERVICE_QUERY_STATUS);
    if (service == NULL)
    {
        return -1;
    }

    SERVICE_STATUS_PROCESS stat;
    DWORD needed = 0;
    DWORD status;
    if (!QueryServiceStatusEx(service, SC_STATUS_PROCESS_INFO, (BYTE*)&stat, sizeof stat, &needed))
    {
        PrintLastError("QueryServiceStatusEx");
        status = -1;
    }
    else
    {
        status = stat.dwCurrentState;
    }

    CloseServiceHandle(service);
    return status;
}

boolean ServiceControl::RunService(_In_ const wchar_t *serviceName)
{
    SC_HANDLE service = GetService(serviceName, 0, SERVICE_START);
    if (service == NULL)
    {
        return false;
    }

    boolean status = StartService(service, 0, NULL);
    if (!status)
    {
        PrintLastError("StartService");
    }

    CloseServiceHandle(service);
    return status;
}

boolean ServiceControl::StopService(_In_ const wchar_t *serviceName)
{
    SC_HANDLE service = GetService(serviceName, 0, SERVICE_STOP);
    if (service == NULL)
    {
        return false;
    }

    SERVICE_STATUS serviceStatus;
    boolean status = ControlService(service, SERVICE_CONTROL_STOP, &serviceStatus);
    if (!status)
    {
        PrintLastError("ControlService");
    }

    CloseServiceHandle(service);
    return status;
}
