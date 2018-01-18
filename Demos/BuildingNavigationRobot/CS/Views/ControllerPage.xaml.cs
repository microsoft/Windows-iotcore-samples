// Copyright (c) Microsoft. All rights reserved.

using AzureIoTHub;
using BuildingNavigationRobot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace BuildingNavigationRobot.Views
{
    /// <summary>
    /// Page for testing the LIDAR and Roomba code
    /// </summary>
    public sealed partial class ControllerPage : Page
    {
        Controller controller;
        VoiceCommandController voiceController;
        Stopwatch stopwatch;

        long lastFrameUpdate;
        long FRAME_WAIT_TIME = 50;
        int START_LOCATION = 2031;

        object frameLock = new object();
        CancellationTokenSource cts;

        // Automatically hides the voice command screen
        DispatcherTimer voiceCommandHideTimer;

        // Update the UI with robot status
        DispatcherTimer robotStatsUpdateTimer;

        Line leftAdjustLine = new Line();
        Line rightAdjustLine = new Line();
        Line leftStopLine = new Line();
        Line rightStopLine = new Line();

        Ellipse stopCircle = new Ellipse();
        Ellipse adjustCircle = new Ellipse();

        // Size of coordinate plane to plot points
        double PLOT_BOUNDS = 3000;

        public ControllerPage()
        {
            this.InitializeComponent();
            moveButton.IsEnabled = stopButton.IsEnabled = navButton.IsEnabled = false;

            stopwatch = new Stopwatch();
            stopwatch.Start();

            voiceCommandHideTimer = new DispatcherTimer();
            voiceCommandHideTimer.Interval = TimeSpan.FromSeconds(5);
            voiceCommandHideTimer.Tick += VoiceCommandHideTimer_Tick;

            controller = new Controller();
            controller.ControllerMessageReceived += Controller_ControllerMessageReceived;

            voiceController = new VoiceCommandController();
            voiceController.ResponseReceived += VoiceController_ResponseReceived;
            voiceController.CommandReceived += VoiceController_CommandReceived;
            voiceController.StateChanged += VoiceController_StateChanged;

            InitializeUI();
        }

        /// <summary>
        /// Starts timer to update the robot stats UI periodically
        /// </summary>
        private void StartRobotUpdateTimer()
        {
            robotStatsUpdateTimer = new DispatcherTimer();
            robotStatsUpdateTimer.Interval = TimeSpan.FromMilliseconds(1000);
            robotStatsUpdateTimer.Tick += (s, args) =>
            {
                robotStatsTextBlock.Text = $"🔋 {controller.Roomba.BatteryLeft} %";

                scrollViewer.UpdateLayout();
                scrollViewer.ChangeView(0, double.MaxValue, 1.0f);
            };
            robotStatsUpdateTimer.Start();
        }
        
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {          
            if (!controller.IsEnabled)
            {
                try
                {
                    // Initialize navigation
                    controller.Navigation.InitializeMap(@"Map\BuildingMap.xml");
                    controller.Navigation.SetStartingPosition(START_LOCATION);

                    // Initialize controller
                    await controller.InitializeAsync();

                    controller.Lidar.ScanComplete += Lidar_ScanComplete;

                    // Initialize voice commands
                    WriteToOutputTextBlock("Initializing voice commands...");
                    voiceController.Initialize();

                    // Initialize the robot stats UI update timer
                    StartRobotUpdateTimer();

                    // Set initial button states
                    moveButton.IsEnabled = stopButton.IsEnabled = navButton.IsEnabled = offButton.IsEnabled = true;

                    // Set initial slider values
                    adjustDistanceSlider.Value = controller.Analysis.AdjustDistance;
                    adjustFovSlider.Value = controller.Analysis.AdjustFov;

                    // When slider is changed, update the adjust distance UI
                    adjustDistanceSlider.ValueChanged += (s, args) =>
                    {
                        UpdateLines();
                        UpdateCircles();
                        controller.Analysis.AdjustDistance = (int)adjustDistanceSlider.Value;
                    };

                    // When slider is changed, update the adjust FOV UI
                    adjustFovSlider.ValueChanged += (s, args) =>
                    {
                        UpdateLines();
                        UpdateCircles();
                        controller.Analysis.AdjustFov = (int)adjustFovSlider.Value;
                    };
                }
                catch (Exception ex)
                {
                    WriteToOutputTextBlock(ex.Message);
                }
            }
            else
            {
                moveButton.IsEnabled = stopButton.IsEnabled = navButton.IsEnabled = offButton.IsEnabled = true;
            }
        }
        
        private void InitializeUI()
        {
            canvas.Width = canvas.Height = PLOT_BOUNDS;
            outputTextBlock.Text = "";
            voiceCommandGrid.Visibility = Visibility.Collapsed;
            controllerGrid.Visibility = Visibility.Visible;
            robotStatsTextBlock.Text = "🔋";
            
            canvas.Children.Add(stopCircle);
            canvas.Children.Add(adjustCircle);

            // Draw the circle that represents the Roomba body
            var roombaCircle = new Ellipse()
            {
                Width = Roomba.RoombaDiameter,
                Height = Roomba.RoombaDiameter,
                Fill = new SolidColorBrush(Colors.Blue),
                Name = "roombaCircle"
            };

            Canvas.SetLeft(roombaCircle, canvas.Width / 2 - roombaCircle.Width / 2);
            Canvas.SetTop(roombaCircle, canvas.Height / 2 - roombaCircle.Height / 4);

            canvas.Children.Add(roombaCircle);

            // Draw center vertical and horizontal lines
            var verticalLine = new Line()
            {
                X1 = canvas.Width / 2,
                Y1 = 0,
                X2 = canvas.Width / 2,
                Y2 = canvas.Height,
                Stroke = new SolidColorBrush(Colors.Aqua),
                StrokeThickness = 5
            };

            canvas.Children.Add(verticalLine);
            Canvas.SetZIndex(verticalLine, 0);

            var horizontalLine = new Line()
            {
                X1 = 0,
                Y1 = canvas.Height / 2,
                X2 = canvas.Width,
                Y2 = canvas.Height / 2,
                Stroke = new SolidColorBrush(Colors.Aqua),
                StrokeThickness = 5
            };

            canvas.Children.Add(horizontalLine);
            Canvas.SetZIndex(verticalLine, 0);
            
            canvas.Children.Add(leftAdjustLine);
            canvas.Children.Add(rightAdjustLine);

            canvas.Children.Add(leftStopLine);
            canvas.Children.Add(rightStopLine);

            UpdateLines();
            UpdateCircles();
        }
        
        #region Helper Functions

        /// <summary>
        /// Speaks text using text-to-speech
        /// </summary>
        /// <param name="text">Text to speak</param>
        private async void Speak(string text)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                SpeechHelper.Speak(text);
            });
        }

        /// <summary>
        /// Enables/disables a button
        /// </summary>
        /// <param name="button">Button to enable/disable</param>
        /// <param name="enable">True to enable, false to disable</param>
        private async void EnableButton(Button button, bool enable)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                button.IsEnabled = enable;
            });
        }

        /// <summary>
        /// Writes the text and emoji icon to the voice command screen
        /// </summary>
        /// <param name="text"></param>
        /// <param name="emoji"></param>
        /// <param name="timeout"></param>
        private async void WriteToCommandTextBlock(string text, string emoji = "😃", long timeout = 3)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                emojiTextBlock.Text = emoji;
                voiceCommandTextBlock.Text = text;

                voiceCommandGrid.Visibility = Visibility.Visible;
                controllerGrid.Visibility = Visibility.Collapsed;

                voiceCommandHideTimer.Interval = TimeSpan.FromSeconds(timeout);
                voiceCommandHideTimer.Start();
            });
        }

        /// <summary>
        /// Writes text to the output textblock
        /// </summary>
        /// <param name="text"></param>
        private async void WriteToOutputTextBlock(string text)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                outputTextBlock.Text = outputTextBlock.Text + ((outputTextBlock.Text == "") ? "" : (Environment.NewLine + Environment.NewLine)) + text;
                scrollViewer.UpdateLayout();
                scrollViewer.ChangeView(0, double.MaxValue, 1.0f);
            });
        }

        /// <summary>
        /// This method plots the Points to the Canvas.  We reuse existing UI elements instead of creating new ones to speed up the process.
        /// </summary>
        /// <param name="points">Array of Points to be plotted</param>
        private async void PlotPoints(Point[] points)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                // Create a queue of points to be plotted
                Queue<Point> pointQueue = new Queue<Point>(points.ToList());

                // Look for existing Ellipses in the Canvas
                foreach (UIElement element in canvas.Children)
                {
                    var ellipse = element as Ellipse;
                    if (ellipse != null && ellipse.Name == "point")
                    {
                        // We found an existing Ellipse and we have Points in the Queue, move it to the Point's coordinates
                        if (pointQueue.Count > 0)
                        {
                            // Remove the Point from the Queue
                            var pointToPlot = pointQueue.Dequeue();

                            ConfigureEllipse(ref ellipse, pointToPlot);

                            // Move the existing Ellipse to the Point coordinates
                            Canvas.SetLeft(ellipse, canvas.Width / 2 + pointToPlot.X);
                            Canvas.SetTop(ellipse, canvas.Height / 2 - pointToPlot.Y);

                            // Set it to visible
                            ellipse.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            // We found an Ellipse, but we have no more Points left to plot
                            ellipse.Visibility = Visibility.Collapsed;
                        }
                    }
                }

                // We still have Points left to plot, but no more Ellipses, so we need to create more
                while (pointQueue.Count > 0)
                {
                    var pointToPlot = pointQueue.Dequeue();

                    // Create the new Ellipse
                    var ellipse = new Ellipse();
                    ConfigureEllipse(ref ellipse, pointToPlot);

                    Canvas.SetLeft(ellipse, canvas.Width / 2 + pointToPlot.X);
                    Canvas.SetTop(ellipse, canvas.Height / 2 - pointToPlot.Y);

                    ellipse.Visibility = Visibility.Visible;

                    // Add the Ellipse to the Canvas
                    canvas.Children.Add(ellipse);
                }

                // Record the last time the points were plotted
                lastFrameUpdate = stopwatch.ElapsedMilliseconds;

                // Delay to give app time to refresh the UI
                await Task.Delay(2);
            });
        }

        /// <summary>
        /// Configures the point for plotting
        /// </summary>
        /// <param name="ellipse"></param>
        /// <param name="point"></param>
        private void ConfigureEllipse(ref Ellipse ellipse, Point point)
        {
            var polar = Lidar.ConvertToPolar(point);

            // Set the color based on how close the point is to the robot
            var ellipseColor = Colors.White;
            if (controller.Analysis.IsInStopZone(polar.Angle, polar.Distance))
            {
                ellipseColor = Colors.Red;
            }
            else if (controller.Analysis.IsInAdjustZone(polar.Angle, polar.Distance))
            {
                ellipseColor = Colors.Yellow;
            }

            ellipse.Width = ellipse.Height = 25;
            ellipse.Fill = new SolidColorBrush(ellipseColor);
            ellipse.Name = "point";
        }

        /// <summary>
        /// Shows/hides the voice command panel
        /// </summary>
        /// <param name="show"></param>
        private async void ShowVoiceCommandPanel(bool show = true)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                if (!show)
                {
                    voiceCommandGrid.Visibility = Visibility.Collapsed;
                    controllerGrid.Visibility = Visibility.Visible;
                    voiceCommandHideTimer.Stop();
                }
                else
                {
                    voiceCommandGrid.Visibility = Visibility.Visible;
                    controllerGrid.Visibility = Visibility.Collapsed;
                }
                await Task.Delay(2);
            });
        }
        
        private async void PlotLine(Line line, Color color)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // Adjust coordinates for plotting on canvas
                line.X1 = canvas.Width / 2 + line.X1;
                line.Y1 = canvas.Height / 2 - line.Y1;

                line.X2 = canvas.Width / 2 + line.X2;
                line.Y2 = canvas.Height / 2 - line.Y2;

                //Debug.WriteLine($"X1: {line.X1}, Y1: {line.Y1}");
                //Debug.WriteLine($"X2: {line.X2}, Y2: {line.Y2}");

                line.Stroke = new SolidColorBrush(color);
                line.StrokeThickness = 10;

                //Canvas.SetZIndex(line, 10000);

                line.Visibility = Visibility.Visible;
            });
        }

        #endregion

        #region Event Handlers
        
        private void VoiceController_StateChanged(object sender, VoiceCommandControllerEventArgs e)
        {
            try
            {
                var state = (VoiceControllerState)e.Data;
                switch (state)
                {
                    case VoiceControllerState.ListenCommand:
                        WriteToOutputTextBlock("Listening for command... 🎤");
                        break;
                    case VoiceControllerState.ListenTrigger:
                        WriteToOutputTextBlock("Listening for trigger... 🎤");
                        break;
                    case VoiceControllerState.ProcessCommand:
                        WriteToOutputTextBlock("Processing command... 🕺");
                        break;
                    case VoiceControllerState.ProcessTrigger:
                        WriteToOutputTextBlock("Processing trigger... 🕺");
                        break;
                    case VoiceControllerState.Idle:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private void VoiceCommandHideTimer_Tick(object sender, object e)
        {
            ShowVoiceCommandPanel(false);
        }

        private async void VoiceController_CommandReceived(object sender, VoiceCommandControllerEventArgs e)
        {
            string response = "Sorry, I didn't get that.";

            try
            {
                var voiceCommand = (VoiceCommand)e.Data;

                switch(voiceCommand.CommandType)
                {
                    case VoiceCommandType.Navigate:
                        int dest = (int)voiceCommand.Data;
                        if (controller.Navigation.DoesRoomExist(dest))
                        {
                            Telemetry.SendReport(Telemetry.MessageType.VoiceCommand, "Successful Command.");

                            response = "Navigating to " + dest + "...";
                            Speak(response);
                            WriteToCommandTextBlock(response, "👌", 1);
                            await controller.NavigateToDestination(dest);
                        }
                        else
                        {
                            response = "Sorry, I don't know where " + dest + " is.";
                            Speak(response);
                            WriteToCommandTextBlock(response, "🤷‍♂️");
                        }

                        break;
                    case VoiceCommandType.Move:

                        response = "Moving...";
                        Speak(response);
                        WriteToCommandTextBlock(response, "🏃‍♂️", 1);

                        cts = new CancellationTokenSource();
                        await ThreadPool.RunAsync(async (s) =>
                        {
                            await controller.ContinuousMove(100, 700, cts.Token);
                            EnableButton(moveButton, true);
                        });
                        break;
                    case VoiceCommandType.Stop:
                        response = "Stopping...";
                        Speak(response);
                        WriteToCommandTextBlock(response, "🛑", 1);

                        cts?.Cancel();
                        WriteToOutputTextBlock("Stopping...");
                        controller.Roomba.Halt(StopReason.Cancellation);
                        break;
                    default:
                        response = "Sorry, I didn't get that." + Environment.NewLine + "Try \"Go to room 2011\"";
                        Speak(response);
                        WriteToCommandTextBlock(response, "🤷‍♂️");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private void VoiceController_ResponseReceived(object sender, VoiceCommandControllerEventArgs e)
        {
            string text = e.Data as string;
            if (!string.IsNullOrEmpty(text) && text != "...")
            {
                switch(text)
                {
                    case "TriggerSuccess":
                        WriteToCommandTextBlock("Please say a command...", "😃", 10000);
                        break;
                    case "TimeoutExceeded":
                        ShowVoiceCommandPanel(false);
                        break;
                    default:
                        WriteToCommandTextBlock(text);
                        break;
                }
            }
        }

        private void Controller_ControllerMessageReceived(object sender, ControllerMessageEventArgs e)
        {
            WriteToOutputTextBlock(e.Message);
        }

        private async void Lidar_ScanComplete(object sender, ScanEventArgs e)
        {
            if (stopwatch.ElapsedMilliseconds - lastFrameUpdate < FRAME_WAIT_TIME)
            {
                return;
            }
            
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (controllerGrid.Visibility != Visibility.Visible)
                {
                    return;
                }

                var points = new List<Point>();

                foreach (var reading in e.Readings)
                {
                    // Only plot valid points
                    if (reading.Quality > 0)
                    {
                        points.Add(reading.CartesianPoint);
                    }
                }

                PlotPoints(points.ToArray());

            });
        }

        #endregion

        #region UI 
        
        /// <summary>
        /// Draw boundary circles
        /// </summary>
        private void UpdateCircles()
        {
            stopCircle.Width = controller.Analysis.StopDistance * 2;
            stopCircle.Height = controller.Analysis.StopDistance * 2;
            stopCircle.StrokeThickness = 10;
            stopCircle.Stroke = new SolidColorBrush(Colors.Red);
            stopCircle.Name = "stopCircle";

            Canvas.SetLeft(stopCircle, canvas.Width / 2 - stopCircle.Width / 2);
            Canvas.SetTop(stopCircle, canvas.Height / 2 - stopCircle.Height / 2);
            
            adjustCircle.Width = controller.Analysis.AdjustDistance * 2;
            adjustCircle.Height = controller.Analysis.AdjustDistance * 2;
            adjustCircle.StrokeThickness = 10;
            adjustCircle.Stroke = new SolidColorBrush(Colors.Yellow);
            adjustCircle.Name = "warnCircle";

            Canvas.SetLeft(adjustCircle, canvas.Width / 2 - adjustCircle.Width / 2);
            Canvas.SetTop(adjustCircle, canvas.Height / 2 - adjustCircle.Height / 2);
        }

        /// <summary>
        /// Draw boundary lines
        /// </summary>
        private void UpdateLines()
        {
            leftAdjustLine.X1 = 0; 
            leftAdjustLine.X2 = -controller.Analysis.AdjustDistance * Math.Sin(controller.Analysis.AdjustFov * Math.PI / 180.0);
            leftAdjustLine.Y1 = 0;
            leftAdjustLine.Y2 = controller.Analysis.AdjustDistance * Math.Cos(controller.Analysis.AdjustFov * Math.PI / 180.0); 
            PlotLine(leftAdjustLine, Colors.Yellow);

            rightAdjustLine.X1 = 0; 
            rightAdjustLine.X2 = controller.Analysis.AdjustDistance * Math.Sin(controller.Analysis.AdjustFov * Math.PI / 180.0); 
            rightAdjustLine.Y1 = 0;
            rightAdjustLine.Y2 = controller.Analysis.AdjustDistance * Math.Cos(controller.Analysis.AdjustFov * Math.PI / 180.0); 
            PlotLine(rightAdjustLine, Colors.Yellow);


            leftStopLine.X1 = 0; 
            leftStopLine.X2 = -controller.Analysis.StopDistance * Math.Sin(controller.Analysis.StopFov * Math.PI / 180.0); 
            leftStopLine.Y1 = 0;
            leftStopLine.Y2 = controller.Analysis.StopDistance * Math.Cos(controller.Analysis.StopFov * Math.PI / 180.0); 
            PlotLine(leftStopLine, Colors.Red);

            rightStopLine.X1 = 0; 
            rightStopLine.X2 = controller.Analysis.StopDistance * Math.Sin(controller.Analysis.StopFov * Math.PI / 180.0); 
            rightStopLine.Y1 = 0;
            rightStopLine.Y2 = controller.Analysis.StopDistance * Math.Cos(controller.Analysis.StopFov * Math.PI / 180.0); 
            PlotLine(rightStopLine, Colors.Red);
        }

        private async void moveButton_Click(object sender, RoutedEventArgs e)
        {
            WriteToOutputTextBlock("Moving...");
            EnableButton(moveButton, false);
            cts = new CancellationTokenSource();
            await ThreadPool.RunAsync(async (s) =>
            {
                await controller.ContinuousMove(100, 700, cts.Token);
                EnableButton(moveButton, true);
            });
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();

            WriteToOutputTextBlock("Stopping...");
            controller.Roomba.Halt(StopReason.Cancellation);

            EnableButton(moveButton, true);
            EnableButton(navButton, true);
        }

        private async void navButton_Click(object sender, RoutedEventArgs e)
        {
            EnableButton(navButton, false);

            int src = START_LOCATION;
            int dest = 2071;

            WriteToOutputTextBlock($"Navigating from {src} to {dest}...");
            await controller.NavigateToDestination(dest);
            WriteToOutputTextBlock($"Navigating from {dest} to {src}...");
            await controller.NavigateToDestination(src);

            EnableButton(navButton, true);
        }

        private void offButton_Click(object sender, RoutedEventArgs e)
        {
            WriteToOutputTextBlock("Setting Roomba to OFF mode...");
            controller.Roomba.SetMode(Mode.Off);
        }

        private void canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var pointerPt = e.GetCurrentPoint(canvas);

            double transX = pointerPt.Position.X - (canvas.Width / 2);
            double transY = (canvas.Height / 2) - pointerPt.Position.Y;


            Debug.WriteLine($"{transX}, {transY}");
        }

        private void closeVoiceCommandButton_Click(object sender, RoutedEventArgs e)
        {
            ShowVoiceCommandPanel(false);
        }

        #endregion
    }
}
