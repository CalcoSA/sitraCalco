namespace Inventory.Domain.Models
{
    public partial class InventoryConfigurationDay
    {
        public long inventory_configuration_day_id { get; set; }

        public long inventory_configuration_id { get; set; }

        public string day_of_week { get; set; } = null!;
    }
}