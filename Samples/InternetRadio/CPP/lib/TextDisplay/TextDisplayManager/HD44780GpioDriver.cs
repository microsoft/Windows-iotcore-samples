
using Microsoft.Maker.Devices.TextDisplay.AsyncHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Devices.Gpio;

namespace Microsoft.Maker.Devices.TextDisplay
{
    class HD44780GpioDriver : TextDisplayBase
    {
        private const short InstructionCode_ClearLcd = 1;
        private const short InstructionCode_NewLine = 192;

        private enum BitMode
        {
            Four,
            Eight
        };

        private enum Register
        {
            Data,
            Instruction
        }

        private GpioController gpio = null;

        private GpioPin registerSelectPin = null;
        private GpioPin enablePin = null;
        private GpioPin[] dataPins = null;

        private BitMode bitMode = BitMode.Eight;

        private readonly AsyncSemaphore lcdLock = new AsyncSemaphore(1);

        public HD44780GpioDriver(TextDisplayConfig config) :
            base(config)
        {
            this.gpio = GpioController.GetDefault();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async Task InitializeInternal(IDictionary<string, string> driverConfigurationValues)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (null == this.gpio)
                throw new NullReferenceException();

            try
            {
                int[] dataPinNumbers = null;

                if (driverConfigurationValues.ContainsKey("RsPin") &&
                driverConfigurationValues.ContainsKey("EnablePin") &&
                driverConfigurationValues.ContainsKey("D4Pin") &&
                driverConfigurationValues.ContainsKey("D5Pin") &&
                driverConfigurationValues.ContainsKey("D6Pin") &&
                driverConfigurationValues.ContainsKey("D7Pin"))
                {
                    int rsPin = Convert.ToInt32(driverConfigurationValues["RsPin"]);
                    int enablePin = Convert.ToInt32(driverConfigurationValues["EnablePin"]);
                    int d4Pin = Convert.ToInt32(driverConfigurationValues["D4Pin"]);
                    int d5Pin = Convert.ToInt32(driverConfigurationValues["D5Pin"]);
                    int d6Pin = Convert.ToInt32(driverConfigurationValues["D6Pin"]);
                    int d7Pin = Convert.ToInt32(driverConfigurationValues["D7Pin"]);

                    if (driverConfigurationValues.ContainsKey("D0Pin") &&
                    driverConfigurationValues.ContainsKey("D1Pin") &&
                    driverConfigurationValues.ContainsKey("D2Pin") &&
                    driverConfigurationValues.ContainsKey("D3Pin"))
                    {
                        dataPinNumbers = new int[8];

                        int d0Pin = Convert.ToInt32(driverConfigurationValues["D0Pin"]);
                        int d1Pin = Convert.ToInt32(driverConfigurationValues["D1Pin"]);
                        int d2Pin = Convert.ToInt32(driverConfigurationValues["D2Pin"]);
                        int d3Pin = Convert.ToInt32(driverConfigurationValues["D3Pin"]);

                        dataPinNumbers[0] = d0Pin;
                        dataPinNumbers[1] = d1Pin;
                        dataPinNumbers[2] = d2Pin;
                        dataPinNumbers[3] = d3Pin;
                        dataPinNumbers[4] = d4Pin;
                        dataPinNumbers[5] = d5Pin;
                        dataPinNumbers[6] = d6Pin;
                        dataPinNumbers[7] = d7Pin;

                        this.bitMode = BitMode.Eight;
                    }
                    else
                    {
                        dataPinNumbers = new int[4];

                        dataPinNumbers[0] = d4Pin;
                        dataPinNumbers[1] = d5Pin;
                        dataPinNumbers[2] = d6Pin;
                        dataPinNumbers[3] = d7Pin;

                        this.bitMode = BitMode.Four;
                    }

                    this.initializePins(enablePin, rsPin, dataPinNumbers);
                    await this.initializeChip();
                }
            }
            catch (FormatException)
            {
                Debug.WriteLine("HD44780GpioDriver: Pin config is invalid");
            }
            catch (OverflowException)
            {
                Debug.WriteLine("HD44780GpioDriver: Pin config is invalid");
            }
            catch (FileLoadException)
            {
                Debug.WriteLine("HD44780GpioDriver: Pin is already open");
            }
        }
        

        protected override async Task DisposeInternal()
        {
            await this.writeValue(Register.Instruction, InstructionCode_ClearLcd);
            await this.wait(TimeSpan.FromMilliseconds(1.64));

            this.registerSelectPin.Dispose();
            this.enablePin.Dispose();
            foreach (GpioPin p in this.dataPins)
            {
                p.Dispose();
            }
        }

        protected async override Task WriteMessageInternal(string message)
        {
            await lcdLock.WaitAsync();

            try
            {
                Debug.WriteLine("HD44780GpioDriver: Writing Message - " + message);

                await this.writeValue(Register.Instruction, InstructionCode_ClearLcd);
                await this.wait(TimeSpan.FromMilliseconds(1.64));

                int totalChars = 0;
                int lineChars = 0;
                int lines = 1;
                foreach (char c in message)
                {
                    if (this.Width == lineChars)
                    {
                        Debug.WriteLine("HD44780GpioDriver: Message overran on line" + lines);
                    }

                    if (this.Height < lines)
                    {
                        Debug.WriteLine("HD44780GpioDriver: Message contains too many lines");
                    }

                    if (c == '\n')
                    {
                        await this.writeValue(Register.Instruction, InstructionCode_NewLine);
                        lines++;
                        lineChars = 0;
                        continue;
                    }

                    await this.writeValue(Register.Data, (short)c);
                    totalChars++;
                    lineChars++;
                    await this.wait(TimeSpan.FromMilliseconds(1));
                }

                Debug.WriteLine("HD44780GpioDriver: Message write complete");
            }
            finally
            {
                lcdLock.Release();
            }
        }

