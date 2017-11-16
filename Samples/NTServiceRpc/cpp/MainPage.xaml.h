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

#pragma once

#include "MainPage.g.h"

#include "RpcAsyncWrapper.h"

namespace NTServiceRpc
{
	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class MainPage sealed
	{
	public:
		MainPage();

	private:
        RpcAsyncWrapper rpc;

        void ConnectToService();
        void NotifyUser(Platform::String^ message);
        void Connect_Click(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
		void GetStatus_Click(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
        void Start_Click(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
        void Stop_Click(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);

    private:
        void Page_Loaded(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
        template<typename T> void CatchRpcException(Concurrency::task<T>& task, std::function<void()> callback);
        template<typename T, typename Callback> void CatchRpcException(Concurrency::task<T>& task, Callback& callback);
    };
}
