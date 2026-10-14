namespace Inventory.Domain.Dtos
{
    // Resultado interno de escritura; nunca se expone como respuesta HTTP.
    public sealed record AuditResult<T>(T Value, string Description = "");
}
