using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FerryDisplayApp.Models
{
    public class Spot : INotifyPropertyChanged
    {
        private int id;
        private string name;
        private long _timestamp;

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonPropertyName("id")]
        public int Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        [JsonPropertyName("name")]
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string rootUrl => $"https://images.drivebc.ca/webcam/api/v1/webcams/{Id}/";
        public string ImageUrl => $"{rootUrl}imageDisplay";
        public string BaseImageUrl => $"{rootUrl}imageSource";

        // Method to update the timestamp, triggering a change in ImageSource
        public string ImageSource
        {
            get => $"{BaseImageUrl}?timestamp={_timestamp}";
            set
            {
                _timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                OnPropertyChanged(nameof(ImageSource));
            }
        }
        public void RefreshImage()
        {
            _timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            OnPropertyChanged(nameof(ImageSource));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}