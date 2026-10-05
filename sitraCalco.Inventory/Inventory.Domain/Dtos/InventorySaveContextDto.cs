using Inventory.Domain.Models;

namespace Inventory.Domain.Dtos
{
    // Datos oficiales internos para construir el snapshot; no forman parte del request HTTP.
    public class InventorySaveContextDto
    {
        public SolutionCenter SolutionCenter { get; set; } = null!;
        public InventoryConfiguration Configuration { get; set; } = null!;
        public Section Section { get; set; } = null!;
    }
}
