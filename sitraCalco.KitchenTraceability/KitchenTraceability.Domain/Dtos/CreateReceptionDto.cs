namespace KitchenTraceability.Domain.Dtos
{
    public class CreateReceptionDto
    {
        public long reception_type_id { get; set; }
        public long supplier_id { get; set; }
        public bool is_compliant { get; set; }
        public string? comments { get; set; }
    }
}