namespace Inventory.Domain.Dtos
{
    public class CreateSectionConfigurationDto
    {
        public string SectionName { get; set; } = null!;

        public string CreatedBy { get; set; } = null!;

        public List<SectionProductDto> Products { get; set; }
            = new List<SectionProductDto>();
    }
}