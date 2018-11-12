// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace SmartDisplay.Controls
{
    public sealed partial class JumboNotificationControl
    {
        private JumboNotificationVM ViewModel { get; } = new JumboNotificationVM();

        public static readonly Color DefaultColor = Color.FromArgb(255, 0, 120, 215);
        public const string DefaultSymbol = "🔔";
        public const int DefaultTimeoutMs = 3000;

        #region UI Properties

        public new double Width
        {
            get { return ViewModel.Width; }
            set { ViewModel.Width = value; }
        }

        public new double Height
        {
            get { return ViewModel.Height; }
            set { ViewModel.Height = value; }
        }

        #endregion

        public JumboNotificationControl()
        {
            InitializeComponent();
            
            // Auto-dismiss timer
            _dismissTimer = new DispatcherTimer();
            _dismissTimer.Tick += DismissJumboNotification;

            ConfigureAnimations(ViewModel.Width);

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Width":
                    ConfigureAnimations(ViewModel.Width);
                    break;
            }
        }

        // Timer for dismissing notification
        private DispatcherTimer _dismissTimer;
        // Animation for background
        private Storyboard _showStoryboard;
        // Animation for notification dismissal
        private Storyboard _dismissStoryboard;

        private void ConfigureAnimations(double width)
        {
            var textStoryboard = AnimationUtil.CreateDoubleStoryboard(_jumboTextGrid, 40, width, 0);
            textStoryboard.Completed += (s, e) =>
            {
                _dismissTimer.Start();
            };

            // The background will appear first, then the text
            _showStoryboard = AnimationUtil.CreateDoubleStoryboard(_jumboNotificationGrid, 75, width, 0);
            // Add a small delay to make the background and the text show up at different times
            _showStoryboard.Duration = new Duration(TimeSpan.FromMilliseconds(250));
            _showStoryboard.Completed += (s, e) =>
            {
                textStoryboard.Begin();
                ViewModel.TextVisible = true;
            };

            var dismissGrid = AnimationUtil.CreateDoubleStoryboard(_jumboNotificationGrid, 40, 0, -width);
            dismissGrid.Completed += (s, e) =>
            {
                _currentJumboNotification?.SetResult(true);
                _currentJumboNotification = null;

                ViewModel.GridVisible = false;
                ViewModel.TextVisible = false;

                Debug.WriteLine("Jumbo notification dismissed.");
            };
            
            // The text will disappear first, then the background
            _dismissStoryboard = AnimationUtil.CreateDoubleStoryboard(_jumboTextGrid, 40, 0, -width);
            _dismissStoryboard.Duration = new Duration(TimeSpan.FromMilliseconds(250));
            _dismissStoryboard.Completed += (s, e) =>
            {
                dismissGrid.Begin();
            };
        }


        // Track the Task for the current jumbo notification so we can complete it during DismissJumboNotification. 
        private TaskCompletionSource<bool> _currentJumboNotification = null;

        public Task ShowAsync(string text, Color color, int timeoutMilliseconds = DefaultTimeoutMs, string symbol = DefaultSymbol)
        {
            Debug.WriteLine($"Showing jumbo notification: {text}");

            ViewModel.UseSymbolImage = false;
            symbol = symbol ?? DefaultSymbol;

            _currentJumboNotification = new TaskCompletionSource<bool>();

            var noWait = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ViewModel.Text = text;
                ViewModel.Symbol = symbol;
                ViewModel.Color = color;

                // The timer will be started after the storyboard animation finishes.
                _dismissTimer.Interval = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                _showStoryboard.Begin();

                ViewModel.GridVisible = true;
            });

            return _currentJumboNotification.Task;
        }

        public Task ShowWithImageAsync(string text, Color color, int timeoutMilliseconds = DefaultTimeoutMs, StorageFile symbolFile = null)
        {
            Debug.WriteLine($"Showing jumbo notification: {text}");

            _currentJumboNotification = new TaskCompletionSource<bool>();

            var noWait = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ViewModel.Text = text;

                // Use symbol image if valid, otherwise default to emoji
                if (symbolFile != null && ViewModel.TrySetSymbolImage(symbolFile))
                {
                    ViewModel.UseSymbolImage = true;
                }
                else
                {
                    ViewModel.UseSymbolImage = false;
                    ViewModel.Symbol = DefaultSymbol;
                }
                ViewModel.Color = color;

                // The timer will be started after the storyboard animation finishes.
                _dismissTimer.Interval = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                _showStoryboard.Begin();

                ViewModel.GridVisible = true;
            });

            return _currentJumboNotification.Task;
        }

        private void DismissJumboNotification(object sender, object e)
        {
            Debug.WriteLine("DismissJumboNotification");

            _dismissTimer?.Stop();
            _dismissStoryboard.Begin();
        }
    }
}
