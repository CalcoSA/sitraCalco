namespace Inventory.Domain.Dtos
{
    public class CreateSectionConfigurationDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("id_section")]
        public long? IdSection { get; set; }

        public string? SectionName { get; set; }

        public List<SectionProductDto> Products { get; set; }
            = new List<SectionProductDto>();
    }
}
