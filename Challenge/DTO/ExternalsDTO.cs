﻿using System.Text.Json.Serialization;

namespace Challenge.DTO
{
    public class ExternalsDto
    {
        [JsonPropertyName("tvrage")]
        public int? Tvrage { get; set; }

        [JsonPropertyName("thetvdb")]
        public int? Thetvdb { get; set; }

        [JsonPropertyName("imdb")]
        public string Imdb { get; set; }
    }
}
