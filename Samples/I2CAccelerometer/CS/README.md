# I2C Accelerometer

We'll connect an I2C accelerometer to your Raspberry Pi 2 or 3/MinnowBoard Max/DragonBoard and create a simple app to read data from it. We'll walk you through step-by-step, so no background knowledge of I2C is needed.
However, if you're curious, SparkFun provides a great [tutorial on I2C](https://learn.sparkfun.com/tutorials/i2c).

This is a headed sample.  To better understand what headed mode is and how to configure your device to be headed, follow the instructions [here](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/HeadlessMode).

### Load the project in Visual Studio

You can find the source code for this sample by downloading a zip of all of our samples [here](https://github.com/Microsoft/Windows-iotcore-samples/archive/master.zip) and navigating to the `samples\I2CAccelerometer`.  Make a copy of the folder on your disk and open the project from Visual Studio.

### Connect the I2C Accelerometer to your device

You'll need a few components:

* <a name="I2C_Accelerometer"></a>an [ADXL345 accelerometer board from Sparkfun](https://www.sparkfun.com/products/9836) with pin headers soldered on

* a breadboard and a couple of male-to-female connector wires

* If you are using a MinnowBoard Max, you'll need a 100 &#x2126; resistor (this is a workaround for a [known I2C hardware issue](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsmbm)

Visit the **Raspberry Pi 2 or 3/MinnowBoard Max** sections below depending on which device you have:

![Electrical Components](https://az835927.vo.msecnd.net/sites/iot/Resources/images/I2CAccelerometer/components.png)

#### Raspberry Pi 2 or 3
If you have a Raspberry Pi 2 or 3, we need to hook up power, ground, and the I2C lines to the accelerometer.
Those familiar with I2C know that normally pull-up resistors need to be installed. However, the Raspberry Pi 2 or 3 already has pull-up resistors on its I2C pins, so we don't need to add any additional external pull-ups here.
 See the [Raspberry Pi 2 or 3 pin mapping page](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsrpi) for more details on the RPi2 and RPi3 IO pins.

**Note: Make sure to power off the RPi2 or RPi3 when connecting your circuit. This is good practice to reduce the chance of an accidental short circuit during construction.**

The ADXL345 breakout board has 8 IO pins, connect them as follows:

1. **GND:**  Connect to ground on the RPi2 or RPi3 (Pin 6)
2. **VCC:**  Connect to 3.3V on the RPi2 or RPi3 (Pin 1)
3. **CS:**   Connect to 3.3V (The ADXL345 actually supports both SPI and I2C protocols. To select I2C, we keep this pin tied to 3.3V. The [datasheet](https://www.sparkfun.com/datasheets/Sensors/Accelerometer/ADXL345.pdf) contains much more information about the pin functions)
4. **INT1:** Leave unconnected, we're not using this pin
5. **INT2:** Leave unconnected, we're not using this pin
6. **SDO:**  Connect to ground (In I2C mode, this pin is used to select the device address. You can attach two ADXL345 to the same I2C bus if you connect this pin to 3.3V on the second device. See the [datasheet](https://www.sparkfun.com/datasheets/Sensors/Accelerometer/ADXL345.pdf) for more details)
7. **SDA:**  Connect to SDA on the RPi2 or RPi3 (Pin 3). This is the data line for the I2C bus.
8. **SCL:**  Connect to SCL on the RPi2 or RPi3 (Pin 5). This is the clock line for the I2C bus.

Here are the connections shown on a breadboard:

![Breadboard connections](https://az835927.vo.msecnd.net/sites/iot/Resources/images/I2CAccelerometer/breadboard_assembled_rpi2.png)

<sub>*Image made with [Fritzing](http://fritzing.org/)*</sub>

Here are the schematics:

![Accelerometer schematics](https://az835927.vo.msecnd.net/sites/iot/Resources/images/I2CAccelerometer/schematics_rpi2.png)

#### MinnowBoard Max
If you have a MinnowBoard Max, we need to hook up power, ground, and the I2C lines to the accelerometer. Those familiar with I2C know that normally pull-up resistors need to be installed. However, the MBM already has 10K pull-up resistors on its IO pins, so we don't need to add any additional external pull-ups here.
 See the [MBM pin mapping page](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsmbm) for more details on the MBM IO pins.

**Note: Make sure to power off the MBM when connecting your circuit. This is good practice to reduce the chance of an accidental short circuit during construction.**

The ADXL345 breakout board has 8 IO pins, connect them as follows:

1. **GND:**  Connect to ground on the MBM (Pin 2)
2. **VCC:**  Connect to 3.3V on the MBM (Pin 4)
3. **CS:**   Connect to 3.3V (The ADXL345 actually supports both SPI and I2C protocols. To select I2C, we keep this pin tied to 3.3V. The [datasheet](https://www.sparkfun.com/datasheets/Sensors/Accelerometer/ADXL345.pdf) contains much more information about the pin functions)
4. **INT1:** Leave unconnected, we're not using this pin
5. **INT2:** Leave unconnected, we're not using this pin
6. **SDO:**  Connect to ground (In I2C mode, this pin is used to select the device address. You can attach two ADXL345 to the same I2C bus if you connect this pin to 3.3V on the second device. See the [datasheet](https://www.sparkfun.com/datasheets/Sensors/Accelerometer/ADXL345.pdf) for more details)
7. **SDA:**  Connect to SDA on the MBM (Pin 15). This is the data line for the I2C bus.
8. **SCL:**  Connect to SCL on the MBM (Pin 13) through the 100 &#x2126; resistor. This is the clock line for the I2C bus.

Here are the connections shown on a breadboard:

![Breadboard connections](https://az835927.vo.msecnd.net/sites/iot/Resources/images/I2CAccelerometer/breadboard_assembled_mbm.png)

<sub>*Image made with [Fritzing](http://fritzing.org/)*</sub>

Here are the schematics:

![Accelerometer schematics](https://az835927.vo.msecnd.net/sites/iot/Resources/images/I2CAccelerometer/schematics_mbm.png)

#### DragonBoard 410c

For the DragonBoard 410c, connections need to be made from the single board computer to the power, ground, and I2C lines of the accelerometer. 
Those familiar with I2C know that normally pull-up resistors need to be installed.  However, the DragonBoard already has 2k&#x2126; resistors for its I2C capabilities.

**Note: Make sure to power off the DragonBoard when connecting your circuit.  This is good practice to reduce the change of an accidental short circuit during construction.**

You'll also need a LM317 voltage regulator along with 2x 120 &#x2126; resistors to provide power to the accelerometer. 
The regulator will output 2.5V when configured as shown in the breadboard diagram, which allows the ADXL345 board to interface with the 1.8V DragonBoard 410c.

The LM317 has 3 pins that need to be wired:

1. **Adj:**    Connect to ground through a 120 &#x2126; resistor
2. **Output:** Connect to **Adj** through a 120 &#x2126; resistor. This pin will output 2.5V once the LM317 is wired up.
3. **Input:**  Connect to 5V on the DragonBoard (Pin 37)

The ADXL345 breakout board has 8 IO pins which are connected to the DragonBoard as follows:

1. **GND:**  Connect to ground on the DragonBoard (Pin 2)
2. **VCC:**  Connect to the LM317 2.5v output rail
3. **CS:**   Connect to 2.5V (The ADXL345 actually supports both SPI and I2C protocols. To select I2C, we keep this pin tied to 2.5V. The [datasheet](https://www.sparkfun.com/datasheets/Sensors/Accelerometer/ADXL345.pdf) contains much more information about the pin functions)
4. **INT1:** Leave unconnected, we're not using this pin
5. **INT2:** Leave unconnected, we're not using this pin
6. **SDO:**  Connect to ground (In I2C mode, this pin is used to select the device address. You can attach two ADXL345 to the same I2C bus if you connect this pin to 3.3V on the second device. See the [datasheet](https://www.sparkfun.com/datasheets/Sensors/Accelerometer/ADXL345.pdf) for more details)
7. **SDA:**  Connect to SDA on the DragonBoard (Pin 17). This is the data line for the I2C bus.
8. **SCL:**  Connect to SCL on the DragonBoard (Pin 15). This is the clock line for the I2C bus.

Here is a diagram showing what your breadboard might look like with the circuit assembled:

![DragonBoard I2C Accelerometer Breadboard](https://az835927.vo.msecnd.net/sites/iot/Resources/images/I2CAccelerometer/breadboard_assembled_db410c.png)

### Deploy and run the app

When everything is set up, power your device back on, and open up the sample app in Visual Studio. Open the file **MainPage.xaml.cs** and change the following line from **Protocol.NONE** to **Protocol.I2C**:

```C++
public sealed partial class MainPage : Page
{
    /* Important! Change this to either Protocol.I2C or Protocol.SPI based on how your accelerometer is wired   */
    private Protocol HW_PROTOCOL = Protocol.I2C; 
    // ...
}
``` 

Follow the instructions to [setup remote debugging and deploy the app](https://docs.microsoft.com/en-us/windows/iot-core/develop-your-app/AppDeployment).
 The I2CAccelerometer app will deploy and start, and you should see accelerometer data show up on screen.
 If you have your accelerometer flat on a surface, the Z axis should read close to 1.000G, while X and Y are close to 0.000G. The values will fluctuate a little even if the device is standing still.
 This is normal and is due to minute vibrations and electrical noise. If you tilt or shake the sensor, you should see the values change in response. Note that this sample configures the device in 4G mode,
so you wont be able to see G readings higher than 4Gs.

![I2C Accelerometer running](https://az835927.vo.msecnd.net/sites/iot/Resources/images/I2CAccelerometer/i2caccelerometer_screenshot.png)

Congratulations! You've connected an I2C accelerometer.

### Let's look at the code
The code in this sample performs two main tasks:

1. First the code initializes the I2C bus and the accelerometer

2. Secondly, we read from the accelerometer at defined intervals and update the display

Let's start by digging into the initializations.

### Initialize the I2C bus
To use the accelerometer, we need to initialize the I2C bus first. Here is the C# code.

```C#
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

/* Initialization for I2C accelerometer */
private async void InitI2CAccel()
{
    try
    {
        var settings = new I2cConnectionSettings(ACCEL_I2C_ADDR);       
        settings.BusSpeed = I2cBusSpeed.FastMode;                       /* 400KHz bus speed */

        string aqs = I2cDevice.GetDeviceSelector();                     /* Get a selector string that will return all I2C controllers on the system */
        var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the I2C bus controller devices with our selector string             */
        I2CAccel = await I2cDevice.FromIdAsync(dis[0].Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings    */
        if (I2CAccel == null)
        {
            Text_Status.Text = string.Format(
                "Slave address {0} on I2C Controller {1} is currently in use by " +
                "another application. Please ensure that no other applications are using I2C.",
                settings.SlaveAddress,
                dis[0].Id);
            return;
        }
    }

    // ...
}
```

Here's an overview of what's happening:

* First, we get the selector strings for all I2C controllers on the device.

* Next, we find all the I2C bus controllers on the system and check that at least one bus controller exists.

* We then create an **I2CConnectionSettings** object with the accelerometer address "ACCEL_I2C_ADDR" (0x53) and bus speed set to "FastMode" (400KHz)

* Finally, we create a new **I2cDevice** and check that it's available for use.

### Initialize the accelerometer

Now that we have the **I2cDevice** accelerometer instance, we're done with the I2C bus initialization. We can now write data over I2C to start up the accelerometer. We do this with the **Write()** function.
For this particular accelerometer, there are two internal registers we need to configure before we can start using the device: The data format register, and the power control register.

1. We first write a 0x01 to the data format register. This configures the device range into +-4G mode. If you consult the [datasheet](https://www.sparkfun.com/datasheets/Sensors/Accelerometer/ADXL345.pdf), you'll see that the device can be configured in a variety of measurement modes ranging from 2G to 16G.
Higher G settings provide you with greater range at the expense of reduced resolution. We choose 4G as a reasonable trade off between the two.

2. We write a 0x08 to the power control register, which wakes the device from standby and starts measuring acceleration. Again, the [datasheet](https://www.sparkfun.com/datasheets/Sensors/Accelerometer/ADXL345.pdf) contains additional information about the device settings and capabilities.

```C#
private async void InitI2CAccel()
{
    // ...

    /* 
     * Initialize the accelerometer:
     *
     * For this device, we create 2-byte write buffers:
     * The first byte is the register address we want to write to.
     * The second byte is the contents that we want to write to the register. 
     */
    byte[] WriteBuf_DataFormat = new byte[] { ACCEL_REG_DATA_FORMAT, 0x01 };        /* 0x01 sets range to +- 4Gs                         */
    byte[] WriteBuf_PowerControl = new byte[] { ACCEL_REG_POWER_CONTROL, 0x08 };    /* 0x08 puts the accelerometer into measurement mode */

    /* Write the register settings */
    try
    {
        I2CAccel.Write(WriteBuf_DataFormat);
        I2CAccel.Write(WriteBuf_PowerControl);
    }
    /* If the write fails display the error and stop running */
    catch (Exception ex)
    {
        Text_Status.Text = "Failed to communicate with device: " + ex.Message;
        return;
    }

    // ...
}
```

### Timer code
After all the initializations are complete, we start a timer to read from the accelerometer periodically. Here is how you set up the timer to trigger every 100mS.
```C#
private async void InitI2CAccel()
{
    // ...

    /* Now that everything is initialized, create a timer so we read data every 100mS */
    periodicTimer = new Timer(this.TimerCallback, null, 0, 100);

    // ...
}

private void TimerCallback(object state)
{
    string xText, yText, zText;
    string statusText;

    /* Read and format accelerometer data */
    try
    {
        Acceleration accel = ReadAccel();
        xText = String.Format("X Axis: {0:F3}G", accel.X);
        yText = String.Format("Y Axis: {0:F3}G", accel.Y);
        zText = String.Format("Z Axis: {0:F3}G", accel.Z);
        statusText = "Status: Running";
    }

    // ...
}
```


### Read data from the accelerometer
With the I2C bus and accelerometer initialized, we can start reading data from the accelerometer. Our **ReadAccel()** function gets called every 100mS by the timer:

```C#
private Acceleration ReadAccel()
{
    const int ACCEL_RES = 1024;         /* The ADXL345 has 10 bit resolution giving 1024 unique values                     */
    const int ACCEL_DYN_RANGE_G = 8;    /* The ADXL345 had a total dynamic range of 8G, since we're configuring it to +-4G */
    const int UNITS_PER_G = ACCEL_RES / ACCEL_DYN_RANGE_G;  /* Ratio of raw int values to G units                          */

    byte[] ReadBuf;                 
    byte[] RegAddrBuf;

    /* 
     * Read from the accelerometer 
     * We first write the address of the X-Axis register, then read all 3 axes into ReadBuf
     */
    switch (HW_PROTOCOL)
    {
        case Protocol.SPI:
            // ...
        case Protocol.I2C:
            ReadBuf = new byte[6];  /* We read 6 bytes sequentially to get all 3 two-byte axes                 */
            RegAddrBuf = new byte[] { ACCEL_REG_X }; /* Register address we want to read from                  */
            I2CAccel.WriteRead(RegAddrBuf, ReadBuf);
            break;
        default:    /* Code should never get here */
            // ...
    }
    
    // ...
    
    /* In order to get the raw 16-bit data values, we need to concatenate two 8-bit bytes for each axis */
    short AccelerationRawX = BitConverter.ToInt16(ReadBuf, 0);
    short AccelerationRawY = BitConverter.ToInt16(ReadBuf, 2);
    short AccelerationRawZ = BitConverter.ToInt16(ReadBuf, 4);

    /* Convert raw values to G's */
    Acceleration accel;
    accel.X = (double)AccelerationRawX / UNITS_PER_G;
    accel.Y = (double)AccelerationRawY / UNITS_PER_G;
    accel.Z = (double)AccelerationRawZ / UNITS_PER_G;

    return accel;
}
```
Here's how this works:

* We begin by reading data from the accelerometer with the WriteRead() function. As the name suggests, this function first performs a write, followed by a read.

* The initial write specifies the register address we want to read from (which in this case is the X-Axis data register). This write ensures that a subsequent read will start from this register address.
We provide the function with a one-byte byte array representing the register address we want to write

* Next we provide the function with a read buffer of size 6 so we read 6 bytes over I2C. Since this device supports sequential reads,
**and** the X, Y, and Z data registers are next to each other, reading 6 bytes give us all of our data in one go. This ensures acceleration values don't change between reads as well.

* We get back 6 bytes of data from our read. These represent the data in the X, Y, and Z data registers respectively.
We separate out the data into their respective axes and concatenate the bytes using **BitConverter.ToInt16()**.

* The raw data is formatted as a 16-bit integer, which contains 10-bit data from the accelerometer. It can take on values ranging from -512 to 511. A reading of -512 corresponds to -4G while 511 is +4G.
 To convert this to G units, we divide by the ratio of full-scale range (8G) to the resolution (1024)

* Now that we have the G unit values, we can display the data on screen. This process is repeated every 100mS so the information is constantly updated.
