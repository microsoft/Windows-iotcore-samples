#include "pch.h"
#include "RpcAsyncWrapper.h"

#include <iostream>
#include <stdexcept>
#include <string>

namespace NTServiceRpc
{
    Concurrency::task<bool> RpcAsyncWrapper::Connect()
    {
        running = false;
        return Concurrency::create_task([this]
        {
            rpcClient = std::make_unique<NTServiceRpc::RpcClient>();
            auto status = rpcClient->Initialize();
            if (status)
            {
                OutputDebugString((L"RPC client initialization failed: " + std::to_wstring(status) + L"\n").c_str());
            }
            else
            {
                running = true;
            }
            return running;
        });
    }

    Concurrency::task<RpcAsyncWrapper::ServiceStatus> RpcAsyncWrapper::GetServiceStatus(const wchar_t * serviceName)
    {
        if (!running)
        {
            OutputDebugString(L"RPC connection not up yet\n");
            return Concurrency::create_task([] { return static_cast<RpcAsyncWrapper::ServiceStatus>(-1); });
        }
        return Concurrency::create_task([this, serviceName]
        {
            return static_cast<RpcAsyncWrapper::ServiceStatus>(rpcClient->GetServiceStatus(serviceName));
        });
    }

    Concurrency::task<boolean> RpcAsyncWrapper::RunService(const wchar_t * serviceName)
    {
        if (!running)
        {
            OutputDebugString(L"RPC connection not up yet\n");
            return Concurrency::create_task([] { return static_cast<boolean>(false); });
        }
        return Concurrency::create_task([this, serviceName]
        {
            return rpcClient->RunService(serviceName);
        });
    }

    Concurrency::task<boolean> RpcAsyncWrapper::StopService(const wchar_t * serviceName)
    {
        if (!running)
        {
            OutputDebugString(L"RPC connection not up yet\n");
            return Concurrency::create_task([] { return static_cast<boolean>(false); });
        }
        return Concurrency::create_task([this, serviceName]
        {
            return rpcClient->StopService(serviceName);
        });
    }
}
