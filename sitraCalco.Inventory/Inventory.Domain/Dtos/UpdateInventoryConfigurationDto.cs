namespace Inventory.Domain.Dtos
{
    public class UpdateInventoryConfigurationDto
    {
        public string InventoryConfigurationName { get; set; } = null!;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}