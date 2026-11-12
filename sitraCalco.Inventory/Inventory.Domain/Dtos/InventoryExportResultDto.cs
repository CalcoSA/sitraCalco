namespace Inventory.Domain.Dtos
{
    public enum InventoryExportStatus { Success, InvalidRequest, Forbidden, NotFound, InvalidParameters, InvalidData }

    // Resultado interno; la API expone Data mediante ResponseApi.
    public class InventoryExportResultDto
    {
        public InventoryExportStatus Status { get; set; }
        public string? Message { get; set; }
        public InventoryExportDto? Data { get; set; }
    }
}
