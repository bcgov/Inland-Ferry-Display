using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FerryDisplayApp.Models;
using System.Drawing;
using System.Windows.Threading;
using System.Windows.Forms;
using Application = System.Windows.Application;
using FerryDisplayApp.Enums;
using System.Diagnostics;
using System.Windows.Data;

namespace FerryDisplayApp.Services
{
    public class ProjectionService
    {
        private DispatcherTimer _imageRefreshTimer = new DispatcherTimer();
        private List<Spot> _currentSpots;
        private Window? _currentProjectionWindow;
        private List<Window> _projectionWindows = new List<Window>();
        private Screen _currentScreen;
        private ProjectionMode _currentMode;
        private int _currentImageIndex = 0;

        public ProjectionService()
        {
            SetupImageRefreshTimer();
        }

        private void SetupImageRefreshTimer()
        {
            _imageRefreshTimer.Interval = TimeSpan.FromMinutes(1); // Adjust as necessary
            _imageRefreshTimer.Tick += ImageRefreshTimer_Tick;
            _imageRefreshTimer.Start();
        }
        private void ImageRefreshTimer_Tick(object sender, EventArgs e)
        {
            Trace.WriteLine("UPDATING");
            UpdateProjection();
        }

        public void StartProjection(List<Spot> spots, Screen screen, ProjectionMode mode)
        {
            _currentSpots = spots;
            _currentScreen = screen;
            _currentMode = mode;
            _currentProjectionWindow = CreateProjectionWindow(screen);
            _currentImageIndex = 0; // Reset for rotating projection

            _currentProjectionWindow.Closed += CurrentProjectionWindow_Closed;
            _projectionWindows.Add(_currentProjectionWindow);

            _currentProjectionWindow.Show();
            UpdateProjection();
            _imageRefreshTimer.Start();
        }

        private void CurrentProjectionWindow_Closed(object sender, EventArgs e)
        {
            var window = sender as Window;
            if (window != null)
            {
                window.Closed -= CurrentProjectionWindow_Closed;
                _projectionWindows.Remove(window);
            }
            // StopProjection();
        }

        private void UpdateProjection()
        {
            switch (_currentMode)
            {
                case ProjectionMode.Grid:
                    UpdateGridProjection();
                    break;
                case ProjectionMode.Rotating:
                    UpdateRotatingProjection();
                    break;
            }
        }

        private void UpdateGridProjection()
        {
            if (_currentProjectionWindow == null || _currentSpots == null || !_currentProjectionWindow.IsVisible) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var gridWindow = _currentProjectionWindow;
                int gridSize = (int)Math.Ceiling(Math.Sqrt(_currentSpots.Count));
                var uniformGrid = new UniformGrid
                {
                    Rows = gridSize,
                    Columns = gridSize
                };

                foreach (var spot in _currentSpots)
                {
                    spot.RefreshImage();
                    var container = new Grid();
                    var image = CreateBoundImageControl(spot);
                    var textBlock = CreateOverlayTextBlock(spot.Name);
                    container.Children.Add(image);
                    container.Children.Add(textBlock);
                    uniformGrid.Children.Add(container);
                }

                gridWindow.Content = uniformGrid;
            });
        }

        private void UpdateRotatingProjection()
        {
            if (_currentProjectionWindow == null || _currentSpots == null || !_currentProjectionWindow.IsVisible) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_currentSpots.Count == 0) return;

                var spot = _currentSpots[_currentImageIndex];
                spot.RefreshImage();
                var image = CreateBoundImageControl(spot);

                _currentProjectionWindow.Content = image;

                // Move to the next image, looping back to the first after the last
                _currentImageIndex = (_currentImageIndex + 1) % _currentSpots.Count;
            });
        }

        private (double scaleX, double scaleY) GetDpiScaling()
        {
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                double dpiX = graphics.DpiX;
                double dpiY = graphics.DpiY;
                return (dpiX / 96.0, dpiY / 96.0); // WPF Default DPI 96
            }
        }

        private Window CreateProjectionWindow(Screen screen)
        {
            var (scaleX, scaleY) = GetDpiScaling();
            return new Window
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Normal,
                Width = screen.Bounds.Width / scaleX,
                Height = screen.Bounds.Height / scaleY,
                Left = screen.Bounds.X / scaleX,
                Top = screen.Bounds.Y / scaleY,
            };
        }

        private TextBlock CreateOverlayTextBlock(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = System.Windows.Media.Brushes.White,
                Background = new SolidColorBrush(Colors.Black) { Opacity = 0.5 },
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
        }

        public void StopProjection()
        {
            foreach (var window in _projectionWindows)
            {
                window.Closed -= CurrentProjectionWindow_Closed; // Unsubscribe to avoid memory leaks
                window.Close();
            }
            _projectionWindows.Clear();

            // Ensure the timer is stopped to prevent memory leaks
            _imageRefreshTimer.Stop();
        }

        private System.Windows.Controls.Image CreateBoundImageControl(Spot spot)
        {
            var image = new System.Windows.Controls.Image
            {
                Stretch = Stretch.UniformToFill

            };

            var binding = new System.Windows.Data.Binding("ImageSource")
            {
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = spot, // Set the source of the binding to the spot object
            };

            image.SetBinding(System.Windows.Controls.Image.SourceProperty, binding);
            return image;
        }
    }
}
