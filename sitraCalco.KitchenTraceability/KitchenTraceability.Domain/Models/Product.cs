namespace KitchenTraceability.Domain.Models
{
    public partial class Product
    {
        public long product_id { get; set; }
        public string product_name { get; set; } = null!;
        public string product_reference { get; set; } = null!;
        public string unit_of_measure { get; set; } = null!;
        public virtual ICollection<ProductReceptionConfiguration> productReceptionConfigurations { get; set; } = new List<ProductReceptionConfiguration>();
        public virtual ICollection<ProductTemperatureConfiguration> productTemperatureConfigurations { get; set; } = new List<ProductTemperatureConfiguration>();
    }
}