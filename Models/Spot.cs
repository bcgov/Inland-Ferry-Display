using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FerryDisplayApp.Models
{
    public class Spot : INotifyPropertyChanged
    {
        private int id;
        private string name;
        private long _timestamp;
        private string _lastModified;

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

        public string LastModified
        {
            get => _lastModified;
            set => SetProperty(ref _lastModified, value);
        }

        public string LastModifiedTime
        {
            get
            {
                if (DateTime.TryParse(LastModified, out DateTime dateTime))
                {
                    return dateTime.ToString("HH:mm:ss");
                }
                return string.Empty;
            }
        }

        public string rootUrl => $"https://images.drivebc.ca/webcam/api/v1/webcams/{Id}/";
        public string ImageUrl => $"{rootUrl}imageDisplay";
        public string BaseImageUrl => $"{rootUrl}imageSource";

        public string ImageSource
        {
            get => $"{BaseImageUrl}?timestamp={_timestamp}";
            set
            {
                _timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                OnPropertyChanged(nameof(ImageSource));
            }
        }

        public async Task RefreshImage()
        {
            Console.WriteLine("UPDATING");
            _timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await FetchImageDataAsync();
            OnPropertyChanged(nameof(ImageSource));
            OnPropertyChanged(nameof(LastModifiedTime));
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

        public async Task FetchImageDataAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                string url = rootUrl.TrimEnd('/');
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(jsonString);
                    if (data.TryGetProperty("imageStats", out JsonElement imageStats) &&
                        imageStats.TryGetProperty("lastModified", out JsonElement lastModified))
                    {
                        LastModified = lastModified.GetProperty("time").GetString();
                    }
                }
            }
        }
    }
}
