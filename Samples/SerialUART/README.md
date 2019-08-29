---
page_type: sample
urlFragment: serial-uart
languages: 
  - csharp
products:
  - windows
description: Communicate between a desktop and a Windows 10 IoT Core device over a serial interface.
---

# Serial UART 

We'll create a simple app that allows communication between a desktop and an IoT device over a serial interface.

This is a headed sample.  To better understand what headed mode is and how to configure your device to be headed, follow the instructions [here](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/headlessmode).

### Load the project in Visual Studio

Make a copy of the folder on your disk and open the project from Visual Studio.

This app is a Universal Windows app and will run on both the PC and your IoT device.

### Wiring the serial connection 

You have two options for wiring up your board:

1. using the On-board UART controller
2. using a USB-to-TTL adapter cable such as [this one](http://www.adafruit.com/products/954).

#### <a name="MBM_UART"></a>On-board UART (MinnowBoard Max)

The MinnowBoard Max has two on-board UARTs. See the [MBM pin mapping page](/Samples/PinMappingsMBM) for more details on the MBM GPIO pins. 

* UART1 uses GPIO pins 6, 8, 10, and 12. 
* UART2 uses GPIO pins 17 and 19. 

In this sample we will use UART2.

Make the following connections:

* Insert the USB end of the USB-to-TTL cable into a USB port on the PC
* Connect the GND wire of the USB-to-TTL cable to Pin 1 (GND) on the MBM board
* Connect the RX wire (white) of the USB-to-TTL cable to Pin 17 (TX) on the MBM board
* Connect the TX wire (green) of the USB-to-TTL cable to Pin 19 (RX) on the MBM board

*Note: Leave the power wire of the USB-to-TTL cable unconnected.*

[UART](../../Resources/images/SerialSample/SiLabs-UART.png)

#### <a name="RPi2_UART"></a>On-board UART (Rasperry Pi2)

The Rasperry Pi 2 or 3 has one on-board UART. See the [Raspberry Pi 2 Pin Mappings page](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsrpi) for more details on the GPIO pins. 

* UART0 uses GPIO pins 6 (GND), 8 (TX) and 10 (RX). 

Make the following connections:

* Insert the USB end of the USB-to-TTL cable into a USB port on the PC
* Connect the GND wire of the USB-to-TTL cable to Pin 6 (GND) on the RPi2 or RPi3 board
* Connect the RX wire (white) of the USB-to-TTL cable to Pin 8 (TX) on the RPi2 or RPi3 board
* Connect the TX wire (green) of the USB-to-TTL cable to Pin 10 (RX) on the RPi2 or RPi3 board

*Note: Leave the power wire of the USB-to-TTL cable unconnected.*

[Raspberry Pi 2 UART](../../Resources/images/SerialSample/RPi2_UART.png)

#### On-Board UART (DragonBoard 410c)

The DragonBoard has two on-board UARTs.

* UART0 uses GPIO pins 3, 5, 7, and 9.
* UART1 uses GPIO pins 11 and 13.

In this sample, UART1 will be used.  Make the following connections:

* Insert the USB end of the USB-to-TTL cable into a USB port on the PC
* Connect the GND wire of the USB-to-TTL cable to pin 1 (GND)
* Connect the RX wire (white) of the USB-to-TTL cable to pin 11 (UART1 TX)
* Connect the TX wire (green) of the USB-to-TTL cable to pin 13 (UART1 RX)

_NOTE: Leave the power wire of the USB-to-TTL cable unconnected._

### <a name="USB_TTL_Adapter"></a>Using USB-to-TTL Adapter

**Note: Only USB-to-TTL cables and modules with Silicon Labs chipsets are natively supported on MinnowBoard Max and Raspberry Pi2.**

You will need:

* 1 X USB-to-TTL module (This is what we will connect to our RPi2 or RPi3 or MBM device. We used [this Silicon Labs CP2102 based USB-to-TTL module](http://www.amazon.com/gp/product/B00LODGRV8))

* 1 X USB-to-TTL cable (This will connect to our PC. We used [this one](http://www.adafruit.com/products/954))

Make the following connections:

* Insert the USB end of the USB-to-TTL **cable** into a USB port on the PC

* Insert the USB end of the USB-to-TTL **module** into a USB port on the RPi2, RPi3 or MBM device 

* Connect the GND pin of the USB-to-TTL **module** to the GND wire of the USB-to-TTL cable 

* Connect the RX pin of the USB-to-TTL **module** to the TX wire (green) of the USB-to-TTL cable

* Connect the TX pin of the USB-to-TTL **module** to the RX wire (white) of the USB-to-TTL cable

Leave the power pin of the USB-to-TTL cable unconnected. It is not needed.

Below is an image of our USB-to-TTL module connected to a USB port in our RPi2 or RPi3. The GND, TX, and RX pins of the module are connected to the GND, RX, TX wires of the USB-to-TTL cable that is connected to our PC.

[CP 2102 Connections](../../Resources/images/SerialSample/CP2102_Connections_500.png)

### Deploy and Launch the SerialSample App

Now that our PC and RPi2, RPi3 or MBM are connected, let's setup and deploy the app.

1. Navigate to the SerialSample source project. 

2. Make two separate copies of the app. We'll refer to them as the 'Device copy' and 'PC copy'.

3. Open two instances of Visual Studio 2017 on your PC. We'll refer to these as 'Instance A' and 'Instance B'.

4. Open the Device copy of the SerialSample app in VS Instance A.

5. Open the PC copy of the SerialSample app in VS Instance B.

6. In VS Instance A, configure the app for deployment to your RPi2 or RPi3 or MBM device.
	
	*For RPi2 or RPi3, set the target device to 'Remote Machine' and target architecture to 'ARM'
	
	*For MBM, set the target device to 'Remote Machine' and target architecture to 'x86'

7. In VS Instance B, set the target architecture to 'x86'. This will be the instance of the sample we run on the PC.

8. In VS Instance A, press F5 to deploy and launch the app on your RPi2, RPi3 or MBM.

9. In VS Instance B, press F5 to deploy and launch the app on your PC.

### Using the SerialSample App 

When the SerialSample app is launched on the PC, a window will open with the user interface similar to the screenshot shown below. When launched on the RPi2 or RPi3 and MBM, the SerialSample will display the user interface shown below on the entire screen.

[Serial Sample on PC](../../Resources/images/SerialSample/SerialSampleRunningPC.PNG)

#### Selecting a Serial Device

When the SerialSample app launches, it looks for all the serial devices that are connected to the device. The device ids of all the serial devices found connected to the device will be listed in the top ListBox of the SerialSample app window.

Select and connect to a serial device on the PC and RPi2 or RPi3 or MBM by doing the following:

1. Select the desired serial device by clicking on the device ID string in the top ListBox next to "Select Device:". 

	* On the PC, the device ID for the USB-to-TTL cable connected in this example begins with '\\?\USB#VID_067B'.
	
	* On the MBM, if using the GPIO for serial communication, select the device ID with **UART2** in it. **UART1** may require using CTS/RTS signals.
    
    * On the DragonBoard, select the device with **QCOM24D4** and **UART1** in it. This will likely be the last device in the listbox (you may need to scroll down).    
	
	* On the MBM and RPi2 or RPi3, if using the USB-to-TTL adapter module, select the device ID that begins with **\\?\USB#**. For the USB-to-TTL module used in this example, the device ID should begin with '\\?\USB#VID_10C4'.

2. Click 'Connect'.	

The app will attempt to connect and configure the selected serial device. When the app has successfully connected to the attached serial device it will display the configuration of the serial device. By default, the app configures the serial device for 9600 Baud, eight data bits, no parity bits and one stop bit (no handshaking).

[Connect device](../../Resources/images/SerialSample/SerialSampleRunningPC_ConnectDevice.PNG)

#### Sending and Receiving Data

After connecting the desired serial device in the SerialSample apps running on both the PC and the RPi2 or RPi3 or MBM we can begin sending and receiving data over the serial connection between the two devices.

To send data from one device to the other connected device do the following:

1. Choose a device to transmit from. On the transmit device, type the message to be sent in the "Write Data" text box. For our example, we typed "Hello World!" in the "Write Data" text box of the SerialSample app running on our PC.

2. Click the 'Write' button.

The app on the transmitting device will display the sent message and "bytes written successfully!" in the status text box in the bottom of the app display.

[Send message](../../Resources/images/SerialSample/SendMessageB.PNG)

The device that is receiving the message will automatically display the text in the 'Read Data:' window.

**KNOWN ISSUES:**

* When connecting to the USB-to-TTL device for the first time from the IoT Device, you may see the error "Object was not instantiated" when you click on `Connect`. If you see this, un-plug the device, plug it back in and refresh the connection or redeploy the app.
* If you have more than one Silicon Labs USB-to-TTL devices connected to your IoT device, only the device that was first connected will be recognized. In order to run this sample, connect only one device
* When connecting USB-to-TTL device to MinnowBoard Max, use a powered USB hub or the bottom USB port


### Let's look at the code

The code for this sample uses the [Windows.Devices.SerialCommunication](https://msdn.microsoft.com/en-us/library/windows.devices.serialcommunication.aspx) namespace. 

The SerialDevice class will be used to enumerate, connect, read, and write to the serial devices connected to the device. 

**NOTE:** The SerialDevice class can be used only for supported USB-to-TTL devices (on PC, Raspberry Pi 2 or 3, and MinnowBoard Max) and the on-board UART (on MinnowBoard Max).

For accessing the serial port, you must add the **DeviceCapability** to the **Package.appxmanifest** file in your project. 

You can add this by opening the **Package.appxmanifest** file in an XML editor (Right Click on the file -> Open with -> XML (Text) Editor) and adding the capabilities as shown below:

    Visual Studio 2017 has a known bug in the Manifest Designer (the visual editor for appxmanifest files) that affects the serialcommunication capability. If your appxmanifest adds the serialcommunication capability, modifying your appxmanifest with the designer will corrupt your appxmanifest (the Device xml child will be lost). You can workaround this problem by hand editing the appxmanifest by right-clicking your appxmanifest and selecting View Code from the context menu.

``` xml
  <Capabilities>
    <DeviceCapability Name="serialcommunication">
      <Device Id="any">
        <Function Type="name:serialPort" />
      </Device>
    </DeviceCapability>
  </Capabilities>
```

### Connect to selected serial device

This sample app enumerates all serial devices connected to the device and displays the list in the **ListBox** ConnectDevices. The following code connects and configure the selected device ID and creates a **SerialDevice** object. 

```csharp
private async void comPortInput_Click(object sender, RoutedEventArgs e)
{
    var selection = ConnectDevices.SelectedItems; // Get selected items from ListBox

    // ...

    DeviceInformation entry = (DeviceInformation)selection[0];         

    try
    {                
        serialPort = await SerialDevice.FromIdAsync(entry.Id);

        // ...

        // Configure serial settings
        serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
        serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);                
        serialPort.BaudRate = 9600;
        serialPort.Parity = SerialParity.None;
        serialPort.StopBits = SerialStopBitCount.One;
        serialPort.DataBits = 8;

        // ...
    }
    catch (Exception ex)
    {
        // ...
    }
}
```

### Perform a read on the serial port

Reading input from serial port is done by **Listen()** invoked right after initialization of the serial port. We do this in the sample code by creating an async read task using the **DataReader** object that waits on the **InputStream** of the **SerialDevice** object. 

Due to differences in handling concurrent tasks, the implementations of **Listen()** in C# and C++ differ:

* C# allows awaiting **ReadAsync()**. All we do is keep reading the serial port in an infinite loop interrupted only when an exception is thrown (triggered by the cancellation token).

```csharp

private async void Listen()
{
    try
    {
        if (serialPort != null)
        {
            dataReaderObject = new DataReader(serialPort.InputStream);

            // keep reading the serial input
            while (true)
            {
                await ReadAsync(ReadCancellationTokenSource.Token);
            }
        }
    }
    catch (Exception ex)
    {
        ...
    }
    finally
    {
        ...
    }
}

private async Task ReadAsync(CancellationToken cancellationToken)
{
    Task<UInt32> loadAsyncTask;

    uint ReadBufferLength = 1024;

    // If task cancellation was requested, comply
    cancellationToken.ThrowIfCancellationRequested();

    // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
    dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

    // Create a task object to wait for data on the serialPort.InputStream
    loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

    // Launch the task and wait
    UInt32 bytesRead = await loadAsyncTask;
    if (bytesRead > 0)
    {
        rcvdText.Text = dataReaderObject.ReadString(bytesRead);
        status.Text = "bytes read successfully!";
    }            
}
```

* C++ does not allow awaiting **ReadAsync()** in Windows Runtime STA (Single Threaded Apartment) threads due to blocking the UI. In order to chain continuation reads from the serial port, we dynamically generate repeating tasks via "recursive" task creation - "recursively" call **Listen()** at the end of the continuation chain. The "recursive" call is not a true recursion. It will not accumulate stack since every recursive is made in a new task.

``` c++

void MainPage::Listen()
{
    try
    {
        if (_serialPort != nullptr)
        {
            // calling task.wait() is not allowed in Windows Runtime STA (Single Threaded Apartment) threads due to blocking the UI.
            concurrency::create_task(ReadAsync(cancellationTokenSource->get_token()));
        }
    }
    catch (Platform::Exception ^ex)
    {
        ...
    }
}

Concurrency::task<void> MainPage::ReadAsync(Concurrency::cancellation_token cancellationToken)
{
    unsigned int _readBufferLength = 1024;
    
    return concurrency::create_task(_dataReaderObject->LoadAsync(_readBufferLength), cancellationToken).then([this](unsigned int bytesRead)
    {
        if (bytesRead > 0)
        {
            rcvdText->Text = _dataReaderObject->ReadString(bytesRead);
            status->Text = "bytes read successfully!";

            /*
            Dynamically generate repeating tasks via "recursive" task creation - "recursively" call Listen() at the end of the continuation chain.
            The "recursive" call is not true recursion. It will not accumulate stack since every recursive is made in a new task.
            */

            // start listening again after done with this chunk of incoming data
            Listen();
        }
    });
}
```

### Perform a write to the serial port

When the bytes are ready to be sent, we write asynchronously to the **OutputStream** of the **SerialDevice** object using the **DataWriter** object.

```csharp
private async void sendTextButton_Click(object sender, RoutedEventArgs e)
{	
    // ...
	
    // Create the DataWriter object and attach to OutputStream   
    dataWriteObject = new DataWriter(serialPort.OutputStream);

    //Launch the WriteAsync task to perform the write
    await WriteAsync();   
	
    // ..

    dataWriteObject.DetachStream();
    dataWriteObject = null;	
}

private async Task WriteAsync()
{
    Task<UInt32> storeAsyncTask;

    // ...
	
    // Load the text from the sendText input text box to the dataWriter object
    dataWriteObject.WriteString(sendText.Text);                

    // Launch an async task to complete the write operation
    storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

    // ...    
}
```

### Cancelling Read

You can cancel the read operation by using **CancellationToken** on the Task. Initialize the **CancellationToken** object and pass that as an argument to the read task.

```csharp

private async void comPortInput_Click(object sender, RoutedEventArgs e)
{
    // ...

    // Create cancellation token object to close I/O operations when closing the device
    ReadCancellationTokenSource = new CancellationTokenSource();
	
    // ...	
}

private async void rcvdText_TextChanged(object sender, TextChangedEventArgs e)
{
    // ...

    await ReadAsync(ReadCancellationTokenSource.Token);

    // ...	
}

private async Task ReadAsync(CancellationToken cancellationToken)
{
    Task<UInt32> loadAsyncTask;

    uint ReadBufferLength = 1024;

    cancellationToken.ThrowIfCancellationRequested();
    
    // ...
	
}
 
private void CancelReadTask()
{         
    if (ReadCancellationTokenSource != null)
    {
        if (!ReadCancellationTokenSource.IsCancellationRequested)
        {
            ReadCancellationTokenSource.Cancel();
        }
    }         
}
```

### Closing the device

When closing the connection with the device, we cancel all pending I/O operations and safely dispose of all the objects. 

In this sample, we proceed to also refresh the list of devices connected.

```csharp
private void closeDevice_Click(object sender, RoutedEventArgs e)
{
    try
    {
        CancelReadTask();
        CloseDevice();
        ListAvailablePorts(); //Refresh the list of available devices
    }
    catch (Exception ex)
    {
       // ...
    }          
}    

private void CloseDevice()
{            
    if (serialPort != null)
    {
        serialPort.Dispose();
    }    

    // ...
}    
```


To summarize:

* First, we enumerate all the serial devices connected and allow the user to connect to the desired one using device ID

* We create an asynchronous task for reading the **InputStream** of the **SerialDevice** object.

* When the user provides input, we write the bytes to the **OutputStream** of the **SerialDevice** object.

* We add the ability to cancel the read task using the **CancellationToken**.

* Finally, we close the device connection and clean up when done.
