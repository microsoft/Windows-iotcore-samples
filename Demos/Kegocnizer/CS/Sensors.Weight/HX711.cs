using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Sensors.Weight
{
    //24-Bit Analog-to-Digital Converter (ADC) for Weigh Scales
    public class HX711
    {
        #region setup

        //PD_SCK
        private GpioPin PowerDownAndSerialClockInput;

        //DOUT
        private GpioPin SerialDataOutput;

        public HX711(GpioPin powerDownAndSerialClockInput, GpioPin serialDataOutput)
        {
            PowerDownAndSerialClockInput = powerDownAndSerialClockInput;
            powerDownAndSerialClockInput.SetDriveMode(GpioPinDriveMode.Output);

            SerialDataOutput = serialDataOutput;
            SerialDataOutput.SetDriveMode(GpioPinDriveMode.Input);
        }

        #endregion

        #region data retrieval

        //When output data is not ready for retrieval,
        //digital output pin DOUT is high.
        private bool IsReady()
        {
            return SerialDataOutput.Read() == GpioPinValue.Low;
        }
        //By applying 25~27 positive clock pulses at the
        //PD_SCK pin, data is shifted out from the DOUT
        //output pin.Each PD_SCK pulse shifts out one bit,
        //starting with the MSB bit first, until all 24 bits are
        //shifted out.
        public int Read()
        {
            while (!IsReady())
            {

            }
            string binaryData = "";
            for (int pulses = 0; pulses < 25 + (int)InputAndGainSelection; pulses++)
            {
                PowerDownAndSerialClockInput.Write(GpioPinValue.High);
                PowerDownAndSerialClockInput.Write(GpioPinValue.Low);
                if (pulses < 25)
                    binaryData += (int)SerialDataOutput.Read();
            }
            return Convert.ToInt32(binaryData, 2);
        }

        #endregion

        #region input selection/ gain selection

        private InputAndGainOption _InputAndGainSelection = InputAndGainOption.A128;

        public InputAndGainOption InputAndGainSelection
        {
            get
            {
                return _InputAndGainSelection;
            }
            set
            {
                _InputAndGainSelection = value;
                Read();
            }
        }

        #endregion

        #region power

        //When PD_SCK pin changes from low to high
        //and stays at high for longer than 60µs, HX711
        //enters power down mode
        public void PowerDown()
        {
            PowerDownAndSerialClockInput.Write(GpioPinValue.Low);
            PowerDownAndSerialClockInput.Write(GpioPinValue.High);
            //wait 60 microseconds
        }

        //When PD_SCK returns to low,
        //chip will reset and enter normal operation mode
        public void PowerOn()
        {
            PowerDownAndSerialClockInput.Write(GpioPinValue.Low);
            _InputAndGainSelection = InputAndGainOption.A128;
        }
        //After a reset or power-down event, input
        //selection is default to Channel A with a gain of 128. 

        #endregion

    }
}
