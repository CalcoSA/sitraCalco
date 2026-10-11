namespace KitchenTraceability.Domain.Models
{
    public partial class Conformity
    {
        public long nonconformity_id { get; set; }
        public string nonconformity_number { get; set; } = null!;
        public long reception_id { get; set; }
        public long? reception_lot_id { get; set; }
        public long reception_type_id { get; set; }
        public string reason_type { get; set; } = null!;
        public decimal? quantity { get; set; }
        public string? document_type { get; set; }
        public string? document_number { get; set; }
        public string observations { get; set; } = null!;
        public string status { get; set; } = null!;
        public string created_by { get; set; } = null!;
        public DateTime created_at { get; set; }
        public virtual Reception reception { get; set; } = null!;
        public virtual ReceptionLot? reception_lot { get; set; }
        public virtual ReceptionType receptionType { get; set; } = null!;
    }
}