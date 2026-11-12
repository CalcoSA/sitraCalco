namespace Inventory.Domain.Dtos
{
    public class SectionSolutionCentersDto
    {
        public long SectionId { get; set; }

        public string SectionName { get; set; } = null!;

        public bool IsActive { get; set; }

        public List<SolutionCenterListDto> SolutionCenters { get; set; }
            = new List<SolutionCenterListDto>();
    }
}