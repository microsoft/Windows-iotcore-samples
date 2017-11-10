#pragma once

#include "RpcClient.h"
#include <memory>

namespace NTServiceRpc
{
    class RpcAsyncWrapper final
    {
    public:
        enum ServiceStatus : DWORD
        {
            SERVICE_STOPPED = 1,
            SERVICE_START_PENDING,
            SERVICE_STOP_PENDING,
            SERVICE_RUNNING,
            SERVICE_CONTINUE_PENDING,
            SERVICE_PAUSE_PENDING,
            SERVICE_PAUSED,
        };

        Concurrency::task<void> Connect();
        Concurrency::task<ServiceStatus> GetServiceStatus(const wchar_t * serviceName);
        Concurrency::task<void> RunService(const wchar_t * serviceName);
        Concurrency::task<void> StopService(const wchar_t * serviceName);
    private:
        std::unique_ptr<RpcClient> rpcClient;
    };
}
