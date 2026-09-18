namespace Inventory.Domain.Dtos
{
    public class SolutionCenterInventoryConfigurationsDto
    {
        public long SolutionCenterId { get; set; }

        public long SolutionCenterTypeId { get; set; }

        public string SolutionCenterTypeName { get; set; } = null!;

        public string SolutionCenterCode { get; set; } = null!;

        public string SolutionCenterName { get; set; } = null!;

        public bool IsActive { get; set; }

        public List<InventoryConfigurationDetailDto>
            Configurations
        { get; set; }
                = new List<InventoryConfigurationDetailDto>();
    }
}