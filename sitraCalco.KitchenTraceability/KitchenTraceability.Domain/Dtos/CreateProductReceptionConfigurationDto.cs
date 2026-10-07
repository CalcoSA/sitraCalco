namespace KitchenTraceability.Domain.Dtos
{
    public class CreateProductReceptionConfigurationDto
    {
        public long product_id { get; set; }
        public int? minimum_shelf_life_days { get; set; }
    }
}