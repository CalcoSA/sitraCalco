namespace Inventory.Domain.Models
{
    public partial class InventoryConfiguration
    {
        public long inventory_configuration_id { get; set; }

        public string inventory_configuration_name { get; set; } = null!;

        public int start_day { get; set; }

        public int end_day { get; set; }
    }
}