namespace Inventory.Domain.Dtos
{
    public class InventorySaveDto
    {
        public string InventoryExecutionId { get; set; } = null!;
        public int CountNumber { get; set; }
        public int SavedItems { get; set; }
    }
}
