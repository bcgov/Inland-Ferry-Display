using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FerryDisplayApp.Models
{

    public class Ferry
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("regionUrl")]
        public string? RegionUrl { get; set; }

        [JsonPropertyName("spots")]
        public List<Spot>? Spots { get; set; }
    }

}
