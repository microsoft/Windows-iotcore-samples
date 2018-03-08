# Raspberry Pi Cognitive Service sample

In this tutorial, we will walk through the vision service on Windows 10 IoT Core and make a UWP app. 

To deploy this sample, you'll need:
* A Windows 10 IoT Core device (Raspberry Pi, MinnowBoard Max or DragonBoard)
* Network access
* Mouse connected to your IoT device
* Microsoft LifeCam HD-3000
* Removable storage (optional)

## Create Vision API Services on Azure Portal

For this part, we will create Vision Services that we used on Azure Portal. Login to the [Azure Portal](https://portal.azure.com) with your username and password. Click “Create a resource” in the Search bar and input “Computer Vision API”. Then click “Create” to start a creation process, input your Computer Vision name, location, pricing tier (F0 is enough for this project) and resource group. After the resource is created, go to your Computer Vision API page, click Keys under Resource management and copy Key 1 to your local document for further use. See the screenshot below.

<img src="../../../Resources/images/RPiCognitiveService/keys.jpg">


Next, you will create your “Emotion API” and “Face API” just as “Computer Vision API” above. Remember to copy the API Keys to your local document for further use.

## Deploy and Run a UWP Application on your Windows 10 IoT Core device

In this section, you'll download the project from this GitHub. Please make sure that you install Visual Studio 2017 and the Windows Software Development Kit (SDK) Version 16299 for Windows 10. Copy and paste your “Computer Vision API”, “Emotion API” and “Face API” that you got in Step 1 to “FacePage.xaml.cs” (in lines 38 and 39) and “PhotoPage.xaml.cs” (in line 35) as follows:

<img src="../../../Resources/images/RPiCognitiveService/api.jpg">

If you want this app running on a Raspberry Pi or Dragon Board, choose ARM and Remote Machine (for Minnow Board MAX, please choose x64). Input the IP address of the device, click Debug. After deployment, we can see the app running on device as follows:

<img src="../../../Resources/images/RPiCognitiveService/device.jpg">

## Cognitive Services for Local Resources

In order to test Cognitive Services for local resources, we'll copy some pictures to the Windows 10 IoT Core device. One convenient way to do this is to use the [Windows Device Portal](https://docs.microsoft.com/en-us/windows/iot-core/manage-your-device/deviceportal). Open your web browser of choice on the Desktop PC, which is connected in the same local network with Windows 10 IoT Core device. Then input the IP address+8080 port in the address bar. Input user name and password (the default user name and password are “administrator” and “p@ssw0rd”). Click Apps->File explorer, choose Picture folder as follows:

<img src="../../../Resources/images/RPiCognitiveService/file-path.jpg">

Then choose local photos and upload them to the device.

<img src="../../../Resources/images/RPiCognitiveService/upload.jpg">

You'll see that there are now photos you can use for the Cognitive Service App.

Switch back to the UWP app running on the device. Click the Photo Analysis menu and you will get the following page:

<img src="../../../Resources/images/RPiCognitiveService/analysis.jpg">

Click the Browser button on the right corner, double click the Pictures folder, and photos that you uploaded above will show as follows:

<img src="../../../Resources/images/RPiCognitiveService/photos.jpg">

Choose one picture then double click or click the “select” button. Photo Analysis will start. If the network is good, then the results will show on the screen in a few seconds as follows:

<img src="../../../Resources/images/RPiCognitiveService/skateboard.jpg">

Now let’s test the Face Page. First, click the Face menu. Then click the Browser button on the right corner, double click the Pictures folder, the photos that you uploaded above will show. Choose one photos then double click or click “select” button. Face Analysis will start. If the network is good, then the results will show on the screen in a few seconds as follows.

<img src="../../../Resources/images/RPiCognitiveService/family.jpg">

## Cognitive Services for Removal Resources

As shown above above, the photos can be located in the local IoT device as well as Removable devices, such as a USB flash disk. 

First, copy the jpg, png or bmp picture files to the USB flash disk. Then, choose Removable Storage in the Photo Analysis page or Face Analysis page as follows:

<img src="../../../Resources/images/RPiCognitiveService/removable.jpg">

Photo Analysis or Face Analysis will begin. If the network is good, then the results will show on the screen in a few seconds as follows:

<img src="../../../Resources/images/RPiCognitiveService/Screenshot.jpg">
