namespace Inventory.Domain.Dtos
{
    public class InventoryHistoryCountDto
    {
        public int CountNumber { get; set; }
        public decimal? CountValue { get; set; }
        public decimal? Open { get; set; }
        public decimal? Closed { get; set; }
        public decimal? MultiplicationValue { get; set; }
        public string EnteredBy { get; set; } = null!;
    }
}