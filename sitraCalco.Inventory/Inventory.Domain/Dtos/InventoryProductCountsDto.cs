namespace Inventory.Domain.Dtos
{
    public class InventoryProductCountsDto
    {
        public long ProductId { get; set; }
        public List<InventoryCountDto> Counts { get; set; } = new();
    }
}
