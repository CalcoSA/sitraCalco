namespace Inventory.Domain.Dtos
{
    public class InventoryHistoryDto
    {
        public DateOnly Date { get; set; }
        public string InventoryExecutionId { get; set; } = null!;
        public List<InventoryHistoryProductDto> Products { get; set; } = new();
    }
}