namespace Inventory.Domain.Dtos
{
    public class InventoryExecutionContextDto
    {
        public long SolutionCenterId { get; set; }
        public long InventoryConfigurationId { get; set; }
        public long SectionId { get; set; }
        public byte CountNumber { get; set; }
    }
}
