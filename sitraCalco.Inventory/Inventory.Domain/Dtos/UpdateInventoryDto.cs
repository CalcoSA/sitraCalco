namespace Inventory.Domain.Dtos
{
    public class UpdateInventoryDto
    {
        public string? Observations { get; set; }
        public bool Append { get; set; } = false;
    }
}