---
page_type: sample
urlFragment: hellocloudheadless
languages:
  - csharp
products:
  - windows
description: Create a headless hello cloud sample with Windows 10 IoT Core and Azure.
---

# Hello Cloud - Headless sample

## Adapted from [here](https://blogs.windows.com/buildingapps/2015/12/09/windows-iot-core-and-azure-iot-hub-putting-the-i-in-iot/).
===============

### What you'll need:
* Microsoft Visual Studio 2015 or [Visual Studio Community](https://docs.nuget.org/consume/Package-Manager-Dialog)
* An active Azure subscription (choose a free version of the Azure IoT Hub service)

To set up an instance of the IoT Hub, log on to the [Azure Portal](https://portal.azure.com/) and click “New” on the jumpbar on the left, then “Internet of Things,” then “IoT Hub.” (The process is straightforward, but if you want additional pointers, there are great tips in [this article](https://azure.microsoft.com/en-us/documentation/articles/iot-hub-csharp-csharp-getstarted/#create-an-iot-hub)). Be sure to choose “F1 –Free” pricing and scale tier. The free option provides one IoT Hub per subscription, which includes 10 devices and 3000 messages per day.

### Creating a device ID

Once you’ve set up your instance of IoT Hub, the first thing you need is to create the identity for your device. Every device must have a key that uniquely identifies it to the service. This way the IoT Hub can securely associate the data with the device that sent it.

1. To create a device ID, use a tool called ‘iothub-explorer’. The tool requires Node.js, so make sure it’s installed on your machine (or get it [here](https://nodejs.org/en/download/)).

2. Now open the command line prompt nad install the iothub-explorer package:

```
npm install -g iothub-explorer@latest
```

The tool will need the connection string to connect to your instance of the Azure IoT Hub. You can find the connection string in the Azure Portal under the settings tab of your IoT Hub instance: navigate to Settings | Shared access policies | Connection string – primary key.

3. Now create a new device called "My Device". Run the tool as follows:

```
iothub-explorer <yourConnectionString> create MyDevice
```

Remember to use your actual connection string in the above command. The tool will create the new device ID and output the primary key for the device, among other things. The primary key is what you’ll use to connect your device to the IoT Hub.

The tool also allows you to list and delete existing devices, as well as listen to the messages coming into the IoT Hub. It might be worth taking a few minutes to explore the tool. Type ‘iothub-explorer help’ to learn about the other commands.

### Deploy the app

1. In Visual Studio 2015, go to File | New | Project menu, select the Blank App UWP application and call it "HelloCloud".

You will use the Microsoft.Azure.Devices.Client library to connect to Azure, so go ahead and install it from the NuGet gallery. (And if you’re curious to know what’s in this library, you can check it out on GitHub [here](https://github.com/azure/azure-iot-sdks)). 

> [!NOTE]
> You will use the UI NuGet and not the command-line. To do this, you’ll need to right click on “References” in the Solution Explorer, then click on "Manage NuGet Packages".

2. For this example, you’ll want to add a reference to the Windows IoT Extensions for the UWP. Add these using directives at the top of the MainPage.xaml.cs file:

```
using System.Text;
using Microsoft.Azure.Devices.Client;
```

3. Next, in the generated project, add the following function to the MainPage class in the MainPage.xaml.cs file:

```
static async void SendDeviceToCloudMessagesAsync()
{
    string iotHubUri = "<replace>"; // ! put in value !
    string deviceId  = "<replace>"; // ! put in value !
    string deviceKey = "<replace>"; // ! put in value !
 
    var deviceClient = DeviceClient.Create(iotHubUri,
            AuthenticationMethodFactory.
                CreateAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey), 
            TransportType.Http1);
 
    var str = "Hello, Cloud!";
    var message = new Message(Encoding.ASCII.GetBytes(str));
 
    await deviceClient.SendEventAsync(message);
}
```
4. Then, invoke this new function from the constructor of your MainPage class in the MainPage.xaml.cs file:

```
public MainPage()
{
    this.InitializeComponent();
    SendDeviceToCloudMessagesAsync();
}
```

5. A few things need to be adjusted in the above code before you can run this app. The iotHubUri needs to be set to the Uri of your IoT Hub, the value of which you can find under the Settings tab. (This should look like your_hub_name.azure-devices.net). The deviceId and deviceKey were created earlier by the CreateDeviceIdentity tool – remember to replace them as well.


Run the app on your IoT device (if you need instructions on how to set up your device, see [here](https://docs.microsoft.com/en-us/windows/iot-core/getstarted)) and – voila! – your data has been sent to the cloud.

### Processing the data in the cloud

Sending data to the cloud is only meaningful if something useful can be done with the data once it gets there. There are many ways to process data in Microsoft Azure – you can connect it to other services such as Azure Stream Analytics, store it into an Azure Blob, visualize it in PowerBI etc.

One simple example is to write an app that retrieves messages sent to the IoT Hub and displays them in a console window – not too fancy, but you can imagine doing something more interesting, such as triggering an alert or sending a command back to the device.

1. Create a console application in Visual Studio 2015 named "ReadDeviceToCloudMessages."" Add the WindowsAzure.ServiceBus library from the NuGet gallery and replace the generated Program.cs with the following code:

```
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
 
class Program
{
    static void Main(string[] args)
    {
        string connectionString = "<replace>";
        string iotHubD2cEndpoint = "messages/events";
 
        var eventHubClient = EventHubClient.
            CreateFromConnectionString(connectionString, iotHubD2cEndpoint);
 
        var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;
 
        foreach (string partition in d2cPartitions)
        {
            var receiver = eventHubClient.GetDefaultConsumerGroup().
                CreateReceiver(partition, DateTime.Now);
            ReceiveMessagesFromDeviceAsync(receiver);
        }
        Console.ReadLine();
    }
 
    async static Task ReceiveMessagesFromDeviceAsync(EventHubReceiver receiver)
    {
        while (true)
        {
            EventData eventData = await receiver.ReceiveAsync();
            if (eventData == null) continue;
 
            string data = Encoding.UTF8.GetString(eventData.GetBytes());
            Console.WriteLine("Message received: '{0}'", data);
        }
    }
}
```

2. As before, replace the connection string with the actual connection string for your IoT Hub instance. When yo ucompile and run the application, it will start listening for the messages sent to the IoT Hub. To make sure it works, re-run the HelloCloud UWP application that you built before. You should see the following output coming from the ReadDeviceToCloudMessages:

```
Message received: 'Hello, Cloud!'
```
