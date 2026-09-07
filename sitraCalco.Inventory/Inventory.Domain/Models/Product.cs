namespace Inventory.Domain.Models
{
    public partial class Product
    {
        public long product_id { get; set; }

        public string product_name { get; set; } = null!;

        public string reference { get; set; } = null!;

        public string unit_of_measure { get; set; } = null!;

        public string? plan_id { get; set; }

        public string? image_path { get; set; }
    }
}
