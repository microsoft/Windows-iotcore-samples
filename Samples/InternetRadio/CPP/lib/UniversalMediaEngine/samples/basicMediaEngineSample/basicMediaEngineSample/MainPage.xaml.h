//
// MainPage.xaml.h
// Declaration of the MainPage class.
//

#pragma once

#include "MainPage.g.h"

using namespace Microsoft::Maker::Media::UniversalMediaEngine;

namespace basicMediaEngineSample
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public ref class MainPage sealed
	{
	public:
		MainPage();

	private:
		MediaEngine m_mediaEngine;

		void Page_Loaded(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
		void Button_Click(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
		void OnMediaStateChanged(MediaState state);
		void Button_Click_1(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
	};
}
