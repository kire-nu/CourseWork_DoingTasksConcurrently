//Erik Olofsson 2019-12-01
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading;

namespace DoingTasksConcurrently
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        // Paramaters for display (text)
        private Thread displayThread;
        private double displaySpeed = 0.2;
        private int displayWait = 1;
        private bool displayRunning = true;

        // Parameters for triangle
        private Thread triangleThread;
        private double triangleSpeed = 0.15;
        private double triangleCurrentAngle = 0;
        private int triangleWait = 2;
        private bool triangleRunning = true;

        public MainWindow() {
            InitializeComponent();
            InitializeGUI();
        }

        public void InitializeGUI() {
            displayStart.IsEnabled = true;
            displayStop.IsEnabled = false;
            triangleStart.IsEnabled = true;
            triangleStop.IsEnabled = false;
            DrawPolygon(triangleCurrentAngle);

        }

        /// <summary>
        /// Exiting program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {

            displayRunning = false;
            displayWait = 0;
            if (displayThread != null) {
                if (displayThread.IsAlive) {
                    displayThread.Abort();
                }
            }
            triangleRunning = false;
            triangleWait = 0;
            if (triangleThread != null) {
                if (triangleThread.IsAlive) {
                    triangleThread.Abort();
                }
            }
        }

        #region displayThread

        // Move text
        private void UpdateDisplayPosition() {

            double[] displayOffset = new double[] { displaySpeed, -displaySpeed }; //X, Y movement
            // True until stop is pressed
            while (displayRunning) {

                this.Dispatcher.Invoke(() => {
                    // Find position of text relative to bounding box
                    Point topLeftOffset = displayText.TranslatePoint(new Point(0, 0), displayCanvas);
                    Point bottomRightBound = new Point(displayCanvas.Width, displayCanvas.Height);
                    Point bottomRightLabel = new Point(topLeftOffset.X + displayText.Width, topLeftOffset.Y + displayText.Height);
                    Point bottomRightOffset = new Point(bottomRightBound.X - bottomRightLabel.X, bottomRightBound.Y - bottomRightLabel.Y);

                    // Check that text is within bounding box and change direction if it is at edge
                    if (topLeftOffset.X <= 0) {
                        displayOffset[0] = displaySpeed;
                    }
                    if (topLeftOffset.Y <= 0) {
                        displayOffset[1] = displaySpeed;
                    }

                    if (bottomRightOffset.X <= 0) {
                        displayOffset[0] = -displaySpeed;
                    }
                    if (bottomRightOffset.Y <= 0) {
                        displayOffset[1] = -displaySpeed;
                    }

                    // Update location of text
                    displayText.Margin = new Thickness(displayText.Margin.Left + displayOffset[0], displayText.Margin.Top + displayOffset[1], 0, 0);
                });

                // Wait before continuing
                Thread.Sleep(displayWait);

            }
        }

        /// <summary>
        /// Start button 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void displayStart_Click(object sender, RoutedEventArgs e) {

            displayRunning = true;
            displayThread = new Thread(UpdateDisplayPosition);
            displayThread.Start();

            displayStart.IsEnabled = false;
            displayStop.IsEnabled = true;
        }

        /// <summary>
        /// Stop buttom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void displayStop_Click(object sender, RoutedEventArgs e) {

            displayRunning = false;

            displayStart.IsEnabled = true;
            displayStop.IsEnabled = false;
        }

        #endregion

        #region triangleThread

        /// <summary>
        /// Calculate Position from centre at angle and radius
        /// </summary>
        /// <param name="degrees"></param>
        /// <param name="radius"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        private Point DegreesToXY(double degrees, double radius, Point origin) {
            Point xy = new Point();
            double radians = degrees * Math.PI / 180.0;

            xy.X = Math.Cos(radians) * radius + origin.X;
            xy.Y = Math.Sin(-radians) * radius + origin.Y;

            return xy;
        }

        /// <summary>
        /// Get position of points from centre
        /// </summary>
        /// <param name="sides"></param>
        /// <param name="radius"></param>
        /// <param name="startingAngle"></param>
        /// <param name="centre"></param>
        /// <returns></returns>
        private Point[] CalculatePoints(int sides, double radius, double startingAngle, Point centre) {
            if (sides < 3) {
                throw new ArgumentException("Polygon must have 3 sides or more.");
            }

            List<Point> points = new List<Point>();
            double step = 360.0 / sides;

            double angle = startingAngle + 90; // Add 90 so first point is on top of centre
            for (double i = startingAngle; i < startingAngle + 360.0 ; i += step) //go in a full circle
            {
                points.Add(DegreesToXY(angle, radius, centre)); 
                angle += step;
            }

            return points.ToArray();
        }

        /// <summary>
        /// Draw a polygon at an angle
        /// </summary>
        /// <param name="startingAngle"></param>
        private void DrawPolygon(double startingAngle) {
            this.Dispatcher.Invoke(() => {
                // Remove current triangle
                triangleCanvas.Children.Clear();

                // Get points and distances
                Point centre = new Point(triangleCanvas.Width / 2, triangleCanvas.Height / 2);
                double radius = Math.Min(triangleCanvas.Width / 2.5, triangleCanvas.Height / 2.5);
                Point[] points = CalculatePoints(3, radius, startingAngle, centre);

                // Crete polygon
                Polygon polygon = new Polygon();
                for (int i = 0; i < points.Length; i++) {

                    polygon.Points.Add(points[i]);
                }
                polygon.Stroke = Brushes.Red;
                polygon.Name = "triangle";

                // Add polygon to canvas
                triangleCanvas.Children.Add(polygon);
            });
        }

        /// <summary>
        /// Rotate triangle
        /// </summary>
        private void UpdateTriangle() {

            // True until stop is pressed
            while (triangleRunning) {

                // Calculate new angle
                triangleCurrentAngle -= triangleSpeed;
                if (triangleCurrentAngle > 360) {
                    triangleCurrentAngle -= 360;
                }
                triangleCurrentAngle -= triangleSpeed;
                if (triangleCurrentAngle < 0) {
                    triangleCurrentAngle += 360;
                }
                // Draw polygon
                DrawPolygon(triangleCurrentAngle);

                // Wait before continuing
                Thread.Sleep(triangleWait);
            }

        }

        private void TriangleStop() {

            triangleRunning = false;

            if (triangleThread != null) {

                int loops = 0;
                while(triangleThread.IsAlive && loops<5) {
                    Thread.Sleep(triangleWait);
                    loops++;
                }
                triangleThread = null;
            }

        }

        /// <summary>
        /// Start button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void triangleStart_Click(object sender, RoutedEventArgs e) {

            triangleRunning = true;
            triangleThread = new Thread(UpdateTriangle);
            triangleThread.Start();

            triangleStart.IsEnabled = false;
            triangleStop.IsEnabled = true;

        }

        /// <summary>
        /// Stop button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void triangleStop_Click(object sender, RoutedEventArgs e) {

            triangleRunning = false;

            triangleStart.IsEnabled = true;
            triangleStop.IsEnabled = false;
        }

        #endregion

    }
}
