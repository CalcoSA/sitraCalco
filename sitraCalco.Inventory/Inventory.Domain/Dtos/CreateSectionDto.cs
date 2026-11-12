namespace Inventory.Domain.Dtos
{
    public class CreateSectionDto
    {
        public string SectionName { get; set; } = null!;

        public long SolutionCenterTypeId { get; set; }
    }
}
