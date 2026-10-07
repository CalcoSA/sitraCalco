namespace KitchenTraceability.Domain.Models
{
    public partial class ProductReceptionConfiguration
    {
        public long product_configuration_id { get; set; }
        public long product_id { get; set; }
        public int? minimum_shelf_life_days { get; set; }
        public DateTime created_at { get; set; }
        public string created_by { get; set; } = null!;
        public DateTime? updated_at { get; set; }
        public string? updated_by { get; set; }
        public virtual Product product { get; set; } = null!;
    }
}