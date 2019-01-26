using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.Maker.Devices.TextDisplay;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TextDisplay.Tests
{
    [TestClass]
    public class TextDisplayManagerTests
    {
        [TestMethod]
        public async Task TestNoConfigsReturnsEmptyListSucceeds()
        {
            // Arrange
            var testConfigProvider = new TestConfigProvider();
            List<ITextDisplay> returnedDisplays;

            // Act
            returnedDisplays = await TextDisplayManager.GetDisplaysForProvider(testConfigProvider) as List<ITextDisplay>;

            // Assert
            Assert.AreEqual(0, returnedDisplays.Count, "No displays should be returned");
        }

        [TestMethod]
        public async Task TestOneConfigReturnsOneDisplaySucceeds()
        {
            // Arrange
            var testConfigProvider = new TestConfigProvider();
            var testConfig = new TextDisplayConfig()
            {
                DriverType = "MockDisplayDriver.MockDisplayDriver",
                DriverAssembly = "MockDisplayDriver",
                Height = 0,
                Width = 0
            };
            (testConfigProvider.Configs as List<TextDisplayConfig>).Add(testConfig);
            List<ITextDisplay> returnedDisplays;

            // Act
            returnedDisplays = await TextDisplayManager.GetDisplaysForProvider(testConfigProvider) as List<ITextDisplay>;

            // Assert
            Assert.AreEqual(1, returnedDisplays.Count, "1 display should be returned");
        }

        [TestMethod]
        public async Task TestTwoConfigsOfSameTypeReturnTwoDisplaysSucceeds()
        {
            // Arrange
            var testConfigProvider = new TestConfigProvider();
            var testConfig = new TextDisplayConfig()
            {
                DriverType = "MockDisplayDriver.MockDisplayDriver",
                DriverAssembly = "MockDisplayDriver",
                Height = 0,
                Width = 0
            };
            (testConfigProvider.Configs as List<TextDisplayConfig>).Add(testConfig);
            (testConfigProvider.Configs as List<TextDisplayConfig>).Add(testConfig);

            List<ITextDisplay> returnedDisplays;

            // Act
            returnedDisplays = await TextDisplayManager.GetDisplaysForProvider(testConfigProvider) as List<ITextDisplay>;

            // Assert
            Assert.AreEqual(2, returnedDisplays.Count, "2 displays should be returned");
        }

        [TestMethod]
        public async Task TestDriverConfigValuesPassedToDriverSucceeds()
        {
            // Arrange
            var testConfigProvider = new TestConfigProvider();
            var testDriverConfigName = "testDriverConfigName";
            var testDriverConfigValue = "testDriverConfigValue";

            var testConfig = new TextDisplayConfig()
            {
                DriverType = "MockDisplayDriver.MockDisplayDriver",
                DriverAssembly = "MockDisplayDriver",
                Height = 0,
                Width = 0
            };

            testConfig.DriverConfigurationValues.Add(testDriverConfigName, testDriverConfigValue);
            (testConfigProvider.Configs as List<TextDisplayConfig>).Add(testConfig);

            List<ITextDisplay> returnedDisplays;

            // Act
            returnedDisplays = await TextDisplayManager.GetDisplaysForProvider(testConfigProvider) as List<ITextDisplay>;
            // Assert
            Assert.AreEqual(1, returnedDisplays.Count, "1 display should be returned");
            var mockDriver = returnedDisplays[0] as MockDisplayDriver.MockDisplayDriver;
            Assert.IsNotNull(mockDriver);
            Assert.AreEqual(mockDriver.Config.DriverConfigurationValues[testDriverConfigName], testDriverConfigValue);
        }

        [TestMethod]
        public async Task TestWriteMessageToDriverSucceeds()
        {
            // Arrange
            var testConfigProvider = new TestConfigProvider();
            var testConfig = new TextDisplayConfig()
            {
                DriverType = "MockDisplayDriver.MockDisplayDriver",
                DriverAssembly = "MockDisplayDriver",
                Height = 0,
                Width = 0
            };
            (testConfigProvider.Configs as List<TextDisplayConfig>).Add(testConfig);

            var testMessage = "testMessage";

            List<ITextDisplay> returnedDisplays;

            // Act
            returnedDisplays = await TextDisplayManager.GetDisplaysForProvider(testConfigProvider) as List<ITextDisplay>;              
            await returnedDisplays[0].WriteMessageAsync(testMessage, 0);

            // Assert
            Assert.AreEqual(1, returnedDisplays.Count, "1 display should be returned");
            var mockDriver = returnedDisplays[0] as MockDisplayDriver.MockDisplayDriver;
            Assert.IsNotNull(mockDriver, "Manager did not return mock driver");
            Assert.AreEqual(testMessage, mockDriver.LastMessage);         
        }

        [TestMethod]
        public async Task TestNonExistentBuiltInDriverTypeFails()
        {
            // Arrange
            var testConfigProvider = new TestConfigProvider();
            var testConfig = new TextDisplayConfig()
            {
                DriverType = "NonExistentDriver",
                Height = 0,
                Width = 0
            };
            (testConfigProvider.Configs as List<TextDisplayConfig>).Add(testConfig);
            List<ITextDisplay> returnedDisplays;

            // Act
            returnedDisplays = await TextDisplayManager.GetDisplaysForProvider(testConfigProvider) as List<ITextDisplay>;

            // Assert
            Assert.AreEqual(0, returnedDisplays.Count, "No displays should be returned");
        }

        [TestMethod]
        public async Task TestNonExistentAssemblyDriverTypeFails()
        {
            // Arrange
            var testConfigProvider = new TestConfigProvider();
            var testConfig = new TextDisplayConfig()
            {
                DriverType = "NonExistentDriver",
                DriverAssembly = "NonExistentAssembly",
                Height = 0,
                Width = 0
            };
            (testConfigProvider.Configs as List<TextDisplayConfig>).Add(testConfig);
            List<ITextDisplay> returnedDisplays;

            // Act
            returnedDisplays = await TextDisplayManager.GetDisplaysForProvider(testConfigProvider) as List<ITextDisplay>;

            // Assert
            Assert.AreEqual(0, returnedDisplays.Count, "No displays should be returned");
        }
    }
}
