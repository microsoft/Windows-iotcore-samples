# RFID scanner with Windows 10 IoTCore

In this sample, we will demonstrate how to use a relatively inexpensive device and a Raspberry Pi to measure the volume of liquid flowing through a hose.
Keep in mind that the GPIO APIs are only available on Windows 10 IoT Core, so this sample cannot run on your desktop.

This is a headed sample. To better understand what headed mode is and how to configure your device to be headed, follow the instructions [here](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/headlessmode).


## Load the project in Visual Studio

## Connect the MFRC522 to your Windows 10 IoT Core device

You'll need a few components:

* MFRC522 scanner
* a Piezo Buzzer (If you want a beep when a card scans)
* a breadboard and a couple of connetor wires

### For Raspberry Pi 2 or 3 (RPi2 or RPi3)

1. Connect RFID SDA to Pin 24
2. Connect RFID SCK to Pin 23
3. Connect RFID MOSI to Pin 19
4. Connect RFID MISO to Pin 21
5. Connect RFID GND to Pin 6
6. Connect RFID RST to Pin 22
7. Connect RFID 3.3V to Pin 1 (For some higher frequency cards this might need 5V)

For reference, here is the pinout of the RPi2 and RPi3:

![](../../../Resources/images/PinMappings/RP2_Pinout.png)

## Deploy your app

1.  With the application open in Visual Studio, set the architecture in the toolbar dropdown. We use `ARM` since we used the Raspberry Pi, but if you’re building for MinnowBoard Max, remember to select `x86`.

2.  Next, in the Visual Studio toolbar, click on the `Local Machine` dropdown and select `Remote Machine`

    ![RemoteMachine Target](../../../Resources/images/HelloWorld/cs-remote-machine-debugging.png)

3.  At this point, Visual Studio will present the **Remote Connections** dialog. If you previously used [PowerShell](https://docs.microsoft.com/en-us/windows/iot-core/connect-your-device/powershell) to set a unique name for your device, you can enter it here (in this example, we’re using **my-device**). Otherwise, use the IP address of your Windows IoT Core device. After entering the device name/IP select `Universal` for Windows Authentication, then click **Select**.

    ![Remote Machine Debugging](../../../Resources/images/HelloWorld/cs-remote-connections.PNG)

4.  You can verify or modify these values by navigating to the project properties (select **Properties** in the Solution Explorer) and choosing the `Debug` tab on the left:

    ![Project Properties Debug Tab](../../../Resources/images/HelloWorld/cs-debug-project-properties.PNG)

	When everything is set up, you should be able to press F5 from Visual Studio. If there are any missing packages that you did not install during setup, Visual Studio may prompt you to acquire those now. The app will deploy and start on the Windows IoT device, and you should see a text block for returned ID from the scanner.

	![](../../../Resources/images/RFIDForIoT/RFIDForIot.jpg)

	Scan the 13.56Mhz sample card that comes with the scanner. You should be able to see a card ID pop up on the screen.

	Congratulations! You just read an ID off of a RFID card.

	## Let’s look at the code

	This sample app relies on MFRC522 library written by a github user Michiel Lowijs. The original library can be found here [MFRC522](https://github.com/mlowijs/mfrc522-netmf).
	We have adapted this library to Universal Windows platform. The adapted library can be found in the project directory by the name Mfrc522Lib.
	Along with the Mfrc522Lib, the samples directory also contains a PeizoBuzzerLib if you want to use the Piezo Buzzer with the sample.

	
## Additional resources
* [Windows 10 IoT Core home page](https://developer.microsoft.com/en-us/windows/iot/)
* [Documentation for all samples](https://developer.microsoft.com/en-us/windows/iot/samples)

This project has adopted the Microsoft Open Source Code of Conduct. For more information see the Code of Conduct FAQ or contact <opencode@microsoft.com> with any additional questions or comments.
