namespace Inventory.Domain.Dtos
{
    public class CreateInventoryDto
    {
        public string? InventoryExecutionId { get; set; }
        public long SolutionCenterId { get; set; }
        public long InventoryConfigurationId { get; set; }
        public long SectionId { get; set; }
        public int CountNumber { get; set; }
        public string? EnteredBy { get; set; }
        public List<CreateInventoryItemDto?>? Items { get; set; }
    }
}
