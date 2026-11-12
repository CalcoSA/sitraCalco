namespace Inventory.Domain.Dtos
{
    public class ProductSyncResultDto
    {
        public int Processed { get; set; }

        public int Created { get; set; }

        public int Updated { get; set; }

        public int Unchanged { get; set; }

        public List<ProductSyncItemDto> CreatedProducts { get; set; }
            = new List<ProductSyncItemDto>();

        public List<ProductSyncItemDto> UpdatedProducts { get; set; }
            = new List<ProductSyncItemDto>();
    }
}