        private void initializePins(int enablePin, int registerSelectPin, int[] dataPins)
        {
            this.registerSelectPin = this.gpio.OpenPin(registerSelectPin);
            this.registerSelectPin.SetDriveMode(GpioPinDriveMode.Output);

            this.enablePin = this.gpio.OpenPin(enablePin);
            this.enablePin.SetDriveMode(GpioPinDriveMode.Output);

            this.dataPins = new GpioPin[dataPins.Count()];
            for (int i = 0; i < dataPins.Count(); i++)
            {
                this.dataPins[i] = this.gpio.OpenPin(dataPins[i]);
                this.dataPins[i].SetDriveMode(GpioPinDriveMode.Output);
            }
        }

        public async Task initializeChip()
        {
            switch(this.bitMode)
            {
                case BitMode.Four:
                    //The initialization uses the instructions and wait times according to the document:http://www.taoli.ece.ufl.edu/teaching/4744/labs/lab7/LCD_V1.pdf
                    //PowerOn
                    this.writeBits(Register.Instruction, 0x03);
                    await this.wait(TimeSpan.FromMilliseconds(15));
                    this.writeBits(Register.Instruction, 0x03);
                    await this.wait(TimeSpan.FromMilliseconds(4.1));
                    this.writeBits(Register.Instruction, 0x03);
                    await this.wait(TimeSpan.FromMilliseconds(4.1));
                    this.writeBits(Register.Instruction, 0x02);
                    await this.wait(TimeSpan.FromMilliseconds(4.1));
                    //Set number of lines and font
                    this.writeBits(Register.Instruction, 0x02);
                    await this.wait(TimeSpan.FromMilliseconds(0.4));
                    this.writeBits(Register.Instruction, 0x08);
                    await this.wait(TimeSpan.FromMilliseconds(0.4));
                    //Display on, cursor off
                    this.writeBits(Register.Instruction, 0x00);
                    await this.wait(TimeSpan.FromMilliseconds(0.4));
                    this.writeBits(Register.Instruction, 0x0C);
                    await this.wait(TimeSpan.FromMilliseconds(0.4));
                    //Inc cursor to the right when writing
                    this.writeBits(Register.Instruction, 0x00);
                    await this.wait(TimeSpan.FromMilliseconds(0.4));
                    this.writeBits(Register.Instruction, 0x06);
                    await this.wait(TimeSpan.FromMilliseconds(0.4));
                    break;
                case BitMode.Eight:
                    throw new NotImplementedException();
            }
        }
        private async Task writeValue(Register register, short value)
        {
            switch(this.bitMode)
            {
                case BitMode.Four:
                    byte value4bits = (byte)value;
                    value4bits >>= 4;
                    this.writeBits(register, value4bits);
                    value4bits = (byte)value;
                    value4bits &= 0x0F;
                    this.writeBits(register, value4bits);
                    break;
                case BitMode.Eight:
                    this.writeBits(register, value);
                    break;
            }

            await this.wait(TimeSpan.FromMilliseconds(0.04));
        }

        private void writeBits(Register register, short bits)
        {
            switch (register)
            {
                case Register.Data:
                    this.enablePin.Write(GpioPinValue.High);
                    this.registerSelectPin.Write(GpioPinValue.High);
                    break;
                case Register.Instruction:
                    this.enablePin.Write(GpioPinValue.High);
                    this.registerSelectPin.Write(GpioPinValue.Low);
                    break;
            }

            int numberOfBits = 0;

            switch(this.bitMode)
            {
                case BitMode.Four:
                    numberOfBits = 4;
                    break;
                case BitMode.Eight:
                    numberOfBits = 8;
                    break;
            }

            char[] charArray = Convert.ToString(bits, 2).PadLeft(numberOfBits, '0').ToCharArray();
            Array.Reverse(charArray);

            for (int i = 0; i < numberOfBits; i++)
            {
                char v = charArray[i];
                if (v == '1')
                {
                    // write GpioPinValue.High to pin
                    this.dataPins[i].Write(GpioPinValue.High);
                }
                else
                {
                    // write GpioPinValue.Low to pin
                    this.dataPins[i].Write(GpioPinValue.Low);
                }
            }

            switch (register)
            {
                case Register.Data:
                    this.enablePin.Write(GpioPinValue.Low);
                    this.registerSelectPin.Write(GpioPinValue.High);
                    break;
                case Register.Instruction:
                    this.enablePin.Write(GpioPinValue.Low);
                    this.registerSelectPin.Write(GpioPinValue.Low);
                    break;
            }
        }

        private async Task wait(TimeSpan duration)
        {
            await Task.Delay(duration);
        }
    }
}