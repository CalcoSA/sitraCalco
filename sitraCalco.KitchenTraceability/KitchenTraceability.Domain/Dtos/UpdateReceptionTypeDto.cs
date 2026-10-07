namespace KitchenTraceability.Domain.Dtos
{
    public class UpdateReceptionTypeDto
    {
        public string name { get; set; } = null!;
        public string? form_code { get; set; }
        public bool? is_active { get; set; }
    }
}