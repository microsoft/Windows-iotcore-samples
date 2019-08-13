---
page_type: sample
urlFragment: iotcore-defaultapp
languages: 
  - csharp
products:
  - windows
description: The current IoT Core Default App, a fully featured sample app for Windows 10 IoT Core.
---

# Windows 10 IoT Core Default App Overview

> If you find that you'd like to see a feature added to this sample app, [open an issue](https://docs.microsoft.com/en-us/windows/iot-core/develop-your-app/iotcoredefaultapp) on GitHub to let us know. 

When you initially flash Windows 10 IoT Core, you will be presented with the Windows 10 IoT Core Default App upon startup, which looks like this:

![Screenshot of the IoT Core Default App](../../Resources/images/iotcoredefaultapp/DeviceInfoPage-Screenshot.jpg)

The purpose of this application is not only to provide you with a friendly shell to interact with when you first boot up Windows 10 IoT Core, but we have open-sourced the code for this application [here](http://github.com/Microsoft/Windows-iotcore-samples/tree/master/Samples/IoTCoreDefaultApp/CS) so that you can plug and play with these features on your own custom application(s).

This article will give you a rundown of the different features that the Windows 10 IoT Core Default App offers as well as how you can leverage these different features for your own applications.

## Leveraging the IoT Core Default App 

The IoT Core Default App can be customized and extended, or you can use the source code as an example for your own app. To try this out for yourself, download the zip of our samples or check out the code for the IoT Core Default App [here](https://github.com/Microsoft/Windows-iotcore-samples/tree/master/Samples/IoTCoreDefaultApp/CS). For any questions, please file an issue on our samples repo [here](https://github.com/Microsoft/Windows-iotcore-samples/issues).

As shown under the [Settings section](https://docs.microsoft.com/en-us/windows/iot-core/develop-your-app/iotcoredefaultapp#settings
) below, in some cases, you may configure default settings and features on your customer system on behalf of the end user. However, if you turn these settings and features on by default or if diagnostics are above the basic setting, you must:

* Notify the end user that these features have been enable and provide the end user with the link to Microsoft's Privacy Statement web page [here](http://go.microsoft.com/fwlink/?LinkId=521839). 
* Secure consent from the relevant end user to enable such features by default (as required by applicable law).
* Provide end users the ability to change the Diagnostics setting back to the basic setting.
* If you enable Microsoft Accounts and you have access to end user data, if the end user deletes the Microsoft Account, you must enable simultaneous deletion of all the end user's Microsoft Account data on the device. 

## Out-of-Box Experience (OOBE)

The out-of-box experience for the IoT Core Default App is as lean as it gets. The first pages will ask for a default language and wi-fi settings. From there, in order for your app to be GDPR-compliant, you must have a diagnostic data screen and, if you're planning to track location, you will need to have a location permissions screen too. Examples of both are shown below. 

![Location settings for OOBE](../../Resources/images/iotcoredefaultapp/OOBE3.jpg)
![Diagnostic settings for OOBE](../../Resources/images/iotcoredefaultapp/OOBE4.jpg)

## Command Bar
The Command Bar is the persistant horizonatal bar located at the bottom of the screen. This provides easy access to the following funtionality:
- Forward and backward page navigation
- Basic device info without leaving the current page
- Turning fullscreen mode on or off
- Advance shortcuts
- Page specific buttons

There are a lot buttons in the Command Bar, and sometimes those buttons can be confusing or hidden. To expand the Command Bar and access those buttons, please press the menu button in the bottom right:

![How to expand Command Bar](../../Resources/images/iotcoredefaultapp/CommandBar.gif)

## Start Menu - Play

The Start Menu is where most plug and play features live.

### Weather
Using data from the National Weather Service, the weather page renders weather information in your current location.

### Web Browser
The web browser allows you to pull up most sites from the web.

### Music
This page will play MP3 and WAV files from the **Music Library**, that can be accessed via the [Windows Device Portal](../manage-your-device/DevicePortal.md).  To upload files to the music player, you will need to navigate to the Windows Device Portal, click on the "Apps" dropdown, navigate to "File Explorer", select "Music" and upload your files from there.


![How to upload music files](../../Resources/images/iotcoredefaultapp/music.gif)

### Slideshow
This page will display any PNG or JPEG image files from the **Pictures Library**, that can be accessed via the [Windows Device Portal](../manage-your-device/DevicePortal.md). To upload images to the slideshow, you will need to navigate to the Windows Device Portal, click on the "Apps" dropdown, navigate to "File Explorer", select "Pictures" and upload your files from there.


![How to upload music files](../../Resources/images/iotcoredefaultapp/slideshow.gif)

### Draw
This page allows you to test out Windows 10 IoT Core's inking capabilities.

## Start Menu - Explore 

### Apps 
This page allows you to launch other foreground applications installed on the device. Launching an application will suspend IoT Core Default App, which can be relaunched by using App Manager in [Windows Device Portal](../manage-your-device/DevicePortal.md).

Nothing special is needed to have your foreground application listed in the page, simply [install](AppInstaller.md) or [deploy](AppDeployment.md) the application. After successful installation or deployment, re-navigate to the Apps page to refresh the list of applications.

Note that there are a couple of auto-generated OS related applications that we filter out, you can find the list of app names [here](http://github.com/Microsoft/Windows-iotcore-samples/tree/master/Samples/IoTCoreDefaultApp/CS/Views/AppLauncherPage.xaml.cs).

### Notifications
This page will list the past 20 notifications since IoT Core Default App was launched. When IoT Core Default App is running in debug mode, buttons are added that will create test notifications.

### Logs
This page will list any auto-generated crash or error logs, which then can be taken off the device and analyzed.

### GitHub
This page will take you to the open-sourced GitHub location of the IoT Core Default App code.

## Start Menu - Windows Device Portal

The pages in this section leverage the Windows Device Portal REST APIs, which requires you to sign in using your Windows Device Portal credentials.

## Device Information

This page allows you to see the different features for your device including Ethernet, OS version, connected devices, and more.

## Command Line

This page allows you to run commands directly on your device.

To enable this feature you have to set a registry key so that the app can run the commands. The first time you try to run a command you will see a link that allows you to set the registry key using a call to Windows Device Portal. Click the link to enable your device to run commands.

Some commands require administrator access. For security purposes the app uses a non-admin account by default to run commands. If you need to run a command as an admin you can type "RunAsAdmin <your command>" in the command line prompt.

## Settings
You'll be able to configure a number of settings here including Wi-Fi, Bluetooth, power options, and more.

### App Settings
The **App Settings** section allows you to configure various settings for pages in the app.  

Some of the settings you can customize are:

##### General Settings
* Set the default page that appears when the app is started
* Enable/disable the screensaver

##### Weather Settings
* Change the location
  > This feature is only enabled if you have provided a valid [Bing Map Service Token](https://msdn.microsoft.com/en-us/library/ff428642.aspx).  To pass the token to the app, create a **MapToken.config** file in the LocalState folder of the app (e.g. C:\Data\USERS\\[User Account]\AppData\Packages\\[Package Full Name]\LocalState\MapToken.config) and restart the app.
* Expand the map
* Enable/disable map flipping so that the map and the weather switch places periodically to prevent screen burn-in

##### Web Browser Settings
* Set the home page for the Web Browser

##### Slideshow Settings
* Set the slideshow interval

##### Appearance
* Use MDL2 Assets instead of Emojis for the tile icons
* Set the tile width and height
* Set UI scaling - Automatic scaling is set by default
* Set the tile color

#### System
Change the language, keyboard layout, and time zone.

#### Network & Wi-Fi
View network adapter properties or connect to an available Wi-Fi network.

#### Bluetooth
Pair with a Bluetooth device.

#### App Updates
Check for app updates or change automatic update settings.

#### Power Options
Restart or shutdown the device.

#### Diagnostics
Select the amount of diagnostic data you wish to provide Microsoft.  We encourage users to opt into **Full** diagnostic data so we can diagnose issues quickly and make improvements to the product.

##### Basic 
Send only info about your device, its settings and capabilities, and whether it is performing properly.

##### Full
Send all Basic diagnostic data, along with info about websites you browse and how you use apps and features, plus additional info about device health, device activity, and enhanced error reporting.

#### Location
Allow or deny the app access to your location.
