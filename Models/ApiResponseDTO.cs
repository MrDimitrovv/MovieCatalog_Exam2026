
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Text.Json.Serialization;
namespace MovieCatalogExam.Models

{
    internal class ApiResponseDTO
    {
        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("movie")]

        public MovieDTO Movie { get; set; } = new MovieDTO();

    }
}
