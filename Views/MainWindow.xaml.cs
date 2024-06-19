using FerryDisplayApp.Enums;
using FerryDisplayApp.Helpers;
using FerryDisplayApp.Models;
using FerryDisplayApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FerryDisplayApp.Views
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly ProjectionService _projectionService;
        private const int WM_DISPLAYCHANGE = 0x7E;

        public MainWindow()
        {
            InitializeComponent();
            LoadFerriesData();
            SetupDisplaySelection();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            this.Arrange(new Rect(0, 0, this.DesiredSize.Width, this.DesiredSize.Height));

            this.MinWidth = this.DesiredSize.Width;
            this.MinHeight = this.DesiredSize.Height;
        }
        private void SetupDisplaySelection()
        {
            DisplaySelectionComboBox.ItemsSource = Screen.AllScreens.Select(screen => screen.DeviceName).ToList();
            DisplayModeComboBox.ItemsSource = Enum.GetValues(typeof(ProjectionMode));
        }

        private void RefreshDisplays_Click(object sender, RoutedEventArgs e)
        {
            SetupDisplaySelection();
            System.Windows.MessageBox.Show("Display list refreshed.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DISPLAYCHANGE)
            {
                SetupDisplaySelection();
            }

            return IntPtr.Zero;
        }

        private void LoadFerriesData()
        {

            try
            {
                string jsonString = JsonFileHelper.ReadJsonFile();
                var ferriesList = JsonSerializer.Deserialize<List<Ferry>>(jsonString); // Deserialize directly into List<Ferry>
                ferriesList ??= new List<Ferry>();

                // Ensure that we handle potential null Spots within any ferry item
                var spots = ferriesList
                    .SelectMany(ferry => ferry.Spots ?? new List<Spot>()) // Provide a fallback empty list if Spots is null
                    .ToList();

                ImageList.ItemsSource = spots;
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException)
            {
                System.Windows.MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        private async void LoadImageAsync(string imageUrl)
        {
            try
            {
                var imageStream = await httpClient.GetStreamAsync(imageUrl);
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = imageStream;
                bitmapImage.EndInit();
                SpotImage.Source = bitmapImage;
                SpotImage.Stretch = Stretch.Uniform;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load image: {ex.Message}");
            }
        }

        // To preview selected image
        private void ImageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageList.SelectedItem is Spot selectedSpot)
            {
                LoadImageAsync(selectedSpot.ImageUrl);
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e) => ImageList.SelectAll();

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedSpots = ImageList.SelectedItems.Cast<Spot>().ToList();
            if (!selectedSpots.Any())
            {
                System.Windows.MessageBox.Show("Please select one or more images.");
                return;
            }

            int selectedDisplayIndex = DisplaySelectionComboBox.SelectedIndex;
            if (selectedDisplayIndex < 0 || selectedDisplayIndex >= Screen.AllScreens.Length)
            {
                System.Windows.MessageBox.Show("Invalid display selection.");
                return;
            }

            if (DisplayModeComboBox.SelectedItem is null)
            {
                System.Windows.MessageBox.Show("Please select a display mode.");
                return;
            }

            ProjectionMode selectedMode = (ProjectionMode)DisplayModeComboBox.SelectedItem;
            Screen selectedScreen = Screen.AllScreens[selectedDisplayIndex];

            ProjectionService.Instance.StartProjection(selectedSpots, selectedScreen, selectedMode);
        }

        private void CloseProjections_Click(object sender, RoutedEventArgs e) => ProjectionService.Instance.StopProjection();

        private void ManageFerries_Click(object sender, RoutedEventArgs e)
        {
            var manageWindow = new ManageFerriesWindow();
            manageWindow.FerriesUpdated += ManageWindow_FerriesUpdated;
            manageWindow.ShowDialog();
        }

        private void ManageWindow_FerriesUpdated(object sender, EventArgs e)
        {
            LoadFerriesData(); // Reload the data when the event is triggered
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }
    }
}
