namespace Inventory.Domain.Dtos
{
    public class SolutionCenterDetailDto
    {
        public long SolutionCenterId { get; set; }

        public long SolutionCenterTypeId { get; set; }

        public string SolutionCenterTypeName { get; set; } = null!;

        public string SolutionCenterCode { get; set; } = null!;

        public string SolutionCenterName { get; set; } = null!;

        public bool IsActive { get; set; }

        public IEnumerable<SolutionCenterSectionDetailDto> Sections
        { get; set; }
            = new List<SolutionCenterSectionDetailDto>();
    }
}