namespace KitchenTraceability.Domain.Dtos
{
    public class SupplierDto
    {
        public long supplier_id { get; set; }
        public string supplier_code { get; set; } = null!;
        public string name { get; set; } = null!;
    }
}