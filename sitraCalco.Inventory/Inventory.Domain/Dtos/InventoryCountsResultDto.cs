namespace Inventory.Domain.Dtos
{
    public enum InventoryCountsStatus
    {
        InvalidRequest,
        Forbidden,
        NotFound,
        Success
    }

    // Resultado interno; el endpoint expone únicamente Data en las respuestas de consulta.
    public class InventoryCountsResultDto
    {
        public InventoryCountsStatus Status { get; set; }
        public string? Message { get; set; }
        public InventoryCountsDto? Data { get; set; }
    }
}
