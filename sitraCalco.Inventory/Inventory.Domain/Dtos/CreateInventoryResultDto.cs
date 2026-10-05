namespace Inventory.Domain.Dtos
{
    public enum InventorySaveStatus
    {
        InvalidRequest,
        Forbidden,
        InvalidContext,
        InvalidExecution,
        Duplicate,
        Success
    }

    // Resultado funcional interno; el endpoint expone Data únicamente en caso de éxito.
    public class CreateInventoryResultDto
    {
        public InventorySaveStatus Status { get; set; }
        public string? Message { get; set; }
        public InventorySaveDto? Data { get; set; }
    }
}
