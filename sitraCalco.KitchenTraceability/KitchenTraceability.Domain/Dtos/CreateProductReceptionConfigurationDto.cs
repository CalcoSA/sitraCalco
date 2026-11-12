namespace KitchenTraceability.Domain.Dtos
{
    public class CreateProductReceptionConfigurationDto
    {
        public long product_id { get; set; }
        public int? minimum_shelf_life_days { get; set; }
        public bool? requires_guide { get; set; }
        public bool? requires_shelf_life { get; set; }
        public bool? requires_temperature { get; set; }
        public bool? requires_visual_inspection { get; set; }
    }
}