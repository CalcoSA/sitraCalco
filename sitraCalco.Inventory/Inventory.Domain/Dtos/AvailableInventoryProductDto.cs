namespace Inventory.Domain.Dtos
{
    public class AvailableInventoryProductDto
    {
        public long SolutionCenterProductId { get; set; }

        public long ProductId { get; set; }

        public string? ImagePath { get; set; }

        public string? ImageUrl { get; set; }

        public string Reference { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public string UnitOfMeasure { get; set; } = null!;

        public string? PlanId { get; set; }

        public int SortOrder { get; set; }
    }
}
