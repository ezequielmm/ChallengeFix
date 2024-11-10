using System.Text.Json.Serialization;
using Challenge.DTOs;

namespace Challenge.DTO
{
    public class NetworkDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("country")]
        public CountryDto Country { get; set; }
    }
}
