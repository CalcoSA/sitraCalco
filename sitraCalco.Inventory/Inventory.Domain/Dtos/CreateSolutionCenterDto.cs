namespace Inventory.Domain.Dtos
{
    public class CreateSolutionCenterDto
    {
        public long SolutionCenterTypeId { get; set; }

        public string SolutionCenterCode { get; set; } = null!;

        public string SolutionCenterName { get; set; } = null!;
    }
}