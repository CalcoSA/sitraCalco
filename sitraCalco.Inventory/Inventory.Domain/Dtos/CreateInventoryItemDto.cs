namespace Inventory.Domain.Dtos
{
    public class CreateInventoryItemDto
    {
        public long ProductId { get; set; }
        public decimal? CountValue { get; set; }
        public decimal? Open { get; set; }
        public decimal? Closed { get; set; }
        public decimal? MultiplicationValue { get; set; }
    }
}
