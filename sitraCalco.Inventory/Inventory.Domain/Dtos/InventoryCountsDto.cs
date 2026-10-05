namespace Inventory.Domain.Dtos
{
    public class InventoryCountsDto
    {
        public string InventoryExecutionId { get; set; } = null!;
        public long SolutionCenterId { get; set; }
        public long SolutionCenterTypeId { get; set; }
        public string SolutionCenterCode { get; set; } = null!;
        public string SolutionCenterName { get; set; } = null!;
        public long InventoryConfigurationId { get; set; }
        public string InventoryConfigurationName { get; set; } = null!;
        public long SectionId { get; set; }
        public string SectionName { get; set; } = null!;
        public string CountMode { get; set; } = null!;
        public List<InventoryProductCountsDto> Items { get; set; } = new();
    }
}
