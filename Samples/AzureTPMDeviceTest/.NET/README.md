# Connecting TPM with the Azure IoT Hub

To connect to the Azure IoT Hub from a provisioned device, use the TpmDevice class from the Microsoft.Devices.Tpm library (available as
the NuGet package). Get the device information stored in the desired slot (typically slot 0), then retrieve the name of the IoT Hub,
the device ID, and the SAS token (the string containing the HMAC produced from the shared access key) and use that to create the  _DeviceClient_:

```
TpmDevice myDevice = new TpmDevice(0); // Use TPM slot 0
string hubUri = myDevice.GetHostName();
string deviceId = myDevice.GetDeviceId();
string sasToken = myDevice.GetSASToken();

var deviceClient = DeviceClient.Create(
    hubUri,
    AuthenticationMethodFactory.
        CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Amqp);

var str = "Hello, Cloud!";
var message = new Message(Encoding.SCII.GetBytes(str));

await deviceClient.SendEventAsync(message);
```

At this point, you have a connected _deviceClient_ object that you can use to send and receive messages. You can view the full working sample [here](https://github.com/ms-iot/samples/tree/develop/Azure/TpmDeviceTest).

To learn more about building secure apps for Windows IoT Core, you can view the blog post [here](https://blogs.windows.com/buildingapps/2016/07/20/building-secure-apps-for-windows-iot-core/#oqFLXiWIL1iCF8j9.97).


