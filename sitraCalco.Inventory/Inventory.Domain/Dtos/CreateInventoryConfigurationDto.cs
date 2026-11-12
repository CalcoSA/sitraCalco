using System.Text.Json.Serialization;

namespace Inventory.Domain.Dtos
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class CreateInventoryConfigurationDto
    {
        public string InventoryConfigurationName { get; set; } = null!;

        public int? StartDay { get; set; }

        public int? EndDay { get; set; }
    }
}
