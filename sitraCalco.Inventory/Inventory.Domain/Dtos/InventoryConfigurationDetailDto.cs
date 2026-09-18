namespace Inventory.Domain.Dtos
{
    public class InventoryConfigurationDetailDto
    {
        public long InventoryConfigurationId { get; set; }

        public string InventoryConfigurationName { get; set; } = null!;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public List<string> Days { get; set; }
            = new List<string>();

        public List<InventoryConfigurationSectionDto> Sections { get; set; }
            = new List<InventoryConfigurationSectionDto>();
    }
}