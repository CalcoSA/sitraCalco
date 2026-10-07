namespace Inventory.Domain.Dtos
{
    // Estado funcional interno; el endpoint expone únicamente Data en las respuestas exitosas.
    public class AvailableInventoryProductsResultDto
    {
        public bool IsValidRole { get; set; }

        public bool IsAllowed { get; set; }

        public bool HasDetailPermission { get; set; }

        public bool SolutionCenterExists { get; set; }

        public bool ConfigurationExists { get; set; }

        public bool IsAvailable { get; set; }

        public bool IsSectionAvailable { get; set; }

        public PagedDto<AvailableInventoryProductDto> Data { get; set; } = new PagedDto<AvailableInventoryProductDto>();
    }
}
