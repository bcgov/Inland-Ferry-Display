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
            return new Window
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Maximized,
                Width = screen.Bounds.Width / scaleX,
                Height = screen.Bounds.Height / scaleY,
                Left = screen.Bounds.X / scaleX,
                Top = screen.Bounds.Y / scaleY,
                ResizeMode = ResizeMode.NoResize
            };
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
                foreach (var win in projectionWindows)
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

        private static void DisplayCurrentImage(Window currentWindow, List<Spot> spots, int currentImageIndex)
        {
            var spot = spots[currentImageIndex];
            currentWindow.Content = CreateBoundImageControl(spot);
        }

        private static void InitializeUniformGrid(Window currentWindow, List<Spot> spots)
        {
            var uniformGrid = new UniformGrid
            {
                Rows = (int)Math.Ceiling(Math.Sqrt(spots.Count)),
                Columns = (int)Math.Ceiling(Math.Sqrt(spots.Count))
            };

            foreach (var spot in spots)
            {
                var image = CreateBoundImageControl(spot);
                var textBlock = CreateOverlayTextBlock(spot.Name);
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

        private static TextBlock CreateOverlayTextBlock(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Colors.Black) { Opacity = 0.5 },
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
        }
    }
}
