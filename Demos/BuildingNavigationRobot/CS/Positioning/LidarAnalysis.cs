// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace BuildingNavigationRobot
{    
    public enum LidarRecommendation
    {
        Stop,
        Move,
        AdjustObstacle
    }

    public class AnalysisEventArgs : EventArgs
    {
        public LidarRecommendation Recommendation;
        public double AdjustAngle;
    }

    public struct PolarCoordinate
    {
        public double Angle, Distance;
    }

    /// <summary>
    /// This class takes points from the LIDAR, analyzes them, and then recommends an action
    /// for the robot to take
    /// </summary>
    public class LidarAnalysis
    {
        private object processingLock = new object();
        
        public delegate void AnalysisEventHandler(object sender, AnalysisEventArgs e);

        public event AnalysisEventHandler AnalysisChanged;
        
        public long LastProcessingTime { get; private set; } = 0;
        public LidarRecommendation LastRecommendation { get; private set; } = LidarRecommendation.Stop;
        public double LastAdjustAngle { get; private set; } = 0;

        // Distance that will cause the robot to stop moving
        public const int DEFAULT_STOP_DISTANCE = 300;

        // Field of view that the robot looks at when stopping
        public const int STOP_FOV_DEGREES = 35;

        // Distance that will cause robot to adjust course
        public const int DEFAULT_ADJUST_DISTANCE = 425;

        // Field of view that the robot looks at when adjusting
        public const int ADJUST_FOV_DEGREES = 65;

        // Number of points needed to trigger an adjustment
        public const int MIN_ADJUST_POINT_COUNT = 1;
        
        public int StopDistance = DEFAULT_STOP_DISTANCE;
        public int AdjustDistance = DEFAULT_ADJUST_DISTANCE;
        public int StopFov = STOP_FOV_DEGREES;
        public int AdjustFov = ADJUST_FOV_DEGREES;

        public const int ERROR_SQUARED_TOLERANCE = 700;
        
        public void ProcessReadings(ref Hit[] readings, long timestamp)
        {
            // Only process one reading at a time
            lock (processingLock)
            {
                // This group of readings are older than the last group processed, so skip
                if (timestamp < LastProcessingTime)
                {
                    return;
                }

                //Debug.WriteLine($"Processing {readings.Length} points, {timestamp}...");

                var newRec = LidarRecommendation.Move;
                double adjustAngle = 0;

                var leftPoints = new List<Hit>();
                var rightPoints = new List<Hit>();

                // Do processing
                foreach (var reading in readings)
                {
                    // Skip bad reading
                    if (reading.Quality == 0)
                    {
                        continue;
                    }

                    // Convert to Cartesian coordiantes
                    var point = reading.CartesianPoint;

                    // Check if we need to stop because we're about to hit something
                    if (IsInStopZone(reading.Angle, reading.Distance))
                    {
                        // Set recommendation to stop so the robot doesn't keep trying to move
                        newRec = LidarRecommendation.Stop;
                        // Update the last processing time
                        LastProcessingTime = timestamp;
                        OnAnalysisChanged(newRec, adjustAngle);
                        return;
                    }

                    // Check if we need to adjust trajectory
                    if (IsInAdjustZone(reading.Angle, reading.Distance))
                    {
                        if (point.X < 0)
                        {
                            leftPoints.Add(reading);
                        }
                        else
                        {
                            rightPoints.Add(reading);
                        }
                    }
                }

                // Check for obstacle
                if (leftPoints.Count > MIN_ADJUST_POINT_COUNT || rightPoints.Count > MIN_ADJUST_POINT_COUNT)
                {
                    // Find the point that is closest to the robot
                    var leftMinPoint = new Hit()
                    {
                        Distance = int.MaxValue
                    };

                    var rightMinPoint = new Hit()
                    {
                        Distance = int.MaxValue
                    };

                    foreach (var point in leftPoints)
                    {
                        if (point.Distance < leftMinPoint.Distance)
                        {
                            leftMinPoint = point;
                        }
                    }

                    foreach (var point in rightPoints)
                    {
                        if (point.Distance < rightMinPoint.Distance)
                        {
                            rightMinPoint = point;
                        }
                    }

                    // Check to see which side is closer to object
                    if (leftMinPoint.Distance < rightMinPoint.Distance)
                    {
                        newRec = LidarRecommendation.AdjustObstacle;
                        adjustAngle = 90;
                    }
                    else if (leftMinPoint.Distance > rightMinPoint.Distance)
                    {
                        newRec = LidarRecommendation.AdjustObstacle;
                        adjustAngle = -90;
                    }
                    else
                    {
                        newRec = LidarRecommendation.Stop;
                    }
                }

                // Update the last processing time
                LastProcessingTime = timestamp;
                OnAnalysisChanged(newRec, adjustAngle);
            }
        }

        /// <summary>
        /// Checks if the angle and distance is within the robot's stop zone, based on the StopDistance and StopFov
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        /// <param name="distance">Distance in mm</param>
        /// <returns></returns>
        public bool IsInStopZone(double angle, double distance)
        {
            return ((angle < StopFov || angle > (360 - StopFov)) && distance < StopDistance);
        }

        /// <summary>
        /// Checks if the angle and distance is within the robot's adjust zone, based on the StopDistance and StopFov
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        /// <param name="distance">Distance in mm</param>
        /// <returns></returns>
        public bool IsInAdjustZone(double angle, double distance)
        {
            return ((angle < AdjustFov || angle > (360 - AdjustFov)) && distance < AdjustDistance);
        }
        
        protected virtual void OnAnalysisChanged(LidarRecommendation recommendation, double adjustAngle = 0)
        {
            if (recommendation != LastRecommendation || adjustAngle != LastAdjustAngle)
            {
                LastRecommendation = recommendation;
                LastAdjustAngle = adjustAngle;

                AnalysisChanged?.Invoke(this, new AnalysisEventArgs()
                {
                    Recommendation = recommendation,
                    AdjustAngle = adjustAngle
                });
            }
        }

        /// <summary>
        /// Converts an array of Hits to Points
        /// </summary>
        /// <param name="hits"></param>
        /// <returns></returns>
        private Point[] ConvertHitsToPoints(Hit[] hits)
        {
            List<Point> points = new List<Point>();
            foreach(var hit in hits)
            {
                points.Add(hit.CartesianPoint);
            }

            return points.ToArray();
        }

        /// <summary>
        /// Takes an array of points and determines if it is a possible wall
        /// </summary>
        /// <param name="points"></param>
        /// <returns>Tuple with slope (m) as Item1 and y-intersect (b) as Item2 if there is a wall, 0 for both if not.</returns>
        private Tuple<double, double> CheckWall(Point[] points)
        {
            // Check if we have enough points to make a guess
            if (points.Length >= MIN_ADJUST_POINT_COUNT)
            {
                // Figure out if points make a straight line - we assume it's a wall if it does
                var leastSquares = CalculateLeastSquares(points.ToArray());
                double m = leastSquares.Item1;
                double b = leastSquares.Item2;

                double errSquared = CalculateErrorSquared(points.ToArray(), m, b);
                //Debug.WriteLine($"Error squared: {errSquared}");

                // Find your error squared value by testing
                if (errSquared > ERROR_SQUARED_TOLERANCE)
                {
                    return Tuple.Create(0.0, 0.0);
                }

                return Tuple.Create(m, b);
            }

            return Tuple.Create(0.0, 0.0);
        }
        
        /// <summary>
        /// Takes an array of points and figures out the best fit line for them
        /// </summary>
        /// <param name="points"></param>
        /// <returns>Tuple with slope (m) as Item1 and y-intersect (b) as Item2</returns>
        public static Tuple<double, double> CalculateLeastSquares(Point[] points)
        {
            double sum1 = 0;
            double sum2 = 0;
            double xAvg = 0;
            double yAvg = 0;
            double m, b = 0;

            // Find X and Y averages
            foreach (var point in points)
            {
                xAvg += point.X;
                yAvg += point.Y;
            }

            xAvg /= points.Length;
            yAvg /= points.Length;

            // Calculate summations
            foreach (var point in points)
            {
                sum1 += (point.X - xAvg) * (point.Y - yAvg);
                sum2 += (point.X - xAvg) * (point.X - xAvg);
            }

            m = sum1 / sum2;
            b = yAvg - m * xAvg;

            return Tuple.Create(m, b);
        }

        /// <summary>
        /// Code for calculating error squared from csharphelper.com
        /// </summary>
        /// <param name="points"></param>
        /// <param name="m">Slope of the line</param>
        /// <param name="b">Y-intersect of the line</param>
        /// <returns></returns>
        public static double CalculateErrorSquared(Point[] points, double m, double b)
        {
            double total = 0;
            foreach (Point point in points)
            {
                double dy = point.Y - (m * point.X + b);
                total += dy * dy;
            }
            return total;
        }
    }
}
