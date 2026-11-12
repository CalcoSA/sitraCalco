namespace KitchenTraceability.Domain.Dtos
{
    public class ConformityDetailDto
    {
        public long nonconformity_id { get; set; }
        public string nonconformity_number { get; set; } = null!;
        public string reception_type_name { get; set; } = null!;
        public string? reception_type_form_code { get; set; }
        public string reason_type { get; set; } = null!;
        public decimal? quantity { get; set; }
        public string? document_type { get; set; }
        public string? document_number { get; set; }
        public string observations { get; set; } = null!;
        public string status { get; set; } = null!;
    }
}