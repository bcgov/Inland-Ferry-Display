using FerryDisplayApp.Enums;
using FerryDisplayApp.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // For Screen
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

namespace FerryDisplayApp.Services
{
    public static class DisplayManager
    {
        public static Window CreateProjectionWindow(Screen screen)
        {
            var (scaleX, scaleY) = GetDpiScaling();
            var window = new Window
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                WindowStyle = WindowStyle.None,
                Width = screen.Bounds.Width / scaleX, // Use entire screen bounds for width
                Height = screen.Bounds.Height / scaleY, // Use entire screen bounds for height
                Left = screen.Bounds.X, // Use absolute X from bounds
                Top = screen.Bounds.Y, // Use absolute Y from bounds
                ResizeMode = ResizeMode.NoResize
            };

            // This ensures the window is moved to the correct screen first
            window.WindowState = WindowState.Normal;
            window.Show();

            // Then maximize the window to fill the screen
            window.WindowState = WindowState.Maximized;
            return window;
        }

        public static void HandleModeDisplay(Window currentWindow, ProjectionMode currentMode, List<Spot> spots, ref int currentImageIndex, DispatcherTimer rotationTimer)
        {
            switch (currentMode)
            {
                case ProjectionMode.Rotating:
                    DisplayCurrentImage(currentWindow, spots, currentImageIndex);
                    rotationTimer.Start();
                    break;
                case ProjectionMode.Grid:
                    rotationTimer.Stop();
                    InitializeUniformGrid(currentWindow, spots);
                    break;
            }
        }

        public static void UpdateProjection(ProjectionMode currentMode, List<Spot> spots)
        {
            if (currentMode == ProjectionMode.Grid)
            {
                foreach (var spot in spots)
                {
                    spot.RefreshImage();
                }
            }
        }

        public static void RotateImage(ProjectionMode currentMode, List<Spot> spots, ref int currentImageIndex, Window currentWindow)
        {
            if (currentMode == ProjectionMode.Rotating && spots.Count > 0)
            {
                currentImageIndex = (currentImageIndex + 1) % spots.Count;
                DisplayCurrentImage(currentWindow, spots, currentImageIndex);
            }
        }

        public static void StopProjection(Window window, List<Window> projectionWindows, DispatcherTimer imageRefreshTimer, DispatcherTimer rotationTimer)
        {
            if (window != null)
            {
                window.Close();
                projectionWindows.Remove(window);
            }
            else
            {
                // Create a copy of the list for iteration
                var windowsToClose = new List<Window>(projectionWindows);
                foreach (var win in windowsToClose)
                {
                    win.Close();
                }
                projectionWindows.Clear();
            }

            if (projectionWindows.Count == 0)
            {
                imageRefreshTimer.Stop();
                rotationTimer.Stop();
            }
        }

        private static async void DisplayCurrentImage(Window currentWindow, List<Spot> spots, int currentImageIndex)
        {
            var spot = spots[currentImageIndex];
            await spot.FetchImageDataAsync();
            var image = CreateBoundImageControl(spot);

            var (scaleX, scaleY) = GetDpiScaling();
            image.Width *= scaleX;
            image.Height *= scaleY;

            var container = new Grid();
            container.Children.Add(image);

            var textBlock = CreateOverlayTextBlock(spot.Name, spot.LastModifiedTime, currentWindow.Width, currentWindow.Height);
            container.Children.Add(textBlock);

            Grid.SetRow(image, 0);
            Grid.SetRow(textBlock, 1);

            currentWindow.Content = container;
        }

        private static async void InitializeUniformGrid(Window currentWindow, List<Spot> spots)
        {
            var uniformGrid = new UniformGrid
            {
                Rows = (int)Math.Ceiling(Math.Sqrt(spots.Count)),
                Columns = (int)Math.Ceiling(Math.Sqrt(spots.Count))
            };

            foreach (var spot in spots)
            {
                await spot.FetchImageDataAsync();
                var image = CreateBoundImageControl(spot);
                var textBlock = CreateOverlayTextBlock(spot.Name, spot.LastModifiedTime, currentWindow.Width / uniformGrid.Columns, currentWindow.Height / uniformGrid.Rows);
                var container = new Grid();

                container.Children.Add(image);
                container.Children.Add(textBlock);
                uniformGrid.Children.Add(container);
            }

            currentWindow.Content = uniformGrid;
        }

        private static (double scaleX, double scaleY) GetDpiScaling()
        {
            using (var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                return (graphics.DpiX / 96.0, graphics.DpiY / 96.0);
            }
        }

        private static Image CreateBoundImageControl(Spot spot)
        {
            var image = new Image { Stretch = Stretch.UniformToFill };
            var binding = new System.Windows.Data.Binding("ImageSource") { Source = spot, Mode = BindingMode.OneWay };
            image.SetBinding(Image.SourceProperty, binding);
            return image;
        }

        private static TextBlock CreateOverlayTextBlock(string text, string timestamp, double containerWidth, double containerHeight)
        {
            double fontSize = CalculateFontSize(containerWidth, containerHeight);

            return new TextBlock
            {
                Text = $"{text} {timestamp}",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Colors.Black) { Opacity = 0.5 },
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Margin = new Thickness(10, 0, 0, 10),
                FontSize = fontSize
            };
        }

        private static double CalculateFontSize(double width, double height)
        {
            double baseFontSize = 16; // Base font size for a standard container size
            double sizeFactor = Math.Sqrt(width * height) / 400; // Scale factor based on container size
            double calculatedFontSize = baseFontSize * sizeFactor;

            return Math.Max(calculatedFontSize, 12); // Ensure a minimum font size of 12
        }
    }
}
