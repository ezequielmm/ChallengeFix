using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Challenge.Models;

namespace Challenge.Models
{
    public class Genre
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonIgnore] // Evita la serialización de la colección de Shows
        public ICollection<Show> Shows { get; set; } = new List<Show>();
    }
}
