namespace Inventory.Domain.Dtos
{
    public class SolutionCenterInventoryConfigurationsResultDto
    {
        public bool IsValidRole { get; set; }

        public bool IsAllowed { get; set; }

        public SolutionCenterInventoryConfigurationsDto?
            Data
        { get; set; }
    }
}