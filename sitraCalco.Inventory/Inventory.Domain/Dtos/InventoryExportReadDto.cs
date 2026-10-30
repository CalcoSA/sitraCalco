using Inventory.Domain.Models;

namespace Inventory.Domain.Dtos
{
    // Proyecciones internas de lectura. No se serializan directamente en la API.
    public class InventoryExportReadDto
    {
        public List<InventoryExportHeaderRow> Headers { get; set; } = new();
        public List<InventoryExportExecutionSummary> Summaries { get; set; } = new();
        public List<InventoryExportSelectedRow> SelectedRows { get; set; } = new();
    }

    public class InventoryExportHeaderRow
    {
        public string ExecutionId { get; set; } = null!;
        public long CenterId { get; set; }
        public long CenterTypeId { get; set; }
        public string CenterCode { get; set; } = null!;
        public string CenterName { get; set; } = null!;
        public long ConfigurationId { get; set; }
        public string ConfigurationName { get; set; } = null!;
        public long SectionId { get; set; }
        public string SectionName { get; set; } = null!;
    }

    public class InventoryExportExecutionSummary
    {
        public string ExecutionId { get; set; } = null!;
        public long BaseF410 { get; set; }
        public bool HasMixedContext { get; set; }
    }

    public class InventoryExportSelectedRow
    {
        public InventoryRecord Record { get; set; } = null!;
        public bool HasDuplicateCounts { get; set; }
    }

    public class InventoryExportCenterAccessDto
    {
        public long SolutionCenterId { get; set; }
        public long SolutionCenterTypeId { get; set; }
        public bool HasNonWarehouseRecords { get; set; }
    }
}
