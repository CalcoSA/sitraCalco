namespace KitchenTraceability.Domain.Models
{
    public partial class ReceptionType
    {
        public long reception_type_id { get; set; }
        public string name { get; set; } = null!;
        public string? form_code { get; set; }
        public bool? is_active { get; set; }
        public virtual ICollection<Reception> receptions { get; set; } = new List<Reception>();
    }
}