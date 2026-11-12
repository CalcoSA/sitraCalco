namespace Inventory.Domain.Dtos
{
    // Estado funcional interno; el endpoint expone únicamente Data en las respuestas exitosas.
    public class AvailableInventorySectionsResultDto
    {
        public bool IsValidRole { get; set; }

        public bool IsAllowed { get; set; }

        public bool SolutionCenterExists { get; set; }

        public bool ConfigurationExists { get; set; }

        public bool IsAvailable { get; set; }

        public List<AvailableInventorySectionDto> Data { get; set; } = new List<AvailableInventorySectionDto>();
    }
}
