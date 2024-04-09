using FerryDisplayApp.Helpers;
using FerryDisplayApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace FerryDisplayApp.Views
{
    public partial class ManageFerriesWindow : Window
    {
        private const string JsonFilePath = "C:/Users/isean/Projects/FerryImageDisplayApp/FerryImageDisplayApp/Resources/FerriesListTest.json";
        private Dictionary<string, Ferry> _ferries;
        public ObservableCollection<Ferry> Ferries { get; set; } = new ObservableCollection<Ferry>(); // to be accessed by UI

        public ManageFerriesWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadFerries();
        }

        private void LoadFerries_Click(object sender, RoutedEventArgs e)
        {
            LoadFerries();
        }

        private void LoadFerries()
        {
            try
            {
                var ferriesList = LoadFerries(JsonFilePath); // This now returns a List<Ferry>
                Ferries.Clear(); // Clear the existing collection

                foreach (var ferry in ferriesList)
                {
                    Ferries.Add(ferry); // Add each ferry to the ObservableCollection
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load ferries: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void AddNewFerry_Click(object sender, RoutedEventArgs e)
        {
            var newFerry = new Ferry
            {
                Name = string.Empty,
                RegionUrl = string.Empty,
                Spots = new List<Spot>() // Ensure Spots is initialized
            };
            Ferries.Add(newFerry);
            FerriesDataGrid.SelectedItem = newFerry;
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize(Ferries.ToList(), options);
                JsonFileHelper.WriteJsonFile(jsonString);

                MessageBox.Show("Changes saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public static List<Ferry> LoadFerries(string filePath)
        {
            string jsonString = JsonFileHelper.ReadJsonFile();
            var ferriesList = JsonSerializer.Deserialize<List<Ferry>>(jsonString);
            return ferriesList ?? new List<Ferry>(); // Return an empty list if null to avoid null reference issues
        }
    }
}