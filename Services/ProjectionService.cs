using FerryDisplayApp.Enums;
using FerryDisplayApp.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms; // For Screen
using System.Windows.Threading;

namespace FerryDisplayApp.Services
{
    public class ProjectionService
    {
        private static ProjectionService _instance;
        private readonly List<Spot> _spots = new();
        private readonly List<Window> _projectionWindows = new();
        private readonly DispatcherTimer _imageRefreshTimer = new() { Interval = TimeSpan.FromMinutes(5) };
        private readonly DispatcherTimer _rotationTimer = new() { Interval = TimeSpan.FromSeconds(3) };

        private Window _currentProjectionWindow;
        private int _currentImageIndex;
        private ProjectionMode _currentMode;

        public static ProjectionService Instance => _instance ??= new ProjectionService();

        private ProjectionService()
        {
            _imageRefreshTimer.Tick += (_, _) => UpdateProjection();
            _rotationTimer.Tick += (_, _) => RotateImage();
            _imageRefreshTimer.Start();
        }

        public void StartProjection(List<Spot> spots, Screen screen, ProjectionMode mode)
        {
            _spots.Clear();
            _spots.AddRange(spots);
            _currentMode = mode;

            _currentProjectionWindow = DisplayManager.CreateProjectionWindow(screen);
            _currentProjectionWindow.Closed += (_, _) => StopProjection(_currentProjectionWindow);
            _projectionWindows.Add(_currentProjectionWindow);

            _currentImageIndex = 0;
            DisplayManager.HandleModeDisplay(_currentProjectionWindow, _currentMode, _spots, ref _currentImageIndex, _rotationTimer);

            _currentProjectionWindow.Show();
        }

        private void UpdateProjection()
        {
            DisplayManager.UpdateProjection(_currentMode, _spots);
        }

        private void RotateImage()
        {
            DisplayManager.RotateImage(_currentMode, _spots, ref _currentImageIndex, _currentProjectionWindow);
        }

        public void StopProjection(Window window = null)
        {
            DisplayManager.StopProjection(window, _projectionWindows, _imageRefreshTimer, _rotationTimer);
        }
    }
}
