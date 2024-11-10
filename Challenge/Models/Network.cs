using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Challenge.Models;

namespace Challenge.Models
{
    public class Network
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }


        [ForeignKey("Country")]
        public string CountryCode { get; set; }
        public Country Country { get; set; }

        [JsonIgnore] // Evita la serialización de la colección de Shows
        public ICollection<Show> Shows { get; set; } = new List<Show>();
    }

}
