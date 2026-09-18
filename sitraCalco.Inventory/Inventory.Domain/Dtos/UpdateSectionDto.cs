namespace Inventory.Domain.Dtos
{
    public class UpdateSectionDto
    {
        public string SectionName { get; set; } = null!;

        public bool IsActive { get; set; }
    }
}