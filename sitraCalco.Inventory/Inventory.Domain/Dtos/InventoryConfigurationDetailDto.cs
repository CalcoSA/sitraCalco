namespace Inventory.Domain.Dtos
{
    public class InventoryConfigurationDetailDto
    {
        public long InventoryConfigurationId { get; set; }

        public string InventoryConfigurationName { get; set; } = null!;

        public int? StartDay { get; set; }

        public int? EndDay { get; set; }

        public List<InventoryConfigurationDayDto> DaysOfWeek { get; set; } = new();

        public List<string> Days { get; set; }
            = new List<string>();

        public List<InventoryConfigurationSectionDto> Sections { get; set; }
            = new List<InventoryConfigurationSectionDto>();
    }
}
