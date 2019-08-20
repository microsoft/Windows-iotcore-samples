# I2C Compass

This sample uses I2C on Windows IoT Core to communicate with an HMC5883L Magnetometer device.

## Set up your hardware
Please reference the datasheet for the HMC3883L found [here](https://github.com/microsoft/Windows-iotcore-samples/blob/develop/Samples/I2CCompass/HMC5883L_3-Axis_Digital_Compass_IC.pdf).

For more information on compass heading using magnetometers please see [here](https://github.com/microsoft/Windows-iotcore-samples/blob/develop/Samples/I2CCompass/AN203_Compass_Heading_Using_Magnetometers.pdf).

## Load the project in Visual Studio
You can find the source code for this sample by downloading a zip of all of our samples [here](https://github.com/Microsoft/Windows-iotcore-samples/archive/master.zip). Extract the zip to your disk, then open the `Samples\I2CCompass\CS\I2CCompass.sln` project from Visual Studio.

Build the project, then [deploy](https://github.com/MicrosoftDocs/windows-iotcore-docs/blob/master/windows-iotcore/develop-your-app/AppDeployment.md) the application to your device.

## Additional information
The HMC3883L device is accessed through the [Windows.Devices.I2c API](https://docs.microsoft.com/en-us/uwp/api/windows.devices.i2c) using the default I2C controller and the address 0x1E.
```CS
    // Setup the settings to address 0x1E with a 400KHz bus speed
    var i2cSettings = new I2cConnectionSettings(ADDRESS);
    i2cSettings.BusSpeed = I2cBusSpeed.FastMode;

    // Create an I2cDevice with our selected bus controller ID and I2C settings
    var controller = await I2cController.GetDefaultAsync();
    _i2cController = controller.GetDevice(i2cSettings);
```
