# Cognitive Services Demo for Windows 10 IoT Core

_Special thanks to Microsoft MVP Jiong Shi for contributing this sample to the community!_

We'll create a simple app that demonstrates Microsoft Cognitive Services on Windows 10 IoT Core Devices.

This is a headed sample.  To better understand what headed mode is and how to configure your device to be headed, follow the instructions [here](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/headlessmode).

## Hardware components 

You will need the following components:

1. Windows 10 IoT Core device (Raspberry Pi, MinnowBoard Max, or DragonBoard).
2. Network Access.
3. Mouse connected to your IoT device.
4. [Microsoft LifeCam HD-3000](https://www.hackster.io/products/buy/28947?s=BAhJIhY4MjUyNSxCYXNlQXJ0aWNsZQY6BkVG%0A)
5. Removable storage (Optional).

## Step 1: Create Vision API Services on Azure Portal

For this part, we will create Vision Services that we used on Azure Portal. Login to the [Azure Portal](https://portal.azure.com) with your username and password. Click “Create a resource” in the Search bar, input “Computer Vision API”. Then click “Create” to start the creation process. Input your computer vision name, location, pricing tier (F0 is enough for this project) and resource group. After the resource is created, go to your Computer Vision API page, click Keys under Resource management, copy Key 1 to your local document for further use. 

Next, you will create your “Face API” just as “Computer Vision API” above. Remember to copy the API Keys to your local document for further use.

## Step 2: Deploy and Run UWP Application on Windows 10 IoT Core device

We can make a copy of the folder on your disk and open the project from Visual Studio. Please make sure that you install Visual Studio 2017 and Windows Software Development Kit (SDK) version 16299 for Windows 10. Then, copy and paste your “Computer Vision API” and “Face API” that you get in Step 1 to “FacePage.xaml.cs” and “PhotoPage.xaml.cs”.

If you want this app running on RPi or Dragon Board, choose ARM and Remote Machine (For Minnow Board MAX, please choose x64), input the IP address of the device, press Debug button. After the deployment, we can see the app running on device.

## Step 3: Cognitive Services for Local Resources

In order to test the Cognitive Services for local resources, we may copy some pictures to the Windows 10 IoT Core device. One convenient way to achieve this is using Windows Device Portal. Open your Edge, or Chrome on the Desktop PC, which is connected in the same local network with Windows 10 IoT Core device. Then input the IP address+8080 port in the address bar. Input user name and password (the default user name and password are “administrator” and “p@ssw0rd”). For more information, readers can refer to the official page [here](https://docs.microsoft.com/en-us/windows/iot-core/manage-your-device/deviceportal). Click Apps->File explorer, choose Picture folder.
Then we can choose local pictures and upload them to the IoT Device.
After this step, you can see that there are some pictures that you can use for our Cognitive Service App.

**Note: Please make sure that you remember the path of your pictures. You can also upload to other folders, such as Documents, Music and so on.**

Then, switch back to the UWP app running on IoT Device. Click the Photo Analysis menu, you will get Photo Analysis page.

Click the Browser button on the right corner, double click the Pictures folder and the pictures that you uploaded above will show.

Choose one picture then double click or click “select” button, the Photo Analysis will start. If the network is OK, then the results will show on the screen in a few seconds.

So, we know that in the Photo Analysis page, the descriptions tags, and faces will be provided.

Now, let’s test the Face Page. First, click Face menu. Then click Browser button on the right corner, double click Pictures folder, the pictures that you uploaded above will show. Choose one picture then double click or click “select” button, the Face Analysis will start. If the network is OK, then the results will show on the screen in a few seconds.

So, we know that in Face Analysis page, the faces and emotions will be given.

**Note: Please make sure that your upload pictures are JPEG, PNG or BMP format resources. Here we only support three kinds of pictures.**

## Step 4: Cognitive Services for Removable Resources

As we designed above, the pictures can be located in local IoT device as well as Removable devices, such as USB flash disk.

Firstly, copy the jpg, png or bmp picture files to the USB flash disk. Then, Choose Removable Storage in the Photo Analysis page or Face Analysis page.

Photo Analysis or Face Analysis will begin. If the network is OK, then the results will show on the screen in a few seconds.

## Step 5: Cognitive Services for realtime camera captured image

If a web camera is plugged in, then users will see  "Camera preview succeeded" on the notification textbox.

Then we can click "Show Preview" to make Preview display on screen. As soon as we click "Take Photo" button, we will get Photo analysis or Face analysis by cognitive service.

## Summary
In this tutorial, we have designed a UWP Cognitive Services app running on Windows 10 IoT Core device. And then give the demonstrations of Cognitive Services API generation, UWP deployment and test results for local resource/removable resource. Hope this will be useful for those who need Cognitive Services on Windows 10 IoT Core device.

For more information, please refer to the project page [here](https://www.hackster.io/JiongShi/microsoft-cognitive-services-demo-on-windows-10-iot-core-4d846e).
