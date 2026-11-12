namespace Inventory.Domain.Dtos
{
    public class InventoryCountDto
    {
        public int CountNumber { get; set; }
        public decimal? Value { get; set; }
        public decimal? Open { get; set; }
        public decimal? Closed { get; set; }
        public decimal? MultiplicationValue { get; set; }
    }
}
