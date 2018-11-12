// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation.Metadata;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using Windows.System.UserProfile;

namespace SmartDisplay.Utils
{
    public class LanguageManager : INotifyPropertyChanged
    {
        private readonly string[] _inputLanguages = {
            "af",      "ar-SA",    "as",         "az-Cyrl",    "az-Latn",
            "ba-Cyrl", "be",       "bg",         "bn-IN",      "bo-Tibt",
            "bs-Cyrl", "chr-Cher", "cs",         "cy",         "da",
            "de-CH",   "de-DE",    "dv",         "el",         "en-CA",
            "en-GB",   "en-IE",    "en-IN",      "en-US",      "es-ES",
            "es-MX",   "et",       "fa",         "fi",         "fo",
            "fr-BE",   "fr-CA",    "fr-CH",      "fr-FR",      "gn",
            "gu",      "ha-Latn",  "haw-Latn",   "he",         "hi",
            "hr-HR",   "hsb",      "hu",         "hy",         "ig-Latn",
            "is",      "it-IT",    "iu-Latn",    "ja",         "ka",
            "kk",      "kl",       "km",         "kn",         "ko",
            "ku-Arab", "ky-Cyrl",  "lb",         "lo",         "lt",
            "lv",      "mi-Latn",  "mk",         "ml",         "mn-Cyrl",
            "mn-Mong", "mr",       "mt",         "my",         "nb",
            "ne-NP",   "nl-BE",    "nl-NL",      "nso",        "or",
            "pa",      "pl",       "ps",         "pt-BR",      "pt-PT",
            "ro-RO",   "sah-Cyrl", "se-Latn-NO", "se-Latn-SE", "si",
            "sk",      "sl",       "sq",         "sv-SE",      "syr-Syrc",
            "ta-IN",   "te",       "tg-Cyrl",    "th",         "tk-Latn",
            "tn-ZA",   "tr",       "tt-Cyrl",    "tzm-Latn",   "tzm-Tfng",
            "ug-Arab", "uk",       "ur-PK",      "uz-Cyrl",    "vi",
            "wo-Latn", "yo-Latn"
            };

        private Dictionary<string, string> _displayNameToLanguageMap;
        private SortedDictionary<string, string> _displayNameToImageLanguageMap;
        private SortedDictionary<string, string> _displayNameToInputLanguageMap;

        public IReadOnlyList<string> LanguageDisplayNames { get; }

        public IReadOnlyList<string> InputLanguageDisplayNames { get; }

        public IReadOnlyList<string> ImageLanguageDisplayNames { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private LanguageManager()
        {
            List<string> imageLanguagesList = GetImageSupportedLanguages();
            // Only Image Enable Map
            _displayNameToImageLanguageMap = new SortedDictionary<string, string>(
                imageLanguagesList.Select(tag =>
                {
                    var lang = new Language(tag);
                    return new KeyValuePair<string, string>(lang.NativeName, GetLocaleFromLanguageTag(lang.LanguageTag));
                }).ToDictionary(keyitem => keyitem.Key, valueItem => valueItem.Value)
                );

            ImageLanguageDisplayNames = _displayNameToImageLanguageMap.Keys.ToList();

            _displayNameToLanguageMap = new Dictionary<string, string>(
                ApplicationLanguages.ManifestLanguages.Union(imageLanguagesList).Select(tag =>
                {
                    var lang = new Language(tag);
                    return new KeyValuePair<string, string>(lang.NativeName, GetLocaleFromLanguageTag(lang.LanguageTag));
                }).Distinct().OrderBy(a => a.Key).ToDictionary(keyitem => keyitem.Key, valueItem => valueItem.Value)
                );

            LanguageDisplayNames = _displayNameToLanguageMap.Keys.ToList();

            _displayNameToInputLanguageMap = new SortedDictionary<string, string>(
                _inputLanguages.Select(tag =>
                {
                    var lang = new Language(tag);
                    return new KeyValuePair<string, string>(lang.NativeName, GetLocaleFromLanguageTag(lang.LanguageTag));
                }).ToDictionary(keyitem => keyitem.Key, valueItem => valueItem.Value)
            );

            InputLanguageDisplayNames = _displayNameToInputLanguageMap.Keys.ToList();

            // Exception when running in Local Machine
            try
            {
                // Add Image Enabled Languages as Global Preferences List
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                {
                    // Find language currently set as UI language. This should be
                    // the first language in the list passed to TrySetLanguages
                    var uiLanguageTag = GlobalizationPreferences.Languages[0];
                    var index = imageLanguagesList.IndexOf(uiLanguageTag);
                    if (index != 0)
                    {
                        if (index != -1)
                        {
                            imageLanguagesList.Remove(uiLanguageTag);
                        }
                        imageLanguagesList.Insert(0, uiLanguageTag);
                    }
                    GlobalizationPreferences.TrySetLanguages(imageLanguagesList);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // This is indicative of EmbeddedMode not being enabled (i.e.
                // running on Desktop or Mobile without enabling EmbeddedMode) 
                //  https://developer.microsoft.com/en-us/windows/iot/docs/embeddedmode
                App.LogService.Write(ex.ToString());
            }
        }


        private static LanguageManager _instance;
        public static LanguageManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new LanguageManager();
            }
            return _instance;
        }

