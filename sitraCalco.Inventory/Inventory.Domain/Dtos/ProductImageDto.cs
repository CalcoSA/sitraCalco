using System.Text.Json.Serialization;

namespace Inventory.Domain.Dtos
{
    public class ProductImageDto
    {
        public long ProductId { get; set; }
        public string ImagePath { get; set; } = null!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ImageUrl { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}
