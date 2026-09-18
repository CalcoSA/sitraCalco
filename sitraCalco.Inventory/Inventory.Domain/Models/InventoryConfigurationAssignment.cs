namespace Inventory.Domain.Models
{
    public partial class InventoryConfigurationAssignment
    {
        public long inventory_configuration_assignment_id { get; set; }

        public long inventory_configuration_id { get; set; }

        public long solution_center_id { get; set; }

        public long section_id { get; set; }

        public bool is_active { get; set; }
    }
}