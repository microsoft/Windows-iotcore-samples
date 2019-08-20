---
page_type: sample
urlFragment: iot-blockly
languages: 
  - csharp
products:
  - windows
description: A sample that shows how to leverage various pieces of open source software to create a "block" development experience on Raspberry Pi for Windows 10 IoT Core.
---

# IoT Blocky
IoTBlockly leverages various pieces of open source software to create a "block" development experience right on your Raspberry Pi.

<ol>
<li>[Google Blockly](https://developers.google.com/blockly) for the block editor.</li>
<li>[Chakra JavaScript engine](https://github.com/Microsoft/Chakra-Samples) to execute JavaScript snippets.</li>
<li>Emmellsoft.IoT.Rpi.SenseHat library to control the [Raspberry Pi Sense Hat](https://github.com/emmellsoft/RPi.SenseHat).</li>
</ol>

## Requirements:
<ol>
<li>Raspberry Pi 2 or Raspberry Pi 3</li>
<li>[Raspberry Pi Sense Hat]((https://www.raspberrypi.org/products/sense-hat)</li>
<li>Windows 10 Core installed and running on the Raspberry Pi</li>
</ol>

## Usage
Compile the solution and deploy the IoTBlocklyBackgroundApp to your Raspberry Pi. Once the app is up and running, browse to http://your-rpi-name:8024 and start coding with Blockly! 
