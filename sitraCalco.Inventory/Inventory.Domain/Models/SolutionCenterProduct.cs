namespace Inventory.Domain.Models
{
    public partial class SolutionCenterProduct
    {
        public long solution_center_product_id { get; set; }

        public long solution_center_id { get; set; }

        public long section_id { get; set; }

        public long product_id { get; set; }

        public int sort_order { get; set; }

        public string created_by { get; set; } = null!;

        public DateTime created_at { get; set; }
    }
}