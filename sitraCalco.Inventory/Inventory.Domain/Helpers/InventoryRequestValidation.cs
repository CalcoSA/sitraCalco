using Inventory.Domain.Dtos;

namespace Inventory.Domain.Helpers
{
    public static class InventoryRequestValidation
    {
        public static string? Validate(CreateInventoryDto? request)
        {
            if (request is null)
                return "Debe enviar los datos del inventario.";
            if (request.SolutionCenterId <= 0)
                return "El identificador de la bodega o punto de venta debe ser mayor a cero.";
            if (request.InventoryConfigurationId <= 0)
                return "El identificador de la configuración de inventario debe ser mayor a cero.";
            if (request.SectionId <= 0)
                return "El identificador de la sección debe ser mayor a cero.";
            if (request.CountNumber is < 1 or > 3)
                return "El número de conteo debe ser 1, 2 o 3.";
            if (string.IsNullOrWhiteSpace(request.EnteredBy) || request.EnteredBy.Trim().Length > 150)
                return "El responsable enteredBy es obligatorio y no puede superar 150 caracteres.";
            if (request.Items is null || request.Items.Count == 0)
                return "Debe enviar al menos un producto en items.";
            if (request.Items.Any(item => item is null || item.ProductId <= 0))
                return "Todos los items deben contener un productId mayor a cero.";

            return null;
        }
    }
}
