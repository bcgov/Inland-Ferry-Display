using System.Text.Json.Serialization;

namespace FerryDisplayApp.Models
{
    public class Spot
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
        public string ImageUrl => $"https://images.drivebc.ca/webcam/api/v1/webcams/{Id}/imageDisplay";
        public string ImageSource => $"https://images.drivebc.ca/webcam/api/v1/webcams/{Id}/imageSource";
    }
}
