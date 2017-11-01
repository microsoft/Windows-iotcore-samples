# IoT Core Default App 

We'll create a default app to demonstrate how to create a simple startup app that has some basic device management for your Windows 10 IoT Core device.

This is a headed sample.  To better understand what headed mode is and how to configure your device to be headed, follow the instructions [here]({{site.baseurl}}/{{page.lang}}/Docs/HeadlessMode).

### IoT Core Default App contents

The IoT Core Default App provides a good example of creating a user experience for IoT Core devices.

#### Set up

Upon first boot, you will be taken through a quick set up experience. Set the language and connect to Wi-Fi. If you don't have a USB Wi-Fi adapter, you can always connect later. 

![DefaultApp setup on Windows 10 IoT Core]({{site.baseurl}}/Resources/images/iotcoredefaultapp/defaultapp_oobe.png)

#### Device Info

This is the main page for you to get started. The default app is intended to help you link your PC to your device. All of the development, debugging and design happens on your PC! 

![DefaultApp on Windows 10 IoT Core]({{site.baseurl}}/Resources/images/iotcoredefaultapp/DefaultAppRpi2.png)

Use the device name and IP address listed here when connecting to you device.

#### Tutorials

A quick set of instructions on how to get your board connected to your PC. If you're on the web, you can find the same set of instructions [here]({{site.baseurl}}/{{page.lang}}/GetStarted)

![DefaultApp tutorials on Windows 10 IoT Core]({{site.baseurl}}/Resources/images/iotcoredefaultapp/defaultapp_tutorial.png)

#### Settings

From settings, you can reconfigure your language, connect via Wi-Fi and connect to a Bluetooth device.

![DefaultApp settings on Windows 10 IoT Core]({{site.baseurl}}/Resources/images/iotcoredefaultapp/defaultapp_settings.png)

### Load the project in Visual Studio

You can find the source code for this sample by downloading a zip of all of our samples [here](https://github.com/Microsoft/Windows-iotcore-samples/archive/master.zip) and navigating to the `samples-develop\IotCoreDefaultApp`.  The sample code is C#. Make a copy of the folder on your disk and open the project from Visual Studio.

Once the project is open and builds, the next step is to [deploy](https://github.com/MicrosoftDocs/windows-iotcore-docs/blob/master/windows-iotcore/develop-your-app/AppDeployment.md) the application to your device.

When everything is set up, you should be able to press F5 from Visual Studio.  The IoT Core Default App will deploy and start on the Windows IoT device.  

Note that this is the same code that is shipped as the startup app in Windows IoT Core images by default.

### Set your app as the Startup App

1. You can set your app to be the 'Startup App' for your Windows IoT Core device, so that when the device reboot, it will start your app automatically. To do so, you'll need to run a command line utility called iotstartup on the Windows IoT Core device. We will do this using PowerShell.

1. Start a PowerShell (PS) session with your Windows IoT Core device as described [here]({{site.baseurl}}/{{page.lang}}/Docs/PowerShell).

1. From the PS session, type (for simplicity, we will assume the app's name is HelloWorld, **please substitute your app's actual name**):

        [192.168.0.243]: PS C:\> iotstartup list HelloWorld

    and you should see the full name of the UWP application, i.e. something like:

        Headed   : HelloWorld_n2pe7ts0w7wey!App

    the utility is confirming that your app is an 'headed' application, and is installed correctly.

1. Now, it's easy to set this app as the 'Startup App'. Just type the command:

        [192.168.0.243]: PS C:\> iotstartup add headed HelloWorld

    The utility will confirm that the new Startup headed app is now your app:

        AppId changed to HelloWorld_n2pe7ts0w7wey!App

1. Go ahead and restart your Windows IoT Core device. From the PS session, you can issue the shutdown command:

        [192.168.0.243]: PS C:\> shutdown /r /t 0

1. Once the device has restarted, you'll see your app start automatically.

1. At this point, you can revert back to using the DefaultApp as your 'Startup App'. Just type the command:

        [192.168.0.243]: PS C:\> iotstartup add headed IoTCoreDefaultApp

    The utility will confirm that the new Startup headed app is now IoTCoreDefaultApp:

        AppId changed to IoTCoreDefaultApp_kwmcxzszfer2y!App
