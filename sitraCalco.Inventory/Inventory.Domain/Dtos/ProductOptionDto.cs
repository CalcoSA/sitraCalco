namespace Inventory.Domain.Dtos
{
    public class ProductOptionDto
    {
        public long ProductId { get; set; }

        public string ProductName { get; set; } = null!;

        public string Reference { get; set; } = null!;

        public string UnitOfMeasure { get; set; } = null!;

        public string? PlanId { get; set; }
    }
}