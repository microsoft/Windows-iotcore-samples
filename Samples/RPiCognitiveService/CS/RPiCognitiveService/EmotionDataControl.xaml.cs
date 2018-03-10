using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace RPiCognitiveService
{
    public sealed partial class EmotionDataControl : UserControl
    {
        public EmotionDataControl()
        {
            this.InitializeComponent();
            Loaded += EmotionDataControl_Loaded;
        }

        /// <summary>
        /// emotion data id-data
        /// </summary>
        public Dictionary<string, double> Data
        {
            get; set;
        }

        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EmotionDataControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Data != null)
            {
                var w = this.ActualWidth;
                var h = this.ActualHeight;

                if (w <= 20)
                    return;
                Line x_axis = new Line();
                x_axis.StrokeThickness = 1;
                x_axis.Stroke = new SolidColorBrush(Colors.Orange);
                Canvas.SetLeft(x_axis, 20);
                Canvas.SetTop(x_axis, h - 20);
                x_axis.Width = w - 40;

                mainCanvas.Children.Add(x_axis);

                var gap_w = (w - 40) / (Data.Count - 1);
                var height = h - 40;

                int index = 0; PointCollection list = new PointCollection();
                list.Add(new Point(20, h - 20));
                foreach (var p in Data)
                {
                    Point point = new Point(20 + index * gap_w, 20 + (height - height * p.Value));

                    TextBlock txt = new TextBlock();
                    txt.FontSize = 12;
                    txt.Text = p.Key;

                    Canvas.SetLeft(txt, point.X - 10);
                    Canvas.SetTop(txt, point.Y - 20);
                    mainCanvas.Children.Add(txt);

                    list.Add(point);

                    index++;
                }
                list.Add(new Point(w - 20, h - 20));

                Polygon po = new Polygon();
                po.Stroke = new SolidColorBrush(Colors.Orange);
                po.StrokeThickness = 1;
                po.Points = list;
                po.Fill = new SolidColorBrush(Colors.Orange);
                Canvas.SetZIndex(po, -1);
                mainCanvas.Children.Add(po);
            }
        }
    }
}
