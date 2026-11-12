namespace KitchenTraceability.Domain.Dtos
{
    public class ReceptionTypeDto
    {
        public long reception_type_id { get; set; }
        public string name { get; set; } = null!;
        public string? form_code { get; set; }
        public bool? is_active { get; set; }
    }
}