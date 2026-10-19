namespace Inventory.Domain.Dtos
{
    public class SectionDto
    {
        public long SectionId { get; set; }

        public long SolutionCenterTypeId { get; set; }

        // Las consultas operativas que reutilizan este DTO conservan su contrato si no cargan el nombre.
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? SolutionCenterTypeName { get; set; }

        public string SectionName { get; set; } = null!;

        public bool IsActive { get; set; }
    }
}
