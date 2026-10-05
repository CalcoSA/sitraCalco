namespace Inventory.Domain.Models
{
    public partial class InventoryRecord
    {
        public long inventory_id { get; set; }

        public string inventory_execution_id { get; set; } = null!;

        public long solution_center_id { get; set; }

        public long solution_center_type_id { get; set; }

        public string solution_center_code { get; set; } = null!;

        public string solution_center_name { get; set; } = null!;

        public long inventory_configuration_id { get; set; }

        public string inventory_configuration_name { get; set; } = null!;

        public long section_id { get; set; }

        public string section_name { get; set; } = null!;

        public long product_id { get; set; }

        public string reference { get; set; } = null!;

        public string product_name { get; set; } = null!;

        public string unit_of_measure { get; set; } = null!;

        public string? plan_id { get; set; }

        public byte count_number { get; set; }

        public decimal? count_value { get; set; }

        public decimal? open { get; set; }

        public decimal? closed { get; set; }

        public decimal? multiplication_value { get; set; }

        public string entered_by { get; set; } = null!;

        public DateTime created_at { get; set; }
    }
}
