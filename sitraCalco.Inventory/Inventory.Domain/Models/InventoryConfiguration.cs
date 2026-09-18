namespace Inventory.Domain.Models
{
    public partial class InventoryConfiguration
    {
        public long inventory_configuration_id { get; set; }

        public string inventory_configuration_name { get; set; } = null!;

        public DateTime? start_date { get; set; }

        public DateTime? end_date { get; set; }
    }
}