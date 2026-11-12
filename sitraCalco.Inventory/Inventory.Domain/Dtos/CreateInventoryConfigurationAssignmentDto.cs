using Inventory.Domain.Dtos;

namespace Inventory.Domain.Dtos
{
    public class CreateInventoryConfigurationAssignmentDto
    {
        public long SolutionCenterId { get; set; }

        public long SectionId { get; set; }
    }
}