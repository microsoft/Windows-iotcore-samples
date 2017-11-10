#include "stdafx.h"
#include <windows.h>

#include <iostream>
#include <string>

namespace RpcServer
{
    class WindowsCodeError : public std::runtime_error
    {
    public:
        WindowsCodeError(const std::string& function, DWORD error);
        DWORD code;
    };

    class ServiceControl
    {
    public:
        DWORD GetServiceStatus(const wchar_t *serviceName);
        void RunService(const wchar_t *serviceName);
        void StopService(const wchar_t *serviceName);

    private:
        void ThrowLastError(const std::string& functionName);
        SC_HANDLE GetService(const wchar_t *serviceName, DWORD serviceControlManagerPermissions, DWORD servicePermission);
    };
}
