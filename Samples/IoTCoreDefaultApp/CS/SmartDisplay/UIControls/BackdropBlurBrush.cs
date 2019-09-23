// Copyright (c) Microsoft Corporation. All rights reserved.

#if !_M_ARM64
using Microsoft.Graphics.Canvas.Effects;
#endif
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace SmartDisplay.Controls
{
    public sealed class BackdropBlurBrush : XamlCompositionBrushBase
    {
        public static readonly DependencyProperty BlurAmountProperty = DependencyProperty.Register(
            "BlurAmount",
            typeof(double),
            typeof(BackdropBlurBrush),
            new PropertyMetadata(0.0, new PropertyChangedCallback(OnBlurAmountChanged)
            )
        );

        public double BlurAmount
        {
            get { return (double)GetValue(BlurAmountProperty); }
            set { SetValue(BlurAmountProperty, value); }
        }

        public static readonly DependencyProperty TintColorProperty = DependencyProperty.Register(
            "TintColor",
            typeof(Color),
            typeof(BackdropBlurBrush),
            new PropertyMetadata(Color.FromArgb(0, 0, 0, 0), new PropertyChangedCallback(OnTintColorChanged)
            )
        );

        public Color TintColor
        {
            get { return (Color)GetValue(TintColorProperty); }
            set { SetValue(TintColorProperty, value); }
        }

        private static void OnBlurAmountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var brush = (BackdropBlurBrush)d;
            // Unbox and set a new blur amount if the CompositionBrush exists.
            brush.CompositionBrush?.Properties.InsertScalar("Blur.BlurAmount", (float)(double)e.NewValue);
        }

        private static void OnTintColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var brush = (BackdropBlurBrush)d;
            brush.CompositionBrush?.Properties.InsertColor("Tint.Color", (Color)e.NewValue);
        }

        public BackdropBlurBrush()
        {
        }

        protected override void OnConnected()
        {
            // Delay creating composition resources until they're required.
            if (CompositionBrush == null)
            {
#if !_M_ARM64
                var backdrop = Window.Current.Compositor.CreateBackdropBrush();

                // Use a Win2D blur affect applied to a CompositionBackdropBrush.
                var graphicsEffect = new GaussianBlurEffect
                {
                    Name = "Blur",
                    BlurAmount = (float)BlurAmount,
                    Source = new CompositionEffectSourceParameter("backdrop")
                };

                var colorEffect = new ColorSourceEffect
                {
                    Name = "Tint",
                    Color = TintColor
                };

                var blendEffect = new BlendEffect
                {
                    Background = graphicsEffect,
                    Foreground = colorEffect,
                    Mode = BlendEffectMode.Overlay
                };

                var effectFactory = Window.Current.Compositor.CreateEffectFactory(blendEffect, new[]
                {
                                    "Blur.BlurAmount",
                                    "Tint.Color"
                                });
                var effectBrush = effectFactory.CreateBrush();

                effectBrush.SetSourceParameter("backdrop", backdrop);

                CompositionBrush = effectBrush;
#else
                CompositionBrush = Window.Current.Compositor.CreateColorBrush(Colors.Black);
#endif
            }
        }

        protected override void OnDisconnected()
        {
            // Dispose of composition resources when no longer in use.
            if (CompositionBrush != null)
            {
                CompositionBrush.Dispose();
                CompositionBrush = null;
            }
        }
    }
}
