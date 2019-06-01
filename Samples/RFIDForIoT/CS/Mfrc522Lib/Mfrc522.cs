/*
 This code is based on the library written by github user mlowijs.
 The original library can be found here https://github.com/mlowijs/mfrc522-netmf
 */
using Mfrc522Lib.Constants;
using System.Threading.Tasks;
using Windows.Devices.Spi;
using Windows.Devices.Gpio;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using System;
using Windows.Devices.Enumeration;
using System.Collections.Generic;

namespace Mfrc522Lib
{
    public class Mfrc522
    {
        public SpiDevice _spi { get; private set; }
        public GpioController IoController { get; private set; }
        public GpioPin _resetPowerDown { get; private set; }
        public int pincount;

        private const string SPI_CONTROLLER_NAME = "SPI0";
        private const Int32 SPI_CHIP_SELECT_LINE = 0;
        private const Int32 RESET_PIN = 25;

        public async Task InitIO()
        {
            try
            {
                IoController = GpioController.GetDefault();
                pincount = IoController.PinCount;
                _resetPowerDown = IoController.OpenPin(RESET_PIN);
                _resetPowerDown.Write(GpioPinValue.High);
                _resetPowerDown.SetDriveMode(GpioPinDriveMode.Output);
            }
            /* If initialization fails, throw an exception */
            catch (Exception ex)
            {
                throw new Exception("GPIO initialization failed", ex);
            }

            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 1000000;
                settings.Mode = SpiMode.Mode0;

                String spiDeviceSelector = SpiDevice.GetDeviceSelector();
                IReadOnlyList<DeviceInformation> devices = await DeviceInformation.FindAllAsync(spiDeviceSelector);

                _spi = await SpiDevice.FromIdAsync(devices[0].Id, settings);

            }
            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }


