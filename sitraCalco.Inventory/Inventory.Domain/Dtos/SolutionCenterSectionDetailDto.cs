namespace Inventory.Domain.Dtos
{
    public class SolutionCenterSectionDetailDto
    {
        public long SectionId { get; set; }

        public string SectionName { get; set; } = null!;

        public bool IsActive { get; set; }

        public IEnumerable<SolutionCenterProductDetailDto> Products
        { get; set; }
            = new List<SolutionCenterProductDetailDto>();
    }
}