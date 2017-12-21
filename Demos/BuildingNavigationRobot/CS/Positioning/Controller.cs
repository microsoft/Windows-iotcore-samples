// Copyright (c) Microsoft. All rights reserved.

using BuildingNavigationRobot.Pathfinding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;

namespace BuildingNavigationRobot
{
    public class ControllerMessageEventArgs : EventArgs
    {
        public string Message;
    }

    /// <summary>
    /// The Controller class coordinates all of the pieces of the robot
    /// </summary>
    public class Controller : IDisposable
    {
        public Roomba Roomba { get; private set; }
        public Lidar Lidar { get; private set; }
        public Navigation Navigation { get; private set; }
        public LidarAnalysis Analysis { get; private set; }

        public delegate void ControllerMessageEventHandler(object sender, ControllerMessageEventArgs e);

        public event ControllerMessageEventHandler ControllerMessageReceived;

        public bool IsEnabled { get; private set; } = false;
        public bool IsNavigating { get; private set; } = false;

        private CancellationTokenSource cts;

        private StopReason lastStopType = StopReason.Requested;

        private const int DEFAULT_MOVE_SPEED = 350;
        private const int DEFAULT_ROTATE_SPEED = 70;
        private const int DEFAULT_ADJUST_SPEED = (int)(0.25 * DEFAULT_ROTATE_SPEED);

        public Controller()
        {
            Roomba = new Roomba();
            Lidar = new Lidar();
            Navigation = new Navigation();
            Analysis = new LidarAnalysis();
        }

        /// <summary>
        /// Initializes the Roomba and LIDAR
        /// </summary>
        /// <param name="roombaPortId">Serial device ID for the Roomba</param>
        /// <param name="lidarPortId">Serial device ID for the LIDAR</param>
        /// <returns></returns>
        public async Task InitializeAsync(string roombaDeviceId = Roomba.DEFAULT_DEVICE_ID, string lidarDeviceId = Lidar.DEFAULT_DEVICE_ID)
        {
            Debug.WriteLine("Initializing Controller...");
            OnControllerMessageReceived("Initializing controller...");

            // Initialize the Roomba
            await Roomba.InitializeAsync(roombaDeviceId, 20);
            Roomba.Stopped += Roomba_Stopped;
            OnControllerMessageReceived("Roomba successfully initialized.");

            // Initialize the LIDAR
            await Lidar.InitializeAsync(lidarDeviceId);
            Lidar.ScanComplete += Lidar_ScanComplete;
            Lidar.StartScan();
            OnControllerMessageReceived("LIDAR successfully initialized.");

            Analysis.AnalysisChanged += Analysis_AnalysisChanged;

            Debug.WriteLine("Controller initialization completed.");

            IsEnabled = true;
        }
        
        /// <summary>
        /// Determine the instructions to get to a room and execute them
        /// </summary>
        /// <param name="room">Room number</param>
        /// <returns></returns>
        public async Task NavigateToDestination(int room)
        {
            if (!IsNavigating)
            {
                cts = new CancellationTokenSource();
                var directions = Navigation.GetDirections(room);
                await ExecuteInstructions(directions.ToArray());
            }
            else
            {
                Debug.WriteLine("Already navigating!");
            }
        }

        /// <summary>
        /// Execute an array of instructions
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        public async Task ExecuteInstructions(Instruction[] instructions)
        {
            IsNavigating = true;

            foreach (var instruction in instructions)
            {
                await ExecuteNavigationInstruction(instruction);

                if (cts.Token.IsCancellationRequested)
                {
                    break;
                }

                await Task.Delay(500);
            }

            IsNavigating = false;
        }

        private async Task ExecuteNavigationInstruction(Instruction instruction)
        {
            Debug.WriteLine($"Executing nav instruction -> {instruction}");
            OnControllerMessageReceived($"Executing navigation instruction -> {instruction}");

            if (instruction.Type == InstructionType.Angle)
            {
                await Roomba.RotateEncoders(DEFAULT_ROTATE_SPEED, instruction.Data);
            }
            else if (instruction.Type == InstructionType.Distance)
            {
                await ContinuousMove(DEFAULT_MOVE_SPEED, (int)(1000 * instruction.Data), cts.Token);
            }
        }
        
