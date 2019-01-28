using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Storage;

namespace Microsoft.Maker.Devices.TextDisplay
{
    class XmlConfigProvider : ITextDisplayConfigProvider
    {
        private Uri configFileLocation;

        public XmlConfigProvider(Uri configFileLocation)
        {
            this.configFileLocation = configFileLocation;
        }

        public IAsyncOperation<IEnumerable<TextDisplayConfig>> GetConfiguredDisplaysAsync()
        {
            return Task.Run(async () =>
            {
                var displaysConfig = new List<TextDisplayConfig>();

                try
                {
                    var configFile = await StorageFile.GetFileFromApplicationUriAsync(this.configFileLocation);

                    var xmlString = await FileIO.ReadTextAsync(configFile);

                    var xml = XElement.Parse(xmlString);

                    var displaysConfigXml = xml.Descendants("Screen");

                    foreach (var displayConfigXml in displaysConfigXml)
                    {
                        var displayConfig = new TextDisplayConfig();
                        var driverTypeAttribute = displayConfigXml.Attributes().Where(a => a.Name == "driverType").FirstOrDefault();

                        if (null != driverTypeAttribute)
                        {
                            displayConfig.DriverType = driverTypeAttribute.Value;
                        }

                        var commonConfigElement = displayConfigXml.Descendants("CommonConfiguration").FirstOrDefault();
                        if (null != commonConfigElement)
                        {
                            var heightElement = commonConfigElement.Descendants("Height").FirstOrDefault();
                            var widthElement = commonConfigElement.Descendants("Width").FirstOrDefault();

                            if (null != heightElement &&
                                null != widthElement)
                            {
                                displayConfig.Height = Convert.ToUInt32(heightElement.Value);
                                displayConfig.Width = Convert.ToUInt32(widthElement.Value);
                            }
                        }

                        var driverConfigElement = displayConfigXml.Descendants("DriverConiguration").FirstOrDefault();
                        foreach (var configValue in displayConfigXml.Descendants())
                        {
                            displayConfig.DriverConfigurationValues.Add(configValue.Name.ToString(), configValue.Value);
                        }

                        displaysConfig.Add(displayConfig);
                    }
                }
                catch (FileNotFoundException)
                {
                    Debug.WriteLine("TextDisplayManager: Screen config file not found");
                }

                return displaysConfig as IEnumerable<TextDisplayConfig>;
            }).AsAsyncOperation();
        }
    }
}
