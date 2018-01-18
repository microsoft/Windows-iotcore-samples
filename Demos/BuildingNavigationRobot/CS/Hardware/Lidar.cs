// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace BuildingNavigationRobot
{
    // A delegate type for hooking up change notifications.
    public delegate void ScanEventHandler(object sender, ScanEventArgs e);
    public delegate void ErrorEventHandler(object sender, EventArgs e);

    public class ScanEventArgs : EventArgs
    {
        public Hit[] Readings;
        public long Timestamp;
    }
    
    /// <summary>
    /// Struct for storing and organizing the details of every data point (or "hit") returned by the Lidar.
    /// </summary>
    public struct Hit
    {
        public bool Error;
        public bool NewScan;

        // Quality of the reflected laser pulse strength
        public int Quality;

        // Measurement heading angle related to LIDAR's heading in degrees
        public int Angle;

        // Object distance in mm, set to 0 when measurement is invalid
        public int Distance;

        public Point CartesianPoint
        {
            get
            {
                return Lidar.ConvertToCartesian(Angle, Distance);
            }
        }

        public override string ToString()
        {
            return $"Angle: {Angle}, Distance: {Distance}, Quality: {Quality}";
        }
    }

    /// <summary>
    /// This class contains all the methods needed to interface with the RPLidar A1
    /// </summary>
    public class Lidar
    {
        public const string DEFAULT_DEVICE_ID = "\\\\?\\USB#VID_10C4&PID_EA60#0001#{86e0d1e0-8089-11d0-9ce4-08003e301f73}";

        // The serial port object to communicate with the lidar
        private SerialDevice serialPort;

        // Data reader & writer objects to read/write from/to the lidar
        private DataReader dataReader;
        private DataWriter dataWriter;

        // Class wide cancellation token.
        private CancellationTokenSource cts;
        private CancellationToken token;
        
        private List<Hit> readings;
        
        public bool IsEnabled { get; private set; } = false;

        public Lidar()
        {
            cts = new CancellationTokenSource();
            token = cts.Token;
        }

        /// <summary>
        /// Cancel the internal token.
        /// </summary>
        /// <returns></returns>
        public bool CancelToken()
        {
            CancelRead();
            if (cts != null)
            {
                cts.Cancel();
                return true;
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        /// Initialize the serial port for the LIDAR
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task InitializeAsync(string deviceId = DEFAULT_DEVICE_ID)
        {
            serialPort?.Dispose();

            serialPort = await SerialDevice.FromIdAsync(deviceId);
            if (serialPort == null)
            {
                throw new Exception("Could not bind the Lidar serial port.");
            }

            ConfigSerialPort();
            await Reset();

            IsEnabled = true;
        }

        public void Close()
        {
            Debug.WriteLine("Closing Lidar...");

            CancelToken();
            if (serialPort != null)
            {
                serialPort.Dispose();
            }

            IsEnabled = false;
        }

        /// <summary>
        /// This method configures the serial port to hard-coded values.
        /// </summary>
        private void ConfigSerialPort()
        {
            // Configure serial settings
            serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.BaudRate = 115200;
            serialPort.Parity = SerialParity.None;
            serialPort.StopBits = SerialStopBitCount.One;
            serialPort.DataBits = 8;
            serialPort.Handshake = SerialHandshake.None;
            dataReader = new DataReader(serialPort.InputStream);
            dataWriter = new DataWriter(serialPort.OutputStream);
            Debug.WriteLine("Serial port configured.");
        }

        public async Task Stop()
        {
            CancelRead();
            await WriteOut(new byte[] { 0xA5, 0x25 });
        }

        /// <summary>
        /// Send message to lidar to reset.
        /// </summary>
        public async Task Reset()
        {
            CancelRead();
            await WriteOut(new byte[] { 0xA5, 0x40 });
            await Task.Delay(1000);
        }

        /// <summary>
        /// Starts scanning
        /// </summary>
        public void StartScan()
        {
            RestartRead();
            ReadAsync(readingToken.Token);
        }

        /// <summary>
        /// Converts angle and distance to Cartesian coordinates
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Point ConvertToCartesian(double angle, double distance)
        {
            Point point = new Point();
            double radians = angle * Math.PI / 180.0;

            point.X = Math.Sin(radians) * distance;
            point.Y = Math.Cos(radians) * distance;

            return point;
        }

        /// <summary>
        /// Converts point to polar coordinates.  Item1 is angle, Item2 is distance.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static PolarCoordinate ConvertToPolar(Point point)
        {
            double distance = Math.Sqrt((point.X * point.X) + (point.Y * point.Y));
            double angle = Math.Asin(point.X / distance);
            
            // Convert to degrees
            angle = angle * 180.0 / Math.PI;
            if(angle < 0)
            {
                angle += 360;
            }

            return new PolarCoordinate()
            {
                Angle = angle,
                Distance = distance
            };
        }


        /// <summary>
        /// Cancellation token to stop reading from the serial port.
        /// </summary>
        CancellationTokenSource readingToken;

        /// <summary>
        /// Stop any read process.
        /// </summary>
        private void CancelRead()
        {
            if (readingToken != null)
            {
                readingToken.Cancel();
            }
        }

        /// <summary>
        /// Cancel and dispose the previous read action (via the token) and start a new, fresh
        /// and uncancelled token source.
        /// </summary>
        /// <returns>The token associated with the new CancellationTokenSource</returns>
        private void RestartRead()
        {
            CancelRead();
            if (readingToken != null)
            {
                readingToken.Dispose();
            }
            readingToken = new CancellationTokenSource();
        }

        /// <summary>
        /// This method launched the infinite process to read from the lidar.
        /// </summary>
        /// <param name="token">Cancellation token</param>
        public void ReadAsync(CancellationToken localToken)
        {
            Debug.WriteLine("Launching read process.");
            ThreadPool.RunAsync((s) => { InfRead(localToken); });
            Debug.WriteLine("Launched!");
        }

        /// <summary>
        /// Looping read operation
        /// </summary>
        /// <param name="readToken">Token to cancel this read operation.</param>
        private async void InfRead(CancellationToken readToken)
        {
            await ClearReadBuffer();
            await WriteOut(new byte[] { 0xA5, 0x20 });
            byte[] descriptor = await ReadData(7, token);
            if (descriptor[0] != 0xA5 || descriptor[1] != 0x5A)
            {
                await Stop();
                Debug.WriteLine("Setup ERROR!");
                return;
            }
            while (true)
            {
                if (readingToken.IsCancellationRequested) break;
                await Read();
            }
        }

        /// <summary>
        /// Reads the data from the LIDAR, formats it and triggers an event
        /// </summary>
        /// <returns></returns>
        private async Task Read()
        {
            if (readings == null)
            {
                readings = new List<Hit>();
            }

            try
            {
                byte[] readout = await ReadData(5, readingToken.Token);
                Hit currentReading = FormatData(readout);

                // Trigger event and clear current readings on new scan
                if (currentReading.NewScan)
                {
                    OnScanCompleted(readings.ToArray());
                    readings.Clear();
                }

                // Only add reading if no error
                if (!currentReading.Error)
                {
                    readings.Add(currentReading);
                }
                else
                {
                    Debug.WriteLine("Read Error -> " + currentReading.ToString());
                    await ClearReadBuffer();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }

        #region Events

        /// <summary>
        /// Scan Complete event, triggered whenever the Lidar completes a full 360-degree sweep.
        /// </summary>
        public event ScanEventHandler ScanComplete;


        /// <summary>
        /// Error detected event, triggered when software detects problems with the Lidar's readings.
        /// </summary>
        public event ErrorEventHandler ErrorDetected;

        /// <summary>
        /// Caller method for the ScanComplete event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnScanCompleted(Hit[] readings)
        {
            ScanComplete?.Invoke(this, new ScanEventArgs() { Readings = readings, Timestamp = DateTime.Now.Ticks });
        }

        /// <summary>
        /// Caller method for the ErrorDetected event. 
        /// </summary>
        protected virtual void OnErrorDetected()
        {
            ErrorDetected?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Helper Functions
        
        /// <summary>
        /// Takes in a set of 5 bytes and interprets them as a reading from the LIDAR.
        /// </summary>
        /// <param name="readout"></param>
        /// <returns></returns>
        private Hit FormatData(byte[] readout)
        {
            Hit temp;

            temp.NewScan = (readout[0] & 1) == 1;
            bool notNewScan = mask(readout[0], 0b10);
            temp.Error = !(temp.NewScan ^ notNewScan);

            temp.Quality = readout[0] >> 2;

            readout[1] = (byte)(readout[1] & 0b11111110);
            temp.Angle = readout[2] << 7;
            temp.Angle += readout[1] >> 1;
            temp.Angle /= 64;

            temp.Distance = readout[4] << 8;
            temp.Distance += readout[3];
            temp.Distance /= 4;

            if (temp.Angle > 360 || temp.Angle < 0)
            {
                temp.Error = true;
            }

            return temp;
        }

        /// <summary>
        /// Apply a given binary mask to an imput.
        /// </summary>
        /// <param name="b">Input value</param>
        /// <param name="mask"></param>
        /// <returns></returns>
        private bool mask(byte b, int mask)
        {
            return (b & mask) != 0;
        }

        #endregion

        #region Serial Communication

        /// <summary>
        /// Writes to the serial port.
        /// </summary>
        /// <param name="outData">Byte array to write.</param>
        /// <returns></returns>
        private async Task WriteOut(byte[] outData)
        {
            Task<UInt32> storeAsyncTask;
            dataWriter.WriteBytes(outData);

            // Launch an async task to complete the write operation
            storeAsyncTask = dataWriter.StoreAsync().AsTask(token);

            try
            {
                UInt32 bytesWritten = await storeAsyncTask;                
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WRITING EXCEPTION!");
                Debug.WriteLine(ex.Message + ex.StackTrace);
            }
        }
        
        /// <summary>
        /// Reads from the serial port.
        /// </summary>
        /// <param name="ReadBufferLength">Amount of bytes to read.</param>
        /// <returns>Byte array with the information read from the serial port.</returns>
        private async Task<byte[]> ReadData(uint ReadBufferLength, CancellationToken readToken)
        {
            // If task cancellation was requested, comply
            token.ThrowIfCancellationRequested();

            Task<UInt32> loadAsyncTask;
            byte[] buffer = new byte[ReadBufferLength];
            loadAsyncTask = dataReader.LoadAsync(ReadBufferLength).AsTask(readToken);

            using (var childCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                UInt32 readBytes = await loadAsyncTask;

                if (readBytes == ReadBufferLength)
                {
                    dataReader.ReadBytes(buffer);
                }
            }

            return buffer;
        }

        /// <summary>
        /// This method will try to read all the bytes in the buffer to empty it.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        private async Task ClearReadBuffer()
        {
            int counter = 0;
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000)))
                {
                    while (true)
                    {
                        if (token.IsCancellationRequested) return;
                        if (readingToken != null && readingToken.IsCancellationRequested) return;
                        await dataReader.LoadAsync(1).AsTask(cts.Token);
                        dataReader.ReadByte();
                        counter++;
                    }
                }
            }
            catch
            {
                Debug.WriteLine("Expected clean buffer timeout.");
            }
            Debug.WriteLine("Cleared {0} bytes.", counter);
        }

        #endregion
    }
}
