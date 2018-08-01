using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using Windows.Devices.Gpio;
using Windows.UI.Core;
using System.Threading.Tasks;
using System.Runtime;
using Keg.DAL.Models;
using Windows.UI.Xaml.Input;
using System.Text;
using Keg.DAL;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Admin.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Admin_App1 : Page
    {
        public Admin_App1()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += OnKeyDown;
        }

        private StringBuilder sb = new StringBuilder();

        public async void OnKeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (e.VirtualKey == Windows.System.VirtualKey.Enter)
            {
                var k = await User.GetUserByHashcode(Hasher.GetSmartCardHash(sb.ToString()));
                Dictionary<string, string> admin = new Dictionary<string, string>();
                admin.Add($"Admin id: {sb.ToString()} ", $"Hash: {Hasher.GetSmartCardHash(sb.ToString())} ");
                if (k != null && k.IsApprover)
                {
                    KegLogger.KegLogTrace("Admin Login successful", "App1:OnKeyDown", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information, admin);
                    Window.Current.CoreWindow.KeyDown -= OnKeyDown;
                    this.Frame.Navigate(typeof(Admin_App2));
                }
                else
                {
                    KegLogger.KegLogTrace("Admin Login unsuccessful", "App1:OnKeyDown", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information, admin);
                }
                sb.Clear();
                return;
            }
            sb.Append((int)e.VirtualKey - 48);
        }

        private async void AddAdminUser(string hashcode)
        {
            User u = new User();
            {
                u.HashCode = hashcode;
                u.IsApprover = true;
            }
            User.AddUserAsync(u);
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };
    }

}
