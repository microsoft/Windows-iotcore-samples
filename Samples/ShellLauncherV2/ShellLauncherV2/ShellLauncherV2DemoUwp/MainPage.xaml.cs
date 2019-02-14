// Copyright (c) 2019, Microsoft Corporation
//
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted, provided that the above
// copyright notice and this permission notice appear in all copies.
//
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
// WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
// SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
// WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
// ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR
// IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ShellLauncherV2DemoUwp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Launchmynotepad_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("mynotepad:"));
        }

        private void ExitAppButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private async void LaunchAnotherUwpButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("anotheruwpapp:"));
        }

        private async void LaunchSecondaryViewButton_Click(object sender, RoutedEventArgs e)
        {
            var newView = CoreApplication.CreateNewView();
            await newView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var frame = new Frame();
                frame.Navigate(typeof(SecondaryViewPage));
                Window.Current.Content = frame;
                // This is a change from 8.1: In order for the view to be displayed later it needs to be activated.
                Window.Current.Activate();
                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(ApplicationView.GetApplicationViewIdForWindow(Windows.UI.Core.CoreWindow.GetForCurrentThread()));
            });
        }

        public string mynotepadreg
        {
            get
            {
                return "Import below registry to register mynotepad: protocol for notepad.exe, then click the link to launch notepad.exe by protocol\r\n\r\nWindows Registry Editor Version 5.00\r\n\r\n[HKEY_CLASSES_ROOT\\mynotepad]\r\n@=\"URL:mynotepad\"\r\n\"URL Protocol\"=\"\"\r\n\"EditFlags\"=dword:00200000\r\n\r\n[HKEY_CLASSES_ROOT\\mynotepad\\shell]\r\n@=\"open\"\r\n\r\n[HKEY_CLASSES_ROOT\\mynotepad\\shell\\open]\r\n\r\n[HKEY_CLASSES_ROOT\\mynotepad\\shell\\open\\command]\r\n@=\"c:\\\\windows\\\\system32\\\\notepad.exe\"\r\n";
            }
        }
    }
}
