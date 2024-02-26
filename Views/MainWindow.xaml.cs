using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FerryDisplayApp.Models;
using System.Windows.Forms;
using System.Drawing;

namespace FerryDisplayApp.Views
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            LoadFerriesData();
            DisplaySelectionComboBox.ItemsSource = GetDisplayNames();
        }

        private void ImageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageList.SelectedItem is Spot selectedSpot)
            {
                LoadImage(selectedSpot.ImageUrl);
            }
        }

        // Populates ImageList.ItemsSource
        private void LoadFerriesData()
        {
            try
            {
                string jsonFilePath = "Resources/FerriesList.json";
                if (!File.Exists(jsonFilePath))
                {
                    System.Windows.Forms.MessageBox.Show("JSON file not found.");
                    return;
                }

                string jsonString = File.ReadAllText(jsonFilePath);
                var ferriesList = JsonSerializer.Deserialize<Dictionary<string, Ferry>>(jsonString);

                var allSpots = ferriesList.Values.SelectMany(ferry => ferry.Spots).ToList();
                ImageList.ItemsSource = allSpots;

            }
            catch (JsonException jsonEx)
            {
                System.Windows.Forms.MessageBox.Show($"JSON Error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"An error occurred: {ex.Message}");
            }

        }

        // Used for giving previews before projection
        private async void LoadImage(string imageUrl)
        {
            try
            {
                var imageStream = await _httpClient.GetStreamAsync(imageUrl);
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = imageStream;
                bitmapImage.EndInit();
                SpotImage.Source = bitmapImage;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to load image: {ex.Message}");
            }
        }

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedSpots = ImageList.SelectedItems.Cast<Spot>().ToList();
            if (!selectedSpots.Any())
            {
                System.Windows.Forms.MessageBox.Show("Please select one or more images.");
                return;
            }

            int selectedDisplayIndex = DisplaySelectionComboBox.SelectedIndex;
            if (selectedDisplayIndex < 0 || selectedDisplayIndex >= Screen.AllScreens.Length)
            {
                System.Windows.Forms.MessageBox.Show("Invalid display selection.");
                return;
            }

            string displayMode = ((ComboBoxItem)DisplayModeComboBox.SelectedItem)?.Content.ToString();
            Screen selectedScreen = Screen.AllScreens[selectedDisplayIndex];

            switch (displayMode)
            {
                case "Grid":
                    ProjectImagesInGrid(selectedSpots, selectedScreen);
                    break;
                case "Rotate":
                    ProjectImagesRotating(selectedSpots, selectedScreen);
                    break;
                default:
                    System.Windows.Forms.MessageBox.Show("Please select a display mode.");
                    break;
            }
        }

        // UNUSED: Display single image
        private void ProjectImageToDisplay(string imageUrl, Screen screen)
        {
            var (scaleX, scaleY) = GetDpiScaling();

            var imageWindow = new Window
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Title = "Image Projection",
                // Adjust for DPI
                Width = screen.Bounds.Width / scaleX,
                Height = screen.Bounds.Height / scaleY,
                Left = screen.Bounds.X / scaleX,
                Top = screen.Bounds.Y / scaleY,
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Normal, // Changed to Normal to manually set size and location
                Background = new ImageBrush(new BitmapImage(new Uri(imageUrl, UriKind.Absolute)))
            };

            imageWindow.Show();
        }

        private void ProjectImagesInGrid(List<Spot> spots, Screen screen)
        {
            var (scaleX, scaleY) = GetDpiScaling();

            // Equally subdivide grids based on N number of spots
            int gridSize = (int)Math.Ceiling(Math.Sqrt(spots.Count));
            var uniformGrid = new System.Windows.Controls.Primitives.UniformGrid
            {
                Rows = gridSize,
                Columns = gridSize
            };

            // Add images to the grid linearly
            foreach (var spot in spots)
            {
                var bitmapImage = new BitmapImage(new Uri(spot.ImageSource, UriKind.Absolute));
                var image = new System.Windows.Controls.Image { Source = bitmapImage, Stretch = Stretch.UniformToFill };
                uniformGrid.Children.Add(image);
            }

            ShowProjectionWindow(screen, scaleX, scaleY, uniformGrid);
        }


        private void ProjectImagesRotating(List<Spot> spots, Screen screen)
        {
            var (scaleX, scaleY) = GetDpiScaling();

            var imageControl = new System.Windows.Controls.Image { Stretch = Stretch.UniformToFill };
            if (spots.Any())
            {
                imageControl.Source = new BitmapImage(new Uri(spots[0].ImageUrl, UriKind.Absolute));
            }

            // Start from the second image for rotation as the first one is already displayed
            int currentIndex = 1;
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15) // Probably need to CONST this
            };
            timer.Tick += (sender, args) =>
            {
                if (currentIndex >= spots.Count) currentIndex = 0;
                imageControl.Source = new BitmapImage(new Uri(spots[currentIndex].ImageUrl, UriKind.Absolute));
                currentIndex++;
            };
            timer.Start();

            ShowProjectionWindow(screen, scaleX, scaleY, imageControl);
        }


        private (double scaleX, double scaleY) GetDpiScaling()
        {
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                double dpiX = graphics.DpiX;
                double dpiY = graphics.DpiY;
                return (dpiX / 96.0, dpiY / 96.0); // WPF default DPI is 96
            }
        }

        private void ShowProjectionWindow(Screen screen, double scaleX, double scaleY, UIElement content)
        {
            var window = new Window
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Title = "Image Projection",
                Width = screen.Bounds.Width / scaleX,
                Height = screen.Bounds.Height / scaleY,
                Left = screen.Bounds.X / scaleX,
                Top = screen.Bounds.Y / scaleY,
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Normal,
                Content = content
            };

            window.Show();
        }

        public List<string> GetDisplayNames()
        {
            List<string> displayNames = new List<string>();
            foreach (var screen in Screen.AllScreens)
            {
                displayNames.Add(screen.DeviceName);
            }
            return displayNames;
        }

    }
}
