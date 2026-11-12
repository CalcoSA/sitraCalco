namespace KitchenTraceability.Domain.Dtos
{
    public class UpdateProductReceptionConfigurationDto
    {
        public int? minimum_shelf_life_days { get; set; }
        public bool? requires_guide { get; set; }
        public bool? requires_shelf_life { get; set; }
        public bool? requires_temperature { get; set; }
        public bool? requires_visual_inspection { get; set; }
    }
}