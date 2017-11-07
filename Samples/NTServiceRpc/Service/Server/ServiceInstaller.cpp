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

#include <stdio.h>
#include <windows.h>

#include "ServiceInstaller.h"

//
//   FUNCTION: InstallService
//
//   PURPOSE: Install the current application as a service to the local 
//   service control manager database.
//
//   PARAMETERS:
//   * pszServiceName - the name of the service to be installed
//   * pszDisplayName - the display name of the service
//   * dwStartType - the service start option. This parameter can be one of 
//     the following values: SERVICE_AUTO_START, SERVICE_BOOT_START, 
//     SERVICE_DEMAND_START, SERVICE_DISABLED, SERVICE_SYSTEM_START.
//   * pszDependencies - a pointer to a double null-terminated array of null-
//     separated names of services or load ordering groups that the system 
//     must start before this service.
//   * pszAccount - the name of the account under which the service runs.
//   * pszPassword - the password to the account name.
//
//   NOTE: If the function fails to install the service, it prints the error 
//   in the standard output stream for users to diagnose the problem.
//
void InstallService(
    _In_ PWSTR pszServiceName,
    _In_ PWSTR pszDisplayName,
    _In_ DWORD dwStartType,
    _In_ PWSTR pszDependencies,
    _In_ PWSTR pszAccount,
    _In_ PWSTR pszPassword)
{
    wchar_t szPath[MAX_PATH];
    if (GetModuleFileName(nullptr, szPath, ARRAYSIZE(szPath)) == 0)
    {
        wprintf(L"GetModuleFileName failed w/err 0x%08lx\n", GetLastError());
        return;
    }

    // Open the local default service control manager database
    SC_HANDLE schSCManager = OpenSCManager(nullptr, nullptr, SC_MANAGER_CONNECT | SC_MANAGER_CREATE_SERVICE);
    if (schSCManager == nullptr)
    {
        wprintf(L"OpenSCManager failed w/err 0x%08lx\n", GetLastError());
        return;
    }

    // Install the service into SCM
    SC_HANDLE schService = CreateService(
        schSCManager,                   // SCManager database
        pszServiceName,                 // Name of service
        pszDisplayName,                 // Name to display
        SERVICE_QUERY_STATUS,           // Desired access
        SERVICE_WIN32_OWN_PROCESS,      // Service type
        dwStartType,                    // Service start type
        SERVICE_ERROR_NORMAL,           // Error control type
        szPath,                         // Service's binary
        nullptr,                           // No load ordering group
        nullptr,                           // No tag identifier
        pszDependencies,                // Dependencies
        pszAccount,                     // Service running account
        pszPassword                     // Password of the account
    );
    CloseServiceHandle(schSCManager);

    if (schService == nullptr)
    {
        wprintf(L"CreateService failed w/err 0x%08lx\n", GetLastError());
        return;
    }

    wprintf(L"%s is installed.\n", pszServiceName);
    CloseServiceHandle(schService);
}


//
//   FUNCTION: UninstallService
//
//   PURPOSE: Stop and remove the service from the local service control 
//   manager database.
//
//   PARAMETERS: 
//   * pszServiceName - the name of the service to be removed.
//
//   NOTE: If the function fails to uninstall the service, it prints the 
//   error in the standard output stream for users to diagnose the problem.
//
void UninstallService(_In_ PWSTR pszServiceName)
{
    SERVICE_STATUS ssSvcStatus = {};

    // Open the local default service control manager database
    SC_HANDLE schSCManager = OpenSCManager(nullptr, nullptr, SC_MANAGER_CONNECT);
    if (schSCManager == nullptr)
    {
        wprintf(L"OpenSCManager failed w/err 0x%08lx\n", GetLastError());
        return;
    }

    SC_HANDLE schService = OpenService(schSCManager, pszServiceName, SERVICE_STOP | SERVICE_QUERY_STATUS | DELETE);
    CloseServiceHandle(schSCManager);
    if (schService == nullptr)
    {
        wprintf(L"OpenService failed w/err 0x%08lx\n", GetLastError());
        return;
    }

    if (ControlService(schService, SERVICE_CONTROL_STOP, &ssSvcStatus))
    {
        wprintf(L"Stopping %s.", pszServiceName);
        Sleep(1000);

        while (QueryServiceStatus(schService, &ssSvcStatus) && ssSvcStatus.dwCurrentState == SERVICE_STOP_PENDING)
        {
            wprintf(L".");
            Sleep(1000);
        }

        wprintf(ssSvcStatus.dwCurrentState == SERVICE_STOPPED ? L"\n%s is stopped.\n" : L"\n%s failed to stop.\n", pszServiceName);
    }

    if (!DeleteService(schService))
    {
        wprintf(L"DeleteService failed w/err 0x%08lx\n", GetLastError());
    }

    CloseServiceHandle(schService);
}