        /// <summary>
        /// This function will move the robot while also stopping if there are obstacles and adjusting 
        /// course if it's heading towards a wall
        /// </summary>
        /// <param name="speed">Speed robot will move</param>
        /// <param name="distMM">Distance in millimeters</param>
        /// <returns></returns>
        public async Task ContinuousMove(int speed, int distMM, CancellationToken token)
        {
            if (!Roomba.IsEnabled)
            {
                Debug.WriteLine("Roomba is not enabled!  Cancelling move...");
                return;
            }

            lastStopType = StopReason.Success;

            int distMoved = 0;
            do
            {
                // Cancel the entire move if we collided with something or canceled everything
                if (token != null && token.IsCancellationRequested)
                {
                    return;
                }
                
                if (lastStopType == StopReason.Collision || lastStopType == StopReason.Cancellation)
                {
                    return;
                }

                switch (Analysis.LastRecommendation)
                {
                    case LidarRecommendation.Move:
                        Debug.WriteLine("ContinuousMove: Moving...");
                        // Calculate distance left to move
                        distMM -= distMoved;

                        // Move the robot
                        OnControllerMessageReceived($"Moving {distMM} mm...");
                        await Roomba.MoveDistanceEncoders(speed, distMM);

                        // Determine how far the robot moved
                        distMoved = (int)Roomba.AverageDistanceTraveled;
                        break;
                    case LidarRecommendation.AdjustObstacle:
                        Debug.WriteLine("ContinuousMove: Adjusting...");
                        // It doesn't matter what the actual adjust angle is, since the robot will
                        // stop rotating once we get the recommendation to move
                        if (Analysis.LastAdjustAngle < 0)
                        {
                            Roomba.SetSpeed(DEFAULT_ROTATE_SPEED, 0);
                        }
                        else
                        {
                            Roomba.SetSpeed(0, DEFAULT_ROTATE_SPEED);
                        }
                        break;
                }

                await Task.Delay(100);
            }
            while (distMoved < distMM );
        }
         

        /// <summary>
        /// Event handler for when the Roomba stops
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Roomba_Stopped(object sender, RoombaStopArgs e)
        {
            var stopReason = Enum.GetName(typeof(StopReason), e.StopOrigin);
            Debug.WriteLine("Roomba_Stopped: " + stopReason);

            lastStopType = e.StopOrigin;

            switch (e.StopOrigin)
            {
                case StopReason.Collision:
                    Debug.WriteLine("Stopped because of collision!");
                    cts?.Cancel();
                    break;
                case StopReason.Cancellation:
                    Debug.WriteLine("Stopped because of class wide cancellation!");
                    cts?.Cancel();
                    break;
                case StopReason.Requested:
                    Debug.WriteLine("Stopped because of a stop request!");
                    break;
                case StopReason.Success:
                    Debug.WriteLine("Stopped because success!");
                    break;
                default:
                    break;
            }
        }
                
        /// <summary>
        /// Event handler for when LIDAR analysis comes up with new recommendation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Analysis_AnalysisChanged(object sender, AnalysisEventArgs e)
        {
            Debug.WriteLine("Analysis_AnalysisChanged: " + Enum.GetName(typeof(LidarRecommendation), e.Recommendation));

            switch(e.Recommendation)
            {
                case LidarRecommendation.Stop:
                    Roomba.Halt();
                    break;
                case LidarRecommendation.AdjustObstacle:
                    Debug.WriteLine("Analysis_AnalysisChanged - Adjust Angle: " + e.AdjustAngle);
                    Roomba.CancelMove();
                    break;
                case LidarRecommendation.Move:
                    break;
            }
        }

        /// <summary>
        /// Event handler for when the LIDAR completes a scan
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Contains the points from the completed scan</param>
        private void Lidar_ScanComplete(object sender, ScanEventArgs e)
        {
            // Send to helper class to process the scan and operate the Roomba
            Analysis.ProcessReadings(ref e.Readings, e.Timestamp);
        }

        /// <summary>
        /// Event for updating the UI
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnControllerMessageReceived(string message)
        {
            Debug.WriteLine("ControllerMessage: " + message);
            ControllerMessageReceived?.Invoke(this, new ControllerMessageEventArgs()
            {
                Message = message
            });
        }

        public void Dispose()
        {
            Roomba.Close();
            Lidar.Close();

            IsEnabled = false;
        }
    }
}
