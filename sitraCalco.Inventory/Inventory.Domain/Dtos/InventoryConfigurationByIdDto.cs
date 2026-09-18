namespace Inventory.Domain.Dtos
{
    public class InventoryConfigurationByIdDto
    {
        public long InventoryConfigurationId { get; set; }

        public string InventoryConfigurationName { get; set; } = null!;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public List<string> Days { get; set; }
            = new List<string>();

        public List<InventoryConfigurationSolutionCenterDto>
            SolutionCenters
        { get; set; }
                = new List<InventoryConfigurationSolutionCenterDto>();
    }
}