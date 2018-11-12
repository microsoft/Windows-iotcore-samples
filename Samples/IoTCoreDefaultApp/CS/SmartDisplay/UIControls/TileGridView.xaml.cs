// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartDisplay.Controls
{
    public sealed partial class TileGridView : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource",
                typeof(IEnumerable<TileGridItem>),
                typeof(TileGridView),
                new PropertyMetadata(null, (sender, args) =>
                {
                    var control = (TileGridView)sender;
                    control.ItemsSource = (args.NewValue as IEnumerable<TileGridItem>) ?? new ObservableCollection<TileGridItem>();
                })
            );

        public IEnumerable<TileGridItem> ItemsSource
        {
            get { return GetValue(ItemsSourceProperty) as IEnumerable<TileGridItem>; }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public event TypedEventHandler<object, TileGridItem> ItemClick;

        public TileGridView()
        {
            InitializeComponent();
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ItemClick?.Invoke(this, e.ClickedItem as TileGridItem);
        }
    }

    public class TileGridItem : BaseViewModel
    {
        public static readonly Color DefaultColor = Color.FromArgb(255, 0, 120, 215);
        public const string SegoeMDL2FontFamily = "Segoe MDL2 Assets";
        public const string DefaultFontFamily = "Segoe UI";

        /// <summary>
        /// Blank constructor
        /// </summary>
        public TileGridItem()
        {
        }

        public TileGridItem(
            PageDescriptor descriptor,
            double width = 150,
            double height = 150,
            bool useMDL2 = false
            )
        {
            UseIcon = true;
            Icon = (useMDL2) ? descriptor.Tag : descriptor.Icon;
            IconFontFamily = new FontFamily(useMDL2 ? SegoeMDL2FontFamily : DefaultFontFamily);
            Title = descriptor.Title;
            Data = descriptor;
        }

        public double Width
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public double Height
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public SolidColorBrush BackgroundColor
        {
            get { return GetStoredProperty<SolidColorBrush>() ?? new SolidColorBrush(DefaultColor); }
            set { SetStoredProperty(value); }
        }

        public bool UseIcon
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    NotifyPropertyChanged("UseImage");
                }
            }
        }

        public FontFamily IconFontFamily
        {
            get { return GetStoredProperty<FontFamily>() ?? new FontFamily(DefaultFontFamily); }
            set { SetStoredProperty(value); }
        }

        public string Icon
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public bool UseImage
        {
            get { return !UseIcon; }
            set { UseIcon = !value; }
        }

        public BitmapImage Image
        {
            get { return GetStoredProperty<BitmapImage>(); }
            set { SetStoredProperty(value); }
        }

        public string Title
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public TextWrapping TitleTextWrapping
        {
            get { return GetStoredProperty<TextWrapping>(); }
            set { SetStoredProperty(value); }
        }

        public object Data;
    }
}
