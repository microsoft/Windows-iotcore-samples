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

#include <string>

using namespace NTServiceRpc;
using namespace std;

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::UI::Core;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls::Primitives;

namespace {
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
            return L"Error getting service status: " + to_wstring(status);
        }
    }
}

MainPage::MainPage()
{
	InitializeComponent();
}

void MainPage::Page_Loaded(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e)
{
    ConnectToService();
    button_Click_Connect(nullptr, nullptr);
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
    rpc.Connect().then([this](bool success) {
        NotifyUser(success ? "Connected to service" : "Connection to service failed");
    });
}

void MainPage::button_Click_Connect(Object^, RoutedEventArgs^)
{
    ConnectToService();
}

void MainPage::button_Click_GetStatus(Object^ sender, RoutedEventArgs^ e)
{
    rpc.GetServiceStatus(ServiceNameTextBox->Text->Data()).then([this](RpcAsyncWrapper::ServiceStatus status) {
        NotifyUser(ref new Platform::String((L"Status: " + ServiceStatusString(status)).c_str()));
    });
}

void MainPage::button_Click_Start(Object^, RoutedEventArgs^)
{
    rpc.RunService(ServiceNameTextBox->Text->Data()).then([this](boolean success) {
        NotifyUser(success ? "Service started" : "Starting service failed");
    });
}

void MainPage::button_Click_Stop(Object^, RoutedEventArgs^)
{
    rpc.StopService(ServiceNameTextBox->Text->Data()).then([this](boolean success) {
        NotifyUser(success ? "Service stopped" : "Stopping service failed");
    });
}
