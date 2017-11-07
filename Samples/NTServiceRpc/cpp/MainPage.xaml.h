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
	public enum class NotifyType
	{
		StatusMessage,
		ErrorMessage
	};

	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class MainPage sealed
	{
	public:
		MainPage();
		void NotifyUser(Platform::String^ message);

	private:
        RpcAsyncWrapper rpc;

        void button_Click_Connect(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
		void button_Click_GetStatus(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
        void button_Click_Start(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
        void button_Click_Stop(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);

	internal:
		static MainPage^ Current;
    };
}
