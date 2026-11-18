namespace Inventory.Domain.Dtos
{
    public class InventoryHistoryCountDto
    {
        public long InventoryId { get; set; }
        public int CountNumber { get; set; }
        public decimal? CountValue { get; set; }
        public decimal? Open { get; set; }
        public decimal? Closed { get; set; }
        public decimal? MultiplicationValue { get; set; }
        public decimal? SiesaStock { get; set; }
        public string? Observations { get; set; }
        public string EnteredBy { get; set; } = null!;
    }
}