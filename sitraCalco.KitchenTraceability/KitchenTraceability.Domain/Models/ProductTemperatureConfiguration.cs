namespace KitchenTraceability.Domain.Models
{
    public partial class ProductTemperatureConfiguration
    {
        public long temperature_configuration_id { get; set; }
        public long product_id { get; set; }
        public decimal ideal_min_temperature { get; set; }
        public decimal ideal_max_temperature { get; set; }
        public decimal? conditional_min_temperature { get; set; }
        public decimal? conditional_max_temperature { get; set; }
        public decimal? rejection_min_temperature { get; set; }
        public decimal? rejection_max_temperature { get; set; }
        public DateTime created_at { get; set; }
        public string created_by { get; set; } = null!;
        public DateTime? updated_at { get; set; }
        public string? updated_by { get; set; }
        public virtual Product product { get; set; } = null!;
    }
}