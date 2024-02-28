using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FerryDisplayApp.Models;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Data;
using FerryDisplayApp.Services;
using FerryDisplayApp.Enums;

namespace FerryDisplayApp.Views
{
    public partial class MainWindow : Window
    {
        private readonly ProjectionService _projectionService;
        private const int WM_DISPLAYCHANGE = 0x7E;
        private readonly HttpClient httpClient = new HttpClient();
        private DispatcherTimer _imageRefreshTimer;

        public MainWindow()
        {
            InitializeComponent();
            LoadFerriesData();
            _projectionService = new ProjectionService();
            DisplaySelectionComboBox.ItemsSource = Screen.AllScreens.Select(screen => screen.DeviceName).ToList();
            DisplayModeComboBox.ItemsSource = Enum.GetValues(typeof(ProjectionMode));
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
                DisplaySelectionComboBox.ItemsSource = Screen.AllScreens.Select(screen => screen.DeviceName).ToList();
            }

            return IntPtr.Zero;
        }

        private void LoadFerriesData()
        {
            string jsonFilePath = "Resources/FerriesList.json";
            if (!File.Exists(jsonFilePath))
            {
                System.Windows.MessageBox.Show("JSON file not found.");
                return;
            }

            try
            {
                string jsonString = File.ReadAllText(jsonFilePath);
                var ferriesList = JsonSerializer.Deserialize<Dictionary<string, Ferry>>(jsonString);
                ImageList.ItemsSource = ferriesList?.Values.SelectMany(ferry => ferry.Spots).ToList();
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException)
            {
                System.Windows.MessageBox.Show($"Error loading data: {ex.Message}");
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

        private async void LoadImageAsync(string imageUrl)
        {
            try
            {
                using (var response = await httpClient.GetStreamAsync(imageUrl))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = response;
                    bitmapImage.EndInit();
                    // bitmapImage.Freeze(); // immutability and thread-safe
                    SpotImage.Source = bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load image: {ex.Message}");
            }
        }

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

            var projectionService = new ProjectionService();

            projectionService.StartProjection(selectedSpots, selectedScreen, selectedMode);
        }

        private void CloseProjections_Click(object sender, RoutedEventArgs e)
        {
            _projectionService.StopProjection();
        }

        private System.Windows.Controls.Image CreateBoundImageControl(Spot spot)
        {
            var image = new System.Windows.Controls.Image();

            var binding = new System.Windows.Data.Binding("ImageSource")
            {
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = spot // Set the source of the binding to the spot object
            };

            image.SetBinding(System.Windows.Controls.Image.SourceProperty, binding);
            return image;
        }

    }
}
