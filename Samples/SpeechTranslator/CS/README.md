# Build the Speech Translator Project

### Component Lists
- An IoT Device (e.g. 2 Raspberry Pi 2 or 3 boards)
- A Headset [e.g. the Microsoft LifeChat-3000 Headset](http://www.microsoft.com/hardware/en-us/p/lifechat-lx-3000/JUG-00013) 
- A Mouse
- A router connected to the Internet 
- An ethernet cable
- An HDMI montor and cables 
- A micro-SD card and reader

### Setup your Development PC (Required for Windows IoT Builds 15063 or greater.)
1. Install the Windows ADK for Windows 10, version from [here](https://developer.microsoft.com/en-us/windows/hardware/windows-assessment-deployment-kit)
    - Ensure that "Imaging And Configuration Designer" (ICD) is selected.
    - Install to the default location.    
2. Install the Windows IoT Core ADK Add-Ons from [here](https://developer.microsoft.com/en-us/windows/hardware/windows-assessment-deployment-kit)
    - Install to the default location.
  
### Setup your IoT Device
1. Install a clean O/S to your IoT Device.
2. Connect your device to the router, and connect the router to the Internet.
3. Connect the headset and mouse to your device.
4. Boot your device.
5. Select a language and, if Wi-Fi is supported on your IoT Device, skip the Wi-Fi Configuration step.
6. Provide Voice Permission from Cortana when prompted.  **(Required for Windows 10 IoT Builds 15063 or greater)**
    1. At the first Cortana Prompt, **press OK**.
    2. At the second Cortana Prompt, press **Maybe Later**.     
7. Rename your device
    1. Use Powershell to connect to your device
    2. Change the name of your device to "speechtranslator"
        - **setcomputername speechtranslator**
    3. Reboot your device.
8. Copy a Speech Language Package to your device.  **(Required for Windows 10 IoT Builds 15063 or greater)**
    1. Open a new powershell window on your desktop
    2. From desktop map your device's disk to a local drive:
        - **net use t: \\speechtranslator\c$ /user:\administrator p@ssw0rd)**
    3. Copy a language package from your desktop to your device.  
        *Note: Language packages are available in this folder:* c:\Program Files (x86)\Windows Kits\10\MSPackages\retail\ **your-processor-type** \fre
        
        For example, to copy Spanish Mexican to an arm based device:
        - **copy "c:\Program Files (x86)\Windows Kits\10\MSPackages\retail\amd64\fre\Microsoft-Windows-OneCore-Microsoft-SpeechData-es-MX-Package.cab" t:**                   
    4. Unmap your local drive (e.g. net use /delete t:).
    
9. Apply the Speech Language Package to your device.  **(Required for Windows 10 IoT Builds 15063 or greater)**
    
    *Note: Your device should reboot after committing each update and a "spinning gears" screen will appear until the update completes.*
    
    - **applyupdate -stage c:\Microsoft-Windows-OneCore-Microsoft-SpeechData-es-MX-Package.cab**
    - **applyupdate -commit**
 
10. Wait for your device to reboot

### Setup Azure with Cognitive Services
1. Follow [these instructions](http://docs.microsofttranslator.com/text-translate.html) to configure your Azure account with the Cognitive Services APIs.
2. After creating your account and subscribing to the Cogntive Services APIs make note of one of the subscription keys for your account.
    From Azure Web Portal Select:
    - "All Resources" (may appear as a 3x3 grid icon).
    - Select your CognitiveServices subscription from the list.
    - Under the "Cognitive Services account" menu, select "Keys". 
    - Make note of *either* Key 1 or Key 2, you will need to add this key to the sample source before rebuilding.
    
### Setup your sample
1. Download the sample from [here](https://github.com/Microsoft/Windows-iotcore-samples/archive/master.zip) to your local PC.
2. Open the solution file in visual studio.
3. Open the constantParam.cs file.
4. Replace the subscriptionKey with either Key 1 or Key 2 [from instructions above](#Setup-Azure-with-Cognitive-Services).
5. Rebuild the solution.
6. Deploy and run on your device.
7. While wearing your headset, Press "Start Talk".
8. Say something in English, the "Message Recognized" box should contain the spoken English phrase.
9. Press "Stop Talk".
10. After a moment the Translated Message should appear in the dialog box, and you should hear the spoken phrase through your headset.
11. The process can be reversed, by changing the "Recognizer Language" to your installed language (e.g. Spanish) and speaking a phrase in that language.

	
