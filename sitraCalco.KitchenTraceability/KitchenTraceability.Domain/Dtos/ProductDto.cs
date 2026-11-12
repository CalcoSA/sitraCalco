namespace KitchenTraceability.Domain.Dtos
{
    public class ProductDto
    {
        public long product_id { get; set; }
        public string product_name { get; set; } = null!;
        public string product_reference { get; set; } = null!;
        public string unit_of_measure { get; set; } = null!;
        public ProductReceptionConfigurationDto? product_reception_configuration { get; set; }
        public ProductTemperatureConfigurationDto? product_temperature_configuration { get; set; }
    }
}