using System;
using System.Windows;
using FerryDisplayApp.Services;
using FerryImageDisplayApp.Properties;

namespace FerryDisplayApp.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            // Load the current settings into the TextBoxes
            RotationTimerTextBox.Text = FerryImageDisplayApp.Properties.Settings.Default.RotationTimer.ToString();
            RefreshRateTextBox.Text = FerryImageDisplayApp.Properties.Settings.Default.RefreshRate.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(RotationTimerTextBox.Text, out int rotationSeconds) && 
                int.TryParse(RefreshRateTextBox.Text, out int refreshMinutes))
            {
                FerryImageDisplayApp.Properties.Settings.Default.RotationTimer = rotationSeconds;
                FerryImageDisplayApp.Properties.Settings.Default.RefreshRate = refreshMinutes;
                FerryImageDisplayApp.Properties.Settings.Default.Save();

                ProjectionService.Instance.SetRotationTimer(TimeSpan.FromSeconds(rotationSeconds));
                ProjectionService.Instance.SetRefreshRate(TimeSpan.FromMinutes(refreshMinutes));

                MessageBox.Show("Settings saved successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            else
            {
                MessageBox.Show("Please enter valid numbers for the settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
