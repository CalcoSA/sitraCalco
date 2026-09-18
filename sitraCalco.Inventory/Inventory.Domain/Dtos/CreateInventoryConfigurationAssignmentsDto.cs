namespace Inventory.Domain.Dtos
{
    public class CreateInventoryConfigurationAssignmentsDto
    {
        public List<CreateInventoryConfigurationAssignmentDto>
            Assignments
        { get; set; }
                = new List<CreateInventoryConfigurationAssignmentDto>();
    }
}