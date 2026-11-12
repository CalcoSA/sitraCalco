namespace KitchenTraceability.Domain.Dtos
{
    public class UpdateProductTemperatureConfigurationDto
    {
        public decimal ideal_min_temperature { get; set; }
        public decimal ideal_max_temperature { get; set; }
        public decimal? conditional_min_temperature { get; set; }
        public decimal? conditional_max_temperature { get; set; }
        public decimal? rejection_min_temperature { get; set; }
        public decimal? rejection_max_temperature { get; set; }
    }
}