namespace Inventory.Domain.Dtos
{
    public class InventoryConfigurationSectionDto
    {
        public long SectionId { get; set; }

        public string SectionName { get; set; } = null!;

        // Estado de la sección en sitracalco_inventory_section.
        public bool SectionIsActive { get; set; }

        // Estado de la asociación dentro de esta
        // configuración de inventario.
        public bool IsActive { get; set; }
    }
}