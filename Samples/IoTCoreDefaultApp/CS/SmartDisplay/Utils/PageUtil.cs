// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Views;
using SmartDisplay.Views.DevicePortal;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SmartDisplay.Utils
{
    public class PageList : List<PageDescriptor>
    {
        public void Add(string icon, string title, Type type, string tag = null, string category = null, object pageParam = null)
        {
            Add(new PageDescriptor()
            {
                Icon = icon,
                Title = title,
                Type = type,
                Tag = tag,
                Category = category,
                PageParam = pageParam,
            });
        }
    }

    public class PageUtil
    {
        public const string DefaultPageIcon = "\xE80F";
        public const string DefaultPageEmoji = "🔷";

        public static string PlayCategory = Common.GetLocalizedText("PlayCategoryText");
        public static string ExploreCategory = Common.GetLocalizedText("ExploreCategoryText");
        public static string WdpCategory = Common.GetLocalizedText("WDPCategoryText");

        private static readonly string[] PageCategories = new string[]
        {
            PlayCategory,
            ExploreCategory,
            WdpCategory,
        };

        private static readonly PageList AllPages = new PageList()
        {
            // Play
            { "🌞", Common.GetLocalizedText("WeatherPageTileText"), typeof(WeatherPage), "\uE706", PlayCategory },
            { "🌐", Common.GetLocalizedText("WebBrowserPageTileText"), typeof(WebBrowserPage), "\xE774", PlayCategory },
            { "🎵", Common.GetLocalizedText("MusicPlayerPageTileText"), typeof(MusicPlayerPage), "\xE8D6", PlayCategory },
            { "🖼️", Common.GetLocalizedText("SlideshowPageTileText"), typeof(PhotoPage), "\xE786", PlayCategory },
            { "✏️", Common.GetLocalizedText("DrawPageTileText"), typeof(DrawingPage), "\xE70F", PlayCategory },
            
            // Explore
            { "💻", Common.GetLocalizedText("AppLauncherPageTileText"), typeof(AppLauncherPage), "\xE8FC", ExploreCategory },
            { "🔔", Common.GetLocalizedText("NotificationsPageTileText"), typeof(NotificationsPage), "\xE8BD", ExploreCategory },
            { "📜", Common.GetLocalizedText("LogsPageTileText"), typeof(LogsPage), "\xEA37", ExploreCategory },
            { "🐱‍🐉", Common.GetLocalizedText("GitHubPageTileText"), typeof(GitHubPage), "\uF1AD", ExploreCategory },

            // Windows Device Portal
            { "📋", Common.GetLocalizedText("OSInfoPageTileText"), typeof(InfoPage), "\uE946", WdpCategory },
            { "📦", Common.GetLocalizedText("PackagesPageTileText"), typeof(PackagesPage), "\uE71D", WdpCategory },
            { "✈️", Common.GetLocalizedText("FlightingPageTileText"), typeof(FlightingPage), "\uE709", WdpCategory },
            { "🔄", Common.GetLocalizedText("WindowsUpdatePageTileText"), typeof(WindowsUpdatePage), "\uE895", WdpCategory },

            // Other
            { DefaultPageEmoji, Common.GetLocalizedText("StartPageTileText"), typeof(TilePage), "\xF0E2" },
            { DefaultPageEmoji, Common.GetLocalizedText("DeviceInfoPageTileText"), typeof(DeviceInfoPage), "\xE950" },
            { DefaultPageEmoji, Common.GetLocalizedText("CommandLinePageTileText"), typeof(CommandLinePage), "\xE756" },
            { DefaultPageEmoji, Common.GetLocalizedText("SettingsPageTileText"), typeof(SettingsPage), "\xE713" },
            { DefaultPageEmoji, Common.GetLocalizedText("AuthPageTileText"), typeof(AuthenticationPage), "\xE77B" },
        };

        public static Dictionary<string, PageDescriptor[]> GetPagesByCategory()
        {
            var categories = new List<string>();

            // Find categories from imported features
            foreach (var feature in AppComposer.Imports.Features)
            {
                if (feature.Pages != null)
                {
                    foreach (var page in feature.Pages)
                    {
                        // All features in Pages should be categorized. Default to the feature name.
                        if (string.IsNullOrEmpty(page.Category))
                        {
                            page.Category = feature.FeatureName;
                        }

                        if (!string.IsNullOrEmpty(page.Category) && !categories.Contains(page.Category))
                        {
                            categories.Add(page.Category);
                        }
                    }
                }
            }

            var categoryPages = new Dictionary<string, PageDescriptor[]>();
            var pages = GetFullPageList();

            // Loop through the imported feature categories plus the built-in categories.
            foreach (var category in PageCategories.Union(categories))
            {
                // Find pages that match the current category and add them to the dictionary.
                categoryPages.Add(category, pages.Where(p => p.Category == category).ToArray());
            }

            return categoryPages;
        }

        public static string GetFriendlyName(Type page)
        {
            var fullList = GetFullPageList();
            return fullList.FirstOrDefault(s => s.Type == page)?.Title;
        }

        public static Type[] GetAllPageTypes()
        {
            return GetFullPageList()
                .Select(pl => pl.Type)
                .Union(SettingsPage.SettingsPages.Values)
                .Distinct()
                .ToArray();
        }

        public static PageList GetFullPageList()
        {
            var pageList = new PageList();

            // In-box pages first
            pageList.AddRange(AllPages);

            // Add the page types from all imported features
            foreach (var feature in AppComposer.Imports.Features)
            {
                if (feature.Pages != null)
                {
                    pageList.AddRange(feature.Pages);
                }
            }

            return pageList;
        }

        public static PageDescriptor GetDescriptorFromTitle(string title)
        {
            return GetFullPageList().FirstOrDefault(x => x.Title == title);
        }

        public static PageDescriptor GetDescriptorFromTypeFullName(string fullName)
        {
            return GetFullPageList().FirstOrDefault(x => x.Type.FullName == fullName);
        }

        public static CommandBarButton CreatePageSettingCommandBarButton(IPageService pageService, SettingsUserControlBase settingControl, string title)
        {
            return new CommandBarButton
            {
                Icon = new FontIcon()
                {
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    Glyph = "\uE771",
                },
                Label = Common.GetLocalizedText("PageSettingsButtonText"),
                Handler = (s, e) => pageService?.ShowSidePane(settingControl, title),
            };
        }
    }
}
