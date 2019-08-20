# Near Field Communication
Near Field Communication allows a device to send small pieces of data when in close proxmimity to other NFC devices. NFC hardware is required for this communication. 
On Windows 10 IoT Core, you can communicate with the hardware manually by using the GPIO, I2C or SPI APIs, or you can use the SmartCard APIs if a driver is associated with the hardware.

In this sample, we will demonstrate how to set up the NXP NFC SBC Kit and how to communicate with an NXP NTAG21x.

## Prerequisites 
In order to build and test this sample, you will need the following:

  * [Visual Studio 2015 Update 3](http://go.microsoft.com/fwlink/?LinkId=691129).
  * [NXP Explore-NFC Kit](http://www.digikey.com/products/en?mpart=OM5577&v=568).
  * [BullsEye NFC NTAG216 Sticker](https://dangerousthings.com/shop/bullseye/) or [xNTi NFC Implant Kit](https://dangerousthings.com/shop/xnti/)


## Set up a Raspberry Pi

  1. Before powering on your Raspberry Pi, assemble the NXP Explore-NFC kit, and attach to the Rasberry Pi.
  1. Set up your Raspberry Pi using the instructions [here](https://docs.microsoft.com/en-us/windows/iot-core/tutorials/rpi).

## Build the ACPI Table.
In order to assocate the NXP Explore-NFC hardware with the driver, resources need to be allocated for it in the ACPI Table. Included in the sample is a file ```pn71x0.asl```, which 
needs to be compiled in order to apply it to your Raspberry PI.

  1. Open a ```VS2015 x64 Native Tools Command Prompt```
  1. Within the command prompt change directory to the NFCOnIoT sample.
  1. Within the command prompt run ```"C:\Program Files (x86)\Windows Kits\10\Tools\x64\ACPIVerify\asl.exe" pn71x0.asl```. NOTE: you will get a warning; this is benign.
  1. This command will have generated a file called ```ACPITABL.dat``` which will be copied to your pi in the next section.
  1. You can type ```start .``` to open an explore window here, which you will use in the next step.

## Setup the NFC Hardware

  1. In the IoT Dashboard, find your Raspberry Pi, then right click and select ```Open Network Share```, Enter credentials if prompted.
  1. Once the network share opens, navigate to ```windows\system32```.
  1. Copy the ```ACPITABL.dat``` from the explorer window you opened in the previous section, and copy it to the folder in on the network share you opened in the previous step.
  1. On the network share, navigate to ```c:\data```.
  1. From the Explorer Window which contains the ```ACPITabl.dat```, there is also a file ```pn71x0.inf```. Copy this to ```c:\data``` on the network share.
  1. Use [SSH](https://docs.microsoft.com/en-us/windows/iot-core/connect-your-device/ssh) or [Powershell](https://docs.microsoft.com/en-us/windows/iot-core/connect-your-device/powershell) to connect to your device. 
  1. Change directory to ```c:\data``` in the connected session.
  1. Run the command ```devcon dp_add pn71x0.inf```.
  1. Run the command ```shutdown -r -t 0``` to restart the Rasberry PI for the hardware changes to take effect.

  
## Configure the NFC Service to start automatically
In order to minimize the number of resources used by IoT Core, the NFC Service does not start by default. To make it start automatically, use [SSH](https://docs.microsoft.com/en-us/windows/iot-core/connect-your-device/ssh) to connect to the device and then

  1. Run ```sc config SEMgrSvc start=auto``` to set this service to autostart on boot.
  1. Run ```sc start SEMgrSvc``` to start it for this session.
  
If you prefer [Powershell](https://docs.microsoft.com/en-us/windows/iot-core/connect-your-device/powershell) to connect to your device, run the following commands in PowerShell.

  1. ```Set-Service SEMgrSvc -StartupType "Automatic"``` to set this service to autostart on boot.
  1. ```Start-Service SEMgrSvc``` to start it for this session. 
  
## Running the NFC Sample

   1. In the Samples folder you downloaded from Github, open NFCForIoT.sln.
   1. Build and deploy the application to your Raspberry Pi.
   1. Use the NTAG21x of your choice to see information about it and optionally configure it.
