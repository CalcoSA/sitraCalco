namespace KitchenTraceability.Domain.Dtos
{
    public class CreateSupplierDto
    {
        public string supplier_code { get; set; } = null!;
        public string name { get; set; } = null!;
    }
}