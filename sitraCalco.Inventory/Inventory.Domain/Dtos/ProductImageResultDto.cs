namespace Inventory.Domain.Dtos
{
    public enum ProductImageStatus
    {
        ProductNotFound,
        ImageNotFound,
        ImageAlreadyExists,
        Conflict,
        Success
    }

    // Resultado funcional interno; únicamente Data se expone al cliente cuando hay imagen.
    public class ProductImageResultDto
    {
        public ProductImageStatus Status { get; set; }
        public ProductImageDto? Data { get; set; }
    }
}
