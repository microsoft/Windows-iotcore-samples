#pragma once

#include "stdafx.h"
#include <windows.h>

#include <iostream>
#include <string>

namespace RpcServer
{
    class ServiceControl
    {
    public:
        DWORD GetServiceStatus(const wchar_t *serviceName);
        void RunService(const wchar_t *serviceName);
        void StopService(const wchar_t *serviceName);

    private:
        SC_HANDLE GetService(const wchar_t *serviceName, DWORD serviceControlManagerPermissions, DWORD servicePermission);
    };
}
