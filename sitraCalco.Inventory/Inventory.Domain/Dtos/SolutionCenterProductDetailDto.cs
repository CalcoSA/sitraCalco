namespace Inventory.Domain.Dtos
{
    public class SolutionCenterProductDetailDto
    {
        public long SolutionCenterProductId { get; set; }

        public long ProductId { get; set; }

        public string ProductName { get; set; } = null!;

        public string Reference { get; set; } = null!;

        public string UnitOfMeasure { get; set; } = null!;

        public string? PlanId { get; set; }

        public int SortOrder { get; set; }

        public string CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}