using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FerryDisplayApp.Models
{
    public class Spot : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
        public string ImageUrl => $"https://images.drivebc.ca/webcam/api/v1/webcams/{Id}/imageDisplay";
        public string BaseImageUrl => $"https://images.drivebc.ca/webcam/api/v1/webcams/{Id}/imageSource";
        private long _timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Modify ImageSource to use the current _timestamp
        public string ImageSource
        {
            get => $"{BaseImageUrl}?timestamp={_timestamp}";
            set
            {
                _timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                OnPropertyChanged(nameof(ImageSource));
            }
        }

        // Method to update the timestamp, triggering a change in ImageSource
        public void RefreshImage()
        {
            _timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            OnPropertyChanged(nameof(ImageSource));
        }
    }
}
