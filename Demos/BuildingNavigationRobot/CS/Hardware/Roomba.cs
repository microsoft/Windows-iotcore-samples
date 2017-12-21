// Copyright (c) Microsoft. All rights reserved.

/*
 * 
 * iRobot(r) Create(r) controller class
 * It is highly recommended to read the iRobot(r) Create(r) 2 Open Interface Specification.
 * Available here: 
 * http://www.irobotweb.com/-/media/MainSite/PDFs/About/STEM/Create/iRobot_Roomba_600_Open_Interface_Spec.pdf?la=en
 * 
 */

using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace BuildingNavigationRobot
{
    // Reason robot has stopped
    public enum StopReason
    {
        Requested,
        Success,
        Collision,
        Cancellation
    }

    // Robot's state
    public enum RobotState
    {
        Moving,
        Stopped,
        Rotating
    }

    // Roomba mode
    public enum Mode
    {
        Off = 0,
        Passive = 1,
        Safe = 2,
        Full = 3
    }

    // A delegate type for hooking up change notifications.
    public delegate void StopEventHandler(object sender, RoombaStopArgs e);

    public class RoombaStopArgs : EventArgs
    {
        public StopReason StopOrigin { get; private set; }
        public RoombaStopArgs(StopReason success)
        {
            StopOrigin = success;
        }
    }

    /// <summary>
    /// This class contains all the methods needed to interface with the iRobot Create 2
    /// </summary>
    public class Roomba
    {
        public const string DEFAULT_DEVICE_ID = "\\\\?\\FTDIBUS#VID_0403+PID_6015+DA01NYTFA#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}";
        public bool IsEnabled { get; private set; } = false;

        public RobotState CurrentState = RobotState.Stopped;

        // The serial port object to communicate with the roomba
        private SerialDevice serialPort;

        // Data reader & writer objects to read/write from/to the roomba
        private DataReader dataReader;
        private DataWriter dataWriter;

        // Class wide cancellation token.
        private CancellationTokenSource cts;
        private CancellationToken token;

        // This flag is used for enabling/disabling the actual serial communication
        // within the infinite process.
        private bool syncActive = false;

        // Variables written to roomba
        // Mode
        private bool modeChanged = false;
        private byte realMode = 0;
        private byte newMode = 0;

        // Motors
        public int LeftWheelSpeed { get; private set; } = 0;
        public int RightWheelSpeed { get; private set; } = 0;

        // Reset flag
        private bool reset = false;

        // Variables read from Roomba
        // Wheel drop
        public bool WheelDropLeft { get; private set; } = false;
        public bool WheelDropRight { get; private set; } = false;

        // Bumpers
        public bool BumperLeft { get; private set; } = false;
        public bool BumperRight { get; private set; } = false;

        // Light Bumpers
        public bool LightBumperRight { get; private set; } = false;
        public bool LightBumperFrontRight { get; private set; } = false;
        public bool LightBumperCenterRight { get; private set; } = false;
        public bool LightBumperCenterLeft { get; private set; } = false;
        public bool LightBumperFrontLeft { get; private set; } = false;
        public bool LightBumperLeft { get; private set; } = false;

        // Cliff
        public bool CliffLeft { get; private set; } = false;
        public bool CliffFrontLeft { get; private set; } = false;
        public bool CliffFrontRight { get; private set; } = false;
        public bool CliffRight { get; private set; } = false;

        public int CliffLeftSignal { get; private set; } = 0;
        public int CliffFrontLeftSignal { get; private set; } = 0;
        public int CliffFrontRightSignal { get; private set; } = 0;
        public int CliffRightSignal { get; private set; } = 0;

        // Virtual wall
        public bool VirtualWall = false;

        // Overcurrent sensors
        public bool LeftWheelOvercurrent { get; private set; } = false;
        public bool RightWheelOvercurrent { get; private set; } = false;
        public bool MainBrushOvercurrent { get; private set; } = false;
        public bool SideBrushOvercurrent { get; private set; } = false;

        // Dirt sensor
        public byte DirtLevel = 0;

        // Buttons
        public bool ClockButton { get; private set; } = false;
        public bool ScheduleButton { get; private set; } = false;
        public bool DayButton { get; private set; } = false;
        public bool HourButton { get; private set; } = false;
        public bool MinuteButton { get; private set; } = false;
        public bool DockButton { get; private set; } = false;
        public bool SpotButton { get; private set; } = false;
        public bool CleanButton { get; private set; } = false;

        // Electrical characteristics
        public byte ChargingState { get; private set; } = 0;
        public double Voltage { get; private set; } = 0;
        public int MilliAmps { get; private set; } = 0;
        public byte Temperature { get; private set; } = 0;
        public int BatteryCharge { get; private set; } = 0;
        public int BatteryCapacity { get; private set; } = 0;
        public int BatteryLeft { get; private set; } = 0;
        public bool HomeBaseCharging { get; private set; } = false;
        public bool InternalCharging { get; private set; } = false;

        // Movement & encoders
        public int Distance { get; private set; } = 0;
        public int Rotation { get; private set; } = 0;

        public ulong LeftEncoderCount { get; private set; } = 0;
        private int? lastLeftEncoderCount = null;
        public double LeftWheelDistance { get; private set; } = 0;

        public ulong RightEncoderCount { get; private set; } = 0;
        private int? lastRightEncoderCount = null;
        public double RightWheelDistance { get; private set; } = 0;

        // Constant values
        // Command to send for reading sensor data
        private byte[] readAllSensors = { 142, 100 };

        // Constant to multiply to get radiand from degrees.
        private const double Deg2Rad = Math.PI / 180.0;

        // Number of encoder counts per wheel rotation.
        private const double countsPerRotarion = 130000;

        // Wheel diameter in mm
        public const double WheelDiameter = 72d;

        /// <summary>
        /// Distance between wheel centers in mm
        /// </summary>
        public const double DriveTrainDiameter = 233d;

        public const double RoombaDiameter = 350d;
        
        public void PrintDebug()
        {
            Debug.WriteLine("ModeI: " + realMode);
            Debug.WriteLine("ModeN: " + newMode);
            Debug.WriteLine("WDL:   " + WheelDropLeft);
            Debug.WriteLine("WDR:   " + WheelDropRight);
            Debug.WriteLine("BumpL: " + BumperLeft);
            Debug.WriteLine("BumpR: " + BumperRight);
            Debug.WriteLine("LtBl:  " + LightBumperLeft);
            Debug.WriteLine("LtBlf: " + LightBumperFrontLeft);
            Debug.WriteLine("LtBlc: " + LightBumperCenterLeft);
            Debug.WriteLine("LtBrc: " + LightBumperCenterRight);
            Debug.WriteLine("LtBrf: " + LightBumperFrontRight);
            Debug.WriteLine("LtBr:  " + LightBumperRight);

            Debug.WriteLine("CliL:  " + CliffLeft);
            Debug.WriteLine("CliLf: " + CliffFrontLeft);
            Debug.WriteLine("CliRF: " + CliffFrontRight);
            Debug.WriteLine("CliR:  " + CliffRight);

            Debug.WriteLine("Volts: " + Voltage);
            Debug.WriteLine("BattS: " + BatteryCapacity);
            Debug.WriteLine("BattC: " + BatteryCharge);
            Debug.WriteLine("Batt%: " + BatteryLeft);

            Debug.WriteLine("Dist:  " + Distance);
            Debug.WriteLine("Rotat: " + Rotation);
            Debug.WriteLine("EncL:  " + LeftEncoderCount);
            Debug.WriteLine("EncR:  " + RightEncoderCount);
        }

        /// <summary>
        /// Cancel the internal token.
        /// </summary>
        /// <returns></returns>
        public bool CancelToken()
        {
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
        /// Constructor
        /// </summary>
        public Roomba()
        {
            cts = new CancellationTokenSource();
            token = cts.Token;
            SyncAsync(token);
        }

        /// <summary>
        /// Constructor.  Starts the Infinite sync process.
        /// </summary>
        /// <param name="_token">Cancelation token for the infinite sync process</param>
        public Roomba(CancellationToken _token)
        {
            token = _token;
            SyncAsync(token);
        }

        /// <summary>
        /// Manually set the cancellation token.
        /// </summary>
        /// <param name="_token">Cancellation token.</param>
        public void SetToken(CancellationToken _token)
        {
            token = _token;
        }

        #region Serial Port Configuration
                
        /// <summary>
        /// Initialize the serial port for the Roomba
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task InitializeAsync(string deviceId = DEFAULT_DEVICE_ID, int numRetries = 5)
        {
            serialPort = await SerialDevice.FromIdAsync(deviceId);

            if (serialPort == null)
            {
                throw new Exception("Could not bind the Roomba serial port.");
            }
            else
            {
                Debug.WriteLine("Connecting to Roomba...");

                //Set it up to roomba port specs
                ConfigSerialPort();

                // Get (or infer) the roomba's mode
                Debug.WriteLine("Retrieving Roomba mode.");
                realMode = await GetMode();

                Debug.WriteLine("Roomba mode: " + realMode);

                for (int i = 0; i < numRetries; i++)
                {
                    if (await InitRoomba())
                    {
                        //Start timer for sync operation if roomba init was successful
                        Debug.WriteLine("Starting Sync Operation...");
                        StartSync();

                        IsEnabled = true;
                        return;
                    }

                    Debug.WriteLine($"Failed to initialize Roomba, retrying ({i})...");
                    await Task.Delay(1000);
                }

                throw new Exception("Error syncing Roomba.");
            }
        }

        public void Close()
        {
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

        #endregion

        #region Virtual Roomba Layer
        
        // BASIC MODE OPERATIONS 

        public void SetMode(Mode mode)
        {
            newMode = (byte)mode;
            modeChanged = true;
        }        

        /// <summary>
        /// Resets & reboots roomba.
        /// </summary>
        public void Reset()
        {
            reset = true;
            realMode = 0;
            newMode = 0;
        }

        // SENSOR OPERATIONS

        /// <summary>
        /// Reset the encoder counters to zero.
        /// </summary>
        public void ResetEncoders()
        {
            RightEncoderCount = 0;
            LeftEncoderCount = 0;

            RightWheelDistance = 0;
            LeftWheelDistance = 0;
        }

        public double AverageDistanceTraveled
        {
            get
            {
                return (LeftWheelDistance / 2d) + (RightWheelDistance / 2d);
            }
        }

        // MOVEMENT OPERATIONS
        // Everything here should call cancelMove()

        /// <summary>
        /// Stop the motors.
        /// </summary>
        public void Halt(StopReason stopType = StopReason.Requested)
        {
            CancelMove();
            SetWheels();
            OnMovementStop(stopType);
        }

        /// <summary>
        /// Set the speed of the wheels in mm/s.
        /// From -500 to 500.
        /// </summary>
        /// <param name="rightSpeed">Right wheel speed.</param>
        /// <param name="leftSpeed">Left wheel speed.</param>
        public void SetSpeed(int rightSpeed, int leftSpeed)
        {
            CancelMove();
            SetWheels(rightSpeed, leftSpeed);
        }

        // Cancellable operations
        // These operations take time and run in a background task, and thus should be
        //  cancellable either via the class-wide token or the local movement token, 
        //  used to stop and override these actions.

        /// <summary>
        /// Move roomba a given distance at a given speed in a straight line.
        /// </summary>
        /// <param name="speed">The movement speed.</param>
        /// <param name="distMM">The distance to travel.</param>
        public Task MoveDistance(int speed, int distMM)
        {
            CancelMove();
            long ms = (Math.Abs(distMM) * 1000) / speed;
            if (distMM < 0)
            {
                speed = -speed;
            }
            currentMove = ThreadPool.RunAsync((s) => {
                MoveTimeInternal(speed, speed, ms);
            }) as Task;
            return currentMove;
        }

        public async Task MoveDistanceEncoders(int speed, int distMM)
        {
            CancelMove();
            await ThreadPool.RunAsync((s) => {
                MoveEncodersInternal(speed, distMM);
            });
        }

        /// <summary>
        /// Rotate Roomba a given amount of degrees at a given speed.
        /// </summary>
        /// <param name="speed">Rotation speed.</param>
        /// <param name="degrees">Degrees to rotate.</param>
        public async Task Rotate(int speed, double degrees)
        {
            CancelMove();
            if (Math.Abs(degrees) < 10) return;
            long ms = (long)(1000 * ((Math.Abs(degrees) * Deg2Rad * 116.5d) / ((double)speed)));
            Debug.WriteLine("Moving for: {0}ms", ms);
            int left = (degrees < 0) ? -speed : speed;
            await ThreadPool.RunAsync((s) => {
                MoveTimeInternal(-left, left, ms);
            });
        }

        /// <summary>
        /// Rotate the roomba using encoder data for precise rotation.
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public async Task RotateEncoders(int speed, double degrees)
        {
            await ThreadPool.RunAsync((s) => {
                RotateEncodersInternal(speed, degrees);
            });
        }

        /// <summary>
        /// Move Indefinetley stopping only by external input. (i.e. crash avoidance)
        /// </summary>
        /// <param name="speed"></param>
        public async Task Move(int speed)
        {
            CancelMove();
            await ThreadPool.RunAsync((s) => {
                MoveIndefInternal(speed, speed);
            });
        }

        // Private functions detailing the underlying behaviour of movement:

        /// <summary>
        /// This private method modifies the variables that are sent to the roomba.
        /// Also sets the <code>motorsChanged</code> flag to <code>true</code> so the value
        /// gets sent in the next sync operation.
        /// </summary>
        /// <param name="rightSpeed"></param>
        /// <param name="leftSpeed"></param>
        private void SetWheels(int rightSpeed = 0, int leftSpeed = 0)
        {
            //Debug.WriteLine("SetWheels: {0}  ===  {1}", leftSpeed, rightSpeed);
            RightWheelSpeed = rightSpeed;
            LeftWheelSpeed = leftSpeed;
        }

        // Movement operations cancellation token
        CancellationTokenSource moveCTS;
        Task currentMove;

        /// <summary>
        /// Mark the currently active CancellationTokenSource as cancelled
        /// </summary>
        public void CancelMove()
        {
            CurrentState = RobotState.Stopped;
            if (moveCTS != null)
            {
                moveCTS.Cancel();
            }
        }

        /// <summary>
        /// Cancel and dispose the previous motion action (via the token) and start a new, fresh
        /// and uncancelled token source.
        /// </summary>
        /// <returns>THe token associated with the new CancellationTokenSource</returns>
        private CancellationToken restartMove()
        {
            CancelMove();
            if (moveCTS != null)
            {
                moveCTS.Dispose();
            }
            moveCTS = new CancellationTokenSource();
            return moveCTS.Token;
        }

        /// <summary>
        /// Move the Motors for a given amount of time then break.
        /// </summary>
        /// <param name="rightSpeed">Speed of the right wheel</param>
        /// <param name="leftSpeed">Speed of the left wheel</param>
        /// <param name="ms">Duration in miliseconds</param>
        private void MoveTimeInternal(int rightSpeed, int leftSpeed, long ms)
        {
            CancellationToken moveToken = restartMove();
            Stopwatch sw = new Stopwatch();
            ResetEncoders();
            CurrentState = RobotState.Moving;
            SetWheels(rightSpeed, leftSpeed);
            sw.Start();
            while (sw.ElapsedMilliseconds < ms)
            {
                // action overriden by another move command
                if (moveToken.IsCancellationRequested) return;

                //Action ends
                if (token.IsCancellationRequested) break;
                if (BumperLeft || BumperRight) break;

                if (rightSpeed == leftSpeed)
                {
                    // Straight line course correction should only run when a straight line is actually desired.
                    CourseCorrect(rightSpeed, leftSpeed);
                }
            }
            Debug.WriteLineIf(BumperLeft || BumperRight, "Bumper finish!");
            Debug.WriteLine("Exited at: {0}", sw.ElapsedMilliseconds);
            SetWheels();
            OnMovementStop();
        }

        /// <summary>
        /// Move the motors without time constraints. Only external input will stop this.
        /// This includes a command being sent, or avoiding a crash.
        /// </summary>
        /// <param name="rightSpeed"></param>
        /// <param name="leftSpeed"></param>
        private void MoveIndefInternal(int rightSpeed, int leftSpeed)
        {
            CancellationToken moveToken = restartMove();
            Stopwatch sw = new Stopwatch();
            ResetEncoders();
            CurrentState = RobotState.Moving;
            SetWheels(rightSpeed, leftSpeed);
            sw.Start();
            while (true)
            {
                // action overriden by another move command
                if (moveToken.IsCancellationRequested) return;

                //Action ends
                if (token.IsCancellationRequested) break;
                if (BumperLeft || BumperRight) break;

                if (rightSpeed == leftSpeed)
                {
                    // Straight line course correction should only run when a straight line is actually desired.
                    CourseCorrect(rightSpeed, leftSpeed);
                }
            }
            sw.Stop();
            Debug.WriteLine("Exited at: {0}", sw.ElapsedMilliseconds);
            Debug.WriteLineIf(BumperLeft || BumperRight, "Bumper finish!");
            SetWheels();
            OnMovementStop();
        }

        /// <summary>
        /// Move the Motors for a given amount of time then break.
        /// </summary>
        /// <param name="rightSpeed">Speed of the right wheel</param>
        /// <param name="leftSpeed">Speed of the left wheel</param>
        /// <param name="ms">Duration in miliseconds</param>
        private void MoveEncodersInternal(int speed, long mm)
        {
            CancellationToken moveToken = restartMove();
            Stopwatch sw = new Stopwatch();
            ResetEncoders();
            CurrentState = RobotState.Moving;
            SetWheels(speed, speed);
            sw.Start();
            StopReason reason = StopReason.Success;

            while (RightWheelDistance < mm && LeftWheelDistance < mm)
            {

                // action overriden by another move command
                if (moveToken.IsCancellationRequested) return;

                //Action ends
                if (token.IsCancellationRequested)
                {
                    reason = StopReason.Cancellation;
                    break;
                }
                if (BumperLeft || BumperRight)
                {
                    reason = StopReason.Collision;
                    break;
                }

                double avgDist = (LeftWheelDistance + RightWheelDistance) / 2d;
                
                double stopCoefficient = (1 - Math.Exp(0.008 * (avgDist - mm - 10)));

                // Gradually reduce the motors' speed as the roomba approaches it's target
                int adjustedSpeed = (int)((double)speed * stopCoefficient);

                CourseCorrect(adjustedSpeed, adjustedSpeed);
            }
            Debug.WriteLineIf(BumperLeft || BumperRight, "Bumper finish!");
            Debug.WriteLine("Exited at: {0}", sw.ElapsedMilliseconds);
            SetWheels();
            OnMovementStop(reason);
        }

        /// <summary>
        /// This method utilizes encoders to make precise turns.
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="degrees"></param>
        private void RotateEncodersInternal(int speed, double degrees)
        {
            bool turnRight = degrees > 0;
            double rotDist = Math.Abs(degrees) * Deg2Rad * DriveTrainDiameter * 0.5d * 0.92;
            CancellationToken moveToken = restartMove();
            Stopwatch sw = new Stopwatch();
            ResetEncoders();
            CurrentState = RobotState.Rotating;
            speed = turnRight ? speed : -speed;
            SetWheels(-speed, speed);
            sw.Start();
            while (true)
            {

                if (turnRight)
                {
                    // Stop by encoder
                    if (Math.Abs(LeftWheelDistance) >= rotDist) break;
                }
                else
                {
                    // Stop by encoder
                    if (Math.Abs(RightWheelDistance) >= rotDist) break;
                }

                // action overriden by another move command
                if (moveToken.IsCancellationRequested) return;

                //Action ends
                if (token.IsCancellationRequested) break;
                if (BumperLeft || BumperRight) break;
            }
            Debug.WriteLine("Rotated: {0} out of {1}", Math.Abs(LeftWheelDistance), rotDist);
            Debug.WriteLineIf(BumperLeft || BumperRight, "Bumper finish!");
            Debug.WriteLine("Exited at: {0}", sw.ElapsedMilliseconds);
            SetWheels();
            OnMovementStop();

        }

        /// <summary>
        /// Method that handles course correction using encoders. 
        /// Should be called iteratively during movement.
        /// <note>Note: It has only been tested with forward movement.</note> 
        /// </summary>
        /// <param name="rightSpeed"></param>
        /// <param name="leftSpeed"></param>
        private void CourseCorrect(int rightSpeed, int leftSpeed)
        {
            // Possible TODO: this currently only supports straight lines. 
            // Functionality could be expanded to controlling movement in curves i.e.
            // when right and left speed are not the same value.
            double rads = (LeftWheelDistance - RightWheelDistance) / 233d;
            double ratio = 2 * Math.Abs(rads);
            if (rads < 0)
            {
                // Leaning left
                // Slow down right
                SetWheels(rightSpeed - (int)(ratio * (double)rightSpeed), leftSpeed);
            }
            else
            {
                // Leaning right
                // Slow down left
                SetWheels(rightSpeed, leftSpeed - (int)(ratio * (double)leftSpeed));
            }
        }

        /// <summary>
        /// An event that can be used to be notified whenever a movement routine is stopped or ends.
        /// </summary>
        public event StopEventHandler Stopped;

        /// <summary>
        /// Invokes the StopEvent. Should be called when a movement routine ends.
        /// </summary>
        protected virtual void OnMovementStop(StopReason type = StopReason.Success)
        {
            Stopped?.Invoke(this, new RoombaStopArgs(type));
        }

        #endregion
        
        #region Hardware Interface Layer

        /* 
         * These methods have the purpouse of orchestrating the communication with the roomba.
         */

        // Sync operation timing & launch.
        // These operations deal with the issues of timing the communications with the roomba.

        /// <summary>
        /// This method launched the infinite process to sync with the roomba.
        /// </summary>
        /// <param name="token">Cancellation token</param>
        public void SyncAsync(CancellationToken token)
        {
            Debug.WriteLine("Launching sync process.");
            ThreadPool.RunAsync((s) => { InfSync(token); });
            Debug.WriteLine("Launched!");
        }

        /// <summary>
        /// This method handles the timing of the infinite sync process with the roomba.
        /// </summary>
        /// <param name="syncToken"></param>
        /// <returns>Cancellation token</returns>
        private async Task InfSync(CancellationToken syncToken)
        {
            Stopwatch sw = new Stopwatch();
            Stopwatch test = new Stopwatch();
            while (true)
            {
                //Debug.WriteLine("i");
                if (syncToken.IsCancellationRequested) break;
                if (!syncActive) continue;
                test.Reset();
                test.Start();
                sw.Reset();
                sw.Start();
                try
                {
                    await SyncOp();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("SyncOp UNEXPECTED Exception.");
                }
                sw.Stop();

                //Ensure there's been at least N miliseconds in between syncs
                int period = 40;
                if (sw.ElapsedMilliseconds < period)
                {
                    try
                    {
                        await Task.Delay((int)(period - sw.ElapsedMilliseconds), syncToken);
                    }
                    catch (OperationCanceledException ex)
                    {
                        Debug.WriteLine("Delay timer canceled exception.");
                        return;
                    }
                }
                test.Stop();
            }
            Debug.WriteLine("Finished infinite process.");
        }

        /// <summary>
        /// Instruct the infinite process to stop contacting the roomba.
        /// </summary>
        private void StopSync()
        {
            syncActive = false;
        }

        /// <summary>
        /// Instruct the infinite process to start contacting the roomba.
        /// </summary>
        private void StartSync()
        {
            syncActive = true;
        }

        /// <summary>
        /// Method for startup purposes. Will retrieve the roomba's current mode.
        /// The program will try to obtaint is manually from the roomba, but if it doesn't
        /// reply in time, a timeout is triggered and it's assumed that the robot is in a state unsuitable
        /// for processing commands, that's either when it's in OFF mode or powered down.
        /// </summary>
        /// <returns>Awaitable. Int indicating Roomba mode.</returns>
        private async Task<byte> GetMode()
        {
            await ClearReadBuffer();
            try
            {
                // Request mode
                await WriteOut(new byte[] { 142, 35 });
                using (var timeoutCancellationTokenSource = new CancellationTokenSource())
                {
                    //Timeout
                    TimeSpan timeout = TimeSpan.FromMilliseconds(50);
                    Task<byte[]> task = ReadData(1);
                    var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                    if (completedTask == task)
                    {
                        // cancel the timeout task
                        timeoutCancellationTokenSource.Cancel();
                        byte[] reply = await task;

                        if (reply[0] >= 0 && reply[0] <= 3)
                        {
                            //The reply makes sense
                            return reply[0];
                        }
                        else
                        {
                            //The reply is gibberish and thus is treated as such, i.e.: ignored
                            Debug.WriteLine("Giberrish: {0}", reply[0]);
                            return 0;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Timed out!");
                        return 0;
                        //throw new TimeoutException("The operation has timed out.");
                    }
                }
            }
            catch (Exception ex)
            {
                //Timeout i.e. Roomba is in OFF mode
                Debug.WriteLine("No reply exception.");
                return 0;
            }
        }

        /// <summary>
        /// This method will attempt to get the roomba to full mode.
        /// </summary>
        /// <returns>Task<bool> stating whether initiarion was successful or not.</returns>
        private async Task<bool> InitRoomba()
        {
            if (realMode == 3)
            {
                Debug.WriteLine("Roomba Ready.");
                return true;
            }
            if (realMode == 0)
            {
                await WriteOut(new byte[] { 128 });
                await Task.Delay(50, token);
            }
            if (realMode != 3)
            {
                await WriteOut(new byte[] { 132 });
            }
            await Task.Delay(50, token);
            realMode = await GetMode();
            Debug.WriteLine("Roomba now in mode " + realMode);

            if (realMode != 3)
            {
                // TODO: figure out what to do if roomba won't respond to any comand, i.e. the roomba has no power
                realMode = 0;
                Debug.WriteLine("ROBOT HAS NO POWER!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Main method for sync-ing the virtual roomba with the real hardware through the serial interface
        /// This method should be called periodically.
        /// </summary>
        private async Task SyncOp()
        {
            if (token.IsCancellationRequested)
            {
                syncActive = false;
                return;
            }

            // Reset ?
            if (reset)
            {
                // Reset reset flag, stop timer, send command, wait 0.5s, restart timer, return
                reset = false;

                // Send reset signal and wait for roomba to reboot
                await WriteOut(new byte[] { 7 });
                await Task.Delay(500);
                newMode = 1;
                realMode = 0;
                await InitRoomba();
                return;
            }

            // If robot is in off mode, the only acceptable command is 128 to get to passive mode
            //  otherwise errors will occur.
            if (realMode == 0 && newMode != 1)
            {
                newMode = 0;
                return;
            }

            //Update Mode
            if (modeChanged)
            {
                Debug.WriteLine("Mode Changing to: {0} -> {1}", newMode, mode2Code(newMode));
                await WriteOut(new byte[] { mode2Code(newMode) });
                if (realMode == 0 && newMode == 1)
                {
                    await ClearReadBuffer();
                }
                await Task.Delay(50, token);
                realMode = await GetMode();
                Debug.WriteLine("Mode Changed to: {0}", realMode);
                modeChanged = false;
            }

            // If true, Roomba is in Off mode and won't respond to any of the following commands,
            //  therefore it's skipped.
            if (realMode == 0)
            {
                return;
            }

            await UpdateMotors(RightWheelSpeed, LeftWheelSpeed);
            //Debug.WriteLine("[{0}] : Motors updated", sw.ElapsedMilliseconds);

            // Start sensor input:
            await WriteOut(readAllSensors);
            //Debug.WriteLine("[{0}] : Data request sent", sw.ElapsedMilliseconds);
            // Read reply
            byte[] data;
            try
            {
                data = await ReadData(80);
            }
            catch (TaskCanceledException tce)
            {
                Debug.WriteLine("Read task cancelled exception.");
                StopSync();
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Read Data exception");
                //Debug.WriteLine(ex.Message + ex.StackTrace);
                return;
            }

            // Byte index, packet ID
            // 0, 7

            WheelDropLeft = mask(data[0], 0b1000);
            WheelDropRight = mask(data[0], 0b0100);
            BumperLeft = mask(data[0], 0b0010);
            BumperRight = mask(data[0], 0b0001);

            // 2, 9
            CliffLeft = mask(data[2], 1);

            // 3, 10
            CliffFrontLeft = data[3] == 1;

            // 4, 11
            CliffFrontRight = data[4] == 1;

            // 5, 12
            CliffRight = data[5] == 1;

            // 6, 13
            VirtualWall = data[6] == 1;

            // 7, 14
            LeftWheelOvercurrent = mask(data[7], 0b10000);
            RightWheelOvercurrent = mask(data[7], 0b01000);
            MainBrushOvercurrent = mask(data[7], 0b00100);
            SideBrushOvercurrent = mask(data[7], 0b00001);

            // 8, 15
            DirtLevel = data[8];

            // 11, 18
            BitArray buttons = new BitArray(new byte[] { data[11] });
            CleanButton = buttons[0];
            SpotButton = buttons[1];
            DockButton = buttons[2];
            MinuteButton = buttons[3];
            HourButton = buttons[4];
            DayButton = buttons[5];
            ScheduleButton = buttons[6];
            ClockButton = buttons[7];

            // 12-13, 19
            Distance = SignedWordToInt(data[12], data[13]);

            // 14-15, 20
            Rotation = SignedWordToInt(data[14], data[15]);

            // 16, 21
            ChargingState = data[16];

            // 17-18, 22
            Voltage = ((double)(UnsignedWordToInt(data[17], data[18]))) / 1000.0;

            // 19-20, 23
            MilliAmps = UnsignedWordToInt(data[17], data[18]);

            // 21, 24
            Temperature = data[21];

            // 22-23, 25
            BatteryCharge = UnsignedWordToInt(data[22], data[23]);

            // 24-25, 26
            BatteryCapacity = UnsignedWordToInt(data[24], data[25]);

            // Battery Left
            if (BatteryCapacity != 0)
                BatteryLeft = (BatteryCharge * 100) / BatteryCapacity;

            // 28-29, 28
            CliffLeftSignal = UnsignedWordToInt(data[28], data[29]);

            // 30-31, 29
            CliffFrontLeftSignal = UnsignedWordToInt(data[30], data[31]);

            // 32-33, 30
            CliffFrontRightSignal = UnsignedWordToInt(data[32], data[33]);

            // 34-35, 31
            CliffRightSignal = UnsignedWordToInt(data[34], data[35]);

            // 39, 34
            HomeBaseCharging = mask(data[39], 0b10);
            InternalCharging = mask(data[39], 0b01);

            // 52-53, 43
            int tempEncoderLeft = SignedWordToInt(data[52], data[53]) + 32768;
            LeftEncoderCount += (ulong)GetEncoderDelta(tempEncoderLeft, lastLeftEncoderCount);
            lastLeftEncoderCount = tempEncoderLeft;
            LeftWheelDistance = (((double)LeftEncoderCount) * Math.PI * WheelDiameter) / countsPerRotarion;

            // 54-55, 44
            int tempEncoderRight = SignedWordToInt(data[54], data[55]) + 32768;
            RightEncoderCount += (ulong)GetEncoderDelta(tempEncoderRight, lastRightEncoderCount);
            lastRightEncoderCount = tempEncoderRight;
            RightWheelDistance = (((double)RightEncoderCount) * Math.PI * WheelDiameter) / countsPerRotarion;

            bool leanRight = LeftWheelDistance > RightWheelDistance;
            double rads = (LeftWheelDistance - RightWheelDistance) / 233d;
            
            // 56, 45
            BitArray ltb = new BitArray(new byte[] { data[56] });
            LightBumperLeft = ltb[0];
            LightBumperFrontLeft = ltb[1];
            LightBumperCenterLeft = ltb[2];
            LightBumperCenterRight = ltb[3];
            LightBumperFrontRight = ltb[4];
            LightBumperRight = ltb[5];            
        }

        /// <summary>
        /// Task that handles writing to the motors. Turns two values into their 16 bit 2's complement
        /// and writes the proper command to the serial port.
        /// </summary>
        /// <param name="right">Right wheel speed.</param>
        /// <param name="left">Left wheel speed.</param>
        /// <returns>An awaitable task.</returns>
        private async Task UpdateMotors(int right, int left)
        {
            //Debug.WriteLine("Set motors: {0}, {1}", left, right);
            byte[] rBytes = TwosComplement(right);
            byte[] lBytes = TwosComplement(left);
            await WriteOut(new byte[] { 145, rBytes[1], rBytes[0], lBytes[1], lBytes[0] });
        }

        #endregion

        #region Helper Functions
        
        /// <summary>
        /// Get the delta value from 2 encoder readings.
        /// </summary>
        /// <param name="current">Encoder read from current update.</param>
        /// <param name="last">Encoder read from previous update.</param>
        /// <returns></returns>
        private int GetEncoderDelta(int current, int? last)
        {
            //First run:
            if (last == null) return 0;

            if (current < last)
            {
                // The encoder overflowed or is going backwards
                // TODO: account for possibility of roomba going backwards!
                int forwardDelta = current + 65536 - last.Value;
                return forwardDelta;
            }
            else
            {
                return current - last.Value;
            }
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

        /// <summary>
        /// Converts 2 bytes representing a 16-bit signed value into an int.
        /// </summary>
        /// <param name="high">High byte.</param>
        /// <param name="low">Low byte.</param>
        /// <returns></returns>
        private int SignedWordToInt(byte high, byte low)
        {
            return (int)BitConverter.ToInt16(new byte[] { high, low }, 0);
        }

        /// <summary>
        /// Converts 2 bytes representing a 16-bit unsigned value into an int.
        /// </summary>
        /// <param name="high"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        private int UnsignedWordToInt(byte high, byte low)
        {
            return (high << 8) + low;
        }

        /// <summary>
        /// Turns an int into a 16-bit 2's complement
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>Byte array of length 2 with the 16 bits.</returns>
        private byte[] TwosComplement(int input)
        {
            short temp = (short)bound(input, -500, 500);
            return BitConverter.GetBytes(temp);
        }

        /// <summary>
        /// Bound a value withing a range
        /// </summary>
        /// <param name="input">Value to bound.</param>
        /// <param name="min">Range minimum.</param>
        /// <param name="max">Range maximum.</param>
        /// <returns>The bounded value.</returns>
        private int bound(int input, int min, int max)
        {
            if (input > max)
            {
                return max;
            }
            else if (input < min)
            {
                return min;
            }
            else
            {
                return input;
            }
        }

        /// <summary>
        /// Translate the mode identifier to the actual code recognized by the interface.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns>The code used by the interface to switch the roomba to the desired mode.</returns>
        private byte mode2Code(byte mode)
        {
            switch (mode)
            {
                case 0:
                    return 173;
                case 1:
                    return 128;
                case 2:
                    return 131;
                case 3:
                    return 132;
                default:
                    return 173;
            }
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

                if (bytesWritten > 0)
                {
                    //Debug.WriteLine("Written: " + bytesWritten.ToString() + " bytes;");
                }
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
        private async Task<byte[]> ReadData(uint ReadBufferLength)
        {
            // If task cancellation was requested, comply
            token.ThrowIfCancellationRequested();

            Task<UInt32> loadAsyncTask;
            byte[] buffer = new byte[ReadBufferLength];
            loadAsyncTask = dataReader.LoadAsync(ReadBufferLength).AsTask(token);

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
