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
#include "ServiceBase.h"
#include "SampleService.h"

#define SERVICE_NAME             L"ServiceStatus"
#define SERVICE_DISPLAY_NAME     L"Status and control of services through RPC"
#define SERVICE_START_TYPE       SERVICE_DEMAND_START
// List of service dependencies - "dep1\0dep2\0\0"
#define SERVICE_DEPENDENCIES     L""

namespace
{
    bool RunCommand(wchar_t *command)
    {
        if (*command != L'-' && *command != L'/')
        {
            return false;
        }
        if (!_wcsicmp(L"install", command + 1))
        {
            InstallService(
                SERVICE_NAME,           // Name of service
                SERVICE_DISPLAY_NAME,   // Name to display
                SERVICE_START_TYPE,     // Service start type
                SERVICE_DEPENDENCIES,   // Dependencies
                NULL,                   // Service running account - NULL uses LocalSystem account
                NULL                    // Password of the account
            );
        }
        else if (!_wcsicmp(L"remove", command + 1))
        {
            UninstallService(SERVICE_NAME);
        }
        else if (!_wcsicmp(L"console", command + 1))
        {
            CSampleService service(SERVICE_NAME);
            service.ConsoleRun();
        }
        else
        {
            return false;
        }
        return true;
    }

    void Usage()
    {
        wprintf(L"Usage:\n");
        wprintf(L"\t-install  Install service.\n");
        wprintf(L"\t-remove   Remove service.\n");
        wprintf(L"\t-console  Run in console mode.\n");
    }
}

int wmain(int argc, wchar_t **argv)
{
    if (argc > 2)
    {
        fwprintf(stderr, L"Invalid number of command line arguments.\n");
        Usage();
        return -1;
    }

    if (argc == 1)
    {
        CSampleService service(SERVICE_NAME);
        if (!CServiceBase::Run(service))
        {
            wprintf(L"Service failed: 0x%08lx\n", GetLastError());
            return -1;
        }
        return 0;
    }

    if (!RunCommand(argv[1]))
    {
        Usage();
        return -1;
    }

    return 0;
}