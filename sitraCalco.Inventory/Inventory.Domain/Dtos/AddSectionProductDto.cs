namespace Inventory.Domain.Dtos
{
    public class AddSectionProductDto
    {
        public long ProductId { get; set; }

        public int Position { get; set; }

        public string CreatedBy { get; set; } = null!;
    }
}