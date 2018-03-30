using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RPiCognitiveService
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

        /// <summary>
        /// Navigate pages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((e.AddedItems[0] as ListViewItem).Tag.ToString().Equals("0"))  //打开 关闭导航栏
            {
                navSplitView.IsPaneOpen = !navSplitView.IsPaneOpen;
            }
            if ((e.AddedItems[0] as ListViewItem).Tag.ToString().Equals("1"))  //打开 图片分析
            {
                frmPages.Navigate(typeof(PhotoPage));
                navSplitView.IsPaneOpen = false;
            }
            if ((e.AddedItems[0] as ListViewItem).Tag.ToString().Equals("2"))  //打开 人脸分析
            {
                frmPages.Navigate(typeof(FacePage));
                navSplitView.IsPaneOpen = false;
            }
        }
    }
}