        public bool UpdateLanguage(string displayName, bool automatic = false)
        {
            string langFromTag;
            var currentLang = ApplicationLanguages.PrimaryLanguageOverride;
            if (!automatic)
            {
                langFromTag = UpdateLanguageByTag(GetLanguageTagFromDisplayName(displayName));
            }
            else
            {
                var getCompatibleLang = CheckUpdateLanguage(displayName);
                langFromTag = UpdateLanguageByTag(getCompatibleLang.Item3);
            }
            if (currentLang != langFromTag)
            {
                // Do this twice because in Release mode, once isn't enough
                // to change the current CultureInfo (changing the WaitOne delay
                // doesn't help).
                for (int i = 0; i < 2; i++)
                {
                    ApplicationLanguages.PrimaryLanguageOverride = langFromTag;

                    // Refresh the resources in new language
                    ResourceContext.GetForCurrentView().Reset();
                    ResourceContext.GetForViewIndependentUse().Reset();

                    // Where seems to be some delay between when this is reset and when
                    // we can start re-evaluating the resources.  Without a pause, sometimes
                    // the first resource remains the previous language.
                    new System.Threading.ManualResetEvent(false).WaitOne(100);
                }

                NotifyPropertyChanged("Item[]");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the full format of Locale ex: for ru, it returns ru-RU
        /// </summary>
        /// <param name="identifier"></param>
        public static string GetLocaleFromLanguageTag(string identifier)
        {
            int result;
            StringBuilder localeName = new StringBuilder(500);
            result = NativeMethods.ResolveLocaleName(identifier, localeName, 500);

            return localeName.ToString();
        }

        /// <summary>
        /// Updates Application Language, Geographic Region and Speech Language
        /// </summary>
        /// <param name="languageTag"></param>
        public string UpdateLanguageByTag(string languageTag)
        {
            var currentLang = ApplicationLanguages.PrimaryLanguageOverride;
            if (currentLang != languageTag && Language.IsWellFormed(languageTag))
            {
                SetLanguageEntities(languageTag);

                // Do this twice because in Release mode, once isn't enough
                // to change the current CultureInfo (changing the WaitOne delay
                // doesn't help).
                for (int i = 0; i < 2; i++)
                {
                    // Refresh the resources in new language
                    ResourceContext.GetForCurrentView().Reset();
                    ResourceContext.GetForViewIndependentUse().Reset();

                    // Where seems to be some delay between when this is reset and when
                    // we can start re-evaluating the resources.  Without a pause, sometimes
                    // the first resource remains the previous language.
                    new System.Threading.ManualResetEvent(false).WaitOne(100);
                }
                return languageTag;
            }
            return currentLang;
        }

        private void SetLanguageEntities(string languageTag)
        {
            // Use BCP47 Format
            string bcp47Tag = GetRegionFromBCP47LanguageTag(languageTag);
            // Apply the PrimaryLanguage
            ApplicationLanguages.PrimaryLanguageOverride = languageTag;

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
            {
                // Set user language
                if (Language.IsWellFormed(languageTag))
                {
                    try
                    {
                        // Set the Region
                        GlobalizationPreferences.TrySetHomeGeographicRegion(bcp47Tag);

                        // Set the Speech Language
                        Task.Run(async () =>
                        {
                            Language speechLanguage = new Language(languageTag);
                            await SpeechRecognizer.TrySetSystemSpeechLanguageAsync(speechLanguage);
                        });

                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // This is indicative of EmbeddedMode not being enabled (i.e.
                        // running on Desktop or Mobile without enabling EmbeddedMode) 
                        //  https://developer.microsoft.com/en-us/windows/iot/docs/embeddedmode
                        App.LogService.Write(ex.ToString());
                        App.LogService.Write("UnauthorizedAccessException: Check to see if Embedded Mode is enabled");
                    }
                }
            } // Only for ApiContract > 5
        }

        private string GetRegionFromBCP47LanguageTag(string languageTag)
        {
            // https://tools.ietf.org/html/bcp47
            // BCP47 language tag is formed by language tag itself along with region subtag, e.g.: 
            //   en-US -> english US region
            //   fr-CA -> french CA region
            //   ex: some are populated as this: az-Cyrl-AZ
            //   without -
            // Not an extensive implementation, but covering major Region Formats
            string region = string.Empty;

            var parts = languageTag.LastIndexOf('-');
            if (parts != -1)
            {
                region = languageTag.Substring(parts + 1);
            }
            return region;
        }

        /// <summary>
        /// Check to return appropriate language depending on selection
        /// Item1: Image Exists, Item2:Speech, Item3:App Localization(manifest), Lang Tag
        /// </summary>
        /// <param name="language"></param>
        public (bool ExistsInImage, bool SupportsSpeech, string LanguageTag) CheckUpdateLanguage(string language)
        {
            string currentLang = ApplicationLanguages.PrimaryLanguageOverride;
            string languageTag = GetLanguageTagFromDisplayName(language);
            Tuple<bool, bool, string> langVerify = new Tuple<bool, bool, string>(false, false, languageTag);

            List<string> imageLanguagesList = GetImageSupportedLanguages();
            langVerify = Tuple.Create<bool, bool, string>(
                imageLanguagesList.Contains(languageTag),
                _displayNameToLanguageMap.Values.Contains(languageTag),
                languageTag);

            if (currentLang != languageTag && Language.IsWellFormed(languageTag))
            {
                // Image does not contain or selected language does not support Speech
                if (!imageLanguagesList.Contains(languageTag))
                {
                    // Look for near Lang removing all sugtags
                    var filteredList = imageLanguagesList.Where(x => x.Contains(languageTag.Substring(0, languageTag.IndexOf('-'))));
                    foreach (var item in filteredList)
                    {
                        if (item != null && item.Trim().Length > 0)
                        {
                            // Found matching Language, take preference, continue checking
                            // Change the primary only if primary language not part of imagelist
                            if (!imageLanguagesList.Contains(languageTag))
                            {
                                languageTag = item;
                            }
                        }
                    }
                }

                langVerify = Tuple.Create<bool, bool, string>(
                        imageLanguagesList.Contains(languageTag),
                        _displayNameToLanguageMap.Values.Contains(languageTag),
                        languageTag);
            }

            return (langVerify.Item1, langVerify.Item2, langVerify.Item3);
        }

        /// <summary>
        /// Returns the Tuple with 
        ///     Item1: Image has language resources
        ///     Item2: Speech supported
        ///     Item3: Supports Localization through Manifest
        /// </summary>
        /// <param name="languageTag"></param>
        public Tuple<bool, bool> GetLanguageTuple(string languageTag)
        {
            List<string> imageList = GetImageSupportedLanguages();
            Tuple<bool, bool> langVerify = new Tuple<bool, bool>(false, false);

            langVerify = Tuple.Create<bool, bool>(
                imageList.Contains(languageTag),
                _displayNameToLanguageMap.Values.Contains(languageTag));

            return langVerify;
        }

        /// <summary>
        /// Returns all the Image Enabled Languages supported
        /// </summary>
        public List<string> GetImageSupportedLanguages()
        {
            ImageLanguages.GetMUILanguages();

            return ImageLanguages.Languages.Values.ToList();
        }

        /// <summary>
        /// Returns LanguageDisplayName from Language Tag
        /// </summary>
        /// <param name="langTag"></param>
        public static string GetDisplayNameFromLanguageTag(string langTag)
        {
            if (string.IsNullOrEmpty(langTag))
            {
                return string.Empty;
            }
            var lang = new Language(langTag);

            return lang.NativeName;
        }

        public bool UpdateInputLanguage(string displayName)
        {
            var currentLang = Language.CurrentInputMethodLanguageTag;
            var newLang = GetInputLanguageTagFromDisplayName(displayName);
            if (currentLang != newLang)
            {
                if (!Language.TrySetInputMethodLanguageTag(newLang))
                {
                    return false;
                }

                NotifyPropertyChanged("Item[]");
                return true;
            }
            return false;
        }

        public string GetLanguageTagFromDisplayName(string displayName)
        {
            if (_displayNameToLanguageMap.TryGetValue(displayName, out string newLang))
            {
                return newLang;
            }
            else
            {
                string partialMatch = _displayNameToImageLanguageMap.Keys.FirstOrDefault(x => displayName.Contains(x));
                if (partialMatch != null)
                {
                    return partialMatch;
                }
            }
            throw new ArgumentException("Failed to get language tag for " + displayName);
        }

        private string GetInputLanguageTagFromDisplayName(string displayName)
        {
            if (!_displayNameToInputLanguageMap.TryGetValue(displayName, out string newLang))
            {
                throw new ArgumentException("Failed to get input language tag for " + displayName);
            }
            return newLang;
        }

        public static string GetCurrentLanguageDisplayName()
        {
            var langTag = ApplicationLanguages.PrimaryLanguageOverride;
            if (string.IsNullOrEmpty(langTag))
            {
                langTag = GlobalizationPreferences.Languages[0];
            }
            var lang = new Language(langTag);

            return lang.NativeName;
        }

        public static string GetCurrentInputLanguageDisplayName()
        {
            var langTag = Language.CurrentInputMethodLanguageTag;
            var lang = new Language(langTag);

            return lang.NativeName;
        }

        // Used during data binding to return localized strings
        public string this[string key]
        {
            get
            {
                return Common.GetLocalizedText(key);
            }
        }

        private void NotifyPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    /// <summary>
    /// Using WinAPI Calls to get the List of Image Enabled Languages. 
    /// </summary>
    internal static class ImageLanguages
    {
        public static Dictionary<int, string> Languages = new Dictionary<int, string>();

        private static bool UILanguageProc(IntPtr lpLang, IntPtr lParam)
        {
            int langID = Convert.ToInt32(Marshal.PtrToStringUni(lpLang), 16);
            StringBuilder data = new StringBuilder(500);

            if (!Languages.ContainsKey(langID))
            {
                NativeMethods.LCIDToLocaleName(Convert.ToUInt32(langID), data, data.Capacity, 0);
                Languages.Add(langID, data.ToString());
            }

            return true;
        }

        public static void GetMUILanguages()
        {
            try
            {
                NativeMethods.EnumUILanguagesW(UILanguageProc, 0, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                App.LogService.Write("EnumUILanguages: " + ex.Message);
                App.LogService.Write(ex.ToString());
            }
        }
    }
}
