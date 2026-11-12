namespace Inventory.Domain.Dtos
{
    public class AvailableInventoryConfigurationDto
    {
        public long InventoryConfigurationId { get; set; }

        public string InventoryConfigurationName { get; set; } = null!;
    }
}
