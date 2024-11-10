using System.Text.Json.Serialization;

namespace Challenge.DTO
{
    public class CountryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }
    }
}
