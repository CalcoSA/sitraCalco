namespace Inventory.Domain.Dtos
{
    public class ProductSyncResultDto
    {
        public int Processed { get; set; }

        public int Created { get; set; }

        public int Updated { get; set; }

        public int Unchanged { get; set; }
    }
}