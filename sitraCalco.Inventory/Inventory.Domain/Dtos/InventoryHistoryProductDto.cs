namespace Inventory.Domain.Dtos
{
    public class InventoryHistoryProductDto
    {
        public string Reference { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string UnitOfMeasure { get; set; } = null!;
        public List<InventoryHistoryCountDto> Counts { get; set; } = new();
    }
}