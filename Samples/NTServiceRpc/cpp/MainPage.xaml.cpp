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

#include "pch.h"
#include "MainPage.xaml.h"

#include <codecvt>
#include <locale>
#include <string>

using namespace NTServiceRpc;
using namespace std;

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::UI::Core;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls::Primitives;

namespace
{
    wstring ServiceStatusString(RpcAsyncWrapper::ServiceStatus status)
    {
        switch (status)
        {
        case RpcAsyncWrapper::SERVICE_STOPPED:
            return L"Service is stopped";
        case RpcAsyncWrapper::SERVICE_START_PENDING:
            return L"Service's start is pending";
        case RpcAsyncWrapper::SERVICE_STOP_PENDING:
            return L"Service's stop is pending";
        case RpcAsyncWrapper::SERVICE_RUNNING:
            return L"Service is running";
        case RpcAsyncWrapper::SERVICE_CONTINUE_PENDING:
            return L"Service's continue is pending";
        case RpcAsyncWrapper::SERVICE_PAUSE_PENDING:
            return L"Service's pause is pending";
        case RpcAsyncWrapper::SERVICE_PAUSED:
            return L"Service is paused";
        default:
            return L"Unknown service status: " + to_wstring(status);
        }
    }

    String^ CharToSystemString(const char *source)
    {
        wstring_convert<codecvt_utf8_utf16<wchar_t>> converter;
        return ref new String(converter.from_bytes(source).c_str());
    }
}

MainPage::MainPage()
{
	InitializeComponent();
}

void MainPage::Page_Loaded(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e)
{
    ConnectToService();
}

void MainPage::NotifyUser(String^ message)
{
    if (Dispatcher->HasThreadAccess)
    {
        UserNotification->Text = message;
    }
    else
    {
        Dispatcher->RunAsync(CoreDispatcherPriority::Normal, ref new DispatchedHandler([this, message]
        {
            UserNotification->Text = message;
        }));
    }
}

void MainPage::ConnectToService()
{
    rpc.Connect().then([this](Concurrency::task<void> t)
    {
        CatchRpcException(t, static_cast<function<void()>>([this]() {
            NotifyUser("Connected to service");
        }));
    });
}

void MainPage::Connect_Click(Object^, RoutedEventArgs^)
{
    ConnectToService();
}

void MainPage::GetStatus_Click(Object^ sender, RoutedEventArgs^ e)
{
    rpc.GetServiceStatus(ServiceNameTextBox->Text->Data()).then([this](Concurrency::task<RpcAsyncWrapper::ServiceStatus> t)
    {
        CatchRpcException(t, [this](RpcAsyncWrapper::ServiceStatus status) {
            NotifyUser(ref new Platform::String((L"Status: " + ServiceStatusString(status)).c_str()));
        });
    });
}

void MainPage::Start_Click(Object^, RoutedEventArgs^)
{
    rpc.RunService(ServiceNameTextBox->Text->Data()).then([this](Concurrency::task<void> t)
    {
        CatchRpcException(t, static_cast<function<void()>>([this]() {
            NotifyUser("Service started");
        }));
    });
}

void MainPage::Stop_Click(Object^, RoutedEventArgs^)
{
    rpc.StopService(ServiceNameTextBox->Text->Data()).then([this](Concurrency::task<void> t)
    {
        CatchRpcException(t, static_cast<function<void()>>([this]() {
            NotifyUser("Service stopped");
        }));
    });
}

/*
 * Check if a previous task threw an exception and show the error. Else, call the callback.
 */
template<typename T> void MainPage::CatchRpcException(Concurrency::task<T>& task, function<void()> callback)
{
    try
    {
        task.get();
        callback();
    }
    catch (const runtime_error& e)
    {
        NotifyUser(CharToSystemString(e.what()));
    }
}

template<typename T, typename Callback> void MainPage::CatchRpcException(Concurrency::task<T>& task, Callback& callback)
{
    try
    {
        T result = task.get();
        callback(result);
    }
    catch (const runtime_error& e)
    {
        NotifyUser(CharToSystemString(e.what()));
    }
}
