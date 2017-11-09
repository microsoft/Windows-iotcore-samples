#include "stdafx.h"
#include <windows.h>

#include <iostream>

namespace RpcServer
{
    class ServiceControl
    {
    public:
        DWORD GetServiceStatus(_In_ const wchar_t *serviceName);
        boolean RunService(_In_ const wchar_t *serviceName);
        boolean StopService(_In_ const wchar_t *serviceName);
    };
}
