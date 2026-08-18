using System.Text.Json.Serialization;

namespace Authentication.Domain.Dtos
{
    public class WordpressUserDto
    {
        public ulong WordpressUserId { get; set; }
        public string? WordpressUserLogin { get; set; }
        public string? WordpressDisplayName { get; set; }
        [JsonIgnore]
        public string? WordpressUserPass { get; set; }
    }
}