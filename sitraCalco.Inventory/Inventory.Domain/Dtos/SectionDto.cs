namespace Inventory.Domain.Dtos
{
    public class SectionDto
    {
        public long SectionId { get; set; }

        public string SectionName { get; set; } = null!;

        public bool IsActive { get; set; }
    }
}