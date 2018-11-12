// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace SmartDisplay.Utils
{
    public static class AnimationUtil
    {
        public static Storyboard CreateDoubleStoryboard(UIElement target, int durationMs, double from, double to, string targetProperty = "(Canvas.Left)")
        {
            var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));
            var animation = new DoubleAnimation
            {
                Duration = duration,
                From = from,
                To = to,
            };
            Storyboard.SetTargetProperty(animation, targetProperty);

            return CreateStoryboard(target, durationMs, animation);
        }

        public static Storyboard CreateStoryboard(UIElement target, int durationMs, params Timeline[] animations)
        {
            var storyboard = CreateStoryboard(target, animations);
            storyboard.Duration = new Duration(TimeSpan.FromMilliseconds(durationMs));

            return storyboard;
        }

        public static Storyboard CreateStoryboard(UIElement target, params Timeline[] animations)
        {
            var storyboard = new Storyboard();

            foreach (var animation in animations)
            {
                storyboard.Children.Add(animation);
                Storyboard.SetTarget(animation, target);
            }

            return storyboard;
        }
    }
}