            await ResetAsync();
        }


        public async Task ResetAsync()
        {
            _resetPowerDown.Write(GpioPinValue.Low);
            await Task.Delay(TimeSpan.FromSeconds(1));

            _resetPowerDown.Write(GpioPinValue.High);
            await Task.Delay(TimeSpan.FromSeconds(1));

            // Force 100% ASK modulation
            WriteRegister(Registers.TxAsk, 0x40);

            // Set CRC to 0x6363
            WriteRegister(Registers.Mode, 0x3D);

            // Enable antenna
            SetRegisterBits(Registers.TxControl, 0x03);
        }


        public async Task<bool> IsTagPresent()
        {
            // Enable short frames
            WriteRegister(Registers.BitFraming, 0x07);

            // Transceive the Request command to the tag
            await Transceive(false, PiccCommands.Request);

            // Disable short frames
            WriteRegister(Registers.BitFraming, 0x00);

            // Check if we found a card
            return GetFifoLevel() == 2 && ReadFromFifoShort() == PiccResponses.AnswerToRequest;
        }

        public async Task<Uid> ReadUid()
        {
            // Run the anti-collision loop on the card
            await Transceive(false, PiccCommands.Anticollision_1, PiccCommands.Anticollision_2);

            // Return tag UID from FIFO
            return new Uid(ReadFromFifo(5));
        }

        public async Task HaltTag()
        {
            // Transceive the Halt command to the tag
            await Transceive(false, PiccCommands.Halt_1, PiccCommands.Halt_2);
        }

        public async Task<bool> SelectTag(Uid uid)
        {
            // Send Select command to tag
            var data = new byte[7];
            data[0] = PiccCommands.Select_1;
            data[1] = PiccCommands.Select_2;
            uid.FullUid.CopyTo(data, 2);

            await Transceive(true, data);

            return GetFifoLevel() == 1 && ReadFromFifo() == PiccResponses.SelectAcknowledge;
        }

        public async Task<byte[]> ReadBlock(byte blockNumber, Uid uid, byte[] keyA = null, byte[] keyB = null)
        {
            if (keyA != null)
               await MifareAuthenticate(PiccCommands.AuthenticateKeyA, blockNumber, uid, keyA);
            else if (keyB != null)
                await MifareAuthenticate(PiccCommands.AuthenticateKeyB, blockNumber, uid, keyB);
            else
                return null;

            // Read block
            await Transceive(true, PiccCommands.Read, blockNumber);

            return ReadFromFifo(16);
        }

        public async Task<bool> WriteBlock(byte blockNumber, Uid uid, byte[] data, byte[] keyA = null, byte[] keyB = null)
        {
            if (keyA != null)
               await MifareAuthenticate(PiccCommands.AuthenticateKeyA, blockNumber, uid, keyA);
            else if (keyB != null)
               await MifareAuthenticate(PiccCommands.AuthenticateKeyB, blockNumber, uid, keyB);
            else
                return false;

            // Write block
            await Transceive(true, PiccCommands.Write, blockNumber);

            if (ReadFromFifo() != PiccResponses.Acknowledge)
                return false;

            // Make sure we write only 16 bytes
            var buffer = new byte[16];
            data.CopyTo(buffer, 0);

            await Transceive(true, buffer);

            return ReadFromFifo() == PiccResponses.Acknowledge;
        }


        protected async Task MifareAuthenticate(byte command, byte blockNumber, Uid uid, byte[] key)
        {
            // Put reader in Idle mode
            WriteRegister(Registers.Command, PcdCommands.Idle);

            // Clear the FIFO
            SetRegisterBits(Registers.FifoLevel, 0x80);

            // Create Authentication packet
            var data = new byte[12];
            data[0] = command;
            data[1] = (byte)(blockNumber & 0xFF);
            key.CopyTo(data, 2);
            uid.Bytes.CopyTo(data, 8);

            WriteToFifo(data);

            // Put reader in MfAuthent mode
            WriteRegister(Registers.Command, PcdCommands.MifareAuthenticate);

            // Wait for (a generous) 25 ms
            await Task.Delay(25);
        }

        protected async Task Transceive(bool enableCrc, params byte[] data)
        {
            if (enableCrc)
            {
                // Enable CRC
                SetRegisterBits(Registers.TxMode, 0x80);
                SetRegisterBits(Registers.RxMode, 0x80);
            }

            // Put reader in Idle mode
            WriteRegister(Registers.Command, PcdCommands.Idle);

            // Clear the FIFO
            SetRegisterBits(Registers.FifoLevel, 0x80);

            // Write the data to the FIFO
            WriteToFifo(data);

            // Put reader in Transceive mode and start sending
            WriteRegister(Registers.Command, PcdCommands.Transceive);
            SetRegisterBits(Registers.BitFraming, 0x80);

            // Wait for (a generous) 25 ms
            await Task.Delay(25);

            // Stop sending
            ClearRegisterBits(Registers.BitFraming, 0x80);

            if (enableCrc)
            {
                // Disable CRC
                ClearRegisterBits(Registers.TxMode, 0x80);
                ClearRegisterBits(Registers.RxMode, 0x80);
            }
        }


        protected byte[] ReadFromFifo(int length)
        {
            var buffer = new byte[length];

            for (int i = 0; i < length; i++)
                buffer[i] = ReadRegister(Registers.FifoData);

            return buffer;
        }

        protected byte ReadFromFifo()
        {
            return ReadFromFifo(1)[0];
        }

        protected void WriteToFifo(params byte[] values)
        {
            foreach (var b in values)
                WriteRegister(Registers.FifoData, b);
        }

        protected int GetFifoLevel()
        {
            return ReadRegister(Registers.FifoLevel);
        }


        protected byte ReadRegister(byte register)
        {
            register <<= 1;
            register |= 0x80;

            var writeBuffer = new byte[] { register, 0x00 };

            return TransferSpi(writeBuffer)[1];
        }

        protected ushort ReadFromFifoShort()
        {
            var low = ReadRegister(Registers.FifoData);
            var high = (ushort)(ReadRegister(Registers.FifoData) << 8);

            return (ushort)(high | low);
        }

        protected void WriteRegister(byte register, byte value)
        {
            register <<= 1;

            var writeBuffer = new byte[] { register, value };

            TransferSpi(writeBuffer);
        }

        protected void SetRegisterBits(byte register, byte bits)
        {
            var currentValue = ReadRegister(register);
            WriteRegister(register, (byte)(currentValue | bits));
        }

        protected void ClearRegisterBits(byte register, byte bits)
        {
            var currentValue = ReadRegister(register);
            WriteRegister(register, (byte)(currentValue & ~bits));
        }


        private byte[] TransferSpi(byte[] writeBuffer)
        {
            var readBuffer = new byte[writeBuffer.Length];
            _spi.TransferFullDuplex(writeBuffer, readBuffer);
            return readBuffer;
        }

    }
}
