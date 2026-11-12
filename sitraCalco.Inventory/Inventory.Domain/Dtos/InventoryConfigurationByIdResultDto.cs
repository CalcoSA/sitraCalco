namespace Inventory.Domain.Dtos
{
    public class InventoryConfigurationByIdResultDto
    {
        public bool IsValidRole { get; set; }

        public InventoryConfigurationByIdDto?
            Data
        { get; set; }
    }
}