namespace Inventory.Domain.Dtos
{
    public class SiesaExistenceDto
    {
        public string Reference { get; set; } = string.Empty;
        public string UnitInventory { get; set; } = string.Empty;
        public decimal? Existence1 { get; set; }
        public decimal? Existence2 { get; set; }
    }
}