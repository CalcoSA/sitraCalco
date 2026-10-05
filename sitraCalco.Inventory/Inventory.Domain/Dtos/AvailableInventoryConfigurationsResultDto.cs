namespace Inventory.Domain.Dtos
{
    public class AvailableInventoryConfigurationsResultDto
    {
        public bool IsValidRole { get; set; }

        public bool IsAllowed { get; set; }

        // Null indica que el centro no existe; una lista vacía indica que no hay disponibilidad.
        public List<AvailableInventoryConfigurationDto>? Data { get; set; }
    }
}
