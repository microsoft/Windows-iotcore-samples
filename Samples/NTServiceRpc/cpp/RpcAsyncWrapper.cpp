#include "pch.h"
#include "RpcAsyncWrapper.h"

#include <iostream>
#include <stdexcept>
#include <string>

using namespace std;

namespace NTServiceRpc
{
    Concurrency::task<void> RpcAsyncWrapper::Connect()
    {
        return Concurrency::create_task([this]
        {
            rpcClient = make_unique<NTServiceRpc::RpcClient>();
            rpcClient->Initialize();
        });
    }

    Concurrency::task<RpcAsyncWrapper::ServiceStatus> RpcAsyncWrapper::GetServiceStatus(const wchar_t * serviceName)
    {
        return Concurrency::create_task([this, serviceName]
        {
            return static_cast<RpcAsyncWrapper::ServiceStatus>(rpcClient->GetServiceStatus(serviceName));
        });
    }

    Concurrency::task<void> RpcAsyncWrapper::RunService(const wchar_t * serviceName)
    {
        return Concurrency::create_task([this, serviceName]
        {
            return rpcClient->RunService(serviceName);
        });
    }

    Concurrency::task<void> RpcAsyncWrapper::StopService(const wchar_t * serviceName)
    {
        return Concurrency::create_task([this, serviceName]
        {
            return rpcClient->StopService(serviceName);
        });
    }
}
