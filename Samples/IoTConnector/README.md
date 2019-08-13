---
page_type: sample
urlFragment: iot-connector
languages: 
  - csharp
products:
  - windows
description: Demonstrate device to device communication with Azure IoT Hub for Windows 10 IoT Core.
---

# IoT Connector

## Introduction

We’ve already seen how to get your devices to send data to the cloud. In most cases, the data is then processed by various Azure services. 
However, in some cases the cloud is nothing more than a means of communication between two devices.

Imagine a Raspberry Pi connected to a garage door. Telling the Raspberry Pi to open or close the door from a phone should only require a 
minimal amount of cloud programming. There is no data processing; the cloud simply relays a message from the phone to the Pi. Or perhaps 
the Raspberry Pi is connected to a motion sensor and you’d like an alert on your phone when it detects movement.

In this blog post, we will experiment with implementing device-to-device communication with as little cloud-side programming as possible. 
A common pipeline for device-to-device communication involves device A sending a message to the cloud, the cloud processing the message 
and sending it to device B, and device B receiving this message. In minimizing that middle step, you can create a functional app that 
only requires the free tier of Azure IoT Hub. It’s a cheap and effective way to design device-to-device communication. So, can two devices 
talk to each other with almost no cloud-based programming?

The answer, of course, is yes. In order to do this, it is important to understand how Azure IoT Hub and the Azure IoT messaging APIs work. 
Currently, Azure IoT messaging involves two different APIs – Microsoft.Azure.Devices.Client is used in the app running on the device (it 
can send device-to-cloud and receive cloud-to-device messages) and Microsoft.Azure.Devices SDK and the ServiceBus SDK are used on the 
service side (it can send cloud-to-device messages and receive device-to-cloud messages). However, our design proposes something slightly 
unorthodox. We will run the service SDK on the device receiving messages, so less code goes into the cloud.

To take advantage of the latest advances in security, we will provision our device to securely connect to Azure with the help of the TPM 
(see our earlier blog post that introduced TPM).

This approach uses a many-to-one messaging model. It allows for a simple design, but limits our capabilities. While many devices can send 
messages, only one can receive. In order to only accept messages from a specific device, the receiver will filter the messages by the device 
id.

## How does all this work?

For a full sample, see the code here. There are two solutions within this project, as described above. The use of the SDKs in each solution 
remains mostly unchanged from the standard design outlined in here. We decided to run the service side SDK on the receiving device – however, 
there is one roadblock. One of the two service side SDKs, ServiceBus, does not support UWP. Fortunately, another library called the 
AMPQNetLite offers a UWP compatible alternative that can be used to send and receive messages on the service side. This requires a little 
more work: we needed to connect to the event hub port that IoT Hub exposes, create a session and build the receiver link.

All the connection information needed to set up a receiver with AMQPNetLite can be found in your instance of IoT Hub. You can also use this 
library to filter incoming messages by device id. See this sample for further details.

## What next?

This experiment intentionally keeps the amount of cloud-based programming to a minimum (zero, really). Even still, this opens a set of new 
opportunities. With this, an IoT device can be remote controlled by any Windows device. However, this system has limitations. Any complex 
message filtering is currently not supported. Extending this solution to be cross platform (using Android or iOS devices) also proves to 
be difficult, as AMPQNetLite is not compatible with Xamarin.

If you’re willing to do more and utilize more cloud services (including paid ones), advanced messaging patterns, sophisticated data analysis 
and long term storage become possible. In particular, Azure Functions allow you to run the receiving code in the cloud, which obviates the 
need of running AMPQNetLite on the client device.

This blog post focused on the simplest pipeline for device-to-device communication, but what we have built is by no means the only solution. 
We’re eager to hear your feedback and welcome your ideas on what more can be done.
