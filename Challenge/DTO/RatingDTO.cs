using System.Text.Json.Serialization;

namespace Challenge.DTO
{
    public class RatingDto
    {
        [JsonPropertyName("average")]
        public double? Average { get; set; }
    }
}
