using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Challenge.Models;

namespace Challenge.Models
{
    public class Externals
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [ForeignKey("Show")]
        public int Id { get; set; }

        [JsonPropertyName("imdb")]
        public string? Imdb { get; set; }

        [JsonPropertyName("tvrage")]
        public int? Tvrage { get; set; }

        [JsonPropertyName("thetvdb")]
        public int? Thetvdb { get; set; }

        [JsonIgnore] // Evita la serialización de la colección de Shows
        public Show Show { get; set; }
    }
}
