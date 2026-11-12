namespace KitchenTraceability.Domain.Dtos
{
    public class ProductReceptionConfigurationDto
    {
        public long product_configuration_id { get; set; }
        public long product_id { get; set; }
        public int? minimum_shelf_life_days { get; set; }
        public bool? requires_guide { get; set; }
        public bool? requires_shelf_life { get; set; }
        public bool? requires_temperature { get; set; }
        public bool? requires_visual_inspection { get; set; }
        public DateTime created_at { get; set; }
        public string created_by { get; set; } = null!;
        public DateTime? updated_at { get; set; }
        public string? updated_by { get; set; }
    }
}