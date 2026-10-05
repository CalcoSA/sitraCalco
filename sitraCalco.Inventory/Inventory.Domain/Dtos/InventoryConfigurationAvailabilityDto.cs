namespace Inventory.Domain.Dtos
{
    // Datos internos para evaluar disponibilidad; no se exponen en el desplegable.
    public class InventoryConfigurationAvailabilityDto
    {
        public long InventoryConfigurationId { get; set; }

        public string InventoryConfigurationName { get; set; } = null!;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public List<string> Days { get; set; } = new List<string>();
    }
}
