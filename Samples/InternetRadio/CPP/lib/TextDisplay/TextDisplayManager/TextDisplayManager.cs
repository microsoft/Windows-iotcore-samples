using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.Maker.Devices.TextDisplay
{
    public static class TextDisplayManager
    {
        private static IEnumerable<ITextDisplay> s_avaliableDisplays = null;

        public static IAsyncOperation<IEnumerable<ITextDisplay>> GetDisplays()
        {
            return Task.Run(async () =>
            {
                if (null == s_avaliableDisplays)
                {
                    var displays = new List<ITextDisplay>();

                    var folder = Windows.Storage.ApplicationData.Current.LocalSettings;

                    var configProvider = new XmlConfigProvider(new Uri("ms-appx:///Microsoft.Maker.Devices.TextDisplay/screens.config"));

                    s_avaliableDisplays = await loadDisplaysForConfigs(await configProvider.GetConfiguredDisplaysAsync());
                }

                return s_avaliableDisplays;
            }).AsAsyncOperation();
        }

        public static IAsyncOperation<IEnumerable<ITextDisplay>> GetDisplaysForProvider(ITextDisplayConfigProvider configProvider)
        {
            return Task.Run(async () =>
            {
                return await loadDisplaysForConfigs(await configProvider.GetConfiguredDisplaysAsync());
            }).AsAsyncOperation();
        }

        private static IAsyncOperation<IEnumerable<ITextDisplay>> loadDisplaysForConfigs(IEnumerable<TextDisplayConfig> configs)
        {
            return Task.Run(async () =>
            {
                var displays = new List<ITextDisplay>();

                foreach (var config in configs)
                {
                    ITextDisplay driver = null;
                    var type = Type.GetType(config.DriverType);
                    
                    if (null == type && null != config.DriverAssembly)
                    {
                        try
                        {
                            var assembly = Assembly.Load(new AssemblyName(config.DriverAssembly));
                            if (null != assembly)
                            {
                                type = assembly.DefinedTypes.Where(t => t.FullName == config.DriverType).FirstOrDefault().AsType();
                            }
                        }
                        catch (FileNotFoundException)
                        {
                            Debug.WriteLine("TextDisplayManager: Failed to load assembly " +
                                            config.DriverAssembly +
                                            " for driver " +
                                            config.DriverType);
                        }
                    }

                    if (null != type)
                    {

                        driver = Activator.CreateInstance(type, config) as ITextDisplay;

                    }

                    if (null != driver)
                    {
                        try
                        {
                            await driver.InitializeAsync();
                            displays.Add(driver);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("TextDisplayManager: Failed to add driver " + type.ToString());
                            Debug.WriteLine("TextDisplayManager: " + e.Message);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("TextDisplayManager: Failed to add driver: " + config.DriverType);
                    }

                }

                return displays as IEnumerable<ITextDisplay>;
            }).AsAsyncOperation();
        }
    }
}
