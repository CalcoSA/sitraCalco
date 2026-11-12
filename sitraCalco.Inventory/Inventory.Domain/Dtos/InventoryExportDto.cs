namespace Inventory.Domain.Dtos
{
    public class InventoryExportDto
    {
        public string Date { get; set; } = null!;
        public SiesaExportParametersDto SiesaParameters { get; set; } = null!;
        public List<InventoryExportCenterDto> SolutionCenters { get; set; } = new();
    }

    public class InventoryExportCenterDto
    {
        public long SolutionCenterId { get; set; }
        public string SolutionCenterCode { get; set; } = null!;
        public string SolutionCenterName { get; set; } = null!;
        public string SiesaWarehouseCode { get; set; } = null!;
        public List<InventoryExportExecutionDto> Inventories { get; set; } = new();
    }

    public class InventoryExportExecutionDto
    {
        public string InventoryExecutionId { get; set; } = null!;
        public long? InventoryConfigurationId { get; set; }
        public string? InventoryConfigurationName { get; set; }
        public long? SectionId { get; set; }
        public string? SectionName { get; set; }
        public string? F410 { get; set; }
        public List<InventoryExportItemDto> Items { get; set; } = new();
        public List<InventoryExportDiagnosticDto> Diagnostics { get; set; } = new();
    }

    public class InventoryExportItemDto
    {
        public long ProductId { get; set; }
        public string Reference { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string UnitOfMeasure { get; set; } = null!;
        public string? PlanId { get; set; }
        public int CountNumber { get; set; }
        // DECIMAL(18,4) se entrega como string invariante para evitar pérdida en JavaScript.
        public string? CountValue { get; set; }
        public string? Open { get; set; }
        public string? Closed { get; set; }
        public string? MultiplicationValue { get; set; }
    }

    // Diagnósticos de exportación; no representan estados persistidos del inventario.
    public class InventoryExportDiagnosticDto
    {
        public string Code { get; set; } = null!;
        public string Message { get; set; } = null!;
        public long? ProductId { get; set; }
        public string? UnitOfMeasure { get; set; }
    }
}
